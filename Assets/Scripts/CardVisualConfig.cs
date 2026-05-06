using TMPro;
using UnityEngine;

namespace SolitaireAssociations
{
    [CreateAssetMenu(fileName = "CardVisualConfig", menuName = "Solitaire Associations/Card Visual Config")]
    public sealed class CardVisualConfig : ScriptableObject
    {
        [Header("Sprites")]
        [SerializeField] private Sprite cardBackSprite;
        [SerializeField] private Sprite normalCardSprite;
        [SerializeField] private Sprite goldCardSprite;
        [SerializeField] private Sprite goalSlotEmblemSprite;

        [Header("Typography")]
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private float titleFontSize = 32f;
        [SerializeField] private float titleFontSizeMin = 17f;
        [SerializeField] private Color textColor = new(0.24f, 0.2f, 0.17f, 1f);
        [SerializeField] private Color goldTextColor = new(0.28f, 0.19f, 0.07f, 1f);

        [Header("Fallback Colors")]
        [SerializeField] private Color normalCardColor = new(0.98f, 0.97f, 0.94f, 1f);
        [SerializeField] private Color goldCardColor = new(0.98f, 0.78f, 0.22f, 1f);
        [SerializeField] private Color cardBackColor = new(0.06f, 0.22f, 0.48f, 1f);
        [SerializeField] private Color goalSlotColor = new(1f, 1f, 1f, 0.28f);
        [SerializeField] private Color normalOutlineColor = new(0f, 0f, 0f, 0.12f);
        [SerializeField] private Color goldOutlineColor = new(0.55f, 0.38f, 0f, 0.35f);
        [SerializeField] private Color backOutlineColor = new(0.05f, 0.27f, 0.5f, 0.45f);

        public Sprite CardBackSprite => cardBackSprite;
        public Sprite NormalCardSprite => normalCardSprite;
        public Sprite GoldCardSprite => goldCardSprite;
        public Sprite GoalSlotEmblemSprite => goalSlotEmblemSprite;
        public TMP_FontAsset Font => font;
        public float TitleFontSize => Mathf.Max(1f, titleFontSize);
        public float TitleFontSizeMin => Mathf.Clamp(titleFontSizeMin, 1f, TitleFontSize);
        public Color TextColor => textColor;
        public Color GoldTextColor => goldTextColor;
        public Color NormalCardColor => normalCardColor;
        public Color GoldCardColor => goldCardColor;
        public Color CardBackColor => cardBackColor;
        public Color GoalSlotColor => goalSlotColor;
        public Color NormalOutlineColor => normalOutlineColor;
        public Color GoldOutlineColor => goldOutlineColor;
        public Color BackOutlineColor => backOutlineColor;
    }
}
