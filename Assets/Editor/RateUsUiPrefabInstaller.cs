using System.IO;
using CardMesh;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RateUsUiPrefabInstaller
{
    private const string ScenePath = "Assets/MainMenu.unity";
    private const string RateSpritePath = "Assets/Art/UI/MainMenuButtons/RateUsButton.png";
    private const string FilledStarSpritePath = "Assets/Art/UI/MainMenuButtons/RatingStarFilled.png";
    private const string EmptyStarSpritePath = "Assets/Art/UI/MainMenuButtons/RatingStarEmpty.png";
    private const string PrefabDirectory = "Assets/Prefabs/UI/MainMenu";
    private const string RateButtonPrefabPath = PrefabDirectory + "/RateUsButton.prefab";
    private const string PopupPrefabPath = PrefabDirectory + "/RateUsPopup.prefab";
    private const string PopupActionButtonPrefabPath = PrefabDirectory + "/PopupActionButton.prefab";
    private const string RateButtonName = "RateUsButton";
    private const string PopupName = "RateUsPopup";
    private const string PopupActionButtonName = "PopupActionButton";

    private static readonly Vector2 ButtonSize = new(340f, 170f);
    private static readonly Vector2 PopupPanelSize = new(820f, 560f);
    private static readonly Vector2 PopupActionButtonSize = new(300f, 66f);
    [MenuItem("Card Mesh/Main Menu/Rebuild Rate Us UI")]
    public static void RebuildRateUsUi()
    {
        ConfigureSpriteImport(RateSpritePath);
        WriteStarSprites();
        ConfigureSpriteImport(FilledStarSpritePath);
        ConfigureSpriteImport(EmptyStarSpritePath);
        Directory.CreateDirectory(PrefabDirectory);

        RebuildRateButtonPrefab();
        RebuildPopupActionButtonPrefab();
        GameObject popupPrefab = RebuildPopupPrefab();
        InstallSceneObjects(popupPrefab);
    }

    private static void ConfigureSpriteImport(string spritePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer == null)
        {
            throw new FileNotFoundException($"Sprite not found at {spritePath}");
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static GameObject RebuildRateButtonPrefab()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RateSpritePath);
        if (sprite == null)
        {
            throw new UnityException($"Rate button sprite failed to import: {RateSpritePath}");
        }

        GameObject root = new(RateButtonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIJuicyButton), typeof(RateUsButton));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = ButtonSize;

        Image image = root.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;

        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, RateButtonPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject RebuildPopupActionButtonPrefab()
    {
        GameObject root = CreatePopupActionButtonRoot(PopupActionButtonName, "Кнопка");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PopupActionButtonPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject RebuildPopupPrefab()
    {
        GameObject root = new(PopupName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(RateUsPopupController));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        Image overlay = root.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.42f);
        overlay.raycastTarget = true;

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        RectTransform panel = CreateImage(root.transform, "Panel", PremiumUISprites.PauseWindowCardMesh, Color.white, PopupPanelSize, Vector2.zero, Image.Type.Simple);

        TMP_Text title = CreateText(panel, "Title", "ОЦЕНИТЕ НАС", 48f, 30f, new Vector2(0f, 170f), new Vector2(580f, 72f));
        title.color = new Color(1f, 0.847f, 0.353f, 1f);

        Sprite filledStar = AssetDatabase.LoadAssetAtPath<Sprite>(FilledStarSpritePath);
        Sprite emptyStar = AssetDatabase.LoadAssetAtPath<Sprite>(EmptyStarSpritePath);
        Image[] starImages = new Image[5];
        Button[] starButtons = new Button[5];
        for (int i = 0; i < starImages.Length; i++)
        {
            Button starButton = CreateStarButton(panel, i, new Vector2(-232f + (i * 116f), 40f), filledStar);
            starButtons[i] = starButton;
            starImages[i] = starButton.transform.Find("StarImage")?.GetComponent<Image>();
        }

        Button cancelButton = CreatePopupActionButton(panel, "CancelButton", "Отмена", new Vector2(-170f, -170f));
        Button submitButton = CreatePopupActionButton(panel, "SubmitButton", "Отправить", new Vector2(170f, -170f));

        RateUsPopupController controller = root.GetComponent<RateUsPopupController>();
        SerializedObject serialized = new(controller);
        serialized.FindProperty("popupRoot").objectReferenceValue = root;
        serialized.FindProperty("submitButton").objectReferenceValue = submitButton;
        serialized.FindProperty("cancelButton").objectReferenceValue = cancelButton;
        SerializedProperty images = serialized.FindProperty("starImages");
        images.arraySize = starImages.Length;
        for (int i = 0; i < starImages.Length; i++)
        {
            images.GetArrayElementAtIndex(i).objectReferenceValue = starImages[i];
        }

        serialized.FindProperty("filledStarSprite").objectReferenceValue = filledStar;
        serialized.FindProperty("emptyStarSprite").objectReferenceValue = emptyStar;
        serialized.FindProperty("selectedRating").intValue = 5;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(submitButton.onClick, controller.Submit);
        UnityEventTools.AddPersistentListener(cancelButton.onClick, controller.Close);

        UnityEventTools.AddPersistentListener(starButtons[0].onClick, controller.SelectOne);
        UnityEventTools.AddPersistentListener(starButtons[1].onClick, controller.SelectTwo);
        UnityEventTools.AddPersistentListener(starButtons[2].onClick, controller.SelectThree);
        UnityEventTools.AddPersistentListener(starButtons[3].onClick, controller.SelectFour);
        UnityEventTools.AddPersistentListener(starButtons[4].onClick, controller.SelectFive);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PopupPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void InstallSceneObjects(GameObject popupPrefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            throw new UnityException($"Cannot install rate UI: Canvas not found in {ScenePath}.");
        }

        RemoveExisting(RateButtonName);
        RemoveExisting(PopupName);

        GameObject popup = PrefabUtility.InstantiatePrefab(popupPrefab, canvas.transform) as GameObject;
        popup.name = PopupName;
        popup.layer = canvas.gameObject.layer;
        SetLayerRecursively(popup, popup.layer);
        ConfigurePopupInstance(popup.GetComponent<RectTransform>());
        popup.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
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

    private static Button CreateStarButton(Transform parent, int index, Vector2 position, Sprite starSprite)
    {
        GameObject go = new($"StarButton{index + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIJuicyButton));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(104f, 104f);
        rect.anchoredPosition = position;

        Image image = go.GetComponent<Image>();
        image.sprite = PremiumUISprites.CleanGoldButton != null ? PremiumUISprites.CleanGoldButton : PremiumUISprites.PrimaryButton;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.04f, 0.18f, 0.07f, 0.18f);

        RectTransform star = CreateImage(go.transform, "StarImage", starSprite, Color.white, new Vector2(86f, 86f), Vector2.zero, Image.Type.Simple);
        star.GetComponent<Image>().raycastTarget = false;
        return go.GetComponent<Button>();
    }

    private static void WriteStarSprites()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilledStarSpritePath));
        WriteStarPng(FilledStarSpritePath, true);
        WriteStarPng(EmptyStarSpritePath, false);
        AssetDatabase.ImportAsset(FilledStarSpritePath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(EmptyStarSpritePath, ImportAssetOptions.ForceUpdate);
    }

    private static void WriteStarPng(string path, bool filled)
    {
        const int size = 192;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        Color32 transparent = new(0, 0, 0, 0);
        Color32[] pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = transparent;
        }

        Vector2 center = new(size * 0.5f, size * 0.54f);
        Vector2[] outer = BuildStarPoints(center, 84f, 37f);
        Vector2[] inner = BuildStarPoints(center, 60f, 27f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new(x + 0.5f, y + 0.5f);
                bool inOuter = PointInPolygon(point, outer);
                bool inInner = PointInPolygon(point, inner);
                if (!inOuter)
                {
                    continue;
                }

                bool border = !inInner || IsNearEdge(point, outer, 5.5f);
                if (!filled && !border)
                {
                    continue;
                }

                float top = Mathf.InverseLerp(size, 0f, y);
                Color color = filled
                    ? Color.Lerp(new Color(0.92f, 0.54f, 0.05f, 1f), new Color(1f, 0.93f, 0.28f, 1f), top)
                    : new Color(0.34f, 0.52f, 0.29f, 0.9f);
                if (border)
                {
                    color = Color.Lerp(new Color(0.65f, 0.31f, 0.02f, 1f), new Color(1f, 0.88f, 0.22f, 1f), top);
                }

                pixels[(y * size) + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
    }

    private static Vector2[] BuildStarPoints(Vector2 center, float outerRadius, float innerRadius)
    {
        Vector2[] points = new Vector2[10];
        for (int i = 0; i < points.Length; i++)
        {
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            float angle = Mathf.Deg2Rad * (-90f + i * 36f);
            points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        return points;
    }

    private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if (((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static bool IsNearEdge(Vector2 point, Vector2[] polygon, float distance)
    {
        float sqrDistance = distance * distance;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if (DistanceToSegmentSquared(point, polygon[j], polygon[i]) <= sqrDistance)
            {
                return true;
            }
        }

        return false;
    }

    private static float DistanceToSegmentSquared(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float length = segment.sqrMagnitude;
        if (length <= Mathf.Epsilon)
        {
            return (point - start).sqrMagnitude;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / length);
        Vector2 projection = start + segment * t;
        return (point - projection).sqrMagnitude;
    }

    private static Button CreatePopupActionButton(Transform parent, string name, string label, Vector2 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupActionButtonPrefabPath);
        GameObject go = prefab != null ? PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject : CreatePopupActionButtonRoot(name, label);
        if (go == null)
        {
            go = CreatePopupActionButtonRoot(name, label);
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

        ConfigurePopupActionButton(go, label);

        Button button = go.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        return button;
    }

    private static GameObject CreatePopupActionButtonRoot(string name, string label)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIJuicyButton));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = PopupActionButtonSize;
        ConfigurePopupActionButton(go, label);
        return go;
    }

    private static void ConfigurePopupActionButton(GameObject go, string label)
    {
        Image image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        image.sprite = PremiumUISprites.GameplayActionButton != null ? PremiumUISprites.GameplayActionButton : PremiumUISprites.PrimaryButton;
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.68f, 1f);
        colors.pressedColor = new Color(0.94f, 0.78f, 0.32f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        TMP_Text text = go.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (text == null)
        {
            text = CreateText(go.transform, "Label", label, 30f, 20f, Vector2.zero, new Vector2(250f, 48f));
        }

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(250f, 48f);

        text.text = label;
        text.color = new Color(1f, 0.847f, 0.353f, 1f);
        text.enableAutoSizing = true;
        text.fontSizeMax = 30f;
        text.fontSizeMin = 20f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        RuntimeCyrillicFontProvider.Apply(text, label);

        if (go.GetComponent<UIJuicyButton>() == null)
        {
            go.AddComponent<UIJuicyButton>();
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }
}
