using CardMesh;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RateUsUiPolishInstaller
{
    private const string PrefabPath =
        "Assets/Prefabs/UI/MainMenu/RateUsPopup.prefab";
    private const string FilledStarPath =
        "Assets/Art/UI/MainMenuButtons/RatingStarFilled.png";
    private static readonly Vector2 Center = new(0.5f, 0.5f);

    [MenuItem("Card Mesh/Main Menu/Polish Rate Us Popup")]
    public static void Apply()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            RectTransform panel = root.transform.Find("Panel") as RectTransform;
            RateUsPopupController controller =
                root.GetComponent<RateUsPopupController>();
            if (panel == null || controller == null)
            {
                throw new UnityException("RateUsPopup structure is incomplete.");
            }

            Sprite filledStar =
                AssetDatabase.LoadAssetAtPath<Sprite>(FilledStarPath);
            AddSubtitle(panel);

            RateUsStarInteraction[] interactions =
                ConfigureStars(panel, controller, filledStar);
            ConfigureSubmitEffects(
                panel,
                out CanvasGroup submitGlow,
                out CanvasGroup submitShine);
            ConfigureSparkles(
                panel,
                filledStar,
                out RectTransform[] sparkleRects,
                out CanvasGroup[] sparkleGroups);

            SerializedObject serializedController = new(controller);
            SetObjectArray(
                serializedController.FindProperty("starInteractions"),
                interactions);
            serializedController.FindProperty("submitGlow").objectReferenceValue =
                submitGlow;
            serializedController.FindProperty("submitShine").objectReferenceValue =
                submitShine;
            SetObjectArray(
                serializedController.FindProperty("sparkleRects"),
                sparkleRects);
            SetObjectArray(
                serializedController.FindProperty("sparkleGroups"),
                sparkleGroups);
            serializedController.FindProperty("selectedRating").intValue = 0;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("RateUsUiPolishInstaller: RateUsPopup polished.");
    }

    private static void AddSubtitle(RectTransform panel)
    {
        Transform existing = panel.Find("Subtitle");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        TMP_Text title = panel.Find("Title")?.GetComponent<TMP_Text>();
        if (title == null)
        {
            throw new UnityException("RateUsPopup/Panel/Title was not found.");
        }

        GameObject subtitleObject =
            Object.Instantiate(title.gameObject, panel, false);
        subtitleObject.name = "Subtitle";
        TMP_Text subtitle = subtitleObject.GetComponent<TMP_Text>();
        SetCenteredRect(
            subtitle.rectTransform,
            new Vector2(620f, 30f),
            new Vector2(0f, 45f));
        subtitle.text = "Понравилась игра? Поддержите нас оценкой!";
        subtitle.fontStyle = FontStyles.Normal;
        subtitle.enableAutoSizing = true;
        subtitle.fontSizeMin = 15f;
        subtitle.fontSizeMax = 21f;
        subtitle.textWrappingMode = TextWrappingModes.NoWrap;
        subtitle.color = new Color(0.93f, 0.88f, 0.72f, 0.9f);
        subtitle.raycastTarget = false;

        Outline outline = subtitle.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = new Color(0.12f, 0.055f, 0.012f, 0.36f);
            outline.effectDistance = new Vector2(0.7f, -0.7f);
        }
    }

    private static RateUsStarInteraction[] ConfigureStars(
        RectTransform panel,
        RateUsPopupController controller,
        Sprite filledStar)
    {
        RateUsStarInteraction[] interactions =
            new RateUsStarInteraction[5];

        for (int index = 0; index < interactions.Length; index++)
        {
            Transform starButton = panel.Find($"StarButton{index + 1}");
            Image starImage =
                starButton?.Find("StarImage")?.GetComponent<Image>();
            if (starButton == null || starImage == null)
            {
                throw new UnityException(
                    $"RateUsPopup StarButton{index + 1} is incomplete.");
            }

            UIJuicyButton juicy = starButton.GetComponent<UIJuicyButton>();
            if (juicy != null)
            {
                Object.DestroyImmediate(juicy);
            }

            Transform oldGlow = starButton.Find("StarGlow");
            if (oldGlow != null)
            {
                Object.DestroyImmediate(oldGlow.gameObject);
            }

            GameObject glowObject = CreateImage(
                starButton,
                "StarGlow",
                filledStar,
                new Color(1f, 0.72f, 0.12f, 0.72f),
                new Vector2(98f, 98f),
                Vector2.zero);
            glowObject.transform.SetSiblingIndex(0);
            CanvasGroup glow = glowObject.AddComponent<CanvasGroup>();
            glow.alpha = 0f;
            glow.interactable = false;
            glow.blocksRaycasts = false;

            RateUsStarInteraction interaction =
                starButton.GetComponent<RateUsStarInteraction>()
                ?? starButton.gameObject.AddComponent<RateUsStarInteraction>();
            SerializedObject serialized = new(interaction);
            serialized.FindProperty("controller").objectReferenceValue =
                controller;
            serialized.FindProperty("ratingValue").intValue = index + 1;
            serialized.FindProperty("starVisual").objectReferenceValue =
                starImage.rectTransform;
            serialized.FindProperty("glow").objectReferenceValue = glow;
            serialized.FindProperty("hoverScale").floatValue = 1.13f;
            serialized.FindProperty("selectedScale").floatValue = 1.04f;
            serialized.FindProperty("animationDuration").floatValue = 0.09f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            interactions[index] = interaction;
        }

        return interactions;
    }

    private static void ConfigureSubmitEffects(
        RectTransform panel,
        out CanvasGroup submitGlow,
        out CanvasGroup submitShine)
    {
        Transform submit = panel.Find("SubmitButton");
        Image sourceImage = submit?.GetComponent<Image>();
        if (submit == null || sourceImage == null)
        {
            throw new UnityException("RateUsPopup SubmitButton was not found.");
        }

        RemoveChild(submit, "SelectionGlow");
        RemoveChild(submit, "SelectionShine");

        GameObject glowObject = CreateImage(
            submit,
            "SelectionGlow",
            sourceImage.sprite,
            new Color(1f, 0.76f, 0.18f, 0.58f),
            new Vector2(312f, 74f),
            Vector2.zero,
            sourceImage.type);
        glowObject.transform.SetSiblingIndex(0);
        submitGlow = glowObject.AddComponent<CanvasGroup>();
        submitGlow.alpha = 0f;
        submitGlow.interactable = false;
        submitGlow.blocksRaycasts = false;

        // The project shine material does not respect the parent CanvasGroup
        // alpha consistently and left visible horizontal bands on the button.
        // Use the soft gold glow alone so the approved button stays intact.
        submitShine = null;
    }

    private static void ConfigureSparkles(
        RectTransform panel,
        Sprite filledStar,
        out RectTransform[] sparkleRects,
        out CanvasGroup[] sparkleGroups)
    {
        RemoveChild(panel, "RatingSparkles");

        GameObject root = new("RatingSparkles", typeof(RectTransform));
        root.transform.SetParent(panel, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        SetCenteredRect(
            rootRect,
            new Vector2(620f, 170f),
            new Vector2(0f, 38f));
        root.transform.SetSiblingIndex(
            Mathf.Max(0, panel.Find("CancelButton").GetSiblingIndex() - 1));

        Vector2[] positions =
        {
            new(-245f, -12f),
            new(-170f, 24f),
            new(-86f, -2f),
            new(0f, 30f),
            new(88f, -8f),
            new(174f, 22f),
            new(246f, -14f)
        };

        sparkleRects = new RectTransform[positions.Length];
        sparkleGroups = new CanvasGroup[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            float size = i % 2 == 0 ? 16f : 12f;
            GameObject sparkle = CreateImage(
                root.transform,
                $"Sparkle{i + 1}",
                filledStar,
                new Color(1f, 0.78f, 0.2f, 0.9f),
                new Vector2(size, size),
                positions[i]);
            CanvasGroup group = sparkle.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            sparkleRects[i] = sparkle.GetComponent<RectTransform>();
            sparkleGroups[i] = group;
        }
    }

    private static GameObject CreateImage(
        Transform parent,
        string name,
        Sprite sprite,
        Color color,
        Vector2 size,
        Vector2 position,
        Image.Type type = Image.Type.Simple)
    {
        GameObject gameObject =
            new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, size, position);

        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = type;
        image.preserveAspect = type == Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return gameObject;
    }

    private static void SetObjectArray<T>(
        SerializedProperty property,
        T[] values)
        where T : Object
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }

    private static void RemoveChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void SetCenteredRect(
        RectTransform rect,
        Vector2 size,
        Vector2 position)
    {
        rect.anchorMin = Center;
        rect.anchorMax = Center;
        rect.pivot = Center;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
