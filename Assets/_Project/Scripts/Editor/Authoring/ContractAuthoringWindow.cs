using System.Collections.Generic;
using CrateExpectations.Cargo;
using CrateExpectations.Contracts;
using CrateExpectations.Economy;
using CrateExpectations.EditorTools.Validation;
using UnityEditor;
using UnityEngine;

namespace CrateExpectations.EditorTools.Authoring
{
    public sealed class ContractAuthoringWindow : EditorWindow
    {
        private const string ContractsFolder = "Assets/_Project/Data/Contracts";

        private static readonly GUIContent NoDisguise = new(
            "- везём как есть -", "Заказ не требует маскировки: заявляем то, что внутри");

        private readonly PayoutCalculator _calculator = new();

        private List<ContractDefinition> _contracts = new();
        private List<CargoTypeDefinition> _cargoTypes = new();
        private List<ContractCatalogDefinition> _boards = new();

        private GUIContent[] _cargoNames = System.Array.Empty<GUIContent>();

        private ContractDefinition _selected;
        private SerializedObject _serialized;

        private Vector2 _listScroll;
        private Vector2 _formScroll;
        private string _renameTo = string.Empty;
        private int _boardIndex;

        [MenuItem("Window/Crate Expectations/Заказы порта")]
        public static void Open()
        {
            var window = GetWindow<ContractAuthoringWindow>();
            window.titleContent = new GUIContent("Заказы порта");
            window.minSize = new Vector2(720f, 420f);
        }

        private void OnEnable() => Reload();

        private void OnFocus() => Reload();

        private void Reload()
        {
            _contracts = ProjectContentScanner.FindAll<ContractDefinition>();
            _cargoTypes = ProjectContentScanner.FindAll<CargoTypeDefinition>();
            _boards = ProjectContentScanner.FindAll<ContractCatalogDefinition>();

            _cargoNames = new GUIContent[_cargoTypes.Count];
            for (int i = 0; i < _cargoTypes.Count; i++)
                _cargoNames[i] = new GUIContent(_cargoTypes[i].DisplayName, _cargoTypes[i].name);

            if (_selected == null || !_contracts.Contains(_selected)) 
                Select(_contracts.Count > 0 ? _contracts[0] : null);
            else if (_serialized == null) 
                Select(_selected);
        }

        private void Select(ContractDefinition contract)
        {
            _selected = contract;
            _serialized = contract != null ? new SerializedObject(contract) : null;
            _renameTo = contract != null ? contract.name : string.Empty;
            _boardIndex = 0;
        }

        private void OnGUI()
        {
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList();
                DrawForm();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Новый заказ", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                    CreateContract(source: null);

                using (new EditorGUI.DisabledScope(_selected == null))
                {
                    if (GUILayout.Button("Дублировать", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                        CreateContract(_selected);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Обновить список", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                    Reload();
            }
        }

        private void DrawList()
        {
            using var pane = new EditorGUILayout.VerticalScope(
                GUILayout.Width(210f), GUILayout.ExpandHeight(true));

            GUILayout.Label($"Заказы в проекте ({_contracts.Count})", EditorStyles.boldLabel);

            using var scope = new EditorGUILayout.ScrollViewScope(_listScroll);
            _listScroll = scope.scrollPosition;

            for (int i = 0; i < _contracts.Count; i++)
            {
                ContractDefinition contract = _contracts[i];

                if (contract == null) 
                    continue;

                bool isSelected = contract == _selected;

                if (GUILayout.Toggle(isSelected, Label(contract), EditorStyles.miniButton) == isSelected)
                    continue;

                Select(contract);
                GUIUtility.keyboardControl = 0;
            }
        }

        private static string Label(ContractDefinition contract) =>
            contract.Cargo != null
                ? $"{contract.DisplayName}  ({contract.Crates}×{contract.Cargo.DisplayName})"
                : $"{contract.DisplayName}  (груз не задан)";

        private void DrawForm()
        {
            using var pane = new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true));

            if (_serialized == null)
            {
                EditorGUILayout.HelpBox(
                    "Выберите заказ слева или создайте новый - ассет ляжет в " +
                    ContractsFolder + " и появится в списке.",
                    MessageType.Info);
                return;
            }

            using var scope = new EditorGUILayout.ScrollViewScope(_formScroll);
            _formScroll = scope.scrollPosition;

            _serialized.Update();

            DrawAssetRow();
            DrawDescription();
            DrawCargo();
            DrawTerms();

            _serialized.ApplyModifiedProperties();

            DrawPayoutPreview();
            DrawBoardRow();
        }

        private void DrawAssetRow()
        {
            GUILayout.Label("Ассет", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _renameTo = EditorGUILayout.TextField("Имя файла", _renameTo);

                using (new EditorGUI.DisabledScope(
                           string.IsNullOrWhiteSpace(_renameTo) || _renameTo == _selected.name))
                {
                    if (GUILayout.Button("Переименовать", GUILayout.Width(120f))) 
                        Rename();
                }
            }

            EditorGUILayout.LabelField(" ", AssetDatabase.GetAssetPath(_selected), EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);
        }

        private void DrawDescription()
        {
            GUILayout.Label("Доска", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(Property("DisplayName"), new GUIContent("Название"));

            SerializedProperty description = Property("Description");
            EditorGUILayout.LabelField("Описание");
            description.stringValue = EditorGUILayout.TextArea(
                description.stringValue, GUILayout.Height(52f));

            EditorGUILayout.Space(4f);
        }

        private void DrawCargo()
        {
            GUILayout.Label("Груз", EditorStyles.boldLabel);

            if (_cargoTypes.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "В проекте нет ни одного типа груза, заказ будет не о чем", MessageType.Error);
                return;
            }

            CargoPopup(
                Property("Cargo"),
                new GUIContent("Что внутри", "Истинный тип груза: по нему сдача засчитывается в заказ"),
                allowNone: false);

            CargoPopup(
                Property("DeclaredAs"),
                new GUIContent("Выдать за", "Под что маскируем. Пусто - заказ не требует маскировки"),
                allowNone: true);

            EditorGUILayout.IntSlider(Property("Crates"), 1, 10, new GUIContent("Ящиков"));
            EditorGUILayout.IntSlider(
                Property("AllowedSeizures"), 0, 5,
                new GUIContent("Прощаемых изъятий", "Больше - заказ считается проваленным"));

            EditorGUILayout.Space(4f);
        }

        private void CargoPopup(SerializedProperty property, GUIContent label, bool allowNone)
        {
            var current = property.objectReferenceValue as CargoTypeDefinition;

            int offset = allowNone ? 1 : 0;
            var options = new GUIContent[_cargoNames.Length + offset];

            if (allowNone) 
                options[0] = NoDisguise;

            _cargoNames.CopyTo(options, offset);

            int index = _cargoTypes.IndexOf(current);
            index = index >= 0 ? index + offset : 0;

            int picked = EditorGUILayout.Popup(label, index, options);

            if (picked == index) 
                return;

            property.objectReferenceValue = allowNone && picked == 0
                ? null
                : _cargoTypes[picked - offset];
        }

        private void DrawTerms()
        {
            GUILayout.Label("Деньги", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                Property("RewardPerCrate"), new GUIContent("Награда за ящик"));
            EditorGUILayout.PropertyField(
                Property("CleanBonus"),
                new GUIContent("Бонус за чистую сдачу", "Платится, если инспектор не нашёл ни одной улики"));
            EditorGUILayout.PropertyField(
                Property("Penalty"),
                new GUIContent("Штраф за изъятие", "Положительное число: знак расставит расчёт"));

            EditorGUILayout.Space(4f);
        }

