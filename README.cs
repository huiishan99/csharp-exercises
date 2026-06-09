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

    [Header("Queue")]
    [SerializeField] private int maxQueueSize = 100;

    [Header("Debug")]
    [SerializeField] private bool logConnection = true;
    [SerializeField] private bool logSendMessage = true;

    private readonly Queue<string> sendQueue = new Queue<string>();
    private readonly Queue<string> logQueue = new Queue<string>();

    private readonly object sendLock = new object();
    private readonly object logLock = new object();

    private Thread workerThread;
    private TcpClient currentClient;
    private volatile bool shouldRun;
    private volatile bool isConnected;

    public bool IsConnected
    {
        get { return isConnected; }
    }

    private void Start()
    {
        if (autoConnect)
        {
            Connect();
        }
    }

    private void Update()
    {
        FlushLogs();
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
            while (sendQueue.Count >= maxQueueSize)
            {
                sendQueue.Dequeue();
                EnqueueLog("[GUI CMD TCP] Send queue is full. Dropped oldest message.");
            }

            sendQueue.Enqueue(json);
        }

        if (logSendMessage)
        {
            EnqueueLog("[GUI CMD Queued] " + json);
        }
    }

    private void RunClientLoop()
    {
        while (shouldRun)
        {
            try
            {
                EnqueueLog("[GUI CMD TCP] Connecting to " + host + ":" + port);

                using (TcpClient client = new TcpClient())
                {
                    currentClient = client;
                    client.NoDelay = true;
                    client.Connect(host, port);

                    isConnected = true;
                    EnqueueLog("[GUI CMD TCP] Connected.");

                    using (NetworkStream stream = client.GetStream())
                    using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.NewLine = "\n";
                        writer.AutoFlush = true;

                        while (shouldRun && client.Connected)
                        {
                            string message = DequeueMessage();

                            if (message == null)
                            {
                                Thread.Sleep(10);
                                continue;
                            }

                            writer.WriteLine(message);

                            if (logSendMessage)
                            {
                                EnqueueLog("[GUI CMD Sent] " + message);
                            }
                        }
                    }
                }
            }
            catch (SocketException exception)
            {
                EnqueueLog("[GUI CMD TCP] Socket error: " + exception.Message);
            }
            catch (IOException exception)
            {
                EnqueueLog("[GUI CMD TCP] IO error: " + exception.Message);
            }
            catch (Exception exception)
            {
                EnqueueLog("[GUI CMD TCP] Error: " + exception.Message);
            }
            finally
            {
                isConnected = false;
                CloseCurrentClient();
                EnqueueLog("[GUI CMD TCP] Disconnected.");
            }

            if (!shouldRun)
            {
                break;
            }

            int sleepMs = Mathf.Max(100, Mathf.RoundToInt(reconnectIntervalSec * 1000f));
            Thread.Sleep(sleepMs);
        }
    }

    private string DequeueMessage()
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

    private void EnqueueLog(string log)
    {
        lock (logLock)
        {
            logQueue.Enqueue(log);
        }
    }

    private void FlushLogs()
    {
        if (!logConnection && !logSendMessage)
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

    private void CloseCurrentClient()
    {
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
}
