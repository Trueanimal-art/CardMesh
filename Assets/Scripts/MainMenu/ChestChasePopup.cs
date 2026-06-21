using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    [ExecuteAlways]
    public sealed class ChestChasePopup : MonoBehaviour
    {
        private static readonly Color TextDark = new(0.16f, 0.09f, 0.03f, 1f);
        private static readonly Color TextCream = new(1f, 0.95f, 0.76f, 1f);
        private static readonly Color TextGold = new(1f, 0.82f, 0.24f, 1f);
        private static readonly Color PanelGreen = new(0.025f, 0.17f, 0.085f, 0.9f);
        private static readonly Vector2 PanelLandscapeSize = new(1600f, 930f);
        private const float HeaderLogoY = -50f;
        private const float HeaderLogoSize = 190f;
        private const float TitleY = -190f;
        private const float DescriptionY = -272f;
        private const float TimerY = -382f;
        private const float HeroY = -454f;
        private const float HeroRewardY = -10f;
        private const float ProgressBadgeY = -112f;
        private const float ProgressLabelY = -598f;
        private const float ProgressTrackY = -646f;
        private const float TooltipY = 308f;
        private const float StageRowY = 178f;

        private ChestChaseManager manager;
        private CanvasGroup canvasGroup;
        private RectTransform panel;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI progressCountText;
        private TextMeshProUGUI progressLabelText;
        private Image progressFill;
        private RectTransform progressTrack;
        private RectTransform stageRow;
        private RectTransform tooltip;
        private TextMeshProUGUI tooltipTitleText;
        private TextMeshProUGUI tooltipCoinText;
        private TextMeshProUGUI tooltipHintText;
        private TextMeshProUGUI tooltipUndoText;
        private TextMeshProUGUI rewardToastText;
        private Coroutine timerRoutine;
        private bool isShowing;
#if UNITY_EDITOR
        private bool editorRefreshQueued;
#endif

        [Header("Shared prefab references")]
        [SerializeField] private PremiumEventPrefabReferences sharedPrefabs = new();

        public static ChestChasePopup GetOrCreate()
        {
            RuntimeCyrillicFontProvider.WarmUp("КОРОЛЕВСКАЯ СЕРИЯ Побеждай уровни подряд и получай всё больше наград. При проигрыше серия сбрасывается. Побед подряд Монеты Подсказки Отмены хода");

            ChestChasePopup existing = FindFirstObjectByType<ChestChasePopup>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.EnsureLayout();
                return existing;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                GameObject canvasObject = new("ChestChaseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080f, 1920f);
                scaler.matchWidthOrHeight = 1f;
            }

            GameObject root = new("ChestChasePopup", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            Stretch(rect);

            ChestChasePopup popup = root.AddComponent<ChestChasePopup>();
            popup.BuildLayout();
            root.SetActive(false);
            return popup;
        }

        public void Show(ChestChaseManager targetManager)
        {
            isShowing = true;
            EnsureLayout();
            manager = targetManager != null ? targetManager : ChestChaseManager.GetOrCreate(this);

            if (manager != null)
            {
                manager.StateChanged -= Refresh;
                manager.StateChanged += Refresh;
                manager.RewardClaimed -= ShowRewardToast;
                manager.RewardClaimed += ShowRewardToast;
            }

            Refresh();
            DisableExternalGlow(transform);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            StartShowAnimation();

            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
            }

            timerRoutine = StartCoroutine(UpdateTimer());
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                sharedPrefabs.AssignDefaultsIfMissing();
#endif
                return;
            }

            EnsureLayout();
            if (!isShowing)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                sharedPrefabs.AssignDefaultsIfMissing();
#endif
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                sharedPrefabs.AssignDefaultsIfMissing();
                QueueEditorRefresh();
            }
        }
#endif

        private void OnDisable()
        {
            isShowing = false;
            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
        }

        private void EnsureLayout()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            Image overlay = GetComponent<Image>();
            if (overlay == null)
            {
                overlay = gameObject.AddComponent<Image>();
            }

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                Stretch(rect);
            }

            panel = transform.Find("Panel") as RectTransform;
            if (panel == null)
            {
#if UNITY_EDITOR
                sharedPrefabs.AssignDefaultsIfMissing();
#endif
                BuildLayout();
            }

            CacheLayoutReferences();
            UpgradeSharedVisuals();
        }

#if UNITY_EDITOR
        public void MaterializeAuthoringLayout()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureLayout();
            CacheLayoutReferences();
            ApplyFontsToAllTexts();
            RefreshEditorPreview();
        }

        private void QueueEditorRefresh()
        {
            sharedPrefabs.AssignDefaultsIfMissing();
            if (editorRefreshQueued)
            {
                return;
            }

            editorRefreshQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                    return;
                }

                editorRefreshQueued = false;
                if (Application.isPlaying)
                {
                    return;
                }

                EnsureLayout();
            };
        }
