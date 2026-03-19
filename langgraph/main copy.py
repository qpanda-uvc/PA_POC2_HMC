import asyncio
import os
import re
import json
from typing import Annotated, List, TypedDict, Optional
from dotenv import load_dotenv

from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_ollama import ChatOllama
from langchain_openai import ChatOpenAI
from langchain_core.messages import BaseMessage, HumanMessage, AIMessage
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
from langgraph.graph import StateGraph, START, END

# 1. 환경 설정 로드
load_dotenv('key.env')
os.environ["GOOGLE_API_KEY"] = os.getenv("GOOGLE_API_KEY")
os.environ["ZHIPUAI_API_KEY"] = os.getenv("Zhipu_API_KEY")

UNITY_SERVER_PARAMS = StdioServerParameters(
    command="node",
    args=["C:/Users/user/Unity Ai_01/Packages/com.gamelovers.mcp-unity/Server~/build/index.js"]
)

# 2. 상태(State) 정의
class AgentState(TypedDict):
    messages:              List[BaseMessage]       # 채팅 기록
    plan:                  str                     # Planner 자연어 요약 (사용자 표시용)
    structured_plan:       list                    # Planner가 생성한 step별 자연어 의도 목록 (메모리)
    current_step_index:    int                     # 현재까지 완료된 step 수 (재계획 시 기준점)
    last_execution_result: str                     # 직전 step 실행 결과 (재계획 시 Planner에 전달)
    user_feedback:         Optional[str]
    execution_status:      str
    execution_errors:      List[str]
    tools_info:            str                     # MCP 도구 스키마 (시작 시 1회 조회)
    scene_state:           str                     # 현재 Unity 씬 상태
    available_prefabs:     str                     # 실제 존재하는 prefab 목록
    feedback_history:      List[str]               # 누적된 사용자 피드백 이력
    execution_ledger:      List[dict]              # 실행 원장: 각 step에서 실제로 무엇을 했는지 구조화된 기록
    next_action:           str                     # 그래프 라우팅용 (confirm/feedback/cancel/execute/stop/done/next/retry)
    prefab_search_query:   str                     # prefab 검색 쿼리 (사용자 요청 또는 피드백 텍스트)

# ------------------------------------------------------------------
# 3-A. CollisionChecker — AABB 충돌 검사 + prefab bounds 실측
# ------------------------------------------------------------------

IGNORE_OBJECTS = {"Main Camera", "Directional Light", "DontDestroyOnLoad", "EventSystem"}

