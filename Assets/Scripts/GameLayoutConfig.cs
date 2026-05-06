using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    [Serializable]
    public struct ColumnLayoutProfile
    {
        [Min(1)] public int columnCount;
        public Vector2 cardSize;
        [Min(0f)] public float columnSpacing;
        [Min(0f)] public float verticalOffset;
    }

    [Serializable]
    public sealed class GameLayoutConfig
    {
        [SerializeField] private Vector2 referenceResolution = new(1920f, 1080f);
        [SerializeField, Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;
        [SerializeField, Min(0f)] private float reservedBottomSpace = 72f;
        [SerializeField] private List<ColumnLayoutProfile> columnProfiles = new()
        {
            new ColumnLayoutProfile
            {
                columnCount = 3,
                cardSize = new Vector2(148f, 216f),
                columnSpacing = 34f,
                verticalOffset = 34f
            },
            new ColumnLayoutProfile
            {
                columnCount = 4,
                cardSize = new Vector2(144f, 210f),
                columnSpacing = 28f,
                verticalOffset = 32f
            },
            new ColumnLayoutProfile
            {
                columnCount = 5,
                cardSize = new Vector2(132f, 192f),
                columnSpacing = 16f,
                verticalOffset = 28f
            }
        };

        public Vector2 ReferenceResolution => referenceResolution;
        public float MatchWidthOrHeight => matchWidthOrHeight;
        public float ReservedBottomSpace => reservedBottomSpace;
        public List<ColumnLayoutProfile> ColumnProfiles => columnProfiles;

        public static GameLayoutConfig CreateDefault()
        {
            return new GameLayoutConfig();
        }

        public ColumnLayoutProfile GetProfile(int targetColumnCount)
        {
            targetColumnCount = Mathf.Clamp(targetColumnCount, 3, 5);

            if (columnProfiles == null || columnProfiles.Count == 0)
            {
                return new ColumnLayoutProfile
                {
                    columnCount = targetColumnCount,
                    cardSize = new Vector2(132f, 192f),
                    columnSpacing = 16f,
                    verticalOffset = 28f
                };
            }

            ColumnLayoutProfile bestProfile = columnProfiles[0];
            int bestDistance = Mathf.Abs(bestProfile.columnCount - targetColumnCount);

            for (int index = 1; index < columnProfiles.Count; index++)
            {
                ColumnLayoutProfile profile = columnProfiles[index];
                int distance = Mathf.Abs(profile.columnCount - targetColumnCount);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestProfile = profile;
                }
            }

            return bestProfile;
        }
    }
}
