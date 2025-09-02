using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class ParticipantGeneralMessaging
{
    private bool participantActive = true;

    //Include different feeds, not widget based so they may be connections or somethign else.
    //Will also need access to write to the appropcirate places (interrupt with new frames/hr values)
    //
    TcpClient tcpClient;
    private NetworkStream stream;

    //Events to interact with partiticpant class
    public event System.Action<string> OnNameReceived;
    public event System.Action<float> OnHeightReceived;

    //Objects to call into directly
    ParticipantIMUReceiver imuStream;

    bool imuStreaming = false;

    public ParticipantGeneralMessaging(TcpClient tcpClient, ParticipantIMUReceiver imuStream) // Not sure where user will input data, whether before or after
    {
        //DELETE THIS: 
        //MainThreadDispatcher.Instance.DispatchAction(() => imuStream.InitializeSkeleton(1.75f));
        //imuStream.InitializeConnection(4210);

        this.imuStream = imuStream;

        this.tcpClient = tcpClient;
        stream = tcpClient.GetStream();
        new Thread(GeneralMessagingReceiveThread).Start();

    }

    public void KillParticipantMessanger()
    {
        participantActive = false;
    }

    public void SendClientMessage(GeneralMessageType messageType, string Message="")
    {
        if (participantActive)
        {
            byte[] message = Encoding.UTF8.GetBytes($"{messageType.ToString()}:{Message}\n");
            stream.Write(message, 0, message.Length);
        }
    }

    public void GeneralMessagingReceiveThread()
    {
        StreamReader reader = new StreamReader(stream);

        while (participantActive)
        {
            string message = reader.ReadLine();
            Debug.Log($"Message received {message}");
            HandleIncomingMessage(message);
        }

        tcpClient.Close();
    }

    private void HandleIncomingMessage(string fullMmessage)
    {

        if (fullMmessage == null)
        {
            Debug.LogWarning("Client disconnected. Ending thread.");
            KillParticipantMessanger(); // Exit the while loop
            return;
        }

        if (string.IsNullOrEmpty(fullMmessage)) return;

        string[] messageParts = fullMmessage.Split(':', 2);

        if(messageParts.Length != 2) 
        {
            Debug.Log("Bad Message!");
            return;
        }

        string type = messageParts[0];
        string message = messageParts[1];

        if(Enum.TryParse(type, out GeneralMessageType messageType))
        {
            switch(messageType)
            {
                //UI elements cannot be changed from a thread, must be done in main thread... so I exit the thread from here
                case GeneralMessageType.NAME_ENTERED:
                    MainThreadDispatcher.Instance.DispatchAction(() => OnNameReceived(message));
                    break;
                case GeneralMessageType.HEIGHT_ENTERED:
                    float height_m = float.Parse(message);
                    MainThreadDispatcher.Instance.DispatchAction(() => imuStream.InitializeSkeleton(height_m));
                    break;
                case GeneralMessageType.IMU_CONFIG:
                    if (imuStreaming) return;
                    imuStreaming = true;
                    int imuPort = int.Parse(message);
                    imuStream.InitializeConnection(imuPort);
                    break;
            }
        }
    }

}

public enum GeneralMessageType
{
    NAME_ENTERED,
    AGE_ENETERED,
    HEIGHT_ENTERED,
    SESSION_START,
    HEART_RATE,
    HR_BASELINE,
    SESSION_END,
    IMU_CONFIG,
}
