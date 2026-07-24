using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuAmbientController : MonoBehaviour
{
    [Header("Master")]
    [SerializeField] private bool enableAmbientAnimation = true;

    [Header("Particles")]
    [SerializeField, Range(0, 15)] private int particleCount = 12;
    [SerializeField, Range(4f, 40f)] private float particleSpeed = 16f;
    [SerializeField, Range(0f, 0.45f)] private float particleAlpha = 0.22f;

    [Header("Fog")]
    [SerializeField, Range(0f, 0.25f)] private float fogAlpha = 0.085f;
    [SerializeField, Range(0f, 20f)] private float fogSpeed = 7f;

    [Header("Light Rays")]
    [SerializeField, Range(0f, 0.25f)] private float rayIntensity = 0.09f;
    [SerializeField, Range(0f, 20f)] private float raySpeed = 5f;

    [Header("Play Button Shine")]
    [SerializeField, Min(1f)] private float shineInterval = 4.2f;
    [SerializeField, Range(0.5f, 2f)] private float shineDuration = 1.05f;

    [Header("Scene References")]
    [SerializeField] private RectTransform fog;
    [SerializeField] private Image fogImage;
    [SerializeField] private RectTransform lightRays;
    [SerializeField] private Image lightRaysImage;
    [SerializeField] private RectTransform particleRoot;
    [SerializeField] private RectTransform shineOverlay;
    [SerializeField] private Image shineImage;

    private RectTransform[] particles = Array.Empty<RectTransform>();
    private Image[] particleImages = Array.Empty<Image>();
    private float[] particlePhases = Array.Empty<float>();
    private float[] particleLifetimes = Array.Empty<float>();
    private float[] particleDrifts = Array.Empty<float>();
    private float[] particleScales = Array.Empty<float>();
    private float elapsed;
    private float shineClock;
    private Material shineMaterial;
    private bool lastEnabledState;

#if UNITY_EDITOR
    private int editorSampleFrames;
    private long editorAllocatedBytes;

    public int EditorSampleFrames => editorSampleFrames;
    public long EditorAllocatedBytes => editorAllocatedBytes;
    public bool EditorVisualLayersActive =>
        fog != null && fog.gameObject.activeSelf
        && lightRays != null && lightRays.gameObject.activeSelf
        && particleRoot != null && particleRoot.gameObject.activeSelf
        && shineOverlay != null && shineOverlay.gameObject.activeSelf;
#endif

    public bool EnableAmbientAnimation
    {
        get => enableAmbientAnimation;
        set
        {
            enableAmbientAnimation = value;
            ApplyEnabledState();
        }
    }

    private void Awake()
    {
        CacheParticles();
        if (shineImage != null && shineImage.material != null)
        {
            shineMaterial = new Material(shineImage.material);
            shineImage.material = shineMaterial;
        }

        lastEnabledState = !enableAmbientAnimation;
        ApplyEnabledState();
    }

    private void OnDestroy()
    {
        if (shineMaterial != null)
        {
            Destroy(shineMaterial);
        }
    }

    private void OnValidate()
    {
        particleCount = Mathf.Clamp(particleCount, 0, 15);
        shineInterval = Mathf.Max(shineDuration + 0.25f, shineInterval);
        if (Application.isPlaying)
        {
            ApplyEnabledState();
        }
    }

    private void Update()
    {
        if (lastEnabledState != enableAmbientAnimation)
        {
            ApplyEnabledState();
        }

        if (!enableAmbientAnimation)
        {
            return;
        }

#if UNITY_EDITOR
        long allocationBefore = GC.GetAllocatedBytesForCurrentThread();
#endif
        float dt = Time.unscaledDeltaTime;
        elapsed += dt;
        AnimateFog();
        AnimateRays();
        AnimateParticles();
        AnimateShine(dt);
#if UNITY_EDITOR
        editorAllocatedBytes += Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - allocationBefore);
        editorSampleFrames++;
#endif
    }

    private void AnimateFog()
    {
        if (fog == null || fogImage == null)
        {
            return;
        }

        float phase = elapsed * (fogSpeed / 7f) * (Mathf.PI * 2f / 16f);
        fog.anchoredPosition = new Vector2(Mathf.Sin(phase) * 54f, 72f + Mathf.Sin(phase * 0.63f) * 8f);
        float pulse = 0.94f + (Mathf.Sin(phase * 0.82f) + 1f) * 0.035f;
        fog.localScale = new Vector3(pulse, 1f + ((pulse - 0.94f) * 0.55f), 1f);
        SetAlpha(fogImage, fogAlpha * (0.88f + (Mathf.Sin(phase * 0.77f) + 1f) * 0.06f));
    }

    private void AnimateRays()
    {
        if (lightRays == null || lightRaysImage == null)
        {
            return;
        }

        float phase = elapsed * (raySpeed / 5f) * (Mathf.PI * 2f / 13f);
        lightRays.anchoredPosition = new Vector2(Mathf.Sin(phase) * 26f, 0f);
        SetAlpha(lightRaysImage, rayIntensity * (0.9f + (Mathf.Sin(phase * 0.73f) + 1f) * 0.05f));
    }

    private void AnimateParticles()
    {
        int visibleCount = Mathf.Min(particleCount, particles.Length);
        for (int i = 0; i < particles.Length; i++)
        {
            bool visible = i < visibleCount;
            if (particles[i].gameObject.activeSelf != visible)
            {
                particles[i].gameObject.SetActive(visible);
            }

            if (!visible)
            {
                continue;
            }

            float cycle = Mathf.Repeat((elapsed * particleSpeed / 16f) + particlePhases[i], particleLifetimes[i]);
            float t = cycle / particleLifetimes[i];
            float x = Mathf.Lerp(-760f, 760f, Mathf.Repeat(particlePhases[i] * 0.173f, 1f));
            x += Mathf.Sin((t * Mathf.PI * 2f) + particlePhases[i]) * particleDrifts[i];
            float y = Mathf.Lerp(-440f, 390f, t);
            particles[i].anchoredPosition = new Vector2(x, y);
            particles[i].localScale = Vector3.one * particleScales[i];

            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.18f))
                * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - t) / 0.24f));
            SetAlpha(particleImages[i], particleAlpha * fade);
        }
    }

    private void AnimateShine(float dt)
    {
        if (shineOverlay == null || shineImage == null || shineMaterial == null)
        {
            return;
        }

        shineClock += dt;
        float activeStart = Mathf.Max(0f, shineInterval - shineDuration);
        if (shineClock < activeStart)
        {
            shineImage.enabled = false;
            return;
        }

        float t = Mathf.Clamp01((shineClock - activeStart) / shineDuration);
        shineImage.enabled = true;
        shineMaterial.SetFloat("_ShineProgress", t);
        if (shineClock >= shineInterval)
        {
            shineClock = 0f;
        }
    }

    private void CacheParticles()
    {
        if (particleRoot == null)
        {
            return;
        }

        int count = particleRoot.childCount;
        particles = new RectTransform[count];
        particleImages = new Image[count];
        particlePhases = new float[count];
        particleLifetimes = new float[count];
        particleDrifts = new float[count];
        particleScales = new float[count];

        var random = new System.Random(82473);
        for (int i = 0; i < count; i++)
        {
            particles[i] = particleRoot.GetChild(i) as RectTransform;
            particleImages[i] = particles[i] != null ? particles[i].GetComponent<Image>() : null;
            particlePhases[i] = (float)random.NextDouble() * 8f;
            particleLifetimes[i] = Mathf.Lerp(4.5f, 7.8f, (float)random.NextDouble());
            particleDrifts[i] = Mathf.Lerp(14f, 52f, (float)random.NextDouble());
            particleScales[i] = Mathf.Lerp(0.55f, 1.35f, (float)random.NextDouble());
        }
    }

    private void ApplyEnabledState()
    {
        lastEnabledState = enableAmbientAnimation;
        if (fog != null)
        {
            fog.gameObject.SetActive(enableAmbientAnimation);
        }

        if (lightRays != null)
        {
            lightRays.gameObject.SetActive(enableAmbientAnimation);
        }

        if (particleRoot != null)
        {
            particleRoot.gameObject.SetActive(enableAmbientAnimation);
        }

        if (shineOverlay != null)
        {
            shineOverlay.gameObject.SetActive(enableAmbientAnimation);
        }
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        RectTransform fogRect,
        Image fogGraphic,
        RectTransform raysRect,
        Image raysGraphic,
        RectTransform particlesRect,
        RectTransform shineRect,
        Image shineGraphic)
    {
        fog = fogRect;
        fogImage = fogGraphic;
        lightRays = raysRect;
        lightRaysImage = raysGraphic;
        particleRoot = particlesRect;
        shineOverlay = shineRect;
        shineImage = shineGraphic;
    }
#endif
}
