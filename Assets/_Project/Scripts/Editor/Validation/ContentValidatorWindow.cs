using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CrateExpectations.EditorTools.Validation
{
    public sealed class ContentValidatorWindow : EditorWindow
    {
        private readonly ContentValidator _validator = new();

        private List<ContentIssue> _issues = new();
        private Vector2 _scroll;
        private bool _showWarnings = true;
        private bool _hasRun;

        [MenuItem("Window/Crate Expectations/Валидатор контента")]
        public static void Open()
        {
            var window = GetWindow<ContentValidatorWindow>();
            window.titleContent = new GUIContent("Валидатор контента");
            window.minSize = new Vector2(520f, 300f);
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!_hasRun)
            {
                EditorGUILayout.HelpBox(
                    "Проверяет контент проекта: выполнимы ли заказы, хватает ли груза на доке, " +
                    "не требует ли регламент того, чего не наносит ни одна станция, " +
                    "все ли ссылки на месте",
                    MessageType.Info);
                return;
            }

            DrawSummary();
            DrawIssues();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Проверить всё", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                    RunValidation();

                GUILayout.FlexibleSpace();

                _showWarnings = GUILayout.Toggle(
                    _showWarnings, "Показывать предупреждения", EditorStyles.toolbarButton);
            }
        }

        private void RunValidation()
        {
            _issues = new List<ContentIssue>(_validator.Validate(ProjectContentScanner.Scan()));
            _hasRun = true;
        }

        private void DrawSummary()
        {
            int errors = 0;
            int warnings = 0;

            for (int i = 0; i < _issues.Count; i++)
                if (_issues[i].IsError) 
                    errors++;
                else 
                    warnings++;

            if (errors == 0 && warnings == 0)
            {
                EditorGUILayout.HelpBox(
                    $"Проблем не найдено. Проверок пройдено: {_validator.Checks.Count}",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                $"Ошибок: {errors}   предупреждений: {warnings}.   " +
                "Клик по записи - переход к ассету",
                errors > 0 ? MessageType.Error : MessageType.Warning);
        }

        private void DrawIssues()
        {
            using var scope = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scope.scrollPosition;

            for (int i = 0; i < _issues.Count; i++)
            {
                ContentIssue issue = _issues[i];

                if (!issue.IsError && !_showWarnings) 
                    continue;

                DrawIssue(issue);
            }
        }

        private static void DrawIssue(in ContentIssue issue)
        {
            using var row = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    EditorGUIUtility.IconContent(issue.IsError ? "console.erroricon.sml" : "console.warnicon.sml"),
                    GUILayout.Width(20f));

                GUILayout.Label(issue.Source, EditorStyles.boldLabel, GUILayout.Width(160f));

                if (issue.Asset != null)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(issue.Asset.name, EditorStyles.miniLabel);
                }
            }

            GUILayout.Label(issue.Message, EditorStyles.wordWrappedLabel);

            if (issue.Asset == null)
                return;

            Rect area = row.rect;
            EditorGUIUtility.AddCursorRect(area, MouseCursor.Link);

            if (Event.current.type != EventType.MouseDown || !area.Contains(Event.current.mousePosition))
                return;

            Selection.activeObject = issue.Asset;
            EditorGUIUtility.PingObject(issue.Asset);
            Event.current.Use();
        }
    }
}
