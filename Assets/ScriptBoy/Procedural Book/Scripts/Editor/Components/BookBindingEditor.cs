using UnityEditor;
using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    [CustomEditor(typeof(BookBinding), true)]
    class BookBindingEditor : Editor
    {
        void OnEnable()
        {
            PropertyUtility.FindProperties(this);

            Undo.undoRedoPerformed += UndoRedoPerformed;
            Undo.postprocessModifications += PostprocessModifications;

            if (target.name == "GameObject") target.name = ObjectNames.GetInspectorTitle(target).Replace(" Book ", " ");
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.postprocessModifications -= PostprocessModifications;
        }

        void UndoRedoPerformed()
        {
            Build();
        }

        UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            foreach (var m in modifications)
            {
                if (m.currentValue.target == target)
                {
                    EditorApplication.delayCall += Build;
                }
            }
            return modifications;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            base.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck())
            {
                Build();
            }
        }

        void Build()
        {
            foreach (var book in Book.instances)
            {
                if (book.binding == target) book.Build();
            }
        }
    }
}