#endif

        private void UpgradeSharedVisuals()
        {
            if (panel != null)
            {
                Image image = panel.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = RoyalEventSprites.Panel != null ? RoyalEventSprites.Panel : PremiumUISprites.EmeraldPanel;
                    image.type = Image.Type.Sliced;
                    image.color = Color.white;
                }

                Shadow shadow = panel.GetComponent<Shadow>() ?? panel.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(1f, 0.68f, 0.14f, 0f);
                shadow.effectDistance = Vector2.zero;
            }

            StyleSharedButton(transform.Find("Panel/CloseButton") as RectTransform, "X", new Vector2(86f, 86f), new Vector2(-72f, -70f), true);
            StyleSharedButton(transform.Find("Panel/InfoButton") as RectTransform, "i", new Vector2(66f, 66f), new Vector2(76f, -78f), false);
            NormalizeLayoutPositions();
            StyleHeaderLogo();
            UpgradeCrownHero();
            DisableDuplicateCrowns(transform);
            DisableExternalGlow(transform);
        }

        private void NormalizeLayoutPositions()
        {
            if (panel == null)
            {
                return;
            }

            bool landscapeLayout = panel.sizeDelta.x > panel.sizeDelta.y;
            panel.sizeDelta = landscapeLayout ? PanelLandscapeSize : panel.sizeDelta;

            RectTransform title = transform.Find("Panel/Title") as RectTransform;
            if (title != null)
            {
                SetAnchored(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(980f, 74f), new Vector2(0f, TitleY));
                TMP_Text text = title.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.fontSizeMax = 54f;
                    text.fontSizeMin = 34f;
                }
            }

            RectTransform description = transform.Find("Panel/Description") as RectTransform;
            if (description != null)
            {
                SetAnchored(description, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(980f, 58f), new Vector2(0f, DescriptionY));
                TMP_Text text = description.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.fontSizeMax = 23f;
                    text.fontSizeMin = 17f;
                    text.lineSpacing = -8f;
                }
            }

            SetAnchored(transform.Find("Panel/Timer") as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(230f, 54f), new Vector2(0f, TimerY));
            SetAnchored(transform.Find("Panel/ProgressLabel") as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(440f, 46f), new Vector2(0f, landscapeLayout ? ProgressLabelY : -646f));
            TMP_Text progressLabel = transform.Find("Panel/ProgressLabel")?.GetComponent<TMP_Text>();
            if (progressLabel != null)
            {
                progressLabel.fontSizeMax = 28f;
                progressLabel.fontSizeMin = 18f;
            }

            SetAnchored(transform.Find("Panel/ProgressBack") as RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(landscapeLayout ? 1050f : 720f, landscapeLayout ? 58f : 52f), new Vector2(0f, landscapeLayout ? ProgressTrackY : -716f));
            SetAnchored(transform.Find("Panel/StageRow") as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(landscapeLayout ? 1210f : 840f, landscapeLayout ? 268f : 246f), new Vector2(0f, landscapeLayout ? StageRowY : 218f));
            SetAnchored(transform.Find("Panel/RewardTooltip") as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(landscapeLayout ? 292f : 340f, landscapeLayout ? 78f : 108f), new Vector2(0f, landscapeLayout ? TooltipY : 348f));
        }

        private void StyleHeaderLogo()
        {
            RectTransform headerLogo = transform.Find("Panel/HeaderLogo") as RectTransform;
            if (panel == null)
            {
                return;
            }

            if (headerLogo == null)
            {
                headerLogo = CreateRect("HeaderLogo", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(HeaderLogoSize, HeaderLogoSize), new Vector2(0f, HeaderLogoY));
                headerLogo.gameObject.AddComponent<Image>();
            }

            SetAnchored(headerLogo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(HeaderLogoSize, HeaderLogoSize), new Vector2(0f, HeaderLogoY));
            Image image = headerLogo.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = GetHeaderLogoSprite();
                image.preserveAspect = true;
                image.color = Color.white;
                image.raycastTarget = false;
            }

            foreach (Transform child in headerLogo)
            {
                child.gameObject.SetActive(false);
            }

            headerLogo.SetAsLastSibling();
        }

        private void UpgradeCrownHero()
        {
            RectTransform hero = transform.Find("Panel/Hero") as RectTransform;
            if (hero == null)
            {
                return;
            }

            SetAnchored(hero, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(650f, 210f), new Vector2(0f, HeroY));

            Image heroImage = hero.GetComponent<Image>();
            if (heroImage != null)
            {
                heroImage.sprite = null;
                heroImage.color = new Color(1f, 1f, 1f, 0f);
                heroImage.raycastTarget = false;
            }

            RectTransform goldFrame = EnsureHeroImage(hero, "IntegratedGoldFrame", PremiumUISprites.CleanGoldButton != null ? PremiumUISprites.CleanGoldButton : PremiumUISprites.PrimaryButton, new Color(1f, 0.82f, 0.24f, 0.78f), new Vector2(0f, HeroRewardY), new Vector2(536f, 92f), Image.Type.Sliced);
            goldFrame.SetAsFirstSibling();

            RectTransform backplate = EnsureHeroImage(hero, "IntegratedBackplate", PremiumUISprites.EmeraldPanel, new Color(0.035f, 0.23f, 0.105f, 0.98f), new Vector2(0f, HeroRewardY), new Vector2(502f, 66f), Image.Type.Sliced);
            backplate.SetSiblingIndex(1);

            RectTransform softHalo = EnsureHeroImage(hero, "SoftGoldHalo", PremiumUISprites.IconCircle, new Color(1f, 0.72f, 0.12f, 0f), new Vector2(0f, 10f), new Vector2(500f, 260f), Image.Type.Simple);
            softHalo.gameObject.SetActive(false);
            softHalo.SetSiblingIndex(1);

            RectTransform leftCard = EnsureHeroImage(hero, "RearCardLeft", RoyalEventSprites.RewardCard, new Color(0.82f, 0.95f, 0.76f, 0.16f), new Vector2(-158f, -4f), new Vector2(90f, 118f), Image.Type.Sliced);
            leftCard.localRotation = Quaternion.Euler(0f, 0f, -8f);
            leftCard.gameObject.SetActive(false);
            leftCard.SetSiblingIndex(2);

            RectTransform rightCard = EnsureHeroImage(hero, "RearCardRight", RoyalEventSprites.RewardCard, new Color(0.82f, 0.95f, 0.76f, 0.16f), new Vector2(158f, -4f), new Vector2(90f, 118f), Image.Type.Sliced);
            rightCard.localRotation = Quaternion.Euler(0f, 0f, 8f);
            rightCard.gameObject.SetActive(false);
            rightCard.SetSiblingIndex(3);

            RectTransform crownArt = EnsureHeroImage(hero, "CrownHeroArt", GetHeaderLogoSprite(), new Color(1f, 1f, 1f, 0f), new Vector2(0f, 64f), new Vector2(96f, 96f), Image.Type.Simple);
            Image crownImage = crownArt.GetComponent<Image>();
            if (crownImage != null)
            {
                crownImage.sprite = GetHeaderLogoSprite();
                crownImage.preserveAspect = true;
                crownImage.color = new Color(1f, 1f, 1f, 0f);
                crownImage.raycastTarget = false;
            }
            crownArt.gameObject.SetActive(false);
            crownArt.SetSiblingIndex(5);

            CreateGoalRewardContent(hero);

            RectTransform pedestal = hero.Find("ProgressPedestal") as RectTransform;
            if (pedestal != null)
            {
                pedestal.gameObject.SetActive(false);
            }

            RectTransform progressBadge = hero.Find("ProgressBadge") as RectTransform;
            if (progressBadge != null)
            {
                SetAnchored(progressBadge, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(320f, 76f), new Vector2(0f, ProgressBadgeY));
                Image badgeImage = progressBadge.GetComponent<Image>();
                if (badgeImage != null)
                {
                    badgeImage.sprite = PremiumUISprites.CleanGoldButton != null ? PremiumUISprites.CleanGoldButton : PremiumUISprites.PrimaryButton;
                    badgeImage.type = badgeImage.sprite != null && badgeImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
                    badgeImage.color = new Color(1f, 0.9f, 0.42f, 0.98f);
                    badgeImage.raycastTarget = false;
                }

                progressBadge.SetAsLastSibling();
                TMP_Text countText = progressBadge.Find("ProgressCount")?.GetComponent<TMP_Text>();
                if (countText != null)
                {
                    countText.fontSizeMax = 50f;
                    countText.fontSizeMin = 30f;
                    SetAnchored(countText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-36f, -8f), new Vector2(18f, 4f));
                }

                Shadow shadow = progressBadge.GetComponent<Shadow>();
                if (shadow != null)
                {
                    shadow.effectColor = new Color(0f, 0f, 0f, 0f);
                    shadow.effectDistance = Vector2.zero;
                }
            }
        }

        private void CacheLayoutReferences()
        {
            panel = transform.Find("Panel") as RectTransform;
            timerText = FindTMP("Panel/Timer/TimerText");
            progressCountText = FindTMP("Panel/Hero/ProgressBadge/ProgressCount");
            progressLabelText = FindTMP("Panel/ProgressLabel");
            progressFill = transform.Find("Panel/ProgressBack/ProgressFill")?.GetComponent<Image>();
            progressTrack = transform.Find("Panel/ProgressBack") as RectTransform;
            stageRow = transform.Find("Panel/StageRow") as RectTransform;
            tooltip = transform.Find("Panel/RewardTooltip") as RectTransform;
            tooltipTitleText = FindTMP("Panel/RewardTooltip/Title");
            tooltipCoinText = FindTMP("Panel/RewardTooltip/Coins/Text");
            tooltipHintText = FindTMP("Panel/RewardTooltip/Hints/Text");
            tooltipUndoText = FindTMP("Panel/RewardTooltip/Undos/Text");
            rewardToastText = FindTMP("Panel/RewardToast");
            BindButton("Panel/CloseButton", Close);
            BindButton("Panel/InfoButton", ShowRules);
        }

        private static void CreateGoalRewardContent(RectTransform hero)
        {
            if (hero == null)
            {
                return;
            }

            TextMeshProUGUI title = hero.Find("GoalRewardTitle")?.GetComponent<TextMeshProUGUI>();
            if (title == null)
            {
                title = CreateText(hero, "GoalRewardTitle", "НАГРАДА ЗА ЦЕЛЬ", 20f, TextGold, TextAlignmentOptions.Center);
            }

            title.fontStyle = FontStyles.Bold;
            title.fontSizeMax = 22f;
            title.fontSizeMin = 17f;
            SetAnchored(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 30f), new Vector2(0f, 33f));
            AddTextOutline(title, new Color(0.16f, 0.05f, 0f, 0.55f), new Vector2(1f, -1f));

            CreateGoalRewardRow(hero, "GoalCoins", RoyalEventSprites.CoinIcon, "x300", new Vector2(-164f, -14f));
            CreateGoalRewardRow(hero, "GoalHints", PremiumUISprites.HintBulbIcon, "x1", new Vector2(0f, -14f));
            CreateGoalRewardRow(hero, "GoalUndos", PremiumUISprites.UndoIcon, "-", new Vector2(164f, -14f));
        }

        private static void CreateGoalRewardRow(RectTransform parent, string name, Sprite iconSprite, string value, Vector2 position)
        {
            RectTransform row = parent.Find(name) as RectTransform;
            if (row == null)
            {
                row = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(142f, 42f), position);
            }

            SetAnchored(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(142f, 42f), position);

            Image icon = row.Find("Icon")?.GetComponent<Image>();
            if (icon == null)
            {
                icon = CreateRect("Icon", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f), new Vector2(24f, 0f)).gameObject.AddComponent<Image>();
            }
            else
            {
                SetAnchored(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f), new Vector2(24f, 0f));
            }

            icon.sprite = iconSprite != null ? iconSprite : PremiumUISprites.IconCircle;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI text = row.Find("Value")?.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = CreateText(row, "Value", value, 24f, TextCream, TextAlignmentOptions.Left);
            }

            SetText(text, value);
            text.fontStyle = FontStyles.Bold;
            text.fontSizeMax = 28f;
            text.fontSizeMin = 18f;
            SetAnchored(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(-48f, 0f), new Vector2(48f, 0f));
            AddTextOutline(text, new Color(0f, 0.02f, 0f, 0.55f), new Vector2(1f, -1f));
        }

        private TextMeshProUGUI FindTMP(string path)
        {
            return transform.Find(path)?.GetComponent<TextMeshProUGUI>();
        }

        private void BindButton(string path, UnityAction action)
        {
            Button button = transform.Find(path)?.GetComponent<Button>();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void BuildLayout()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            Image overlay = GetComponent<Image>();
            overlay.color = new Color(0f, 0.035f, 0.019f, 0.9f);
            overlay.raycastTarget = true;

            Vector2 panelSize = CalculatePanelSize();
            bool landscapeLayout = panelSize.x > panelSize.y;
            panel = CreateRect("Panel", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), panelSize, new Vector2(0f, 10f));
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = RoyalEventSprites.Panel != null ? RoyalEventSprites.Panel : PremiumUISprites.EmeraldPanel;
            panelImage.type = panelImage.sprite != null && panelImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            panelImage.color = Color.white;
            AddShadow(panel.gameObject, new Color(1f, 0.68f, 0.14f, 0f), Vector2.zero);

            Image headerLogo = CreateRect("HeaderLogo", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(132f, 132f), new Vector2(0f, HeaderLogoY)).gameObject.AddComponent<Image>();
            headerLogo.sprite = GetHeaderLogoSprite();
            headerLogo.preserveAspect = true;
            headerLogo.color = Color.white;
            headerLogo.raycastTarget = false;

            TextMeshProUGUI title = CreateText(panel, "Title", "КОРОЛЕВСКАЯ СЕРИЯ", 54f, TextGold, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(980f, 74f), new Vector2(0f, TitleY));
            AddTextOutline(title, new Color(0.25f, 0.1f, 0f, 0.62f), new Vector2(1.5f, -1.5f));
            AddTextShadow(title.gameObject, new Color(1f, 0.72f, 0.12f, 0f), Vector2.zero);

            TextMeshProUGUI description = CreateText(panel, "Description", "Побеждай уровни подряд и получай всё больше наград.\nПри проигрыше серия сбрасывается.", 23f, TextCream, TextAlignmentOptions.Center);
            description.fontStyle = FontStyles.Bold;
            description.lineSpacing = -8f;
            SetAnchored(description.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(980f, 58f), new Vector2(0f, DescriptionY));
            AddTextOutline(description, new Color(0f, 0.02f, 0f, 0.62f), new Vector2(1f, -1f));

            Button closeButton = CreateCircleButton(panel, "CloseButton", "X", new Color(0.95f, 0.58f, 0.05f, 1f), TextDark, Close);
            SetAnchored(closeButton.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(84f, 84f), new Vector2(-70f, -64f));

            Button infoButton = CreateCircleButton(panel, "InfoButton", "i", new Color(0.06f, 0.31f, 0.14f, 1f), TextGold, ShowRules);
            SetAnchored(infoButton.transform as RectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f), new Vector2(82f, -82f));

            RectTransform timer = CreateRect("Timer", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(230f, 54f), new Vector2(0f, TimerY));
            Image timerImage = timer.gameObject.AddComponent<Image>();
            timerImage.sprite = PremiumUISprites.CleanGoldButton != null ? PremiumUISprites.CleanGoldButton : (PremiumUISprites.PrimaryButton != null ? PremiumUISprites.PrimaryButton : RoyalEventSprites.Tooltip);
            timerImage.type = timerImage.sprite != null && timerImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            timerImage.color = Color.white;
            timerText = CreateText(timer, "TimerText", string.Empty, 30f, TextDark, TextAlignmentOptions.Center);
            timerText.fontStyle = FontStyles.Bold;
            Stretch(timerText.rectTransform, 16f, 16f, 0f, 1f);

            RectTransform hero = CreateRect("Hero", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), landscapeLayout ? new Vector2(650f, 210f) : new Vector2(690f, 260f), new Vector2(0f, landscapeLayout ? HeroY : -456f));
            Image heroImage = hero.gameObject.AddComponent<Image>();
            heroImage.sprite = RoyalEventSprites.Hero;
            heroImage.color = new Color(0.98f, 0.96f, 0.88f, 0.94f);
            heroImage.raycastTarget = false;
            AddPulse(hero.gameObject, 0.006f, 0f, 0.85f, 0f);

            RectTransform progressBadge = CreateRect("ProgressBadge", hero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(320f, 76f), new Vector2(0f, ProgressBadgeY));
            Image badgeImage = progressBadge.gameObject.AddComponent<Image>();
            badgeImage.sprite = PremiumUISprites.CleanGoldButton != null ? PremiumUISprites.CleanGoldButton : PremiumUISprites.PrimaryButton;
            badgeImage.type = badgeImage.sprite != null && badgeImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            badgeImage.color = new Color(1f, 0.9f, 0.42f, 0.98f);
            badgeImage.raycastTarget = false;
            AddShadow(progressBadge.gameObject, new Color(0f, 0f, 0f, 0f), Vector2.zero);

            progressCountText = CreateText(progressBadge, "ProgressCount", string.Empty, 50f, TextCream, TextAlignmentOptions.Center);
            progressCountText.fontStyle = FontStyles.Bold;
            progressCountText.fontSizeMin = 30f;
            Stretch(progressCountText.rectTransform, 18f, 18f, 0f, 8f);
            AddTextOutline(progressCountText, new Color(0f, 0.02f, 0f, 0.62f), new Vector2(1.4f, -1.4f));

            progressLabelText = CreateText(panel, "ProgressLabel", "ПОБЕД ПОДРЯД", 28f, TextCream, TextAlignmentOptions.Center);
            progressLabelText.fontStyle = FontStyles.Bold;
            SetAnchored(progressLabelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(440f, 46f), new Vector2(0f, landscapeLayout ? ProgressLabelY : -646f));
            AddTextOutline(progressLabelText, new Color(0f, 0.02f, 0f, 0.5f), new Vector2(1f, -1f));

            float progressWidth = landscapeLayout ? 1050f : 720f;
            RectTransform progressBack = CreateRect("ProgressBack", panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(progressWidth, landscapeLayout ? 58f : 52f), new Vector2(0f, landscapeLayout ? ProgressTrackY : -716f));
            progressTrack = progressBack;
            Image progressBackImage = progressBack.gameObject.AddComponent<Image>();
            progressBackImage.sprite = RoyalEventSprites.ProgressBack != null ? RoyalEventSprites.ProgressBack : PremiumUISprites.HudCounterGold;
            progressBackImage.type = progressBackImage.sprite != null && progressBackImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            progressBackImage.color = Color.white;

            progressFill = CreateRect("ProgressFill", progressBack, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, landscapeLayout ? 48f : 42f), Vector2.zero).gameObject.AddComponent<Image>();
            progressFill.sprite = RoyalEventSprites.ProgressFill != null ? RoyalEventSprites.ProgressFill : PremiumUISprites.SecondaryButton;
            progressFill.type = progressFill.sprite != null && progressFill.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            progressFill.color = Color.white;

            CreateProgressMilestones(progressBack);

            CreateSparkles(panel, landscapeLayout);

            stageRow = CreateRect("StageRow", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), landscapeLayout ? new Vector2(1210f, 268f) : new Vector2(840f, 246f), new Vector2(0f, landscapeLayout ? StageRowY : 218f));

            tooltip = CreateRect("RewardTooltip", panel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), landscapeLayout ? new Vector2(292f, 78f) : new Vector2(340f, 108f), new Vector2(0f, landscapeLayout ? TooltipY : 348f));
            Image tooltipImage = tooltip.gameObject.AddComponent<Image>();
            tooltipImage.sprite = RoyalEventSprites.Tooltip != null ? RoyalEventSprites.Tooltip : PremiumUISprites.CreamPanel;
            tooltipImage.type = tooltipImage.sprite != null && tooltipImage.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            tooltipImage.color = Color.white;
            AddShadow(tooltip.gameObject, new Color(0.1f, 0.04f, 0f, 0f), Vector2.zero);

            tooltipTitleText = CreateText(tooltip, "Title", "Награда", 18f, TextDark, TextAlignmentOptions.Center);
            tooltipTitleText.fontStyle = FontStyles.Bold;
            SetAnchored(tooltipTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(276f, 24f), new Vector2(0f, -12f));
            tooltipCoinText = CreateTooltipRow(tooltip, "Coins", RoyalEventSprites.CoinIcon, new Vector2(-94f, -55f));
            tooltipHintText = CreateTooltipRow(tooltip, "Hints", PremiumUISprites.HintBulbIcon, new Vector2(0f, -55f));
            tooltipUndoText = CreateTooltipRow(tooltip, "Undos", PremiumUISprites.UndoIcon, new Vector2(94f, -55f));

            rewardToastText = CreateText(panel, "RewardToast", string.Empty, 28f, TextGold, TextAlignmentOptions.Center);
            rewardToastText.fontStyle = FontStyles.Bold;
            SetAnchored(rewardToastText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(720f, 64f), new Vector2(0f, 72f));
            AddTextOutline(rewardToastText, new Color(0f, 0.02f, 0f, 0.55f), new Vector2(1.2f, -1.2f));
            rewardToastText.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (manager == null)
            {
                return;
            }

            int required = Mathf.Max(1, manager.KeysRequired);
            int current = Mathf.Clamp(manager.CurrentKeys, 0, required);

            SetText(progressCountText, $"{current}/{required}");
            SetText(progressLabelText, "ПОБЕД ПОДРЯД");

            if (progressFill != null)
            {
                RectTransform track = progressFill.transform.parent as RectTransform;
                float trackWidth = track != null ? Mathf.Max(track.rect.width, track.sizeDelta.x) : 720f;
                float trackHeight = track != null ? Mathf.Max(track.rect.height, track.sizeDelta.y) : 62f;
                float width = Mathf.Lerp(10f, Mathf.Max(10f, trackWidth - 12f), current / (float)required);
                progressFill.rectTransform.sizeDelta = new Vector2(width, Mathf.Max(28f, trackHeight - 8f));
            }

            RefreshProgressMarkers(current, required);
            RebuildStages();
            if (tooltip != null)
            {
                tooltip.gameObject.SetActive(false);
            }
            UpdateTimerText();
            DisableExternalGlow(transform);
        }

        private void RebuildStages()
        {
            if (stageRow == null || manager == null)
            {
                return;
            }

            foreach (Transform child in stageRow)
            {
                Destroy(child.gameObject);
            }

            IReadOnlyList<ChestChaseRewardTier> tiers = manager.Tiers;
            int count = tiers != null ? tiers.Count : 0;
            if (count <= 0)
            {
                return;
            }

            float rowWidth = Mathf.Max(stageRow.rect.width, stageRow.sizeDelta.x);
            bool wide = rowWidth > 1000f;
            Vector2 stageSize = wide ? new Vector2(210f, 260f) : new Vector2(168f, 230f);
            float usableWidth = wide ? Mathf.Min(1080f, rowWidth - 130f) : 700f;
            float step = count > 1 ? usableWidth / (count - 1) : 0f;
            float startX = -step * (count - 1) * 0.5f;
            for (int index = 0; index < count; index++)
            {
                int stageIndex = index;
                RectTransform stage = CreateRect($"Stage_{index + 1}", stageRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), stageSize, new Vector2(startX + (step * index), 0f));
                Button button = stage.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => ShowTooltipForStage(stageIndex));
                if (button.GetComponent<UIJuicyButton>() == null)
                {
                    button.gameObject.AddComponent<UIJuicyButton>();
                }

                bool isCompleted = index < manager.CurrentStage;
                bool isCurrent = index == manager.CurrentStage;
                CreateRewardStageCard(stage, tiers[index], index + 1, isCompleted, isCurrent);
            }
        }

        private void ShowTooltipForStage(int stageIndex)
        {
            if (manager == null || tooltip == null)
            {
                return;
            }

            ChestChaseRewardTier reward = manager.GetTier(stageIndex);
            if (reward == null)
            {
                tooltip.gameObject.SetActive(false);
                return;
            }

            tooltip.gameObject.SetActive(true);
            SetText(tooltipTitleText, "Награда");
            SetText(tooltipCoinText, $"x{reward.Coins}");
            SetText(tooltipHintText, $"x{reward.Hints}");
            SetText(tooltipUndoText, reward.Undos > 0 ? $"x{reward.Undos}" : "-");

            IReadOnlyList<ChestChaseRewardTier> tiers = manager.Tiers;
            int count = tiers != null ? Mathf.Max(1, tiers.Count) : 1;
            float normalized = count <= 1 ? 0.5f : stageIndex / (float)(count - 1);
            float rowWidth = stageRow != null ? Mathf.Max(stageRow.rect.width, stageRow.sizeDelta.x) : 820f;
            float x = Mathf.Lerp(rowWidth > 1000f ? -480f : -306f, rowWidth > 1000f ? 480f : 306f, normalized);
            tooltip.anchoredPosition = new Vector2(x, tooltip.anchoredPosition.y);
        }

        private IEnumerator UpdateTimer()
        {
            while (gameObject.activeSelf)
            {
                UpdateTimerText();
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        private void UpdateTimerText()
        {
            if (timerText == null || manager == null)
            {
                return;
            }

            TimeSpan remaining = manager.TimeRemaining;
            string value = remaining.TotalDays >= 1d
                ? $"{Mathf.FloorToInt((float)remaining.TotalDays)}д {remaining.Hours}ч"
                : $"{remaining.Hours}ч {remaining.Minutes}м";
            SetText(timerText, value);
        }

        private void ShowRewardToast(ChestChaseRewardTier reward)
        {
            if (rewardToastText == null || reward == null)
            {
                return;
            }

            SetText(rewardToastText, $"Получено: монеты {reward.Coins}, подсказки {reward.Hints}, отмены {reward.Undos}");
            rewardToastText.gameObject.SetActive(true);
            StopCoroutine(nameof(HideRewardToastAfterDelay));
            StartCoroutine(nameof(HideRewardToastAfterDelay));
        }

        private IEnumerator HideRewardToastAfterDelay()
        {
            yield return new WaitForSecondsRealtime(2.6f);
            if (rewardToastText != null)
            {
                rewardToastText.gameObject.SetActive(false);
            }
        }

        private void ShowRules()
        {
            ShowTooltipForStage(manager != null ? manager.CurrentStage : 0);
            if (rewardToastText != null)
            {
                SetText(rewardToastText, "1 победа продвигает серию. При поражении прогресс серии сбрасывается.");
                rewardToastText.gameObject.SetActive(true);
                StopCoroutine(nameof(HideRewardToastAfterDelay));
                StartCoroutine(nameof(HideRewardToastAfterDelay));
            }
        }

        private void Close()
        {
            isShowing = false;
            gameObject.SetActive(false);
        }

        private void ApplyFontsToAllTexts()
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                RuntimeCyrillicFontProvider.Apply(text, text.text);
            }
        }

        private void RefreshEditorPreview()
        {
            if (Application.isPlaying)
            {
                return;
            }

            SetText(timerText, "1д 16ч");
            SetText(progressCountText, "0/20");
            SetText(progressLabelText, "ПОБЕД ПОДРЯД");
        }

        private void StartShowAnimation()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (panel != null)
            {
                panel.localScale = Vector3.one;
            }
        }

        private static void CreateProgressMilestones(RectTransform parent)
        {
            for (int index = 0; index < 5; index++)
            {
                float normalized = index / 4f;
                RectTransform mark = CreateRect($"Milestone_{index + 1}", parent, new Vector2(normalized, 0.5f), new Vector2(normalized, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(52f, 52f), Vector2.zero);
                mark.localRotation = Quaternion.Euler(0f, 0f, 45f);
                Image image = mark.gameObject.AddComponent<Image>();
                image.sprite = RoyalEventSprites.ProgressMarker != null ? RoyalEventSprites.ProgressMarker : PremiumUISprites.IconCircle;
                image.color = new Color(1f, 1f, 1f, 0.62f);
                image.raycastTarget = false;
            }
        }

        private void RefreshProgressMarkers(int current, int required)
        {
            if (progressTrack == null)
            {
                return;
            }

            for (int index = 0; index < 5; index++)
            {
                Image marker = progressTrack.Find($"Milestone_{index + 1}")?.GetComponent<Image>();
                if (marker == null)
                {
                    continue;
                }

                RectTransform markerRect = marker.rectTransform;
                if (markerRect != null)
                {
                    markerRect.sizeDelta = new Vector2(52f, 52f);
                }

                int threshold = Mathf.RoundToInt(required * (index / 4f));
                bool active = current >= threshold;
                marker.color = active
                    ? new Color(1f, 1f, 1f, index == 0 || current == threshold ? 0.95f : 0.82f)
                    : new Color(1f, 0.9f, 0.52f, 0.42f);
            }
        }

        private static void CreateRewardStageCard(RectTransform root, ChestChaseRewardTier reward, int stageNumber, bool completed, bool current)
        {
            bool large = root.sizeDelta.x >= 170f;
            RectTransform glow = CreateRect("Glow", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), large ? new Vector2(40f, 40f) : new Vector2(30f, 30f), Vector2.zero);
            Image glowImage = glow.gameObject.AddComponent<Image>();
            glowImage.sprite = RoyalEventSprites.RewardCard;
            glowImage.color = new Color(1f, 1f, 1f, 0f);
            glowImage.raycastTarget = false;
            glow.gameObject.SetActive(false);

            RectTransform card = CreateRect("Card", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), large ? new Vector2(194f, 232f) : new Vector2(160f, 220f), new Vector2(0f, large ? -14f : -10f));
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = RoyalEventSprites.RewardCard;
            cardImage.color = current ? Color.white : completed ? new Color(0.9f, 1f, 0.82f, 1f) : new Color(0.74f, 0.82f, 0.7f, 1f);
            cardImage.raycastTarget = false;

            TextMeshProUGUI stageLabel = CreateText(root, "StageLabel", Roman(stageNumber), large ? 34f : 28f, TextGold, TextAlignmentOptions.Center);
            stageLabel.fontStyle = FontStyles.Bold;
            SetAnchored(stageLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(78f, 40f), new Vector2(0f, large ? -2f : -10f));
            AddTextOutline(stageLabel, new Color(0.18f, 0.06f, 0f, 0.65f), new Vector2(1.2f, -1.2f));

            CreateRewardLine(card, "Coins", RoyalEventSprites.CoinIcon, reward != null ? reward.Coins.ToString() : "0", large ? 64f : 52f, large);
            CreateRewardLine(card, "Hints", PremiumUISprites.HintBulbIcon, reward != null ? reward.Hints.ToString() : "0", large ? 2f : -2f, large);
            if (reward != null && reward.Undos > 0)
            {
                CreateRewardLine(card, "Undo", PremiumUISprites.UndoIcon, reward.Undos.ToString(), large ? -62f : -54f, large);
            }

            if (completed)
            {
                RectTransform done = CreateRect("CompletedGem", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), large ? new Vector2(32f, 32f) : new Vector2(46f, 46f), new Vector2(0f, large ? 10f : 14f));
                Image doneImage = done.gameObject.AddComponent<Image>();
                doneImage.sprite = PremiumUISprites.IconCircle != null ? PremiumUISprites.IconCircle : RoyalEventSprites.CoinIcon;
                doneImage.color = new Color(0.18f, 1f, 0.28f, 1f);
                doneImage.raycastTarget = false;
            }
        }

        private static void CreateRewardLine(RectTransform parent, string name, Sprite iconSprite, string value, float y, bool large)
        {
            RectTransform row = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), large ? new Vector2(150f, 42f) : new Vector2(124f, 40f), new Vector2(0f, y));
            RectTransform icon = CreateRect("Icon", row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), large ? new Vector2(32f, 32f) : new Vector2(34f, 34f), new Vector2(large ? -48f : -36f, 0f));
            Image image = icon.gameObject.AddComponent<Image>();
            image.sprite = iconSprite != null ? iconSprite : PremiumUISprites.IconCircle;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;

            bool longValue = large && value != null && value.Length >= 4;
            TextMeshProUGUI label = CreateText(row, "Value", value, longValue ? 25f : large ? 32f : 30f, TextCream, TextAlignmentOptions.Left);
            label.fontStyle = FontStyles.Bold;
            label.fontSizeMax = longValue ? 25f : large ? 32f : label.fontSizeMax;
            label.fontSizeMin = large ? 18f : label.fontSizeMin;
            label.enableAutoSizing = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            SetAnchored(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), new Vector2(large ? 94f : 84f, 0f), new Vector2(large ? -22f : -12f, 0f));
            AddTextOutline(label, new Color(0f, 0.02f, 0f, 0.6f), new Vector2(1f, -1f));
        }

        private static TextMeshProUGUI CreateTooltipRow(RectTransform parent, string name, Sprite iconSprite, Vector2 position)
        {
            RectTransform row = CreateRect(name, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(86f, 36f), position);
            RectTransform icon = CreateRect("Icon", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(25f, 25f), new Vector2(16f, 0f));
            Image image = icon.gameObject.AddComponent<Image>();
            image.sprite = iconSprite != null ? iconSprite : PremiumUISprites.IconCircle;
            image.color = Color.white;
            image.raycastTarget = false;

            TextMeshProUGUI text = CreateText(row, "Text", string.Empty, 20f, TextDark, TextAlignmentOptions.Left);
            text.fontStyle = FontStyles.Bold;
            SetAnchored(text.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(-34f, 0f), new Vector2(36f, 0f));
            text.lineSpacing = 0f;
            return text;
        }

        private static string Roman(int number)
        {
            return number switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                5 => "V",
                _ => number.ToString()
            };
        }

        private static Button CreateCircleButton(Transform parent, string name, string label, Color color, Color textColor, UnityAction action)
        {
            Button button = CreateImageButton(parent, name, label, PremiumUISprites.IconCircle, color, textColor, 36f);
            button.onClick.AddListener(action);
            return button;
        }

        private static RectTransform EnsureHeroImage(RectTransform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size, Image.Type imageType)
        {
            RectTransform rect = parent.Find(name) as RectTransform;
            if (rect == null)
            {
                rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);
            }
            else
            {
                SetAnchored(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);
            }

            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = rect.gameObject.AddComponent<Image>();
            }

            image.sprite = sprite != null ? sprite : PremiumUISprites.EmeraldPanel;
            image.type = image.sprite != null && image.sprite.border != Vector4.zero ? imageType : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static void StyleSharedButton(RectTransform rect, string label, Vector2 size, Vector2 position, bool rightTop)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = rightTop ? Vector2.one : new Vector2(0f, 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = PremiumUISprites.CleanIconCircle != null ? PremiumUISprites.CleanIconCircle : PremiumUISprites.IconCircle;
                image.type = Image.Type.Sliced;
                image.color = new Color(1f, 1f, 1f, 0.94f);
            }

            Shadow shadow = rect.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.effectColor = new Color(0f, 0.02f, 0f, 0f);
                shadow.effectDistance = Vector2.zero;
            }

            TMP_Text text = rect.Find("Label")?.GetComponent<TMP_Text>();
            if (text != null)
            {
                SetText(text, label);
                text.fontSizeMax = size.y * 0.46f;
                text.color = new Color(1f, 0.82f, 0.24f, 1f);
            }
        }

        private static Button CreateImageButton(Transform parent, string name, string label, Sprite sprite, Color color, Color textColor, float fontSize)
        {
            RectTransform root = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220f, 72f), Vector2.zero);
            Image image = root.gameObject.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : PremiumUISprites.CreamPanel;
            image.type = image.sprite != null && image.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            Button button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            AddShadow(root.gameObject, new Color(0f, 0.02f, 0f, 0f), Vector2.zero);

            TextMeshProUGUI text = CreateText(root, "Label", label, fontSize, textColor, TextAlignmentOptions.Center);
            text.fontStyle = FontStyles.Bold;
            Stretch(text.rectTransform, 8f, 8f, 0f, 2f);
            AddTextOutline(text, new Color(0.18f, 0.06f, 0f, 0.5f), new Vector2(1f, -1f));

            if (button.GetComponent<UIJuicyButton>() == null)
            {
                button.gameObject.AddComponent<UIJuicyButton>();
            }

            return button;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.enableAutoSizing = true;
            text.fontSizeMax = fontSize;
            text.fontSizeMin = Mathf.Max(12f, fontSize * 0.58f);
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            RuntimeCyrillicFontProvider.Apply(text, value);
            SetText(text, value);
            return text;
        }

        private static Vector2 CalculatePanelSize()
        {
            float screenWidth = Mathf.Max(Screen.width, 1080f);
            float screenHeight = Mathf.Max(Screen.height, 1080f);
            bool landscape = screenWidth >= screenHeight;
            if (landscape)
            {
                return PanelLandscapeSize;
            }

            return new Vector2(
                Mathf.Clamp(screenWidth * 0.9f, 860f, 980f),
                Mathf.Clamp(screenHeight * 0.74f, 1120f, 1260f));
        }

        private static Sprite GetHeaderLogoSprite()
        {
            Sprite sprite = RoyalEventSprites.EventLogo;
#if UNITY_EDITOR
            sprite = sprite != null ? sprite : UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/UI/RoyalEvent/royal_event_logo.png");
#endif
            return sprite != null ? sprite : RoyalEventSprites.ProgressMarker;
        }

        private static void DisableDuplicateCrowns(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "HeaderLogo" || child.IsChildOf(root.Find("Panel/HeaderLogo")))
                {
                    continue;
                }

                string name = child.name.ToLowerInvariant();
                if (name.Contains("crown"))
                {
                    foreach (Image image in child.GetComponentsInChildren<Image>(true))
                    {
                        Color color = image.color;
                        color.a = 0f;
                        image.color = color;
                        image.raycastTarget = false;
                    }

                    child.gameObject.SetActive(false);
                }
            }
        }

        private static void CreateSparkles(RectTransform parent, bool landscapeLayout)
        {
            Vector2[] positions = landscapeLayout
                ? new[]
                {
                    new Vector2(-480f, 160f),
                    new Vector2(-360f, -10f),
                    new Vector2(-210f, 210f),
                    new Vector2(260f, 186f),
                    new Vector2(430f, 24f),
                    new Vector2(540f, -122f),
                    new Vector2(-560f, -128f)
                }
                : new[]
                {
                    new Vector2(-300f, 260f),
                    new Vector2(300f, 220f),
                    new Vector2(-260f, -40f),
                    new Vector2(250f, -110f)
                };

            for (int index = 0; index < positions.Length; index++)
            {
                RectTransform sparkle = CreateRect($"Sparkle_{index + 1}", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.one * (index % 2 == 0 ? 24f : 16f), positions[index]);
                Image image = sparkle.gameObject.AddComponent<Image>();
                image.sprite = RoyalEventSprites.ProgressMarker != null ? RoyalEventSprites.ProgressMarker : RoyalEventSprites.CoinIcon;
                image.color = new Color(1f, 0.9f, 0.28f, 0f);
                image.raycastTarget = false;
                sparkle.gameObject.SetActive(false);
            }
        }

        private static void AddPulse(GameObject target, float scaleAmplitude, float alphaAmplitude, float speed, float phase)
        {
            if (target == null)
            {
                return;
            }

            RoyalEventPulse pulse = target.GetComponent<RoyalEventPulse>();
            if (pulse == null)
            {
                pulse = target.AddComponent<RoyalEventPulse>();
            }

            pulse.Configure(scaleAmplitude, alphaAmplitude, speed, phase);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            RuntimeCyrillicFontProvider.Apply(text, value);
            text.text = value ?? string.Empty;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject target = new(name, typeof(RectTransform));
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetAnchored(rect, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
            return rect;
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f, 0f, 0f, 0f);
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = target.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(color.r, color.g, color.b, 0f);
            shadow.effectDistance = Vector2.zero;
            shadow.useGraphicAlpha = true;
        }

        private static void AddTextShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = new Color(color.r, color.g, color.b, 0f);
            shadow.effectDistance = Vector2.zero;
            shadow.useGraphicAlpha = true;
        }

        private static void DisableExternalGlow(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Shadow shadow in root.GetComponentsInChildren<Shadow>(true))
            {
                if (shadow is Outline)
                {
                    continue;
                }

                shadow.effectColor = new Color(shadow.effectColor.r, shadow.effectColor.g, shadow.effectColor.b, 0f);
                shadow.effectDistance = Vector2.zero;
            }

            foreach (Outline outline in root.GetComponentsInChildren<Outline>(true))
            {
                if (outline.GetComponent<TMP_Text>() != null)
                {
                    continue;
                }

                outline.effectColor = new Color(outline.effectColor.r, outline.effectColor.g, outline.effectColor.b, 0f);
                outline.effectDistance = Vector2.zero;
            }

            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (!IsExternalGlowName(image.gameObject.name))
                {
                    continue;
                }

                Color color = image.color;
                color.a = 0f;
                image.color = color;
                image.gameObject.SetActive(false);
            }
        }

        private static bool IsExternalGlowName(string objectName)
        {
            string name = objectName.ToLowerInvariant();
            return name.Contains("glow")
                || name.Contains("halo")
                || name.Contains("flash")
                || name.Contains("shine")
                || name.Contains("sparkle")
                || name.Contains("aura")
                || name.Contains("bloom")
                || name.Contains("outer")
                || name.Contains("highlight")
                || name.Contains("backlight");
        }

        private static void AddTextOutline(TMP_Text text, Color color, Vector2 distance)
        {
            Outline outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = color;
            outline.effectDistance = distance;
        }
    }
}
