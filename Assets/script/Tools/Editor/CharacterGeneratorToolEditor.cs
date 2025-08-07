#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Elementor
{
    [CustomEditor(typeof(CharacterGeneratorTool))]
    public class CharacterGeneratorToolEditor : Editor
    {
        private CharacterGeneratorTool tool;
        private string newCharacterName = "";

        private void OnEnable()
        {
            tool = (CharacterGeneratorTool)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Character Generation Tools", EditorStyles.boldLabel);

            // Character name input section
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Add New Character", EditorStyles.boldLabel);
            newCharacterName = EditorGUILayout.TextField("Character Name", newCharacterName);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Character Name"))
            {
                if (!string.IsNullOrEmpty(newCharacterName))
                {
                    tool.AddCharacterName(newCharacterName);
                    newCharacterName = "";
                    EditorUtility.SetDirty(tool);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Group generation section
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Chemical Formula Generation", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField("Input chemical formulas (separated by commas):");
            EditorGUILayout.LabelField("Examples: Fe, Cl2, Fe3O4, H2O, NaCl", EditorStyles.helpBox);
            
            string currentGroupText = tool.GetGroupInputText();
            string newGroupText = EditorGUILayout.TextArea(currentGroupText, GUILayout.Height(60));
            
            if (newGroupText != currentGroupText)
            {
                tool.SetGroupInputText(newGroupText);
                EditorUtility.SetDirty(tool);
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Parse and Generate", GUILayout.Height(25)))
            {
                tool.ParseAndGenerateGroups();
            }
            EditorGUILayout.EndHorizontal();
            
            // Show parsed groups
            List<string> parsedGroups = tool.GetParsedGroups();
            if (parsedGroups.Count > 0)
            {
                EditorGUILayout.LabelField("Parsed Formulas:", EditorStyles.boldLabel);
                foreach (string group in parsedGroups)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {group}", GUILayout.Width(150));
                    if (GUILayout.Button("Generate", GUILayout.Width(80)))
                    {
                        tool.GenerateFromFormula(group);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            // Individual character generation
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Generate Individual Characters", EditorStyles.boldLabel);
            
            SerializedProperty characterNames = serializedObject.FindProperty("characterNames");
            for (int i = 0; i < characterNames.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                string characterName = characterNames.GetArrayElementAtIndex(i).stringValue;
                EditorGUILayout.LabelField(characterName, GUILayout.Width(150));
                
                if (GUILayout.Button("Generate", GUILayout.Width(80)))
                {
                    tool.GenerateCharacter(characterName);
                }
                
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    tool.RemoveCharacterName(i);
                    EditorUtility.SetDirty(tool);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            // Batch generation
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate All Characters", GUILayout.Height(30)))
            {
                tool.GenerateAllCharacters();
            }
            
            if (GUILayout.Button("Clear All Generated", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear Characters", 
                    "Are you sure you want to clear all generated characters and groups?", 
                    "Yes", "No"))
                {
                    tool.ClearGeneratedCharacters();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            // Transform management
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Transform Management", EditorStyles.boldLabel);
            
            SerializedProperty spawnTransforms = serializedObject.FindProperty("spawnTransforms");
            for (int i = 0; i < spawnTransforms.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                Transform transform = (Transform)spawnTransforms.GetArrayElementAtIndex(i).objectReferenceValue;
                string transformName = transform != null ? transform.name : "None";
                EditorGUILayout.LabelField($"Transform {i}: {transformName}");
                
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    tool.RemoveSpawnTransform(i);
                    EditorUtility.SetDirty(tool);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif
