#nullable enable

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MasterDataDownloader
{
    [CustomPropertyDrawer(typeof(SheetEntry))]
    public sealed class SheetEntryDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            var sheetNameProp = property.FindPropertyRelative("_sheetName");
            var headerLabel = string.IsNullOrEmpty(sheetNameProp?.stringValue)
                ? "(Empty)"
                : sheetNameProp!.stringValue;

            var foldout = new Foldout { text = headerLabel };

            var sheetIdField = new PropertyField(property.FindPropertyRelative("_sheetId"), "Sheet ID");
            foldout.Add(sheetIdField);

            var sheetNameField = new PropertyField(sheetNameProp!, "Sheet Name");
            sheetNameField.RegisterValueChangeCallback(evt =>
            {
                var val = sheetNameProp?.stringValue;
                foldout.text = string.IsNullOrEmpty(val) ? "(Empty)" : val;
            });
            foldout.Add(sheetNameField);

            var outputPathField = new PropertyField(property.FindPropertyRelative("_outputPath"), "Output Path");
            foldout.Add(outputPathField);

            container.Add(foldout);
            return container;
        }
    }
}
