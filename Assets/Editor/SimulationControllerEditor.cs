using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SimulationController))]
public class SimulationControllerEditor : Editor
{
    private GUIStyle boldLabel;
    private SerializedProperty resolution;
    private SerializedProperty maxPopulation;
    private SerializedProperty livingPopulation;

    private void OnEnable()
    {
        boldLabel = new GUIStyle();
        boldLabel.normal.textColor = Color.white;
        boldLabel.fontSize = 14;
        boldLabel.fontStyle = FontStyle.Bold;

        resolution = serializedObject.FindProperty("resolution");
        maxPopulation = serializedObject.FindProperty("maxPopulation");
        livingPopulation = serializedObject.FindProperty("livingPopulation");
    }

    // Update is called once per frame
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1. Shader Properties
        EditorGUI.indentLevel = 0;
        EditorGUILayout.LabelField("1. Shader Properties", boldLabel);

        EditorGUI.indentLevel = 1;
        EditorGUILayout.PropertyField(resolution);

        // 2. Simulation Properties
        EditorGUI.indentLevel = 0;
        EditorGUILayout.LabelField("2. Simulation Properties", boldLabel);

        EditorGUI.indentLevel = 1;
        EditorGUILayout.PropertyField(maxPopulation);
        EditorGUILayout.LabelField("Living Population: " + livingPopulation.intValue);

        serializedObject.ApplyModifiedProperties();
    }
}
