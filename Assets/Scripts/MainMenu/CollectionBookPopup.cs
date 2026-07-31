using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    [ExecuteAlways]
    public sealed class CollectionBookPopup : MonoBehaviour
    {
        private const string PopupName = "CollectionBookPopup";

        [Header("Runtime bindings")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text progressValueText;
        [SerializeField] private TMP_Text nextRewardText;
        [SerializeField] private TMP_Text levelTitleText;
        [SerializeField] private TMP_Text levelProgressText;
        [SerializeField] private TMP_Text levelRemainingText;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image levelProgressFill;
        [SerializeField] private Image rewardProgressLine;
        [SerializeField] private Image rewardProgressMarker;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button infoButton;
        [SerializeField] private RectTransform rewardCardsRoot;
        [SerializeField] private RectTransform scoreRowsRoot;
        [SerializeField] private CollectionBookPremiumMotion premiumMotion;

        [Header("Shared prefab references")]
        [SerializeField] private PremiumEventPrefabReferences sharedPrefabs = new();

        private CollectionBookManager manager;
        private bool hasShownRuntime;
        private Coroutine openRoutine;
        private float lastRenderedProgress = -1f;
        private int lastClaimedRewards = -1;
        private static TMP_FontAsset rewardCardFont;
#if UNITY_EDITOR
        private bool editorRefreshQueued;
#endif

        public static CollectionBookPopup SceneInstance =>
            FindFirstObjectByType<CollectionBookPopup>(FindObjectsInactive.Include);

        private void Reset()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                sharedPrefabs.AssignDefaultsIfMissing();
            }
#endif
            BindSceneReferences();
        }

        private void Awake()
        {
            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireButtons();

            if (Application.isPlaying && !hasShownRuntime)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                sharedPrefabs.AssignDefaultsIfMissing();
            }
#endif

            if (Application.isPlaying)
            {
                BindSceneReferences();
                ApplyFontsToAllTexts();
                WireButtons();
                Refresh();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                sharedPrefabs.AssignDefaultsIfMissing();
            }
        }
