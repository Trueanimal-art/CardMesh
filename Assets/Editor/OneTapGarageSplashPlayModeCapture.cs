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

        GameObject runner = new("OneTapGarageSplashCaptureRunner");
        Object.DontDestroyOnLoad(runner);
        runner.AddComponent<OneTapGarageSplashCaptureRunner>();
    }

    internal static void MarkCaptureStarted()
    {
        SessionState.SetBool(CapturePendingKey, false);
    }
}

internal sealed class OneTapGarageSplashCaptureRunner : MonoBehaviour
{
    private const float CaptureTimeout = 12.0f;
    private static readonly float[] CaptureElapsedTargets =
    {
        0f,
        0.20f,
        0.48f,
        0.70f,
        0.92f,
        1.10f,
        1.40f,
        1.75f,
        2.05f,
    };

    private readonly List<Texture2D> frames = new();
    private readonly List<float> captureTimes = new();

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        OneTapGarageSplashPlayModeCapture.MarkCaptureStarted();

        float startedAt = Time.realtimeSinceStartup;
        int targetIndex = 0;
        CardMesh.OneTapGarage.OneTapGarageSplashController controller = null;

        while (targetIndex < CaptureElapsedTargets.Length
               && Time.realtimeSinceStartup - startedAt <= CaptureTimeout)
        {
            yield return new WaitForEndOfFrame();

            if (controller == null)
            {
                controller = Object.FindFirstObjectByType<
                    CardMesh.OneTapGarage.OneTapGarageSplashController>();
            }

            if (controller == null
                || controller.Elapsed + 0.0001f
                    < CaptureElapsedTargets[targetIndex])
            {
                continue;
            }

            CaptureCurrentFrame(controller.Elapsed);
            targetIndex++;
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
            manifest.WriteLine("capture_mode=animation_milestones");
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

    private void CaptureCurrentFrame(float animationElapsed)
    {
        Texture2D frame =
            new(Screen.width, Screen.height, TextureFormat.RGB24, false);
        frame.ReadPixels(
            new Rect(0f, 0f, Screen.width, Screen.height),
            0,
            0,
            false);
        frame.Apply(false, false);
        frames.Add(frame);
        captureTimes.Add(animationElapsed);
    }
}
