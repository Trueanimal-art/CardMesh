using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class LevelFrameChrome : MonoBehaviour
    {
        public enum Preset
        {
            Compact,
            Medium,
            Tall
        }

        public readonly struct Metrics
        {
            public readonly Vector2 PanelSize;
            public readonly Vector2 PanelPosition;
            public readonly float ContentTop;
            public readonly float ContentSide;
            public readonly float ContentBottom;
            public readonly float BadgeSize;

            public Metrics(Vector2 panelSize, Vector2 panelPosition, float contentTop, float contentSide, float contentBottom, float badgeSize)
            {
                PanelSize = panelSize;
                PanelPosition = panelPosition;
                ContentTop = contentTop;
                ContentSide = contentSide;
                ContentBottom = contentBottom;
                BadgeSize = badgeSize;
            }
        }

        private const string ChromeName = "LevelFrameChrome";
        private const float ViewportMargin = 24f;
        private static readonly Color FillColor = new(0.018f, 0.12f, 0.058f, 0.985f);
        private static readonly Color GoldFallback = new(0.94f, 0.68f, 0.12f, 1f);

        [SerializeField] private Preset preset;
        [SerializeField] private RectTransform chromeRoot;
        [SerializeField] private RectTransform surface;
        [SerializeField] private RectTransform fill;
        [SerializeField] private float panelWidthOverride;
        [SerializeField] private bool overridePanelPosition;
        [SerializeField] private Vector2 panelPositionOverride;
        [SerializeField] private bool overrideBadgeLayout;
        [SerializeField] private Vector2 badgeSizeOverride;
        [SerializeField] private Vector2 badgePositionOverride;
        [SerializeField] private Sprite surfaceSpriteOverride;
        [SerializeField] private bool showTopJewel;

        public Metrics CurrentMetrics => GetMetrics(preset);

        private void Start()
        {
            // Start runs after Unity's hierarchy consistency pass, so creating the
            // runtime chrome does not trigger OnTransformChildrenChanged warnings.
            RefreshChrome();
        }

        private void RefreshChrome()
        {
            RectTransform badge = transform.Find("TopBadge") as RectTransform
                ?? transform.Find("HeaderIcon") as RectTransform;
            Button close = transform.Find("CloseButton")?.GetComponent<Button>();
            Button info = transform.Find("InfoButton")?.GetComponent<Button>();
            Configure(preset, badge, close, info);
        }

        public static Metrics GetMetrics(Preset value)
        {
            return value switch
            {
                Preset.Tall => new Metrics(new Vector2(1020f, 1760f), new Vector2(0f, -40f), 268f, 76f, 120f, 152f),
                Preset.Medium => new Metrics(new Vector2(920f, 1040f), new Vector2(0f, 12f), 156f, 72f, 72f, 112f),
                _ => new Metrics(new Vector2(920f, 700f), Vector2.zero, 128f, 72f, 72f, 108f)
            };
        }

        public void Configure(Preset value, RectTransform badge = null, Button close = null, Button info = null)
        {
            preset = value;
            EnsureHierarchy();
            ApplyLayout();
            LayoutBadge(badge);
            bool tall = preset == Preset.Tall;
            LayoutCornerButton(close != null ? close.transform as RectTransform : null, true, tall);
            LayoutCornerButton(info != null ? info.transform as RectTransform : null, false, tall);
            if (tall)
            {
                StyleTallCloseButton(close);
            }
            OrderLayers(badge, close, info);
        }

        public void SetPanelLayoutOverride(float width, Vector2 position)
        {
            panelWidthOverride = Mathf.Max(0f, width);
            overridePanelPosition = true;
            panelPositionOverride = position;
            ApplyLayout();

            RectTransform badge = transform.Find("TopBadge") as RectTransform
                ?? transform.Find("HeaderIcon") as RectTransform;
            Button close = transform.Find("CloseButton")?.GetComponent<Button>();
            Button info = transform.Find("InfoButton")?.GetComponent<Button>();
            LayoutBadge(badge);
            LayoutCornerButton(close != null ? close.transform as RectTransform : null, true, preset == Preset.Tall);
            LayoutCornerButton(info != null ? info.transform as RectTransform : null, false, preset == Preset.Tall);
        }

        public void SetBadgeLayoutOverride(Vector2 size, Vector2 position)
        {
            overrideBadgeLayout = true;
            badgeSizeOverride = size;
            badgePositionOverride = position;
            RectTransform badge = transform.Find("TopBadge") as RectTransform
                ?? transform.Find("HeaderIcon") as RectTransform;
            LayoutBadge(badge);
        }

        public void SetSurfaceSpriteOverride(Sprite sprite)
        {
            surfaceSpriteOverride = sprite;
            ApplyLayout();
        }

        public void SetTopJewelVisible(bool visible)
        {
            showTopJewel = visible;
            RectTransform jewel = Find("JewelTop");
            if (jewel == null)
            {
                return;
            }

            jewel.gameObject.SetActive(visible && preset == Preset.Tall);
            if (jewel.gameObject.activeSelf)
            {
                // The tall panel contains transparent breathing room above the
                // authored frame. Anchor the jewel to the badge medallion itself,
                // rather than to the outer panel edge, so it matches the baked
                // jewel used by the other three popup frames.
                Vector2 badgeSize = overrideBadgeLayout
                    ? badgeSizeOverride
                    : Vector2.one * CurrentMetrics.BadgeSize;
                Vector2 badgePosition = overrideBadgeLayout
                    ? badgePositionOverride
                    : new Vector2(0f, -168f);
                jewel.anchoredPosition = new Vector2(
                    badgePosition.x,
                    badgePosition.y + badgeSize.y * 0.5f);
                jewel.SetAsLastSibling();
            }
        }

        public void EnsureHierarchy()
        {
            RectTransform panel = transform as RectTransform;
            if (panel == null)
            {
                return;
            }

            chromeRoot = panel.Find(ChromeName) as RectTransform ?? CreateRect(panel, ChromeName);
            Stretch(chromeRoot, 0f);

            surface = EnsureImage(chromeRoot, "Surface", ResolveSurfaceSprite(), Color.white, Image.Type.Simple).rectTransform;
            fill = EnsureImage(chromeRoot, "Fill", PremiumUISprites.EmeraldPanel, FillColor, Image.Type.Sliced).rectTransform;
            EnsureImage(chromeRoot, "RailTop", Load("level_frame_rail_h"), GoldFallback, Image.Type.Simple);
            EnsureImage(chromeRoot, "RailBottom", Load("level_frame_rail_h"), GoldFallback, Image.Type.Simple);
            EnsureImage(chromeRoot, "RailLeft", Load("level_frame_rail_v"), GoldFallback, Image.Type.Simple);
            EnsureImage(chromeRoot, "RailRight", Load("level_frame_rail_v"), GoldFallback, Image.Type.Simple);
            EnsureImage(chromeRoot, "CornerTopLeft", Load("level_frame_jewel"), GoldFallback, Image.Type.Simple);
            EnsureImage(chromeRoot, "CornerTopRight", Load("level_frame_jewel"), GoldFallback, Image.Type.Simple);
            EnsureImage(chromeRoot, "CornerBottomLeft", Load("level_frame_jewel"), GoldFallback, Image.Type.Simple);
            EnsureImage(chromeRoot, "CornerBottomRight", Load("level_frame_jewel"), GoldFallback, Image.Type.Simple);
            EnsureImage(chromeRoot, "JewelTop", Load("level_frame_jewel"), Color.white, Image.Type.Simple);
            EnsureImage(chromeRoot, "JewelBottom", Load("level_frame_jewel"), Color.white, Image.Type.Simple);

            DisableLegacyFrameImages(panel);
        }

        public void ApplyLayout()
        {
            RectTransform panel = transform as RectTransform;
            if (panel == null || chromeRoot == null)
            {
                return;
            }

            Metrics metrics = CurrentMetrics;
            Vector2 panelSize = metrics.PanelSize;
            if (panelWidthOverride > 0f)
            {
                panelSize.x = panelWidthOverride;
            }

            SetCentered(panel, panelSize, overridePanelPosition ? panelPositionOverride : metrics.PanelPosition);
            ApplyViewportScale(panel);
            Stretch(chromeRoot, 0f);
            Stretch(surface, 0f);

            Image surfaceImage = surface != null ? surface.GetComponent<Image>() : null;
            if (surfaceImage != null)
            {
                surfaceImage.sprite = ResolveSurfaceSprite();
                surfaceImage.type = preset == Preset.Tall || preset == Preset.Compact
                    ? Image.Type.Simple
                    : Image.Type.Sliced;
                surfaceImage.color = Color.white;
                surfaceImage.preserveAspect = false;
                surfaceImage.enabled = true;
            }

            // Tall popups use the authored mobile frame as one coherent textured surface.
            // The old composed variant exposed a flat bright-green fill and technical rails.
            SetChromePartsActive(false);
            ConfigureTallShade();

            // The source vertical rail contains a large baked midpoint diamond.
            // Tall popups deliberately use a restrained solid gold rail instead.
            ConfigureCleanVerticalRail(Find("RailLeft"));
            ConfigureCleanVerticalRail(Find("RailRight"));

            SetStretchHorizontal(Find("RailTop"), 30f, 30f, 18f, -3f, true);
            SetStretchHorizontal(Find("RailBottom"), 30f, 30f, 18f, 3f, false);
            SetStretchVertical(Find("RailLeft"), 30f, 30f, 6f, 17f, true);
            SetStretchVertical(Find("RailRight"), 30f, 30f, 6f, -17f, false);

            SetCorner(Find("CornerTopLeft"), new Vector2(0f, 1f), new Vector2(42f, 46f), new Vector2(18f, -18f), Vector3.one);
            SetCorner(Find("CornerTopRight"), Vector2.one, new Vector2(42f, 46f), new Vector2(-18f, -18f), Vector3.one);
            SetCorner(Find("CornerBottomLeft"), Vector2.zero, new Vector2(42f, 46f), new Vector2(18f, 18f), Vector3.one);
            SetCorner(Find("CornerBottomRight"), new Vector2(1f, 0f), new Vector2(42f, 46f), new Vector2(-18f, 18f), Vector3.one);

            SetEdgeJewel(Find("JewelTop"), true);
            SetEdgeJewel(Find("JewelBottom"), false);
            SetTopJewelVisible(showTopJewel);
            chromeRoot.SetAsFirstSibling();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled || transform is not RectTransform panel)
            {
                return;
            }

            ApplyViewportScale(panel);
        }

        private static void ApplyViewportScale(RectTransform panel)
        {
            if (panel.parent is not RectTransform viewport
                || viewport.rect.width <= 1f
                || viewport.rect.height <= 1f
                || panel.rect.width <= 1f
                || panel.rect.height <= 1f)
            {
                panel.localScale = Vector3.one;
                return;
            }

            // Keep every popup on the same authored 1020x1760 grid. On short
            // aspect ratios (notably tablets) scale that grid uniformly instead
            // of moving individual controls or inventing device-specific centres.
            float availableWidth = Mathf.Max(1f,
                viewport.rect.width - ViewportMargin * 2f - Mathf.Abs(panel.anchoredPosition.x) * 2f);
            float availableHeight = Mathf.Max(1f,
                viewport.rect.height - ViewportMargin * 2f - Mathf.Abs(panel.anchoredPosition.y) * 2f);
            float scale = Mathf.Min(1f,
                availableWidth / panel.rect.width,
                availableHeight / panel.rect.height);
            panel.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

        public void LayoutBadge(RectTransform badge)
        {
            if (badge == null)
            {
                return;
            }

            Vector2 size = overrideBadgeLayout
                ? badgeSizeOverride
                : Vector2.one * CurrentMetrics.BadgeSize;
            badge.anchorMin = new Vector2(0.5f, 1f);
            badge.anchorMax = new Vector2(0.5f, 1f);
            badge.pivot = new Vector2(0.5f, 0.5f);
            badge.sizeDelta = size;
            badge.anchoredPosition = overrideBadgeLayout
                ? badgePositionOverride
                : preset == Preset.Tall
                    ? new Vector2(0f, -146f)
                    : new Vector2(0f, -6f);
            badge.localScale = Vector3.one;
        }

        public static void LayoutCornerButton(RectTransform rect, bool right)
        {
            LayoutCornerButton(rect, right, false);
        }

        private static void LayoutCornerButton(RectTransform rect, bool right, bool tall)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = right ? Vector2.one : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = tall ? new Vector2(72f, 72f) : new Vector2(60f, 60f);
            // The ornamental circle is baked slightly below the geometric centre
            // of the tall frame's top corner. Match that optical centre.
            float tallWidth = rect.parent is RectTransform parent ? parent.rect.width : 1020f;
            float tallHorizontalOffset = 166f * Mathf.Max(1f, tallWidth) / 941f;
            float tallHeight = rect.parent is RectTransform tallParent ? tallParent.rect.height : 1760f;
            float tallVerticalOffset = -223f * Mathf.Max(1f, tallHeight) / 1672f;
            Vector2 offset = tall ? new Vector2(tallHorizontalOffset, tallVerticalOffset) : new Vector2(90f, -100f);
            rect.anchoredPosition = right ? new Vector2(-offset.x, offset.y) : offset;
            rect.localScale = Vector3.one;
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.raycastPadding = new Vector4(-12f, -12f, -12f, -12f);
            }
        }

        private static void StyleTallCloseButton(Button button)
        {
            if (button == null || button.transform is not RectTransform root)
            {
                return;
            }

            Image target = root.GetComponent<Image>();
            if (target != null)
            {
                // The tall frame already contains the circular close medallion.
                // Keep only its hit target so a second circle is not drawn on top.
                target.sprite = null;
                target.color = Color.clear;
                target.raycastTarget = true;
                button.targetGraphic = target;
            }

            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                label.gameObject.SetActive(false);
            }

            foreach (Image childImage in root.GetComponentsInChildren<Image>(true))
            {
                if (childImage.transform == root
                    || childImage.name == "CloseStrokeA"
                    || childImage.name == "CloseStrokeB")
                {
                    continue;
                }

                childImage.gameObject.SetActive(false);
            }

            EnsureCloseStroke(root, "CloseStrokeA", 45f);
            EnsureCloseStroke(root, "CloseStrokeB", -45f);
        }

        private static void EnsureCloseStroke(RectTransform parent, string objectName, float angle)
        {
            RectTransform stroke = parent.Find(objectName) as RectTransform ?? CreateRect(parent, objectName);
            Image image = stroke.GetComponent<Image>() ?? stroke.gameObject.AddComponent<Image>();
            stroke.gameObject.SetActive(true);
            image.sprite = null;
            image.color = new Color(1f, 0.82f, 0.28f, 1f);
            image.raycastTarget = false;

            stroke.anchorMin = stroke.anchorMax = stroke.pivot = new Vector2(0.5f, 0.5f);
            stroke.anchoredPosition = Vector2.zero;
            stroke.sizeDelta = new Vector2(36f, 6f);
            stroke.localScale = Vector3.one;
            stroke.localRotation = Quaternion.Euler(0f, 0f, angle);
            stroke.SetAsLastSibling();
        }

        private void OrderLayers(RectTransform badge, Button close, Button info)
        {
            chromeRoot?.SetAsFirstSibling();
            badge?.SetAsLastSibling();
            info?.transform.SetAsLastSibling();
            close?.transform.SetAsLastSibling();
        }

        private RectTransform Find(string childName) => chromeRoot != null ? chromeRoot.Find(childName) as RectTransform : null;

        private static Sprite Load(string assetName) => Resources.Load<Sprite>($"UI/Premium/LevelFrame/{assetName}");

        private Sprite ResolveSurfaceSprite()
        {
            if (surfaceSpriteOverride != null)
            {
                return surfaceSpriteOverride;
            }

            if (preset == Preset.Tall && PremiumUISprites.MobilePopupFrame != null)
            {
                return PremiumUISprites.MobilePopupFrame;
            }

            if (preset == Preset.Compact)
            {
                return PremiumUISprites.PauseWindowCardMesh;
            }

            Sprite sliced = Resources.Load<Sprite>("UI/Premium/three_popup_clean_frame_sprite");
            return sliced != null ? sliced : PremiumUISprites.PauseWindowCardMesh;
        }

        private void SetChromePartsActive(bool active)
        {
            string[] names =
            {
                "Fill", "RailTop", "RailBottom", "RailLeft", "RailRight",
                "CornerTopLeft", "CornerTopRight", "CornerBottomLeft", "CornerBottomRight",
                "JewelTop", "JewelBottom"
            };
            for (int i = 0; i < names.Length; i++)
            {
                Transform child = chromeRoot != null ? chromeRoot.Find(names[i]) : null;
                if (child != null)
                {
                    child.gameObject.SetActive(active);
                }
            }
        }

        private void ConfigureTallShade()
        {
            if (fill == null)
            {
                return;
            }

            // Tall mobile frames already contain the complete textured emerald
            // surface. An additional translucent fill creates a visible grey
            // rectangle above the top rail on narrow/tall displays, so keep the
            // technical backing disabled for every standardized tall popup.
            fill.gameObject.SetActive(false);
        }

        private static void ConfigureCleanVerticalRail(RectTransform rect)
        {
            Image image = rect != null ? rect.GetComponent<Image>() : null;
            if (image == null)
            {
                return;
            }

            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = GoldFallback;
        }

        private static void DisableLegacyFrameImages(RectTransform panel)
        {
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.enabled = false;
                panelImage.raycastTarget = false;
            }

            Transform legacyFrame = panel.Find("Frame");
            Image legacyImage = legacyFrame != null ? legacyFrame.GetComponent<Image>() : null;
            if (legacyImage != null)
            {
                legacyImage.enabled = false;
                legacyImage.raycastTarget = false;
            }
        }

        private static Image EnsureImage(Transform parent, string objectName, Sprite sprite, Color fallbackColor, Image.Type type)
        {
            RectTransform rect = parent.Find(objectName) as RectTransform ?? CreateRect(parent, objectName);
            Image image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? type : Image.Type.Simple;
            image.color = sprite != null ? Color.white : fallbackColor;
            image.preserveAspect = false;
            image.raycastTarget = false;
            image.material = null;
            return image;
        }

        private static RectTransform CreateRect(Transform parent, string objectName)
        {
            GameObject child = new(objectName, typeof(RectTransform));
            child.layer = parent.gameObject.layer;
            child.transform.SetParent(parent, false);
            return child.transform as RectTransform;
        }

        private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            rect.localScale = Vector3.one;
        }

        private static void SetStretchHorizontal(RectTransform rect, float left, float right, float height, float edgeOffset, bool top)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, edgeOffset - height * 0.5f);
            rect.offsetMax = new Vector2(-right, edgeOffset + height * 0.5f);
            rect.localScale = top ? Vector3.one : new Vector3(1f, -1f, 1f);
        }

        private static void SetStretchVertical(RectTransform rect, float bottom, float top, float width, float edgeOffset, bool left)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
            rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(edgeOffset - width * 0.5f, bottom);
            rect.offsetMax = new Vector2(edgeOffset + width * 0.5f, -top);
            rect.localScale = left ? Vector3.one : new Vector3(-1f, 1f, 1f);
        }

        private static void SetCorner(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position, Vector3 scale)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = scale;
        }

        private static void SetEdgeJewel(RectTransform rect, bool top)
        {
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(50f, 56f);
            rect.anchoredPosition = new Vector2(0f, top ? -1f : 1f);
            rect.localScale = top ? Vector3.one : new Vector3(1f, -1f, 1f);
        }
    }
}