class CollisionChecker:
    """배치된 오브젝트의 AABB 충돌 검사 및 자동 보정"""

    def __init__(self):
        self.occupied = []              # [{"center": {x,y,z}, "half_extents": {x,y,z}, "name": str}]
        self.prefab_bounds_cache = {}   # assetPath → {"x": w, "y": h, "z": d}
        self.placed_instances = {}      # {prefab_name: [instance_id, ...]} — 삭제 시 활용

    # ── prefab 크기 실측 (유형별 1회, 캐시) ──────────────────────
    async def probe_prefab_bounds(self, session, asset_path: str) -> dict:
        if asset_path in self.prefab_bounds_cache:
            return self.prefab_bounds_cache[asset_path]

        default_bounds = {"x": 1.0, "y": 1.0, "z": 1.0}
        try:
            # 먼 곳에 임시 인스턴스 생성
            res = await session.call_tool("add_asset_to_scene", {
                "assetPath": asset_path,
                "position": {"x": 1000, "y": 1000, "z": 1000}
            })
            res_text = res.content[0].text if isinstance(res.content, list) else str(res.content)
            inst_id = None
            m = re.search(r'"instanceId"\s*:\s*(-?\d+)', res_text)
            if not m:
                m = re.search(r'instance ID\s+(-?\d+)', res_text)
            if m:
                inst_id = int(m.group(1))

            if inst_id is None:
                self.prefab_bounds_cache[asset_path] = default_bounds
                return default_bounds

            # bounds 조회
            obj_res = await session.call_tool("get_gameobject", {"idOrName": str(inst_id)})
            obj_text = obj_res.content[0].text if isinstance(obj_res.content, list) else str(obj_res.content)
            bounds = self._parse_bounds_from_text(obj_text)

            # 임시 인스턴스 삭제
            await session.call_tool("delete_gameobject", {"instanceId": inst_id})

            result = bounds if bounds else default_bounds
            self.prefab_bounds_cache[asset_path] = result
            print(f"  [CollisionChecker] {asset_path} bounds: {result}")
            return result
        except Exception as e:
            print(f"  [CollisionChecker] bounds 프로브 실패 ({asset_path}): {e}")
            self.prefab_bounds_cache[asset_path] = default_bounds
            return default_bounds

    def _parse_bounds_from_text(self, text: str) -> dict | None:
        """get_gameobject 응답에서 bounds size 추출"""
        try:
            data = json.loads(text) if isinstance(text, str) else text
        except:
            return None

        # JSON 내부 재귀 탐색: "size" 키가 {x,y,z}인 것을 찾음
        return self._find_bounds_size(data)

    def _find_bounds_size(self, obj, depth=0) -> dict | None:
        if depth > 10 or obj is None:
            return None
        if isinstance(obj, dict):
            # "bounds" → "size" → {x, y, z} 패턴
            if "bounds" in obj and isinstance(obj["bounds"], dict):
                size = obj["bounds"].get("size")
                if isinstance(size, dict) and "x" in size:
                    return {"x": abs(size["x"]), "y": abs(size["y"]), "z": abs(size["z"])}
            # BoxCollider의 "size" 필드
            if "size" in obj and isinstance(obj["size"], dict) and "x" in obj["size"]:
                s = obj["size"]
                if all(isinstance(s.get(k), (int, float)) for k in ("x", "y", "z")):
                    return {"x": abs(s["x"]), "y": abs(s["y"]), "z": abs(s["z"])}
            for v in obj.values():
                r = self._find_bounds_size(v, depth + 1)
                if r:
                    return r
        elif isinstance(obj, list):
            for item in obj:
                r = self._find_bounds_size(item, depth + 1)
                if r:
                    return r
        return None

    def _find_gameobject_bounds(self, go, parent_world_pos=None) -> dict | None:
        """gameObject 계층에서 bounds + center 추출.
        자식 우선 탐색 → 자기 자신 (children 제외) 순서.
        부모에 bounds가 있고 자식이 있으면 → 자식 위치/스케일로 center 추정."""
        parent_world_pos = parent_world_pos or {"x": 0, "y": 0, "z": 0}

        local_pos = None
        for comp in go.get("components", []):
            props = comp.get("properties", {})
            if "position" in props and isinstance(props["position"], dict):
                p = props["position"]
                if "x" in p:
                    local_pos = {"x": float(p["x"]), "y": float(p["y"]), "z": float(p["z"])}

        world_pos = {
            "x": parent_world_pos["x"] + (local_pos["x"] if local_pos else 0),
            "y": parent_world_pos["y"] + (local_pos["y"] if local_pos else 0),
            "z": parent_world_pos["z"] + (local_pos["z"] if local_pos else 0),
        }

        # 1) 자식 먼저 탐색 (자식 자체에 bounds가 있으면 그것을 사용)
        for child in go.get("children", []):
            r = self._find_gameobject_bounds(child, world_pos)
            if r:
                return r

        # 2) 이 gameObject 자체에서 bounds 탐색 (children 키 제외)
        go_no_children = {k: v for k, v in go.items() if k != "children"}
        bounds_size = self._find_bounds_size(go_no_children)
        if bounds_size:
            children = go.get("children", [])
            if children:
                # 부모에 bounds가 있지만 자식에는 없음
                # → 자식 중 가장 큰 scale의 위치를 geometry center로 추정
                center = self._estimate_center_from_children(children, world_pos)
                return {"center": center, "size": bounds_size}
            return {"center": world_pos, "size": bounds_size}

        return None

    def _estimate_center_from_children(self, children, parent_world_pos) -> dict:
        """자식 중 가장 큰 scale(volume)을 가진 오브젝트의 world position을 center로 추정"""
        best_pos = None
        best_vol = -1
        for child in children:
            child_pos = None
            child_scale = None
            for comp in child.get("components", []):
                props = comp.get("properties", {})
                if "position" in props and isinstance(props["position"], dict):
                    p = props["position"]
                    if "x" in p:
                        child_pos = {a: float(p[a]) for a in ("x", "y", "z")}
                if "scale" in props and isinstance(props["scale"], dict):
                    s = props["scale"]
                    if "x" in s:
                        child_scale = {a: abs(float(s[a])) for a in ("x", "y", "z")}
            if child_pos:
                vol = (child_scale["x"] * child_scale["y"] * child_scale["z"]) if child_scale else 0
                if vol > best_vol:
                    best_vol = vol
                    best_pos = {a: parent_world_pos[a] + child_pos[a] for a in ("x", "y", "z")}
        return best_pos if best_pos else parent_world_pos

    # ── 기존 씬 오브젝트 로딩 ────────────────────────────────────
    async def load_existing_objects(self, session):
        """씬의 기존 오브젝트를 레지스트리에 등록"""
        try:
            scene_text = await get_current_scene_state(session)
        except:
            return

        # 씬 텍스트에서 루트 오브젝트 이름 추출 (다양한 형식 대응)
        obj_names = []
        for line in scene_text.split('\n'):
            line = line.strip()
            # "- ObjectName" 또는 "ObjectName (instanceId: 123)" 등
            m = re.match(r'[-*]?\s*(.+?)(?:\s*\(|$)', line)
            if m:
                name = m.group(1).strip()
                if name and name not in IGNORE_OBJECTS and not name.startswith('['):
                    obj_names.append(name)

        for name in obj_names:
            try:
                obj_res = await session.call_tool("get_gameobject", {"idOrName": f"/{name}"})
                obj_text = obj_res.content[0].text if isinstance(obj_res.content, list) else str(obj_res.content)
                self._register_from_gameobject(obj_text, name)
            except Exception as e:
                print(f"  [CollisionChecker] '{name}' 로딩 실패: {e}")

    def _register_from_gameobject(self, text: str, fallback_name: str):
        """get_gameobject 응답에서 position + bounds 추출하여 등록"""
        try:
            data = json.loads(text) if isinstance(text, str) else text
        except:
            return

        go = data.get("gameObject", data)

        # 1) gameObject 계층에서 position+bounds 쌍 탐색 (같은 오브젝트에서 추출)
        gb = self._find_gameobject_bounds(go)

        if gb:
            position = gb["center"]
            bounds = gb["size"]
        else:
            # fallback: Transform position + _find_bounds_size
            position = {"x": 0, "y": 0, "z": 0}
            for comp in go.get("components", []):
                props = comp.get("properties", {})
                if "position" in props and isinstance(props["position"], dict):
                    p = props["position"]
                    if "x" in p:
                        position = {"x": float(p["x"]), "y": float(p["y"]), "z": float(p["z"])}
                        break
            bounds = self._find_bounds_size(go)
            if bounds is None:
                bounds = {"x": 1.0, "y": 1.0, "z": 1.0}

        half_ext = {"x": bounds["x"] / 2, "y": bounds["y"] / 2, "z": bounds["z"] / 2}
        name = go.get("name", fallback_name)
        self.register(position, half_ext, name)

        # 자식 오브젝트 재귀 등록
        for child in go.get("children", []):
            child_text = json.dumps({"gameObject": child})
            self._register_from_gameobject(child_text, child.get("name", "child"))

    # ── AABB 충돌 검사 ──────────────────────────────────────────
    def check_collision(self, position: dict, half_extents: dict, exclude_names: set = None):
        """충돌하는 첫 번째 오브젝트 반환, 없으면 None"""
        for obj in self.occupied:
            if exclude_names and obj["name"] in exclude_names:
                continue
            if self._aabb_overlap(position, half_extents, obj["center"], obj["half_extents"]):
                return obj
        return None

    @staticmethod
    def _aabb_overlap(pos_a, ext_a, pos_b, ext_b) -> bool:
        for axis in ('x', 'y', 'z'):
            if abs(pos_a[axis] - pos_b[axis]) >= ext_a[axis] + ext_b[axis]:
                return False
        return True

    def find_safe_position(self, position: dict, half_extents: dict, margin: float = 0.15,
                           exclude_names: set = None, interior: dict = None) -> dict:
        """나선형 그리드 탐색으로 충돌 없는 위치 찾기 (push 방식 대체)"""
        pos = dict(position)
        # 현재 위치에서 충돌 없으면 즉시 반환
        if not self.check_collision(pos, half_extents, exclude_names=exclude_names):
            return pos

        # 오브젝트 크기 단위로 탐색 스텝 결정
        step_x = half_extents["x"] * 2 + margin
        step_z = half_extents["z"] * 2 + margin
        best_pos = None
        best_dist = float('inf')

        # 최대 7링 나선 탐색 (~196 후보)
        for ring in range(1, 8):
            for dx in range(-ring, ring + 1):
                for dz in range(-ring, ring + 1):
                    if abs(dx) != ring and abs(dz) != ring:
                        continue  # 현재 링의 테두리만 탐색
                    candidate = {
                        "x": pos["x"] + dx * step_x,
                        "y": pos["y"],
                        "z": pos["z"] + dz * step_z
                    }
                    # interior 제약 확인
                    if interior:
                        if (candidate["x"] - half_extents["x"] < interior["min_x"] or
                            candidate["x"] + half_extents["x"] > interior["max_x"] or
                            candidate["z"] - half_extents["z"] < interior["min_z"] or
                            candidate["z"] + half_extents["z"] > interior["max_z"]):
                            continue
                    # 충돌 검사
                    if not self.check_collision(candidate, half_extents, exclude_names=exclude_names):
                        dist = (candidate["x"] - pos["x"]) ** 2 + (candidate["z"] - pos["z"]) ** 2
                        if dist < best_dist:
                            best_dist = dist
                            best_pos = candidate
            # 현재 링에서 후보를 찾았으면 가장 가까운 것 반환
            if best_pos is not None:
                return best_pos

        # 모든 링 탐색 실패 → 원래 위치 반환
        return pos

    def find_by_name(self, name: str) -> dict | None:
        """이름으로 등록된 오브젝트 찾기 (첫 번째 매칭)"""
        for obj in self.occupied:
            if obj["name"] == name:
                return obj
        return None

    def register(self, position: dict, half_extents: dict, name: str):
        self.occupied.append({"center": dict(position), "half_extents": dict(half_extents), "name": name})

    def compute_interior_bounds(self, wall_prefix: str, margin: float = 0.1) -> dict | None:
        """벽 오브젝트들의 AABB로부터 내부 빈 공간의 경계를 계산.
        클러스터링 기반: 같은 z좌표에 3개 이상 나란한 벽 = 수평 벽선(back/front),
        나머지 = 측면 벽(left/right). ㄷ자/ㅁ자 모두 대응."""
        walls = [obj for obj in self.occupied if obj["name"].startswith(wall_prefix)]
        if len(walls) < 3:
            print(f"  [CollisionChecker] '{wall_prefix}' 벽 {len(walls)}개 — 최소 3개 필요")
            return None

        print(f"  [CollisionChecker] '{wall_prefix}' 벽 {len(walls)}개 발견")
        for w in walls:
            print(f"    - '{w['name']}' center=({w['center']['x']:.2f}, {w['center']['z']:.2f}) "
                  f"half=({w['half_extents']['x']:.2f}, {w['half_extents']['z']:.2f})")

        # ── Step 1: z좌표 기준 클러스터링 ──
        z_tol = max(w["half_extents"]["x"] for w in walls) * 4  # 벽 두께의 4배
        z_sorted = sorted(walls, key=lambda w: w["center"]["z"])
        z_clusters = [[z_sorted[0]]]
        for w in z_sorted[1:]:
            if abs(w["center"]["z"] - z_clusters[-1][0]["center"]["z"]) < z_tol:
                z_clusters[-1].append(w)
            else:
                z_clusters.append([w])

        # ── Step 2: 수평 벽선 식별 (3개 이상 나란한 클러스터) ──
        h_lines = sorted([c for c in z_clusters if len(c) >= 3],
                         key=len, reverse=True)
        h_wall_ids = set()
        for line in h_lines:
            for w in line:
                h_wall_ids.add(id(w))

        # 측면 벽 = 수평 벽선에 속하지 않는 나머지
        side_walls = [w for w in walls if id(w) not in h_wall_ids]

        if not h_lines:
            # x좌표 기준으로 재시도 (90도 회전된 ㄷ자)
            x_sorted = sorted(walls, key=lambda w: w["center"]["x"])
            x_clusters = [[x_sorted[0]]]
            for w in x_sorted[1:]:
                if abs(w["center"]["x"] - x_clusters[-1][0]["center"]["x"]) < z_tol:
                    x_clusters[-1].append(w)
                else:
                    x_clusters.append([w])
            v_lines = [c for c in x_clusters if len(c) >= 3]
            if not v_lines:
                print(f"  [CollisionChecker] 수평/수직 벽선을 찾을 수 없음")
                return None
            # x축 벽선 → z축과 x축 역할 교환하여 재귀적으로 처리하는 대신
            # 간단히 v_lines를 h_lines처럼 사용 (축만 바꿈)
            return self._compute_interior_from_v_lines(walls, v_lines, z_tol, margin, wall_prefix)

        if not side_walls:
            print(f"  [CollisionChecker] 측면 벽을 찾을 수 없음 (모든 벽이 수평선에 포함)")
            return None

        print(f"  [CollisionChecker] 클러스터: 수평 벽선 {len(h_lines)}개 "
              f"({[len(c) for c in h_lines]}), 측면 벽 {len(side_walls)}개")

        # ── Step 3: 실제 벽 패널 크기 추정 (피스 간격 기반) ──
        # probe_prefab_bounds가 자식 메시 하나만 잡아 AABB가 실제보다 작을 수 있음.
        # 수평 벽선의 피스 간격 = 실제 패널 1개 폭으로 추정.
        h_line_main = h_lines[0]
        h_x_positions = sorted(set(round(w["center"]["x"], 2) for w in h_line_main))
        if len(h_x_positions) >= 2:
            spacings = [h_x_positions[i+1] - h_x_positions[i] for i in range(len(h_x_positions)-1)]
            wall_panel_half = min(spacings) / 2  # 패널 반폭 = 간격의 절반
        else:
            wall_panel_half = h_line_main[0]["half_extents"]["x"]

        # 등록된 half_extents보다 간격 기반 추정이 크면 그걸 사용
        registered_half_x = max(w["half_extents"]["x"] for w in walls)
        effective_half = max(registered_half_x, wall_panel_half)
        if effective_half > registered_half_x:
            print(f"  [CollisionChecker] 벽 패널 크기 보정: 등록={registered_half_x:.2f} → "
                  f"간격 기반={effective_half:.2f} (피스 간격={min(spacings):.2f})")

        # ── Step 4: X 경계 (측면 벽의 좌/우 inner edge) ──
        side_sorted_x = sorted(side_walls, key=lambda w: w["center"]["x"])
        left_wall = side_sorted_x[0]
        right_wall = side_sorted_x[-1]
        min_x = left_wall["center"]["x"] + effective_half + margin
        max_x = right_wall["center"]["x"] - effective_half - margin

        # ── Step 5: Z 경계 ──
        if len(h_lines) >= 2:
            # ㅁ자: 수평 벽선 2개 → 그 사이가 interior
            h_top = max(h_lines[:2], key=lambda c: c[0]["center"]["z"])
            h_bot = min(h_lines[:2], key=lambda c: c[0]["center"]["z"])
            max_z = min(w["center"]["z"] - w["half_extents"]["z"] for w in h_top) - margin
            min_z = max(w["center"]["z"] + w["half_extents"]["z"] for w in h_bot) + margin
        else:
            # ㄷ자: 수평 벽선 1개 + 열린 면
            h_line = h_lines[0]
            h_z = h_line[0]["center"]["z"]
            side_z_avg = sum(w["center"]["z"] for w in side_walls) / len(side_walls)

            if h_z > side_z_avg:
                # 수평 벽이 위(z+)에 → interior는 아래쪽, 열린 면은 측면 벽 끝까지
                max_z = min(w["center"]["z"] - w["half_extents"]["z"] for w in h_line) - effective_half - margin
                min_z = min(w["center"]["z"] - w["half_extents"]["z"] for w in side_walls)
            else:
                # 수평 벽이 아래(z-)에 → interior는 위쪽
                min_z = max(w["center"]["z"] + w["half_extents"]["z"] for w in h_line) + effective_half + margin
                max_z = max(w["center"]["z"] + w["half_extents"]["z"] for w in side_walls)

        # 유효성 검사
        if min_x >= max_x or min_z >= max_z:
            print(f"  [CollisionChecker] interior 계산 실패: 유효 범위 없음 "
                  f"x=[{min_x:.2f},{max_x:.2f}] z=[{min_z:.2f},{max_z:.2f}]")
            return None

        interior = {"min_x": min_x, "max_x": max_x, "min_z": min_z, "max_z": max_z}
        print(f"  [CollisionChecker] '{wall_prefix}' interior: "
              f"x=[{min_x:.2f}, {max_x:.2f}], z=[{min_z:.2f}, {max_z:.2f}] "
              f"(가용 크기: {max_x - min_x:.2f} x {max_z - min_z:.2f})")
        return interior

    def _compute_interior_from_v_lines(self, walls, v_lines, tol, margin, prefix):
        """수직(x축) 벽선 기반 interior 계산 (90도 회전된 ㄷ자용)"""
        v_wall_ids = set()
        for line in v_lines:
            for w in line:
                v_wall_ids.add(id(w))
        side_walls = [w for w in walls if id(w) not in v_wall_ids]
        if not side_walls:
            return None

        # 실제 벽 패널 크기 추정 (수직 벽선의 피스 간격 기반)
        v_line_main = max(v_lines, key=len)
        v_z_positions = sorted(set(round(w["center"]["z"], 2) for w in v_line_main))
        if len(v_z_positions) >= 2:
            spacings = [v_z_positions[i+1] - v_z_positions[i] for i in range(len(v_z_positions)-1)]
            wall_panel_half = min(spacings) / 2
        else:
            wall_panel_half = v_line_main[0]["half_extents"]["z"]
        registered_half = max(w["half_extents"]["z"] for w in walls)
        effective_half = max(registered_half, wall_panel_half)

        # Z 경계 (측면 벽 = 수평 방향 벽)
        side_sorted_z = sorted(side_walls, key=lambda w: w["center"]["z"])
        front_wall = side_sorted_z[0]
        back_wall = side_sorted_z[-1]
        min_z = front_wall["center"]["z"] + effective_half + margin
        max_z = back_wall["center"]["z"] - effective_half - margin

        # X 경계
        if len(v_lines) >= 2:
            v_left = min(v_lines[:2], key=lambda c: c[0]["center"]["x"])
            v_right = max(v_lines[:2], key=lambda c: c[0]["center"]["x"])
            min_x = max(w["center"]["x"] + w["half_extents"]["x"] for w in v_left) + effective_half + margin
            max_x = min(w["center"]["x"] - w["half_extents"]["x"] for w in v_right) - effective_half - margin
        else:
            v_line = v_lines[0]
            v_x = v_line[0]["center"]["x"]
            side_x_avg = sum(w["center"]["x"] for w in side_walls) / len(side_walls)
            if v_x < side_x_avg:
                min_x = max(w["center"]["x"] + w["half_extents"]["x"] for w in v_line) + effective_half + margin
                max_x = max(w["center"]["x"] + w["half_extents"]["x"] for w in side_walls)
            else:
                max_x = min(w["center"]["x"] - w["half_extents"]["x"] for w in v_line) - effective_half - margin
                min_x = min(w["center"]["x"] - w["half_extents"]["x"] for w in side_walls)

        if min_x >= max_x or min_z >= max_z:
            return None
        interior = {"min_x": min_x, "max_x": max_x, "min_z": min_z, "max_z": max_z}
        print(f"  [CollisionChecker] '{prefix}' interior (수직 벽선): "
              f"x=[{min_x:.2f}, {max_x:.2f}], z=[{min_z:.2f}, {max_z:.2f}] "
              f"(가용 크기: {max_x - min_x:.2f} x {max_z - min_z:.2f})")
        return interior

