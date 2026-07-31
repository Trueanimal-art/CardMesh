using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CardMesh
{
    /// <summary>
    /// Runtime behaviour for the scene-authored shop popup.
    /// All visual objects and RectTransform values live in MainMenu.unity.
    /// </summary>
    public sealed class ShopPopup : MonoBehaviour
    {
        [Header("Runtime bindings")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button infoButton;

        private Coroutine openRoutine;
        private Coroutine purchaseFeedbackRoutine;
        private bool hasShownRuntime;

        private static readonly Color Gold = new(1f, 0.78f, 0.2f, 1f);
        private static readonly Color SoftGold = new(1f, 0.87f, 0.5f, 1f);
        private static readonly Color PremiumFill = new(0.018f, 0.115f, 0.072f, 0.96f);
        private static readonly Color ResourceFill = new(0.024f, 0.14f, 0.087f, 0.94f);

        public static ShopPopup SceneInstance =>
            FindFirstObjectByType<ShopPopup>(FindObjectsInactive.Include);

        private void Awake()
        {
            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireButtons();

            if (!hasShownRuntime)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireButtons();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        public void Show()
        {
            hasShownRuntime = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireButtons();
            StartOpenAnimation();
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void ShowInfo()
        {
            Debug.Log("Shop: purchases are demo UI only. Payments are not connected.");
        }

        private void Buy(ShopProductButton product)
        {
            if (product == null)
            {
                return;
            }

            Debug.Log($"Shop purchase clicked: {product.ProductName} | {product.Price} | {product.PurchaseType}");
            PlayPurchaseFeedback(product.transform.parent as RectTransform);
        }

        private void BindSceneReferences()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            panel = transform.Find("Panel") as RectTransform;
            closeButton = transform.Find("Panel/CloseButton")?.GetComponent<Button>();
            infoButton = transform.Find("Panel/InfoButton")?.GetComponent<Button>();
        }

        private void WireButtons()
        {
            BindButton(closeButton, Hide);
            BindButton(infoButton, ShowInfo);
            BindBuyButtons(transform);
        }

        private void UnwireButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
            }

            if (infoButton != null)
            {
                infoButton.onClick.RemoveListener(ShowInfo);
            }
        }

        private static void BindButton(Button button, UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void BindBuyButtons(Transform root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                ShopProductButton product = button.GetComponent<ShopProductButton>();
                if (product == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Buy(product));
            }
        }

        private void ApplyUiPolish()
        {
            if (panel == null)
            {
                return;
            }

            RectTransform header = FindRect(panel, "Header");
            RectTransform content = FindRect(panel, "ShopContent");
            if (content == null)
            {
                return;
            }

            float panelWidth = panel.rect.width > 100f ? panel.rect.width : 1510f;
            float premiumWidth = Mathf.Clamp(panelWidth * 0.82f, 1080f, 1240f);
            float resourcesWidth = Mathf.Clamp(panelWidth * 0.715f, 920f, 1080f);

            ConfigureHeader(header);
            ConfigureSectionText(content);

            RectTransform treasuryTitle = FindRect(content, "TreasuryTitle");
            RectTransform treasuryCopy = FindRect(content, "TreasuryCopy");
            RectTransform premiumTitle = FindRect(content, "PremiumTitle");
            RectTransform premiumBundles = FindRect(content, "PremiumBundles");
            RectTransform resourcesTitle = FindRect(content, "ResourcesTitle");
            RectTransform resources = FindRect(content, "Resources");

            SetRelativeAnchor(treasuryTitle, new Vector2(0.5f, 0.77f), new Vector2(900f, 40f));
            SetRelativeAnchor(treasuryCopy, new Vector2(0.5f, 0.725f), new Vector2(960f, 28f));
            SetRelativeAnchor(premiumTitle, new Vector2(0.5f, 0.68f), new Vector2(620f, 34f));
            SetRelativeAnchor(premiumBundles, new Vector2(0.5f, 0.48f), new Vector2(premiumWidth, 348f));
            SetRelativeAnchor(resourcesTitle, new Vector2(0.5f, 0.295f), new Vector2(760f, 30f));
            SetRelativeAnchor(resources, new Vector2(0.5f, 0.155f), new Vector2(resourcesWidth, 200f));

            ConfigurePremiumBundles(premiumBundles);
            ConfigureResourceOffers(resources);
            EnsurePremiumMotion();
        }

        private static void ConfigureHeader(RectTransform header)
        {
            if (header == null)
            {
                return;
            }

            SetRelativeAnchor(header, new Vector2(0.5f, 0.85f), new Vector2(1180f, 94f));
            TextMeshProUGUI title = header.Find("Title")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI subtitle = header.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
            SetLocalRect(title?.rectTransform, new Vector2(0f, 17f), new Vector2(900f, 58f));
            SetLocalRect(subtitle?.rectTransform, new Vector2(0f, -27f), new Vector2(1100f, 32f));
            ConfigureText(title, 50f, 44f, 54f, Gold, FontStyles.Bold);
            ConfigureText(subtitle, 22f, 18f, 23f, new Color(1f, 0.92f, 0.72f, 0.94f), FontStyles.Bold);
        }

        private static void ConfigureSectionText(RectTransform content)
        {
            TextMeshProUGUI treasuryTitle = content.Find("TreasuryTitle")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI treasuryCopy = content.Find("TreasuryCopy")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI premiumTitle = content.Find("PremiumTitle")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI resourcesTitle = content.Find("ResourcesTitle")?.GetComponent<TextMeshProUGUI>();

            ConfigureText(treasuryTitle, 31f, 25f, 32f, new Color(1f, 0.76f, 0.22f, 0.96f), FontStyles.Bold);
            ConfigureText(treasuryCopy, 18f, 15f, 19f, new Color(1f, 0.9f, 0.7f, 0.82f), FontStyles.Normal);
            ConfigureText(premiumTitle, 25f, 21f, 26f, Gold, FontStyles.Bold);
            ConfigureText(resourcesTitle, 25f, 21f, 26f, Gold, FontStyles.Bold);
        }

        private static void ConfigurePremiumBundles(RectTransform container)
        {
            if (container == null)
            {
                return;
            }

            string[] names = { "StarterBundle", "RoyalBundle", "EmperorBundle" };
            float[] anchors = { 0.16f, 0.5f, 0.84f };
            float cardWidth = Mathf.Clamp(container.rect.width * 0.29f, 330f, 360f);

            for (int index = 0; index < names.Length; index++)
            {
                RectTransform card = FindRect(container, names[index]);
                if (card == null)
                {
                    continue;
                }

                SetRelativeAnchor(card, new Vector2(anchors[index], 0.5f), new Vector2(cardWidth, 348f));
                card.localScale = index == 1 ? Vector3.one * 1.025f : Vector3.one;
                ConfigurePremiumCard(card, index == 1);
            }
        }

        private static void ConfigurePremiumCard(RectTransform card, bool recommended)
        {
            Image fill = card.Find("CardFill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = recommended
                    ? new Color(0.028f, 0.17f, 0.1f, 0.98f)
                    : PremiumFill;
                Outline outline = GetOrAdd<Outline>(fill.gameObject);
                outline.effectColor = recommended
                    ? new Color(1f, 0.75f, 0.16f, 0.62f)
                    : new Color(0.86f, 0.62f, 0.13f, 0.38f);
                outline.effectDistance = recommended ? new Vector2(1.7f, -1.7f) : new Vector2(1.15f, -1.15f);
                outline.useGraphicAlpha = true;
            }

            RectTransform titleRect = FindRect(card, "Title");
            RectTransform iconRect = FindRect(card, "Icon");
            RectTransform contentsRect = FindRect(card, "Contents");
            RectTransform rewardIconsRect = FindRect(card, "RewardIcons");
            RectTransform premiumRect = FindRect(card, "Premium");
            RectTransform buyButton = FindRect(card, "BuyButton");

            SetLocalRect(titleRect, new Vector2(0f, 142f), new Vector2(card.rect.width - 24f, 30f));
            if (iconRect != null)
            {
                iconRect.anchoredPosition = new Vector2(0f, 70f);
            }

            SetLocalRect(contentsRect, new Vector2(45f, -61f), new Vector2(220f, 86f));
            SetLocalRect(rewardIconsRect, new Vector2(-112f, -61f), new Vector2(26f, 86f));
            SetLocalRect(premiumRect, new Vector2(45f, -104f), new Vector2(210f, 18f));
            SetLocalRect(buyButton, new Vector2(0f, -143f), new Vector2(252f, 50f));

            ConfigureText(titleRect?.GetComponent<TextMeshProUGUI>(), 24f, 18f, 25f, Gold, FontStyles.Bold);
            ConfigureText(contentsRect?.GetComponent<TextMeshProUGUI>(), 19f, 16f, 20f, new Color(1f, 0.94f, 0.76f, 1f), FontStyles.Bold);
            ConfigureText(premiumRect?.GetComponent<TextMeshProUGUI>(), 14f, 12f, 15f, new Color(1f, 0.79f, 0.26f, 0.92f), FontStyles.Bold);
            ConfigureBuyButton(buyButton, 25f);

            if (recommended)
            {
                SetLocalRect(titleRect, new Vector2(0f, 124f), new Vector2(card.rect.width - 24f, 28f));
                if (iconRect != null)
                {
                    iconRect.anchoredPosition = new Vector2(0f, 55f);
                }

                EnsurePopularBadge(card);
            }
            else
            {
                Transform oldBadge = card.Find("PopularBadge");
                if (oldBadge != null)
                {
                    oldBadge.gameObject.SetActive(false);
                }
            }
        }

        private static void ConfigureResourceOffers(RectTransform container)
        {
            if (container == null)
            {
                return;
            }

            string[] names = { "Coins10000", "Hints20", "Moves50", "RemoveAds" };
            float[] anchors = { 0.125f, 0.375f, 0.625f, 0.875f };
            float cardWidth = Mathf.Clamp(container.rect.width * 0.215f, 206f, 230f);

            for (int index = 0; index < names.Length; index++)
            {
                RectTransform card = FindRect(container, names[index]);
                if (card == null)
                {
                    continue;
                }

                SetRelativeAnchor(card, new Vector2(anchors[index], 0.5f), new Vector2(cardWidth, 194f));
                Image fill = card.Find("CardFill")?.GetComponent<Image>();
                if (fill != null)
                {
                    fill.color = ResourceFill;
                    Outline outline = GetOrAdd<Outline>(fill.gameObject);
                    outline.effectColor = new Color(0.9f, 0.67f, 0.16f, 0.4f);
                    outline.effectDistance = new Vector2(1f, -1f);
                    outline.useGraphicAlpha = true;
                }

                RectTransform offerTitle = FindRect(card, "OfferTitle");
                RectTransform offerAmount = FindRect(card, "OfferAmount");
                RectTransform offerDetail = FindRect(card, "OfferDetail");
                RectTransform buyButton = FindRect(card, "BuyButton");
                SetLocalRect(offerTitle, new Vector2(0f, 78f), new Vector2(cardWidth - 20f, 28f));
                SetLocalRect(offerAmount, new Vector2(0f, -26f), new Vector2(cardWidth - 20f, 30f));
                SetLocalRect(offerDetail, new Vector2(0f, -49f), new Vector2(cardWidth - 20f, 22f));
                SetLocalRect(buyButton, new Vector2(0f, -76f), new Vector2(cardWidth - 40f, 40f));

                ConfigureText(offerTitle?.GetComponent<TextMeshProUGUI>(), 21f, 15f, 22f, Gold, FontStyles.Bold);
                ConfigureText(offerAmount?.GetComponent<TextMeshProUGUI>(), 25f, 19f, 26f, new Color(1f, 0.94f, 0.74f, 1f), FontStyles.Bold);
                ConfigureText(offerDetail?.GetComponent<TextMeshProUGUI>(), 14f, 11f, 15f, new Color(1f, 0.9f, 0.7f, 0.9f), FontStyles.Bold);
                ConfigureBuyButton(buyButton, 21f);
            }
        }

        private static void ConfigureBuyButton(RectTransform buttonRect, float priceSize)
        {
            if (buttonRect == null)
            {
                return;
            }

            Image image = buttonRect.GetComponent<Image>();
            Button button = buttonRect.GetComponent<Button>();
            if (image != null)
            {
                Sprite sprite = PremiumUISprites.GameplayActionButton != null
                    ? PremiumUISprites.GameplayActionButton
                    : PremiumUISprites.PrimaryButton;
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.color = Color.white;
            }

            if (button != null)
            {
                button.targetGraphic = image;
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.96f, 0.76f, 1f);
                colors.pressedColor = new Color(0.76f, 0.86f, 0.68f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.38f, 0.42f, 0.36f, 0.72f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.08f;
                button.colors = colors;
            }

            TextMeshProUGUI label = buttonRect.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                Stretch(label.rectTransform, 16f, 16f, 7f, 7f);
                ConfigureText(label, priceSize, Mathf.Max(16f, priceSize - 5f), priceSize + 1f, SoftGold, FontStyles.Bold);
                label.alignment = TextAlignmentOptions.Center;
            }

            EnsureHoverGlow(buttonRect, image);
            if (buttonRect.GetComponent<UIJuicyButton>() == null)
            {
                buttonRect.gameObject.AddComponent<UIJuicyButton>();
            }
        }

        private static void EnsureHoverGlow(RectTransform buttonRect, Image source)
        {
            if (source == null)
            {
                return;
            }

            RectTransform glowRect = buttonRect.Find("HoverGlow") as RectTransform;
            Image glow;
            CanvasGroup group;
            if (glowRect == null)
            {
                GameObject glowObject = new("HoverGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                glowObject.layer = buttonRect.gameObject.layer;
                glowObject.transform.SetParent(buttonRect, false);
                glowRect = glowObject.GetComponent<RectTransform>();
                glowRect.SetAsFirstSibling();
                glow = glowObject.GetComponent<Image>();
                group = glowObject.GetComponent<CanvasGroup>();
            }
            else
            {
                glow = glowRect.GetComponent<Image>() ?? glowRect.gameObject.AddComponent<Image>();
                group = glowRect.GetComponent<CanvasGroup>() ?? glowRect.gameObject.AddComponent<CanvasGroup>();
            }

            Stretch(glowRect, -4f, -4f, -3f, -3f);
            glow.sprite = source.sprite;
            glow.type = source.type;
            glow.color = new Color(1f, 0.78f, 0.22f, 0.22f);
            glow.raycastTarget = false;
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static void EnsurePopularBadge(RectTransform card)
        {
            RectTransform badge = card.Find("PopularBadge") as RectTransform;
            Image badgeImage;
            TextMeshProUGUI label;
            if (badge == null)
            {
                GameObject badgeObject = new("PopularBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeObject.layer = card.gameObject.layer;
                badgeObject.transform.SetParent(card, false);
                badge = badgeObject.GetComponent<RectTransform>();
                badgeImage = badgeObject.GetComponent<Image>();

                GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelObject.layer = card.gameObject.layer;
                labelObject.transform.SetParent(badge, false);
                label = labelObject.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                badge.gameObject.SetActive(true);
                badgeImage = badge.GetComponent<Image>() ?? badge.gameObject.AddComponent<Image>();
                label = badge.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (label == null)
                {
                    GameObject labelObject = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    labelObject.layer = card.gameObject.layer;
                    labelObject.transform.SetParent(badge, false);
                    label = labelObject.GetComponent<TextMeshProUGUI>();
                }
            }

            badge.anchorMin = new Vector2(0.5f, 0.5f);
            badge.anchorMax = badge.anchorMin;
            badge.pivot = new Vector2(0.5f, 0.5f);
            badge.anchoredPosition = new Vector2(0f, 157f);
            badge.sizeDelta = new Vector2(150f, 26f);
            badge.localScale = Vector3.one;
            badgeImage.sprite = PremiumUISprites.CategoryLabel != null
                ? PremiumUISprites.CategoryLabel
                : PremiumUISprites.CleanGoldButton;
            badgeImage.type = Image.Type.Simple;
            badgeImage.color = new Color(0.98f, 0.78f, 0.22f, 0.96f);
            badgeImage.raycastTarget = false;

            Stretch(label.rectTransform, 10f, 10f, 3f, 3f);
            label.text = "ПОПУЛЯРНЫЙ";
            label.alignment = TextAlignmentOptions.Center;
            ConfigureText(label, 14f, 12f, 15f, new Color(0.16f, 0.075f, 0.012f, 1f), FontStyles.Bold);
            RuntimeCyrillicFontProvider.Apply(label, label.text);
            badge.SetAsLastSibling();
        }

        private void EnsurePremiumMotion()
        {
            if (GetComponent<ShopPremiumMotion>() == null)
            {
                gameObject.AddComponent<ShopPremiumMotion>();
            }
        }

        private void PlayPurchaseFeedback(RectTransform card)
        {
            if (card == null || !isActiveAndEnabled)
            {
                return;
            }

            if (purchaseFeedbackRoutine != null)
            {
                StopCoroutine(purchaseFeedbackRoutine);
            }

            purchaseFeedbackRoutine = StartCoroutine(PurchaseFeedback(card));
        }

        private IEnumerator PurchaseFeedback(RectTransform card)
        {
            Vector3 baseScale = card.localScale;
            Vector3 pressedScale = baseScale * 0.985f;
            const float halfDuration = 0.07f;

            for (int phase = 0; phase < 2; phase++)
            {
                float elapsed = 0f;
                Vector3 from = phase == 0 ? baseScale : pressedScale;
                Vector3 to = phase == 0 ? pressedScale : baseScale;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / halfDuration);
                    card.localScale = Vector3.LerpUnclamped(from, to, 1f - Mathf.Pow(1f - t, 2f));
                    yield return null;
                }
            }

            card.localScale = baseScale;
            purchaseFeedbackRoutine = null;
        }

        private static void ConfigureText(
            TextMeshProUGUI text,
            float size,
            float minSize,
            float maxSize,
            Color color,
            FontStyles style)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static RectTransform FindRect(Transform root, string childName)
        {
            return root?.Find(childName) as RectTransform;
        }

        private static void SetRelativeAnchor(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void SetLocalRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.localScale = Vector3.one;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.GetComponent<T>() ?? target.AddComponent<T>();
        }

        private void StartOpenAnimation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
            }

            openRoutine = StartCoroutine(PlayOpenAnimation());
        }

        private IEnumerator PlayOpenAnimation()
        {
            const float duration = 0.18f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.96f;

            if (panel != null)
            {
                panel.localScale = startScale;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = eased;
                }

                if (panel != null)
                {
                    panel.localScale = Vector3.LerpUnclamped(startScale, Vector3.one, eased);
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (panel != null)
            {
                panel.localScale = Vector3.one;
            }
        }

        private void ApplyFontsToAllTexts()
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                RuntimeCyrillicFontProvider.Apply(text, text.text);
            }
        }

#if UNITY_EDITOR
        // Applies the polish once in Edit Mode and persists every object and
        // RectTransform value into MainMenu.unity for normal scene authoring.
        public void MaterializeAuthoringLayout()
        {
            BindSceneReferences();
            ApplyUiPolish();
            ApplyFontsToAllTexts();
        }
#endif
    }
}
