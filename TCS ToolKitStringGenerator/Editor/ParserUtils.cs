using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TCS.UIToolKitUtils {
    internal static class ParserUtils {
        public static TextField CreateTextArea(string textContent)
            => new() { value = textContent, multiline = true, isReadOnly = true };

        public static string LoadAssetContent(Object asset) {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            return System.IO.File.ReadAllText(assetPath);
        }

        public static List<string> ExtractNamesFromUxml(VisualTreeAsset visualTreeAsset) {
            List<string> nameList = new();

            var root = new VisualElement();
            visualTreeAsset.CloneTree(root);

            TraverseVisualElementTree(root, nameList);

            return nameList;
        }

        static void TraverseVisualElementTree(VisualElement element, List<string> nameList) {
            if (!string.IsNullOrEmpty(element.name)) {
                nameList.Add(element.name);
            }

            foreach (var child in element.Children()) {
                TraverseVisualElementTree(child, nameList);
            }
        }

        public static List<string> ExtractClassNamesFromStyleSheet(StyleSheet styleSheet) {
            List<string> classNames = new();
            string styleSheetPath = AssetDatabase.GetAssetPath(styleSheet);
            string styleSheetContent = System.IO.File.ReadAllText(styleSheetPath);

            var regex = new Regex(@"[#.]([a-zA-Z0-9_-]+)\s*\{");
            var matches = regex.Matches(styleSheetContent);

            foreach (Match match in matches) {
                if (match.Groups.Count > 1) {
                    classNames.Add(match.Groups[1].Value);
                }
            }

            return classNames;
        }

        public static string ConvertToConstName(string name) {
            IEnumerable<string> words = Regex.Matches(name, @"[A-Z][a-z]*|[a-z]+|\d+")
                .Select(m => m.Value.ToUpper());
            return string.Join("_", words);
        }
    }
}