# ------------------------------------------------------------------
# 3-B. LayoutEngine — layout 의도 → 충돌 없는 좌표 배열 생성
# ------------------------------------------------------------------

class LayoutEngine:
    def __init__(self, collision_checker: CollisionChecker):
        self.cc = collision_checker

    def _find_asset_path(self, prefab_name: str, available_prefabs: str) -> str | None:
        """prefab 이름으로 available_prefabs에서 assetPath 매칭 (정확 매칭 우선)"""
        candidates = []
        for line in available_prefabs.split('\n'):
            m = re.search(r'Path:\s*(Assets/[^\s|]+)', line)
            if m:
                path = m.group(1).strip()
                stem = path.rsplit('/', 1)[-1].rsplit('.', 1)[0]
                if stem.lower() == prefab_name.lower():
                    return path                  # 정확 매칭 즉시 반환
                if prefab_name.lower() in stem.lower():
                    candidates.append(path)      # 부분 매칭 후보
        return candidates[0] if candidates else None

    async def resolve_positions(self, session, layout: dict, available_prefabs: str) -> list:
        """layout dict → 충돌 없는 좌표 리스트"""
        layout_type = layout.get("type", "custom")
        prefab_name = layout.get("prefab", "")
        container_name = layout.get("container")
        enclosure_prefix = layout.get("enclosure")

        # assetPath 결정 + bounds 조회
        asset_path = self._find_asset_path(prefab_name, available_prefabs)
        if asset_path:
            bounds = await self.cc.probe_prefab_bounds(session, asset_path)
        else:
            bounds = {"x": 1.0, "y": 1.0, "z": 1.0}

        # container 정보 조회 (MCP에서 실제 geometry 재조회)
        container_info = None
        if container_name:
            container_info = self.cc.find_by_name(container_name)
            if container_info:
                try:
                    obj_res = await session.call_tool("get_gameobject", {"idOrName": container_name})
                    obj_text = obj_res.content[0].text if isinstance(obj_res.content, list) else str(obj_res.content)
                    data = json.loads(obj_text) if isinstance(obj_text, str) else obj_text
                    go = data.get("gameObject", data)
                    gb = self.cc._find_gameobject_bounds(go)
                    if gb:
                        container_info = {
                            "center": gb["center"],
                            "half_extents": {a: gb["size"][a] / 2 for a in ("x", "y", "z")},
                            "name": container_name
                        }
                except Exception as e:
                    print(f"  [LayoutEngine] container MCP 재조회 실패: {e}, 기존 정보 사용")
                print(f"  [LayoutEngine] container '{container_name}' 발견: "
                      f"center={container_info['center']}, half_extents={container_info['half_extents']}")
            else:
                print(f"  [LayoutEngine] WARNING: container '{container_name}' 미발견, 일반 모드로 진행")

        # enclosure 내부 공간 계산
        interior = None
        near_enclosure = layout.get("near_enclosure")
        if enclosure_prefix:
            interior = self.cc.compute_interior_bounds(enclosure_prefix)
            if interior:
                print(f"  [LayoutEngine] enclosure '{enclosure_prefix}' interior: "
                      f"x=[{interior['min_x']:.2f}, {interior['max_x']:.2f}], "
                      f"z=[{interior['min_z']:.2f}, {interior['max_z']:.2f}]")

        # near_enclosure: 벽 입구 근처에 자동 배치
        raw_positions = []  # near_enclosure 또는 아래 분기에서 채워짐
        if near_enclosure and not enclosure_prefix:
            near_interior = self.cc.compute_interior_bounds(near_enclosure)
            if near_interior:
                count = layout.get("count", 1)
                # 입구 = interior의 열린 면 (min_z 또는 max_z 중 벽이 없는 쪽)
                # 열린 면 감지: 벽들의 z범위 중 interior의 min_z가 벽 끝까지 가면 열린 면
                walls = [obj for obj in self.cc.occupied if obj["name"].startswith(near_enclosure)]
                all_min_z = min(w["center"]["z"] - w["half_extents"]["z"] for w in walls)
                all_max_z = max(w["center"]["z"] + w["half_extents"]["z"] for w in walls)

                # 열린 면: interior 경계와 벽 외곽이 같은 쪽
                open_z = None
                if abs(near_interior["min_z"] - all_min_z) < 0.5:
                    open_z = "min_z"  # 남쪽이 열림
                elif abs(near_interior["max_z"] - all_max_z) < 0.5:
                    open_z = "max_z"  # 북쪽이 열림

                cx = (near_interior["min_x"] + near_interior["max_x"]) / 2
                entrance_margin = bounds["z"] + 0.5  # 오브젝트 크기 + 여유

                if open_z == "min_z":
                    entrance_z = near_interior["min_z"] - entrance_margin
                elif open_z == "max_z":
                    entrance_z = near_interior["max_z"] + entrance_margin
                else:
                    # 기본: min_z 쪽을 열린 면으로 가정
                    entrance_z = near_interior["min_z"] - entrance_margin

                # count개를 입구 앞에 x방향으로 나열
                spacing_x = bounds["x"] + 0.5
                total_width = (count - 1) * spacing_x
                start_x = cx - total_width / 2

                raw_positions = []
                for j in range(count):
                    raw_positions.append({
                        "x": start_x + j * spacing_x,
                        "y": 0,
                        "z": entrance_z
                    })
                print(f"  [LayoutEngine] near_enclosure '{near_enclosure}': "
                      f"입구 z={entrance_z:.2f}, {count}개 배치")
                # custom positions를 덮어씀 (container 오프셋 불필요 — 절대좌표)
                layout_type = "custom"  # 아래 분기에서 custom으로 처리
                # container 오프셋 적용 건너뛰기 위해 container_info 무효화
                container_info = None

        # near_enclosure가 이미 raw_positions를 생성한 경우 스킵
        near_enclosure_resolved = near_enclosure and not enclosure_prefix and raw_positions

        # enclosure가 있으면 interior 범위 내에서 grid start/spacing 자동 조정
        if near_enclosure_resolved:
            pass  # near_enclosure에서 이미 raw_positions 생성됨
        elif interior and layout_type == "grid":
            half_ext_obj = {"x": bounds["x"] / 2, "z": bounds["z"] / 2}
            margin = 0.15
            usable_min_x = interior["min_x"] + half_ext_obj["x"]
            usable_max_x = interior["max_x"] - half_ext_obj["x"]
            usable_min_z = interior["min_z"] + half_ext_obj["z"]
            usable_max_z = interior["max_z"] - half_ext_obj["z"]

            rows = layout.get("rows", 1)
            cols = layout.get("cols", 1)
            min_spacing_x = bounds["x"] + margin
            min_spacing_z = bounds["z"] + margin

            # 사용 가능한 범위에 맞게 spacing 계산
            avail_x = usable_max_x - usable_min_x
            avail_z = usable_max_z - usable_min_z
            if cols > 1:
                spacing_x = max(min_spacing_x, avail_x / (cols - 1))
            else:
                spacing_x = min_spacing_x
            if rows > 1:
                spacing_z = max(min_spacing_z, avail_z / (rows - 1))
            else:
                spacing_z = min_spacing_z

            # interior 내부에서 시작점 결정 (container 오프셋 적용 전 상대좌표가 아닌 절대좌표)
            start_x = usable_min_x
            start_z = usable_min_z

            # container 오프셋이 있으면 start를 절대좌표로 직접 설정하므로 오프셋 적용 스킵
            raw_positions = []
            for r in range(rows):
                for c in range(cols):
                    raw_positions.append({
                        "x": start_x + c * spacing_x,
                        "y": layout.get("start", {}).get("y", 0),
                        "z": start_z + r * spacing_z
                    })
            print(f"  [LayoutEngine] enclosure grid: start=({start_x:.2f}, {start_z:.2f}), "
                  f"spacing=({spacing_x:.2f}, {spacing_z:.2f}), {rows}x{cols}={len(raw_positions)}개")
        else:
            # layout type별 좌표 생성
            if layout_type == "grid":
                raw_positions = self._generate_grid(layout, bounds)
            elif layout_type == "line":
                raw_positions = self._generate_line(layout, bounds)
            elif layout_type == "perimeter":
                raw_positions = self._generate_perimeter(layout, bounds)
            elif layout_type == "custom":
                raw_positions = layout.get("positions", [])
            else:
                raw_positions = layout.get("positions", [])

            # container가 있으면 상대좌표 → 월드좌표 오프셋 (enclosure grid가 아닌 경우만)
            if container_info:
                c_center = container_info["center"]
                raw_positions = [
                    {"x": p["x"] + c_center["x"], "y": p["y"] + c_center["y"], "z": p["z"] + c_center["z"]}
                    for p in raw_positions
                ]
                print(f"  [LayoutEngine] container 오프셋 적용 (center={c_center})")

        # container 내부 클램핑 (enclosure가 없을 때만 — enclosure는 interior로 대체)
        half_ext_obj = {"x": bounds["x"] / 2, "y": bounds["y"] / 2, "z": bounds["z"] / 2}
        if container_info and not interior:
            c_center = container_info["center"]
            c_half = container_info["half_extents"]
            c_margin = layout.get("container_margin", 2.0)
            clamped_positions = []
            for pos in raw_positions:
                clamped = dict(pos)
                for axis in ('x', 'z'):
                    min_val = c_center[axis] - c_half[axis] + half_ext_obj[axis] + c_margin
                    max_val = c_center[axis] + c_half[axis] - half_ext_obj[axis] - c_margin
                    if clamped[axis] < min_val:
                        print(f"  [LayoutEngine] container 클램핑: {axis} {clamped[axis]:.2f} → {min_val:.2f}")
                        clamped[axis] = min_val
                    elif clamped[axis] > max_val:
                        print(f"  [LayoutEngine] container 클램핑: {axis} {clamped[axis]:.2f} → {max_val:.2f}")
                        clamped[axis] = max_val
                # Y축: 바닥 위에 놓이도록 보정
                floor_y = c_center["y"] + c_half["y"]
                min_y = floor_y + half_ext_obj["y"]
                if clamped["y"] < min_y:
                    print(f"  [LayoutEngine] container 클램핑: y {clamped['y']:.2f} → {min_y:.2f}")
                    clamped["y"] = min_y
                clamped_positions.append(clamped)
            # 클램핑 후 겹침 제거
            min_dist_x = bounds["x"] + 0.15
            min_dist_z = bounds["z"] + 0.15
            filtered = [clamped_positions[0]]
            for pos in clamped_positions[1:]:
                too_close = False
                for existing in filtered:
                    if abs(pos["x"] - existing["x"]) < min_dist_x and abs(pos["z"] - existing["z"]) < min_dist_z:
                        too_close = True
                        break
                if not too_close:
                    filtered.append(pos)
            if len(filtered) < len(clamped_positions):
                print(f"  [LayoutEngine] 클램핑 후 겹침 제거: {len(clamped_positions)} → {len(filtered)}개")
            raw_positions = filtered

        # 각 좌표에 충돌 검사 + 보정 적용
        half_ext = {"x": bounds["x"] / 2, "y": bounds["y"] / 2, "z": bounds["z"] / 2}
        exclude_names = {container_name} if container_name else set()
        # enclosure가 있으면 벽 오브젝트들을 exclude_names에 추가 (interior bounds가 벽 회피를 보장)
        if enclosure_prefix:
            for obj in self.cc.occupied:
                if obj["name"].startswith(enclosure_prefix):
                    exclude_names.add(obj["name"])
        exclude_names = exclude_names if exclude_names else None

        safe_positions = []
        for pos in raw_positions:
            safe_pos = self.cc.find_safe_position(pos, half_ext, exclude_names=exclude_names, interior=interior)
            if safe_pos != pos:
                print(f"  [LayoutEngine] 충돌 보정: {pos} → {safe_pos}")
            safe_positions.append(safe_pos)
            # 확정된 좌표를 미리 등록 (같은 step 내 다음 오브젝트와의 충돌 방지)
            self.cc.register(safe_pos, half_ext, prefab_name)

        return safe_positions

    def _generate_grid(self, layout: dict, bounds: dict) -> list:
        """grid 패턴 좌표 생성 — spacing이 오브젝트보다 작으면 자동 확대"""
        margin = 0.15
        min_spacing_x = bounds["x"] + margin
        min_spacing_z = bounds["z"] + margin
        spacing_x = max(layout.get("spacing_x", 1.5), min_spacing_x)
        spacing_z = max(layout.get("spacing_z", 1.5), min_spacing_z)

        if spacing_x > layout.get("spacing_x", 1.5) or spacing_z > layout.get("spacing_z", 1.5):
            print(f"  [LayoutEngine] spacing 자동 보정: x={spacing_x:.2f}, z={spacing_z:.2f} "
                  f"(bounds: {bounds['x']:.2f}x{bounds['z']:.2f})")

        start = layout.get("start", {"x": 0, "y": 0, "z": 0})
        rows = layout.get("rows", 1)
        cols = layout.get("cols", 1)

        positions = []
        for r in range(rows):
            for c in range(cols):
                positions.append({
                    "x": start["x"] + c * spacing_x,
                    "y": start["y"],
                    "z": start["z"] + r * spacing_z
                })
        return positions

    def _generate_line(self, layout: dict, bounds: dict) -> list:
        """line 패턴 좌표 생성"""
        axis = layout.get("axis", "x")
        margin = 0.15
        min_spacing = bounds[axis] + margin
        spacing = max(layout.get("spacing", 1.0), min_spacing)

        if spacing > layout.get("spacing", 1.0):
            print(f"  [LayoutEngine] line spacing 자동 보정: {spacing:.2f} (bounds {axis}={bounds[axis]:.2f})")

        start = layout.get("start", {"x": 0, "y": 0, "z": 0})
        count = layout.get("count", 1)

        positions = []
        for i in range(count):
            pos = dict(start)
            pos[axis] = start[axis] + i * spacing
            positions.append(pos)
        return positions

    def _generate_perimeter(self, layout: dict, bounds: dict) -> list:
        """perimeter(ㄷ/ㅁ) 패턴 좌표 생성"""
        center = layout.get("center", {"x": 0, "y": 0, "z": 0})
        width = layout.get("width", 4)
        depth = layout.get("depth", 3)
        open_side = layout.get("open_side", "south")
        margin = 0.15

        # spacing 계산: 축에 맞는 bounds 사용
        spacing_x = max(layout.get("spacing", 1.0), bounds["x"] + margin)
        spacing_z = max(layout.get("spacing", 1.0), bounds["z"] + margin)

        half_w = width / 2
        half_d = depth / 2
        positions = []

        # 뒤쪽 벽 (항상 포함)
        back_z = center["z"] + half_d if open_side == "south" else center["z"] - half_d
        n_back = max(1, int(width / spacing_x) + 1)
        for i in range(n_back):
            positions.append({
                "x": center["x"] - half_w + i * spacing_x,
                "y": center["y"],
                "z": back_z
            })

        # 왼쪽 벽 (뒷벽 위치 건너뛰고 개방면까지)
        n_side = max(1, int(depth / spacing_z))
        for i in range(n_side):
            z_offset = (i + 1) * spacing_z  # i+1: 뒷벽과 같은 z 중복 방지
            if open_side == "south":
                z = center["z"] + half_d - z_offset
                if z < center["z"] - half_d:
                    continue
            else:
                z = center["z"] - half_d + z_offset
                if z > center["z"] + half_d:
                    continue
            positions.append({
                "x": center["x"] - half_w,
                "y": center["y"],
                "z": z
            })

        # 오른쪽 벽 (뒷벽 위치 건너뛰고 개방면까지)
        for i in range(n_side):
            z_offset = (i + 1) * spacing_z  # i+1: 뒷벽과 같은 z 중복 방지
            if open_side == "south":
                z = center["z"] + half_d - z_offset
                if z < center["z"] - half_d:
                    continue
            else:
                z = center["z"] - half_d + z_offset
                if z > center["z"] + half_d:
                    continue
            positions.append({
                "x": center["x"] + half_w,
                "y": center["y"],
                "z": z
            })

        return positions

