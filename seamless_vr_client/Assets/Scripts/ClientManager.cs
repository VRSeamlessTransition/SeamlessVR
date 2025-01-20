using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class ClientManager : MonoBehaviour
{
    [SerializeField] private ShellGameTaskSpawner m_ShellGameTask;

    private const string serverIP = "127.0.0.1"; 
    private const int port = 5000;
    private TcpClient tcpClient;
    private NetworkStream stream;
    private BinaryReader reader;
    private BinaryWriter writer;
    private Thread clientThread;
    private bool threadCompleted = false;

    #region Events

    public event Action OnServerMessageReceived;

    #endregion

    private void OnEnable()
    {
        m_ShellGameTask.OnShellGameObjectSpawned += SendShellGameData;
    }

    private void OnDisable()
    {
        m_ShellGameTask.OnShellGameObjectSpawned -= SendShellGameData;
    }

    private void Start()
    {

    }

    public void ConnectToTcpServer()
    {
        StopCurrentThread();

        try
        {
            tcpClient = new TcpClient(serverIP, port);
            stream = tcpClient.GetStream();
            Debug.Log("tcp connected status " + tcpClient.Connected);
            clientThread = new Thread(new ThreadStart(ReceiveMessage));
            clientThread.IsBackground = true;
            threadCompleted = false;
            clientThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }

    private void StopCurrentThread()
    {
        if (clientThread != null && clientThread.IsAlive)
        {
            tcpClient.Close();
            clientThread.Join();
        }

        Debug.Log("finish stop thread!");
    }

    private void ReceiveMessage()
    {
        try
        {
            while (tcpClient.Connected)
            {
                if (stream.CanRead && stream.DataAvailable)
                {
                    reader = new BinaryReader(stream);

                    try
                    {
                        int responseLength = reader.ReadInt32();
                        if (responseLength > 0)
                        {
                            byte[] responseBytes = reader.ReadBytes(responseLength);
                            string serverMessage = Encoding.UTF8.GetString(responseBytes);
                            Debug.Log("Server message received: " + serverMessage);

                            if (serverMessage.Length > 0)
                            {
                                tcpClient.Client.Disconnect(false);
                                OnServerMessageReceived?.Invoke();
                                break;  // exit the loop in the thread
                            }
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        Debug.Log("End of stream reached.");
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception in ReceiveMessage: " + e);
        }
        finally
        {
            threadCompleted = true;
            CleanUp();
            Debug.Log("TCP client is not connected to receive message from the server!");
        }
    }

    public void SendData(Texture2D texture, string message)
    {
        if (tcpClient == null || !tcpClient.Connected)
        {
            Debug.Log("TCP client is not conncted!");
            ConnectToTcpServer();
        }

        try
        {
            writer = new BinaryWriter(stream);

            // send message
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            writer.Write(messageBytes.Length);
            writer.Write(messageBytes);

            // send texture
            byte[] textureBytes = texture.EncodeToPNG();
            writer.Write(textureBytes.Length);
            writer.Write(textureBytes);

            /** !!Attention!! Don't close the any stream or writer, or it will make the current connection disconnted.*/
            writer.Flush();
            Debug.Log("Data sent.");
        }
        catch (IOException e)
        {
            Debug.LogError("IOException: " + e);
        }

        Debug.Log("TCP connection status: " + tcpClient.Connected);
    }

    public void SendShellGameData()
    {
        if (tcpClient == null || !tcpClient.Connected)
        {
            Debug.Log("TCP client is not connected!");
            ConnectToTcpServer();
        }

        try
        {
            writer = new BinaryWriter(stream);

            // send message (conditionIdx, shellGameTaskIndex, trialIndex, camerainfos)
            string message = m_ShellGameTask.GetShellGameData();
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            writer.Write(messageBytes.Length);
            writer.Write(messageBytes);

            writer.Flush();
            Debug.Log("Data sent.");
        }
        catch (IOException e)
        {
            Debug.LogError("IOException: " + e);
        }

        Debug.Log("TCP connection status: " + tcpClient.Connected);
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        if (reader != null) reader.Close();
        if (writer != null) writer.Close();
        if (tcpClient != null) tcpClient.Close();
    }
}
