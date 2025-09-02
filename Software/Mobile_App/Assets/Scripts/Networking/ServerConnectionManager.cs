using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class ServerConnectionManager : MonoBehaviour
{
    public static ServerConnectionManager Instance { get; private set; }

    public int defaultPort = 6900;
    private TcpClient client;
    private NetworkStream stream;

    private bool isConnected = false;

    public void ConnectToServer(string ip, int port)
    {
        try
        {
            client = new TcpClient();
            client.Connect(ip, port); // Blocking call
            stream = client.GetStream();
            isConnected = true;
            new Thread(GeneralMessagingReceiveThread).Start();
            Debug.Log("Connected to server!");
        }
        catch (SocketException e)
        {
            Debug.LogError("Connection failed: " + e.Message);
        }
    }

    public bool GetIsConnected()
    {
        return isConnected;
    }

    public void KillConnection()
    {
        isConnected = false;
    }

    private void OnApplicationQuit()
    {
        stream?.Close();
        client?.Close();
    }

    private void Awake()
    {
        Instance = this;
    }

    public void SendServerMessage(GeneralMessageType messageType, string message_str)
    {
        if (isConnected)
        {
            byte[] message = Encoding.UTF8.GetBytes($"{messageType.ToString()}:{message_str}\n");
            stream.Write(message, 0, message.Length);
        }
    }

    public void SendServerMessage(GeneralMessageType messageType, float message_f)
    {
        string message_str = message_f.ToString();
        SendServerMessage(messageType, message_str);
    }

    public void SendServerMessage(GeneralMessageType messageType, int message_int)
    {
        string message_str = message_int.ToString();
        SendServerMessage(messageType, message_str);
    }

    public void GeneralMessagingReceiveThread()
    {
        StreamReader reader = new StreamReader(stream);

        while (isConnected)
        {
            string message = reader.ReadLine();
            Debug.Log($"Message received {message}");
            HandleIncomingMessage(message);
        }

        client.Close();
    }

    private void HandleIncomingMessage(string fullMmessage)
    {

        if (fullMmessage == null)
        {
            Debug.LogWarning("Server lost. Ending thread.");
            KillConnection(); // Exit the while loop
            return;
        }

        if (string.IsNullOrEmpty(fullMmessage)) return;

        string[] messageParts = fullMmessage.Split(':', 2);

        if (messageParts.Length != 2)
        {
            Debug.Log("Bad Message!");
            return;
        }

        string type = messageParts[0];
        string message = messageParts[1];

        if (Enum.TryParse(type, out GeneralMessageType messageType))
        {
            switch (messageType)
            {
                //UI elements cannot be changed from a thread, must be done in main thread
                case GeneralMessageType.SESSION_START:
                    MainThreadDispatcher.Instance.DispatchAction(() => PageManager.Instance.GoToSessionPage());
                    break;
                case GeneralMessageType.SESSION_END:
                    MainThreadDispatcher.Instance.DispatchAction(() => PageManager.Instance.GoToSessionCompletePage());
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