#endif

        private void OnDestroy()
        {
            UnwireButtons();
            if (manager != null)
            {
                manager.StateChanged -= Refresh;
            }
        }

        public void Show(CollectionBookManager targetManager)
        {
            manager = targetManager != null ? targetManager : CollectionBookManager.GetOrCreate(this);
            manager.StateChanged -= Refresh;
            manager.StateChanged += Refresh;

            hasShownRuntime = true;
            gameObject.SetActive(true);
            BindSceneReferences();
            ApplyFontsToAllTexts();
            WireButtons();
            Refresh();
            if (premiumMotion != null)
            {
                premiumMotion.PlayOpenSequence();
            }

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

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void ShowRules()
        {
            if (nextRewardText == null || manager == null)
            {
                return;
            }

            nextRewardText.text =
                $"Обычная категория +{manager.NormalCategoryPoints}, категория с картинками +{manager.ImageCategoryPoints}, без подсказки +{manager.NoHintBonusPoints}, без отмены +{manager.NoUndoBonusPoints}.";
            RuntimeCyrillicFontProvider.Apply(nextRewardText, nextRewardText.text);
        }

        private void Refresh()
        {
            if (!Application.isPlaying && manager == null)
            {
                RefreshEditorPreview();
                return;
            }

            manager ??= CollectionBookManager.GetOrCreate(this);
            CollectionBookReward next = manager.GetNextReward();
            int previous = manager.GetPreviousMilestonePoints();
            int target = next != null ? next.RequiredPoints : Mathf.Max(1, manager.CurrentPoints);
            int span = Mathf.Max(1, target - previous);
            int localProgress = Mathf.Clamp(manager.CurrentPoints - previous, 0, span);

            SetText(titleText, "КНИГА КОЛЛЕКЦИЙ");
            SetText(subtitleText, "Собирай категории, пополняй книгу и получай ценные награды!");
            SetText(progressValueText, next != null ? $"{manager.CurrentPoints} / {target}" : $"{manager.CurrentPoints}");
            SetText(nextRewardText, next != null ? $"Следующая награда: {next.Title}" : "Все текущие награды получены");
            SetText(levelTitleText, manager.GetLevelTitle().ToUpperInvariant());
            SetText(levelProgressText, next != null ? $"{localProgress} / {span} ОЧКОВ" : $"{manager.CurrentPoints} ОЧКОВ");
            SetText(levelRemainingText, next != null
                ? $"До следующего уровня: {Mathf.Max(0, span - localProgress)} очков"
                : "Все уровни пройдены");

            float targetProgress = next != null ? Mathf.Clamp01((float)manager.CurrentPoints / target) : 1f;
            if (progressFill != null)
            {
                bool canAnimate = premiumMotion != null
                    && gameObject.activeInHierarchy
                    && lastRenderedProgress >= 0f;
                int claimedRewardIndex = lastClaimedRewards >= 0
                    && manager.ClaimedRewardsCount > lastClaimedRewards
                        ? manager.ClaimedRewardsCount - 1
                        : -1;

                if (canAnimate)
                {
                    premiumMotion.AnimateProgress(targetProgress, claimedRewardIndex);
                }
                else
                {
                    progressFill.fillAmount = targetProgress;
                }
            }

            if (levelProgressFill != null)
            {
                levelProgressFill.fillAmount = next != null ? Mathf.Clamp01((float)localProgress / span) : 1f;
            }

            RefreshRewardCards();
            RefreshRewardProgressMarker();
            premiumMotion?.SetActiveReward(manager.ClaimedRewardsCount);
            lastRenderedProgress = targetProgress;
            lastClaimedRewards = manager.ClaimedRewardsCount;
        }

        private void RefreshEditorPreview()
        {
            SetText(titleText, "КНИГА КОЛЛЕКЦИЙ");
            SetText(subtitleText, "Собирай категории, пополняй книгу и получай ценные награды!");
            SetText(progressValueText, "0 / 10");
            SetText(nextRewardText, "Следующая награда: +1 Отмена");
            SetText(levelTitleText, "КОЛЛЕКЦИОНЕР I");
            SetText(levelProgressText, "0 / 10 ОЧКОВ");
            SetText(levelRemainingText, "До следующего уровня: 10 очков");
            if (progressFill != null)
            {
                progressFill.fillAmount = 0f;
            }

            if (levelProgressFill != null)
            {
                levelProgressFill.fillAmount = 0f;
            }

            RefreshRewardCards();
        }

        private void RefreshRewardCards()
        {
            if (rewardCardsRoot == null)
            {
                return;
            }

            int rewardIndex = 0;
            for (int childIndex = 0; childIndex < rewardCardsRoot.childCount; childIndex++)
            {
                Transform card = rewardCardsRoot.GetChild(childIndex);
                if (!card.name.StartsWith("RewardStage_"))
                {
                    continue;
                }

                CollectionBookReward reward = manager != null ? manager.GetReward(rewardIndex) : null;
                int required = reward != null ? reward.RequiredPoints : new[] { 10, 25, 50, 75, 100, 150 }[Mathf.Min(rewardIndex, 5)];
                string rewardTitle = reward != null ? reward.Title : new[] { "+1 Отмена", "+1 Подсказка", "50 монет", "+2 Отмены", "+2 Подсказки", "+3 хода" }[Mathf.Min(rewardIndex, 5)];
                bool claimed = manager != null && rewardIndex < manager.ClaimedRewardsCount;
                bool current = manager == null ? rewardIndex == 0 : rewardIndex == manager.ClaimedRewardsCount;

                SetText(card.Find("Points")?.GetComponent<TMP_Text>(), required.ToString());
                SetRewardCardText(card.Find("Reward")?.GetComponent<TMP_Text>(), rewardTitle);
                SetText(card.Find("Status")?.GetComponent<TMP_Text>(), claimed ? "ПОЛУЧЕНО" : current ? "СЛЕДУЮЩАЯ" : "ЗАБЛОКИРОВАНО");
                Image cardImage = card.GetComponent<Image>();
                if (cardImage != null)
                {
                    cardImage.color = claimed
                        ? new Color(0.08f, 0.27f, 0.16f, 0.98f)
                        : current
                            ? new Color(0.075f, 0.31f, 0.18f, 0.99f)
                            : new Color(0.045f, 0.19f, 0.115f, 0.96f);
                }
                rewardIndex++;
            }
        }

        private void RefreshRewardProgressMarker()
        {
            if (rewardCardsRoot == null || manager == null)
            {
                return;
            }

            RectTransform firstCard = rewardCardsRoot.Find("RewardStage_1") as RectTransform;
            Image firstMilestone = firstCard != null ? firstCard.Find("MilestoneDiamond")?.GetComponent<Image>() : null;
            if (firstCard == null || firstMilestone == null)
            {
                return;
            }

            if (rewardProgressMarker == null)
            {
                Transform existing = rewardCardsRoot.Find("CurrentProgressMarker");
                rewardProgressMarker = existing != null ? existing.GetComponent<Image>() : null;
            }

            if (rewardProgressLine == null)
            {
                Transform existing = rewardCardsRoot.Find("CurrentProgressLine");
                rewardProgressLine = existing != null ? existing.GetComponent<Image>() : null;
            }

            // The progress visuals are materialized in MainMenu.unity by the
            // editor installer. Runtime never creates scene layout objects.
            if (rewardProgressMarker == null || rewardProgressLine == null)
            {
                return;
            }

            rewardProgressMarker.sprite = firstMilestone.sprite;
            rewardProgressMarker.type = Image.Type.Simple;
            rewardProgressMarker.preserveAspect = true;
            rewardProgressMarker.raycastTarget = false;
            rewardProgressMarker.color = new Color(0.35f, 1f, 0.55f, 1f);

            RectTransform markerRect = rewardProgressMarker.rectTransform;
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = new Vector2(40f, 44f);
            markerRect.localScale = Vector3.one * 1.12f;
            markerRect.SetAsLastSibling();

            float firstCardX = GetLocalCenterX(firstCard, rewardCardsRoot);
            float markerX = firstCardX - 108f;
            int previousRequired = 0;
            float previousX = markerX;
            int visibleRewardCount = 0;

            for (int index = 0; ; index++)
            {
                RectTransform card = rewardCardsRoot.Find($"RewardStage_{index + 1}") as RectTransform;
                CollectionBookReward reward = manager.GetReward(index);
                if (card == null || reward == null)
                {
                    break;
                }

                visibleRewardCount++;
                if (manager.CurrentPoints <= reward.RequiredPoints)
                {
                    float segmentProgress = Mathf.InverseLerp(previousRequired, reward.RequiredPoints, manager.CurrentPoints);
                    markerX = Mathf.Lerp(previousX, GetLocalCenterX(card, rewardCardsRoot), segmentProgress);
                    break;
                }

                previousRequired = reward.RequiredPoints;
                previousX = GetLocalCenterX(card, rewardCardsRoot);
                markerX = previousX;
            }

            rewardProgressMarker.gameObject.SetActive(visibleRewardCount > 0);
            float timelineY = firstMilestone.rectTransform.anchoredPosition.y;
            float timelineStartX = firstCardX - 108f;

            rewardProgressLine.sprite = null;
            rewardProgressLine.type = Image.Type.Simple;
            rewardProgressLine.raycastTarget = false;
            rewardProgressLine.color = new Color(0.2f, 0.95f, 0.43f, 1f);
            rewardProgressLine.gameObject.SetActive(visibleRewardCount > 0);

            RectTransform lineRect = rewardProgressLine.rectTransform;
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0f, 0.5f);
            lineRect.anchoredPosition = new Vector2(timelineStartX, timelineY);
            lineRect.sizeDelta = new Vector2(Mathf.Max(0f, markerX - timelineStartX), 6f);
            lineRect.localScale = Vector3.one;
            lineRect.SetAsLastSibling();

            markerRect.anchoredPosition = new Vector2(markerX, timelineY);
            markerRect.SetAsLastSibling();
        }

        private IEnumerator PlayOpenAnimation()
        {
            float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = eased;
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            openRoutine = null;
        }

        private void BindSceneReferences()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            panel = FindRect("Panel");
            titleText = FindTMP("Panel/Header/Title");
            subtitleText = FindTMP("Panel/Header/Subtitle");
            progressValueText = FindTMP("Panel/ProgressBlock/ProgressValue");
            nextRewardText = FindTMP("Panel/ProgressBlock/NextReward");
            levelTitleText = FindTMP("Panel/LevelBlock/LevelTitle");
            levelProgressText = FindTMP("Panel/LevelBlock/LevelProgress");
            levelRemainingText = FindTMP("Panel/LevelBlock/LevelRemaining");
            progressFill = transform.Find("Panel/ProgressBlock/ProgressTrack/ProgressFill")?.GetComponent<Image>();
            levelProgressFill = transform.Find("Panel/LevelBlock/LevelProgressTrack/LevelProgressFill")?.GetComponent<Image>();
            ConfigureProgressFill(progressFill);
            ConfigureProgressFill(levelProgressFill);
            closeButton = transform.Find("Panel/CloseButton")?.GetComponent<Button>();
            infoButton = transform.Find("Panel/InfoButton")?.GetComponent<Button>();
            rewardCardsRoot = FindRect("Panel/RewardStages");
            scoreRowsRoot = FindRect("Panel/ScoreRules");
            premiumMotion = GetComponent<CollectionBookPremiumMotion>();
        }

        private static void ConfigureProgressFill(Image fill)
        {
            if (fill == null)
            {
                return;
            }

            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillClockwise = true;
        }

        private void WireButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }

            if (infoButton != null)
            {
                infoButton.onClick.RemoveListener(ShowRules);
                infoButton.onClick.AddListener(ShowRules);
            }
        }

        private void UnwireButtons()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
            if (infoButton != null) infoButton.onClick.RemoveListener(ShowRules);
        }

        private RectTransform FindRect(string path) => transform.Find(path)?.GetComponent<RectTransform>();

        private static float GetLocalCenterX(RectTransform child, RectTransform parent)
        {
            if (child == null || parent == null)
            {
                return 0f;
            }

            return (child.anchorMin.x - parent.pivot.x) * parent.rect.width + child.anchoredPosition.x;
        }

        private TMP_Text FindTMP(string path)
        {
            TMP_Text text = transform.Find(path)?.GetComponent<TMP_Text>();
            if (text != null)
            {
                RuntimeCyrillicFontProvider.Apply(text, text.text);
            }

            return text;
        }

        private void ApplyFontsToAllTexts()
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                RuntimeCyrillicFontProvider.Apply(texts[i], texts[i].text);
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            text.text = value;
            RuntimeCyrillicFontProvider.Apply(text, value);
        }

        private static void SetRewardCardText(TMP_Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            int separator = string.IsNullOrEmpty(value) ? -1 : value.IndexOf(' ');
            string formatted = separator > 0
                ? value.Substring(0, separator) + "\n" + value.Substring(separator + 1)
                : value;

            text.text = formatted;
            rewardCardFont ??= Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF");
            if (rewardCardFont != null)
            {
                rewardCardFont.TryAddCharacters(formatted, out _);
                text.font = rewardCardFont;
                text.fontSharedMaterial = rewardCardFont.material;
            }
            else
            {
                RuntimeCyrillicFontProvider.Apply(text, formatted);
            }

            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = 23f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.lineSpacing = -8f;
        }