# ------------------------------------------------------------------
# 3-C. 헬퍼 함수 (LLM 대체 불가 - MCP API 호출)
# ------------------------------------------------------------------

async def get_available_tools(session):
    """Unity MCP 서버에서 사용 가능한 도구 및 파라미터 스키마 가져오기"""
    tools = await session.list_tools()
    tool_list = []
    for tool in tools.tools:
        schema_str = ""
        if hasattr(tool, 'inputSchema') and tool.inputSchema:
            props = tool.inputSchema.get('properties', {})
            required = tool.inputSchema.get('required', [])
            if props:
                param_descs = []
                for k, v in props.items():
                    req_mark = "*" if k in required else ""
                    param_descs.append(f"{k}{req_mark}({v.get('type','?')})")
                schema_str = f" | params: {', '.join(param_descs)}"
        tool_list.append(f"- {tool.name}: {tool.description}{schema_str}")
    return "\n".join(tool_list)

def _build_ledger_entry(step_num: int, step: dict, result: dict) -> dict:
    """실행 결과에서 구조화된 원장 항목 생성"""
    entry = {
        "step": step_num,
        "description": step.get('description', ''),
        "action": "unknown",
        "prefab": "",
        "count": 0,
        "details": []
    }
    layout = step.get('layout')
    if layout and layout.get('type') == 'delete':
        entry["action"] = "delete"
        entry["prefab"] = layout.get('prefab', '')
        deleted = [r for r in result.get('results', []) if 'result' in r and '삭제' in r.get('result', '')]
        entry["count"] = len(deleted)
    else:
        placed = []
        for r in result.get('results', []):
            rt = r.get('result', '')
            name_m = re.search(r"asset '([^']+)'", rt)
            id_m = re.search(r'instance ID\s+(-?\d+)', rt)
            if name_m and id_m:
                placed.append({"name": name_m.group(1), "id": int(id_m.group(1))})
        if placed:
            entry["action"] = "place"
            entry["prefab"] = placed[0]["name"]
            entry["count"] = len(placed)
            entry["details"] = placed
    return entry


def format_ledger_for_planner(ledger: list) -> str:
    """실행 원장을 Planner가 읽기 쉬운 요약 텍스트로 변환"""
    if not ledger:
        return ""
    lines = []
    for e in ledger:
        action = e.get('action', '?')
        prefab = e.get('prefab', '?')
        count = e.get('count', 0)
        desc = e.get('description', '')
        if action == 'place':
            lines.append(f"  Step {e['step']}: {prefab} {count}개 배치 완료 — {desc}")
        elif action == 'delete':
            lines.append(f"  Step {e['step']}: {prefab} {count}개 삭제 완료 — {desc}")
        else:
            lines.append(f"  Step {e['step']}: {desc} (결과 불분명)")
    return "\n".join(lines)


async def get_current_scene_state(session):
    """현재 Unity 씬의 오브젝트 상태를 조회"""
    try:
        result = await session.call_tool("get_scene_info", {})
        text = result.content[0].text if isinstance(result.content, list) else str(result.content)
        return text
    except:
        pass
    try:
        result = await session.call_tool("get_hierarchy", {})
        text = result.content[0].text if isinstance(result.content, list) else str(result.content)
        return text
    except:
        pass
    return "씬 상태를 조회할 수 없습니다."

async def search_prefabs_for_plan(session, plan: str) -> str:
    """
    Planner 계획에서 필요한 오브젝트 키워드를 LLM으로 추출하고,
    실제 존재하는 prefab 목록을 MCP search_prefabs로 조회하여 반환.
    LLM이 추측이 아닌 실제 경로/GUID를 사용하도록 하기 위함.
    """
    try:
        llm = ChatOllama(model="qwen2.5:14b", temperature=0)
        keyword_prompt = f"""Extract English keywords for Unity prefab search from the plan below.
Only output comma-separated English words, no explanation.
Example output: warehouse, pallet, wall, AMR

Plan:
{plan}

Keywords:"""
        response = llm.invoke(keyword_prompt)
        keywords = [k.strip() for k in response.content.strip().replace('\n', ',').split(',') if k.strip()]
        keywords = keywords[:8]
    except Exception as e:
        print(f"  키워드 추출 실패 ({e}), 기본 키워드 사용")
        keywords = ["warehouse", "pallet", "wall", "AMR"]
    print(f"  prefab 검색 키워드: {keywords}")

    all_results = []
    seen_paths = set()

    for keyword in keywords:
        try:
            result = await session.call_tool("search_prefabs", {"name": keyword})
            text = result.content[0].text if isinstance(result.content, list) else str(result.content)
            entries = re.findall(
                r'\d+\.\s+(\S+)\s+Path:\s+(Assets/[^\n]+)\s+GUID:\s+([a-f0-9]+)',
                text
            )
            for name, path, guid in entries:
                if path not in seen_paths:
                    seen_paths.add(path)
                    all_results.append(f"- Name: {name} | Path: {path} | GUID: {guid}")
        except Exception as e:
            print(f"  search_prefabs({keyword}) 실패: {e}")
            continue

    return "\n".join(all_results) if all_results else "prefab을 찾지 못했습니다."


