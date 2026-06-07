using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    public sealed class SettingsPopup : MonoBehaviour
    {
        private const string PopupName = "SettingsPopup";
        private const string MusicVolumeKey = "MusicVolume";
        private const string SfxVolumeKey = "SfxVolume";
        private const string HintsEnabledKey = "HintsEnabled";
        private const float DefaultMusicVolume = 0.7f;
        private const float DefaultSfxVolume = 0.8f;
        private const float FadeDuration = 0.18f;

        private readonly Color textGold = new(1f, 0.83f, 0.34f, 1f);
        private readonly Color textDark = new(0.16f, 0.09f, 0.02f, 1f);
        private readonly Color textCream = new(1f, 0.93f, 0.64f, 1f);

        private CanvasGroup canvasGroup;
        private RectTransform panel;
        private RectTransform modalLayer;
        private TMP_FontAsset fontAsset;
        private Slider musicSlider;
        private Slider sfxSlider;
        private TextMeshProUGUI musicValueText;
        private TextMeshProUGUI sfxValueText;
        private TextMeshProUGUI hintsValueText;
        private bool hintsEnabled;
        private Coroutine animationRoutine;

        public static SettingsPopup GetOrCreate()
        {
            SettingsPopup existing = FindFirstObjectByType<SettingsPopup>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            EnsureEventSystem();

            GameObject root = new(PopupName, typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            StretchToFill(root.transform as RectTransform, 0f, 0f, 0f, 0f);

            SettingsPopup popup = root.AddComponent<SettingsPopup>();
            popup.BuildUi();
            root.SetActive(false);
            return popup;
        }

        public void Show()
        {
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

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (panel == null)
            {
                panel = transform.Find("Panel") as RectTransform;
            }

            ApplyHiddenVisuals();
        }

        private void BuildUi()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            fontAsset = ResolveFont();

            GameObject dim = CreateChild(gameObject, "Dim", typeof(Image), typeof(Button));
            StretchToFill(dim.transform as RectTransform, 0f, 0f, 0f, 0f);
            Image dimImage = dim.GetComponent<Image>();
            dimImage.color = new Color(0f, 0.03f, 0.01f, 0.72f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            GameObject panelObject = CreateChild(gameObject, "Panel", typeof(Image));
            panel = panelObject.transform as RectTransform;
            SetAnchored(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820f, 780f), new Vector2(0f, 120f));

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.sprite = PremiumUISprites.EmeraldPanel;
            panelImage.type = panelImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            panelImage.color = new Color(0.93f, 1f, 0.86f, 1f);
            AddShadow(panelObject, new Color(0f, 0.03f, 0.01f, 0.58f), new Vector2(0f, -12f));
            AddOutline(panelObject, new Color(1f, 0.78f, 0.22f, 0.86f), new Vector2(3f, -3f));

            TextMeshProUGUI title = CreateText(panel, "Title", "Настройки", 58f, textGold, TextAlignmentOptions.Center);
            SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 82f), new Vector2(0f, -44f));
            title.fontStyle = FontStyles.Bold;
            AddTextOutline(title, new Color(0.02f, 0.08f, 0.03f, 0.8f), new Vector2(1.6f, -1.6f));

            Button closeButton = CreateImageButton(panel, "CloseButton", PremiumUISprites.IconCircle, new Color(1f, 0.82f, 0.18f, 1f));
            SetAnchored(closeButton.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(72f, 72f), new Vector2(-56f, -54f));
            closeButton.onClick.AddListener(Hide);
            TextMeshProUGUI closeLabel = CreateText(closeButton.transform as RectTransform, "Label", "X", 30f, textDark, TextAlignmentOptions.Center);
            StretchToFill(closeLabel.rectTransform, 0f, 0f, 0f, 2f);
            closeLabel.fontStyle = FontStyles.Bold;

            GameObject contentPanel = CreateChild(panelObject, "SettingsContent", typeof(Image));
            RectTransform contentRect = contentPanel.transform as RectTransform;
            SetAnchored(contentRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(690f, -210f), new Vector2(0f, -26f));
            Image contentImage = contentPanel.GetComponent<Image>();
            contentImage.sprite = PremiumUISprites.CategorySlot;
            contentImage.type = contentImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            contentImage.color = new Color(0.04f, 0.22f, 0.1f, 0.7f);

            musicSlider = CreateSliderRow(contentRect, "MusicRow", "Музыка", new Vector2(0f, -74f), OnMusicSliderChanged, out musicValueText);
            sfxSlider = CreateSliderRow(contentRect, "SfxRow", "Звуки", new Vector2(0f, -190f), OnSfxSliderChanged, out sfxValueText);
            CreateHintsRow(contentRect, new Vector2(0f, -306f));

            Button howToButton = CreateImageButton(contentRect, "HowToPlayButton", PremiumUISprites.PrimaryButton, Color.white);
            SetAnchored(howToButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(540f, 78f), new Vector2(0f, 176f));
            howToButton.onClick.AddListener(ShowHowToPlay);
            TextMeshProUGUI howToText = CreateText(howToButton.transform, "Label", "Как играть", 34f, textDark, TextAlignmentOptions.Center);
            StretchToFill(howToText.rectTransform, 24f, 24f, 0f, 2f);
            howToText.fontStyle = FontStyles.Bold;

            Button resetButton = CreateImageButton(contentRect, "ResetProgressButton", PremiumUISprites.SecondaryButton, Color.white);
            SetAnchored(resetButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(540f, 78f), new Vector2(0f, 72f));
            resetButton.onClick.AddListener(ShowResetConfirmation);
            TextMeshProUGUI resetText = CreateText(resetButton.transform, "Label", "Сброс прогресса", 32f, textCream, TextAlignmentOptions.Center);
            StretchToFill(resetText.rectTransform, 24f, 24f, 0f, 2f);
            resetText.fontStyle = FontStyles.Bold;
            AddTextOutline(resetText, new Color(0f, 0.06f, 0.02f, 0.52f), new Vector2(1f, -1f));

            GameObject modalObject = CreateChild(gameObject, "ModalLayer", typeof(RectTransform));
            modalLayer = modalObject.transform as RectTransform;
            StretchToFill(modalLayer, 0f, 0f, 0f, 0f);
            modalObject.SetActive(false);
        }

        private Slider CreateSliderRow(RectTransform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction<float> changed, out TextMeshProUGUI valueText)
        {
            GameObject row = CreateChild(parent.gameObject, name, typeof(RectTransform));
            RectTransform rowRect = row.transform as RectTransform;
            SetAnchored(rowRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(600f, 88f), position);

            TextMeshProUGUI labelText = CreateText(rowRect, "Label", label, 34f, textCream, TextAlignmentOptions.Left);
            SetAnchored(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f, 48f), new Vector2(0f, 0f));
            labelText.fontStyle = FontStyles.Bold;
            AddTextOutline(labelText, new Color(0f, 0.06f, 0.02f, 0.58f), new Vector2(1f, -1f));

            valueText = CreateText(rowRect, "Value", "70%", 28f, textGold, TextAlignmentOptions.Right);
            SetAnchored(valueText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(90f, 42f), new Vector2(0f, 0f));
            valueText.fontStyle = FontStyles.Bold;

            GameObject sliderObject = CreateChild(row, "Slider", typeof(RectTransform), typeof(Slider));
            RectTransform sliderRect = sliderObject.transform as RectTransform;
            SetAnchored(sliderRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-270f, 42f), new Vector2(40f, 0f));

            GameObject background = CreateChild(sliderObject, "Background", typeof(Image));
            RectTransform backgroundRect = background.transform as RectTransform;
            StretchToFill(backgroundRect, 0f, 0f, 12f, 12f);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = PremiumUISprites.CategorySlot;
            backgroundImage.type = backgroundImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            backgroundImage.color = new Color(0.01f, 0.12f, 0.05f, 0.78f);

            GameObject fillArea = CreateChild(sliderObject, "Fill Area", typeof(RectTransform));
            RectTransform fillAreaRect = fillArea.transform as RectTransform;
            StretchToFill(fillAreaRect, 8f, 8f, 14f, 14f);

            GameObject fill = CreateChild(fillArea, "Fill", typeof(Image));
            RectTransform fillRect = fill.transform as RectTransform;
            StretchToFill(fillRect, 0f, 0f, 0f, 0f);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.sprite = PremiumUISprites.PrimaryButton;
            fillImage.type = fillImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            fillImage.color = new Color(1f, 0.72f, 0.12f, 1f);

            GameObject handle = CreateChild(sliderObject, "Handle", typeof(Image));
            RectTransform handleRect = handle.transform as RectTransform;
            SetAnchored(handleRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, 46f), Vector2.zero);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.sprite = PremiumUISprites.IconCircle;
            handleImage.type = handleImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            handleImage.color = new Color(1f, 0.84f, 0.22f, 1f);

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

        private void CreateHintsRow(RectTransform parent, Vector2 position)
        {
            GameObject row = CreateChild(parent.gameObject, "HintsRow", typeof(RectTransform));
            RectTransform rowRect = row.transform as RectTransform;
            SetAnchored(rowRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(600f, 88f), position);

            TextMeshProUGUI labelText = CreateText(rowRect, "Label", "Подсказки", 34f, textCream, TextAlignmentOptions.Left);
            SetAnchored(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(260f, 48f), new Vector2(0f, 0f));
            labelText.fontStyle = FontStyles.Bold;
            AddTextOutline(labelText, new Color(0f, 0.06f, 0.02f, 0.58f), new Vector2(1f, -1f));

            Button toggleButton = CreateImageButton(rowRect, "HintsToggle", PremiumUISprites.SecondaryButton, Color.white);
            SetAnchored(toggleButton.transform as RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(190f, 64f), new Vector2(0f, 0f));
            toggleButton.onClick.AddListener(ToggleHints);
            hintsValueText = CreateText(toggleButton.transform, "Label", "Вкл", 30f, textCream, TextAlignmentOptions.Center);
            StretchToFill(hintsValueText.rectTransform, 16f, 16f, 0f, 2f);
            hintsValueText.fontStyle = FontStyles.Bold;
            AddTextOutline(hintsValueText, new Color(0f, 0.06f, 0.02f, 0.52f), new Vector2(1f, -1f));
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
                musicValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
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
                sfxValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        private void ToggleHints()
        {
            hintsEnabled = !hintsEnabled;
            PlayerPrefs.SetInt(HintsEnabledKey, hintsEnabled ? 1 : 0);
            PlayerPrefs.Save();
            RefreshHintsLabel();
        }

        private void RefreshHintsLabel()
        {
            if (hintsValueText != null)
            {
                hintsValueText.text = hintsEnabled ? "Вкл" : "Выкл";
                hintsValueText.color = hintsEnabled ? textCream : new Color(0.76f, 0.78f, 0.66f, 1f);
            }
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
            if (modalLayer == null)
            {
                return;
            }

            CloseModal();
            modalLayer.gameObject.SetActive(true);

            GameObject blocker = CreateChild(modalLayer.gameObject, "Blocker", typeof(Image));
            StretchToFill(blocker.transform as RectTransform, 0f, 0f, 0f, 0f);
            blocker.GetComponent<Image>().color = new Color(0f, 0.02f, 0.01f, 0.52f);

            GameObject modalPanelObject = CreateChild(modalLayer.gameObject, "ModalPanel", typeof(Image));
            RectTransform modalPanel = modalPanelObject.transform as RectTransform;
            SetAnchored(modalPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(660f, 430f), new Vector2(0f, 118f));
            Image modalImage = modalPanelObject.GetComponent<Image>();
            modalImage.sprite = PremiumUISprites.EmeraldPanel;
            modalImage.type = modalImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            modalImage.color = new Color(0.93f, 1f, 0.86f, 1f);
            AddShadow(modalPanelObject, new Color(0f, 0.03f, 0.01f, 0.58f), new Vector2(0f, -10f));
            AddOutline(modalPanelObject, new Color(1f, 0.78f, 0.22f, 0.86f), new Vector2(3f, -3f));

            TextMeshProUGUI titleText = CreateText(modalPanel, "Title", title, 46f, textGold, TextAlignmentOptions.Center);
            SetAnchored(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(480f, 70f), new Vector2(0f, -38f));
            titleText.fontStyle = FontStyles.Bold;
            AddTextOutline(titleText, new Color(0.02f, 0.08f, 0.03f, 0.8f), new Vector2(1.4f, -1.4f));

            Button closeButton = CreateImageButton(modalPanel, "CloseButton", PremiumUISprites.IconCircle, new Color(1f, 0.82f, 0.18f, 1f));
            SetAnchored(closeButton.transform as RectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(58f, 58f), new Vector2(-42f, -42f));
            closeButton.onClick.AddListener(CloseModal);
            TextMeshProUGUI closeLabel = CreateText(closeButton.transform, "Label", "X", 24f, textDark, TextAlignmentOptions.Center);
            StretchToFill(closeLabel.rectTransform, 0f, 0f, 0f, 2f);
            closeLabel.fontStyle = FontStyles.Bold;

            TextMeshProUGUI bodyText = CreateText(modalPanel, "Body", body, 28f, textCream, TextAlignmentOptions.Center);
            SetAnchored(bodyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(540f, 170f), new Vector2(0f, 16f));
            bodyText.enableWordWrapping = true;
            bodyText.fontStyle = FontStyles.Bold;
            AddTextOutline(bodyText, new Color(0f, 0.06f, 0.02f, 0.42f), new Vector2(1f, -1f));

            float primaryX = secondary.HasValue ? -138f : 0f;
            Button primaryButton = CreateModalButton(modalPanel, primary.label, new Vector2(primaryX, -154f));
            primaryButton.onClick.AddListener(() => primary.action?.Invoke());

            if (secondary.HasValue)
            {
                Button secondaryButton = CreateModalButton(modalPanel, secondary.Value.label, new Vector2(138f, -154f));
                secondaryButton.onClick.AddListener(() => secondary.Value.action?.Invoke());
            }
        }

        private Button CreateModalButton(RectTransform parent, string label, Vector2 position)
        {
            Button button = CreateImageButton(parent, $"{label}Button", PremiumUISprites.PrimaryButton, Color.white);
            SetAnchored(button.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(220f, 68f), position);
            TextMeshProUGUI text = CreateText(button.transform, "Label", label, 30f, textDark, TextAlignmentOptions.Center);
            StretchToFill(text.rectTransform, 18f, 18f, 0f, 2f);
            text.fontStyle = FontStyles.Bold;
            return button;
        }

        private void CloseModal()
        {
            if (modalLayer == null)
            {
                return;
            }

            for (int index = modalLayer.childCount - 1; index >= 0; index--)
            {
                Destroy(modalLayer.GetChild(index).gameObject);
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

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            float startAlpha = show ? 0f : canvasGroup.alpha;
            float endAlpha = show ? 1f : 0f;
            Vector3 startScale = show ? new Vector3(0.92f, 0.92f, 1f) : panel.localScale;
            Vector3 endScale = show ? Vector3.one : new Vector3(0.92f, 0.92f, 1f);

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
                panel.localScale = new Vector3(0.92f, 0.92f, 1f);
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
            colors.highlightedColor = new Color(1f, 0.96f, 0.76f, 1f);
            colors.pressedColor = new Color(0.9f, 0.68f, 0.18f, 1f);
            colors.disabledColor = new Color(0.48f, 0.48f, 0.42f, 0.65f);
            button.colors = colors;

            buttonObject.AddComponent<UIJuicyButton>();
            AddShadow(buttonObject, new Color(0.02f, 0.01f, 0f, 0.32f), new Vector2(0f, -5f));
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
            return label;
        }

        private static GameObject CreateChild(GameObject parent, string objectName, params Type[] components)
        {
            GameObject child = new(objectName, components);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static void SetAnchored(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.sizeDelta = size;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private static void StretchToFill(RectTransform rectTransform, float left, float right, float top, float bottom)
        {
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
