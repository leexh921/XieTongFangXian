using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Unity WebSocket 客户端单例。
/// 
/// 为了减少导入问题，这里使用 .NET 自带的 System.Net.WebSockets.ClientWebSocket，
/// 不依赖 Newtonsoft.Json、NativeWebSocket、BestHTTP 等第三方包。
/// 
/// Unity 设置建议：
/// Edit -> Project Settings -> Player -> Other Settings -> Api Compatibility Level
/// 选择 .NET Standard 2.1 或 .NET Framework 4.x。
/// </summary>
public class WebSocketClient : MonoBehaviour
{
    public static WebSocketClient Instance { get; private set; }

    public event Action OnConnected;
    public event Action<string> OnDisconnected;
    public event Action<string> OnRawMessage;
    public event Action<LoginResultMessage> OnLoginResult;
    public event Action<GameStartMessage> OnGameStart;
    public event Action<StateUpdateMessage> OnStateUpdate;
    public event Action<BuildResultMessage> OnBuildResult;
    public event Action<GameOverMessage> OnGameOver;
    public event Action<ErrorMessage> OnErrorMessage;

    private ClientWebSocket socket;
    private CancellationTokenSource cancellation;
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();
    private bool quitting;

    public bool IsConnected
    {
        get { return socket != null && socket.State == WebSocketState.Open; }
    }

