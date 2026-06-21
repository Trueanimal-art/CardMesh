using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SolitaireAssociations
{
    [ExecuteAlways]
    public sealed class ShopPopup : MonoBehaviour
    {
        private const int CurrentAuthoringLayoutVersion = 3;

        private static readonly Color TextCream = new(1f, 0.92f, 0.7f, 1f);
        private static readonly Color TextGold = new(1f, 0.78f, 0.2f, 1f);
        private static readonly Color DeepPanel = new(0.015f, 0.105f, 0.055f, 0.96f);

        [Header("Runtime bindings")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button infoButton;

        [Header("Shared prefab references")]
        [SerializeField] private PremiumEventPrefabReferences sharedPrefabs = new();
#if UNITY_EDITOR
        [SerializeField, HideInInspector] private int authoringLayoutVersion;
#endif

        private Coroutine openRoutine;
        private bool hasShownRuntime;
#if UNITY_EDITOR
        private bool editorRefreshQueued;
#endif

        public static ShopPopup SceneInstance =>
            FindFirstObjectByType<ShopPopup>(FindObjectsInactive.Include);

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
                ApplyFontsToAllTexts();
                ApplyRuntimePolish();
                WireButtons();
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
        }

        public void Show()
        {
            hasShownRuntime = true;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            BindSceneReferences();
            ApplyFontsToAllTexts();
            DisableExternalGlow(transform);
            ApplyRuntimePolish();
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

        private void Buy(string productName, string price, string purchaseType)
        {
            Debug.Log($"Shop purchase clicked: {productName} | {price} | {purchaseType}");
        }

        private void ApplyRuntimePolish()
        {
            RuntimeSetAnchored(panel, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(1510f, 980f), Vector2.zero);

            RectTransform headerIcon = transform.Find("Panel/TopShopGem") as RectTransform;
            RuntimeSetImage(headerIcon, GetShopIcon(), Color.white, Image.Type.Simple);
            RuntimeSetTextRect(headerIcon, new Vector2(0f, 414f), new Vector2(178f, 178f));

            RuntimeSetActiveIfExists(transform.Find("Panel"), "Crown", false);
            RuntimeSetActiveIfExists(transform.Find("Panel"), "HeaderCrown", false);

            RuntimeSetTextRect(transform.Find("Panel/Title") as RectTransform, new Vector2(0f, 326f), new Vector2(850f, 74f));
            RuntimeTuneText(transform.Find("Panel/Title")?.GetComponent<TMP_Text>(), 60f, 40f, false);
            RuntimeSetTextRect(transform.Find("Panel/Subtitle") as RectTransform, new Vector2(0f, 269f), new Vector2(980f, 42f));
            RuntimeTuneText(transform.Find("Panel/Subtitle")?.GetComponent<TMP_Text>(), 28f, 20f, false);

            RectTransform hero = transform.Find("Panel/TreasuryHero") as RectTransform;
            RuntimeSetAnchored(hero, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(1020f, 96f), new Vector2(0f, 210f));
            RuntimeSetImage(hero, PremiumUISprites.EmeraldPanel, new Color(0.035f, 0.20f, 0.105f, 0.82f), Image.Type.Sliced);
            RuntimeSetActiveIfExists(hero, "ChestIcon", false);
            RuntimeSetTextRect(hero != null ? hero.Find("HeroTitle") as RectTransform : null, new Vector2(0f, 19f), new Vector2(820f, 36f));
            RuntimeSetTextRect(hero != null ? hero.Find("HeroCopy") as RectTransform : null, new Vector2(0f, -20f), new Vector2(820f, 32f));
            RuntimeTuneText(hero != null ? hero.Find("HeroTitle")?.GetComponent<TMP_Text>() : null, 29f, 20f, false);
            RuntimeTuneText(hero != null ? hero.Find("HeroCopy")?.GetComponent<TMP_Text>() : null, 21f, 15f, false);

            RuntimeSetTextRect(transform.Find("Panel/PremiumTitle") as RectTransform, new Vector2(0f, 128f), new Vector2(720f, 40f));
            RuntimeTuneText(transform.Find("Panel/PremiumTitle")?.GetComponent<TMP_Text>(), 29f, 20f, false);
            PolishBundleCard("StarterBundle", new Vector2(-470f, -58f));
            PolishBundleCard("RoyalBundle", new Vector2(0f, -58f));
            PolishBundleCard("EmperorBundle", new Vector2(470f, -58f));

            RuntimeSetTextRect(transform.Find("Panel/ResourcesTitle") as RectTransform, new Vector2(-284f, -166f), new Vector2(420f, 40f));
            RuntimeTuneText(transform.Find("Panel/ResourcesTitle")?.GetComponent<TMP_Text>(), 28f, 20f, false);
            RectTransform shelf = transform.Find("Panel/ResourcesShelf") as RectTransform;
            RuntimeSetAnchored(shelf, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(910f, 250f), new Vector2(-270f, -340f));
            RuntimeSetImage(shelf, PremiumUISprites.EmeraldPanel, new Color(0.02f, 0.14f, 0.075f, 0.18f), Image.Type.Sliced);
            PolishResourceShelf(shelf);

            RuntimeSetTextRect(transform.Find("Panel/NoAdsTitle") as RectTransform, new Vector2(502f, -148f), new Vector2(360f, 40f));
            RuntimeTuneText(transform.Find("Panel/NoAdsTitle")?.GetComponent<TMP_Text>(), 28f, 20f, false);
            PolishNoAdsCard();

            RuntimeSetTextRect(transform.Find("Panel/Disclaimer") as RectTransform, new Vector2(0f, -454f), new Vector2(720f, 32f));
            TMP_Text disclaimer = transform.Find("Panel/Disclaimer")?.GetComponent<TMP_Text>();
            RuntimeTuneText(disclaimer, 18f, 13f, false);
            if (disclaimer != null)
            {
                disclaimer.color = new Color(1f, 0.88f, 0.58f, 0.78f);
            }
        }

        private void BindSceneReferences()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            panel = transform.Find("Panel") as RectTransform;
            closeButton = transform.Find("Panel/CloseButton")?.GetComponent<Button>();
            infoButton = transform.Find("Panel/InfoButton")?.GetComponent<Button>();
        }

        private void PolishBundleCard(string cardName, Vector2 position)
        {
            RectTransform card = transform.Find($"Panel/{cardName}") as RectTransform;
            RuntimeSetAnchored(card, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(390f, 300f), position);
            RuntimeSetImage(card, PremiumUISprites.EmeraldPanel, new Color(0.035f, 0.18f, 0.1f, 0.9f), Image.Type.Sliced);
            RuntimeSetSubtleOutline(card, new Color(1f, 0.72f, 0.18f, 0.65f), new Vector2(1.6f, -1.6f));
            RuntimeSetTextRect(card != null ? card.Find("Title") as RectTransform : null, new Vector2(0f, 92f), new Vector2(340f, 36f));
            RuntimeSetTextRect(card != null ? card.Find("BundleIcon") as RectTransform : null, new Vector2(0f, 44f), new Vector2(82f, 68f));
            RuntimeSetTextRect(card != null ? card.Find("EmperorCrown") as RectTransform : null, new Vector2(0f, 52f), new Vector2(32f, 32f));
            RuntimeSetTextRect(card != null ? card.Find("Contents") as RectTransform : null, new Vector2(0f, -36f), new Vector2(320f, 104f));
            RuntimeSetTextRect(card != null ? card.Find("BuyButton") as RectTransform : null, new Vector2(0f, -124f), new Vector2(210f, 46f));
            RuntimeTuneText(card != null ? card.Find("Title")?.GetComponent<TMP_Text>() : null, 21f, 15f, false);
            RuntimeTuneText(card != null ? card.Find("Contents")?.GetComponent<TMP_Text>() : null, 20f, 14f, true);
            RuntimeTuneText(card != null ? card.Find("BuyButton/Label")?.GetComponent<TMP_Text>() : null, 24f, 17f, false);
        }

        private void PolishResourceShelf(RectTransform shelf)
        {
            if (shelf == null)
            {
                return;
            }

            Vector2[] positions =
            {
                new(-336f, 64f),
                new(-112f, 64f),
                new(112f, 64f),
                new(336f, 64f),
                new(-336f, -78f),
                new(-112f, -78f),
                new(112f, -78f),
                new(336f, -78f)
            };

            for (int i = 0; i < shelf.childCount && i < positions.Length; i++)
            {
                Transform child = shelf.GetChild(i);
                if (child == null || child.name.StartsWith("Header_", System.StringComparison.Ordinal))
                {
                    continue;
                }

                RectTransform card = child as RectTransform;
                RuntimeSetTextRect(card, positions[i], new Vector2(204f, 116f));
                RuntimeSetImage(card, PremiumUISprites.EmeraldPanel, new Color(0.035f, 0.18f, 0.1f, 0.92f), Image.Type.Sliced);
                RuntimeSetTextRect(child.Find("Icon") as RectTransform, new Vector2(-62f, 24f), new Vector2(56f, 56f));
                RuntimeSetTextRect(child.Find("Amount") as RectTransform, new Vector2(40f, 28f), new Vector2(126f, 34f));
                RuntimeSetTextRect(child.Find("Label") as RectTransform, new Vector2(40f, 0f), new Vector2(126f, 28f));
                RuntimeSetTextRect(child.Find("BuyButton") as RectTransform, new Vector2(0f, -44f), new Vector2(134f, 34f));
                RuntimeTuneText(child.Find("Amount")?.GetComponent<TMP_Text>(), 26f, 18f, false);
                RuntimeTuneText(child.Find("Label")?.GetComponent<TMP_Text>(), 17f, 12f, false);
                RuntimeTuneText(child.Find("BuyButton/Label")?.GetComponent<TMP_Text>(), 18f, 12f, false);
            }
        }

        private void PolishNoAdsCard()
        {
            RectTransform card = transform.Find("Panel/NoAdsCard") as RectTransform;
            RuntimeSetAnchored(card, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(410f, 250f), new Vector2(502f, -340f));
            RuntimeSetImage(card, PremiumUISprites.EmeraldPanel, new Color(0.035f, 0.18f, 0.1f, 0.92f), Image.Type.Sliced);
            RuntimeSetTextRect(card != null ? card.Find("Title") as RectTransform : null, new Vector2(0f, 78f), new Vector2(330f, 34f));
            RuntimeSetTextRect(card != null ? card.Find("IconBase") as RectTransform : null, new Vector2(-104f, 6f), new Vector2(88f, 88f));
            RuntimeSetTextRect(card != null ? card.Find("IconText") as RectTransform : null, new Vector2(-104f, 9f), new Vector2(72f, 38f));
            RuntimeSetTextRect(card != null ? card.Find("Description") as RectTransform : null, new Vector2(74f, 0f), new Vector2(210f, 104f));
            RuntimeSetTextRect(card != null ? card.Find("NoAdsBuy") as RectTransform : null, new Vector2(0f, -86f), new Vector2(210f, 44f));
            RuntimeTuneText(card != null ? card.Find("Title")?.GetComponent<TMP_Text>() : null, 24f, 16f, false);
            RuntimeTuneText(card != null ? card.Find("IconText")?.GetComponent<TMP_Text>() : null, 25f, 17f, false);
            RuntimeTuneText(card != null ? card.Find("Description")?.GetComponent<TMP_Text>() : null, 19f, 13f, true);
            RuntimeTuneText(card != null ? card.Find("NoAdsBuy/Label")?.GetComponent<TMP_Text>() : null, 22f, 16f, false);
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
                button.onClick.AddListener(() => Buy(product.ProductName, product.Price, product.PurchaseType));
            }
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
            float duration = 0.18f;
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.96f;
            Vector3 endScale = Vector3.one;

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
                    panel.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
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

#if UNITY_EDITOR
        public void MaterializeAuthoringLayout()
        {
            if (Application.isPlaying)
            {
                return;
            }

            EnsureAuthoringLayout();
            BindSceneReferences();
            ApplyFontsToAllTexts();
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

                MaterializeAuthoringLayout();
            };
        }

        private void EnsureAuthoringLayout()
        {
            sharedPrefabs.AssignDefaultsIfMissing();

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            Image overlay = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0.025f, 0.012f, 0.82f);
            overlay.raycastTarget = true;

            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            RectTransform existingPanel = transform.Find("Panel") as RectTransform;
            if (existingPanel == null || authoringLayoutVersion != CurrentAuthoringLayoutVersion)
            {
                if (existingPanel != null)
                {
                    DestroyImmediate(existingPanel.gameObject);
                }

                BuildLayout();
                authoringLayoutVersion = CurrentAuthoringLayoutVersion;
                UnityEditor.EditorUtility.SetDirty(this);
            }

            UpgradeSharedVisuals();
        }

        private void UpgradeSharedVisuals()
        {
            panel = transform.Find("Panel") as RectTransform;
            if (panel == null)
            {
                return;
            }

            panel.sizeDelta = new Vector2(1510f, 980f);
            Image panelImage = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
            panelImage.sprite = RoyalEventSprites.Panel != null ? RoyalEventSprites.Panel : PremiumUISprites.EmeraldPanel;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;

            Shadow shadow = panel.GetComponent<Shadow>() ?? panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(1f, 0.68f, 0.14f, 0f);
            shadow.effectDistance = Vector2.zero;

            StyleSharedButton(transform.Find("Panel/CloseButton") as RectTransform, "X", new Vector2(86f, 86f), new Vector2(-72f, -70f), true);
            StyleSharedButton(transform.Find("Panel/InfoButton") as RectTransform, "i", new Vector2(66f, 66f), new Vector2(76f, -78f), false);
            DisableExternalGlow(transform);
        }

        private void BuildLayout()
        {
            panel = CreateRect(transform, "Panel", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(1510f, 980f), Vector2.zero);
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = RoyalEventSprites.Panel != null ? RoyalEventSprites.Panel : PremiumUISprites.EmeraldPanel;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
            AddShadow(panel.gameObject, new Color(1f, 0.68f, 0.14f, 0f), Vector2.zero);

            CreateImage(panel, "TopShopGem", GetShopIcon(), Color.white, new Vector2(0f, 414f), new Vector2(178f, 178f), Image.Type.Simple);
            CreateText(panel, "Title", "МАГАЗИН", 60f, TextGold, FontStyles.Bold, new Vector2(0f, 326f), new Vector2(850f, 74f));
            CreateText(panel, "Subtitle", "Получай ресурсы и усиливай свой прогресс.", 28f, TextCream, FontStyles.Bold, new Vector2(0f, 269f), new Vector2(980f, 42f));

            CreateTreasuryHero(panel);
            CreateText(panel, "PremiumTitle", "ПРЕМИАЛЬНЫЕ НАБОРЫ", 29f, TextGold, FontStyles.Bold, new Vector2(0f, 128f), new Vector2(720f, 40f));
            infoButton = CreateCircleButton(panel, "InfoButton", "i", new Vector2(-642f, 344f), new Vector2(74f, 74f));
            closeButton = CreateCircleButton(panel, "CloseButton", "X", new Vector2(642f, 344f), new Vector2(92f, 92f));

            CreateBundleCard(panel, "StarterBundle", "СТАРТОВЫЙ НАБОР", "1000 монет\n5 подсказок\n5 Undo\n10 ходов", "$0.99", new Vector2(-470f, -58f), 0);
            CreateBundleCard(panel, "RoyalBundle", "КОРОЛЕВСКИЙ НАБОР", "5000 монет\n20 подсказок\n20 Undo\n25 ходов", "$4.99", new Vector2(0f, -58f), 1);
            CreateBundleCard(panel, "EmperorBundle", "ИМПЕРАТОРСКИЙ НАБОР", "15000 монет\n50 подсказок\n50 Undo\n50 ходов", "$9.99", new Vector2(470f, -58f), 2);

            CreateResourceShelf(panel);
            CreateNoAdsPremiumCard(panel);

            CreateText(panel, "Disclaimer", "Покупки носят демонстрационный характер", 18f, new Color(1f, 0.88f, 0.58f, 0.78f), FontStyles.Normal, new Vector2(0f, -454f), new Vector2(720f, 32f));
        }
