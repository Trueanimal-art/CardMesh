using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardMesh
{
    public sealed class ShopPremiumMotion : MonoBehaviour
    {
        private const float RecommendedGlowPeriod = 4.8f;

        private Image[] chestIcons;
        private Color[] chestBaseColors;
        private Vector3[] chestBaseScales;
        private Outline recommendedOutline;
        private Coroutine glintRoutine;
        private float glowTime;

        private void OnEnable()
        {
            ResolveReferences();
            if (Application.isPlaying)
            {
                glintRoutine = StartCoroutine(PlayRareChestGlints());
            }
        }

        private void OnDisable()
        {
            if (glintRoutine != null)
            {
                StopCoroutine(glintRoutine);
                glintRoutine = null;
            }

            ResetChestVisuals();
        }

        private void Update()
        {
            if (!Application.isPlaying || recommendedOutline == null)
            {
                return;
            }

            glowTime += Time.unscaledDeltaTime;
            float wave = 0.5f + 0.5f * Mathf.Sin(glowTime * Mathf.PI * 2f / RecommendedGlowPeriod);
            Color color = recommendedOutline.effectColor;
            color.a = Mathf.Lerp(0.38f, 0.58f, wave);
            recommendedOutline.effectColor = color;
        }

        private void ResolveReferences()
        {
            Transform bundles = transform.Find("Panel/ShopContent/PremiumBundles");
            if (bundles == null)
            {
                chestIcons = System.Array.Empty<Image>();
                chestBaseColors = System.Array.Empty<Color>();
                chestBaseScales = System.Array.Empty<Vector3>();
                recommendedOutline = null;
                return;
            }

            string[] names = { "StarterBundle", "RoyalBundle", "EmperorBundle" };
            chestIcons = new Image[names.Length];
            chestBaseColors = new Color[names.Length];
            chestBaseScales = new Vector3[names.Length];
            for (int index = 0; index < names.Length; index++)
            {
                chestIcons[index] = bundles.Find($"{names[index]}/Icon")?.GetComponent<Image>();
                if (chestIcons[index] != null)
                {
                    chestBaseColors[index] = chestIcons[index].color;
                    chestBaseScales[index] = chestIcons[index].rectTransform.localScale;
                }
            }

            recommendedOutline = bundles.Find("RoyalBundle/CardFill")?.GetComponent<Outline>();
        }

        private IEnumerator PlayRareChestGlints()
        {
            yield return new WaitForSecondsRealtime(2.8f);
            while (isActiveAndEnabled)
            {
                yield return new WaitForSecondsRealtime(Random.Range(6.5f, 9.5f));
                for (int index = 0; index < chestIcons.Length; index++)
                {
                    Image icon = chestIcons[index];
                    if (icon != null)
                    {
                        yield return AnimateSoftGlint(icon);
                    }

                    yield return new WaitForSecondsRealtime(0.12f);
                }
            }
        }

        private static IEnumerator AnimateSoftGlint(Image icon)
        {
            RectTransform rect = icon.rectTransform;
            Color baseColor = icon.color;
            Vector3 baseScale = rect.localScale;
            Color highlight = new(
                Mathf.Min(1f, baseColor.r * 1.08f + 0.06f),
                Mathf.Min(1f, baseColor.g * 1.07f + 0.04f),
                Mathf.Min(1f, baseColor.b * 1.02f + 0.01f),
                baseColor.a);

            const float duration = 0.72f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float wave = Mathf.Sin(progress * Mathf.PI);
                icon.color = Color.Lerp(baseColor, highlight, wave * 0.72f);
                rect.localScale = Vector3.LerpUnclamped(baseScale, baseScale * 1.018f, wave);
                yield return null;
            }

            icon.color = baseColor;
            rect.localScale = baseScale;
        }

        private void ResetChestVisuals()
        {
            if (chestIcons == null)
            {
                return;
            }

            for (int index = 0; index < chestIcons.Length; index++)
            {
                Image icon = chestIcons[index];
                if (icon != null)
                {
                    icon.color = chestBaseColors[index];
                    icon.rectTransform.localScale = chestBaseScales[index];
                }
            }
        }
    }
}
