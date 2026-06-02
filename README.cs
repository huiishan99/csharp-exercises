using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

using Debug = UnityEngine.Debug;

public class DemoWindowsAudioOutputSwitcher : MonoBehaviour
{
    [SerializeField] private string relativeScriptPath = "AudioTools/SwitchDefaultAudioOutput.ps1";
    [SerializeField] private bool resetUnityAudioAfterSwitch = true;
    [SerializeField] private float unityAudioResetDelay = 0.3f;

    public void SwitchToNextDevice()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        string scriptPath = Path.Combine(Application.streamingAssetsPath, relativeScriptPath);

        if (!File.Exists(scriptPath))
        {
            Debug.LogError("[AudioOutput] PowerShell script not found: " + scriptPath);
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                LogProcessOutput(output, false);
                LogProcessOutput(error, true);

                if (process.ExitCode != 0)
                {
                    Debug.LogWarning("[AudioOutput] Switch process failed. ExitCode: " + process.ExitCode);
                    return;
                }

                if (resetUnityAudioAfterSwitch)
                {
                    StartCoroutine(ResetUnityAudioAfterDelay());
                }
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[AudioOutput] Failed to run PowerShell: " + exception.Message);
        }
#else
        Debug.LogWarning("[AudioOutput] Windows audio output switching is only supported on Windows.");
#endif
    }

    private IEnumerator ResetUnityAudioAfterDelay()
    {
        yield return new WaitForSeconds(unityAudioResetDelay);

        AudioConfiguration configuration = AudioSettings.GetConfiguration();
        bool result = AudioSettings.Reset(configuration);

        Debug.Log("[AudioOutput] Unity audio reset: " + result);
    }

    private void LogProcessOutput(string text, bool isError)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string[] lines = text.Split(
            new[] { "\r\n", "\n" },
            System.StringSplitOptions.RemoveEmptyEntries
        );

        for (int i = 0; i < lines.Length; i++)
        {
            if (isError)
            {
                Debug.LogWarning(lines[i]);
            }
            else
            {
                Debug.Log(lines[i]);
            }
        }
    }
}
