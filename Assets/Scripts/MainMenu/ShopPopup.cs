using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CardMesh
{
    /// <summary>
    /// Runtime behaviour for the scene-authored shop popup.
    /// Objects live in MainMenu.unity; ApplyUiPolish owns the runtime layout
    /// (header, scrollable product list, card internals) in reference units of
    /// the 1080x1920 canvas.
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

        // Layout guides in reference units (panel is 1020x1760, centre origin).
        private const string ScrollViewportName = "ShopScrollViewport";
        private const float ViewportTop = 446f;
        private const float ViewportBottom = -752f;
        private const float ContentWidth = 840f;
        private const float SectionLabelHeight = 48f;
        private const float BundleCardWidth = 820f;
        private const float HeroBundleHeight = 470f;
        private const float BundleHeight = 440f;
        private const float BundleGap = 24f;
        private const float ResourceCardWidth = 404f;
        private const float ResourceCardHeight = 330f;
        private const float ResourceGap = 16f;
        private const float ButtonLabelSize = 36f;

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
            RectTransform content = FindRect(panel, "ShopContent")
                ?? FindRect(panel, ScrollViewportName + "/ShopContent");
            if (content == null)
            {
                return;
            }

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

            // With the readable type scale (body >= 32, buttons >= 36 reference
            // units) the products no longer fit the fixed 1020x1760 frame, so the
            // product list scrolls inside the frame while the header stays fixed.
            // Every block is placed top-down in content space (y grows downward).
            // Starts 22 units down so the section label clears the mask's 20-unit edge fade.
            float cursor = 22f;
            PlaceFromTop(premiumTitle, cursor, new Vector2(800f, SectionLabelHeight));
            // Extra room under the label: the "ЛУЧШАЯ ЦЕНА" ribbon sits on the
            // hero card's top edge and must not touch the section label.
            cursor += SectionLabelHeight + 46f;

            float bundlesHeight = HeroBundleHeight + BundleGap + BundleHeight + BundleGap + BundleHeight;
            PlaceFromTop(premiumBundles, cursor, new Vector2(ContentWidth, bundlesHeight));
            cursor += bundlesHeight + 30f;

            PlaceFromTop(resourcesTitle, cursor, new Vector2(800f, SectionLabelHeight));
            cursor += SectionLabelHeight + 16f;

            float resourcesHeight = (ResourceCardHeight * 2f) + ResourceGap;
            PlaceFromTop(resources, cursor, new Vector2(ContentWidth, resourcesHeight));
            cursor += resourcesHeight + 28f;

            ConfigureScroll(content, cursor);
            ConfigurePremiumBundles(premiumBundles);
            ConfigureResourceOffers(resources);
            DisablePremiumMotion();
        }

        private void ConfigureScroll(RectTransform content, float contentHeight)
        {
            RectTransform viewport = content.parent as RectTransform;
            if (viewport == null || viewport.name != ScrollViewportName)
            {
                GameObject viewportObject = new(
                    ScrollViewportName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(RectMask2D),
                    typeof(ScrollRect));
                viewportObject.layer = content.gameObject.layer;
                RectTransform created = viewportObject.GetComponent<RectTransform>();
                int siblingIndex = content.GetSiblingIndex();
                created.SetParent(panel, false);
                created.SetSiblingIndex(siblingIndex);
                content.SetParent(created, false);
                viewport = created;
            }

            // Panel-space window between the fixed header and the frame's bottom
            // ornament; the inner frame border sits at x = +-418.
            SetLocalRect(
                viewport,
                new Vector2(0f, (ViewportTop + ViewportBottom) * 0.5f),
                new Vector2(ContentWidth, ViewportTop - ViewportBottom));

            // Invisible hit area so the list can be dragged from the gaps between cards.
            Image hitArea = GetOrAdd<Image>(viewport.gameObject);
            hitArea.sprite = null;
            hitArea.color = new Color(0f, 0f, 0f, 0f);
            hitArea.raycastTarget = true;

            RectMask2D mask = GetOrAdd<RectMask2D>(viewport.gameObject);
            mask.softness = new Vector2Int(0, 20);

            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(ContentWidth, contentHeight);
            content.anchoredPosition = Vector2.zero;
            content.localScale = Vector3.one;

            ScrollRect scroll = GetOrAdd<ScrollRect>(viewport.gameObject);
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.scrollSensitivity = 30f;
            scroll.horizontalScrollbar = null;
            scroll.verticalScrollbar = null;
            scroll.StopMovement();
        }

        private static void ConfigureHeader(RectTransform header)
        {
            if (header == null)
            {
                return;
            }

            // Header centre sits 500 units above the panel centre, under the
            // medallion's lower diamond (~575). Title caps end ~553, the subtitle
            // baseline is ~457 and the scroll window starts at 446.
            SetLocalRect(header, new Vector2(0f, 500f), new Vector2(840f, 140f));
            TextMeshProUGUI title = header.Find("Title")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI subtitle = header.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
            SetLocalRect(title?.rectTransform, new Vector2(0f, 32f), new Vector2(780f, 68f));
            SetLocalRect(subtitle?.rectTransform, new Vector2(0f, -32f), new Vector2(820f, 44f));
            ConfigureFixedText(title, 56f, Gold, FontStyles.Bold);
            if (subtitle != null)
            {
                // Shortened from "...усиливай свой прогресс." so it stays on one line at 32.
                subtitle.text = "Получай ресурсы и усиливай прогресс.";
                ConfigureFixedText(subtitle, 32f, new Color(1f, 0.92f, 0.72f, 0.94f), FontStyles.Bold);
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
            ConfigureText(treasuryCopy, 18f, 15f, 19f, new Color(1f, 0.9f, 0.7f, 0.82f), FontStyles.Bold);
            ConfigureFixedText(premiumTitle, 36f, Gold, FontStyles.Bold);
            ConfigureFixedText(resourcesTitle, 36f, Gold, FontStyles.Bold);
        }

        private static void ConfigurePremiumBundles(RectTransform container)
        {
            if (container == null)
            {
                return;
            }

            // Full-width cards stacked top-down: the hero first, then the two
            // smaller bundles. Side-by-side half-width cards cannot hold a 32-unit
            // item list next to the chest inside the niche columns.
            string[] names = { "RoyalBundle", "StarterBundle", "EmperorBundle" };
            float top = 0f;
            for (int index = 0; index < names.Length; index++)
            {
                bool hero = index == 0;
                float height = hero ? HeroBundleHeight : BundleHeight;
                RectTransform card = FindRect(container, names[index]);
                if (card != null)
                {
                    PlaceFromTop(card, top, new Vector2(BundleCardWidth, height));
                    ConfigurePremiumCard(card, hero);
                }

                top += height + BundleGap;
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

            bool starter = card.name == "StarterBundle";
            TextMeshProUGUI cardTitle = titleRect?.GetComponent<TextMeshProUGUI>();
            if (cardTitle != null)
            {
                cardTitle.text = recommended
                    ? "КОРОЛЕВСКИЙ НАБОР"
                    : (starter ? "СТАРТОВЫЙ НАБОР" : "ИМПЕРАТОРСКИЙ НАБОР");
            }

            // Card-local guides (centre origin). Hero (820x470, hero niche): top
            // diamond tip ~+158, inner columns at x = +-250, bottom ornament ~-171.
            // Small (820x440, card niche): arch top ~+151, columns at x = +-298,
            // ledge ~-163. Title / chest + list / price button are stacked so the
            // 32-unit list never meets a column or the button.
            if (recommended)
            {
                SetLocalRect(titleRect, new Vector2(0f, 118f), new Vector2(620f, 44f));
                SetLocalRect(iconRect, new Vector2(-190f, 4f), new Vector2(280f, 168f));
                SetLocalRect(contentsRect, new Vector2(95f, 6f), new Vector2(300f, 160f));
                SetLocalRect(buyButton, new Vector2(0f, -125f), new Vector2(340f, 70f));
            }
            else
            {
                SetLocalRect(titleRect, new Vector2(0f, 106f), new Vector2(640f, 44f));
                SetLocalRect(iconRect, new Vector2(-180f, -2f), new Vector2(216f, 145f));
                SetLocalRect(contentsRect, new Vector2(110f, 0f), new Vector2(300f, 160f));
                SetLocalRect(buyButton, new Vector2(0f, -125f), new Vector2(300f, 70f));
            }

            if (iconRect != null)
            {
                Image iconImage = iconRect.GetComponent<Image>();
                if (recommended && iconImage != null)
                {
                    Sprite openRoyalChest = Resources.Load<Sprite>("UI/Shop/royal_chest_open");
                    if (openRoyalChest != null)
                    {
                        iconImage.sprite = openRoyalChest;
                        iconImage.color = Color.white;
                    }
                }

                if (iconImage != null)
                {
                    iconImage.preserveAspect = true;
                }
            }

            if (rewardIconsRect != null)
            {
                rewardIconsRect.gameObject.SetActive(false);
            }
            if (premiumRect != null)
            {
                premiumRect.gameObject.SetActive(false);
            }

            ConfigureFixedText(cardTitle, 36f, Gold, FontStyles.Bold);

            TextMeshProUGUI contents = contentsRect?.GetComponent<TextMeshProUGUI>();
            ConfigureFixedText(contents, 32f, new Color(1f, 0.94f, 0.76f, 1f), FontStyles.Bold);
            if (contents != null)
            {
                // 4 lines x 1.172 em x 32 = 150 units inside a 160-unit rect.
                contents.alignment = TextAlignmentOptions.MidlineLeft;
            }

            ConfigureBuyButton(buyButton, ButtonLabelSize);

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
            float columnOffset = (ResourceCardWidth + ResourceGap) * 0.5f;

            // Card-local guides for the 404x330 card niche: fleur/arch top ~+113,
            // niche columns at x = +-147, ledge ~-122. Stack: title (32) /
            // icon + amount row (34) / caption (28) / price button (36).
            const float titleY = 74f;
            const float rowY = 30f;
            const float captionY = -22f;
            const float buttonY = -87f;
            const float iconSize = 56f;
            const float iconGap = 12f;
            const float maxAmountWidth = 200f;

            for (int index = 0; index < names.Length; index++)
            {
                RectTransform card = FindRect(container, names[index]);
                if (card == null)
                {
                    continue;
                }

                float x = index % 2 == 0 ? -columnOffset : columnOffset;
                float top = index < 2 ? 0f : ResourceCardHeight + ResourceGap;
                PlaceFromTop(card, top, new Vector2(ResourceCardWidth, ResourceCardHeight), x);
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

                TextMeshProUGUI titleText = offerTitle?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI amountText = offerAmount?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI detailText = offerDetail?.GetComponent<TextMeshProUGUI>();

                ConfigureFixedText(titleText, 32f, Gold, FontStyles.Bold);
                ConfigureFixedText(amountText, 34f, new Color(1f, 0.94f, 0.74f, 1f), FontStyles.Bold);
                ConfigureFixedText(detailText, 28f, new Color(1f, 0.9f, 0.7f, 0.9f), FontStyles.Bold);
                if (amountText != null)
                {
                    amountText.alignment = TextAlignmentOptions.MidlineLeft;
                    RuntimeCyrillicFontProvider.Apply(amountText, amountText.text);
                }

                SetLocalRect(offerTitle, new Vector2(0f, titleY), new Vector2(290f, 40f));
                SetLocalRect(offerDetail, new Vector2(0f, captionY), new Vector2(290f, 34f));

                // Icon + amount form one group centred on the card, so short
                // amounts ("20") and long ones ("НАВСЕГДА") are both centred.
                float amountWidth = MeasurePreferredWidth(amountText, maxAmountWidth);
                float groupLeft = -(iconSize + iconGap + amountWidth) * 0.5f;
                SetLocalRect(offerIcon, new Vector2(groupLeft + (iconSize * 0.5f), rowY), new Vector2(iconSize, iconSize));
                SetLocalRect(
                    offerAmount,
                    new Vector2(groupLeft + iconSize + iconGap + ((amountWidth + 8f) * 0.5f), rowY),
                    new Vector2(amountWidth + 8f, 44f));
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

                SetLocalRect(buyButton, new Vector2(0f, buttonY), new Vector2(200f, 70f));
                ConfigureBuyButton(buyButton, ButtonLabelSize);
            }
        }

        private static float MeasurePreferredWidth(TextMeshProUGUI text, float maxWidth)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
            {
                return 0f;
            }

            float width = text.GetPreferredValues(text.text, 10000f, 10000f).x;
            if (float.IsNaN(width) || width <= 1f)
            {
                // Font not ready: rough Roboto Bold average incl. bold spacing.
                width = text.text.Length * text.fontSize * 0.68f;
            }

            return Mathf.Min(width, maxWidth);
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
                SetLocalRect(slash, visualRoot.anchoredPosition, new Vector2(visualRoot.sizeDelta.x * 1.4f, 8f));
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
                // Fixed size (no autosize shrink): the button art's gold rim is
                // ~9 units at a 70-unit height, so a 36-unit label keeps >= 14
                // units of clearance on every side.
                Stretch(label.rectTransform, 24f, 24f, 9f, 9f);
                ConfigureFixedText(label, priceSize, SoftGold, FontStyles.Bold);
            }

            DisableHoverGlow(buttonRect);
            if (buttonRect.GetComponent<UIJuicyButton>() == null)
            {
                buttonRect.gameObject.AddComponent<UIJuicyButton>();
            }
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
            // Ribbon straddling the hero card's top edge, left of the centre
            // diamond and clear of the corner jewel (x <= -369) and the title.
            badge.anchoredPosition = new Vector2(-205f, card.rect.height * 0.5f);
            badge.sizeDelta = new Vector2(320f, 62f);
            badge.localScale = Vector3.one;
            badgeImage.sprite = PremiumUISprites.CategoryLabel != null
                ? PremiumUISprites.CategoryLabel
                : PremiumUISprites.CleanGoldButton;
            badgeImage.type = Image.Type.Simple;
            badgeImage.color = new Color(0.98f, 0.78f, 0.22f, 0.96f);
            badgeImage.raycastTarget = false;

            // The pill art has ~12/8 units of glow margin; 32/10 insets keep the
            // 28-unit caption >= 14 units inside the visible gold pill.
            Stretch(label.rectTransform, 32f, 32f, 10f, 10f);
            label.text = "ЛУЧШАЯ ЦЕНА";
            ConfigureFixedText(label, 28f, new Color(0.16f, 0.075f, 0.012f, 1f), FontStyles.Bold);
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

        private static void ConfigureFixedText(TextMeshProUGUI text, float size, Color color, FontStyles style)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = false;
            text.fontSize = size;
            text.fontSizeMin = size;
            text.fontSizeMax = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = 0f;
            text.characterSpacing = 0f;
            text.margin = Vector4.zero;
        }

        private static RectTransform FindRect(Transform root, string childName)
        {
            return root?.Find(childName) as RectTransform;
        }

        // Places a rect whose parent is laid out top-down (y grows downward from
        // the parent's top edge). The pivot stays centred so press feedback
        // scales cards around their middle.
        private static void PlaceFromTop(RectTransform rect, float top, Vector2 size, float x = 0f)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, -(top + (size.y * 0.5f)));
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
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
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
