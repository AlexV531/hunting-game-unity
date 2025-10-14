using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemDatabase))]
public class ItemDatabaseEditor : Editor
{
    private SerializedProperty allItemsProp;

    private void OnEnable()
    {
        allItemsProp = serializedObject.FindProperty("allItems");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw default inspector for the list
        EditorGUILayout.PropertyField(allItemsProp, includeChildren: true);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Add", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Generic Item"))
        {
            AddItem(typeof(ItemDefinition));
        }

        if (GUILayout.Button("Add Weapon"))
        {
            AddItem(typeof(WeaponDefinition));
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AddItem(System.Type type)
    {
        int index = allItemsProp.arraySize;
        allItemsProp.InsertArrayElementAtIndex(index);

        var element = allItemsProp.GetArrayElementAtIndex(index);
        element.managedReferenceValue = System.Activator.CreateInstance(type);
    }
}
