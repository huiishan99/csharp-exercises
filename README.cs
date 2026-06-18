using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class GuiCommandTcpClientSender : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 5000;
    [SerializeField] private float reconnectIntervalSec = 2f;
    [SerializeField] private bool autoConnect = true;

    [Header("Event Receive")]
    [SerializeField] private GuiEventDispatcher eventDispatcher;

    [Header("Queue")]
    [SerializeField] private int maxSendQueueSize = 100;
    [SerializeField] private int maxReceiveQueueSize = 200;

    [Header("Debug")]
    [SerializeField] private bool logConnection = true;
    [SerializeField] private bool logSendMessage = true;
    [SerializeField] private bool logReceivedMessage = true;

    private readonly Queue<string> sendQueue = new Queue<string>();
    private readonly Queue<string> receivedQueue = new Queue<string>();
    private readonly Queue<string> logQueue = new Queue<string>();

    private readonly object sendLock = new object();
    private readonly object receivedLock = new object();
    private readonly object logLock = new object();

    private readonly byte[] receiveBuffer = new byte[4096];
    private readonly StringBuilder receiveTextBuffer = new StringBuilder();

    private Thread workerThread;
    private TcpClient currentClient;
    private NetworkStream currentStream;

    private volatile bool shouldRun;
    private volatile bool isConnected;

    public bool IsConnected
    {
        get { return isConnected; }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();

        if (autoConnect)
        {
            Connect();
        }
    }

    private void Update()
    {
        FlushLogs();
        FlushReceivedMessages();
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    public void Connect()
    {
        if (workerThread != null && workerThread.IsAlive)
        {
            return;
        }

        shouldRun = true;

        workerThread = new Thread(RunClientLoop);
        workerThread.IsBackground = true;
        workerThread.Start();
    }

    public void Disconnect()
    {
        shouldRun = false;
        isConnected = false;

        CloseCurrentClient();

        if (workerThread != null && workerThread.IsAlive)
        {
            workerThread.Join(500);
        }

        workerThread = null;
    }

    public void SendCommand(string messageType)
    {
        SendRawJson(GuiCommandFactory.CreateCommand(messageType));
    }

    public void SendCommand(string messageType, string payloadJson)
    {
        SendRawJson(GuiCommandFactory.CreateCommand(messageType, payloadJson));
    }

    public void SendRawJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        lock (sendLock)
        {
            while (sendQueue.Count >= maxSendQueueSize)
            {
                sendQueue.Dequeue();
                EnqueueLog("[GUI TCP] Send queue is full. Dropped oldest message.");
            }

            sendQueue.Enqueue(json);
        }

        if (logSendMessage)
        {
            EnqueueLog("[GUI TCP Queued] " + json);
        }
    }

    private void RunClientLoop()
    {
        while (shouldRun)
        {
            try
            {
                EnqueueLog("[GUI TCP] Connecting to " + host + ":" + port);

                using (TcpClient client = new TcpClient())
                {
                    currentClient = client;
                    client.NoDelay = true;
                    client.Connect(host, port);

                    using (NetworkStream stream = client.GetStream())
                    {
                        currentStream = stream;
                        isConnected = true;

                        ClearReceiveTextBuffer();

                        EnqueueLog("[GUI TCP] Connected.");

                        while (shouldRun && IsClientAlive(client))
                        {
                            ReceiveAvailableMessages(stream);
                            SendQueuedMessages(stream);

                            Thread.Sleep(10);
                        }
                    }
                }
            }
            catch (SocketException exception)
            {
                EnqueueLog("[GUI TCP] Socket error: " + exception.Message);
            }
            catch (IOException exception)
            {
                EnqueueLog("[GUI TCP] IO error: " + exception.Message);
            }
            catch (Exception exception)
            {
                EnqueueLog("[GUI TCP] Error: " + exception.Message);
            }
            finally
            {
                isConnected = false;
                currentStream = null;
                CloseCurrentClient();
                EnqueueLog("[GUI TCP] Disconnected.");
            }

            if (!shouldRun)
            {
                break;
            }

            int sleepMs = Mathf.Max(100, Mathf.RoundToInt(reconnectIntervalSec * 1000f));
            Thread.Sleep(sleepMs);
        }
    }

    private bool IsClientAlive(TcpClient client)
    {
        if (client == null)
        {
            return false;
        }

        if (!client.Connected)
        {
            return false;
        }

        try
        {
            Socket socket = client.Client;

            if (socket == null)
            {
                return false;
            }

            bool disconnected = socket.Poll(0, SelectMode.SelectRead)
                && socket.Available == 0;

            return !disconnected;
        }
        catch
        {
            return false;
        }
    }

    private void ReceiveAvailableMessages(NetworkStream stream)
    {
        if (stream == null)
        {
            return;
        }

        while (stream.DataAvailable)
        {
            int readCount = stream.Read(receiveBuffer, 0, receiveBuffer.Length);

            if (readCount <= 0)
            {
                return;
            }

            string text = Encoding.UTF8.GetString(receiveBuffer, 0, readCount);
            AppendReceivedText(text);
        }
    }

    private void AppendReceivedText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        receiveTextBuffer.Append(text);

        while (true)
        {
            string current = receiveTextBuffer.ToString();
            int lineBreakIndex = current.IndexOf('\n');

            if (lineBreakIndex < 0)
            {
                break;
            }

            string line = current.Substring(0, lineBreakIndex).Trim();

            receiveTextBuffer.Remove(0, lineBreakIndex + 1);

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            EnqueueReceivedMessage(line);
        }
    }

    private void SendQueuedMessages(NetworkStream stream)
    {
        if (stream == null)
        {
            return;
        }

        while (true)
        {
            string message = DequeueSendMessage();

            if (message == null)
            {
                break;
            }

            string line = message + "\n";
            byte[] data = Encoding.UTF8.GetBytes(line);

            stream.Write(data, 0, data.Length);
            stream.Flush();

            if (logSendMessage)
            {
                EnqueueLog("[GUI TCP Sent] " + message);
            }
        }
    }

    private string DequeueSendMessage()
    {
        lock (sendLock)
        {
            if (sendQueue.Count == 0)
            {
                return null;
            }

            return sendQueue.Dequeue();
        }
    }

    private void EnqueueReceivedMessage(string message)
    {
        lock (receivedLock)
        {
            while (receivedQueue.Count >= maxReceiveQueueSize)
            {
                receivedQueue.Dequeue();
                EnqueueLog("[GUI TCP] Receive queue is full. Dropped oldest message.");
            }

            receivedQueue.Enqueue(message);
        }

        if (logReceivedMessage)
        {
            EnqueueLog("[GUI TCP Received] " + message);
        }
    }

    private void FlushReceivedMessages()
    {
        ResolveReferences();

        while (true)
        {
            string message = null;

            lock (receivedLock)
            {
                if (receivedQueue.Count > 0)
                {
                    message = receivedQueue.Dequeue();
                }
            }

            if (message == null)
            {
                break;
            }

            if (eventDispatcher == null)
            {
                Debug.LogWarning("[GUI TCP] EventDispatcher is not assigned. Message dropped: " + message);
                continue;
            }

            eventDispatcher.ReceiveRawJson(message);
        }
    }

    private void EnqueueLog(string log)
    {
        lock (logLock)
        {
            logQueue.Enqueue(log);
        }
    }

    private void FlushLogs()
    {
        if (!logConnection && !logSendMessage && !logReceivedMessage)
        {
            ClearLogs();
            return;
        }

        while (true)
        {
            string log = null;

            lock (logLock)
            {
                if (logQueue.Count > 0)
                {
                    log = logQueue.Dequeue();
                }
            }

            if (log == null)
            {
                break;
            }

            Debug.Log(log);
        }
    }

    private void ClearLogs()
    {
        lock (logLock)
        {
            logQueue.Clear();
        }
    }

    private void ClearReceiveTextBuffer()
    {
        receiveTextBuffer.Length = 0;
    }

    private void CloseCurrentClient()
    {
        try
        {
            if (currentStream != null)
            {
                currentStream.Close();
            }
        }
        catch
        {
            // 終了時の例外は無視する。
        }
        finally
        {
            currentStream = null;
        }

        try
        {
            if (currentClient != null)
            {
                currentClient.Close();
            }
        }
        catch
        {
            // 終了時の例外は無視する。
        }
        finally
        {
            currentClient = null;
        }
    }

    private void ResolveReferences()
    {
        if (eventDispatcher == null)
        {
            eventDispatcher = FindFirstObjectByType<GuiEventDispatcher>();
        }
    }
}
