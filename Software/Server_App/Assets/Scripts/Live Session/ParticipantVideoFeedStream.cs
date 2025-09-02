using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Concurrent;

public class ParticipantVideoFeedStream : MonoBehaviour 
{
    public RawImage targetImage;  // Drag your RawImage here in the inspector
    public int width = 1280;
    public int height = 720;

    private Process streamProcess;
    private Process saveProcess;
    private Texture2D videoTexture;
    private byte[] buffer;

    private volatile byte[] latestFrame;
    private object frameLock = new object();
    private bool newFrameAvailable = false;
    private int frameSize;


    private ConcurrentQueue<byte[]> frameQueue = new ConcurrentQueue<byte[]>();
    private const int maxBufferedFrames = 10;

    private float deltaTimeAccumulator = 0f;
    private float frameInterval = 1f / 30f; // ~33 ms for 30 FPS


    public void Initialize()
    {
        videoTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        targetImage.texture = videoTexture;
        frameSize = width * height * 3;

        StartFFmpegStream(width, height);
    }

    void Update()
    {
        deltaTimeAccumulator += Time.deltaTime;

        if (deltaTimeAccumulator >= frameInterval)
        {
            if (frameQueue.TryDequeue(out byte[] frame))
            {
                videoTexture.LoadRawTextureData(frame);
                videoTexture.Apply();
            }

            deltaTimeAccumulator -= frameInterval;
        }
    }


    void StartFFmpegStream(int width, int height)
    {
        //Unity Feed Process
        streamProcess = new Process();
        streamProcess.StartInfo.FileName = "ffmpeg/bin/ffmpeg.exe";
        streamProcess.StartInfo.Arguments = "-fflags nobuffer -i rtmp://localhost/live/stream1 -vf \"vflip\" -f rawvideo -pix_fmt rgb24 -";
        streamProcess.StartInfo.RedirectStandardOutput = true;
        streamProcess.StartInfo.UseShellExecute = false;
        streamProcess.StartInfo.CreateNoWindow = true;
        streamProcess.Start();

        videoTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
        targetImage.texture = videoTexture;

        // Run the read loop on a background thread
        new Thread(ReadFrames).Start();
    }

    public void StartSavingStream(string userFolderPath)
    {
        // Save-to-disk process
        saveProcess = new Process();
        saveProcess.StartInfo.FileName = "ffmpeg/bin/ffmpeg.exe";
        saveProcess.StartInfo.Arguments =
            $"-fflags nobuffer -i rtmp://localhost/live/stream1 -c:v libx264 -preset ultrafast -crf 23 pov.mp4";
        saveProcess.StartInfo.UseShellExecute = false;
        saveProcess.StartInfo.CreateNoWindow = true;
        saveProcess.StartInfo.RedirectStandardInput = true;
        saveProcess.Start();
    }


    void ReadFrames()
    {
        var buffer = new byte[frameSize];

        while (streamProcess != null && !streamProcess.HasExited)
        {
            int read = 0;
            while (read < frameSize)
            {
                int bytes = streamProcess.StandardOutput.BaseStream.Read(buffer, read, frameSize - read);
                if (bytes == 0)
                    return;
                read += bytes;
            }

            var copy = new byte[frameSize];
            Buffer.BlockCopy(buffer, 0, copy, 0, frameSize);

            frameQueue.Enqueue(copy);

            // Keep buffer size capped
            while (frameQueue.Count > maxBufferedFrames)
                frameQueue.TryDequeue(out _); // drop oldest
        }
    }

    public void StopAndSaveVideoFeed()
    {
        // Safely stop the save process
        if (saveProcess != null && !saveProcess.HasExited)
        {
            try
            {
                saveProcess.StandardInput.WriteLine("q"); // signal ffmpeg to stop
                saveProcess.StandardInput.Close(); // close the input stream
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("Failed to send 'q' to ffmpeg: " + ex.Message);
            }

            // Wait for ffmpeg to flush output and exit
            saveProcess.WaitForExit();
            saveProcess.Dispose();
            saveProcess = null;
        }

        // Kill Unity display stream
        if (streamProcess != null && !streamProcess.HasExited)
        {
            streamProcess.Kill();
            streamProcess.WaitForExit();
            streamProcess.Dispose();
            streamProcess = null;
        }
    }




    void OnApplicationQuit()
    {
        StopAndSaveVideoFeed();
    }
}
