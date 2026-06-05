using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class OledTouchTcpClientService : MonoBehaviour
{
    [Header("Connection Settings")]
    [SerializeField] private string host = "127.0.0.1";
    [SerializeField] private int port = 5000;
    [SerializeField] private float reconnectIntervalSec = 2f;
    [SerializeField] private bool autoConnect = true;

    [Header("Receiver")]
    [SerializeField] private OledTouchRouter touchRouter;

    [Header("Debug")]
    [SerializeField] private bool logConnection = true;
    [SerializeField] private bool logReceivedMessage = false;

    private readonly Queue<string> messageQueue = new Queue<string>();
    private readonly Queue<string> logQueue = new Queue<string>();

    private readonly object messageLock = new object();
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
        FlushMessages();
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

    private void RunClientLoop()
    {
        while (shouldRun)
        {
            try
            {
                EnqueueLog("[OLED TCP] Connecting to " + host + ":" + port);

                using (TcpClient client = new TcpClient())
                {
                    currentClient = client;
                    client.NoDelay = true;
                    client.Connect(host, port);

                    isConnected = true;
                    EnqueueLog("[OLED TCP] Connected.");

                    using (NetworkStream stream = client.GetStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        while (shouldRun && client.Connected)
                        {
                            string line = reader.ReadLine();

                            if (line == null)
                            {
                                break;
                            }

                            EnqueueMessage(line);
                        }
                    }
                }
            }
            catch (SocketException exception)
            {
                EnqueueLog("[OLED TCP] Socket error: " + exception.Message);
            }
            catch (IOException exception)
            {
                EnqueueLog("[OLED TCP] IO error: " + exception.Message);
            }
            catch (Exception exception)
            {
                EnqueueLog("[OLED TCP] Error: " + exception.Message);
            }
            finally
            {
                isConnected = false;
                CloseCurrentClient();
                EnqueueLog("[OLED TCP] Disconnected.");
            }

            if (!shouldRun)
            {
                break;
            }

            int sleepMs = Mathf.Max(100, Mathf.RoundToInt(reconnectIntervalSec * 1000f));
            Thread.Sleep(sleepMs);
        }
    }

    private void EnqueueMessage(string message)
    {
        lock (messageLock)
        {
            messageQueue.Enqueue(message);
        }
    }

    private void EnqueueLog(string log)
    {
        lock (logLock)
        {
            logQueue.Enqueue(log);
        }
    }

    private void FlushMessages()
    {
        while (true)
        {
            string message = null;

            lock (messageLock)
            {
                if (messageQueue.Count > 0)
                {
                    message = messageQueue.Dequeue();
                }
            }

            if (message == null)
            {
                break;
            }

            if (logReceivedMessage)
            {
                Debug.Log("[OLED TCP Message] " + message);
            }

            if (touchRouter != null)
            {
                touchRouter.ReceiveRawJson(message);
            }
        }
    }

    private void FlushLogs()
    {
        if (!logConnection)
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
