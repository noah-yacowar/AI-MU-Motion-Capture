using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPage : Page
{
    public GameObject newSessionSetupPage;
    public GameObject replaySessionSetupPage;

    public Button newSessionButton;
    public Button replaySessionButton;

    public override void Start()
    {
        base.Start();

        newSessionButton.onClick.AddListener(() => goToNextPage(newSessionSetupPage));
        replaySessionButton.onClick.AddListener(LaunchApp);
    }

    public void LaunchApp()
    {
        // Path to exe
        string exePath = Path.Combine(Application.streamingAssetsPath, "PostSoftware/post_software.exe");

        if (File.Exists(exePath))
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = false,   // run without opening extra cmd shell
                CreateNoWindow = true      // don’t pop up a console window
            };

            Process.Start(startInfo);
            UnityEngine.Debug.Log("Launched Python app: " + exePath);
        }
        else
        {
            UnityEngine.Debug.LogError("EXE not found at: " + exePath);
        }
    }
}