#endif

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

        private static void CreateTreasuryHero(Transform parent)
        {
            RectTransform hero = CreateRect(parent, "TreasuryHero", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(1020f, 96f), new Vector2(0f, 210f));
            Image image = hero.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.EmeraldPanel;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.035f, 0.2f, 0.105f, 0.82f);
            AddShadow(hero.gameObject, new Color(1f, 0.68f, 0.1f, 0f), Vector2.zero);

            Image chestIcon = CreateImage(hero, "ChestIcon", GetShopIcon(), Color.white, new Vector2(-390f, 0f), new Vector2(106f, 106f), Image.Type.Simple);
            chestIcon.gameObject.SetActive(false);
            CreateText(hero, "HeroTitle", "КОРОЛЕВСКАЯ СОКРОВИЩНИЦА", 29f, TextGold, FontStyles.Bold, new Vector2(0f, 19f), new Vector2(820f, 36f));
            CreateText(hero, "HeroCopy", "Лучшие наборы и ресурсы для ускорения прогресса", 21f, TextCream, FontStyles.Normal, new Vector2(0f, -20f), new Vector2(820f, 32f));
        }

        private static void CreateBundleCard(Transform parent, string name, string title, string contents, string price, Vector2 position, int variant)
        {
            RectTransform card = CreateRect(parent, name, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(390f, 300f), position);
            Image image = card.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.EmeraldPanel;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.035f, 0.18f, 0.1f, 0.9f);
            AddShadow(card.gameObject, new Color(1f, 0.72f, 0.12f, 0f), Vector2.zero);
            AddSubtleOutline(card.gameObject, new Color(1f, 0.72f, 0.18f, 0.65f), new Vector2(1.6f, -1.6f));

            CreateText(card, "Title", title, 21f, TextGold, FontStyles.Bold, new Vector2(0f, 92f), new Vector2(340f, 36f));
            Sprite icon = variant == 1 && RoyalEventSprites.Hero != null ? RoyalEventSprites.Hero : GetShopIcon();
            CreateImage(card, "BundleIcon", icon, Color.white, new Vector2(0f, 44f), new Vector2(82f, 68f), Image.Type.Simple);
            if (variant == 2)
            {
                CreateImage(card, "EmperorCrown", RoyalEventSprites.ProgressMarker, Color.white, new Vector2(0f, 52f), new Vector2(32f, 32f), Image.Type.Simple);
            }

            CreateText(card, "Contents", contents, 20f, TextCream, FontStyles.Bold, new Vector2(0f, -36f), new Vector2(320f, 104f));
            CreateBuyButton(card, "BuyButton", title, price, "bundle", new Vector2(0f, -124f), new Vector2(210f, 46f), 24f);
        }

        private static void CreateResourceShelf(Transform parent)
        {
            CreateText(parent, "ResourcesTitle", "РЕСУРСЫ", 28f, TextGold, FontStyles.Bold, new Vector2(-284f, -166f), new Vector2(420f, 40f));

            RectTransform shelf = CreateRect(parent, "ResourcesShelf", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(910f, 250f), new Vector2(-270f, -340f));
            Image image = shelf.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.EmeraldPanel;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.02f, 0.14f, 0.075f, 0.18f);
            AddShadow(shelf.gameObject, new Color(1f, 0.64f, 0.08f, 0f), Vector2.zero);

            CreateShelfResourceCard(shelf, "Coins2500", "2 500", "монет", "$1.99", "coins", new Vector2(-336f, 64f));
            CreateShelfResourceCard(shelf, "Coins10000", "10 000", "монет", "$4.99", "coins", new Vector2(-112f, 64f));
            CreateShelfResourceCard(shelf, "Hints20", "20", "подсказок", "$1.99", "hints", new Vector2(112f, 64f));
            CreateShelfResourceCard(shelf, "Undo20", "20", "Undo", "$1.99", "undo", new Vector2(336f, 64f));

            CreateShelfResourceCard(shelf, "Coins25000", "25 000", "монет", "$9.99", "coins", new Vector2(-336f, -78f));
            CreateShelfResourceCard(shelf, "Hints50", "50", "подсказок", "$4.99", "hints", new Vector2(-112f, -78f));
            CreateShelfResourceCard(shelf, "Undo50", "50", "Undo", "$4.99", "undo", new Vector2(112f, -78f));
            CreateShelfResourceCard(shelf, "Moves50", "+50", "ходов", "$4.99", "moves", new Vector2(336f, -78f));
        }

        private static void CreateShelfResourceCard(Transform parent, string name, string amount, string label, string price, string type, Vector2 position)
        {
            RectTransform card = CreateRect(parent, name, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(204f, 116f), position);
            Image image = card.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.EmeraldPanel;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.035f, 0.18f, 0.1f, 0.92f);

            CreateImage(card, "Icon", GetIcon(type), Color.white, new Vector2(-62f, 24f), new Vector2(56f, 56f), Image.Type.Simple);
            CreateText(card, "Amount", amount, 26f, TextCream, FontStyles.Bold, new Vector2(40f, 28f), new Vector2(126f, 34f));
            CreateText(card, "Label", label, 17f, TextCream, FontStyles.Bold, new Vector2(40f, 0f), new Vector2(126f, 28f));
            CreateBuyButton(card, "BuyButton", $"{amount} {label}", price, type, new Vector2(0f, -44f), new Vector2(134f, 34f), 18f);
        }

        private static void CreateResourceGroupHeader(Transform parent, string title)
        {
            RectTransform rect = CreateRect(parent, $"Header_{title}", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(72f, 182f), Vector2.zero);
            LayoutElement element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 72f;
            element.preferredHeight = 182f;
            CreateText(rect, "Label", title, 20f, TextGold, FontStyles.Bold, Vector2.zero, new Vector2(70f, 160f));
        }

        private static void CreateResourceCard(Transform parent, string name, string amount, string label, string price, string type)
        {
            RectTransform card = CreateRect(parent, name, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(142f, 198f), Vector2.zero);
            LayoutElement element = card.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 142f;
            element.preferredHeight = 198f;

            Image image = card.gameObject.AddComponent<Image>();
            image.sprite = RoyalEventSprites.RewardCard != null ? RoyalEventSprites.RewardCard : PremiumUISprites.EmeraldPanel;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            AddShadow(card.gameObject, new Color(1f, 0.72f, 0.12f, 0f), Vector2.zero);

            CreateImage(card, "Icon", GetIcon(type), Color.white, new Vector2(0f, 44f), new Vector2(68f, 68f), Image.Type.Simple);
            CreateText(card, "Amount", amount, 28f, TextCream, FontStyles.Bold, new Vector2(0f, -18f), new Vector2(116f, 38f));
            CreateText(card, "Label", label, 18f, TextCream, FontStyles.Bold, new Vector2(0f, -50f), new Vector2(120f, 30f));
            CreateBuyButton(card, "BuyButton", $"{amount} {label}", price, type, new Vector2(0f, -82f), new Vector2(104f, 42f), 18f);
        }

        private static void CreateNoAdsPremiumCard(Transform parent)
        {
            CreateText(parent, "NoAdsTitle", "ПРЕМИУМ", 28f, TextGold, FontStyles.Bold, new Vector2(502f, -148f), new Vector2(360f, 40f));

            RectTransform card = CreateRect(parent, "NoAdsCard", Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, new Vector2(410f, 250f), new Vector2(502f, -340f));
            Image image = card.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.EmeraldPanel;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.035f, 0.18f, 0.1f, 0.92f);
            AddShadow(card.gameObject, new Color(1f, 0.68f, 0.08f, 0f), Vector2.zero);

            CreateText(card, "Title", "УБРАТЬ РЕКЛАМУ", 24f, TextGold, FontStyles.Bold, new Vector2(0f, 78f), new Vector2(330f, 34f));
            CreateImage(card, "IconBase", PremiumUISprites.CleanIconCircle != null ? PremiumUISprites.CleanIconCircle : PremiumUISprites.IconCircle, Color.white, new Vector2(-104f, 6f), new Vector2(88f, 88f), Image.Type.Simple);
            CreateText(card, "IconText", "ADS", 25f, TextCream, FontStyles.Bold, new Vector2(-104f, 9f), new Vector2(72f, 38f));
            CreateText(card, "Description", "Отключает межстраничную рекламу и делает игру комфортнее.", 19f, TextCream, FontStyles.Normal, new Vector2(74f, 0f), new Vector2(210f, 104f));
            CreateBuyButton(card, "NoAdsBuy", "УБРАТЬ РЕКЛАМУ", "$2.99", "no_ads", new Vector2(0f, -86f), new Vector2(210f, 44f), 22f);
        }

        private static Button CreateBuyButton(Transform parent, string name, string product, string price, string type, Vector2 position, Vector2 size, float fontSize = 28f)
        {
            RectTransform rect = CreateRect(parent, name, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.CleanGoldButton != null ? PremiumUISprites.CleanGoldButton : PremiumUISprites.PrimaryButton;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            rect.gameObject.AddComponent<UIJuicyButton>();
            ShopProductButton productButton = rect.gameObject.AddComponent<ShopProductButton>();
            productButton.Configure(product, price, type);
            CreateText(rect, "Label", price, fontSize, new Color(1f, 0.9f, 0.65f, 1f), FontStyles.Bold, Vector2.zero, size - new Vector2(26f, 10f));
            return button;
        }

        private static Sprite GetIcon(string type)
        {
            return type switch
            {
                "hints" => PremiumUISprites.HintBulbIcon,
                "undo" => PremiumUISprites.UndoIcon,
                "moves" => PremiumUISprites.BoosterRocketIcon,
                "no_ads" => PremiumUISprites.IconCircle,
                _ => RoyalEventSprites.CoinIcon
            };
        }

        private static Sprite GetShopIcon()
        {
#if UNITY_EDITOR
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Button/ShopIcon.png");
            if (sprite != null)
            {
                return sprite;
            }
#endif
            return RoyalEventSprites.ButtonIcon != null ? RoyalEventSprites.ButtonIcon : (PremiumUISprites.CleanIconCircle != null ? PremiumUISprites.CleanIconCircle : PremiumUISprites.IconCircle);
        }

        private static Button CreateCircleButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(parent, name, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = PremiumUISprites.CleanIconCircle != null ? PremiumUISprites.CleanIconCircle : PremiumUISprites.IconCircle;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(rect, "Label", label, size.y * 0.46f, new Color(1f, 0.82f, 0.24f, 1f), FontStyles.Bold, Vector2.zero, size * 0.72f);
            rect.gameObject.AddComponent<UIJuicyButton>();
            return button;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer));
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
            RectTransform rect = CreateRect(parent, name, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float size, Color color, FontStyles style, Vector2 position, Vector2 rectSize)
        {
            RectTransform rect = CreateRect(parent, name, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, rectSize, position);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Max(9f, size * 0.55f);
            label.enableAutoSizing = true;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            RuntimeCyrillicFontProvider.Apply(label, text);
            return label;
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

        private static void RuntimeSetAnchored(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
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

        private static void RuntimeSetTextRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void RuntimeSetImage(RectTransform rect, Sprite sprite, Color color, Image.Type type)
        {
            Image image = rect != null ? rect.GetComponent<Image>() : null;
            if (image == null)
            {
                return;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
            }

            image.type = type;
            image.color = color;
            image.preserveAspect = type == Image.Type.Simple;
        }

        private static void RuntimeTuneText(TMP_Text text, float maxSize, float minSize, bool wrap)
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
            RuntimeCyrillicFontProvider.Apply(text, text.text);
        }

        private static void RuntimeSetActiveIfExists(Transform parent, string childName, bool active)
        {
            Transform child = parent != null ? parent.Find(childName) : null;
            if (child != null)
            {
                child.gameObject.SetActive(active);
            }
        }

        private void ApplyFontsToAllTexts()
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                RuntimeCyrillicFontProvider.Apply(text, text.text);
            }
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            Shadow shadow = target.GetComponent<Shadow>() ?? target.AddComponent<Shadow>();
            shadow.effectColor = new Color(color.r, color.g, color.b, 0f);
            shadow.effectDistance = Vector2.zero;
            shadow.useGraphicAlpha = true;
        }

        private static void AddSubtleOutline(GameObject target, Color color, Vector2 distance)
        {
            Outline outline = target.GetComponent<Outline>() ?? target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static void RuntimeSetSubtleOutline(RectTransform rect, Color color, Vector2 distance)
        {
            if (rect == null)
            {
                return;
            }

            AddSubtleOutline(rect.gameObject, color, distance);
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

                if (outline.gameObject.name is "StarterBundle" or "RoyalBundle" or "EmperorBundle")
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
    }
}
