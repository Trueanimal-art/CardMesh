using System.IO;
using CardMesh;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ContactButtonPrefabInstaller
{
    private const string ScenePath = "Assets/MainMenu.unity";
    private const string SpritePath = "Assets/Art/UI/MainMenuButtons/ContactButton.png";
    private const string PrefabDirectory = "Assets/Prefabs/UI/MainMenu";
    private const string PrefabPath = PrefabDirectory + "/ContactButton.prefab";
    private const string ContactPopupPrefabPath = PrefabDirectory + "/ContactPopup.prefab";
    private const string PopupActionButtonPrefabPath = PrefabDirectory + "/PopupActionButton.prefab";
    private const string ButtonName = "ContactButton";
    private const string ContactPopupName = "ContactPopup";

    private static readonly Vector2 ButtonSize = new(340f, 170f);
    private static readonly Vector2 ButtonPosition = new(-245f, 405f);
    private static readonly Vector2 PopupPanelSize = new(820f, 560f);
    private static readonly Vector2 PopupActionButtonSize = new(300f, 66f);

    [MenuItem("Card Mesh/Main Menu/Rebuild Contact Button")]
    public static void RebuildContactButton()
    {
        ConfigureSpriteImport();
        Directory.CreateDirectory(PrefabDirectory);

        RebuildButtonPrefab();
        GameObject popupPrefab = RebuildContactPopupPrefab();
        InstallSceneInstance(popupPrefab);
    }

    private static void ConfigureSpriteImport()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null)
        {
            throw new FileNotFoundException($"Contact button sprite not found at {SpritePath}");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static GameObject RebuildButtonPrefab()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite == null)
        {
            throw new UnityException($"Contact button sprite failed to import: {SpritePath}");
        }

        GameObject root = new(ButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIJuicyButton), typeof(ContactSupportButton));
        ConfigureRoot(root.GetComponent<RectTransform>());

        Image image = root.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = true;

        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;
        ContactSupportButton contact = root.GetComponent<ContactSupportButton>();
        UnityEventTools.AddPersistentListener(button.onClick, contact.OpenContactMail);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject RebuildContactPopupPrefab()
    {
        GameObject root = new(ContactPopupName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(ContactPopupController));
        ConfigurePopupInstance(root.GetComponent<RectTransform>());

        Image overlay = root.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.42f);
        overlay.raycastTarget = true;

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        RectTransform panel = CreateImage(root.transform, "Panel", PremiumUISprites.SettingsWindowCardMesh, Color.white, PopupPanelSize, Vector2.zero, Image.Type.Simple);

        TMP_Text title = CreateText(panel, "Title", "СВЯЗАТЬСЯ С НАМИ", 42f, 28f, new Vector2(0f, 170f), new Vector2(680f, 72f));
        title.color = new Color(1f, 0.847f, 0.353f, 1f);

        TMP_InputField messageInput = CreateMessageInput(panel, new Vector2(0f, 2f), new Vector2(620f, 220f));
        TMP_Text statusLabel = CreateText(panel, "StatusLabel", "До 1000 символов", 22f, 16f, new Vector2(0f, -121f), new Vector2(560f, 32f));
        statusLabel.color = new Color(1f, 0.9f, 0.55f, 0.82f);

        Button cancelButton = CreatePopupActionButton(panel, "CancelButton", "Отмена", new Vector2(-170f, -170f));
        Button submitButton = CreatePopupActionButton(panel, "SubmitButton", "Отправить", new Vector2(170f, -170f));

        ContactPopupController controller = root.GetComponent<ContactPopupController>();
        SerializedObject serialized = new(controller);
        serialized.FindProperty("popupRoot").objectReferenceValue = root;
        serialized.FindProperty("messageInput").objectReferenceValue = messageInput;
        serialized.FindProperty("statusLabel").objectReferenceValue = statusLabel;
        serialized.FindProperty("submitButton").objectReferenceValue = submitButton;
        serialized.FindProperty("cancelButton").objectReferenceValue = cancelButton;
        serialized.FindProperty("maxMessageLength").intValue = 1000;
        serialized.FindProperty("fallbackToMailClient").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(submitButton.onClick, controller.Submit);
        UnityEventTools.AddPersistentListener(cancelButton.onClick, controller.Close);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ContactPopupPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void InstallSceneInstance(GameObject popupPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            throw new UnityException($"Cannot install contact UI: Canvas not found in {ScenePath}.");
        }

        RemoveExisting(ButtonName);
        RemoveExisting(ContactPopupName);

        GameObject popup = PrefabUtility.InstantiatePrefab(popupPrefab, canvas.transform) as GameObject;
        if (popup == null)
        {
            throw new UnityException($"Cannot instantiate {ContactPopupPrefabPath}.");
        }

        popup.name = ContactPopupName;
        popup.layer = canvas.gameObject.layer;
        SetLayerRecursively(popup, popup.layer);
        ConfigurePopupInstance(popup.GetComponent<RectTransform>());
        popup.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureRoot(RectTransform rect)
    {
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = ButtonPosition;
        rect.sizeDelta = ButtonSize;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static void RemoveExisting(string objectName)
    {
        foreach (Transform existing in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (existing != null && existing.name == objectName)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }
    }

    private static void ConfigurePopupInstance(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private static RectTransform CreateImage(Transform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 position, Image.Type type)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = type;
        image.color = color;
        image.raycastTarget = true;
        return rect;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float maxSize, float minSize, Vector2 position, Vector2 size)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow), typeof(Outline));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMax = maxSize;
        text.fontSizeMin = minSize;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        RuntimeCyrillicFontProvider.Apply(text, value);

        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0.03f, 0.012f, 0f, 0.32f);
        shadow.effectDistance = new Vector2(1f, -2f);

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0.17f, 0.078f, 0.016f, 0.7f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);
        return text;
    }

    private static TMP_InputField CreateMessageInput(Transform parent, Vector2 position, Vector2 size)
    {
        GameObject root = new("MessageInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image background = root.GetComponent<Image>();
        background.sprite = PremiumUISprites.EmeraldPanel;
        background.type = background.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        background.color = new Color(0.02f, 0.15f, 0.08f, 0.62f);
        background.raycastTarget = true;

        RectTransform viewport = CreateInputViewport(root.transform);
        TMP_Text placeholder = CreateInputLabel(viewport, "Placeholder", "Введите сообщение...", new Color(1f, 0.9f, 0.55f, 0.48f));
        TMP_Text text = CreateInputLabel(viewport, "Text", string.Empty, new Color(1f, 0.92f, 0.62f, 1f));

        TMP_InputField input = root.GetComponent<TMP_InputField>();
        input.textViewport = viewport;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.MultiLineNewline;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.characterLimit = 1000;
        input.richText = false;
        input.targetGraphic = background;
        input.selectionColor = new Color(1f, 0.82f, 0.24f, 0.38f);
        input.caretColor = new Color(1f, 0.86f, 0.3f, 1f);
        input.customCaretColor = true;
        return input;
    }

    private static RectTransform CreateInputViewport(Transform parent)
    {
        GameObject go = new("TextViewport", typeof(RectTransform), typeof(RectMask2D));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(24f, 18f);
        rect.offsetMax = new Vector2(-24f, -18f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static TMP_Text CreateInputLabel(Transform parent, string name, string value, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.color = color;
        text.fontSize = 25f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        RuntimeCyrillicFontProvider.Apply(text, value);
        return text;
    }

    private static Button CreatePopupActionButton(Transform parent, string name, string label, Vector2 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupActionButtonPrefabPath);
        GameObject go = prefab != null ? PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject : CreatePopupActionButtonFallback(name, label);
        if (go == null)
        {
            go = CreatePopupActionButtonFallback(name, label);
        }

        go.transform.SetParent(parent, false);
        go.name = name;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = PopupActionButtonSize;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        ConfigurePopupActionLabel(go, label);

        Button button = go.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        return button;
    }

    private static GameObject CreatePopupActionButtonFallback(string name, string label)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIJuicyButton));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = PopupActionButtonSize;

        Image image = go.GetComponent<Image>();
        image.sprite = PremiumUISprites.GameplayActionButton != null ? PremiumUISprites.GameplayActionButton : PremiumUISprites.PrimaryButton;
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        CreateText(go.transform, "Label", label, 30f, 20f, Vector2.zero, new Vector2(250f, 48f));
        ConfigurePopupActionLabel(go, label);
        return go;
    }

    private static void ConfigurePopupActionLabel(GameObject buttonObject, string label)
    {
        TMP_Text text = buttonObject.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(250f, 48f);

        text.text = label;
        text.color = new Color(1f, 0.847f, 0.353f, 1f);
        text.enableAutoSizing = true;
        text.fontSizeMax = 30f;
        text.fontSizeMin = 20f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        RuntimeCyrillicFontProvider.Apply(text, label);
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }
}
