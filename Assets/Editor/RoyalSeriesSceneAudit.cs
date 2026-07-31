using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh.Editor
{
    public static class RoyalSeriesSceneAudit
    {
        [MenuItem("Tools/CardMesh/Audit Royal Series Scene UI")]
        public static void DumpHierarchy()
        {
            EditorSceneManager.OpenScene("Assets/MainMenu.unity", OpenSceneMode.Single);
            StringBuilder report = new();
            Dump<DailyRewardsPopup>("DAILY REWARDS", report);
            Dump<ChestChasePopup>("ROYAL SERIES", report);
            Debug.Log(report.ToString());
        }

        private static void Dump<T>(string heading, StringBuilder report) where T : Component
        {
            T root = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            report.AppendLine($"===== {heading} =====");
            if (root == null)
            {
                report.AppendLine("MISSING");
                return;
            }

            DumpTransform(root.transform, root.transform, report, 0);
        }

        private static void DumpTransform(Transform root, Transform current, StringBuilder report, int depth)
        {
            RectTransform rect = current as RectTransform;
            string path = current == root ? current.name : AnimationUtility.CalculateTransformPath(current, root);
            report.Append(' ', depth * 2).Append(path);
            if (rect != null)
            {
                report.Append($" | pos={rect.anchoredPosition} size={rect.sizeDelta} anchors={rect.anchorMin}->{rect.anchorMax} scale={rect.localScale}");
            }

            Image image = current.GetComponent<Image>();
            if (image != null)
            {
                report.Append($" | Image={AssetDatabase.GetAssetPath(image.sprite)} material={AssetDatabase.GetAssetPath(image.material)} type={image.type} raycast={image.raycastTarget}");
            }

            TMP_Text text = current.GetComponent<TMP_Text>();
            if (text != null)
            {
                report.Append($" | TMP=\"{text.text.Replace("\n", " / ")}\" font={AssetDatabase.GetAssetPath(text.font)} size={text.fontSize}");
            }

            report.AppendLine();
            foreach (Transform child in current)
            {
                DumpTransform(root, child, report, depth + 1);
            }
        }
    }
}
