
using UnityEditor;
using UnityEngine;
using WSH;

[CustomEditor(typeof(WayPoint))]
public class WayPointEditor : Editor
{
    WayPoint wp;

    void OnEnable()
    {
        wp = (WayPoint)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("Add Point"))
        {
            wp.CreateConnectPoint();
        }
    }
}