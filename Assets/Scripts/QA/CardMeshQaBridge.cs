#if CARDMESH_QA
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardMesh.QA
{
    /// <summary>
    /// QA-only bridge for WebGL layout checks. Compiled only with the
    /// CARDMESH_QA define, never in store builds. JavaScript drives it via
    /// unityInstance.SendMessage("CardMeshQA", method, arg) and reads the
    /// "[QA]" lines from the browser console.
    /// </summary>
    public sealed class CardMeshQaBridge : MonoBehaviour
    {
        private const string Tag = "[QA] ";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<CardMeshQaBridge>() != null)
            {
                return;
            }

            GameObject root = new("CardMeshQA");
            DontDestroyOnLoad(root);
            root.AddComponent<CardMeshQaBridge>();
        }

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
            Debug.Log(Tag + "loaded " + sceneName);
        }

        // Lists active, interactable buttons with their label and screen rect.
        public void ListButtons()
        {
            StringBuilder sb = new();
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!button.isActiveAndEnabled)
                {
                    continue;
                }

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(false);
                Rect r = ScreenRect(button.transform as RectTransform);
                sb.Append(Path(button.transform)).Append(" | '")
                    .Append(label != null ? Clean(label.text) : "")
                    .Append("' | ").Append(Fmt(r)).Append('\n');
            }

            Debug.Log(Tag + "buttons\n" + sb);
        }

        // Invokes onClick of the first active button whose path contains the fragment.
        public void Click(string pathFragment)
        {
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (button.isActiveAndEnabled && Path(button.transform).IndexOf(pathFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    button.onClick.Invoke();
                    Debug.Log(Tag + "clicked " + Path(button.transform));
                    return;
                }
            }

            Debug.Log(Tag + "click target not found: " + pathFragment);
        }

        public void ShowGameplayPopup(string id)
        {
            UIManager ui = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            switch (id)
            {
                case "win":
                    WinScreenView win = FindFirstObjectByType<WinScreenView>(FindObjectsInactive.Include);
                    if (win != null)
                    {
                        win.gameObject.SetActive(true);
                        win.Show(42);
                    }
                    break;
                case "fail":
                    ui?.ShowLevelFailedPopup(() => true);
                    break;
                case "lives":
                    ui?.ShowLivesEmptyPopup(FindFirstObjectByType<LivesManager>(FindObjectsInactive.Include));
                    break;
                case "restart":
                    ui?.ShowRestartConfirmation(() => true);
                    break;
                case "pause":
                    FindFirstObjectByType<ScenePauseMenuButton>(FindObjectsInactive.Include)?.OpenPauseMenu();
                    break;
                case "bonus":
                    ui?.ShowMovesBonusText(10);
                    break;
            }

            Debug.Log(Tag + "gameplay popup " + id);
        }

        /// <summary>
        /// Checks every visible TMP text: overflow of its own rect, overflow of the
        /// nearest visible frame (Image) it sits in, centering inside buttons and
        /// collisions with other texts. Prints one line per problem.
        /// </summary>
        public void Audit(string label)
        {
            Canvas.ForceUpdateCanvases();
            List<(TMP_Text text, Rect rect, string path)> visible = new();
            StringBuilder sb = new();
            int problems = 0;

            foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!IsVisible(text))
                {
                    continue;
                }

                text.ForceMeshUpdate();
                if (text.textInfo == null || text.textInfo.characterCount == 0)
                {
                    continue;
                }

                Rect own = ScreenRect(text.rectTransform);
                Rect ink = InkRect(text);
                if (ink.width <= 0f || ink.height <= 0f)
                {
                    continue;
                }

                string path = Path(text.transform);
                visible.Add((text, ink, path));
                float tol = Mathf.Max(2f, text.fontSize * 0.08f * Scale(text));

                if (text.isTextTruncated || text.textInfo.pageCount > 1)
                {
                    problems++;
                    sb.Append("TRUNCATED ").Append(Describe(text, path, ink, own)).Append('\n');
                }

                if (ink.xMin < own.xMin - tol || ink.xMax > own.xMax + tol || ink.yMin < own.yMin - tol || ink.yMax > own.yMax + tol)
                {
                    problems++;
                    sb.Append("OVERFLOW_RECT ").Append(Describe(text, path, ink, own)).Append('\n');
                }

                RectTransform frame = FindFrame(text.transform);
                if (frame != null)
                {
                    Rect fr = ScreenRect(frame);
                    float inset = Mathf.Min(fr.width, fr.height) * 0.04f;
                    if (ink.xMin < fr.xMin + inset || ink.xMax > fr.xMax - inset || ink.yMin < fr.yMin + inset * 0.5f || ink.yMax > fr.yMax - inset * 0.5f)
                    {
                        problems++;
                        sb.Append("OUTSIDE_FRAME ").Append(Describe(text, path, ink, own))
                            .Append(" frame=").Append(Path(frame)).Append(' ').Append(Fmt(fr)).Append('\n');
                    }

                    Button button = text.GetComponentInParent<Button>();
                    if (button != null && button.transform == frame && IsCentered(text.alignment) && fr.Contains(ink.min) && fr.Contains(ink.max))
                    {
                        float dx = ink.center.x - fr.center.x;
                        float dy = ink.center.y - fr.center.y;
                        if (Mathf.Abs(dx) > fr.width * 0.06f || Mathf.Abs(dy) > fr.height * 0.10f)
                        {
                            problems++;
                            sb.Append("OFF_CENTER ").Append(Describe(text, path, ink, own))
                                .Append($" dx={dx:0} dy={dy:0} button={Fmt(fr)}").Append('\n');
                        }
                    }
                }

                Rect screen = new(0f, 0f, Screen.width, Screen.height);
                if (ink.xMin < screen.xMin - 1f || ink.xMax > screen.xMax + 1f || ink.yMin < -1f || ink.yMax > screen.yMax + 1f)
                {
                    problems++;
                    sb.Append("OFF_SCREEN ").Append(Describe(text, path, ink, own)).Append('\n');
                }
            }

            for (int i = 0; i < visible.Count; i++)
            {
                for (int j = i + 1; j < visible.Count; j++)
                {
                    Rect a = visible[i].rect;
                    Rect b = visible[j].rect;
                    if (!a.Overlaps(b))
                    {
                        continue;
                    }

                    float ow = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
                    float oh = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
                    float minArea = Mathf.Min(a.width * a.height, b.width * b.height);
                    if (ow * oh > minArea * 0.08f && SameLayer(visible[i].text, visible[j].text))
                    {
                        problems++;
                        sb.Append("TEXT_OVERLAP '").Append(Clean(visible[i].text.text)).Append("' x '")
                            .Append(Clean(visible[j].text.text)).Append("' ")
                            .Append(visible[i].path).Append(" <> ").Append(visible[j].path).Append('\n');
                    }
                }
            }

            Debug.Log($"{Tag}audit {label} screen={Screen.width}x{Screen.height} texts={visible.Count} problems={problems}\n{sb}");
        }

        // Unlocks and starts a level (e.g. "501" for the first 5-column layout).
        public void PlayLevel(string levelText)
        {
            int level = Mathf.Max(1, int.Parse(levelText));
            PlayerPrefs.SetInt("CardMesh.Player.MaxCompletedLevel", Mathf.Max(PlayerPrefs.GetInt("CardMesh.Player.MaxCompletedLevel", 0), level - 1));
            PlayerPrefs.SetInt("CardMesh.Player.MaxUnlockedLevel", Mathf.Max(PlayerPrefs.GetInt("CardMesh.Player.MaxUnlockedLevel", 1), level));
            PlayerPrefs.SetInt("CardMesh.Player.CurrentLevelNumber", level);
            PlayerPrefs.SetInt("CardMesh.Player.LastSelectedLevel", level);
            PlayerPrefs.DeleteKey("CardMesh.Session.Board");
            PlayerPrefs.Save();
            foreach (LevelProgressionManager manager in FindObjectsByType<LevelProgressionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Destroy(manager);
            }

            SceneManager.LoadScene("Main");
            Debug.Log(Tag + "play level " + level);
        }

        /// <summary>
        /// Puts every card title from the content files on a clone of a real
        /// card title label (current layout) and reports titles that do not fit.
        /// </summary>
        public void AuditCardTitles()
        {
            TMP_Text sample = null;
            foreach (CardView card in FindObjectsByType<CardView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!card.IsFaceUp || card.Data == null || card.Data.IsGold)
                {
                    continue;
                }

                foreach (TMP_Text text in card.GetComponentsInChildren<TMP_Text>(false))
                {
                    if (text.name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0 && text.text == card.Data.Title)
                    {
                        sample = text;
                        break;
                    }
                }

                if (sample != null)
                {
                    break;
                }
            }

            if (sample == null)
            {
                Debug.Log(Tag + "card titles: no face-up text card on board");
                return;
            }

            HashSet<string> titles = new();
            foreach (string resource in new[] { "Data/cards", "ContentPacks/content_cards" })
            {
                TextAsset asset = Resources.Load<TextAsset>(resource);
                if (asset == null)
                {
                    continue;
                }

                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(asset.text, "\"title\"\\s*:\\s*\"([^\"]+)\""))
                {
                    titles.Add(System.Text.RegularExpressions.Regex.Unescape(match.Groups[1].Value));
                }
            }

            GameObject clone = Instantiate(sample.gameObject, sample.transform.parent);
            TMP_Text probe = clone.GetComponent<TMP_Text>();
            probe.color = new Color(0f, 0f, 0f, 0f);
            StringBuilder sb = new();
            int bad = 0;
            foreach (string title in titles)
            {
                string shown = RussianHyphenation.Hyphenate(title);
                probe.text = shown;
                probe.ForceMeshUpdate(true);
                bool brokenWord = false;
                for (int line = 1; line < probe.textInfo.lineCount; line++)
                {
                    int first = probe.textInfo.lineInfo[line].firstCharacterIndex;
                    int sourceIndex = first > 0 && first < probe.textInfo.characterCount ? probe.textInfo.characterInfo[first].index : -1;
                    char before = sourceIndex > 0 ? shown[sourceIndex - 1] : ' ';
                    if (before != ' ' && before != '\u00AD' && before != '-')
                    {
                        brokenWord = true;
                    }
                }

                if (probe.isTextTruncated || brokenWord || (probe.enableAutoSizing && probe.fontSize <= probe.fontSizeMin + 0.01f && probe.isTextOverflowing))
                {
                    bad++;
                    sb.Append(title).Append(" (").Append(probe.fontSize.ToString("0.#")).Append(brokenWord ? ", word split" : "").Append(")\n");
                }
            }

            Destroy(clone);
            Debug.Log($"{Tag}card titles screen={Screen.width}x{Screen.height} rect={sample.rectTransform.rect.width:0}x{sample.rectTransform.rect.height:0} checked={titles.Count} bad={bad}\n{sb}");
        }

        // Lists every UI object the EventSystem hits at a screen point "x,y" (pixels from bottom-left).
        public void HitTest(string point)
        {
            string[] parts = point.Split(',');
            Vector2 position = new(float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
            UnityEngine.EventSystems.PointerEventData data = new(UnityEngine.EventSystems.EventSystem.current) { position = position };
            List<UnityEngine.EventSystems.RaycastResult> results = new();
            UnityEngine.EventSystems.EventSystem.current?.RaycastAll(data, results);
            StringBuilder sb = new();
            foreach (UnityEngine.EventSystems.RaycastResult result in results)
            {
                sb.Append(Path(result.gameObject.transform)).Append('\n');
            }

            Debug.Log(Tag + "hits at " + point + "\n" + sb);
        }

        public void ProbeScrolls()
        {
            foreach (ScrollRect scroll in FindObjectsByType<ScrollRect>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (scroll.GetComponent<DragProbe>() == null)
                {
                    scroll.gameObject.AddComponent<DragProbe>();
                }
            }

            Debug.Log(Tag + "probes attached");
        }

        private sealed class DragProbe : MonoBehaviour,
            UnityEngine.EventSystems.IPointerDownHandler,
            UnityEngine.EventSystems.IInitializePotentialDragHandler,
            UnityEngine.EventSystems.IBeginDragHandler,
            UnityEngine.EventSystems.IDragHandler,
            UnityEngine.EventSystems.IEndDragHandler
        {
            private int drags;

            private void Log(string what, UnityEngine.EventSystems.PointerEventData data)
            {
                ScrollRect scroll = GetComponent<ScrollRect>();
                Debug.Log($"{Tag}probe {what} pos={data.position} press={data.pressPosition} button={data.button} cam={(data.pressEventCamera != null ? data.pressEventCamera.name : "null")} drag={(data.pointerDrag != null ? data.pointerDrag.name : "null")} content={(scroll != null && scroll.content != null ? scroll.content.anchoredPosition.ToString() : "-")}");
            }

            public void OnPointerDown(UnityEngine.EventSystems.PointerEventData data) => Log("down", data);
            public void OnInitializePotentialDrag(UnityEngine.EventSystems.PointerEventData data) => Log("init", data);
            public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData data) => Log("begin", data);

            public void OnDrag(UnityEngine.EventSystems.PointerEventData data)
            {
                if (drags++ % 5 == 0)
                {
                    Log("drag", data);
                }
            }

            public void OnEndDrag(UnityEngine.EventSystems.PointerEventData data) => Log("end", data);
        }

        public void DescribeScrolls()
        {
            StringBuilder sb = new();
            foreach (ScrollRect scroll in FindObjectsByType<ScrollRect>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                RectTransform viewport = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;
                sb.Append(Path(scroll.transform))
                    .Append($" enabled={scroll.enabled} active={scroll.IsActive()} vertical={scroll.vertical}")
                    .Append($" viewport={viewport.rect.size} content={(scroll.content != null ? scroll.content.rect.size.ToString() : "null")}")
                    .Append($" contentPos={(scroll.content != null ? scroll.content.anchoredPosition.ToString() : "-")} norm={scroll.verticalNormalizedPosition:0.00} velocity={scroll.velocity}")
                    .Append('\n');
            }

            UnityEngine.EventSystems.EventSystem system = UnityEngine.EventSystems.EventSystem.current;
            sb.Append($"eventSystem={(system != null ? system.name : "none")} module={(system != null && system.currentInputModule != null ? system.currentInputModule.GetType().Name : "none")} dragThreshold={(system != null ? system.pixelDragThreshold : -1)} timeScale={Time.timeScale}");
            Debug.Log(Tag + "scrolls\n" + sb);
        }

        public void DumpTexts()
        {
            StringBuilder sb = new();
            foreach (TMP_Text text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!IsVisible(text))
                {
                    continue;
                }

                text.ForceMeshUpdate();
                sb.Append(Describe(text, Path(text.transform), InkRect(text), ScreenRect(text.rectTransform))).Append('\n');
            }

            Debug.Log(Tag + "texts\n" + sb);
        }

        private static bool SameLayer(TMP_Text a, TMP_Text b)
        {
            Canvas ca = a.canvas != null ? a.canvas.rootCanvas : null;
            Canvas cb = b.canvas != null ? b.canvas.rootCanvas : null;
            if (ca != cb)
            {
                return false;
            }

            // Texts in different popups that are both visible are a real collision too,
            // but a text and its own shadow/outline copy under one parent are not.
            return a.transform.parent != b.transform.parent || a.text != b.text;
        }

        private static bool IsCentered(TextAlignmentOptions alignment)
        {
            return alignment is TextAlignmentOptions.Center or TextAlignmentOptions.Midline
                or TextAlignmentOptions.CenterGeoAligned or TextAlignmentOptions.Capline;
        }

        private static RectTransform FindFrame(Transform start)
        {
            Transform t = start.parent;
            int depth = 0;
            while (t != null && depth < 4)
            {
                if (t.TryGetComponent(out Button button) && button.targetGraphic is Image)
                {
                    return t as RectTransform;
                }

                if (t.TryGetComponent(out Image image) && image.enabled && image.sprite != null && image.color.a > 0.2f
                    && t.GetComponent<Canvas>() == null)
                {
                    Rect r = ScreenRect(t as RectTransform);
                    if (r.width < Screen.width * 0.95f || r.height < Screen.height * 0.95f)
                    {
                        return t as RectTransform;
                    }
                }

                t = t.parent;
                depth++;
            }

            return null;
        }

        private static bool IsVisible(TMP_Text text)
        {
            if (!text.isActiveAndEnabled || string.IsNullOrWhiteSpace(text.text) || text.color.a < 0.05f)
            {
                return false;
            }

            CanvasGroup[] groups = text.GetComponentsInParent<CanvasGroup>();
            foreach (CanvasGroup group in groups)
            {
                if (group.alpha < 0.05f)
                {
                    return false;
                }
            }

            return text.canvas != null;
        }

        private static Rect InkRect(TMP_Text text)
        {
            TMP_TextInfo info = text.textInfo;
            bool any = false;
            Vector3 min = new(float.MaxValue, float.MaxValue);
            Vector3 max = new(float.MinValue, float.MinValue);
            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible)
                {
                    continue;
                }

                any = true;
                min = Vector3.Min(min, new Vector3(c.bottomLeft.x, Mathf.Min(c.descender, c.bottomLeft.y)));
                max = Vector3.Max(max, new Vector3(c.topRight.x, Mathf.Max(c.ascender, c.topRight.y)));
            }

            if (!any)
            {
                return Rect.zero;
            }

            Vector3 a = text.transform.TransformPoint(min);
            Vector3 b = text.transform.TransformPoint(max);
            Camera cam = CanvasCamera(text.canvas);
            Vector2 sa = RectTransformUtility.WorldToScreenPoint(cam, a);
            Vector2 sb = RectTransformUtility.WorldToScreenPoint(cam, b);
            return Rect.MinMaxRect(Mathf.Min(sa.x, sb.x), Mathf.Min(sa.y, sb.y), Mathf.Max(sa.x, sb.x), Mathf.Max(sa.y, sb.y));
        }

        private static Rect ScreenRect(RectTransform rt)
        {
            if (rt == null)
            {
                return Rect.zero;
            }

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Canvas canvas = rt.GetComponentInParent<Canvas>();
            Camera cam = CanvasCamera(canvas);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private static Camera CanvasCamera(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            Canvas root = canvas.rootCanvas;
            return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        }

        private static float Scale(TMP_Text text)
        {
            return text.canvas != null ? text.canvas.rootCanvas.scaleFactor : 1f;
        }

        private static string Describe(TMP_Text text, string path, Rect ink, Rect own)
        {
            string auto = text.enableAutoSizing ? $" auto[{text.fontSizeMin:0}-{text.fontSizeMax:0}]" : "";
            return $"'{Clean(text.text)}' {path} size={text.fontSize:0.#}{auto} align={text.alignment} ink={Fmt(ink)} rect={Fmt(own)}";
        }

        private static string Fmt(Rect r)
        {
            return $"[{r.xMin:0},{r.yMin:0} {r.width:0}x{r.height:0}]";
        }

        private static string Clean(string s)
        {
            s = s.Replace('\n', ' ');
            return s.Length > 40 ? s.Substring(0, 40) + "…" : s;
        }

        private static string Path(Transform t)
        {
            List<string> parts = new();
            int guard = 0;
            while (t != null && guard++ < 8)
            {
                parts.Add(t.name);
                t = t.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
#endif
