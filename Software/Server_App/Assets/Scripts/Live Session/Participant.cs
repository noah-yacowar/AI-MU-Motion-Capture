using UnityEngine;
using System.Net.Sockets;
using System;
using System.IO;
using UnityEngine.UI;

public class Participant
{
    private ParticipantGeneralMessaging generalMessanger;
    private ParticipantIMUReceiver imuStream;
    private SkeletonManager skeletonManager;
    private ParticipantUIManager uiManager;

    public int id;
    public string name;

    public event System.Action<string> OnNameReceived;

    string userFolderPath = "";
    string userFolderPathRel = "";

    //Include different feeds, not widget based so they may be connections or somethign else.
    //Will also need access to write to the appropcirate places (interrupt with new frames/hr values)
    //


    public Participant(TcpClient tcpClient, int id, ParticipantUIManager uiManager, SkeletonManager skeletonManager, Button calibrateButton) // Not sure where user will input data, whether before or after
    {
        this.id = id;

        this.uiManager = uiManager;
        this.skeletonManager = skeletonManager;

        imuStream = new ParticipantIMUReceiver(skeletonManager, calibrateButton);

        generalMessanger = new ParticipantGeneralMessaging(tcpClient, imuStream); //Eventually hr manager should run its own messaging thread
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

        imuStream?.StopReceiving();
        skeletonManager?.StopBVHStreaming();
        generalMessanger?.KillParticipantMessanger();
    }

    public void StartSession()
    {
    }

    public void StartRecording()
    {
        SendMessage(GeneralMessageType.SESSION_START);

        userFolderPathRel = Path.Combine(SessionManager.sessionFolderPathRel, name); ;
        userFolderPath = Path.Combine(SessionManager.sessionFolderPath, name);
        Directory.CreateDirectory(userFolderPath);

        // Create CSV file inside this folder
        string hrPath = Path.Combine(userFolderPath, "Heart_Rate.csv");

        // on StartRecording
        var bvhPath = Path.Combine(userFolderPath, "mocap.bvh");
        skeletonManager.StartBVHStreaming(bvhPath, 30f);
    }
}
