"""
MCP로 현재 Unity 씬을 분석하여 오브젝트 그룹/페어링을 자동 감지
"""
import asyncio
import json
import math
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

UNITY_SERVER_PARAMS = StdioServerParameters(
    command="node",
    args=["C:/Users/user/HyundaiWia_DemoCenter-refactoring/Packages/com.gamelovers.mcp-unity/Server~/build/index.js"]
)

async def main():
    print("=" * 60)
    print("Unity Scene Auto-Analysis")
    print("=" * 60)

    async with stdio_client(UNITY_SERVER_PARAMS) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()

            # 1. Hierarchy 가져오기
            print("\n[1] Scene hierarchy...")
            result = await session.read_resource("unity://scenes_hierarchy")
            hierarchy_text = result.contents[0].text if hasattr(result.contents[0], 'text') else str(result.contents[0])
            data = json.loads(hierarchy_text)

            # 루트 오브젝트 추출
            root_objects = []
            if isinstance(data, list):
                for scene in data:
                    for obj in scene.get("rootObjects", []):
                        root_objects.append(obj)

            print(f"  Root objects: {len(root_objects)}")

            # Object 그룹 (동적 설비) 의 자식들만 집중 분석
            target_paths = []
            for obj in root_objects:
                name = obj.get("name", "")
                if name in ("Object", "Object_Static"):
                    for child in obj.get("children", []):
                        child_name = child.get("name", "")
                        path = f"{name}/{child_name}"
                        target_paths.append((path, child_name, name))

            print(f"  Analysis targets: {len(target_paths)} objects under Object/ and Object_Static/")

            # 2. 각 오브젝트 위치 조회
            print("\n[2] Getting positions...")
            objects_info = []

            for path, name, parent in target_paths:
                try:
                    result = await session.call_tool("get_gameobject", {"idOrName": path})
                    obj_text = result.content[0].text if isinstance(result.content, list) else str(result.content)
                    obj_data = json.loads(obj_text)
                    go = obj_data.get("gameObject", obj_data)

                    # position, components 추출
                    position = {"x": 0, "y": 0, "z": 0}
                    components = []
                    for comp in go.get("components", []):
                        comp_type = comp.get("type", "")
                        components.append(comp_type)
                        props = comp.get("properties", {})
                        if "position" in props and isinstance(props["position"], dict):
                            position = props["position"]

                    children = [c.get("name", "?") for c in go.get("children", [])]

                    objects_info.append({
                        "path": path,
                        "name": name,
                        "parent": parent,
                        "position": position,
                        "components": components,
                        "children": children,
                    })
                    px = position.get("x", 0)
                    py = position.get("y", 0)
                    pz = position.get("z", 0)
                    comp_short = [c for c in components if c not in ("Transform", "GameObject")]
                    print(f"  [{parent}] {name}: ({px:.2f}, {py:.2f}, {pz:.2f}) comps={comp_short} children={children[:3]}")
                except Exception as e:
                    err = str(e)[:100]
                    print(f"  [{parent}] {name}: ERROR - {err}")

            # 3. 근접 그룹 분석 (Object 내부 오브젝트끼리)
            print("\n[3] Proximity pairs (Object group, distance < 5.0)...")
            obj_group = [o for o in objects_info if o["parent"] == "Object"]
            for i, a in enumerate(obj_group):
                for j, b in enumerate(obj_group):
                    if i >= j:
                        continue
                    dx = a["position"].get("x", 0) - b["position"].get("x", 0)
                    dz = a["position"].get("z", 0) - b["position"].get("z", 0)
                    dist = math.sqrt(dx*dx + dz*dz)
                    if 0.01 < dist < 5.0:
                        print(f"  PAIR: {a['name']} <-> {b['name']} (dist={dist:.2f})")

            # 4. Object <-> Object_Static 근접 쌍
            print("\n[4] Cross-group pairs (Object <-> Object_Static, distance < 3.0)...")
            static_group = [o for o in objects_info if o["parent"] == "Object_Static"]
            for a in obj_group:
                for b in static_group:
                    dx = a["position"].get("x", 0) - b["position"].get("x", 0)
                    dz = a["position"].get("z", 0) - b["position"].get("z", 0)
                    dist = math.sqrt(dx*dx + dz*dz)
                    if 0.01 < dist < 3.0:
                        print(f"  PAIR: {a['name']} (Object) <-> {b['name']} (Static) dist={dist:.2f}")

            # 5. 요약
            print("\n[5] Object group layout:")
            for o in obj_group:
                px = o["position"].get("x", 0)
                pz = o["position"].get("z", 0)
                print(f"  {o['name']:30s} x={px:7.2f}  z={pz:7.2f}")

            print("\n" + "=" * 60)

if __name__ == "__main__":
    asyncio.run(main())