def repair_truncated_json(text):
    """잘린 JSON을 자동으로 닫아서 파싱 시도"""
    try:
        return json.loads(text)
    except:
        pass
    repaired = text.rstrip()
    quote_count = repaired.count('"') - repaired.count('\\"')
    if quote_count % 2 != 0:
        repaired += '"'
    open_braces = repaired.count('{') - repaired.count('}')
    open_brackets = repaired.count('[') - repaired.count(']')
    last_complete = repaired.rfind('},')
    if last_complete > 0 and (open_braces > 1 or open_brackets > 0):
        repaired = repaired[:last_complete + 1]
        open_braces = repaired.count('{') - repaired.count('}')
        open_brackets = repaired.count('[') - repaired.count(']')
    repaired += ']' * open_brackets
    repaired += '}' * open_braces
    try:
        return json.loads(repaired)
    except:
        return None


def extract_tool_calls_fallback(text):
    """개별 {"tool":..., "params":...} 객체를 하나씩 추출하는 폴백"""
    results = []
    # 중첩 braces를 고려하여 각 객체를 추출
    depth = 0
    start = None
    for i, ch in enumerate(text):
        if ch == '{':
            if depth == 0:
                start = i
            depth += 1
        elif ch == '}':
            depth -= 1
            if depth == 0 and start is not None:
                obj_str = text[start:i+1]
                try:
                    obj = json.loads(obj_str)
                    if 'tool' in obj:
                        results.append(obj)
                except json.JSONDecodeError:
                    # 개별 객체도 파싱 실패하면 추가 정리 시도
                    cleaned_obj = re.sub(r'//[^\n]*', '', obj_str)
                    cleaned_obj = re.sub(r',\s*([}\]])', r'\1', cleaned_obj)
                    try:
                        obj = json.loads(cleaned_obj)
                        if 'tool' in obj:
                            results.append(obj)
                    except:
                        pass
                start = None
    return results if results else None


def clean_llm_json(raw):
    """LLM 출력에서 JSON 배열을 추출하고 정리"""
    cleaned = raw
    # 마크다운 코드 블록 제거
    cleaned = re.sub(r'```json\s*', '', cleaned)
    cleaned = re.sub(r'```\s*', '', cleaned)
    # C-style 블록 주석 제거
    cleaned = re.sub(r'/\*.*?\*/', '', cleaned, flags=re.DOTALL)
    # 라인 주석 제거 (URL의 // 는 보존)
    cleaned = re.sub(r'(?<![:\w])//[^\n]*', '', cleaned)
    # trailing comma 제거
    cleaned = re.sub(r',\s*([}\]])', r'\1', cleaned)
    # 문자열 값 내부의 제어 문자 제거 (탭/줄바꿈)
    cleaned = re.sub(r'(?<=": ")([^"]*?)[\t\r]', lambda m: m.group(0).replace('\t', ' ').replace('\r', ''), cleaned)
    return cleaned

# ------------------------------------------------------------------
# 4. 노드(에이전트) 정의
# ------------------------------------------------------------------

async def planner_node(state: AgentState) -> dict:
    """
    Planner: 씬 상태 + prefab 목록을 보고
    자연어 의도 수준의 step 목록(structured_plan)을 JSON으로 생성.
    - 각 step은 도구명 없이 '의도'만 기술 (Executor가 도구 매핑 담당)
    - 재계획 시 current_step_index 이후분만 새로 생성
    """
    llm = ChatOpenAI(
        model="glm-5",
        temperature=0.7,
        openai_api_key=os.environ["ZHIPUAI_API_KEY"],
        openai_api_base="https://open.bigmodel.cn/api/paas/v4",
    )
    #llm = ChatGoogleGenerativeAI(model="gemini-2.5-flash")
    #llm = ChatOllama(model="qwen2.5:14b", temperature=0)

    user_request      = state['messages'][0].content
    scene_state       = state.get('scene_state', '씬 상태 정보 없음')
    available_prefabs = state.get('available_prefabs', '아직 조회 안 됨')
    feedback          = state.get('user_feedback', '') or ''
    current_index     = state.get('current_step_index', 0)
    existing_plan     = state.get('structured_plan', [])

    feedback_history   = state.get('feedback_history', [])
    execution_ledger   = state.get('execution_ledger', [])
    is_replan       = bool(feedback and existing_plan)
    completed_steps = existing_plan[:current_index] if is_replan else []
    last_exec       = state.get('last_execution_result', '') or ''

    feedback_section  = f"\n[사용자 피드백 - 반드시 반영]\n{feedback}\n" if feedback else ""

    # 피드백 히스토리 섹션: 이전 피드백들을 맥락으로 제공
    history_section = ""
    if feedback_history and len(feedback_history) > 1:
        prev_feedbacks = feedback_history[:-1]  # 최신 제외 (이미 feedback_section에 포함)
        history_lines = "\n".join([f"  - {fb}" for fb in prev_feedbacks])
        history_section = f"\n[이전 피드백 이력 - 맥락 파악용]\n{history_lines}\n"

    # 실행 원장 섹션: 지금까지 실제로 무엇이 배치/삭제되었는지 구조화된 요약
    ledger_section = ""
    if execution_ledger:
        ledger_text = format_ledger_for_planner(execution_ledger)
        ledger_section = f"\n[실행 원장 — 지금까지 Executor가 실제로 수행한 작업]\n{ledger_text}\n⚠ 위 원장에서 잘못 배치된 오브젝트가 있으면 반드시 삭제 step을 먼저 추가하세요.\n"

    completed_section = ""
    if completed_steps:
        lines = "\n".join([f"  {i+1}. {s.get('description','')}" for i, s in enumerate(completed_steps)])
        completed_section = f"\n[이미 완료된 단계 (수정 불가)]\n{lines}\n"

    # 재계획 시: 직전 실행 결과 + cleanup 지시
    last_exec_section = ""
    cleanup_note      = ""
    if is_replan:
        if last_exec:
            last_exec_section = (
                f"\n[직전 Step 실행 결과 - 잘못 배치된 오브젝트 파악용]\n{last_exec}\n"
            )
        cleanup_note = (
            "10. 재계획이므로 완료된 단계 이후부터만 새로 계획해.\n"
            "11. [중요] 사용자 피드백의 의도를 정확히 파악해. 왜 이 변경을 요청하는지 이해한 후 전체 맥락에서 계획을 세워.\n"
            "12. [중요] 현재 씬 상태를 분석해서, 잘못 배치되었거나 불필요한 오브젝트가 있으면 삭제 step을 먼저 추가해.\n"
            "   - 삭제 step에는 씬 상태에 나온 실제 오브젝트 이름을 그대로 사용해. Unity 자동 네이밍을 가정하지 마.\n"
            "   - 예: 씬에 'Pallet'이 6개 있고 모두 이름이 'Pallet'이면 'Pallet' 6개 삭제라고 명시\n"
            "   - 예: 씬에 'CageWall', 'CageWall (1)', 'CageWall (2)'가 있으면 이 이름들을 정확히 명시\n"
            "   - 삭제 후 올바른 위치에 재배치 step을 추가해."
        )

    scene_header = "[현재 Unity 씬 상태 - 반드시 확인: 오브젝트 이름, 위치, 수량]" if is_replan else "[현재 Unity 씬 상태]"

    prompt = f"""너는 Unity 공장 자동화 시나리오 설계 전문가야.
사용자 요청, 현재 씬 상태, 실제 prefab 목록을 보고 단계별 실행 계획을 JSON 배열로 작성해줘.
{feedback_section}{history_section}{ledger_section}{completed_section}{last_exec_section}
[사용자 요청]
{user_request}

{scene_header}
{scene_state}

[실제 존재하는 Prefab 목록]
{available_prefabs}

작성 규칙:
1. 씬에 이미 존재하는 오브젝트는 다시 배치하지 마.
2. Prefab 목록에 있는 것만 사용해. 없으면 "prefab 검색 필요" 라고 prefab_hint에 명시해.
3. 각 step은 배치 패턴(layout)으로 작성. 정확한 좌표는 시스템이 오브젝트 크기를 기반으로 자동 계산하므로 좌표를 일일이 나열하지 마.
4. layout.type 종류:
   - "grid": NxM 격자 배열. 필수: prefab, start(시작점), rows, cols, spacing_x, spacing_z
   - "line": 1열 배치. 필수: prefab, start, count, axis("x"또는"z"), spacing
   - "perimeter": ㄷ자/ㅁ자 벽 배치. 필수: prefab, center(중심), width, depth, open_side("south"/"north"/"east"/"west"), spacing
   - "custom": 특수 배치(자유 좌표). 필수: prefab, positions 배열
   - "delete": 오브젝트 삭제. 필수: targets(삭제할 오브젝트 이름 배열)
5. spacing은 대략적 권장값만 지정 (시스템이 오브젝트 크기보다 작으면 자동 확대).
6. 도구명(add_asset_to_scene 등) 절대 언급 금지. 순수 의도만 작성.
7. 불가능한 작업 제외: 스크립트 작성, NavMesh, 물리 컴포넌트, 학습 로직.
8. 공장 학습 환경은 최소 오브젝트 3개 이상. 1개만 배치하는 계획 금지.
9. 창고(Warehouse), 건물 등 큰 오브젝트 내부에 물건을 배치할 때는 반드시 layout에 "container" 필드를 추가해.
   - container에는 씬에 이미 존재하는 오브젝트 이름을 정확히 입력 (예: "Warehouse_simpleRL")
   - container를 지정하면 start/center 좌표는 container 중심 기준 상대 좌표로 작성 (0,0,0 = container 중심)
   - container_margin: (선택, 기본 2.0) 컨테이너 벽 두께/여유 공간. AABB 경계에서 이 값만큼 안쪽으로 제한
   - 이렇게 하면 시스템이 container의 실제 위치를 기준으로 오브젝트를 내부에 배치하고, container와의 충돌 검사를 건너뜀
10. 벽(CageWall 등)으로 둘러싸인 공간 안에 물건을 배치할 때는 layout에 "enclosure" 필드를 추가해.
   - enclosure에는 벽 오브젝트 이름의 접두어를 입력 (예: "CageWall")
   - container와 enclosure를 동시에 지정 가능 (container 오프셋 적용 후 enclosure 내부에 배치)
   - enclosure를 지정하면 시스템이 벽들의 내부 공간을 자동 계산하고, 그 안에서만 배치함
   - 벽과의 충돌 보정 진동 문제가 해결됨
   - 벽 근처(입구 앞 등)에 오브젝트를 배치할 때는 "near_enclosure" 필드를 사용해. 시스템이 입구 위치를 자동 계산해줌
   - near_enclosure 사용 시 positions 좌표를 직접 지정하지 않아도 됨. count만 지정하면 입구 앞에 자동 배치
{cleanup_note}

출력 형식 (JSON 배열만, 설명 없이):
[
  {{
    "step": 1,
    "description": "작업 설명 (prefab 이름, 수량, 배치 의도)",
    "prefab_hint": "검색할 prefab 키워드 (영어, 없으면 빈 문자열)",
    "layout": {{
      "type": "grid|line|perimeter|custom|delete",
      "prefab": "프리팹 이름",
      "container": "(선택) 내부에 배치할 컨테이너 오브젝트 이름",
      "container_margin": "(선택, 기본 2.0) 컨테이너 벽 두께 여유분",
      "enclosure": "(선택) 벽 오브젝트 이름 접두어 — 벽 내부 공간에 배치 시 사용",
      "near_enclosure": "(선택) 벽 입구 근처에 배치 시 사용. count로 개수 지정",
      "...type별 파라미터..."
    }}
  }}
]

layout 예시:
- 그리드: {{"type":"grid","prefab":"Pallet","start":{{"x":2,"y":0,"z":2}},"rows":2,"cols":3,"spacing_x":1.5,"spacing_z":1.5}}
- 1열: {{"type":"line","prefab":"CageWall","start":{{"x":-2,"y":0,"z":4}},"count":5,"axis":"x","spacing":1.0}}
- ㄷ자벽: {{"type":"perimeter","prefab":"CageWall","center":{{"x":0,"y":0,"z":3}},"width":4,"depth":3,"open_side":"south","spacing":1.0}}
- 자유: {{"type":"custom","prefab":"AMR","positions":[{{"x":-2,"y":0,"z":0}},{{"x":2,"y":0,"z":0}}]}}
- 삭제: {{"type":"delete","targets":["Pallet","Pallet","CageWall"]}}
- 창고 내부 벽: {{"type":"perimeter","prefab":"CageWall","container":"Warehouse_simpleRL","center":{{"x":0,"y":0,"z":0}},"width":8,"depth":6,"open_side":"south","spacing":1.0}}
- 창고 내부 그리드: {{"type":"grid","prefab":"Pallet","container":"Warehouse_simpleRL","start":{{"x":-3,"y":0,"z":-3}},"rows":2,"cols":3,"spacing_x":1.5,"spacing_z":1.5}}
- 벽 안 그리드: {{"type":"grid","prefab":"Pallet","container":"Warehouse_simpleRL","enclosure":"CageWall","rows":2,"cols":4,"spacing_x":1.5,"spacing_z":1.5}}
- 벽 입구 앞: {{"type":"custom","prefab":"AMR","near_enclosure":"CageWall","count":2}}

출력:"""

    response = llm.invoke(prompt)
    raw = response.content

    # JSON 파싱
    try:
        cleaned = re.sub(r'```json\s*', '', raw)
        cleaned = re.sub(r'```\s*', '', cleaned)
        cleaned = re.sub(r'//[^\n]*', '', cleaned)
        cleaned = re.sub(r',\s*([}\]])', r'\1', cleaned)
        arr_match = re.search(r'\[[\s\S]*\]', cleaned)
        if not arr_match:
            raise ValueError("JSON 배열 없음")
        new_steps = json.loads(arr_match.group())
    except Exception as e:
        print(f"  Planner JSON 파싱 실패: {e}\n  raw: {raw[:300]}")
        new_steps = [{"step": 1, "description": raw.strip(), "prefab_hint": "", "positions": []}]

    # 재계획 시: 완료 step + 새 step 합치기, step 번호 재정렬
    if is_replan:
        for i, s in enumerate(new_steps):
            s['step'] = current_index + i + 1
        full_plan = completed_steps + new_steps
    else:
        full_plan = new_steps

    summary_lines = [f"  {s['step']}. {s['description']}" for s in full_plan]
    plan_summary  = "\n".join(summary_lines)

    return {
        "plan":              plan_summary,
        "structured_plan":   full_plan,
        "current_step_index": current_index if is_replan else 0,
        "user_feedback":     ""
    }



