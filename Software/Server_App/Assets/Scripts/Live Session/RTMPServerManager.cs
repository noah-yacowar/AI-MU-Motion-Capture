using System.Diagnostics;
using System.IO;
using UnityEngine;

public class RTMPServerManager : MonoBehaviour
{
    public static RTMPServerManager Instance { get; private set; }

    private Process serverProcess;

    void Start()
    { 
        Instance = this;
        StartRTMPServer();
    }

    void OnApplicationQuit()
    {
        StopRTMPServer();
    }

    void StartRTMPServer()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string serverDir = Path.Combine(projectRoot, "NodeMediaServer"); 

        string serverScript = Path.Combine(serverDir, "server.js");
        if (!File.Exists(serverScript))
        {
            UnityEngine.Debug.LogError("server.js not found at: " + serverScript);
            return;
        }

        serverProcess = new Process();
        serverProcess.StartInfo.WorkingDirectory = serverDir;
        serverProcess.StartInfo.FileName = "node";
        serverProcess.StartInfo.Arguments = "server.js";
        serverProcess.StartInfo.UseShellExecute = false;
        serverProcess.StartInfo.CreateNoWindow = true;
        serverProcess.Start();

        UnityEngine.Debug.Log("NodeMediaServer started.");
    }

    void StopRTMPServer()
    { 
        serverProcess.Kill();
        serverProcess.WaitForExit();
        UnityEngine.Debug.Log("NodeMediaServer stopped.");
    }
}
