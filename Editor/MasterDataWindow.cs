#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MasterDataDownloader
{
    public sealed class MasterDataWindow : EditorWindow
    {
        private SheetRegistry? _registry;
        private SerializedObject? _serializedRegistry;
        private CancellationTokenSource? _cts;

        private VisualElement _entryContainer = null!;
        private VisualElement _toolbarContainer = null!;
        private HelpBox _helpBox = null!;

        [MenuItem("Tools/Master Data Downloader")]
        public static void Open()
        {
            GetWindow<MasterDataWindow>("Master Data Downloader");
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingTop = 4;
            root.style.paddingBottom = 4;
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;

            // Registry selector row
            var selectorRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var registryField = new ObjectField("Registry")
            {
                objectType = typeof(SheetRegistry),
                style = { flexGrow = 1 }
            };
            registryField.RegisterValueChangedCallback(evt => BindRegistry(evt.newValue as SheetRegistry));
            selectorRow.Add(registryField);

            var createButton = new Button(CreateRegistryAsset) { text = "New", style = { width = 48 } };
            selectorRow.Add(createButton);

            root.Add(selectorRow);

            _helpBox = new HelpBox("Select a SheetRegistry or create one with New.", HelpBoxMessageType.Info);
            root.Add(_helpBox);

            // Toolbar
            _toolbarContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = 4,
                    marginBottom = 4,
                    display = DisplayStyle.None
                }
            };

            var downloadAllButton = new Button(() => DownloadAllAsync().Forget()) { text = "Download All", style = { flexGrow = 1 } };
            var hookAllButton = new Button(ExecuteAllHooks) { text = "Run All Hooks", style = { flexGrow = 1 } };
            var addEntryButton = new Button(AddEntry) { text = "+", tooltip = "Add Entry", style = { width = 32 } };

            _toolbarContainer.Add(downloadAllButton);
            _toolbarContainer.Add(hookAllButton);
            _toolbarContainer.Add(addEntryButton);
            root.Add(_toolbarContainer);

            // Entry list
            var scrollView = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
            _entryContainer = new VisualElement();
            scrollView.Add(_entryContainer);
            root.Add(scrollView);

            if (_registry != null)
            {
                registryField.value = _registry;
                BindRegistry(_registry);
            }
        }

        private void BindRegistry(SheetRegistry? registry)
        {
            _registry = registry;
            _serializedRegistry = registry != null ? new SerializedObject(registry) : null;
            RebuildEntryList();
        }

        private void RebuildEntryList()
        {
            _entryContainer.Clear();

            if (_serializedRegistry == null)
            {
                _helpBox.style.display = DisplayStyle.Flex;
                _toolbarContainer.style.display = DisplayStyle.None;
                return;
            }

            _helpBox.style.display = DisplayStyle.None;
            _toolbarContainer.style.display = DisplayStyle.Flex;
            _serializedRegistry.Update();

            var entriesProp = _serializedRegistry.FindProperty("_entries");
            if (entriesProp == null) return;

            for (var i = 0; i < entriesProp.arraySize; i++)
            {
                var index = i;
                var entryProp = entriesProp.GetArrayElementAtIndex(i);
                _entryContainer.Add(BuildEntryElement(entryProp, index));
            }
        }

        private VisualElement BuildEntryElement(SerializedProperty entryProp, int index)
        {
            var container = new VisualElement
            {
                style =
                {
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.2f, 0.2f, 0.2f, 0.5f),
                    paddingTop = 4,
                    paddingBottom = 6,
                    paddingLeft = 4,
                    paddingRight = 4,
                    marginBottom = 2
                }
            };

            // Header row
            var sheetNameProp = entryProp.FindPropertyRelative("_sheetName");
            var headerLabel = string.IsNullOrEmpty(sheetNameProp?.stringValue) ? "(Empty)" : sheetNameProp!.stringValue;

            var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };
            var headerFoldout = new Foldout { text = headerLabel, value = true, style = { flexGrow = 1 } };

            var removeButton = new Button(() => RemoveEntry(index))
            {
                text = "×",
                tooltip = "Remove Entry",
                style = { width = 24, height = 20, alignSelf = Align.FlexStart }
            };
            headerRow.Add(headerFoldout);
            headerRow.Add(removeButton);
            container.Add(headerRow);

            // Fields
            var fieldsContainer = new VisualElement { style = { paddingLeft = 16 } };

            var sheetIdField = new TextField("Sheet ID");
            sheetIdField.BindProperty(entryProp.FindPropertyRelative("_sheetId"));
            fieldsContainer.Add(sheetIdField);

            var sheetNameField = new TextField("Sheet Name");
            sheetNameField.BindProperty(sheetNameProp!);
            sheetNameField.RegisterValueChangedCallback(evt =>
            {
                headerFoldout.text = string.IsNullOrEmpty(evt.newValue) ? "(Empty)" : evt.newValue;
            });
            fieldsContainer.Add(sheetNameField);

            // Output path row with folder picker
            var outputPathRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var outputPathField = new TextField("Output Path") { style = { flexGrow = 1 } };
            outputPathField.BindProperty(entryProp.FindPropertyRelative("_outputPath"));
            outputPathRow.Add(outputPathField);

            var browseButton = new Button(() =>
            {
                var folder = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (string.IsNullOrEmpty(folder)) return;

                var assetsIndex = folder.IndexOf("Assets", StringComparison.Ordinal);
                if (assetsIndex < 0) return;

                var relativePath = folder[assetsIndex..];
                var name = string.IsNullOrEmpty(sheetNameProp?.stringValue) ? "output" : sheetNameProp!.stringValue;
                outputPathField.value = $"{relativePath}/{name}.csv";
            })
            {
                text = "...",
                style = { width = 32 }
            };
            outputPathRow.Add(browseButton);
            fieldsContainer.Add(outputPathRow);

            // PostProcessor info
            var outputPath = entryProp.FindPropertyRelative("_outputPath")?.stringValue ?? "";
            var processors = CsvPostProcessorRegistry.GetForPath(outputPath);
            var ppLabel = processors.Count > 0
                ? string.Join(", ", System.Linq.Enumerable.Select(processors, p => p.DisplayName))
                : "(None)";
            fieldsContainer.Add(new Label($"PostProcessor: {ppLabel}")
            {
                style = { color = new Color(0.6f, 0.6f, 0.6f), marginTop = 2, marginBottom = 2 }
            });

            // Action buttons
            var actionsRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 2 } };
            actionsRow.Add(new Button(() =>
            {
                if (_registry == null) return;
                var entry = _registry.Entries[index];
                DownloadSingleAsync(entry).Forget();
            })
            { text = "Download", style = { flexGrow = 1 } });

            actionsRow.Add(new Button(() =>
            {
                if (_registry == null) return;
                CsvPostProcessorRegistry.ExecuteForPath(_registry.Entries[index].OutputPath);
            })
            { text = "Run Hooks", style = { flexGrow = 1 } });

            fieldsContainer.Add(actionsRow);

            headerFoldout.Add(fieldsContainer);

            // Foldout controls visibility
            headerFoldout.RegisterValueChangedCallback(evt =>
            {
                fieldsContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });

            return container;
        }

        private void CreateRegistryAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create SheetRegistry", "SheetRegistry", "asset",
                "Select a location to save the SheetRegistry asset.");

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<SheetRegistry>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            BindRegistry(asset);

            // Update ObjectField
            var objectField = rootVisualElement.Q<ObjectField>();
            if (objectField != null) objectField.value = asset;
        }

        private void AddEntry()
        {
            if (_serializedRegistry == null) return;

            _serializedRegistry.Update();
            var entriesProp = _serializedRegistry.FindProperty("_entries");
            entriesProp.InsertArrayElementAtIndex(entriesProp.arraySize);
            _serializedRegistry.ApplyModifiedProperties();
            RebuildEntryList();
        }

        private void RemoveEntry(int index)
        {
            if (_serializedRegistry == null) return;

            _serializedRegistry.Update();
            var entriesProp = _serializedRegistry.FindProperty("_entries");
            entriesProp.DeleteArrayElementAtIndex(index);
            _serializedRegistry.ApplyModifiedProperties();
            RebuildEntryList();
        }

        private void ResetCts()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        private async UniTaskVoid DownloadSingleAsync(SheetEntry entry)
        {
            ResetCts();

            try
            {
                EditorUtility.DisplayProgressBar("Master Data Downloader",
                    $"Downloading: {entry.SheetName}", 0f);

                await CsvDownloader.DownloadAsync(entry, _cts.Token);
                Debug.Log($"Downloaded: {entry.SheetName} → {entry.OutputPath}");
                CsvPostProcessorRegistry.ExecuteForPath(entry.OutputPath);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Download cancelled.");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private async UniTaskVoid DownloadAllAsync()
        {
            if (_registry == null) return;

            ResetCts();

            try
            {
                var progress = new Progress<(int Current, int Total, string SheetName)>(p =>
                {
                    EditorUtility.DisplayProgressBar("Master Data Downloader",
                        $"Downloading: {p.SheetName} ({p.Current}/{p.Total})",
                        (float)p.Current / p.Total);
                });

                await CsvDownloader.DownloadAllAsync(_registry, progress, _cts.Token);

                foreach (var entry in _registry.Entries)
                    CsvPostProcessorRegistry.ExecuteForPath(entry.OutputPath);

                Debug.Log("All downloads completed.");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Download cancelled.");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ExecuteAllHooks()
        {
            if (_registry == null) return;

            foreach (var entry in _registry.Entries)
                CsvPostProcessorRegistry.ExecuteForPath(entry.OutputPath);

            Debug.Log("All hooks executed.");
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