async def resolve_params_with_llm(llm, params: dict, step_results_context: str) -> dict:
    """
    파라미터 값 중 '이전 단계 결과에서 추출' 이 있는 경우,
    이전 단계 결과 텍스트에서 값을 추출하여 반영.
    assetPath는 정규식으로 직접 추출해 LLM 오파싱(GUID 혼입 등) 방지.
    """
    resolved = dict(params)

    for param_key, param_val in params.items():
        if not (isinstance(param_val, str) and "이전 단계" in param_val):
            continue

        extracted = ""

        # ── assetPath: 정규식으로 직접 추출 (LLM 사용 안 함) ──────
        if param_key == "assetPath":
            match = re.search(r'Path:\s*(Assets/[^\s\n]+\.(?:prefab|fbx|glb|asset|mat|png|jpg))',
                              step_results_context)
            if match:
                extracted = match.group(1).strip()

        # ── guid: 정규식으로 직접 추출 ─────────────────────────────
        elif param_key == "guid":
            match = re.search(r'GUID:\s*([a-f0-9]{32})', step_results_context)
            if match:
                extracted = match.group(1).strip()

        # ── parentId: instanceId(정수)를 추출 ──────────────────────
        elif param_key == "parentId":
            match = re.search(r'instance ID\s+(-?\d+)', step_results_context)
            if match:
                extracted = match.group(1).strip()

        # ── 그 외 파라미터: LLM으로 추출 ──────────────────────────
        else:
            extract_prompt = f"""아래 Unity MCP 도구 실행 결과에서 "{param_key}" 파라미터에 넣을 값만 추출해줘.

이전 단계 실행 결과:
{step_results_context[:800]}

규칙:
- 값만 한 줄로 출력. 설명 없이. 줄바꿈 없이.
- 찾을 수 없으면 빈 문자열만 출력.

추출된 값:"""
            try:
                response = llm.invoke(extract_prompt)
                extracted = response.content.strip().strip('"').strip("'").split('\n')[0].strip()
            except Exception as e:
                print(f"   파라미터 추출 실패 ({param_key}): {e}")

        if extracted:
            resolved[param_key] = extracted
            print(f"   동적 파라미터 해결: {param_key} = {extracted[:80]}")

    return resolved


