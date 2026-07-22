using UnityEditor;
using UnityEngine;

public class TransformApplierWindow : EditorWindow
{
    private GameObject referenceObject;

    [MenuItem("Tools/Transform Applier")]
    private static void Open()
    {
        GetWindow<TransformApplierWindow>("Transform Applier");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Reference Transform", EditorStyles.boldLabel);

        referenceObject = (GameObject)EditorGUILayout.ObjectField(
            "Reference Object",
            referenceObject,
            typeof(GameObject),
            true);

        EditorGUILayout.Space();

        GUI.enabled = referenceObject != null && Selection.gameObjects.Length > 0;

        if (GUILayout.Button("Apply To Selected"))
        {
            ApplyTransform();
        }

        GUI.enabled = true;

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            $"Selected Objects: {Selection.gameObjects.Length}",
            MessageType.Info);
    }

    private void ApplyTransform()
    {
        Transform source = referenceObject.transform;

        foreach (GameObject target in Selection.gameObjects)
        {
            if (target == referenceObject)
                continue;

            Undo.RecordObject(target.transform, "Apply Transform");

            target.transform.position = source.position;
            target.transform.rotation = source.rotation;
            target.transform.localScale = source.localScale;

            EditorUtility.SetDirty(target.transform);
        }
    }
}