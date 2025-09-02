using UnityEngine;
using System.Net.Sockets;
using System;
using System.IO;
using UnityEngine.UI;

public class Participant
{
    private ParticipantGeneralMessaging generalMessanger;
    private ParticipantHeartRateStream hrManager;
    private ParticipantIMUReceiver imuStream;
    private SkeletonManager skeletonManager;
    public ParticipantVideoFeedStream videoFeedReceiver;
    private ParticipantUIManager uiManager;

    public int id;
    public string name;

    public event System.Action<string> OnNameReceived;

    private StreamWriter hrWriter;
    private StreamWriter exertionWriter;

    string userFolderPath = "";
    string userFolderPathRel = "";

    //Include different feeds, not widget based so they may be connections or somethign else.
    //Will also need access to write to the appropcirate places (interrupt with new frames/hr values)
    //


    public Participant(TcpClient tcpClient, int id, ParticipantUIManager uiManager, ParticipantVideoFeedStream videoReceiver, SkeletonManager skeletonManager, Button calibrateButton) // Not sure where user will input data, whether before or after
    {
        this.id = id;

        this.uiManager = uiManager;
        this.videoFeedReceiver = videoReceiver;
        this.skeletonManager = skeletonManager;

        hrManager = new ParticipantHeartRateStream();
        hrManager.OnHeartRateReceived += SetHR;
        hrManager.OnExertionReceived += SetExertion;

        imuStream = new ParticipantIMUReceiver(skeletonManager, calibrateButton);

        generalMessanger = new ParticipantGeneralMessaging(tcpClient, hrManager, imuStream); //Eventually hr manager should run its own messaging thread
        generalMessanger.OnNameReceived += SetUserName;

        Action addUserTagMethod = () => ParticipantJoinListUIManager.Instance.AddNewUserTag(id);
        MainThreadDispatcher.Instance.DispatchAction(addUserTagMethod); //This constructor runs within a thread, UI only on main thread
    }    

    public void SendMessage(GeneralMessageType messageType, string Message="")
    {
        generalMessanger.SendClientMessage(messageType, Message);
    }

    public void SetUserName(string name) 
    {
        this.name = name;
        OnNameReceived(name);
        uiManager.SetName(name);
        ParticipantJoinListUIManager.Instance.ChangeTagName(id, name);
    }

    public void EndSession()
    {
        SendMessage(GeneralMessageType.SESSION_END);

        hrWriter?.Flush();
        hrWriter?.Close();
        hrWriter = null;

        exertionWriter?.Flush();
        exertionWriter?.Close();
        exertionWriter = null;

        videoFeedReceiver?.StopAndSaveVideoFeed();
        imuStream?.StopReceiving();
        skeletonManager?.StopBVHStreaming();
        generalMessanger?.KillParticipantMessanger();
    }

    public void StartSession()
    {
        videoFeedReceiver.Initialize();
    }

    public void StartRecording()
    {
        SendMessage(GeneralMessageType.SESSION_START);

        userFolderPathRel = Path.Combine(SessionManager.sessionFolderPathRel, name); ;
        userFolderPath = Path.Combine(SessionManager.sessionFolderPath, name);
        Directory.CreateDirectory(userFolderPath);

        videoFeedReceiver.StartSavingStream(userFolderPathRel);

        // Create CSV file inside this folder
        string hrPath = Path.Combine(userFolderPath, "Heart_Rate.csv");

        hrWriter = new StreamWriter(hrPath, append: false);
        hrWriter.WriteLine("Timestamp,HeartRate");

        // Create CSV file inside this folder
        string exertionPath = Path.Combine(userFolderPath, "Exertion.csv");
        exertionWriter = new StreamWriter(exertionPath, append: false);
        exertionWriter.WriteLine("Timestamp,Exertion");

        // on StartRecording
        var bvhPath = Path.Combine(userFolderPath, "mocap.bvh");
        skeletonManager.StartBVHStreaming(bvhPath, 30f);
    }

    public void SetHR(int hr)
    {
        uiManager.SetHR(hr);
        if (hrWriter != null)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            hrWriter.WriteLine($"{timestamp},{hr}");
        }
    }

    public void SetExertion(float exertion)
    {
        uiManager.SetExertion(exertion);
        if (exertionWriter != null)
        {
            float exertionClamped = Mathf.Clamp(exertion, 0f, 100f);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            exertionWriter.WriteLine($"{timestamp},{exertionClamped:F2}");
        }
    }
}