async def executor_node(step: dict, session, tools_info: str, available_prefabs: str,
                        layout_engine: LayoutEngine = None) -> dict:
    """
    Executor: 자연어 의도 step 1개를 받아서
    로컬 LLM(qwen2.5:14b)이 tool명 + params JSON으로 변환 후 실행.
    - layout이 있으면 LayoutEngine으로 좌표 생성 (LLM 좌표 계산 불필요)
    - 이전 단계 결과가 필요한 파라미터는 동적 해결
    """
    llm = ChatOllama(model="qwen2.5:14b", temperature=0)

    description = step.get('description', '')
    layout      = step.get('layout')
    positions   = step.get('positions', [])

    # ── delete 전용 처리 (instance ID 기반 삭제) ──
    if layout and layout.get('type') == 'delete':
        targets = layout.get('targets', [])
        unique_targets = list(set(targets))
        print(f"  [삭제] 대상 prefab: {unique_targets}")

        results = []
        for target in unique_targets:
            # 1) 저장된 instance ID로 삭제 (배치 시 기록된 ID)
            ids = []
            base_name = target
            if layout_engine:
                ids = layout_engine.cc.placed_instances.get(target, [])
                if not ids:
                    # "CageWall_1" → base "CageWall" 추출 후 해당 키의 전체 인스턴스 삭제
                    base_match = re.match(r'^(.+?)(?:_\d+|\s*\(\d+\))$', target)
                    if base_match:
                        base_name = base_match.group(1)
                        ids = layout_engine.cc.placed_instances.get(base_name, [])
            if ids:
                print(f"  [삭제] '{target}' → {len(ids)}개 instance ID 발견 (key: '{base_name}')")
                for inst_id in ids:
                    try:
                        res = await session.call_tool("delete_gameobject", {"instanceId": inst_id})
                        res_text = res.content[0].text if isinstance(res.content, list) else str(res.content)
                        print(f"   ✓ '{target}' (ID:{inst_id}) 삭제 완료")
                        results.append({"tool": "delete_gameobject", "result": res_text[:200]})
                    except Exception as e:
                        print(f"   ✗ '{target}' (ID:{inst_id}) 삭제 실패: {e}")
                        results.append({"tool": "delete_gameobject", "error": str(e)})
                layout_engine.cc.placed_instances.pop(base_name, None)
            else:
                # 2) fallback: 이름 패턴 순회 삭제 (재시작 후 placed_instances 없을 때)
                # NOTE: objectPath에 leading '/' 없이 전달 → GameObject.Find("name") = 전체 검색
                deleted_count = 0
                # 기본 이름
                try:
                    res = await session.call_tool("delete_gameobject", {"objectPath": target})
                    rt = res.content[0].text if isinstance(res.content, list) else str(res.content)
                    if "not found" not in rt.lower():
                        deleted_count += 1
                        print(f"   ✓ '{target}' 삭제 완료")
                        results.append({"tool": "delete_gameobject", "result": rt[:200]})
                except Exception:
                    pass
                # {target}_1, {target}_2, ... 패턴
                miss = 0
                for i in range(1, 51):
                    try:
                        name = f"{target}_{i}"
                        res = await session.call_tool("delete_gameobject", {"objectPath": name})
                        rt = res.content[0].text if isinstance(res.content, list) else str(res.content)
                        if "not found" in rt.lower():
                            miss += 1
                            if miss >= 3:
                                break
                        else:
                            miss = 0
                            deleted_count += 1
                            print(f"   ✓ '{name}' 삭제 완료")
                            results.append({"tool": "delete_gameobject", "result": rt[:200]})
                    except Exception:
                        miss += 1
                        if miss >= 3:
                            break
                # Unity 자동 네이밍: {target} (1), {target} (2), ...
                miss = 0
                for i in range(1, 51):
                    try:
                        name = f"{target} ({i})"
                        res = await session.call_tool("delete_gameobject", {"objectPath": name})
                        rt = res.content[0].text if isinstance(res.content, list) else str(res.content)
                        if "not found" in rt.lower():
                            miss += 1
                            if miss >= 3:
                                break
                        else:
                            miss = 0
                            deleted_count += 1
                            print(f"   ✓ '{name}' 삭제 완료")
                            results.append({"tool": "delete_gameobject", "result": rt[:200]})
                    except Exception:
                        miss += 1
                        if miss >= 3:
                            break
                if deleted_count == 0:
                    print(f"   ✗ '{target}' 미발견")
                else:
                    print(f"  [삭제] '{target}' 총 {deleted_count}개 삭제")

        # CollisionChecker occupied에서도 제거
        if layout_engine:
            layout_engine.cc.occupied = [
                obj for obj in layout_engine.cc.occupied
                if not any(obj["name"] == t or obj["name"].startswith(t + '_') for t in unique_targets)
            ]

        success = all("error" not in r for r in results)
        return {"success": success or len(results) == 0, "results": results, "error": ""}

    # layout이 있으면 LayoutEngine으로 좌표 생성
    if layout and layout_engine and layout.get('type') not in ('delete', None):
        print(f"  [LayoutEngine] layout type={layout['type']} 좌표 생성 중...")
        positions = await layout_engine.resolve_positions(session, layout, available_prefabs)
        step['positions'] = positions
        print(f"  [LayoutEngine] {len(positions)}개 좌표 생성 완료")

    # 다수 좌표 → 첫 좌표만 LLM에 보내서 템플릿 1개 생성 (truncation 방지)
    prompt_positions = positions[:1] if len(positions) > 1 else positions

    # layout에서 prefab 힌트 추출하여 프롬프트에 강조
    prefab_hint = ""
    if layout and layout.get('prefab'):
        prefab_name = layout['prefab']
        prefab_hint = f"\n[★ 핵심 ★ 이 step에서 배치해야 할 prefab: {prefab_name}]\n반드시 '{prefab_name}'을 assetPath에 사용하세요. 다른 prefab(예: CageWall, Warehouse 등)을 배치하면 안 됩니다.\n"

    mapping_prompt = f"""너는 Unity MCP 도구 매핑 전문가야.
아래 작업 1개를 MCP 도구 호출 목록으로 변환해줘.

[작업 설명]
{description}
{prefab_hint}
[사전 계산된 좌표 목록 - 반드시 이 좌표를 사용]
{json.dumps(prompt_positions, ensure_ascii=False)}

[실제 존재하는 Prefab 목록 - 이 목록만 사용 가능]
{available_prefabs}

[사용 가능한 MCP 도구 목록 (파라미터명 포함)]
{tools_info}

규칙:
1. prefab 배치(add_asset_to_scene): assetPath는 반드시 Prefab 목록에 있는 경로만 사용. 없으면 search_prefabs를 먼저 포함.
2. 좌표 목록의 각 좌표마다 개별 tool_call을 생성해.
3. 파라미터명은 도구 목록의 이름과 정확히 일치.
4. 이전 도구 결과가 필요하면 해당 params 값을 "이전 단계 결과에서 추출" 로 표시.
5. [중요] 오브젝트 삭제(delete_gameobject) 규칙:
   - objectPath 파라미터를 사용해. 예: {{"objectPath": "/CageWall"}}, {{"objectPath": "/CageWall (1)"}}
   - objectPath는 씬 계층에서의 오브젝트 이름 (슬래시로 시작, 접두어 /). 경로가 있다면 전체 경로 사용.
   - instanceId는 Unity 정수형 ID이므로 문자열(경로, GUID)을 넣으면 절대 안 됨. 확실한 정수 instanceId를 모른 경우 objectPath를 사용.
   - 삭제할 오브젝트가 여러 개이면 각각 개별 tool_call로 생성해.
   - search_prefabs는 씨엀 에셋 검색 도구이므로 삭제에 사용하지 마.
6. parentPath, parentId는 사용하지 마. 오브젝트는 씬 루트에 배치.
7. JSON 배열만 출력. 설명 없이.

출력 형식:
[
  {{"tool": "도구이름", "params": {{"파라미터명": "값"}}}}
]

출력:"""

    MAX_MAPPING_RETRIES = 2
    tool_calls = None
    last_error = None

    for mapping_attempt in range(MAX_MAPPING_RETRIES):
        try:
            raw = llm.invoke(mapping_prompt).content
            print(f"  [DEBUG] LLM raw output ({len(raw)} chars):\n{raw[:500]}")

            cleaned = clean_llm_json(raw)
            arr_match = re.search(r'\[[\s\S]*\]', cleaned)
            if not arr_match:
                raise ValueError("JSON 배열 없음")
            json_text = arr_match.group()
            try:
                tool_calls = json.loads(json_text)
            except json.JSONDecodeError as je:
                print(f"  [DEBUG] JSON 파싱 실패: {je}")
                print(f"  [DEBUG] 문제 위치 근처: ...{json_text[max(0,je.pos-50):je.pos+50]}...")
                # 1차 폴백: 잘린 JSON 복구
                repaired = repair_truncated_json(json_text)
                if repaired and isinstance(repaired, list):
                    print("  → 잘린 JSON 복구 성공")
                    tool_calls = repaired
                else:
                    # 2차 폴백: 개별 객체 추출
                    extracted = extract_tool_calls_fallback(json_text)
                    if extracted:
                        print(f"  → 개별 객체 추출 성공: {len(extracted)}개")
                        tool_calls = extracted
                    else:
                        raise
            if tool_calls:
                break
        except Exception as e:
            last_error = e
            if mapping_attempt < MAX_MAPPING_RETRIES - 1:
                print(f"  → 도구 매핑 실패 ({e}), 재시도 중...")
            continue

    if not tool_calls:
        return {"success": False, "error": f"도구 매핑 실패: {last_error}", "results": []}

    # 다수 좌표 → position 파라미터를 가진 배치 call만 복제
    if len(positions) > 1:
        # step의 layout prefab 힌트 추출 (소문자 비교)
        expected_prefab = (layout.get('prefab', '') if layout else '').lower() if layout else ''

        placement_idx = None
        # 1차: prefab 힌트와 assetPath가 일치하는 add_asset_to_scene 우선
        if expected_prefab:
            for idx, tc in enumerate(tool_calls):
                if tc.get('tool') == 'add_asset_to_scene' and 'position' in tc.get('params', {}):
                    asset_path = tc['params'].get('assetPath', '').lower()
                    if expected_prefab in asset_path:
                        placement_idx = idx
                        break
        # 2차: prefab 매칭 실패 시 기존 로직 (첫 position 보유 call)
        if placement_idx is None:
            for idx, tc in enumerate(tool_calls):
                if 'params' in tc and 'position' in tc.get('params', {}):
                    placement_idx = idx
                    break
        if placement_idx is not None:
            print(f"  → 템플릿 선택: [{placement_idx}] {tool_calls[placement_idx].get('tool')} (assetPath: {tool_calls[placement_idx].get('params',{}).get('assetPath','?')})")

        if placement_idx is not None:
            template = tool_calls[placement_idx]
            # parentPath/parentId가 동적 해결 대기 값이면 제거 (불필요한 부모 지정 방지)
            for pkey in ('parentPath', 'parentId'):
                val = template.get('params', {}).get(pkey, '')
                if isinstance(val, str) and ('이전 단계' in val or val == ''):
                    template['params'].pop(pkey, None)
            pre_calls = tool_calls[:placement_idx]
            replicated = []
            for i, pos in enumerate(positions):
                tc = json.loads(json.dumps(template))  # deep copy
                tc['params']['position'] = pos
                # 오브젝트 이름에 인덱스 추가 (Unity Hierarchy 구별용)
                asset_path = tc['params'].get('assetPath', '')
                base_name = asset_path.rsplit('/', 1)[-1].rsplit('.', 1)[0] if asset_path else 'Object'
                tc['params']['name'] = f"{base_name}_{i+1}"
                replicated.append(tc)
            tool_calls = pre_calls + replicated  # post_calls 제거: 복제가 모든 좌표 커버
            print(f"  → 템플릿 복제: {len(replicated)}개 배치 call 생성")
            for ri, rtc in enumerate(replicated):
                rp = rtc['params']['position']
                print(f"     [{ri+1}] x={rp['x']:.2f}, y={rp['y']:.2f}, z={rp['z']:.2f}")

    print(f"  → {len(tool_calls)}개 tool call 생성")

    results          = []
    prev_result_text = ""

    for i, tc in enumerate(tool_calls):
        tool_name = tc.get('tool', '')
        params    = dict(tc.get('params', {}))

        # 이전 결과에서 동적 파라미터 해결
        needs_resolution = any(isinstance(v, str) and "이전 단계" in v for v in params.values())
        if needs_resolution and prev_result_text:
            params = await resolve_params_with_llm(llm, params, prev_result_text)

        params = {k: v for k, v in params.items() if v is not None}

        # 숫자형 파라미터 자동 변환 (LLM이 문자열로 반환하는 문제 대응)
        INT_PARAMS = {"instanceId", "parentId"}
        for key in INT_PARAMS:
            if key in params and isinstance(params[key], str):
                try:
                    params[key] = int(params[key])
                except ValueError:
                    # GUID 등 숫자 변환 불가능한 값은 제거 (MCP가 number 기대)
                    print(f"   ⚠ '{key}' 값 '{params[key][:40]}' → 숫자 변환 불가, 파라미터 제거")
                    del params[key]

        print(f"   [{i+1}/{len(tool_calls)}] {tool_name} | {params}")

        for attempt in range(3):
            try:
                result      = await session.call_tool(tool_name, params)
                result_text = result.content[0].text if isinstance(result.content, list) else str(result.content)
                prev_result_text = result_text
                results.append({"tool": tool_name, "result": result_text[:300]})
                # 배치 결과에서 이름+ID 강조 표시
                id_m = re.search(r'instance ID\s+(-?\d+)', result_text)
                name_m = re.search(r"asset '([^']+)'", result_text)
                if id_m and name_m and 'position' in params:
                    pos = params['position']
                    print(f"   ✓ '{name_m.group(1)}' (ID:{id_m.group(1)}) at ({pos.get('x',0):.1f}, {pos.get('y',0):.1f}, {pos.get('z',0):.1f})")
                else:
                    print(f"   결과: {result_text[:200]}")
                break
            except Exception as e:
                print(f"   오류 (시도 {attempt+1}): {e}")
                if attempt >= 2:
                    results.append({"tool": tool_name, "error": str(e)})

        # add_asset_to_scene 후 이름 변경 + instance ID 저장
        if tool_name == 'add_asset_to_scene':
            id_match = re.search(r'instance ID\s+(-?\d+)', prev_result_text)
            if id_match:
                inst_id = int(id_match.group(1))
                # 이름 변경 (MCP 도구가 name 파라미터 미지원)
                if 'name' in tc.get('params', {}):
                    desired_name = tc['params']['name']
                    try:
                        await session.call_tool('update_gameobject', {
                            'instanceId': inst_id,
                            'gameObjectData': {'name': desired_name}
                        })
                        print(f"   → 이름 변경: {desired_name}")
                    except Exception as e:
                        print(f"   → 이름 변경 실패: {e}")
                # instance ID 저장 (삭제 시 활용)
                if layout_engine:
                    asset_path = tc['params'].get('assetPath', '')
                    prefab = asset_path.rsplit('/', 1)[-1].rsplit('.', 1)[0] if asset_path else 'Object'
                    layout_engine.cc.placed_instances.setdefault(prefab, []).append(inst_id)

    success = all("error" not in r for r in results)
    return {"success": success, "results": results, "error": ""}

# ------------------------------------------------------------------
# 5. LangGraph 노드 함수 정의
# ------------------------------------------------------------------

async def init_node(state: AgentState, config) -> dict:
    """MCP 도구 목록, 씬 상태 조회 + 사용자 요청 입력"""
    ctx = config["configurable"]["ctx"]
    session = ctx["session"]

    print("\nMCP 도구 목록 조회 중...")
    tools_info = await get_available_tools(session)
    print(f"\n[MCP 사용 가능 도구 목록]\n{tools_info}\n")

    print("씬 상태 조회 중...")
    scene_state = await get_current_scene_state(session)
    print(f"씬 상태 (요약):\n{scene_state[:300]}...")

    user_input = input("\n요청사항을 입력하세요: ").strip()

    return {
        "messages": [HumanMessage(content=user_input)],
        "tools_info": tools_info,
        "scene_state": scene_state,
        "prefab_search_query": user_input,
    }


async def search_prefabs_graph_node(state: AgentState, config) -> dict:
    """Prefab 검색 (사용자 요청 또는 피드백 텍스트 기반)"""
    ctx = config["configurable"]["ctx"]
    session = ctx["session"]
    query = state.get("prefab_search_query") or state["messages"][0].content

    print("\n[Prefab 검색] prefab 목록 조회 중...")
    available_prefabs = await search_prefabs_for_plan(session, query)
    print(f"\n[실제 존재하는 Prefab 목록]\n{available_prefabs}")

    return {"available_prefabs": available_prefabs}


