using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// DT 재구성용 에디터 도구 (간소화 씬 버전)
///
/// 4개 그룹:
///   VMSArea   - VMS 컨베이어 + 입고 AMR(Receipt 10) + 입고 웨이포인트
///   ASRSArea  - ASRS + Stacker + ASRS 컨베이어 + ASRS 웨이포인트
///   ULDArea   - AMR_Receipt (9) + 출고 웨이포인트
///   Obstacles - palet, Wire Wall (장애물)
///
/// 사용법: Unity 메뉴 → HMC → Setup DT Hierarchy
/// </summary>
public class SetupDTHierarchy : EditorWindow
{
    [MenuItem("HMC/Setup DT Hierarchy")]
    static void ShowWindow()
    {
        GetWindow<SetupDTHierarchy>("DT Hierarchy Setup");
    }

    private Vector2 scrollPos;
    private Dictionary<string, List<string>> previewGroups = new Dictionary<string, List<string>>();
    private bool showPreview = false;

    void OnGUI()
    {
        GUILayout.Label("DT 장비·웨이포인트 재구성", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "4개 그룹으로 재구성합니다:\n" +
            "  VMSArea   — VMS 컨베이어 + 입고 AMR + 입고 웨이포인트\n" +
            "  ASRSArea  — ASRS + Stacker + ASRS 웨이포인트\n" +
            "  ULDArea   — AMR_Receipt (9) + 출고 웨이포인트\n" +
            "  Obstacles — palet, VMS_Mesh (장애물)\n\n" +
            "Area를 이동하면 장비 + 웨이포인트가 함께 움직입니다.",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("미리보기 (Preview)", GUILayout.Height(30)))
        {
            previewGroups.Clear();
            var grouping = ComputeGrouping();
            foreach (var kv in grouping)
                previewGroups[kv.Key] = kv.Value.Select(go => $"  {go.name}").ToList();
            showPreview = true;
        }

        if (showPreview)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            foreach (var group in previewGroups)
            {
                EditorGUILayout.LabelField($"[{group.Key}] ({group.Value.Count}개)", EditorStyles.boldLabel);
                foreach (var item in group.Value)
                    EditorGUILayout.LabelField(item);
                GUILayout.Space(3);
            }
            EditorGUILayout.EndScrollView();
        }