        private void DrawPayoutPreview()
        {
            var terms = new PayoutTerms(
                Property("RewardPerCrate").intValue,
                Property("Penalty").intValue,
                Property("CleanBonus").intValue);

            PayoutResult plain = _calculator.Calculate(terms, new DeliveryReport(DeliveryOutcome.Cleared));
            PayoutResult clean = _calculator.Calculate(
                terms, new DeliveryReport(DeliveryOutcome.Cleared, spotless: true));
            PayoutResult seized = _calculator.Calculate(terms, new DeliveryReport(DeliveryOutcome.Seized));

            int crates = Property("Crates").intValue;
            int allowed = Property("AllowedSeizures").intValue;

            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

            GUILayout.Label("Сколько это стоит (расчёт игры)", EditorStyles.boldLabel);

            Row("Сдал ящик, к чему-то придрались", plain.Amount);
            Row("Сдал ящик безупречно", clean.Amount);
            Row("Ящик изъяли", seized.Amount);

            EditorGUILayout.Space(2f);

            Row($"Заказ целиком, безупречно - ящиков: {crates}", clean.Amount * crates);
            Row($"Провал заказа - изъятий: {allowed + 1}", seized.Amount * (allowed + 1));
        }

        private static void Row(string label, int amount)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label);
                GUILayout.FlexibleSpace();

                Color previous = GUI.contentColor;
                GUI.contentColor = amount < 0 ? new Color(1f, 0.45f, 0.4f) : new Color(0.5f, 0.9f, 0.55f);
                GUILayout.Label($"{amount:+#;-#;0}", EditorStyles.boldLabel, GUILayout.Width(70f));
                GUI.contentColor = previous;
            }
        }

        private void DrawBoardRow()
        {
            if (_boards.Count == 0)
                return;

            EditorGUILayout.Space(4f);
            GUILayout.Label("Доска заказов", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                var names = new string[_boards.Count];
                for (int i = 0; i < _boards.Count; i++) names[i] = _boards[i].name;

                _boardIndex = Mathf.Clamp(_boardIndex, 0, _boards.Count - 1);
                _boardIndex = EditorGUILayout.Popup("Каталог", _boardIndex, names);

                ContractCatalogDefinition board = _boards[_boardIndex];
                bool already = IsOnBoard(board, _selected);

                using (new EditorGUI.DisabledScope(already))
                {
                    if (GUILayout.Button(already ? "Уже на доске" : "Добавить на доску", GUILayout.Width(150f)))
                        AddToBoard(board);
                }
            }
        }

        private static bool IsOnBoard(ContractCatalogDefinition board, ContractDefinition contract)
        {
            IReadOnlyList<ContractDefinition> contracts = board.Contracts;

            for (int i = 0; i < contracts.Count; i++)
            {
                if (contracts[i] == contract) 
                    return true;
            }

            return false;
        }

        private void AddToBoard(ContractCatalogDefinition board)
        {
            var serialized = new SerializedObject(board);
            SerializedProperty list = serialized.FindProperty("_contracts");

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = _selected;

            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssetIfDirty(board);
        }

        private void CreateContract(ContractDefinition source)
        {
            if (!AssetDatabase.IsValidFolder(ContractsFolder))
            {
                EditorUtility.DisplayDialog(
                    "Некуда класть заказ",
                    $"Папки {ContractsFolder} нет. Создайте её и повторите.",
                    "Понятно");
                return;
            }

            ContractDefinition contract = source != null
                ? Instantiate(source)
                : CreateInstance<ContractDefinition>();

            string baseName = source != null ? $"{source.name}_Copy" : "Contract_New";
            string path = AssetDatabase.GenerateUniqueAssetPath($"{ContractsFolder}/{baseName}.asset");

            AssetDatabase.CreateAsset(contract, path);
            AssetDatabase.SaveAssets();

            Reload();
            Select(contract);
            EditorGUIUtility.PingObject(contract);
        }

        private void Rename()
        {
            string error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(_selected), _renameTo);

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"Переименовать не вышло: {error}");
                return;
            }

            AssetDatabase.SaveAssets();
            Reload();
        }

        private SerializedProperty Property(string propertyName) =>
            _serialized.FindProperty($"<{propertyName}>k__BackingField");
    }
}
