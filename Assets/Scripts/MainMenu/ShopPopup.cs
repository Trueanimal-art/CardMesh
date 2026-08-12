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
            ApplyUiPolish();
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
            ApplyUiPolish();
            ApplyFontsToAllTexts();
            WireButtons();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        public void Show()
        {
            MainMenuPopupExclusivity.OpenExclusive(this);
            hasShownRuntime = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            MainMenuNavigationVisibility.SetBottomNavigationVisible(this, true);
            BindSceneReferences();
            ApplyUiPolish();
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
            MainMenuNavigationVisibility.SetBottomNavigationVisible(this, true);
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

            RectTransform topBadge = FindRect(panel, "TopBadge") ?? FindRect(panel, "HeaderIcon");
            LevelFrameChrome chrome = panel.GetComponent<LevelFrameChrome>() ?? panel.gameObject.AddComponent<LevelFrameChrome>();
            chrome.Configure(LevelFrameChrome.Preset.Tall, topBadge, closeButton, infoButton);
            chrome.SetPanelLayoutOverride(1020f, LevelFrameChrome.GetMetrics(LevelFrameChrome.Preset.Tall).PanelPosition);
            chrome.SetBadgeLayoutOverride(new Vector2(152f, 152f), new Vector2(0f, -168f));
            // Use the same authored tall frame as Collection/Daily/Royal. Its top
            // diamond is baked into the frame at the correct scale and position;
            // the shop-only texture intentionally omitted it.
            chrome.SetSurfaceSpriteOverride(Resources.Load<Sprite>("UI/Premium/popup_frame_mobile_h3"));
            chrome.SetTopJewelVisible(false);
            Transform topJewel = panel.Find("LevelFrameChrome/JewelTop");
            if (topJewel != null)
            {
                topJewel.gameObject.SetActive(false);
            }
            Transform legacyTopGem = panel.Find("TopGem");
            if (legacyTopGem != null)
            {
                legacyTopGem.gameObject.SetActive(false);
            }
            Transform legacyTopFrameJewel = panel.Find("TopFrameJewel");
            if (legacyTopFrameJewel != null)
            {
                legacyTopFrameJewel.gameObject.SetActive(false);
            }
            if (topBadge != null)
            {
                // The source icon has generous transparent padding; scale its rect up
                // so the bag fills the authored header medallion instead of floating in it.
                topBadge.sizeDelta = new Vector2(152f, 152f);
                topBadge.anchoredPosition = new Vector2(0f, -168f);
            }
            if (infoButton != null)
            {
                infoButton.gameObject.SetActive(false);
            }
#if UNITY_EDITOR
            Image headerIcon = topBadge?.GetComponent<Image>();
            if (headerIcon != null)
            {
                headerIcon.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/MainMenuEmeraldVault/icon_shop_royal_seal.png");
                headerIcon.preserveAspect = true;
                headerIcon.color = Color.white;
                headerIcon.raycastTarget = false;
            }
#endif

            RectTransform header = FindRect(panel, "Header");
            RectTransform content = FindRect(panel, "ShopContent");
            if (content == null)
            {
                return;
            }

            SetRelativeAnchor(content, new Vector2(0.5f, 0.494f), new Vector2(840f, 1240f));
            ConfigureHeader(header);
            ConfigureSectionText(content);

            RectTransform treasuryTitle = FindRect(content, "TreasuryTitle");
            RectTransform treasuryCopy = FindRect(content, "TreasuryCopy");
            RectTransform premiumTitle = FindRect(content, "PremiumTitle");
            RectTransform premiumBundles = FindRect(content, "PremiumBundles");
            RectTransform resourcesTitle = FindRect(content, "ResourcesTitle");
            RectTransform resources = FindRect(content, "Resources");

            if (treasuryTitle != null) treasuryTitle.gameObject.SetActive(false);
            if (treasuryCopy != null) treasuryCopy.gameObject.SetActive(false);
            SetRelativeAnchor(premiumTitle, new Vector2(0.5f, 0.89f), new Vector2(800f, 34f));
            SetRelativeAnchor(premiumBundles, new Vector2(0.5f, 0.618f), new Vector2(840f, 700f));
            SetRelativeAnchor(resourcesTitle, new Vector2(0.5f, 0.312f), new Vector2(800f, 38f));
            SetRelativeAnchor(resources, new Vector2(0.5f, 0.108f), new Vector2(840f, 448f));

            ConfigurePremiumBundles(premiumBundles);
            ConfigureResourceOffers(resources);
            DisablePremiumMotion();
        }

        private static void ConfigureHeader(RectTransform header)
        {
            if (header == null)
            {
                return;
            }

            SetRelativeAnchor(header, new Vector2(0.5f, 0.79f), new Vector2(720f, 124f));
            TextMeshProUGUI title = header.Find("Title")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI subtitle = header.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
            SetLocalRect(title?.rectTransform, new Vector2(0f, 24f), new Vector2(720f, 64f));
            SetLocalRect(subtitle?.rectTransform, new Vector2(0f, -10f), new Vector2(760f, 34f));
            ConfigureText(title, 48f, 38f, 52f, Gold, FontStyles.Bold);
            ConfigureText(subtitle, 21f, 17f, 23f, new Color(1f, 0.92f, 0.72f, 0.94f), FontStyles.Bold);
            if (subtitle != null)
            {
                subtitle.enableAutoSizing = false;
                subtitle.fontSize = 20f;
                subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        private static void ConfigureSectionText(RectTransform content)
        {
            TextMeshProUGUI treasuryTitle = content.Find("TreasuryTitle")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI treasuryCopy = content.Find("TreasuryCopy")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI premiumTitle = content.Find("PremiumTitle")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI resourcesTitle = content.Find("ResourcesTitle")?.GetComponent<TextMeshProUGUI>();
            if (resourcesTitle != null)
            {
                resourcesTitle.gameObject.SetActive(true);
                resourcesTitle.text = "РЕСУРСЫ";
            }

            if (premiumTitle != null)
            {
                premiumTitle.text = "НАБОРЫ";
            }

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
            Vector2[] positions =
            {
                new(-205f, -195f),
                new(0f, 145f),
                new(205f, -195f)
            };

            for (int index = 0; index < names.Length; index++)
            {
                RectTransform card = FindRect(container, names[index]);
                if (card == null)
                {
                    continue;
                }

                bool hero = index == 1;
                Vector2 size = hero ? new Vector2(820f, 350f) : new Vector2(398f, 310f);
                SetLocalRect(card, positions[index], size);
                card.localScale = Vector3.one;
                ConfigurePremiumCard(card, hero);
            }
        }

        private static void ConfigurePremiumCard(RectTransform card, bool recommended)
        {
            ConfigureTreasurySurface(card, recommended);

            Image fill = card.Find("CardFill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.gameObject.SetActive(false);
            }

            RectTransform titleRect = FindRect(card, "Title");
            RectTransform iconRect = FindRect(card, "Icon");
            RectTransform contentsRect = FindRect(card, "Contents");
            RectTransform rewardIconsRect = FindRect(card, "RewardIcons");
            RectTransform premiumRect = FindRect(card, "Premium");
            RectTransform buyButton = FindRect(card, "BuyButton");

            // Keep every element inside the decorative niche.  The art has a deep
            // top/bottom frame, so content must use the same visual guides instead
            // of the old card-local offsets.
            bool starter = card.name == "StarterBundle";
            bool emperor = card.name == "EmperorBundle";
            if (titleRect != null && !recommended)
            {
                TextMeshProUGUI smallTitle = titleRect.GetComponent<TextMeshProUGUI>();
                if (smallTitle != null)
                {
                    smallTitle.text = starter ? "СТАРТОВЫЙ\nНАБОР" : "ИМПЕРАТОРСКИЙ\nНАБОР";
                }
            }

            SetLocalRect(
                titleRect,
                new Vector2(0f, recommended ? 108f : 76f),
                recommended ? new Vector2(card.rect.width - 70f, 40f) : new Vector2(300f, 58f));
            float iconColumnX = recommended ? -205f : (emperor ? -76f : -92f);
            float detailsColumnX = recommended ? 172f : (emperor ? 88f : 72f);
            float detailsWidth = recommended ? 300f : (emperor ? 150f : 190f);
            if (iconRect != null)
            {
                Image iconImage = iconRect.GetComponent<Image>();
                if (recommended && iconImage != null)
                {
                    Sprite openRoyalChest = Resources.Load<Sprite>("UI/Shop/royal_chest_open");
                    if (openRoyalChest != null)
                    {
                        iconImage.sprite = openRoyalChest;
                        iconImage.preserveAspect = true;
                        iconImage.color = Color.white;
                    }
                }

                SetLocalRect(
                    iconRect,
                    new Vector2(iconColumnX, recommended ? -5f : -4f),
                    recommended ? new Vector2(300f, 190f) : new Vector2(155f, 104f));
            }

            SetLocalRect(contentsRect, new Vector2(detailsColumnX, recommended ? 8f : 0f), new Vector2(detailsWidth, recommended ? 130f : 104f));
            if (rewardIconsRect != null)
            {
                rewardIconsRect.gameObject.SetActive(false);
            }
            if (premiumRect != null)
            {
                premiumRect.gameObject.SetActive(false);
            }
            PlacePriceButton(
                card,
                buyButton,
                recommended ? 330f : 258f,
                54f,
                recommended ? 42f : 38f,
                0f);

            ConfigureText(titleRect?.GetComponent<TextMeshProUGUI>(), recommended ? 27f : 20f, 18f, recommended ? 28f : 21f, Gold, FontStyles.Bold);
            TextMeshProUGUI cardTitle = titleRect?.GetComponent<TextMeshProUGUI>();
            if (cardTitle != null)
            {
                cardTitle.alignment = TextAlignmentOptions.Center;
                cardTitle.textWrappingMode = TextWrappingModes.NoWrap;
                cardTitle.overflowMode = TextOverflowModes.Overflow;
                if (!recommended)
                {
                    cardTitle.enableAutoSizing = false;
                    cardTitle.fontSize = 20f;
                    cardTitle.lineSpacing = -8f;
                }
            }
            TextMeshProUGUI contents = contentsRect?.GetComponent<TextMeshProUGUI>();
            ConfigureText(contents, recommended ? 20f : 17f, recommended ? 16f : 14f, recommended ? 21f : 18f, new Color(1f, 0.94f, 0.76f, 1f), FontStyles.Bold);
            if (contents != null)
            {
                contents.alignment = TextAlignmentOptions.MidlineLeft;
                contents.lineSpacing = 10f;
                contents.overflowMode = TextOverflowModes.Overflow;
            }
            ConfigureText(premiumRect?.GetComponent<TextMeshProUGUI>(), 14f, 12f, 15f, new Color(1f, 0.79f, 0.26f, 0.92f), FontStyles.Bold);
            ConfigureBuyButton(buyButton, 25f);

            if (recommended)
            {
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
            Vector2[] anchors =
            {
                new(0.25f, 0.75f),
                new(0.75f, 0.75f),
                new(0.25f, 0.25f),
                new(0.75f, 0.25f)
            };
            const float cardWidth = 400f;
            const float cardHeight = 212f;

            for (int index = 0; index < names.Length; index++)
            {
                RectTransform card = FindRect(container, names[index]);
                if (card == null)
                {
                    continue;
                }

                SetRelativeAnchor(card, anchors[index], new Vector2(cardWidth, cardHeight));
                ConfigureTreasurySurface(card, false);
                Image fill = card.Find("CardFill")?.GetComponent<Image>();
                if (fill != null)
                {
                    fill.gameObject.SetActive(false);
                }

                RectTransform offerTitle = FindRect(card, "OfferTitle");
                RectTransform offerIcon = FindRect(card, "Icon");
                if (index == 3 && offerIcon == null)
                {
                    offerIcon = FindRect(card, "IconBase");
                }
                RectTransform offerAmount = FindRect(card, "OfferAmount");
                RectTransform offerDetail = FindRect(card, "OfferDetail");
                RectTransform buyButton = FindRect(card, "BuyButton");
                if (index == 0)
                {
                    SetOfferText(offerDetail, "МОНЕТ");
                }
                if (index == 3)
                {
                    TextMeshProUGUI removeAdsTitle = offerTitle?.GetComponent<TextMeshProUGUI>();
                    if (removeAdsTitle != null)
                    {
                        removeAdsTitle.text = "БЕЗ РЕКЛАМЫ";
                    }

                    SetOfferText(offerAmount, "НАВСЕГДА");
                    SetOfferText(offerDetail, "БЕЗ БАННЕРОВ");
                }

                SetLocalRect(
                    offerTitle,
                    new Vector2(0f, 48f),
                    new Vector2(300f, 42f));
                float offerIconX = index == 3 ? -108f : -110f;
                Vector2 offerIconSize = index == 3 ? new Vector2(58f, 58f) : new Vector2(54f, 54f);
                SetLocalRect(offerIcon, new Vector2(offerIconX, -17f), offerIconSize);
                if (offerIcon != null)
                {
                    offerIcon.gameObject.SetActive(true);
                    if (index == 1)
                    {
                        ApplyGameplayHintIcon(offerIcon);
                    }
                }
                if (index == 3)
                {
                    ConfigureRemoveAdsIcon(card, offerIcon);
                }
                SetLocalRect(offerAmount, new Vector2(-35f, -5f), new Vector2(82f, 30f));
                SetLocalRect(offerDetail, new Vector2(-35f, -30f), new Vector2(88f, 18f));
                SetLocalRect(buyButton, new Vector2(74f, -17f), new Vector2(128f, 46f));

                ConfigureText(offerTitle?.GetComponent<TextMeshProUGUI>(), 18f, 17f, 18f, Gold, FontStyles.Bold);
                ConfigureText(offerAmount?.GetComponent<TextMeshProUGUI>(), 23f, 23f, 23f, new Color(1f, 0.94f, 0.74f, 1f), FontStyles.Bold);
                ConfigureText(offerDetail?.GetComponent<TextMeshProUGUI>(), 13f, 13f, 13f, new Color(1f, 0.9f, 0.7f, 0.9f), FontStyles.Bold);
                ConfigureBuyButton(buyButton, 19f);

                TextMeshProUGUI resourceTitle = offerTitle?.GetComponent<TextMeshProUGUI>();
                if (resourceTitle != null)
                {
                    resourceTitle.alignment = TextAlignmentOptions.Center;
                    resourceTitle.textWrappingMode = TextWrappingModes.Normal;
                    resourceTitle.enableAutoSizing = false;
                    resourceTitle.fontSize = 18f;
                    resourceTitle.lineSpacing = -6f;
                }

                TextMeshProUGUI amountText = offerAmount?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI detailText = offerDetail?.GetComponent<TextMeshProUGUI>();
                if (amountText != null)
                {
                    amountText.enableAutoSizing = false;
                    amountText.fontSize = index == 3 ? 17f : 23f;
                }
                if (detailText != null)
                {
                    detailText.enableAutoSizing = false;
                    detailText.fontSize = index == 3 ? 11f : 13f;
                }
            }
        }

        private static void ApplyGameplayHintIcon(RectTransform iconRect)
        {
            if (iconRect == null)
            {
                return;
            }

            Image icon = iconRect.GetComponent<Image>();
            if (icon == null)
            {
                return;
            }

#if UNITY_EDITOR
            icon.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/GameActions/VariantA/icon_hint_oracle_final.png");
#else
            // The authored scene keeps the same sprite reference in player builds.
#endif
            icon.preserveAspect = true;
            icon.type = Image.Type.Simple;
            icon.color = Color.white;
            icon.raycastTarget = false;
        }

        private static void SetOfferText(RectTransform rect, string value)
        {
            if (rect == null)
            {
                return;
            }

            rect.gameObject.SetActive(true);
            TextMeshProUGUI label = rect.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = value;
                RuntimeCyrillicFontProvider.Apply(label, value);
            }
        }

        private static void ConfigureRemoveAdsIcon(RectTransform card, RectTransform iconRect)
        {
            RectTransform iconBase = FindRect(card, "IconBase");
            RectTransform iconText = FindRect(card, "IconText");
            RectTransform slash = FindRect(card, "NoAdsSlash");
            RectTransform visualRoot = iconRect != null ? iconRect : iconBase;
            if (visualRoot == null)
            {
                return;
            }

            if (iconBase != null)
            {
                iconBase.gameObject.SetActive(true);
                SetLocalRect(iconBase, visualRoot.anchoredPosition, visualRoot.sizeDelta);
                Image baseImage = iconBase.GetComponent<Image>();
                if (baseImage != null)
                {
                    baseImage.preserveAspect = true;
                    baseImage.color = Color.white;
                    baseImage.raycastTarget = false;
                }
            }

            if (iconText != null)
            {
                iconText.gameObject.SetActive(true);
                SetLocalRect(iconText, visualRoot.anchoredPosition, visualRoot.sizeDelta - new Vector2(12f, 12f));
                TextMeshProUGUI label = iconText.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = "ADS";
                    ConfigureText(label, 19f, 16f, 20f, SoftGold, FontStyles.Bold);
                    label.alignment = TextAlignmentOptions.Center;
                    label.raycastTarget = false;
                    RuntimeCyrillicFontProvider.Apply(label, label.text);
                }
            }

            if (slash != null)
            {
                slash.gameObject.SetActive(true);
                SetLocalRect(slash, visualRoot.anchoredPosition, new Vector2(82f, 8f));
                slash.localRotation = Quaternion.Euler(0f, 0f, -42f);
                Image strike = slash.GetComponent<Image>();
                if (strike != null)
                {
                    strike.sprite = null;
                    strike.type = Image.Type.Simple;
                    strike.color = new Color(0.9f, 0.18f, 0.08f, 1f);
                    strike.raycastTarget = false;
                }
            }

            iconBase?.SetAsLastSibling();
            iconText?.SetAsLastSibling();
            slash?.SetAsLastSibling();
        }

        private static void ConfigureTreasurySurface(RectTransform card, bool hero)
        {
            if (card == null)
            {
                return;
            }

            Image surface = card.GetComponent<Image>() ?? card.gameObject.AddComponent<Image>();
            surface.sprite = Resources.Load<Sprite>(hero
                ? "UI/Shop/treasury_hero_niche"
                : "UI/Shop/treasury_card_niche");
            surface.type = Image.Type.Simple;
            surface.preserveAspect = false;
            surface.color = Color.white;
            surface.material = null;
            surface.enabled = true;
            surface.raycastTarget = false;

            Outline outline = card.GetComponent<Outline>() ?? card.gameObject.AddComponent<Outline>();
            outline.enabled = false;
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

            DisableHoverGlow(buttonRect);
            if (buttonRect.GetComponent<UIJuicyButton>() == null)
            {
                buttonRect.gameObject.AddComponent<UIJuicyButton>();
            }
        }

        private static void PlacePriceButton(
            RectTransform card,
            RectTransform button,
            float width,
            float height,
            float bottomInset,
            float x)
        {
            if (card == null || button == null)
            {
                return;
            }

            // One rule for every product card: the CTA baseline is derived from
            // the card's lower edge. This keeps prices stable if a card changes
            // size and prevents per-card hand-tuned vertical drift.
            float y = (-card.rect.height * 0.5f) + bottomInset + (height * 0.5f);
            SetLocalRect(button, new Vector2(x, y), new Vector2(width, height));
        }

        private static void DisableHoverGlow(RectTransform buttonRect)
        {
            RectTransform glowRect = buttonRect.Find("HoverGlow") as RectTransform;
            if (glowRect != null)
            {
                glowRect.gameObject.SetActive(false);
            }
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
            badge.anchoredPosition = new Vector2(-294f, 96f);
            badge.sizeDelta = new Vector2(188f, 34f);
            badge.localScale = Vector3.one;
            badgeImage.sprite = PremiumUISprites.CategoryLabel != null
                ? PremiumUISprites.CategoryLabel
                : PremiumUISprites.CleanGoldButton;
            badgeImage.type = Image.Type.Simple;
            badgeImage.color = new Color(0.98f, 0.78f, 0.22f, 0.96f);
            badgeImage.raycastTarget = false;

            Stretch(label.rectTransform, 10f, 10f, 3f, 3f);
            label.text = "ЛУЧШАЯ ЦЕНА";
            label.alignment = TextAlignmentOptions.Center;
            ConfigureText(label, 14f, 12f, 15f, new Color(0.16f, 0.075f, 0.012f, 1f), FontStyles.Bold);
            RuntimeCyrillicFontProvider.Apply(label, label.text);
            badge.SetAsLastSibling();
        }

        private void DisablePremiumMotion()
        {
            ShopPremiumMotion motion = GetComponent<ShopPremiumMotion>();
            if (motion != null)
            {
                motion.enabled = false;
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
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
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
            rect.localScale = Vector3.one;
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
            rect.localScale = Vector3.one;
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
