using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class WebSocketClient : MonoBehaviour
{
    public event Action Connected;
    public event Action Closed;
    public event Action<string> MessageReceived;
    public event Action<string> ErrorReceived;

    public bool IsConnected
    {
        get { return clientWebSocket != null && clientWebSocket.State == WebSocketState.Open; }
    }

    public bool IsConnecting { get; private set; }

    private readonly Queue<string> pendingMessages = new Queue<string>();
    private readonly Queue<string> pendingErrors = new Queue<string>();
    private readonly Queue<string> pendingSends = new Queue<string>();
    private readonly object queueLock = new object();

    private ClientWebSocket clientWebSocket;
    private CancellationTokenSource cancellationTokenSource;
    private bool pendingConnected;
    private bool pendingClosed;

    private void Update()
    {
        bool shouldNotifyConnected = false;
        bool shouldNotifyClosed = false;

        lock (queueLock)
        {
            shouldNotifyConnected = pendingConnected;
            shouldNotifyClosed = pendingClosed;
            pendingConnected = false;
            pendingClosed = false;
        }

        if (shouldNotifyConnected)
        {
            Connected?.Invoke();
        }

        DispatchQueuedStrings(pendingMessages, MessageReceived);
        DispatchQueuedStrings(pendingErrors, ErrorReceived);

        if (shouldNotifyClosed)
        {
            Closed?.Invoke();
        }
    }

    private void OnDestroy()
    {
        Close();
    }

    public async void Connect(string url)
    {
        if (IsConnected || IsConnecting)
        {
            return;
        }

        if (string.IsNullOrEmpty(url))
        {
            EnqueueError("WebSocket url is empty.");
            return;
        }

        try
        {
            IsConnecting = true;
            DisposeSocket();
            cancellationTokenSource = new CancellationTokenSource();
            clientWebSocket = new ClientWebSocket();
            clientWebSocket.Options.Proxy = null;
            await clientWebSocket.ConnectAsync(new Uri(url), cancellationTokenSource.Token);

            IsConnecting = false;
            EnqueueConnected();
            await FlushPendingSends();
            await ReceiveLoop(cancellationTokenSource.Token);
            DisposeSocket();
            EnqueueClosed();
        }
        catch (OperationCanceledException)
        {
            IsConnecting = false;
            DisposeSocket();
        }
        catch (Exception exception)
        {
            IsConnecting = false;
            ClearPendingSends();
            DisposeSocket();
            EnqueueError("WebSocket connection failed: " + GetExceptionMessage(exception));
            EnqueueClosed();
            Debug.LogWarning("[WebSocketClient] Connect failed: " + exception);
        }
    }

    public void Send(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            EnqueueError("WebSocket send ignored empty json.");
            return;
        }

        lock (queueLock)
        {
            pendingSends.Enqueue(json);
        }

        if (IsConnected)
        {
            _ = FlushPendingSends();
        }
        else if (!IsConnecting)
        {
            EnqueueError("WebSocket is not connected. Message queued but no active connection exists.");
        }
    }

    public async void Close()
    {
        try
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            if (clientWebSocket != null
                && (clientWebSocket.State == WebSocketState.Open || clientWebSocket.State == WebSocketState.CloseReceived))
            {
                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "client close", CancellationToken.None);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[WebSocketClient] Close failed: " + exception.Message);
        }
        finally
        {
            IsConnecting = false;
            DisposeSocket();
            EnqueueClosed();
        }
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        while (!cancellationToken.IsCancellationRequested && IsConnected)
        {
            using (var stream = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        EnqueueClosed();
                        return;
                    }

                    stream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                string message = Encoding.UTF8.GetString(stream.ToArray());
                EnqueueMessage(message);
            }
        }
    }

    private async Task FlushPendingSends()
    {
        while (IsConnected)
        {
            string json = null;
            lock (queueLock)
            {
                if (pendingSends.Count > 0)
                {
                    json = pendingSends.Dequeue();
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await clientWebSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationTokenSource.Token);
            }
            catch (Exception exception)
            {
                EnqueueError("WebSocket send failed: " + exception.Message);
                Debug.LogWarning("[WebSocketClient] Send failed: " + exception);
                return;
            }
        }
    }

    private void DispatchQueuedStrings(Queue<string> queue, Action<string> handler)
    {
        while (true)
        {
            string value = null;
            lock (queueLock)
            {
                if (queue.Count > 0)
                {
                    value = queue.Dequeue();
                }
            }

            if (value == null)
            {
                return;
            }

            handler?.Invoke(value);
        }
    }

    private void EnqueueConnected()
    {
        lock (queueLock)
        {
            pendingConnected = true;
        }
    }

    private void EnqueueClosed()
    {
        lock (queueLock)
        {
            pendingClosed = true;
        }
    }

    private void EnqueueMessage(string message)
    {
        lock (queueLock)
        {
            pendingMessages.Enqueue(message);
        }
    }

    private void EnqueueError(string message)
    {
        lock (queueLock)
        {
            pendingErrors.Enqueue(message);
        }
    }

    private void ClearPendingSends()
    {
        lock (queueLock)
        {
            pendingSends.Clear();
        }
    }

    private string GetExceptionMessage(Exception exception)
    {
        if (exception == null)
        {
            return "unknown exception";
        }

        if (exception.InnerException == null)
        {
            return exception.Message;
        }

        return exception.Message + " Inner: " + exception.InnerException.Message;
    }

    private void DisposeSocket()
    {
        if (clientWebSocket != null)
        {
            clientWebSocket.Dispose();
            clientWebSocket = null;
        }

        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }
    }
}