async def plan_graph_node(state: AgentState, config) -> dict:
    """Planner LLM으로 계획 수립 (씬 상태 갱신 후 호출)"""
    ctx = config["configurable"]["ctx"]
    session = ctx["session"]

    scene_state = await get_current_scene_state(session)
    state_for_planner = {**state, "scene_state": scene_state}

    print("\n[Planner] 계획 수립 중...")
    result = await planner_node(state_for_planner)
    result["scene_state"] = scene_state
    return result


async def confirm_plan_graph_node(state: AgentState, config) -> dict:
    """사용자에게 계획 확인 요청 → next_action 결정"""
    print(f"\n{'='*60}")
    print("[Planner 수립 계획]")
    print(f"{'='*60}")
    print(state['plan'])
    print(f"\n총 {len(state['structured_plan'])}단계")
    print(f"{'='*60}")

    user_confirm = input("\n이 계획으로 진행할까요? (엔터-진행, 피드백 입력-재계획, n-취소): ").strip()

    if user_confirm.lower() in ['n', 'no', '취소']:
        print("취소되었습니다.")
        return {"next_action": "cancel"}

    if user_confirm and user_confirm.lower() not in ['', 'y', 'yes']:
        return {
            "next_action": "feedback",
            "user_feedback": user_confirm,
            "feedback_history": state.get('feedback_history', []) + [user_confirm],
            "current_step_index": 0,
            "structured_plan": [],
            "prefab_search_query": user_confirm,
        }

    return {"next_action": "confirm"}


async def init_layout_graph_node(state: AgentState, config) -> dict:
    """CollisionChecker + LayoutEngine 초기화 (최초 1회만 실행)"""
    ctx = config["configurable"]["ctx"]
    session = ctx["session"]

    if ctx.get("layout_engine") is not None:
        return {}  # 이미 초기화됨 — 재계획 후 재진입 시 스킵

    print("\n[CollisionChecker] 기존 씬 오브젝트 로딩 중...")
    collision_checker = CollisionChecker()
    await collision_checker.load_existing_objects(session)
    print(f"  → {len(collision_checker.occupied)}개 오브젝트 등록")
    layout_engine = LayoutEngine(collision_checker)

    ctx["collision_checker"] = collision_checker
    ctx["layout_engine"] = layout_engine

    total = len(state.get('structured_plan', []))
    print(f"\n총 {total}단계 실행 시작")
    return {}


async def present_step_graph_node(state: AgentState, config) -> dict:
    """현재 step 표시 + 사용자 입력 → next_action 결정"""
    structured_plan = state['structured_plan']
    i = state.get('current_step_index', 0)

    if i >= len(structured_plan):
        return {"next_action": "done"}

    step = structured_plan[i]
    step_num = step.get('step', i + 1)
    total = len(structured_plan)

    print(f"\n{'='*60}")
    print(f"[Step {step_num}/{total}]")
    print(f"작업: {step.get('description', '')}")
    if step.get('layout'):
        layout = step['layout']
        print(f"레이아웃: type={layout.get('type')}, prefab={layout.get('prefab', '')}")
    elif step.get('positions'):
        print(f"좌표: {step['positions']}")
    print(f"{'='*60}")

    user_input_step = input("진행 방법: (엔터-실행, 피드백 입력-이 step부터 재계획, n-중지): ").strip()

    if user_input_step.lower() in ['n', 'no', '중지']:
        print(f"\nStep {step_num}에서 중지.")
        return {"next_action": "stop"}

    if user_input_step and user_input_step.lower() not in ['', 'y', 'yes']:
        return {
            "next_action": "feedback",
            "user_feedback": user_input_step,
            "feedback_history": state.get('feedback_history', []) + [user_input_step],
            "prefab_search_query": user_input_step,
        }

    return {"next_action": "execute"}


async def execute_step_graph_node(state: AgentState, config) -> dict:
    """현재 step 1개를 Executor로 실행"""
    ctx = config["configurable"]["ctx"]
    session = ctx["session"]
    layout_engine = ctx.get("layout_engine")

    i = state['current_step_index']
    step = state['structured_plan'][i]
    step_num = step.get('step', i + 1)

    print(f"\n[실행 중] Step {step_num}...")
    result = await executor_node(
        step, session, state['tools_info'], state['available_prefabs'],
        layout_engine
    )

    if result['success']:
        print(f"\n✓ Step {step_num} 완료")
        last_exec = "\n".join(
            r.get('result', r.get('error', '')) for r in result.get('results', [])
        )
        ledger_entry = _build_ledger_entry(step_num, step, result)
        return {
            "last_execution_result": last_exec,
            "execution_ledger": state.get('execution_ledger', []) + [ledger_entry],
            "execution_status": "success",
        }
    else:
        err_msg = result.get('error', '알 수 없는 오류')
        print(f"\n✗ Step {step_num} 실패: {err_msg}")
        return {
            "execution_errors": state.get('execution_errors', []) + [f"Step {step_num}: {err_msg}"],
            "execution_status": "failed",
        }


async def post_execute_graph_node(state: AgentState, config) -> dict:
    """실행 후 사용자 피드백 수집 + 다음 행동 결정"""
    i = state['current_step_index']

    if state.get('execution_status') == 'success':
        post_fb = input("어떻게 할까요? (엔터-다음 step 계속, 피드백 입력-재계획, n-중지): ").strip()

        if post_fb.lower() in ['n', 'no', '중지']:
            return {"next_action": "stop", "current_step_index": i + 1}

        if post_fb and post_fb.lower() not in ['', 'y', 'yes']:
            return {
                "next_action": "feedback",
                "current_step_index": i + 1,
                "user_feedback": post_fb,
                "feedback_history": state.get('feedback_history', []) + [post_fb],
                "prefab_search_query": post_fb,
            }

        new_index = i + 1
        if new_index >= len(state['structured_plan']):
            return {"next_action": "done", "current_step_index": new_index}
        return {"next_action": "next", "current_step_index": new_index}

    else:
        # 실행 실패
        retry = input("어떻게 할까요? (엔터-다음 step 계속, r-재시도, n-중지): ").strip()
        if retry.lower() in ['n', 'no']:
            return {"next_action": "stop"}
        if retry.lower() == 'r':
            return {"next_action": "retry"}  # 같은 step 재시도 (index 유지)
        return {"next_action": "next", "current_step_index": i + 1}


async def summary_graph_node(state: AgentState, config) -> dict:
    """최종 실행 결과 요약"""
    errors = state.get('execution_errors', [])
    total = len(state.get('structured_plan', []))
    completed = state.get('current_step_index', 0)

    print("\n" + "=" * 60)
    print("실행 완료")
    print("=" * 60)
    print(f"완료: {completed}/{total} 단계")
    if errors:
        print(f"\n오류 ({len(errors)}개):")
        for err in errors:
            print(f"  - {err}")
    else:
        print("오류 없음 ✓")

    return {"execution_status": "completed"}


# ------------------------------------------------------------------
# 5-B. 라우팅 함수 (조건부 엣지용)
# ------------------------------------------------------------------

def route_confirm_plan(state: AgentState) -> str:
    return state.get("next_action", "cancel")

def route_present_step(state: AgentState) -> str:
    return state.get("next_action", "stop")

def route_post_execute(state: AgentState) -> str:
    return state.get("next_action", "done")


# ------------------------------------------------------------------
# 5-C. LangGraph 그래프 정의
# ------------------------------------------------------------------
#
#  ┌───────────────────────────────────────────────────────────────┐
#  │                      그래프 흐름도                              │
#  │                                                               │
#  │  START → init → search_prefabs → plan → confirm_plan          │
#  │                      ↑                       │                │
#  │                      │              ┌────────┼────────┐       │
#  │                      │           feedback  confirm  cancel    │
#  │                      │              │        │        │       │
#  │                      └──────────────┘        │       END      │
#  │                                              ▼                │
#  │                                        init_layout            │
#  │                                              │                │
#  │                                              ▼                │
#  │                  ┌──────────────────► present_step             │
#  │                  │                       │                     │
#  │                  │              ┌────────┼────────┐            │
#  │                  │           execute  feedback  stop/done      │
#  │                  │              │        │        │            │
#  │                  │              │   search_prefabs│            │
#  │                  │              ▼   (→plan→...)   │            │
#  │                  │        execute_step            │            │
#  │                  │              │                 │            │
#  │                  │              ▼                 │            │
#  │                  │        post_execute            │            │
#  │                  │         │    │    │            │            │
#  │                  │       next retry feedback      │            │
#  │                  │         │    │    │            │            │
#  │                  └─────────┘    │ search_prefabs  ▼            │
#  │                         ↑      │                summary → END │
#  │                         └──────┘                              │
#  └───────────────────────────────────────────────────────────────┘

graph = StateGraph(AgentState)

# 노드 등록
graph.add_node("init",           init_node)
graph.add_node("search_prefabs", search_prefabs_graph_node)
graph.add_node("plan",           plan_graph_node)
graph.add_node("confirm_plan",   confirm_plan_graph_node)
graph.add_node("init_layout",    init_layout_graph_node)
graph.add_node("present_step",   present_step_graph_node)
graph.add_node("execute_step",   execute_step_graph_node)
graph.add_node("post_execute",   post_execute_graph_node)
graph.add_node("summary",        summary_graph_node)

# 엣지 정의
graph.add_edge(START, "init")
graph.add_edge("init", "search_prefabs")
graph.add_edge("search_prefabs", "plan")
graph.add_edge("plan", "confirm_plan")

graph.add_conditional_edges("confirm_plan", route_confirm_plan, {
    "confirm":  "init_layout",
    "feedback": "search_prefabs",  # 피드백 → prefab 재검색 → 재계획
    "cancel":   END,
})

graph.add_edge("init_layout", "present_step")

graph.add_conditional_edges("present_step", route_present_step, {
    "execute":  "execute_step",
    "feedback": "search_prefabs",  # 이 step부터 재계획
    "stop":     "summary",
    "done":     "summary",
})

graph.add_edge("execute_step", "post_execute")

graph.add_conditional_edges("post_execute", route_post_execute, {
    "next":     "present_step",    # 다음 step으로
    "retry":    "execute_step",    # 같은 step 재시도
    "feedback": "search_prefabs",  # 다음 step부터 재계획
    "stop":     "summary",
    "done":     "summary",
})

graph.add_edge("summary", END)

# 컴파일
workflow = graph.compile()


# ------------------------------------------------------------------
# 6. 메인 진입점
# ------------------------------------------------------------------

async def run_workflow():
    """LangGraph 워크플로우 실행"""
    print("=" * 60)
    print("Unity Factory Automation - LangGraph Planner + Step Executor")
    print("=" * 60)

    async with stdio_client(UNITY_SERVER_PARAMS) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()

            # 비직렬화 객체(MCP 세션, 엔진)는 config를 통해 노드에 전달
            ctx = {
                "session": session,
                "collision_checker": None,
                "layout_engine": None,
            }
            config = {"configurable": {"ctx": ctx}}

            initial_state: AgentState = {
                "messages":              [],
                "plan":                  "",
                "structured_plan":       [],
                "current_step_index":    0,
                "last_execution_result": "",
                "user_feedback":         None,
                "execution_status":      "",
                "execution_errors":      [],
                "tools_info":            "",
                "scene_state":           "",
                "available_prefabs":     "",
                "feedback_history":      [],
                "execution_ledger":      [],
                "next_action":           "",
                "prefab_search_query":   "",
            }

            await workflow.ainvoke(initial_state, config=config)


if __name__ == "__main__":
    asyncio.run(run_workflow())