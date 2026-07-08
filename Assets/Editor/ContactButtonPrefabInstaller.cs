using System.IO;
using SolitaireAssociations;
using UnityEditor;
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
    private const string ButtonName = "ContactButton";

    private static readonly Vector2 ButtonSize = new(340f, 170f);
    private static readonly Vector2 ButtonPosition = new(-245f, 405f);

    [MenuItem("Solitaire/Main Menu/Rebuild Contact Button")]
    public static void RebuildContactButton()
    {
        ConfigureSpriteImport();
        GameObject prefab = RebuildPrefab();
        InstallSceneInstance(prefab);
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

    private static GameObject RebuildPrefab()
    {
        Directory.CreateDirectory(PrefabDirectory);

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
        UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, contact.OpenContactMail);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void InstallSceneInstance(GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform parent = FindButtonParent();
        if (parent == null)
        {
            throw new UnityException($"Cannot install {ButtonName}: button parent not found in {ScenePath}.");
        }

        GameObject existing = GameObject.Find(ButtonName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            throw new UnityException($"Cannot instantiate {PrefabPath}.");
        }

        instance.name = ButtonName;
        instance.layer = parent.gameObject.layer;
        ConfigureRoot(instance.GetComponent<RectTransform>());

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static Transform FindButtonParent()
    {
        GameObject playButton = GameObject.Find("PlayButton");
        if (playButton != null)
        {
            return playButton.transform.parent;
        }

        GameObject canvas = GameObject.Find("Canvas");
        return canvas != null ? canvas.transform : null;
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
}
