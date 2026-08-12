using System;
using CardMesh;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh.EditorTools
{
    public static class MobileLayoutBatchAudit
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";

        [MenuItem("Tools/Card Mesh/Mobile/Audit All Column Layouts")]
        public static void BatchAuditAllColumnLayouts()
        {
            EditorSceneManager.OpenScene(MainScenePath);
            CardMeshSceneRefs refs =
                UnityEngine.Object.FindFirstObjectByType<CardMeshSceneRefs>(
                    FindObjectsInactive.Include);
            if (refs == null)
            {
                throw new InvalidOperationException(
                    "Main scene is missing CardMeshSceneRefs.");
            }

            for (int columns = 3; columns <= 5; columns++)
            {
                AuditProfile(refs, columns);
            }

            refs.SetEditorPreviewColumns(3);
            EditorUtility.SetDirty(refs);
            EditorSceneManager.MarkSceneDirty(refs.gameObject.scene);
            EditorSceneManager.SaveScene(refs.gameObject.scene);
            Debug.Log("Mobile layout audit passed for 3, 4 and 5 columns.");
        }

        private static void AuditProfile(CardMeshSceneRefs refs, int columns)
        {
            refs.SetEditorPreviewColumns(columns);
            LayoutRebuilder.ForceRebuildLayoutImmediate(refs.ColumnSlotsRoot);
            ColumnLayoutProfile profile =
                GameManager.GetGameplayLayoutProfile(columns);

            HorizontalLayoutGroup group =
                refs.ColumnSlotsRoot.GetComponent<HorizontalLayoutGroup>();
            Require(group != null, "ColumnSlotsRoot layout group is missing.");
            Require(
                Mathf.Approximately(group.spacing, profile.columnSpacing),
                $"{columns} columns spacing is {group.spacing}, expected "
                + $"{profile.columnSpacing}.");
            int expectedTopPadding = columns switch
            {
                4 => 36,
                5 => 76,
                _ => 0
            };
            Require(
                group.padding.top == expectedTopPadding,
                $"{columns} columns top padding is {group.padding.top}, "
                + $"expected {expectedTopPadding}.");

            int activeColumns = 0;
            foreach (ColumnRef column in refs.Columns)
            {
                if (column?.Root == null || !column.Root.gameObject.activeSelf)
                {
                    continue;
                }

                activeColumns++;
                RectTransform crown = column.CrownSlot as RectTransform;
                RectTransform stack = column.CardsStackRoot as RectTransform;
                Require(crown != null && stack != null,
                    $"{columns} columns has an incomplete column hierarchy.");
                Require(
                    Approximately(crown.sizeDelta, profile.cardSize),
                    $"{columns} columns crown size {crown.sizeDelta}, expected "
                    + $"{profile.cardSize}.");

                Vector2 expectedStackSize = new(
                    profile.cardSize.x,
                    profile.cardSize.y + profile.verticalOffset * 5f);
                Require(
                    Approximately(stack.sizeDelta, expectedStackSize),
                    $"{columns} columns stack size {stack.sizeDelta}, expected "
                    + $"{expectedStackSize}.");

                float expectedStackY =
                    crown.anchoredPosition.y
                    - profile.cardSize.y * 0.5f
                    - 30f;
                Require(
                    Mathf.Abs(stack.anchoredPosition.y - expectedStackY) < 0.1f,
                    $"{columns} columns stack y {stack.anchoredPosition.y}, "
                    + $"expected {expectedStackY}.");
            }

            Require(
                activeColumns == columns,
                $"{columns} columns preview activated {activeColumns} columns.");

            Debug.Log(
                $"Mobile layout profile passed: columns={columns}, "
                + $"card={profile.cardSize}, spacing={profile.columnSpacing}, "
                + $"stackStep={profile.verticalOffset}.");
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Mathf.Abs(left.x - right.x) < 0.1f
                && Mathf.Abs(left.y - right.y) < 0.1f;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
