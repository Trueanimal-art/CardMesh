using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardMesh
{
    [ExecuteAlways]
    public sealed class SettingsPopup : MonoBehaviour
    {
        private const string PopupName = "SettingsPopup";
        private const string ResourcesPrefabPath = "UI/MainMenu/SettingsPopup";
        private const string MusicVolumeKey = "MusicVolume";
        private const string SfxVolumeKey = "SfxVolume";
        private const string HintsEnabledKey = "HintsEnabled";
        private const string SettingsContentPanelName = "SettingsContentPanel";
        private const string ViewportName = "Viewport";
        private const string ContentName = "Content";
        private const float DefaultMusicVolume = 0.7f;
        private const float DefaultSfxVolume = 0.8f;
        private const float FadeDuration = 0.18f;
        private static readonly Vector2 PolishedTitleOffset = new(0f, -88f);
        private static readonly Vector2 PolishedClosePosition = new(-80f, -132f);
        private static readonly Vector2 PolishedCloseOffset = new(-14f, -56f);
        private static readonly Vector2 PanelSize = new(920f, 780f);
        private static readonly Vector2 TitleOffset = new(0f, -90f);
        private static readonly Vector2 TitleSize = new(-160f, 76f);
        private static readonly Vector2 CloseOffset = new(-52f, -98f);
        private static readonly Vector2 CloseSize = new(58f, 58f);
        private static readonly Vector2 ContentPanelSize = new(760f, 500f);
        private static readonly Vector2 ContentPanelOffset = new(0f, -184f);
        private const float ContentPanelHorizontalInset = 38f;
        private const float ContentPanelTopInset = 68f;
        private const float ContentPanelBottomInset = 36f;
        private const float SettingsRowHeight = 72f;
        private const float SettingsButtonHeight = 58f;
        private const float SettingsRowSpacing = 12f;
        private const float SettingsContentHeight = 430f;
        private const int SettingsContentHorizontalPadding = 26;
        private const int SettingsContentVerticalPadding = 10;

        private readonly Color textGold = new(1f, 0.83f, 0.34f, 1f);
        private readonly Color textDark = new(0.16f, 0.09f, 0.02f, 1f);
        private readonly Color textCream = new(1f, 0.93f, 0.64f, 1f);

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private PopupShell shell;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform modalLayer;
        [SerializeField] private RectTransform modalPanel;
        [SerializeField] private Button dimButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button howToButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button hintsToggleButton;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI musicValueText;
        [SerializeField] private TextMeshProUGUI sfxValueText;
        [SerializeField] private TextMeshProUGUI hintsValueText;
        [SerializeField] private Button modalBlockerButton;
        [SerializeField] private Button modalCloseButton;
        [SerializeField] private Button modalPrimaryButton;
        [SerializeField] private Button modalSecondaryButton;
        [SerializeField] private TextMeshProUGUI modalTitleText;
        [SerializeField] private TextMeshProUGUI modalBodyText;
        [SerializeField] private TextMeshProUGUI modalPrimaryText;
        [SerializeField] private TextMeshProUGUI modalSecondaryText;

        private TMP_FontAsset fontAsset;
        private bool hintsEnabled;
        private Coroutine animationRoutine;
        private Coroutine musicPercentRoutine;
        private Coroutine sfxPercentRoutine;
        private Coroutine hintsToggleRoutine;
        private Vector3 shownPanelScale = Vector3.one;
        private bool hasCapturedShownPanelScale;

        public static SettingsPopup SceneInstance =>
            FindFirstObjectByType<SettingsPopup>(FindObjectsInactive.Include);

        public static SettingsPopup GetOrCreate()
        {
            SettingsPopup existing = SceneInstance;
            if (existing != null)
            {
                return existing;
            }

            Canvas canvas = GetOrCreateCanvas();
            EnsureEventSystem();

            GameObject prefab = Resources.Load<GameObject>(ResourcesPrefabPath);
            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, canvas.transform, false);
                instance.name = PopupName;
                StretchToFill(instance.transform as RectTransform, 0f, 0f, 0f, 0f);

                SettingsPopup popup = instance.GetComponent<SettingsPopup>();
                if (popup == null)
                {
                    popup = instance.AddComponent<SettingsPopup>();
                }

                popup.BindSceneReferences();
                popup.WireStaticControls();
                popup.ApplyHiddenVisuals();
                instance.SetActive(false);
                return popup;
            }

            Debug.LogError($"SettingsPopup prefab is missing at Resources/{ResourcesPrefabPath}. Runtime UI construction is disabled for this popup.");
            return null;
        }

        public void Show()
        {
            BindSceneReferences();
            ApplyShellVisualOverrides();
            if (panel == null || contentRoot == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EnsureAuthoringHierarchy();
                    BindSceneReferences();
                }
#endif
            }

            if (panel == null || contentRoot == null)
            {
                Debug.LogError("SettingsPopup hierarchy is incomplete. Rebuild Assets/Resources/UI/MainMenu/SettingsPopup.prefab.");
                return;
            }

            WireStaticControls();
            CloseModal();
            LoadSavedValues();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PlayAnimation(true, null);
        }

        public void Hide()
        {
            CloseModal();
            PlayAnimation(false, () => gameObject.SetActive(false));
        }

        public void EnsureAuthoringHierarchy()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("SettingsPopup authoring hierarchy cannot be built in Play Mode. Use the prefab asset instead.");
                return;
            }

            BindSceneReferences();
            if (shell == null || !shell.IsReady || panel == null || contentRoot == null || !HasSettingsContent())
            {
                BuildUi();
                BindSceneReferences();
            }

            ApplyFontsToAllTexts();
            WireStaticControls();
            EnsureModalTemplateReady();
            CloseModal();
            ApplyInitialVisuals();
        }

        private void Reset()
        {
#if UNITY_EDITOR
            EnsureAuthoringHierarchy();
#endif
        }

        private void Awake()
        {
            BindSceneReferences();
            ApplyShellVisualOverrides();
            if (!Application.isPlaying && (panel == null || contentRoot == null || !HasSettingsContent()))
            {
                EnsureAuthoringHierarchy();
                BindSceneReferences();
            }

            CaptureShownPanelScale();
            ApplyFontsToAllTexts();
            WireStaticControls();
            ApplyInitialVisuals();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                BindSceneReferences();
                ApplyShellVisualOverrides();
                CaptureShownPanelScale();
                ApplyFontsToAllTexts();
                WireStaticControls();
                ApplyEditorPreviewVisuals();
            }
        }

        private void BindSceneReferences()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            shell ??= GetComponent<PopupShell>();
            if (shell != null)
            {
                shell.EnsureHierarchy();
                panel = shell.PanelRoot != null ? shell.PanelRoot : panel;
                contentRoot = shell.ContentRoot != null ? shell.ContentRoot : contentRoot;
                dimButton = shell.BackdropButton != null ? shell.BackdropButton : dimButton;
                closeButton = shell.CloseButton != null ? shell.CloseButton : closeButton;
            }

            panel = transform.Find("Panel") as RectTransform ?? panel;
            contentRoot = transform.Find("Panel/ContentRoot") as RectTransform
                ?? transform.Find($"Panel/{SettingsContentPanelName}/{ViewportName}/{ContentName}") as RectTransform
                ?? contentRoot;
            modalLayer = transform.Find("ModalLayer") as RectTransform ?? modalLayer;
            modalPanel = FindChildRecursive<RectTransform>(transform, "ModalPanel") ?? modalPanel;
            dimButton = FindButton(transform, "Backdrop") ?? FindButton(transform, "Dim") ?? dimButton;
            closeButton = shell != null && shell.CloseButton != null ? shell.CloseButton : FindButton(transform, "CloseButton") ?? closeButton;
            howToButton = FindButton(contentRoot, "HowToPlayButton") ?? FindButton(transform, "HowToPlayButton") ?? howToButton;
            resetButton = FindButton(contentRoot, "ResetProgressButton") ?? FindButton(transform, "ResetProgressButton") ?? resetButton;
            hintsToggleButton = FindButton(contentRoot, "HintsToggle") ?? FindButton(transform, "HintsToggle") ?? hintsToggleButton;
            modalBlockerButton = FindButton(transform, "ModalBlocker") ?? modalBlockerButton;
            modalCloseButton = FindButton(transform, "ModalCloseButton") ?? modalCloseButton;
            modalPrimaryButton = FindButton(transform, "ModalPrimaryButton") ?? modalPrimaryButton;
            modalSecondaryButton = FindButton(transform, "ModalSecondaryButton") ?? modalSecondaryButton;
            musicSlider = FindChildRecursive<Slider>(contentRoot, "MusicSlider") ?? FindChildRecursive<Slider>(transform, "MusicSlider") ?? musicSlider;
            sfxSlider = FindChildRecursive<Slider>(contentRoot, "SfxSlider") ?? FindChildRecursive<Slider>(transform, "SfxSlider") ?? sfxSlider;
            musicValueText = FindChildRecursive<TextMeshProUGUI>(contentRoot, "MusicValue") ?? FindChildRecursive<TextMeshProUGUI>(transform, "MusicValue") ?? musicValueText;
            sfxValueText = FindChildRecursive<TextMeshProUGUI>(contentRoot, "SfxValue") ?? FindChildRecursive<TextMeshProUGUI>(transform, "SfxValue") ?? sfxValueText;
            hintsValueText = FindChildRecursive<TextMeshProUGUI>(contentRoot, "HintsValue") ?? FindChildRecursive<TextMeshProUGUI>(transform, "HintsValue") ?? hintsValueText;
            modalTitleText = FindChildRecursive<TextMeshProUGUI>(transform, "ModalTitle") ?? modalTitleText;
            modalBodyText = FindChildRecursive<TextMeshProUGUI>(transform, "ModalBody") ?? modalBodyText;
            modalPrimaryText = FindChildRecursive<TextMeshProUGUI>(transform, "ModalPrimaryLabel") ?? modalPrimaryText;
            modalSecondaryText = FindChildRecursive<TextMeshProUGUI>(transform, "ModalSecondaryLabel") ?? modalSecondaryText;
            fontAsset ??= ResolveFont();
        }

        private void WireStaticControls()
        {
            if (dimButton != null)
            {
                dimButton.onClick.RemoveListener(Hide);
                dimButton.onClick.AddListener(Hide);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }

            if (howToButton != null)
            {
                howToButton.onClick.RemoveListener(ShowHowToPlay);
                howToButton.onClick.AddListener(ShowHowToPlay);
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(ShowResetConfirmation);
                resetButton.onClick.AddListener(ShowResetConfirmation);
            }

            if (hintsToggleButton != null)
            {
                hintsToggleButton.onClick.RemoveListener(ToggleHints);
                hintsToggleButton.onClick.AddListener(ToggleHints);
            }

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
                musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
                sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            }

            if (modalBlockerButton != null)
            {
                modalBlockerButton.onClick.RemoveListener(CloseModal);
                modalBlockerButton.onClick.AddListener(CloseModal);
            }

            if (modalCloseButton != null)
            {
                modalCloseButton.onClick.RemoveListener(CloseModal);
                modalCloseButton.onClick.AddListener(CloseModal);
            }
        }

        private void ApplyShellVisualOverrides()
        {
            if (shell == null)
            {
                return;
            }

            shell.Configure(
                PopupShell.SizePreset.Compact,
                "Настройки",
                useInnerBackground: false,
                customTitlePositionOffset: PolishedTitleOffset,
                customClosePositionOffset: PolishedCloseOffset);

            RectTransform closeRect = shell.CloseButton != null
                ? shell.CloseButton.transform as RectTransform
                : null;
            if (closeRect != null)
            {
                closeRect.anchoredPosition = PolishedClosePosition;
                Image closeImage = shell.CloseButton.GetComponent<Image>();
                if (closeImage != null)
                {
                    closeImage.raycastPadding =
                        new Vector4(-10f, -10f, -10f, -10f);
                }
            }

            Image backdropImage = shell.Backdrop != null ? shell.Backdrop.GetComponent<Image>() : null;
            if (backdropImage != null)
            {
                backdropImage.enabled = false;
            }

            Shadow frameShadow = shell.Frame != null ? shell.Frame.GetComponent<Shadow>() : null;
            if (frameShadow != null)
            {
                frameShadow.enabled = false;
            }
        }

        private void BuildUi()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            fontAsset = ResolveFont();

            shell = GetComponent<PopupShell>();
            if (shell == null)
            {
                shell = gameObject.AddComponent<PopupShell>();
            }

            shell.Configure(
                PopupShell.SizePreset.Compact,
                "Настройки",
                useInnerBackground: false,
                customTitlePositionOffset: PolishedTitleOffset,
                customClosePositionOffset: PolishedCloseOffset);
            panel = shell.PanelRoot;
            contentRoot = shell.ContentRoot;
            dimButton = shell.BackdropButton;
            closeButton = shell.CloseButton;
            DeactivateLegacySettingsShellChildren();

            if (!HasSettingsContent())
            {
                CreateSettingsContent();
            }

            modalLayer = transform.Find("ModalLayer") as RectTransform;
            if (modalLayer == null)
            {
                GameObject modalObject = CreateChild(gameObject, "ModalLayer", typeof(RectTransform));
                modalLayer = modalObject.transform as RectTransform;
                StretchToFill(modalLayer, 0f, 0f, 0f, 0f);
                CreateModalTemplate();
                modalObject.SetActive(false);
            }
            else
            {
                EnsureModalTemplateReady();
                modalLayer.gameObject.SetActive(false);
            }
        }

        private void DeactivateLegacySettingsShellChildren()
        {
            if (panel == null)
            {
                return;
            }

            DeactivateDirectPanelChild("Title");
            DeactivateDirectPanelChild(SettingsContentPanelName);
        }

        private void DeactivateDirectPanelChild(string childName)
        {
            Transform child = panel.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private void CreateModalTemplate()
        {
            GameObject blocker = CreateChild(modalLayer.gameObject, "ModalBlocker", typeof(Image), typeof(Button));
            StretchToFill(blocker.transform as RectTransform, 0f, 0f, 0f, 0f);
            blocker.GetComponent<Image>().color = new Color(0f, 0.02f, 0.01f, 0.52f);
            modalBlockerButton = blocker.GetComponent<Button>();

            GameObject modalPanelObject = CreateChild(modalLayer.gameObject, "ModalPanel", typeof(Image));
            modalPanel = modalPanelObject.transform as RectTransform;
            SetAnchored(modalPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(680f, 440f), new Vector2(0f, 74f));
            Image modalImage = modalPanelObject.GetComponent<Image>();
            modalImage.sprite = GetWindowSprite();
            modalImage.type = Image.Type.Simple;
            modalImage.color = Color.white;
            modalImage.raycastTarget = false;
            AddShadow(modalPanelObject, new Color(0.02f, 0.01f, 0f, 0.42f), new Vector2(0f, -8f));

            modalTitleText = CreateText(modalPanel, "ModalTitle", "Настройки", 44f, textGold, TextAlignmentOptions.Center);
            SetAnchored(modalTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-120f, 68f), new Vector2(0f, -78f));
            modalTitleText.fontStyle = FontStyles.Bold;
            AddTextOutline(modalTitleText, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));

            modalCloseButton = CreateCloseButton(modalPanel);
            modalCloseButton.name = "ModalCloseButton";
            SetAnchored(modalCloseButton.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(50f, 50f), new Vector2(-52f, -86f));

            modalBodyText = CreateText(modalPanel, "ModalBody", string.Empty, 27f, textCream, TextAlignmentOptions.Center);
            SetAnchored(modalBodyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 170f), new Vector2(0f, -8f));
            modalBodyText.enableWordWrapping = true;
            modalBodyText.fontStyle = FontStyles.Bold;
            AddTextOutline(modalBodyText, new Color(0f, 0.06f, 0.02f, 0.42f), new Vector2(1f, -1f));

            modalPrimaryButton = CreateModalButton(modalPanel, "ModalPrimaryButton", "Понятно", "ModalPrimaryLabel", new Vector2(0f, -138f));
            modalSecondaryButton = CreateModalButton(modalPanel, "ModalSecondaryButton", "Нет", "ModalSecondaryLabel", new Vector2(138f, -138f));
            modalSecondaryButton.gameObject.SetActive(false);
        }

        private void CreateSettingsContent()
        {
            float top = -SettingsContentVerticalPadding;
            musicSlider = CreateSliderRow(contentRoot, "MusicRow", "Музыка", top, "MusicSlider", "MusicValue", OnMusicSliderChanged, out musicValueText);
            top -= SettingsRowHeight + SettingsRowSpacing;
            sfxSlider = CreateSliderRow(contentRoot, "SfxRow", "Звуки", top, "SfxSlider", "SfxValue", OnSfxSliderChanged, out sfxValueText);
            top -= SettingsRowHeight + SettingsRowSpacing;
            CreateHintsRow(contentRoot, top);
            top -= SettingsRowHeight + 24f;
            howToButton = CreateSettingsButton(contentRoot, "HowToPlayButton", "Как играть", textGold, top);
            top -= SettingsButtonHeight + SettingsRowSpacing;
            resetButton = CreateSettingsButton(contentRoot, "ResetProgressButton", "Сброс прогресса", textGold, top);
        }

        private bool HasSettingsContent()
        {
            return FindChildRecursive<Slider>(contentRoot, "MusicSlider") != null
                && FindChildRecursive<Slider>(contentRoot, "SfxSlider") != null
                && FindButton(contentRoot, "HintsToggle") != null
                && FindButton(contentRoot, "HowToPlayButton") != null
                && FindButton(contentRoot, "ResetProgressButton") != null;
        }

        private Slider CreateSliderRow(RectTransform parent, string rowName, string label, float topY, string sliderName, string valueName, UnityEngine.Events.UnityAction<float> changed, out TextMeshProUGUI valueText)
        {
            GameObject row = CreateChild(parent.gameObject, rowName, typeof(RectTransform));
            RectTransform rowRect = row.transform as RectTransform;
            SetAnchored(rowRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-SettingsContentHorizontalPadding * 2f, SettingsRowHeight), new Vector2(0f, topY));

            TextMeshProUGUI labelText = CreateText(rowRect, "Label", label, 31f, textCream, TextAlignmentOptions.Left);
            SetAnchored(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, 48f), Vector2.zero);
            labelText.fontStyle = FontStyles.Bold;
            AddTextOutline(labelText, new Color(0f, 0.06f, 0.02f, 0.58f), new Vector2(1f, -1f));

            valueText = CreateText(rowRect, valueName, "70%", 27f, textGold, TextAlignmentOptions.Right);
            SetAnchored(valueText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(92f, 42f), Vector2.zero);
            valueText.fontStyle = FontStyles.Bold;

            GameObject sliderObject = CreateChild(row, sliderName, typeof(RectTransform), typeof(Slider));
            RectTransform sliderRect = sliderObject.transform as RectTransform;
            SetAnchored(sliderRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-326f, 38f), new Vector2(44f, 0f));

            GameObject background = CreateChild(sliderObject, "Background", typeof(Image));
            RectTransform backgroundRect = background.transform as RectTransform;
            SetAnchored(backgroundRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), Vector2.zero);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = PremiumUISprites.SettingsSliderTrack;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = Color.white;

            GameObject fillArea = CreateChild(sliderObject, "Fill Area", typeof(RectTransform));
            RectTransform fillAreaRect = fillArea.transform as RectTransform;
            SetAnchored(fillAreaRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-12f, 16f), Vector2.zero);

            GameObject fill = CreateChild(fillArea, "Fill", typeof(Image));
            RectTransform fillRect = fill.transform as RectTransform;
            StretchToFill(fillRect, 0f, 0f, 0f, 0f);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.sprite = PremiumUISprites.SettingsSliderFill;
            fillImage.type = Image.Type.Simple;
            fillImage.color = Color.white;

            GameObject handle = CreateChild(sliderObject, "Handle", typeof(Image));
            RectTransform handleRect = handle.transform as RectTransform;
            SetAnchored(handleRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(38f, 38f), Vector2.zero);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.sprite = PremiumUISprites.SettingsSliderHandle != null
                ? PremiumUISprites.SettingsSliderHandle
                : PremiumUISprites.EmeraldIconCircle;
            handleImage.type = Image.Type.Simple;
            handleImage.preserveAspect = true;
            handleImage.color = Color.white;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.onValueChanged.AddListener(changed);
            return slider;
        }

        private void CreateHintsRow(RectTransform parent, float topY)
        {
            GameObject row = CreateChild(parent.gameObject, "HintsRow", typeof(RectTransform));
            RectTransform rowRect = row.transform as RectTransform;
            SetAnchored(rowRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-SettingsContentHorizontalPadding * 2f, SettingsRowHeight), new Vector2(0f, topY));

            TextMeshProUGUI labelText = CreateText(rowRect, "Label", "Подсказки", 31f, textCream, TextAlignmentOptions.Left);
            SetAnchored(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(190f, 48f), Vector2.zero);
            labelText.fontStyle = FontStyles.Bold;
            AddTextOutline(labelText, new Color(0f, 0.06f, 0.02f, 0.58f), new Vector2(1f, -1f));

            hintsToggleButton = CreateImageButton(rowRect, "HintsToggle", GetLevelButtonSprite(), Color.white);
            SetAnchored(hintsToggleButton.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(172f, 54f), new Vector2(44f, 0f));
            hintsToggleButton.onClick.AddListener(ToggleHints);
            hintsValueText = CreateText(hintsToggleButton.transform, "HintsValue", "Вкл", 28f, textGold, TextAlignmentOptions.Center);
            StretchToFill(hintsValueText.rectTransform, 16f, 16f, 0f, 2f);
            hintsValueText.fontStyle = FontStyles.Bold;
            AddTextOutline(hintsValueText, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));
        }

        private Button CreateSettingsButton(RectTransform parent, string objectName, string label, Color labelColor, float topY)
        {
            Button button = CreateImageButton(parent, objectName, GetLevelButtonSprite(), Color.white);
            RectTransform rect = button.transform as RectTransform;
            SetAnchored(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, SettingsButtonHeight), new Vector2(0f, topY));

            TextMeshProUGUI text = CreateText(button.transform, "Label", label, 30f, labelColor, TextAlignmentOptions.Center);
            StretchToFill(text.rectTransform, 24f, 24f, 0f, 2f);
            text.fontStyle = FontStyles.Bold;
            AddTextOutline(text, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));
            return button;
        }

        private void LoadSavedValues()
        {
            float music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume));
            float sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));
            hintsEnabled = PlayerPrefs.GetInt(HintsEnabledKey, 1) == 1;

            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(music);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(sfx);
            }

            ApplyMusicVolume(music);
            ApplySfxVolume(sfx);
            RefreshHintsLabel();
        }

        private void OnMusicSliderChanged(float value)
        {
            ApplyMusicVolume(value);
        }

        private void OnSfxSliderChanged(float value)
        {
            ApplySfxVolume(value);
        }

        private void ApplyMusicVolume(float value)
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, value);
            PlayerPrefs.Save();
            if (MusicPlayer.Instance != null)
            {
                MusicPlayer.Instance.SetMusicVolume(value);
            }

            if (musicValueText != null)
            {
                AnimatePercentage(
                    musicValueText,
                    Mathf.RoundToInt(value * 100f),
                    true);
            }
        }

        private void ApplySfxVolume(float value)
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, value);
            PlayerPrefs.Save();
            if (MusicPlayer.Instance != null)
            {
                MusicPlayer.Instance.SetSfxVolume(value);
            }

            if (sfxValueText != null)
            {
                AnimatePercentage(
                    sfxValueText,
                    Mathf.RoundToInt(value * 100f),
                    false);
            }
        }

        private void ToggleHints()
        {
            hintsEnabled = !hintsEnabled;
            PlayerPrefs.SetInt(HintsEnabledKey, hintsEnabled ? 1 : 0);
            PlayerPrefs.Save();
            RefreshHintsLabel();
            PlayHintsToggleFeedback();
        }

        private void RefreshHintsLabel()
        {
            if (hintsValueText != null)
            {
                hintsValueText.text = hintsEnabled ? "Вкл" : "Выкл";
                hintsValueText.color = hintsEnabled ? textGold : new Color(0.86f, 0.8f, 0.62f, 0.64f);
            }
        }

        private void AnimatePercentage(
            TextMeshProUGUI label,
            int targetValue,
            bool isMusic)
        {
            if (label == null)
            {
                return;
            }

            targetValue = Mathf.Clamp(targetValue, 0, 100);
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                label.text = $"{targetValue}%";
                return;
            }

            Coroutine running = isMusic
                ? musicPercentRoutine
                : sfxPercentRoutine;
            if (running != null)
            {
                StopCoroutine(running);
            }

            Coroutine next = StartCoroutine(
                AnimatePercentageRoutine(label, targetValue, isMusic));
            if (isMusic)
            {
                musicPercentRoutine = next;
            }
            else
            {
                sfxPercentRoutine = next;
            }
        }

        private System.Collections.IEnumerator AnimatePercentageRoutine(
            TextMeshProUGUI label,
            int targetValue,
            bool isMusic)
        {
            int startValue = ParsePercentage(label.text, targetValue);
            float elapsed = 0f;
            const float duration = 0.09f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                label.text =
                    $"{Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, eased))}%";
                yield return null;
            }

            label.text = $"{targetValue}%";
            if (isMusic)
            {
                musicPercentRoutine = null;
            }
            else
            {
                sfxPercentRoutine = null;
            }
        }

        private static int ParsePercentage(string value, int fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            string digits = value.Replace("%", string.Empty).Trim();
            return int.TryParse(digits, out int parsed)
                ? Mathf.Clamp(parsed, 0, 100)
                : fallback;
        }

        private void PlayHintsToggleFeedback()
        {
            if (!Application.isPlaying
                || !isActiveAndEnabled
                || hintsToggleButton == null)
            {
                return;
            }

            if (hintsToggleRoutine != null)
            {
                StopCoroutine(hintsToggleRoutine);
            }

            hintsToggleRoutine = StartCoroutine(AnimateHintsToggle());
        }

        private System.Collections.IEnumerator AnimateHintsToggle()
        {
            RectTransform rect = hintsToggleButton.transform as RectTransform;
            if (rect == null)
            {
                hintsToggleRoutine = null;
                yield break;
            }

            Vector3 baseScale = Vector3.one;
            float elapsed = 0f;
            const float duration = 0.16f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float response = Mathf.Sin(t * Mathf.PI);
                rect.localScale =
                    baseScale * Mathf.Lerp(1f, 1.055f, response);
                yield return null;
            }

            rect.localScale = baseScale;
            hintsToggleRoutine = null;
        }

        private void ShowHowToPlay()
        {
            ShowModal(
                "Как играть",
                "Соединяйте обычные карты с золотыми картами по смысловой ассоциации. Соберите все карты нужной категории, чтобы завершить стопку. Следите за количеством ходов: когда они закончатся, уровень будет проигран.",
                ("Понятно", CloseModal),
                null);
        }

        private void ShowResetConfirmation()
        {
            ShowModal(
                "Сброс прогресса",
                "Сбросить весь прогресс? Это действие нельзя отменить.",
                ("Да", ConfirmResetProgress),
                ("Нет", CloseModal));
        }

        private void ConfirmResetProgress()
        {
            LevelProgressionManager manager = FindFirstObjectByType<LevelProgressionManager>(FindObjectsInactive.Include);
            if (manager != null)
            {
                manager.ResetProgress();
            }
            else
            {
                LevelProgressionManager.ClearSavedProgress();
            }

            CloseModal();
        }

        private void ShowModal(string title, string body, (string label, Action action) primary, (string label, Action action)? secondary)
        {
            BindSceneReferences();
            if (!EnsureModalTemplateReady())
            {
                Debug.LogError("SettingsPopup modal layer is missing. Rebuild Assets/Resources/UI/MainMenu/SettingsPopup.prefab.");
                return;
            }

            CloseModal();
            modalLayer.SetAsLastSibling();
            modalPanel.gameObject.SetActive(true);
            modalPanel.SetAsLastSibling();

            float primaryX = secondary.HasValue ? -138f : 0f;
            SetModalText(modalTitleText, title);
            SetModalText(modalBodyText, body);
            ConfigureModalButton(modalPrimaryButton, modalPrimaryText, primary.label, primary.action, new Vector2(primaryX, -138f), true);

            ConfigureModalButton(
                modalSecondaryButton,
                modalSecondaryText,
                secondary.HasValue ? secondary.Value.label : string.Empty,
                secondary.HasValue ? secondary.Value.action : null,
                new Vector2(138f, -138f),
                secondary.HasValue);

            modalLayer.gameObject.SetActive(true);
        }

        private bool EnsureModalTemplateReady()
        {
            if (modalLayer == null)
            {
                RectTransform existingLayer = transform.Find("ModalLayer") as RectTransform;
                if (existingLayer != null)
                {
                    modalLayer = existingLayer;
                }
                else
                {
                    GameObject modalObject = CreateChild(gameObject, "ModalLayer", typeof(RectTransform));
                    modalLayer = modalObject.transform as RectTransform;
                }
            }

            if (modalLayer == null)
            {
                return false;
            }

            StretchToFill(modalLayer, 0f, 0f, 0f, 0f);
            modalLayer.SetAsLastSibling();
            EnsureModalBlocker();
            EnsureModalPanel();
            if (modalPanel == null)
            {
                return false;
            }

            modalPanel.gameObject.SetActive(true);
            modalPanel.SetAsLastSibling();
            EnsureModalContent();
            WireStaticControls();
            return true;
        }

        private void EnsureModalBlocker()
        {
            modalBlockerButton = FindButton(modalLayer, "ModalBlocker") ?? modalBlockerButton;
            if (modalBlockerButton == null || !modalBlockerButton.transform.IsChildOf(modalLayer))
            {
                GameObject blocker = CreateChild(modalLayer.gameObject, "ModalBlocker", typeof(Image), typeof(Button));
                modalBlockerButton = blocker.GetComponent<Button>();
            }

            StretchToFill(modalBlockerButton.transform as RectTransform, 0f, 0f, 0f, 0f);
            Image blockerImage = modalBlockerButton.GetComponent<Image>() ?? modalBlockerButton.gameObject.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0.02f, 0.01f, 0.52f);
            blockerImage.raycastTarget = true;
            modalBlockerButton.targetGraphic = blockerImage;
            modalBlockerButton.gameObject.SetActive(true);
            modalBlockerButton.transform.SetAsFirstSibling();
        }

        private void EnsureModalPanel()
        {
            modalPanel = FindChildRecursive<RectTransform>(modalLayer, "ModalPanel") ?? modalPanel;
            bool createdPanel = false;
            if (modalPanel == null || !modalPanel.transform.IsChildOf(modalLayer))
            {
                GameObject modalPanelObject = CreateChild(modalLayer.gameObject, "ModalPanel", typeof(Image));
                modalPanel = modalPanelObject.transform as RectTransform;
                createdPanel = true;
            }

            if (createdPanel)
            {
                SetAnchored(modalPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(680f, 440f), new Vector2(0f, 74f));
            }

            Image modalImage = modalPanel.GetComponent<Image>() ?? modalPanel.gameObject.AddComponent<Image>();
            modalImage.sprite = GetWindowSprite();
            modalImage.type = Image.Type.Simple;
            modalImage.color = Color.white;
            modalImage.raycastTarget = false;
            AddShadow(modalPanel.gameObject, new Color(0.02f, 0.01f, 0f, 0.42f), new Vector2(0f, -8f));
        }

        private void EnsureModalContent()
        {
            modalTitleText = FindChildRecursive<TextMeshProUGUI>(modalPanel, "ModalTitle") ?? modalTitleText;
            bool createdTitle = false;
            if (modalTitleText == null || !modalTitleText.transform.IsChildOf(modalPanel))
            {
                modalTitleText = CreateText(modalPanel, "ModalTitle", "Настройки", 44f, textGold, TextAlignmentOptions.Center);
                createdTitle = true;
            }

            if (createdTitle)
            {
                SetAnchored(modalTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-120f, 68f), new Vector2(0f, -78f));
            }

            modalTitleText.gameObject.SetActive(true);
            modalTitleText.fontStyle = FontStyles.Bold;
            AddTextOutline(modalTitleText, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));

            modalCloseButton = FindButton(modalPanel, "ModalCloseButton") ?? modalCloseButton;
            bool createdCloseButton = false;
            if (modalCloseButton == null || !modalCloseButton.transform.IsChildOf(modalPanel))
            {
                modalCloseButton = CreateCloseButton(modalPanel);
                modalCloseButton.name = "ModalCloseButton";
                createdCloseButton = true;
            }

            if (createdCloseButton)
            {
                SetAnchored(modalCloseButton.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(50f, 50f), new Vector2(-52f, -86f));
            }

            modalCloseButton.gameObject.SetActive(true);

            modalBodyText = FindChildRecursive<TextMeshProUGUI>(modalPanel, "ModalBody") ?? modalBodyText;
            bool createdBody = false;
            if (modalBodyText == null || !modalBodyText.transform.IsChildOf(modalPanel))
            {
                modalBodyText = CreateText(modalPanel, "ModalBody", string.Empty, 27f, textCream, TextAlignmentOptions.Center);
                createdBody = true;
            }

            if (createdBody)
            {
                SetAnchored(modalBodyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 170f), new Vector2(0f, -8f));
            }

            modalBodyText.gameObject.SetActive(true);
            modalBodyText.enableWordWrapping = true;
            modalBodyText.fontStyle = FontStyles.Bold;
            AddTextOutline(modalBodyText, new Color(0f, 0.06f, 0.02f, 0.42f), new Vector2(1f, -1f));

            modalPrimaryButton = FindButton(modalPanel, "ModalPrimaryButton") ?? modalPrimaryButton;
            if (modalPrimaryButton == null || !modalPrimaryButton.transform.IsChildOf(modalPanel))
            {
                modalPrimaryButton = CreateModalButton(modalPanel, "ModalPrimaryButton", "Понятно", "ModalPrimaryLabel", new Vector2(0f, -138f));
            }

            modalPrimaryText = FindChildRecursive<TextMeshProUGUI>(modalPrimaryButton.transform, "ModalPrimaryLabel") ?? modalPrimaryText;
            if (modalPrimaryText == null || !modalPrimaryText.transform.IsChildOf(modalPrimaryButton.transform))
            {
                modalPrimaryText = CreateText(modalPrimaryButton.transform, "ModalPrimaryLabel", "Понятно", 30f, textGold, TextAlignmentOptions.Center);
                StretchToFill(modalPrimaryText.rectTransform, 18f, 18f, 0f, 2f);
                modalPrimaryText.fontStyle = FontStyles.Bold;
                AddTextOutline(modalPrimaryText, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));
            }

            modalSecondaryButton = FindButton(modalPanel, "ModalSecondaryButton") ?? modalSecondaryButton;
            if (modalSecondaryButton == null || !modalSecondaryButton.transform.IsChildOf(modalPanel))
            {
                modalSecondaryButton = CreateModalButton(modalPanel, "ModalSecondaryButton", "Нет", "ModalSecondaryLabel", new Vector2(138f, -138f));
            }

            modalSecondaryText = FindChildRecursive<TextMeshProUGUI>(modalSecondaryButton.transform, "ModalSecondaryLabel") ?? modalSecondaryText;
            if (modalSecondaryText == null || !modalSecondaryText.transform.IsChildOf(modalSecondaryButton.transform))
            {
                modalSecondaryText = CreateText(modalSecondaryButton.transform, "ModalSecondaryLabel", "Нет", 30f, textGold, TextAlignmentOptions.Center);
                StretchToFill(modalSecondaryText.rectTransform, 18f, 18f, 0f, 2f);
                modalSecondaryText.fontStyle = FontStyles.Bold;
                AddTextOutline(modalSecondaryText, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));
            }
        }

        private Button CreateModalButton(RectTransform parent, string objectName, string label, string labelName, Vector2 position)
        {
            Button button = CreateImageButton(parent, objectName, GetLevelButtonSprite(), Color.white);
            SetAnchored(button.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220f, 64f), position);
            TextMeshProUGUI text = CreateText(button.transform, labelName, label, 30f, textGold, TextAlignmentOptions.Center);
            StretchToFill(text.rectTransform, 18f, 18f, 0f, 2f);
            text.fontStyle = FontStyles.Bold;
            AddTextOutline(text, new Color(0.12f, 0.055f, 0.012f, 0.62f), new Vector2(1f, -1f));
            return button;
        }

        private void ConfigureModalButton(Button button, TextMeshProUGUI label, string text, Action action, Vector2 position, bool visible)
        {
            _ = position;
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(visible);
            button.onClick.RemoveAllListeners();
            if (!visible)
            {
                return;
            }

            SetModalText(label, text);
            button.onClick.AddListener(() => action?.Invoke());
        }

        private static void SetModalText(TextMeshProUGUI label, string text)
        {
            if (label == null)
            {
                return;
            }

            label.text = text;
            RuntimeCyrillicFontProvider.Apply(label, text);
        }

        private void CloseModal()
        {
            if (modalLayer == null)
            {
                return;
            }

            if (modalPrimaryButton != null)
            {
                modalPrimaryButton.onClick.RemoveAllListeners();
            }

            if (modalSecondaryButton != null)
            {
                modalSecondaryButton.onClick.RemoveAllListeners();
            }

            modalLayer.gameObject.SetActive(false);
        }

        private void PlayAnimation(bool show, Action completed)
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            animationRoutine = StartCoroutine(Animate(show, completed));
        }

        private System.Collections.IEnumerator Animate(bool show, Action completed)
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (panel == null)
            {
                panel = transform.Find("Panel") as RectTransform;
            }

            CaptureShownPanelScale();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            float startAlpha = show ? 0f : canvasGroup.alpha;
            float endAlpha = show ? 1f : 0f;
            Vector3 hiddenScale = new(shownPanelScale.x * 0.92f, shownPanelScale.y * 0.92f, shownPanelScale.z);
            Vector3 startScale = show ? hiddenScale : panel.localScale;
            Vector3 endScale = show ? shownPanelScale : hiddenScale;

            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / FadeDuration);
                float eased = show ? 1f - Mathf.Pow(1f - progress, 2f) : progress * progress;

                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
                if (panel != null)
                {
                    panel.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
                }

                yield return null;
            }

            canvasGroup.alpha = endAlpha;
            canvasGroup.blocksRaycasts = show;
            canvasGroup.interactable = show;
            if (panel != null)
            {
                panel.localScale = endScale;
            }

            animationRoutine = null;
            completed?.Invoke();
        }

        private void ApplyHiddenVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (panel != null)
            {
                CaptureShownPanelScale();
                panel.localScale = new Vector3(shownPanelScale.x * 0.92f, shownPanelScale.y * 0.92f, shownPanelScale.z);
            }
        }

        private void ApplyInitialVisuals()
        {
            if (Application.isPlaying)
            {
                ApplyHiddenVisuals();
                return;
            }

            ApplyEditorPreviewVisuals();
        }

        private void ApplyEditorPreviewVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (panel != null)
            {
                CaptureShownPanelScale();
            }
        }

        private void CaptureShownPanelScale()
        {
            if (panel == null)
            {
                return;
            }

            if (!hasCapturedShownPanelScale || !Application.isPlaying || canvasGroup == null || canvasGroup.alpha > 0.01f)
            {
                shownPanelScale = panel.localScale;
                hasCapturedShownPanelScale = true;
            }
        }

        private void ApplyFontsToAllTexts()
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                RuntimeCyrillicFontProvider.Apply(text, text.text);
                text.raycastTarget = false;
            }
        }

        private TMP_FontAsset ResolveFont()
        {
            TMP_Text sceneText = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
            if (sceneText != null && sceneText.font != null)
            {
                return sceneText.font;
            }

            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            Font font = Resources.Load<Font>("Fonts/Roboto-Bold");
            return font != null ? TMP_FontAsset.CreateFontAsset(font) : null;
        }

        private Button CreateImageButton(Transform parent, string objectName, Sprite sprite, Color color)
        {
            GameObject buttonObject = CreateChild(parent.gameObject, objectName, typeof(Image), typeof(Button));
            Image image = buttonObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.62f, 1f);
            colors.pressedColor = new Color(0.96f, 0.72f, 0.18f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.48f, 0.48f, 0.42f, 0.65f);
            button.colors = colors;

            if (buttonObject.GetComponent<UIJuicyButton>() == null)
            {
                buttonObject.AddComponent<UIJuicyButton>();
            }

            AddShadow(buttonObject, new Color(0.02f, 0.01f, 0f, 0.32f), new Vector2(0f, -5f));
            return button;
        }

        private Button CreateCloseButton(RectTransform parent)
        {
            Button button = PopupShell.EnsureIconCloseButton(parent, "CloseButton");
            SetAnchored(button.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), CloseSize, CloseOffset);
            return button;
        }

        private TextMeshProUGUI CreateText(Transform parent, string objectName, string text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateChild(parent.gameObject, objectName, typeof(TextMeshProUGUI));
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = fontAsset != null ? fontAsset : ResolveFont();
            label.enableAutoSizing = true;
            label.fontSizeMax = fontSize;
            label.fontSizeMin = Mathf.Max(14f, fontSize * 0.58f);
            label.enableWordWrapping = false;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            RuntimeCyrillicFontProvider.Apply(label, text);
            return label;
        }

        private static GameObject CreateChild(GameObject parent, string objectName, params Type[] components)
        {
            GameObject child = new(objectName, components);
            child.layer = parent.layer;
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static RectTransform CreateRect(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
        {
            GameObject child = CreateChild(parent.gameObject, objectName, typeof(RectTransform));
            RectTransform rectTransform = child.transform as RectTransform;
            SetAnchored(rectTransform, anchorMin, anchorMax, pivot, size, anchoredPosition);
            return rectTransform;
        }

        private static void SetAnchored(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.localScale = Vector3.one;
        }

        private static void StretchToFill(RectTransform rectTransform, float left, float right, float top, float bottom)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
            rectTransform.localScale = Vector3.one;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.GetComponent<Shadow>() ?? target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void AddTextOutline(TMP_Text text, Color color, Vector2 distance)
        {
            Outline outline = text.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static Sprite GetWindowSprite()
        {
            return PremiumUISprites.SettingsWindowCardMesh != null
                ? PremiumUISprites.SettingsWindowCardMesh
                : PremiumUISprites.EmeraldPanel;
        }

        private static Sprite GetLevelButtonSprite()
        {
            return PremiumUISprites.GameplayActionButton != null
                ? PremiumUISprites.GameplayActionButton
                : PremiumUISprites.PrimaryButton;
        }

        private static Button FindButton(Transform root, string targetName)
        {
            return FindChildRecursive<Button>(root, targetName);
        }

        private static T FindChildRecursive<T>(Transform root, string targetName) where T : Component
        {
            if (root == null)
            {
                return null;
            }

            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (component != null && component.name == targetName)
                {
                    return component;
                }
            }

            return null;
        }

        private static Canvas GetOrCreateCanvas()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                return canvas;
            }

            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
