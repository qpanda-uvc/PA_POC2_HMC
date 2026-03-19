using UnityEditor;
using UnityEngine;
using WSH;
[CustomEditor(typeof(Map))]
public class MapEditor : Editor
{
    Map map;

    void OnEnable()
    {
        map = (Map)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Point Load"))
        {
            map.PointLoad();
        }
    }
}