        GUILayout.Space(10);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("실행 (Apply)", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("확인", "4개 그룹으로 재구성합니다.\nUndo 지원됩니다.", "실행", "취소"))
                ApplyGrouping();
        }
        GUI.backgroundColor = Color.white;
    }

    // ==================== 그룹 분류 로직 ====================

    static readonly string[] OBSTACLE_KEYWORDS = { "palet", "Palet", "VMS_Mesh", "Wire" };
    static readonly string[] GROUP_NAMES = { "VMSArea", "ASRSArea", "ULDArea", "Obstacles" };

    Dictionary<string, List<GameObject>> ComputeGrouping()
    {
        var result = new Dictionary<string, List<GameObject>>();
        foreach (var name in GROUP_NAMES)
            result[name] = new List<GameObject>();

        var assigned = new HashSet<int>();

        // ---- 1. 장비 직접 할당 ----

        // VMS 컨베이어 → VMSArea
        foreach (var c in Object.FindObjectsOfType<Conveyor>())
        {
            if (c.conveyorType == ConveyorType.VMSIn || c.conveyorType == ConveyorType.VMSOut)
                Assign(result, assigned, c.gameObject, "VMSArea");
        }

        // "4. AMR_Receipt (10)" → VMSArea (입고 AMR)
        foreach (var go in FindSceneObjectsByName("4. AMR_Receipt (10)"))
            Assign(result, assigned, go, "VMSArea");

        // "VMS" 오브젝트 → VMSArea
        foreach (var go in FindSceneObjectsByName("VMS"))
            Assign(result, assigned, go, "VMSArea");

        // ASRS (통째로) + Stacker → ASRSArea
        // 주의: ASRS 내부 자식(ASRSOut, Storage 등)은 개별 분리하면 안 됨
        var asrs = Object.FindObjectOfType<ASRS>();
        if (asrs != null)
            Assign(result, assigned, asrs.gameObject, "ASRSArea");

        var stacker = Object.FindObjectOfType<StackerCrane>();
        if (stacker != null)
            Assign(result, assigned, stacker.gameObject, "ASRSArea");

        // "4. AMR_Receipt (9)" → ULDArea (출고)
        foreach (var go in FindSceneObjectsByName("4. AMR_Receipt (9)"))
            Assign(result, assigned, go, "ULDArea");

        // ---- 2. 장애물 ----
        foreach (var go in GetAllSceneRootObjects())
        {
            if (assigned.Contains(go.GetInstanceID())) continue;
            if (IsObstacle(go))
                Assign(result, assigned, go, "Obstacles");
        }

        // ---- 3. 웨이포인트 분류 ----

        Vector3 vmsPos = GetGroupCenter(result["VMSArea"]);
        Vector3 asrsPos = asrs != null ? asrs.transform.position : Vector3.zero;
        Vector3 uldPos = GetGroupCenter(result["ULDArea"]);

        // AMR 웨이포인트 — 경로별 명시적 분류
        var amrWP = Object.FindObjectOfType<AMRWaypointManager>();
        if (amrWP != null)
        {
            // === 입고 경로 ===
            // VmsToASRS: 앞쪽 절반 → VMSArea, 뒤쪽 절반 → ASRSArea
            ClassifyPathSplit(result, assigned, amrWP.VmsToASRS, "VMSArea", "ASRSArea");
            // ASRSToVMSWaiting: 앞쪽 → ASRSArea, 뒤쪽 → VMSArea
            ClassifyPathSplit(result, assigned, amrWP.ASRSToVMSWaiting, "ASRSArea", "VMSArea");
            // VMSWaitingToVMS: 전부 → VMSArea
            ClassifyPathAll(result, assigned, amrWP.VMSWaitingToVMS, "VMSArea");

            // === 출고 경로 ===
            // ASRSToULD: 앞쪽 → ASRSArea, 뒤쪽 → ULDArea
            ClassifyPathSplit(result, assigned, amrWP.ASRSToULD, "ASRSArea", "ULDArea");
            // ULDToASRSWaiting: 앞쪽 → ULDArea, 뒤쪽 → ASRSArea
            ClassifyPathSplit(result, assigned, amrWP.ULDToASRSWaiting, "ULDArea", "ASRSArea");
            // ASRSEntryWaiting: 전부 → ASRSArea
            ClassifyPathAll(result, assigned, amrWP.ASRSEntryWaiting, "ASRSArea");
            // WaitingToASRS: 전부 → ASRSArea
            ClassifyPathAll(result, assigned, amrWP.WaitingToASRS, "ASRSArea");
        }

        // ForkLift 웨이포인트 (남아있다면)
        var forkWP = Object.FindObjectOfType<ForkLiftWaypointManager>();
        if (forkWP != null)
        {
            foreach (var wp in forkWP.InputForkLiftPath)
                if (wp != null) Assign(result, assigned, wp, "VMSArea");
            foreach (var wp in forkWP.OutputForkLiftPath)
                if (wp != null) Assign(result, assigned, wp, "ULDArea");
        }

        // LocationReference 하위 미분류 웨이포인트
        var locRef = GameObject.Find("LocationReference");
        if (locRef != null)
        {
            for (int i = 0; i < locRef.transform.childCount; i++)
            {
                var child = locRef.transform.GetChild(i).gameObject;
                if (assigned.Contains(child.GetInstanceID())) continue;
                if (IsObstacle(child)) { Assign(result, assigned, child, "Obstacles"); continue; }
                string group = NearestArea(child.transform.position, vmsPos, asrsPos, uldPos);
                Assign(result, assigned, child, group);
            }
        }

        return result;
    }

    // ==================== 헬퍼 ====================

    void Assign(Dictionary<string, List<GameObject>> result, HashSet<int> assigned, GameObject go, string group, bool lockChildren = false)
    {
        if (go == null || assigned.Contains(go.GetInstanceID())) return;
        assigned.Add(go.GetInstanceID());
        result[group].Add(go);

        // 자식이 있는 오브젝트는 자식들도 assigned에 등록 (다른 그룹에 빠지지 않도록)
        if (lockChildren || go.transform.childCount > 0)
        {
            foreach (Transform child in go.GetComponentsInChildren<Transform>())
                assigned.Add(child.gameObject.GetInstanceID());
        }
    }

    /// <summary>경로의 앞 절반은 startArea, 뒤 절반은 endArea에 할당</summary>
    void ClassifyPathSplit(Dictionary<string, List<GameObject>> result, HashSet<int> assigned,
        List<GameObject> waypoints, string startArea, string endArea)
    {
        if (waypoints == null) return;
        int half = waypoints.Count / 2;
        for (int i = 0; i < waypoints.Count; i++)
        {
            var wp = waypoints[i];
            if (wp == null || assigned.Contains(wp.GetInstanceID())) continue;
            if (wp.name.Contains("AMR_Receipt")) continue;
            Assign(result, assigned, wp, i < half ? startArea : endArea);
        }
    }

    /// <summary>경로의 모든 웨이포인트를 하나의 Area에 할당</summary>
    void ClassifyPathAll(Dictionary<string, List<GameObject>> result, HashSet<int> assigned,
        List<GameObject> waypoints, string area)
    {
        if (waypoints == null) return;
        foreach (var wp in waypoints)
        {
            if (wp == null || assigned.Contains(wp.GetInstanceID())) continue;
            if (wp.name.Contains("AMR_Receipt")) continue;
            Assign(result, assigned, wp, area);
        }
    }

    string NearestArea(Vector3 pos, Vector3 vms, Vector3 asrs, Vector3 uld)
    {
        float dV = Vector3.Distance(pos, vms);
        float dA = Vector3.Distance(pos, asrs);
        float dU = Vector3.Distance(pos, uld);
        float min = Mathf.Min(dV, dA, dU);
        if (min == dV) return "VMSArea";
        if (min == dA) return "ASRSArea";
        return "ULDArea";
    }

    Vector3 GetGroupCenter(List<GameObject> objects)
    {
        if (objects == null || objects.Count == 0) return Vector3.zero;
        return objects.Where(o => o != null)
            .Aggregate(Vector3.zero, (sum, o) => sum + o.transform.position) / objects.Count;
    }

    bool IsObstacle(GameObject go)
    {
        foreach (var kw in OBSTACLE_KEYWORDS)
            if (go.name.Contains(kw)) return true;
        return false;
    }

    List<GameObject> FindSceneObjectsByName(string exactName)
    {
        var found = new List<GameObject>();
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene.isLoaded && go.name == exactName)
                found.Add(go);
        }
        return found;
    }

    GameObject[] GetAllSceneRootObjects()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
    }

    // ==================== 적용 ====================

    void ApplyGrouping()
    {
        var grouping = ComputeGrouping();

        Undo.SetCurrentGroupName("DT Hierarchy Setup");
        int undoGroup = Undo.GetCurrentGroup();

        // Area 부모 오브젝트 생성
        foreach (var groupName in GROUP_NAMES)
        {
            var existing = GameObject.Find(groupName);
            if (existing == null)
            {
                var obj = new GameObject(groupName);
                Undo.RegisterCreatedObjectUndo(obj, "Create " + groupName);
                obj.transform.position = Vector3.zero; // 원점에 생성 → 자식 로컬좌표 = 월드좌표
            }
        }

        // 오브젝트 재배치
        int totalMoved = 0;
        foreach (var kv in grouping)
        {
            var parent = GameObject.Find(kv.Key);
            if (parent == null) continue;

            foreach (var go in kv.Value)
            {
                if (go == null || go.transform.parent == parent.transform) continue;

                Vector3 worldPos = go.transform.position;
                Quaternion worldRot = go.transform.rotation;
                Vector3 worldScale = go.transform.lossyScale;

                Undo.SetTransformParent(go.transform, parent.transform, "Reparent " + go.name);

                go.transform.position = worldPos;
                go.transform.rotation = worldRot;

                totalMoved++;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[DT Setup] 완료: {totalMoved}개 오브젝트 → {GROUP_NAMES.Length}개 그룹");
        foreach (var kv in grouping)
            Debug.Log($"  [{kv.Key}] {kv.Value.Count}개: {string.Join(", ", kv.Value.Select(g => g.name))}");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