#if UNITY_EDITOR
        private void EnsureOpenBookButtonIsNonInteractive()
        {
            RectTransform openButton = transform.Find("Panel/OpenBookButton") as RectTransform;
            if (openButton == null)
            {
                return;
            }

            Button button = openButton.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
                button.enabled = false;
            }

            UIJuicyButton juicyButton = openButton.GetComponent<UIJuicyButton>();
            if (juicyButton != null)
            {
                juicyButton.enabled = false;
            }

            Image image = openButton.GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = 0f;
                image.color = color;
                image.raycastTarget = false;
            }

            TMP_Text label = openButton.Find("Label")?.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.raycastTarget = false;
                Color color = label.color;
                color.a = 0f;
                label.color = color;
            }

            CanvasGroup group = GetOrCreateCanvasGroup(openButton.gameObject);
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            openButton.gameObject.SetActive(false);
        }

        private static CanvasGroup GetOrCreateCanvasGroup(GameObject owner)
        {
            if (owner == null)
            {
                return null;
            }

            CanvasGroup group = owner.GetComponent<CanvasGroup>();
            return group != null ? group : owner.AddComponent<CanvasGroup>();
        }

        private static void ApplyPopupPanelVisual(Image panelImage)
        {
            panelImage.sprite = GetPopupPanelSprite();
            panelImage.type = PremiumUISprites.PauseWindowCardMesh != null ? Image.Type.Simple : Image.Type.Sliced;
            panelImage.color = Color.white;
        }

        private static Sprite GetPopupPanelSprite()
        {
            return PremiumUISprites.PauseWindowCardMesh != null
                ? PremiumUISprites.PauseWindowCardMesh
                : RoyalEventSprites.Panel != null
                    ? RoyalEventSprites.Panel
                    : PremiumUISprites.EmeraldPanel;
        }

        public void MaterializeAuthoringLayout()
        {
            if (Application.isPlaying)
            {
                return;
            }

            BindSceneReferences();
            UpgradeAuthoringLayout();
            EnsureProgressVisuals();
            EnsurePremiumMotion();
            BindSceneReferences();
            ApplyFontsToAllTexts();
            RefreshEditorPreview();
        }

        private void EnsureAuthoringLayout()
        {
            if (transform.Find("Panel") != null)
            {
                UpgradeAuthoringLayout();
                return;
            }

            sharedPrefabs.AssignDefaultsIfMissing();

            Image dim = GetComponent<Image>();
            if (dim != null)
            {
                dim.color = new Color(0f, 0.025f, 0.012f, 0.78f);
                dim.raycastTarget = true;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            panel = CreateRect(transform, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1500f, 980f), Vector2.zero);
            Image panelImage = panel.gameObject.AddComponent<Image>();
            ApplyPopupPanelVisual(panelImage);
            Shadow panelShadow = panel.gameObject.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(1f, 0.74f, 0.12f, 0f);
            panelShadow.effectDistance = Vector2.zero;

            RectTransform header = CreateRect(panel, "Header", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1220f, 178f), new Vector2(0f, -36f));
            titleText = CreateText(header, "Title", "КНИГА КОЛЛЕКЦИЙ", 56f, new Color(1f, 0.78f, 0.18f, 1f), FontStyles.Bold, new Vector2(0f, -34f), new Vector2(1120f, 64f));
            subtitleText = CreateText(header, "Subtitle", "Собирай категории, пополняй свою коллекцию и получай ценные награды!", 31f, new Color(1f, 0.88f, 0.62f, 1f), FontStyles.Bold, new Vector2(0f, -102f), new Vector2(1120f, 70f));

            Button info = CreateIconButton(panel, "InfoButton", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(80f, 80f), new Vector2(84f, -86f), "i");
            infoButton = info;

            Button close = CreateIconButton(panel, "CloseButton", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(104f, 104f), new Vector2(-84f, -76f), "X");
            closeButton = close;

            RectTransform bookHero = CreateRect(panel, "BookHero", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460f, 380f), new Vector2(330f, 184f));
            Image bookGlow = CreateImage(bookHero, "Glow", PremiumUISprites.IconCircle, new Color(1f, 0.78f, 0.12f, 0f), new Vector2(0f, 0f), new Vector2(430f, 430f), Image.Type.Simple);
            bookGlow.gameObject.SetActive(false);
            RectTransform book = CreateRect(bookHero, "Book", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(260f, 290f), new Vector2(0f, 0f));
            Image bookImage = book.gameObject.AddComponent<Image>();
            bookImage.sprite = PremiumUISprites.EmeraldPanel;
            bookImage.type = Image.Type.Sliced;
            bookImage.color = new Color(0.05f, 0.28f, 0.16f, 1f);
            Outline bookOutline = book.GetComponent<Outline>();
            if (bookOutline != null)
            {
                bookOutline.effectColor = new Color(1f, 0.76f, 0.16f, 0f);
            }
            TMP_Text oldGem = CreateText(book, "Gem", string.Empty, 90f, new Color(0.08f, 0.94f, 0.47f, 1f), FontStyles.Bold, new Vector2(0f, 8f), new Vector2(140f, 120f));
            oldGem.gameObject.SetActive(false);
            TMP_Text oldBookLabel = CreateText(book, "Crown", string.Empty, 42f, new Color(1f, 0.78f, 0.13f, 1f), FontStyles.Bold, new Vector2(0f, -74f), new Vector2(170f, 88f));
            oldBookLabel.gameObject.SetActive(false);

            RectTransform progressBlock = CreateRect(panel, "ProgressBlock", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(610f, 300f), new Vector2(-420f, 194f));
            Image progressPanelImage = progressBlock.gameObject.AddComponent<Image>();
            progressPanelImage.sprite = PremiumUISprites.EmeraldPanel;
            progressPanelImage.type = Image.Type.Sliced;
            progressPanelImage.color = new Color(0.02f, 0.14f, 0.08f, 0.86f);
            CreateText(progressBlock, "ProgressTitle", "ТЕКУЩИЙ ПРОГРЕСС", 28f, new Color(1f, 0.78f, 0.26f, 1f), FontStyles.Bold, new Vector2(0f, 96f), new Vector2(540f, 48f));
            progressValueText = CreateText(progressBlock, "ProgressValue", "28 / 50", 58f, new Color(1f, 0.91f, 0.68f, 1f), FontStyles.Bold, new Vector2(0f, 36f), new Vector2(540f, 76f));
            RectTransform track = CreateRect(progressBlock, "ProgressTrack", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500f, 46f), new Vector2(0f, -30f));
            Image trackImage = track.gameObject.AddComponent<Image>();
            trackImage.sprite = RoyalEventSprites.ProgressBack != null ? RoyalEventSprites.ProgressBack : PremiumUISprites.HudCounterGold;
            trackImage.type = Image.Type.Sliced;
            progressFill = CreateImage(track, "ProgressFill", RoyalEventSprites.ProgressFill, Color.white, new Vector2(0f, 0f), new Vector2(0f, 0f), Image.Type.Filled);
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            RectTransform fillRect = progressFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            nextRewardText = CreateText(progressBlock, "NextReward", "Следующая награда через: 22 очка", 26f, new Color(1f, 0.9f, 0.66f, 1f), FontStyles.Bold, new Vector2(0f, -100f), new Vector2(540f, 52f));

            CreateSectionTitle(panel, "RewardTitle", "ЭТАПЫ НАГРАД", new Vector2(0f, -160f));
            rewardCardsRoot = CreateRect(panel, "RewardStages", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1300f, 265f), new Vector2(0f, 252f));
            for (int i = 0; i < 6; i++)
            {
                CreateRewardCard(rewardCardsRoot, i);
            }

            scoreRowsRoot = CreateRect(panel, "ScoreRules", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(575f, 180f), new Vector2(340f, 116f));
            Image scoreImage = scoreRowsRoot.gameObject.AddComponent<Image>();
            scoreImage.sprite = PremiumUISprites.EmeraldPanel;
            scoreImage.type = Image.Type.Sliced;
            scoreImage.color = new Color(0.01f, 0.11f, 0.06f, 0.86f);
            CreateText(scoreRowsRoot, "Title", "КАК ПОЛУЧАТЬ ОЧКИ?", 27f, new Color(1f, 0.78f, 0.23f, 1f), FontStyles.Bold, new Vector2(0f, 58f), new Vector2(500f, 42f));
            CreateText(scoreRowsRoot, "Rows", "Обычная категория  +1 очко\nКатегория с картинками  +2 очка\nБез подсказок  +1 бонус\nБез отмены  +1 бонус", 22f, new Color(1f, 0.9f, 0.68f, 1f), FontStyles.Bold, new Vector2(0f, -30f), new Vector2(500f, 110f));

            RectTransform levelBlock = CreateRect(panel, "LevelBlock", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(575f, 180f), new Vector2(-340f, 116f));
            Image levelImage = levelBlock.gameObject.AddComponent<Image>();
            levelImage.sprite = PremiumUISprites.EmeraldPanel;
            levelImage.type = Image.Type.Sliced;
            levelImage.color = new Color(0.01f, 0.11f, 0.06f, 0.86f);
            CreateText(levelBlock, "Title", "УРОВЕНЬ КОЛЛЕКЦИИ", 27f, new Color(1f, 0.78f, 0.23f, 1f), FontStyles.Bold, new Vector2(0f, 58f), new Vector2(500f, 42f));
            levelTitleText = CreateText(levelBlock, "LevelTitle", "ИССЛЕДОВАТЕЛЬ КАТЕГОРИЙ", 30f, new Color(1f, 0.83f, 0.35f, 1f), FontStyles.Bold, new Vector2(92f, 0f), new Vector2(360f, 74f));
            levelProgressText = CreateText(levelBlock, "LevelProgress", "28 / 50", 22f, new Color(1f, 0.88f, 0.62f, 1f), FontStyles.Bold, new Vector2(92f, -68f), new Vector2(280f, 34f));
            CreateText(levelBlock, "Medal", "III", 64f, new Color(1f, 0.78f, 0.18f, 1f), FontStyles.Bold, new Vector2(-190f, -12f), new Vector2(150f, 110f));

            Button openButton = CreateTextButton(panel, "OpenBookButton", "ОТКРЫТЬ КНИГУ", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(520f, 96f), new Vector2(0f, 54f));
            openButton.interactable = false;

            UpgradeAuthoringLayout();
            BindSceneReferences();
            WireButtons();
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

                EnsureAuthoringLayout();
                BindSceneReferences();
                ApplyFontsToAllTexts();
                RefreshEditorPreview();
            };
        }

        private void UpgradeAuthoringLayout()
        {
            sharedPrefabs.AssignDefaultsIfMissing();
            HideOpenBookButton();
            UpgradeCollectionHero();
            UpgradeProgressBlock();
            UpgradeRewardCards();
            AlignMilestoneLine();
            UpgradeBottomBlocks();
        }

        private void AlignMilestoneLine()
        {
            RectTransform panelRect = transform.Find("Panel") as RectTransform;
            RectTransform line = transform.Find("Panel/MilestoneLine") as RectTransform;
            RectTransform firstMilestone = transform.Find("Panel/RewardStages/RewardStage_1/MilestoneDiamond") as RectTransform;
            if (panelRect == null || line == null || firstMilestone == null)
            {
                return;
            }

            float milestoneY = panelRect.InverseTransformPoint(firstMilestone.position).y;
            line.anchorMin = new Vector2(0.5f, 0.5f);
            line.anchorMax = line.anchorMin;
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = new Vector2(0f, milestoneY);
            line.sizeDelta = new Vector2(1080f, 2f);
        }

        private void EnsureProgressVisuals()
        {
            RectTransform progressBlock = transform.Find("Panel/ProgressBlock") as RectTransform;
            if (progressBlock != null)
            {
                Image highlight = progressBlock.Find("InnerHighlight")?.GetComponent<Image>();
                if (highlight == null)
                {
                    highlight = CreateImage(
                        progressBlock,
                        "InnerHighlight",
                        PremiumUISprites.EmeraldPanel,
                        new Color(1f, 0.88f, 0.42f, 0.075f),
                        Vector2.zero,
                        new Vector2(540f, 122f),
                        Image.Type.Sliced);
                }

                highlight.sprite = PremiumUISprites.EmeraldPanel;
                highlight.sprite = null;
                highlight.type = Image.Type.Simple;
                highlight.color = new Color(1f, 0.84f, 0.32f, 0.13f);
                highlight.raycastTarget = false;
                SetTextRect(highlight.rectTransform, new Vector2(0f, 61f), new Vector2(470f, 2f));
                highlight.rectTransform.SetAsFirstSibling();
            }

            RectTransform root = transform.Find("Panel/RewardStages") as RectTransform;
            if (root == null)
            {
                return;
            }

            RectTransform firstCard = root.Find("RewardStage_1") as RectTransform;
            Image firstMilestone = firstCard != null
                ? firstCard.Find("MilestoneDiamond")?.GetComponent<Image>()
                : null;
            if (firstMilestone == null)
            {
                return;
            }

            if (root.Find("CurrentProgressLine") == null)
            {
                Image line = CreateImage(
                    root,
                    "CurrentProgressLine",
                    null,
                    new Color(0.58f, 0.88f, 0.2f, 0.94f),
                    Vector2.zero,
                    new Vector2(1f, 5f),
                    Image.Type.Simple);
                line.rectTransform.SetAsLastSibling();
            }

            if (root.Find("CurrentProgressMarker") == null)
            {
                Image marker = CreateImage(
                    root,
                    "CurrentProgressMarker",
                    firstMilestone.sprite,
                    new Color(0.52f, 1f, 0.45f, 1f),
                    Vector2.zero,
                    new Vector2(40f, 44f),
                    Image.Type.Simple);
                marker.preserveAspect = true;
                marker.rectTransform.SetAsLastSibling();
            }
        }

        private void EnsurePremiumMotion()
        {
            premiumMotion = GetComponent<CollectionBookPremiumMotion>();
            if (premiumMotion == null)
            {
                premiumMotion = gameObject.AddComponent<CollectionBookPremiumMotion>();
            }
        }

        private void StyleHeader()
        {
            RectTransform header = transform.Find("Panel/Header") as RectTransform;
            if (header != null)
            {
                header.sizeDelta = new Vector2(1220f, 260f);
                header.anchoredPosition = new Vector2(0f, -34f);
                SetActiveIfExists(header, "Crown", false);
            }

            RectTransform title = transform.Find("Panel/Header/Title") as RectTransform;
            if (title != null)
            {
                title.sizeDelta = new Vector2(1120f, 62f);
                title.anchoredPosition = new Vector2(0f, -142f);
            }

            if (titleText != null)
            {
                titleText.fontSizeMax = 52f;
                titleText.fontSizeMin = 34f;
                titleText.color = new Color(1f, 0.78f, 0.2f, 1f);
            }

            RectTransform subtitle = transform.Find("Panel/Header/Subtitle") as RectTransform;
            if (subtitle != null)
            {
                subtitle.sizeDelta = new Vector2(1040f, 42f);
                subtitle.anchoredPosition = new Vector2(0f, -198f);
            }

            if (subtitleText != null)
            {
                subtitleText.fontSizeMax = 25f;
                subtitleText.fontSizeMin = 18f;
            }
        }

        private void StyleHeaderIcon()
        {
            RectTransform existingPanel = transform.Find("Panel") as RectTransform;
            if (existingPanel == null)
            {
                return;
            }

            SetActiveIfExists(existingPanel, "Crown", false);
            SetActiveIfExists(existingPanel, "HeaderCrown", false);
            Transform header = existingPanel.Find("Header");
            if (header != null)
            {
                SetActiveIfExists(header, "Crown", false);
                SetActiveIfExists(header, "HeaderCrown", false);
            }

            RectTransform iconRect = existingPanel.Find("HeaderIcon") as RectTransform;
            Image icon = iconRect != null ? iconRect.GetComponent<Image>() : null;
            if (icon == null)
            {
                icon = CreateImage(existingPanel, "HeaderIcon", LoadSprite("Assets/Art/Button/CollectionIcon.png"), Color.white, new Vector2(0f, -50f), new Vector2(190f, 190f), Image.Type.Simple);
                iconRect = icon.rectTransform;
            }

            icon.sprite = LoadSprite("Assets/Art/Button/CollectionIcon.png");
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            SetAnchored(iconRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(190f, 190f), new Vector2(0f, -50f));
            iconRect.SetAsLastSibling();
        }

        private void UpgradeCollectionHero()
        {
            RectTransform bookHero = transform.Find("Panel/BookHero") as RectTransform;
            if (bookHero == null)
            {
                return;
            }

            foreach (Image image in bookHero.GetComponentsInChildren<Image>(true))
            {
                image.enabled = false;
                image.raycastTarget = false;
            }

            foreach (TMP_Text text in bookHero.GetComponentsInChildren<TMP_Text>(true))
            {
                text.enabled = false;
                text.raycastTarget = false;
            }
        }

        private void UpgradeProgressBlock()
        {
            RectTransform progressBlock = transform.Find("Panel/ProgressBlock") as RectTransform;
            if (progressBlock != null)
            {
                SetAnchored(progressBlock, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560f, 150f), new Vector2(0f, 120f));
                Image progressImage = progressBlock.GetComponent<Image>();
                if (progressImage != null)
                {
                    progressImage.enabled = false;
                    progressImage.raycastTarget = false;
                    Outline outline = progressBlock.GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.enabled = false;
                    }
                }

                RectTransform progressFrame = progressBlock.Find("ProgressFrame") as RectTransform;
                if (progressFrame != null)
                {
                    progressFrame.gameObject.SetActive(true);
                    SetTextRect(progressFrame, Vector2.zero, new Vector2(550f, 144f));
                    Image frameImage = progressFrame.GetComponent<Image>();
                    if (frameImage != null)
                    {
                        frameImage.enabled = true;
                        frameImage.color = Color.white;
                        frameImage.type = Image.Type.Sliced;
                        frameImage.raycastTarget = false;
                    }

                    progressFrame.SetAsFirstSibling();
                }

                SetActiveIfExists(progressBlock, "InnerHighlight", false);
                SetTextRect(progressBlock.Find("ProgressTitle") as RectTransform, new Vector2(0f, 48f), new Vector2(470f, 24f));
                SetTextRect(progressBlock.Find("ProgressValue") as RectTransform, new Vector2(0f, 17f), new Vector2(470f, 48f));
                RectTransform track = progressBlock.Find("ProgressTrack") as RectTransform;
                if (track != null)
                {
                    track.gameObject.SetActive(true);
                }

                SetTextRect(track, new Vector2(0f, -17f), new Vector2(400f, 14f));
                SetTextRect(progressBlock.Find("NextReward") as RectTransform, new Vector2(0f, -51f), new Vector2(470f, 26f));
                TuneText(progressBlock.Find("ProgressTitle")?.GetComponent<TMP_Text>(), 20f, 16f, false);
                TMP_Text progressValue = progressBlock.Find("ProgressValue")?.GetComponent<TMP_Text>();
                TuneText(progressValue, 52f, 40f, false);
                if (progressValue != null)
                {
                    progressValue.enabled = true;
                    progressValue.color = new Color(1f, 0.92f, 0.68f, 1f);
                    progressValue.fontStyle = FontStyles.Bold;
                    progressValue.enableWordWrapping = false;
                    progressValue.overflowMode = TextOverflowModes.Overflow;
                    TMP_Text progressTitle = progressBlock.Find("ProgressTitle")?.GetComponent<TMP_Text>();
                    if (progressTitle != null && progressTitle.font != null)
                    {
                        progressValue.font = progressTitle.font;
                        progressValue.fontSharedMaterial = progressTitle.fontSharedMaterial;
                    }

                    progressValue.rectTransform.SetAsLastSibling();
                }

                SetActiveIfExists(progressBlock, "ProgressFillPanel", false);
                TMP_Text nextReward = progressBlock.Find("NextReward")?.GetComponent<TMP_Text>();
                TuneText(nextReward, 16f, 13f, false);
                if (nextReward != null)
                {
                    nextReward.lineSpacing = -5f;
                    nextReward.color = new Color(1f, 0.91f, 0.72f, 0.96f);
                }

                Image trackImage = track != null ? track.GetComponent<Image>() : null;
                if (trackImage != null)
                {
                    trackImage.sprite = null;
                    trackImage.type = Image.Type.Simple;
                    trackImage.color = new Color(0.018f, 0.13f, 0.075f, 1f);
                    Outline trackOutline = track.GetComponent<Outline>();
                    if (trackOutline == null)
                    {
                        trackOutline = track.gameObject.AddComponent<Outline>();
                    }

                    trackOutline.effectColor = new Color(0.9f, 0.68f, 0.17f, 0.44f);
                    trackOutline.effectDistance = new Vector2(1f, -1f);
                    trackOutline.useGraphicAlpha = true;
                }

                Image fill = track != null ? track.Find("ProgressFill")?.GetComponent<Image>() : null;
                if (fill != null)
                {
                    fill.color = new Color(0.72f, 0.9f, 0.25f, 1f);
                }
            }

            RectTransform rewardTitle = transform.Find("Panel/RewardTitle") as RectTransform;
            if (rewardTitle != null)
            {
                rewardTitle.gameObject.SetActive(false);
            }

            RectTransform rewardStages = transform.Find("Panel/RewardStages") as RectTransform;
            if (rewardStages != null)
            {
                SetAnchored(rewardStages, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1300f, 236f), new Vector2(0f, 270f));
            }
        }

        private void UpgradeRewardCards()
        {
            RectTransform root = transform.Find("Panel/RewardStages") as RectTransform;
            if (root == null)
            {
                return;
            }

            int rewardIndex = 0;
            for (int childIndex = 0; childIndex < root.childCount; childIndex++)
            {
                Transform card = root.GetChild(childIndex);
                if (!card.name.StartsWith("RewardStage_"))
                {
                    continue;
                }

                int i = rewardIndex++;
                RectTransform cardRect = card as RectTransform;
                if (cardRect != null)
                {
                    cardRect.sizeDelta = new Vector2(182f, 218f);
                    float anchorX = (i + 0.5f) / 6f;
                    cardRect.anchorMin = new Vector2(anchorX, 0.5f);
                    cardRect.anchorMax = cardRect.anchorMin;
                    cardRect.pivot = new Vector2(0.5f, 0.5f);
                    cardRect.anchoredPosition = Vector2.zero;
                }

                Image cardImage = card.GetComponent<Image>();
                if (cardImage != null)
                {
                    cardImage.sprite = PremiumUISprites.EmeraldPanel;
                    cardImage.type = Image.Type.Sliced;
                    cardImage.color = i == 0
                        ? new Color(0.075f, 0.31f, 0.18f, 0.99f)
                        : new Color(0.045f, 0.19f, 0.115f, 0.96f);
                    cardImage.raycastTarget = false;
                    Outline outline = card.GetComponent<Outline>();
                    if (outline == null)
                    {
                        outline = card.gameObject.AddComponent<Outline>();
                    }

                    outline.effectColor = i == 0
                        ? new Color(1f, 0.76f, 0.18f, 0.58f)
                        : new Color(0.88f, 0.65f, 0.16f, 0.32f);
                    outline.effectDistance = i == 0 ? new Vector2(1.6f, -1.6f) : new Vector2(1f, -1f);
                    outline.useGraphicAlpha = true;
                }

                foreach (Shadow shadow in card.GetComponents<Shadow>())
                {
                    shadow.effectColor = new Color(shadow.effectColor.r, shadow.effectColor.g, shadow.effectColor.b, 0f);
                    shadow.effectDistance = Vector2.zero;
                }

                CanvasGroup cardGroup = GetOrCreateCanvasGroup(card.gameObject);
                cardGroup.alpha = 1f;
                cardGroup.interactable = true;
                cardGroup.blocksRaycasts = true;

                TMP_Text oldIcon = card.Find("Icon")?.GetComponent<TMP_Text>();
                if (oldIcon != null)
                {
                    oldIcon.gameObject.SetActive(false);
                }

                SetTextRect(card.Find("Points") as RectTransform, new Vector2(0f, 139f), new Vector2(154f, 30f));
                TMP_Text pointsText = card.Find("Points")?.GetComponent<TMP_Text>();
                TuneText(pointsText, 22f, 17f, false);
                if (pointsText != null)
                {
                    pointsText.color = new Color(1f, 0.87f, 0.48f, 1f);
                }

                Image milestone = card.Find("MilestoneDiamond")?.GetComponent<Image>();
                if (milestone == null)
                {
                    milestone = CreateImage(
                        card,
                        "MilestoneDiamond",
                        LoadSprite("Assets/Resources/UI/DailyRewards/DailyRewards/DailyRewardsTopFrameJewel.asset"),
                        Color.white,
                        new Vector2(0f, 109f),
                        new Vector2(30f, 34f),
                        Image.Type.Simple);
                }

                milestone.color = Color.white;
                milestone.preserveAspect = true;
                milestone.raycastTarget = false;
                SetTextRect(
                    milestone.rectTransform,
                    new Vector2(0f, 109f),
                    i == 0 ? new Vector2(38f, 42f) : new Vector2(30f, 34f));
                milestone.color = i == 0
                    ? new Color(0.62f, 1f, 0.48f, 1f)
                    : new Color(1f, 0.88f, 0.46f, 0.94f);

                Image icon = card.Find("RewardIcon")?.GetComponent<Image>();
                if (icon == null)
                {
                    icon = CreateImage(card, "RewardIcon", GetCollectionRewardIcon(i), Color.white, new Vector2(0f, 29f), new Vector2(58f, 58f), Image.Type.Simple);
                }

                icon.sprite = GetCollectionRewardIcon(i);
                icon.preserveAspect = true;
                icon.color = Color.white;
                icon.rectTransform.sizeDelta = i == 4 ? new Vector2(68f, 68f) : new Vector2(64f, 64f);
                icon.rectTransform.anchoredPosition = new Vector2(0f, 27f);

                RectTransform reward = card.Find("Reward") as RectTransform;
                SetTextRect(reward, new Vector2(0f, -45f), new Vector2(148f, 62f));
                TMP_Text rewardText = reward != null ? reward.GetComponent<TMP_Text>() : null;
                TuneText(rewardText, i == 4 ? 17.5f : 19f, 13.5f, true);
                if (rewardText != null)
                {
                    rewardText.margin = new Vector4(8f, 5f, 8f, 5f);
                    rewardText.lineSpacing = -3f;
                    rewardText.color = new Color(1f, 0.93f, 0.75f, 1f);
                }

                SetTextRect(card.Find("Status") as RectTransform, new Vector2(0f, -91f), new Vector2(166f, 24f));
                TuneText(card.Find("Status")?.GetComponent<TMP_Text>(), 11.5f, 8.5f, false);
                Transform status = card.Find("Status");
                if (status != null)
                {
                    status.gameObject.SetActive(false);
                }

                Image glow = card.Find("ActiveGlow")?.GetComponent<Image>();
                if (glow == null)
                {
                    glow = CreateImage(
                        card,
                        "ActiveGlow",
                        cardImage != null ? cardImage.sprite : PremiumUISprites.EmeraldPanel,
                        new Color(1f, 0.76f, 0.18f, 0.18f),
                        Vector2.zero,
                        new Vector2(190f, 226f),
                        Image.Type.Sliced);
                }

                glow.sprite = cardImage != null ? cardImage.sprite : PremiumUISprites.EmeraldPanel;
                glow.type = Image.Type.Sliced;
                glow.color = new Color(1f, 0.76f, 0.18f, 0.18f);
                glow.raycastTarget = false;
                SetTextRect(glow.rectTransform, Vector2.zero, new Vector2(190f, 226f));
                glow.gameObject.SetActive(i == 0);
                glow.rectTransform.SetAsFirstSibling();
            }
        }

        private void UpgradeBottomBlocks()
        {
            RectTransform scoreRules = transform.Find("Panel/ScoreRules") as RectTransform;
            if (scoreRules != null)
            {
                SetAnchored(scoreRules, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(530f, 130f), new Vector2(360f, 175f));
                Image scoreImage = scoreRules.GetComponent<Image>();
                if (scoreImage != null)
                {
                    scoreImage.sprite = PremiumUISprites.EmeraldPanel;
                    scoreImage.type = Image.Type.Sliced;
                    scoreImage.color = new Color(0.045f, 0.22f, 0.13f, 0.96f);
                    scoreImage.raycastTarget = false;
                }

                SetTextRect(scoreRules.Find("Title") as RectTransform, new Vector2(0f, 43f), new Vector2(480f, 26f));
                SetTextRect(scoreRules.Find("Rows") as RectTransform, new Vector2(0f, -18f), new Vector2(490f, 82f));
                TuneText(scoreRules.Find("Title")?.GetComponent<TMP_Text>(), 23f, 18f, false);
                TMP_Text rules = scoreRules.Find("Rows")?.GetComponent<TMP_Text>();
                SetText(rules, "• Обычная категория  +1 очко\n• Категория с картинками  +2 очка\n• Без подсказок  +1 бонус\n• Без отмены  +1 бонус");
                TuneText(rules, 16f, 13f, true);
                if (rules != null)
                {
                    rules.alignment = TextAlignmentOptions.Left;
                    rules.lineSpacing = -2f;
                    rules.margin = new Vector4(28f, 5f, 22f, 5f);
                    rules.color = new Color(1f, 0.92f, 0.75f, 0.98f);
                }
            }

            RectTransform levelBlock = transform.Find("Panel/LevelBlock") as RectTransform;
            if (levelBlock != null)
            {
                SetAnchored(levelBlock, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(530f, 130f), new Vector2(-360f, 175f));
                Image levelImage = levelBlock.GetComponent<Image>();
                if (levelImage != null)
                {
                    levelImage.sprite = PremiumUISprites.EmeraldPanel;
                    levelImage.type = Image.Type.Sliced;
                    levelImage.color = new Color(0.045f, 0.22f, 0.13f, 0.96f);
                    levelImage.raycastTarget = false;
                }

                SetTextRect(levelBlock.Find("Title") as RectTransform, new Vector2(0f, 43f), new Vector2(480f, 26f));
                SetTextRect(levelBlock.Find("Medal") as RectTransform, new Vector2(-165f, -10f), new Vector2(126f, 68f));
                SetTextRect(levelBlock.Find("LevelTitle") as RectTransform, new Vector2(70f, 10f), new Vector2(330f, 32f));
                SetTextRect(levelBlock.Find("LevelProgress") as RectTransform, new Vector2(70f, -21f), new Vector2(290f, 21f));
                SetTextRect(levelBlock.Find("LevelRemaining") as RectTransform, new Vector2(70f, -45f), new Vector2(310f, 18f));
                TuneText(levelBlock.Find("Title")?.GetComponent<TMP_Text>(), 23f, 17f, false);
                TuneText(levelBlock.Find("Medal")?.GetComponent<TMP_Text>(), 62f, 48f, false);
                TuneText(levelBlock.Find("LevelTitle")?.GetComponent<TMP_Text>(), 23f, 17f, false);
                TuneText(levelBlock.Find("LevelProgress")?.GetComponent<TMP_Text>(), 17f, 14f, false);
                TuneText(levelBlock.Find("LevelRemaining")?.GetComponent<TMP_Text>(), 14.5f, 12f, false);
            }

            HideOpenBookButton();
        }

        private void HideOpenBookButton()
        {
            EnsureOpenBookButtonIsNonInteractive();
        }

        private static void TuneText(TMP_Text text, float maxSize, float minSize, bool wrap)
        {
            if (text == null)
            {
                return;
            }

            text.enableAutoSizing = true;
            text.fontSizeMax = maxSize;
            text.fontSizeMin = minSize;
            text.enableWordWrapping = wrap;
            text.lineSpacing = wrap ? 3f : 0f;
            text.overflowMode = TextOverflowModes.Ellipsis;
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
        }

        private static void SetTextRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.sizeDelta = size;
            rect.anchoredPosition = position;
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
            if (shadow != null && shadow is not Outline)
            {
                shadow.effectColor = new Color(shadow.effectColor.r, shadow.effectColor.g, shadow.effectColor.b, 0f);
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

        private static void StyleMainActionButton(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.sizeDelta = new Vector2(520f, 96f);
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = PremiumUISprites.CleanGoldButton != null ? PremiumUISprites.CleanGoldButton : PremiumUISprites.PrimaryButton;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
        }

        private static void SetActiveIfExists(Transform parent, string childName, bool active)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        private static Sprite GetCollectionRewardIcon(int index)
        {
            return index switch
            {
                0 or 3 => PremiumUISprites.UndoIcon,
                1 => PremiumUISprites.HintBulbIcon,
                2 => RoyalEventSprites.CoinIcon,
                4 => PremiumUISprites.HintBulbIcon,
                _ => PremiumUISprites.BoosterRocketIcon
            };
        }

        private static Sprite LoadSprite(string assetPath)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, Vector2 position, Vector2 size, Image.Type type)
        {
            RectTransform rect = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float size, Color color, FontStyles style, Vector2 position, Vector2 rectSize)
        {
            RectTransform rect = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), rectSize, position);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Max(12f, size * 0.52f);
            label.enableAutoSizing = true;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            RuntimeCyrillicFontProvider.Apply(label, text);
            return label;
        }

        private static void CreateSectionTitle(Transform parent, string name, string text, Vector2 position)
        {
            CreateText(parent, name, text, 31f, new Color(1f, 0.78f, 0.22f, 1f), FontStyles.Bold, position, new Vector2(620f, 52f));
        }

        private static Button CreateTextButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, pivot, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.CleanGoldButton != null ? PremiumUISprites.CleanGoldButton : PremiumUISprites.PrimaryButton;
            image.type = Image.Type.Sliced;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
            CreateText(rect, "Label", label, 35f, new Color(1f, 0.9f, 0.65f, 1f), FontStyles.Bold, Vector2.zero, size - new Vector2(72f, 28f));
            rect.gameObject.AddComponent<UIJuicyButton>();
            return button;
        }

        private static Button CreateIconButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position, string label)
        {
            RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, pivot, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.IconCircle;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(rect, "Label", label, size.y * 0.48f, new Color(1f, 0.86f, 0.28f, 1f), FontStyles.Bold, Vector2.zero, size * 0.7f);
            rect.gameObject.AddComponent<UIJuicyButton>();
            return button;
        }

        private static void CreateRewardCard(Transform parent, int index)
        {
            float x = -540f + index * 216f;
            RectTransform card = CreateRect(parent, $"RewardStage_{index + 1}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(170f, 230f), new Vector2(x, 0f));
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.sprite = RoyalEventSprites.RewardCard;
            cardImage.type = Image.Type.Sliced;
            Image glow = CreateImage(card, "ActiveGlow", RoyalEventSprites.RewardCard, new Color(1f, 0.82f, 0.16f, 0f), Vector2.zero, new Vector2(188f, 248f), Image.Type.Sliced);
            glow.gameObject.SetActive(false);
            CreateText(card, "Points", new[] { "10", "25", "50", "75", "100", "150" }[index], 28f, new Color(1f, 0.89f, 0.62f, 1f), FontStyles.Bold, new Vector2(0f, 74f), new Vector2(130f, 42f));
            TMP_Text oldIcon = CreateText(card, "Icon", string.Empty, 42f, new Color(1f, 0.78f, 0.19f, 1f), FontStyles.Bold, new Vector2(0f, 18f), new Vector2(120f, 58f));
            oldIcon.gameObject.SetActive(false);
            CreateText(card, "Reward", new[] { "+1 Отмена", "+1 Подсказка", "50 монет", "+2 Отмены", "+2 Подсказки", "+3 хода" }[index], 23f, new Color(1f, 0.9f, 0.68f, 1f), FontStyles.Bold, new Vector2(0f, -48f), new Vector2(140f, 62f));
            CreateText(card, "Status", index < 2 ? "ПОЛУЧЕНО" : index == 2 ? "СЛЕДУЮЩАЯ" : "ЗАБЛОКИРОВАНО", 15f, new Color(1f, 0.82f, 0.36f, 1f), FontStyles.Bold, new Vector2(0f, -92f), new Vector2(140f, 28f));
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
#endif
    }
}
