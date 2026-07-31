using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class OneTapGarageSplashPlayModeCapture
{
    internal const string OutputDirectory =
        "Artifacts/OneTapGarageSplash/UnityIntegration/EditorPlayMode/LaunchFrames";
    private const string EditorSkipKey = "CardMesh.OneTapGarage.SkipInEditor";
    private const string CapturePendingKey =
        "CardMesh.OneTapGarage.CapturePending";

    static OneTapGarageSplashPlayModeCapture()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/ONE TAP GARAGE/Capture Real Play Mode Launch")]
    public static void Capture()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new System.InvalidOperationException(
                "Stop Play Mode before starting the ONE TAP GARAGE capture.");
        }

        EditorPrefs.SetBool(EditorSkipKey, false);
        Directory.CreateDirectory(OutputDirectory);
        string[] oldFrames = Directory.GetFiles(OutputDirectory, "frame_*.png");
        for (int index = 0; index < oldFrames.Length; index++)
        {
            File.Delete(oldFrames[index]);
        }

        EditorSceneManager.OpenScene(OneTapGarageSplashInstaller.ScenePath);
        SessionState.SetBool(CapturePendingKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode
            || !SessionState.GetBool(CapturePendingKey, false))
        {
            return;
        }

        SessionState.SetBool(CapturePendingKey, false);
        GameObject runner = new("OneTapGarageSplashCaptureRunner");
        Object.DontDestroyOnLoad(runner);
        runner.AddComponent<OneTapGarageSplashCaptureRunner>();
    }

}

internal sealed class OneTapGarageSplashCaptureRunner : MonoBehaviour
{
    private const int CaptureFps = 15;
    private const float CaptureDuration = 4.0f;

    private readonly List<Texture2D> frames = new();
    private readonly List<float> captureTimes = new();

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        float startedAt = Time.realtimeSinceStartup;
        float nextCapture = 0f;

        while (Time.realtimeSinceStartup - startedAt <= CaptureDuration)
        {
            yield return new WaitForEndOfFrame();

            float elapsed = Time.realtimeSinceStartup - startedAt;
            if (elapsed + 0.0001f < nextCapture)
            {
                continue;
            }

            Texture2D frame =
                new(Screen.width, Screen.height, TextureFormat.RGB24, false);
            frame.ReadPixels(
                new Rect(0f, 0f, Screen.width, Screen.height),
                0,
                0,
                false);
            frame.Apply(false, false);
            frames.Add(frame);
            captureTimes.Add(elapsed);
            nextCapture = elapsed + 1f / CaptureFps;
        }

        for (int index = 0; index < frames.Count; index++)
        {
            string path = Path.Combine(
                OneTapGarageSplashPlayModeCapture.OutputDirectory,
                $"frame_{index:000}.png");
            File.WriteAllBytes(path, frames[index].EncodeToPNG());
            Destroy(frames[index]);
        }

        using (StreamWriter manifest = new(
                   Path.Combine(
                       OneTapGarageSplashPlayModeCapture.OutputDirectory,
                       "capture_manifest.txt"),
                   false))
        {
            manifest.WriteLine($"capture_fps={CaptureFps}");
            manifest.WriteLine($"screen={Screen.width}x{Screen.height}");
            manifest.WriteLine($"frames={frames.Count}");
            for (int index = 0; index < captureTimes.Count; index++)
            {
                manifest.WriteLine(
                    $"frame_{index:000}="
                    + captureTimes[index].ToString(
                        "0.0000",
                        CultureInfo.InvariantCulture));
            }
        }

        Debug.Log(
            $"ONE TAP GARAGE real Play Mode capture complete: "
            + $"{frames.Count} frames at {Screen.width}x{Screen.height}.");
        EditorApplication.ExitPlaymode();
    }
}
