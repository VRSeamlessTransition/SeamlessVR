using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System;

public class ServerManager : MonoBehaviour
{
    [SerializeField] private TaskManager m_TaskManager;
    public RawImage m_Image;
    public TextMeshProUGUI m_TextLog;

    private TcpListener tcpListener;
    private const int port = 5000;
    private TcpClient tcpClient;
    private NetworkStream stream;
    private BinaryReader reader;
    private BinaryWriter writer;
    private CancellationTokenSource cancellationTokenSource;

    void Start()
    {
        cancellationTokenSource = new CancellationTokenSource();
        
        // server application set up
        Application.runInBackground = true;

        try
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            m_TextLog.SetText("Server started...");
            ListenForData(cancellationTokenSource.Token);
        }
        catch (SocketException e)
        {
            m_TextLog.SetText("SocketException: " + e);
        }
        catch (IOException e)
        {
            m_TextLog.SetText("IOException: " + e);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StopServer();
            if (reader != null) reader.Close();
            if (writer != null) writer.Close();
            if (stream != null) stream.Close();
            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }
            Application.Quit();
        }
    }

    private async void ListenForData(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                tcpClient = await tcpListener.AcceptTcpClientAsync();
                //if (!tcpClient.Connected)
                //    m_TextLog.SetText("Disconnected");
                //else
                //    m_TextLog.SetText("Connected!");
                stream = tcpClient.GetStream();
                ReceiveDataFromClient();
            }
            catch (SocketException e)
            {
                m_TextLog.SetText("SocketException: " + e);
                break;
            }
            catch (IOException e)
            {
                m_TextLog.SetText("IOException: " + e);
                break;
            }
            catch (ObjectDisposedException)
            {
                Debug.Log("Listener closed, exiting loop.");
                break;
            }
        }
    }

    private void ReceiveDataFromClient()
    {
        if (stream.CanRead)  // stream.DataAvailable
        {
            try
            {
                reader = new BinaryReader(stream);

                // recv message
                int messageLength = reader.ReadInt32();
                byte[] messageBytes = reader.ReadBytes(messageLength);
                string message = System.Text.Encoding.UTF8.GetString(messageBytes);
                Debug.Log("Data received..." + message);
                var res = ParseMessage(message);
                m_TaskManager.SetupCurrentTask(res.Item1, res.Item2, res.Item3, res.Item4, res.Item5, res.Item6);

                if (res.Item1 != 2)  // != shell game
                {
                    // recv texture
                    int textureLength = reader.ReadInt32();
                    byte[] textureBytes = reader.ReadBytes(textureLength);
                    Texture2D texture2D = new Texture2D(Constants.DISPLAY_SCREEN_WIDTH, Constants.DISPLAY_SCREEN_HEIGHT);
                    texture2D.LoadImage(textureBytes);
                    m_Image.texture = texture2D;
                }
            }
            catch (IOException e)
            {
                m_TextLog.SetText("IOException: " + e);
            }
        }
    }

    /** FORMAT [taskIndex], [trialIndex], [isFinalInt], [waitTimeBeforePrompt], [conditionType], [taskSpecificInfo]
     */
    private (int, int, int, float, int, float[]) ParseMessage(string message)
    {
        const int staticItemCount = 5;
        string[] splitStrs = message.Split(',');

        int taskId = int.Parse(splitStrs[0].Trim());     
        int trialId = int.Parse(splitStrs[1].Trim());
        int isFinalInt = int.Parse(splitStrs[2].Trim());
        float timeElapsedInVR = float.Parse(splitStrs[3].Trim());
        int conditionType = int.Parse(splitStrs[4].Trim());
        
        if (splitStrs.Length > staticItemCount)
        {
            float[] otherNums = new float[splitStrs.Length - staticItemCount];
            for (int i = staticItemCount; i < splitStrs.Length; i++)
            {
                otherNums[i - staticItemCount] = float.Parse(splitStrs[i].Trim());
            }
            return (taskId, trialId, isFinalInt, timeElapsedInVR, conditionType, otherNums);
        }
        else
        {
            return (taskId, trialId, isFinalInt, timeElapsedInVR, conditionType, null);
        }
    }

    private void StopServer()
    {
        cancellationTokenSource?.Cancel();
        tcpListener?.Stop();
    }

    public void SendBackToClient()
    {
        if (tcpClient != null && tcpClient.Connected)
        {
            SendResponse();
        }
        Debug.Log("test send back to client!");
    }

    private void SendResponse()
    {
        try
        {
            writer = new BinaryWriter(stream);

            // Send response back to client
            string responseMessage = "Next";
            byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(responseMessage);
            writer.Write(responseBytes.Length);
            writer.Write(responseBytes);
            writer.Flush();
            m_TextLog.SetText("Response sent to client...");
        }
        catch (IOException e)
        {
            Debug.LogError("IOException: " + e);
        }
    }

    private void OnDestroy()
    {
        StopServer();
        reader?.Close();
        writer?.Close();
        stream?.Close();
        if (tcpClient != null)
        {
            tcpClient.Close();
            tcpClient = null;
        }
    }
}
