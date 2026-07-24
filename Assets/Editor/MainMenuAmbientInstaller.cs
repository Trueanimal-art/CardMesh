using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuAmbientInstaller
{
    private const string ScenePath = "Assets/MainMenu.unity";
    private const string MaterialDirectory = "Assets/Resources/Materials/MainMenuAmbient";

    [MenuItem("Tools/CardMesh/Install Main Menu Ambient Animation")]
    public static void Install()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            throw new UnityException("MainMenuAmbientInstaller: Canvas not found.");
        }

        Transform existing = canvas.transform.Find("AmbientAnimation");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        Material raysMaterial = EnsureMaterial("AmbientRays", "CardMesh/UI/Ambient Rays");
        Material fogMaterial = EnsureMaterial("AmbientFog", "CardMesh/UI/Ambient Fog");
        Material particleMaterial = EnsureMaterial("AmbientParticle", "CardMesh/UI/Ambient Particle");
        Material shineMaterial = EnsureMaterial("PlayButtonShine", "CardMesh/UI/Button Shine");

        RectTransform root = CreateRect(canvas.transform, "AmbientAnimation");
        Stretch(root);
        root.SetSiblingIndex(Mathf.Min(1, canvas.transform.childCount - 1));

        RectTransform rays = CreateImage(root, "LightRays", raysMaterial, new Color(1f, 0.84f, 0.28f, 0.09f));
        Stretch(rays);

        RectTransform fog = CreateImage(root, "AmbientFog", fogMaterial, new Color(0.18f, 1f, 0.38f, 0.085f));
        fog.anchorMin = fog.anchorMax = new Vector2(0.5f, 0.5f);
        fog.sizeDelta = new Vector2(1260f, 680f);
        fog.anchoredPosition = new Vector2(0f, 72f);

        RectTransform particleRoot = CreateRect(root, "AmbientParticles");
        Stretch(particleRoot);
        Color[] colors =
        {
            new(1f, 0.72f, 0.16f, 0.22f),
            new(0.54f, 1f, 0.42f, 0.18f),
            new(0.84f, 0.96f, 0.24f, 0.18f)
        };
        for (int i = 0; i < 15; i++)
        {
            RectTransform particle = CreateImage(particleRoot, $"Particle_{i + 1:00}", particleMaterial, colors[i % colors.Length]);
            particle.anchorMin = particle.anchorMax = new Vector2(0.5f, 0.5f);
            float size = 8f + (i % 5) * 3f;
            particle.sizeDelta = new Vector2(size, size);
        }

        GameObject playButton = GameObject.Find("PlayButton");
        if (playButton == null)
        {
            throw new UnityException("MainMenuAmbientInstaller: PlayButton not found.");
        }

        Transform oldShine = FindChildRecursive(playButton.transform, "AmbientShine");
        if (oldShine != null)
        {
            Object.DestroyImmediate(oldShine.gameObject);
        }

        Image sourceImage = FindPrimaryButtonImage(playButton.transform);
        if (sourceImage == null)
        {
            throw new UnityException("MainMenuAmbientInstaller: PlayButton primary Image not found.");
        }

        RectTransform shine = CreateImage(sourceImage.rectTransform, "AmbientShine", shineMaterial, new Color(1f, 0.92f, 0.52f, 0.32f));
        Stretch(shine);
        Image shineImage = shine.GetComponent<Image>();
        shineImage.sprite = sourceImage.sprite;
        shineImage.type = sourceImage.type;
        shineImage.preserveAspect = sourceImage.preserveAspect;
        shineImage.enabled = false;

        MainMenuAmbientController controller = root.gameObject.AddComponent<MainMenuAmbientController>();
        controller.EditorConfigure(
            fog,
            fog.GetComponent<Image>(),
            rays,
            rays.GetComponent<Image>(),
            particleRoot,
            shine,
            shineImage);

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MainMenu ambient animation installed: rays, fog, 15 pooled particles (12 visible), and PlayButton shine.");
    }

    public static void InstallAndExit()
    {
        try
        {
            Install();
            EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static Material EnsureMaterial(string fileName, string shaderName)
    {
        Directory.CreateDirectory(MaterialDirectory);
        string path = $"{MaterialDirectory}/{fileName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            throw new UnityException($"MainMenuAmbientInstaller: shader '{shaderName}' not found.");
        }

        if (material == null)
        {
            material = new Material(shader) { name = fileName };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static RectTransform CreateImage(Transform parent, string name, Material material, Color color)
    {
        RectTransform rect = CreateRect(parent, name);
        Image image = rect.gameObject.AddComponent<Image>();
        image.material = material;
        image.color = color;
        image.raycastTarget = false;
        image.maskable = true;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static Image FindPrimaryButtonImage(Transform root)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i].sprite != null && images[i].gameObject.name != "AmbientShine")
            {
                return images[i];
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
