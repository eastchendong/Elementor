using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ScriptBoy.ProceduralBook
{
    [AddComponentMenu(" Script Boy/Procedural Book/  Demo/Auto Turning Demo")]
    public sealed class AutoTurningDemo : MonoBehaviour
    {
        [SerializeField] Book m_Book;
        [Space]
        [Tooltip("The settings for auto page turning when calling the AutoTurnNext, AutoTurnBack, and SingleAutoTurn methods.")]
        [SerializeField] AutoTurnSettings m_SingleTurnSettings;
        [Tooltip("The settings for auto page turning when calling the AutoTurnFirst, AutoTurnLast, and MultiAutoTurn methods.")]
        [SerializeField] AutoTurnSettings m_MultiTurnSettings;
        [Space]
        [Tooltip("The delay per turn when calling the AutoTurnFirst, AutoTurnLast, and MultiAutoTurn methods.")]
        [SerializeField, AutoTurnSettingRange(0, 5)] AutoTurnSetting m_DelyPerTurn = new AutoTurnSetting(0.5f);

        /// <summary>
        /// The target book.
        /// </summary>
        public Book book
        {
            get => m_Book;
            set => m_Book = value;
        }

        /// <summary>
        /// The settings for auto page turning when calling the AutoTurnNext, AutoTurnBack, and SingleAutoTurn methods.
        /// </summary>
        public AutoTurnSettings singleTurnSettings
        {
            get => m_SingleTurnSettings;
            set => m_SingleTurnSettings = value;
        }

        /// <summary>
        /// The settings for auto page turning when calling the AutoTurnFirst, AutoTurnLast, and MultiAutoTurn methods.
        /// </summary>
        public AutoTurnSettings multiTurnSettings
        {
            get => m_MultiTurnSettings;
            set => m_MultiTurnSettings = value;
        }

        /// <summary>
        /// The delay per turn when calling the AutoTurnFirst, AutoTurnLast, and MultiAutoTurn methods.
        /// </summary>
        public AutoTurnSetting delyPerTurn
        {
            get => m_DelyPerTurn;
            set => m_DelyPerTurn = value;
        }

        /// <summary>
        /// Automatically turns to the next page.
        /// </summary>
        public void AutoTurnNext() => SingleAutoTurn(AutoTurnDirection.Next);

        /// <summary>
        /// Automatically turns to the previous page.
        /// </summary>
        public void AutoTurnBack() => SingleAutoTurn(AutoTurnDirection.Back);

        /// <summary>
        /// Continuously auto turn pages until reaching the first page.
        /// </summary>
        public void AutoTurnFirst() => MultiAutoTurn(AutoTurnDirection.Back, int.MaxValue);

        /// <summary>
        /// Continuously auto turn pages until reaching the last page.
        /// </summary>
        public void AutoTurnLast() => MultiAutoTurn(AutoTurnDirection.Next, int.MaxValue);

        /// <summary>
        /// Automatically turns a single page. (Returns true if it does not fail.)
        /// </summary>
        public bool SingleAutoTurn(AutoTurnDirection direction)
        {
            if (m_Book == null) return false;

            return m_Book.StartAutoTurning(direction, m_SingleTurnSettings);
        }

        /// <summary>
        /// Automatically turns multiple pages. (Returns true if it does not fail.)
        /// </summary>
        public bool MultiAutoTurn(AutoTurnDirection direction, int turnCount)
        {
            if (m_Book == null) return false;

            return m_Book.StartAutoTurning(direction, m_MultiTurnSettings, turnCount, m_DelyPerTurn);
        }

        /// <summary>
        /// Cancels any pending auto turns that have not started yet.
        /// </summary>
        public void CancelPendingAutoTurns()
        {
            if (m_Book == null) return;

            m_Book.CancelPendingAutoTurns();
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(AutoTurningDemo))]
    class AutoTurningDemoEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            AutoTurningDemo demo = target as AutoTurningDemo;
            Book book = demo.book;

            using (new EditorGUI.DisabledGroupScope(!Application.isPlaying || book == null))
            {
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("<<")) demo.AutoTurnFirst();
                    if (GUILayout.Button("<")) demo.AutoTurnBack();
                    using (new EditorGUI.DisabledGroupScope(book == null || !book.hasPendingAutoTurns))
                    {
                        if (GUILayout.Button("||")) demo.CancelPendingAutoTurns();
                    }
                    if (GUILayout.Button(">")) demo.AutoTurnNext();
                    if (GUILayout.Button(">>")) demo.AutoTurnLast();
                }
            }

            if(Application.isPlaying && book != null) Repaint();
        }
    }
#endif
}