    /// <summary>
    /// 确保场景中存在 WebSocketClient。
    /// 如果美术或 UI 场景忘记放 Network 对象，也会自动创建，减少协作时的场景依赖问题。
    /// </summary>
    public static WebSocketClient EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        WebSocketClient existing = FindObjectOfType<WebSocketClient>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject holder = new GameObject("WebSocketClient");
        return holder.AddComponent<WebSocketClient>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // ClientWebSocket 的回调不保证在 Unity 主线程。
        // 所有 UI 更新、Instantiate、Destroy 都必须切回主线程执行。
        while (true)
        {
            Action action = null;

            lock (mainThreadActions)
            {
                if (mainThreadActions.Count > 0)
                {
                    action = mainThreadActions.Dequeue();
                }
            }

            if (action == null)
            {
                break;
            }

            action.Invoke();
        }
    }

    private async void OnApplicationQuit()
    {
        quitting = true;
        await DisconnectAsync("application_quit");
    }

    /// <summary>
    /// 给 Button.OnClick 这类 UnityEvent 使用的包装函数。
    /// 真正的异步连接逻辑在 ConnectAsync 里。
    /// </summary>
    public async void ConnectToServer(string wsUrl)
    {
        await ConnectAsync(wsUrl);
    }

    /// <summary>
    /// 连接 FastAPI 的 /ws 地址，例如 ws://127.0.0.1:8000/ws。
    /// 连接成功后触发 OnConnected，UI 可以在事件里显示“已连接”。
    /// </summary>
    public async Task ConnectAsync(string wsUrl)
    {
        if (IsConnected)
        {
            EnqueueMainThread(OnConnected);
            return;
        }

        await DisconnectAsync("reconnect");

        cancellation = new CancellationTokenSource();
        socket = new ClientWebSocket();

        try
        {
            await socket.ConnectAsync(new Uri(wsUrl), cancellation.Token);
            EnqueueMainThread(OnConnected);
            _ = ReceiveLoopAsync();
        }
        catch (Exception ex)
        {
            EnqueueError("connect_failed", ex.Message);
            CleanupSocket();
        }
    }

    /// <summary>
    /// 主动断开连接。
    /// 场景切换不需要调用它，因为本对象会 DontDestroyOnLoad。
    /// </summary>
    public async Task DisconnectAsync(string reason = "disconnect")
    {
        ClientWebSocket closingSocket = socket;
        bool hadSocket = closingSocket != null;

        try
        {
            if (cancellation != null && !cancellation.IsCancellationRequested)
            {
                cancellation.Cancel();
            }

            if (closingSocket != null && closingSocket.State == WebSocketState.Open)
            {
                await closingSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    reason,
                    CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("WebSocket disconnect warning: " + ex.Message);
        }
        finally
        {
            CleanupSocket();

            if (!quitting && hadSocket)
            {
                EnqueueMainThread(() => OnDisconnected?.Invoke(reason));
            }
        }
    }

    /// <summary>
    /// 发送登录消息。
    /// LoginUI 只需要传入玩家昵称，不需要知道 JSON 具体字段。
    /// </summary>
    public void SendLogin(string username)
    {
        LoginRequest request = new LoginRequest
        {
            username = username
        };

        _ = SendJsonAsync(request);
    }

    /// <summary>
    /// 发送开始游戏消息。
    /// 服务端应在收到该消息时读取 MySQL 最新配置并创建本局状态。
    /// </summary>
    public void SendStartGame(string username, int levelId)
    {
        StartGameRequest request = new StartGameRequest
        {
            username = username,
            level_id = levelId
        };

        _ = SendJsonAsync(request);
    }

    /// <summary>
    /// 发送建塔请求。
    /// tileId 负责逻辑定位，worldPosition 主要方便服务端回传或调试。
    /// </summary>
    public void SendBuildRequest(string gameId, int tileId, int towerId, Vector3 worldPosition)
    {
        BuildRequest request = new BuildRequest
        {
            game_id = gameId,
            tile_id = tileId,
            tower_id = towerId,
            x = worldPosition.x,
            y = worldPosition.y,
            z = worldPosition.z
        };

        _ = SendJsonAsync(request);
    }

    /// <summary>
    /// 把 C# 消息对象转成 JSON 并发送。
    /// 这里使用 UnityEngine.JsonUtility，避免引入 Newtonsoft.Json。
    /// </summary>
    public async Task SendJsonAsync<T>(T payload)
    {
        string json = JsonUtility.ToJson(payload);
        await SendTextAsync(json);
    }

    /// <summary>
    /// 发送原始 JSON 文本。
    /// 调试协议时可以直接调用它，但正式业务脚本建议调用 SendLogin/SendStartGame/SendBuildRequest。
    /// </summary>
    public async Task SendTextAsync(string json)
    {
        if (!IsConnected)
        {
            EnqueueError("not_connected", "WebSocket is not connected.");
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(json);
        ArraySegment<byte> segment = new ArraySegment<byte>(bytes);

        try
        {
            await socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellation.Token);
        }
        catch (Exception ex)
        {
            EnqueueError("send_failed", ex.Message);
        }
    }

    /// <summary>
    /// 持续接收服务端消息。
    /// WebSocket 一条消息可能分多段到达，所以这里用 MemoryStream 拼完整。
    /// </summary>
    private async Task ReceiveLoopAsync()
    {
        byte[] buffer = new byte[8192];

        try
        {
            while (socket != null && socket.State == WebSocketState.Open && !cancellation.IsCancellationRequested)
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            cancellation.Token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await DisconnectAsync("server_close");
                            return;
                        }

                        stream.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    string json = Encoding.UTF8.GetString(stream.ToArray());
                    EnqueueMainThread(() => DispatchMessage(json));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 主动断开时会进入这里，不需要提示错误。
        }
        catch (Exception ex)
        {
            EnqueueError("receive_failed", ex.Message);
        }
        finally
        {
            if (!quitting && !IsConnected)
            {
                EnqueueMainThread(() => OnDisconnected?.Invoke("receive_loop_end"));
            }
        }
    }

    /// <summary>
    /// 根据 type 字段把 JSON 分发成具体消息事件。
    /// 所有监听者都在 Unity 主线程收到事件，可以安全更新 UI 或场景对象。
    /// </summary>
    private void DispatchMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        OnRawMessage?.Invoke(json);

        BaseMessage baseMessage;

        try
        {
            baseMessage = JsonUtility.FromJson<BaseMessage>(json);
        }
        catch (Exception ex)
        {
            OnErrorMessage?.Invoke(new ErrorMessage
            {
                type = MessageTypes.Error,
                code = "invalid_json",
                message = ex.Message
            });
            return;
        }

        if (baseMessage == null || string.IsNullOrEmpty(baseMessage.type))
        {
            OnErrorMessage?.Invoke(new ErrorMessage
            {
                type = MessageTypes.Error,
                code = "missing_type",
                message = "Message does not contain type field."
            });
            return;
        }

        switch (baseMessage.type)
        {
            case MessageTypes.LoginResult:
                OnLoginResult?.Invoke(JsonUtility.FromJson<LoginResultMessage>(json));
                break;

            case MessageTypes.GameStart:
                OnGameStart?.Invoke(JsonUtility.FromJson<GameStartMessage>(json));
                break;

            case MessageTypes.StateUpdate:
                OnStateUpdate?.Invoke(JsonUtility.FromJson<StateUpdateMessage>(json));
                break;

            case MessageTypes.BuildResult:
                OnBuildResult?.Invoke(JsonUtility.FromJson<BuildResultMessage>(json));
                break;

            case MessageTypes.GameOver:
                OnGameOver?.Invoke(JsonUtility.FromJson<GameOverMessage>(json));
                break;

            case MessageTypes.Error:
                OnErrorMessage?.Invoke(JsonUtility.FromJson<ErrorMessage>(json));
                break;

            default:
                Debug.LogWarning("Unknown WebSocket message type: " + baseMessage.type + "\n" + json);
                break;
        }
    }

    private void EnqueueError(string code, string message)
    {
        EnqueueMainThread(() =>
        {
            OnErrorMessage?.Invoke(new ErrorMessage
            {
                type = MessageTypes.Error,
                code = code,
                message = message
            });
        });
    }

    private void EnqueueMainThread(Action action)
    {
        if (action == null)
        {
            return;
        }

        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    private void CleanupSocket()
    {
        if (socket != null)
        {
            socket.Dispose();
            socket = null;
        }

        if (cancellation != null)
        {
            cancellation.Dispose();
            cancellation = null;
        }
    }
}
