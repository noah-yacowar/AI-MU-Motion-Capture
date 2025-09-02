using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using UnityEngine;

public class ParticipantPoseStream
{ 
    private bool participantActive = true;

    //Include different feeds, not widget based so they may be connections or somethign else.
    //Will also need access to write to the appropcirate places (interrupt with new frames/hr values)
    //
    TcpClient tcpClient;
    private NetworkStream stream;

    //Events to interact with partiticpant class
    public event System.Action<string> OnPoseReceived;

    public ParticipantPoseStream() // Not sure where user will input data, whether before or after
    {
        stream = tcpClient.GetStream();
        new Thread(GeneralMessagingReceiveThread).Start();
    }

    public void KillParticipantMessanger()
    {
        participantActive = false;
    }

    public void SendClientMessage(GeneralMessageType messageType, string Message = "")
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
    }

    tcpClient.Close();
    }

}