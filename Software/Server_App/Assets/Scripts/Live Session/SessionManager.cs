using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using Unity.VisualScripting;
using System;
using System.IO;
using UnityEngine.UI;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    TcpListener TcpServer;

    private Dictionary<int, Participant> participantDict = new Dictionary<int, Participant>();
    int numParticipants = 0;

    private bool sessionStarted = false;

    public static string sessionFolderName = "";
    public static string sessionFolderPath = "";
    public static string sessionFolderPathRel = "";


    //This is temporary and should be spawned dynamically for multiple participants
    public ParticipantUIManager participantUIManager; 
    public ParticipantVideoFeedStream participantVideoFeedReceiver;
    public SkeletonManager skeletonManager;
    public Button calibrateButton;

    private void Start()
    {
        Instance = this;

        TcpServer = new TcpListener(IPAddress.Any, NetworkInfo.defaultPort);
        TcpServer.Start();
        new Thread(SearchForNewConnections).Start();
    }

    public void KillSearchNewParticipantsThread()
    {
        try { TcpServer?.Stop(); } catch { }
        sessionStarted = true;
    }

    private void SearchForNewConnections()
    {
        while(!sessionStarted)
        {
            TcpClient client = TcpServer.AcceptTcpClient();
            Participant newParticipant = new Participant(client, numParticipants, participantUIManager, participantVideoFeedReceiver, skeletonManager, calibrateButton);
            newParticipant.OnNameReceived += SetParticipantName;
            participantDict[numParticipants] = newParticipant;
            numParticipants++;
        }
    }

    public void EndSession()
    {
        Debug.Log("Ending session!");

        foreach (Participant participant in participantDict.Values)
        {
            participant.EndSession();
        }
    }

    public void StartSession()
    {
        KillSearchNewParticipantsThread();

        foreach (Participant participant in participantDict.Values)
        {
            participant.StartSession();
        }
    }

    public void StartRecording()
    {
        sessionFolderName = "Session_" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
        sessionFolderPathRel = Path.Combine("Sessions", sessionFolderName).Replace("/", "\\");
        sessionFolderPath = Path.Combine(Application.dataPath, sessionFolderPathRel).Replace("/", "\\");
        Directory.CreateDirectory(sessionFolderPath); // Create the directory!

        Debug.Log("Starting session!");

        foreach(Participant participant in participantDict.Values) 
        {
            participant.StartRecording();
        }
    }

    private void OnApplicationQuit()
    {
        KillSearchNewParticipantsThread();
        EndSession();
    }

    //TEMPORARY FUNCTIONS FOR SINGLE PARTICIPANT BOX EDITING (STATIC REFERENCE)
    private void SetParticipantName(string name)
    {
    }
}
