using UnityEngine;
using UnityEditor;

namespace UnityAIStudio.McpServer.UI
{
    /// <summary>
    /// Centralized UI styles for MCP Server window
    /// </summary>
    public static class McpUIStyles
    {
        private static bool initialized = false;

        // Header styles
        public static GUIStyle WindowHeaderStyle { get; private set; }
        public static GUIStyle SectionHeaderStyle { get; private set; }

        // Card styles
        public static GUIStyle CardStyle { get; private set; }
        public static GUIStyle StatusBoxStyle { get; private set; }

        // Status styles
        public static GUIStyle RunningStatusStyle { get; private set; }
        public static GUIStyle StoppedStatusStyle { get; private set; }
        public static GUIStyle ErrorStatusStyle { get; private set; }
        public static GUIStyle WarningStatusStyle { get; private set; }

        // Button styles
        public static GUIStyle ButtonStyle { get; private set; }
        public static GUIStyle StartButtonStyle { get; private set; }
        public static GUIStyle StopButtonStyle { get; private set; }
        public static GUIStyle RestartButtonStyle { get; private set; }

        // Text styles
        public static GUIStyle LogMessageStyle { get; private set; }
        public static GUIStyle BoldLabelStyle { get; private set; }

        // Foldout styles
        public static GUIStyle FoldoutStyle { get; private set; }

        // Colors
        public static readonly Color SuccessColor = new Color(0.3f, 0.9f, 0.3f);
        public static readonly Color ErrorColor = new Color(1f, 0.4f, 0.4f);
        public static readonly Color WarningColor = new Color(0.9f, 0.7f, 0.2f);
        public static readonly Color InfoColor = new Color(0.7f, 0.85f, 1f);
        public static readonly Color NeutralColor = new Color(0.6f, 0.6f, 0.6f);

        public static void Initialize()
        {
            if (initialized) return;

            InitializeHeaderStyles();
            InitializeCardStyles();
            InitializeStatusStyles();
            InitializeButtonStyles();
            InitializeTextStyles();
            InitializeFoldoutStyles();

            initialized = true;
        }

        private static void InitializeHeaderStyles()
        {
            WindowHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 15, 15),
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };

            SectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(5, 0, 8, 5),
                normal = { textColor = InfoColor }
            };
        }

        private static void InitializeCardStyles()
        {
            StatusBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(15, 15, 12, 12),
                margin = new RectOffset(8, 8, 5, 5)
            };

            CardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(5, 5, 3, 3)
            };
        }

        private static void InitializeStatusStyles()
        {
            RunningStatusStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = SuccessColor },
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            StoppedStatusStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = NeutralColor },
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            ErrorStatusStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = ErrorColor },
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            WarningStatusStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = WarningColor },
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
        }

        private static void InitializeButtonStyles()
        {
            ButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 8, 8)
            };

            StartButtonStyle = new GUIStyle(ButtonStyle);
            StopButtonStyle = new GUIStyle(ButtonStyle);
            RestartButtonStyle = new GUIStyle(ButtonStyle);
        }

        private static void InitializeTextStyles()
        {
            LogMessageStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 11,
                padding = new RectOffset(5, 5, 2, 2)
            };

            BoldLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11
            };
        }

        private static void InitializeFoldoutStyles()
        {
            FoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = InfoColor }
            };
        }

        public static void DrawSeparator(float width, float height = 2)
        {
            Rect separatorRect = GUILayoutUtility.GetRect(width, height);
            EditorGUI.DrawRect(separatorRect, new Color(0.3f, 0.5f, 0.8f, 0.8f));
        }

        public static void DrawHeader(string title, Rect position)
        {
            // Header background
            Rect headerRect = new Rect(0, 0, position.width, 50);
            EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.15f, 0.15f, 1f));

            // Separator line
            Rect separatorRect = new Rect(0, 50, position.width, 2);
            EditorGUI.DrawRect(separatorRect, new Color(0.3f, 0.5f, 0.8f, 0.8f));
        }
    }
}
