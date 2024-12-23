#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;   
using static TCS.ToolKitStringGenerator.ParserUtils;
using Object = UnityEngine.Object;

namespace TCS.ToolKitStringGenerator {
    public class ToolKitStringGeneratorWindow : EditorWindow, IDisposable {
        [MenuItem("Tools/Tent City Studio/UIToolKit String Generator")]
        static void OpenWindow() => GetWindow<ToolKitStringGeneratorWindow>().Show();

        StyleSheet m_styleSheet;
        VisualTreeAsset m_visualTreeAsset;
        int m_maxLength = 20;
        string m_namespace;
        string m_customFilePath;

        string m_styleSheetPreview;
        string m_uxmlPreview;
        ScrollView m_scrollView;
        
        ObjectField m_styleSheetField;
        ObjectField m_uxmlField;
        IntegerField m_maxLengthField;
        TextField m_namespaceField;
        TextField m_filePathField;

        string SafeNamespace => m_namespace.ConvertToAlphanumeric();
        const string DEFAULT_NAMESPACE = "UIToolKitStrings";

        void OnEnable() {
            InitializeComponents();
            CreateToolbar();
            CreateFields();
        }

        void OnDisable() {
            Dispose();
        }

        void InitializeComponents() {
            m_scrollView = new ScrollView();
            rootVisualElement.Add(m_scrollView);
        }

        void CreateToolbar() {
            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(() => ShowPreview("StyleSheet")) { text = "StyleSheet" });
            toolbar.Add(new ToolbarButton(() => ShowPreview("UXML")) { text = "UXML" });
            toolbar.Add(new ToolbarButton(HidePreviews) { text = "Hide Previews" });
            rootVisualElement.Add(toolbar);
        }

        void CreateFields() {
            // StyleSheet field
            m_styleSheetField = new ObjectField("StyleSheet") {
                objectType = typeof(StyleSheet),
                value = m_styleSheet,
            };
            m_styleSheetField.RegisterValueChangedCallback(evt =>
            {
                m_styleSheet = evt.newValue as StyleSheet;
                OnStyleSheetChanged(evt);
            });
            rootVisualElement.Add(m_styleSheetField);

            // VisualTreeAsset field
            m_uxmlField = new ObjectField("UXML File") {
                objectType = typeof(VisualTreeAsset),
                value = m_visualTreeAsset,
            };
            m_uxmlField.RegisterValueChangedCallback(evt =>
            {
                m_visualTreeAsset = evt.newValue as VisualTreeAsset;
                OnVisualTreeAssetChanged(evt);
            });
            rootVisualElement.Add(m_uxmlField);

            // Max Length field
            m_maxLengthField = new IntegerField("Max Length") {
                value = m_maxLength,
            };
            m_maxLengthField.RegisterValueChangedCallback(evt => m_maxLength = evt.newValue);
            rootVisualElement.Add(m_maxLengthField);

            // Namespace field
            m_namespaceField = new TextField("Namespace") {
                value = m_namespace,
            };
            m_namespaceField.RegisterValueChangedCallback(evt => m_namespace = evt.newValue);
            rootVisualElement.Add(m_namespaceField);

            // Custom File Path field with browse button
            var filePathContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                },
            };

            m_filePathField = new TextField("Custom File Path") {
                value = m_customFilePath,
            };
            m_filePathField.RegisterValueChangedCallback(evt => m_customFilePath = evt.newValue);
            m_filePathField.style.flexGrow = 1; // Allow the text field to expand
            filePathContainer.Add(m_filePathField);

            var browseButton = new Button(() => {
                string selectedFolder = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                if (string.IsNullOrEmpty(selectedFolder)) return;
                
                // Convert absolute path to relative path starting from Assets
                if (selectedFolder.StartsWith(Application.dataPath)) {
                    selectedFolder = "Assets" + selectedFolder.Substring(Application.dataPath.Length);
                } else {
                    EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder within the project's Assets folder.", "OK");
                    return;
                }
                m_customFilePath = selectedFolder;
                m_filePathField.value = m_customFilePath;
            }) {
                text = "...",
                style = {
                    width = 30,
                },
            };
            filePathContainer.Add(browseButton);

            rootVisualElement.Add(filePathContainer);

            // Button to generate static classes
            var generateButton = new Button(GenerateStaticClasses) {
                text = "Generate Static Classes",
            };
            rootVisualElement.Add(generateButton);
        }

        void HidePreviews() => m_scrollView.Clear();

        void ShowPreview(string previewType) {
            m_scrollView.Clear();

            switch (previewType) {
                case "StyleSheet":
                    if (m_styleSheet) {
                        var styleSheetTextArea = CreateTextArea(m_styleSheetPreview);
                        m_scrollView.Add(styleSheetTextArea);
                    }

                    break;

                case "UXML":
                    if (m_visualTreeAsset) {
                        var uxmlTextArea = CreateTextArea(m_uxmlPreview);
                        m_scrollView.Add(uxmlTextArea);
                    }

                    break;
            }
        }

        void OnStyleSheetChanged(ChangeEvent<Object> evt) {
            m_styleSheetPreview = m_styleSheet ? LoadAssetContent(m_styleSheet) : string.Empty;
            ShowFirstPreview();
        }

        void OnVisualTreeAssetChanged(ChangeEvent<Object> evt) {
            m_uxmlPreview = m_visualTreeAsset ? LoadAssetContent(m_visualTreeAsset) : string.Empty;
            ShowFirstPreview();
        }

        void ShowFirstPreview() {
            if (!string.IsNullOrEmpty(m_styleSheetPreview)) {
                ShowPreview("StyleSheet");
            }
            else if (!string.IsNullOrEmpty(m_uxmlPreview)) {
                ShowPreview("UXML");
            }
        }

        void GenerateStaticClasses() {
            if (!ValidateAssetsSelected()) return;

            string namespaceToUse = string.IsNullOrEmpty(SafeNamespace) ? DEFAULT_NAMESPACE : SafeNamespace;
            string filePath = string.IsNullOrEmpty(m_customFilePath) ? "Assets/UI Toolkit/StringLibrary/" : m_customFilePath;

            if (m_styleSheet) {
                GenerateStyleSheetClass(namespaceToUse, filePath);
            }

            if (m_visualTreeAsset) {
                GenerateUxmlClass(namespaceToUse, filePath);
            }

            EditorUtility.DisplayDialog("Success", "Static classes generated successfully!", "OK");
            AssetDatabase.Refresh();
        }

        bool ValidateAssetsSelected() {
            if (m_styleSheet || m_visualTreeAsset) return true;
            EditorUtility.DisplayDialog("Error", "No StyleSheet or UXML file selected!", "OK");
            return false;
        }

        void GenerateStyleSheetClass(string namespaceToUse, string filePath) {
            string className = m_styleSheet.name.EndsWith("SS")
                ? $"{m_styleSheet.name[..^2]}Classes"
                : $"{m_styleSheet.name}Classes";
            List<string> classNames = ExtractClassNamesFromStyleSheet(m_styleSheet)
                .Distinct()
                .ToList();
            
            Dictionary<string, string> nameValuePairs = new();
            foreach (string originalName in classNames) {
                string variableName = ConvertToConstName(originalName);
                
                var index = 1;
                string uniqueVariableName = variableName;
                while (nameValuePairs.ContainsKey(uniqueVariableName)) {
                    uniqueVariableName = $"{variableName}_{index}";
                    index++;
                }

                nameValuePairs[uniqueVariableName] = originalName;
            }

            GeneratorUtils.SaveToFile(namespaceToUse, className, nameValuePairs, filePath, m_maxLength);
        }

        void GenerateUxmlClass(string namespaceToUse, string filePath) {
            var className = $"{m_visualTreeAsset.name}Strings";
            List<string> nameVariables = ExtractNamesFromUxml(m_visualTreeAsset)
                .Distinct()
                .ToList();

            // Create a dictionary mapping variable names to original string values
            Dictionary<string, string> nameValuePairs = new();
            foreach (string originalName in nameVariables) {
                string variableName = ConvertToConstName(originalName);

                // Ensure the variable name is unique
                var index = 1;
                string uniqueVariableName = variableName;
                while (nameValuePairs.ContainsKey(uniqueVariableName)) {
                    uniqueVariableName = $"{variableName}_{index}";
                    index++;
                }

                nameValuePairs[uniqueVariableName] = originalName;
            }

            GeneratorUtils.SaveToFile(namespaceToUse, className, nameValuePairs, filePath, m_maxLength);
        }

        public void Dispose() {
            m_styleSheetField.UnregisterValueChangedCallback(OnStyleSheetChanged);
            m_uxmlField.UnregisterValueChangedCallback(OnVisualTreeAssetChanged);
            m_maxLengthField.UnregisterValueChangedCallback(evt => m_maxLength = evt.newValue);
            m_namespaceField.UnregisterValueChangedCallback(evt => m_namespace = evt.newValue);
            m_filePathField.UnregisterValueChangedCallback(evt => m_customFilePath = evt.newValue);
            
            m_styleSheet = null;
            m_visualTreeAsset = null;
            m_scrollView = null;
            
            rootVisualElement.Clear();
            
            Close();
        }
    }
}

  #endif