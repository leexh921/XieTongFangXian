using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    public event Action<LoginResultData> OnLoginResult;
    public event Action<GameStartData> OnGameStart;
    public event Action<BuildResultData> OnBuildResult;
    public event Action<StateUpdateData> OnStateUpdate;
    public event Action<GameOverData> OnGameOver;
    public event Action<string> OnStatusMessage;

    [Header("Connection")]
    public bool use_mock_server = true;
    public string server_url = "ws://192.168.221.81:8765/ws";
    public bool IsLoggedIn { get; private set; }
    public bool IsConnected { get { return isConnected; } }
    public string LastStatusMessage { get; private set; }

    private MockServerClient mockServerClient;
    private WebSocketClient webSocketClient;
    private bool isConnected;
    private int requestCounter;
    private float nextStateUpdateLogTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureMockServerClient();
        ResolveWebSocketClient(true);
    }

    private void OnEnable()
    {
        EnsureMockServerClient();
        ResolveWebSocketClient(true);
        SubscribeMockEvents();
        SubscribeWebSocketEvents();
    }

    private void OnDisable()
    {
        UnsubscribeMockEvents();
        UnsubscribeWebSocketEvents();
    }

    private void OnDestroy()
    {
        if (webSocketClient != null)
        {
            webSocketClient.Close();
        }
    }

    public void Connect()
    {
        if (use_mock_server)
        {
            EnsureMockServerClient();
            isConnected = true;
            SetStatus("Mock mode started.");
            Debug.Log("[NetworkManager] Mock mode started. No real WebSocket connection will be opened.");
            return;
        }

        ResolveWebSocketClient(true);
        if (webSocketClient == null)
        {
            isConnected = false;
            SetStatus("WebSocketClient is missing.");
            Debug.LogWarning("[NetworkManager] WebSocketClient is missing.");
            return;
        }

        if (webSocketClient.IsConnected)
        {
            isConnected = true;
            SetStatus("WebSocket already connected.");
            return;
        }

        isConnected = false;
        SetStatus("Connecting WebSocket server...");
        Debug.Log("[NetworkManager] Connecting WebSocket server: " + server_url);
        webSocketClient.Connect(server_url);
    }

    public void Login(string username)
    {
        var request = CreateRequest(MessageTypes.LoginRequest, new LoginRequestData
        {
            username = username
        });

        if (use_mock_server)
        {
            if (!isConnected)
            {
                Connect();
            }

            SetStatus("Sending login_request to mock.");
            Debug.Log("[NetworkManager] Send login_request to mock: " + JsonUtility.ToJson(request));
            mockServerClient.SendLoginRequest(request);
            return;
        }

        SendWebSocketMessage(request, MessageTypes.LoginRequest);
    }

    public void StartGame(int levelId)
    {
        var request = CreateRequest(MessageTypes.StartGameRequest, new StartGameRequestData
        {
            level_id = levelId
        });

        if (use_mock_server)
        {
            if (!isConnected)
            {
                Connect();
            }

            SetStatus("Sending start_game_request to mock.");
            Debug.Log("[NetworkManager] Send start_game_request to mock: " + JsonUtility.ToJson(request));
            mockServerClient.SendStartGameRequest(request);
            return;
        }

        SendWebSocketMessage(request, MessageTypes.StartGameRequest);
    }

    public void SendBuildRequest(int towerId, int gridX, int gridY)
    {
        var request = CreateRequest(MessageTypes.BuildRequest, new BuildRequestData
        {
            tower_id = towerId,
            grid_x = gridX,
            grid_y = gridY
        });

        if (use_mock_server)
        {
            if (!isConnected)
            {
                Connect();
            }

            SetStatus("Sending build_request to mock.");
            Debug.Log("[NetworkManager] Send build_request to mock: " + JsonUtility.ToJson(request));
            mockServerClient.SendBuildRequest(request);
            return;
        }

        SendWebSocketMessage(request, MessageTypes.BuildRequest);
    }

    [ContextMenu("Mock Test Login")]
    private void MockTestLogin()
    {
        use_mock_server = true;
        Connect();
        Login("MockPlayer");
    }

    [ContextMenu("Mock Test Start Game")]
    private void MockTestStartGame()
    {
        use_mock_server = true;
        Connect();
        StartGame(1);
    }

    private ProtocolMessage<TData> CreateRequest<TData>(string type, TData data)
    {
        GameManager gameManager = GetGameManager();
        int playerId = gameManager != null ? gameManager.player_id : 0;
        int gameId = gameManager != null ? gameManager.game_id : 0;

        return new ProtocolMessage<TData>
        {
            type = type,
            request_id = NextRequestId(),
            game_id = gameId,
            player_id = playerId,
            timestamp = CurrentTimestampMilliseconds(),
            data = data
        };
    }

    private void SendWebSocketMessage<TData>(ProtocolMessage<TData> request, string messageType)
    {
        ResolveWebSocketClient(true);

        if (webSocketClient == null)
        {
            SetStatus("WebSocketClient is missing.");
            Debug.LogWarning("[NetworkManager] WebSocketClient is missing. " + messageType + " was not sent.");
            return;
        }

        if (!webSocketClient.IsConnected && !webSocketClient.IsConnecting)
        {
            Connect();
        }

        string json = JsonConvert.SerializeObject(request);
        SetStatus("Sending " + messageType + " to WebSocket.");
        Debug.Log("[NetworkManager] Send " + messageType + " to WebSocket: " + json);
        webSocketClient.Send(json);
    }

    private void HandleWebSocketMessage(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[NetworkManager] Ignored empty WebSocket message.");
            return;
        }

        Debug.Log("[NetworkManager] Received WebSocket message: " + json);

        try
        {
            JObject envelope = JObject.Parse(json);
            string messageType = envelope.Value<string>("type");
            if (string.IsNullOrEmpty(messageType))
            {
                HandleError("Invalid message: missing type.");
                return;
            }

            switch (messageType)
            {
                case MessageTypes.LoginResult:
                    HandleLoginResult(envelope.ToObject<ProtocolMessage<LoginResultData>>());
                    break;
                case MessageTypes.GameStart:
                    HandleGameStart(envelope.ToObject<ProtocolMessage<GameStartData>>());
                    break;
                case MessageTypes.StateUpdate:
                    HandleStateUpdate(envelope.ToObject<ProtocolMessage<StateUpdateData>>());
                    break;
                case MessageTypes.BuildResult:
                    HandleBuildResult(envelope.ToObject<ProtocolMessage<BuildResultData>>());
                    break;
                case MessageTypes.GameOver:
                    HandleGameOver(envelope.ToObject<ProtocolMessage<GameOverData>>());
                    break;
                case MessageTypes.Error:
                    HandleError(envelope.ToObject<ProtocolMessage<ErrorData>>());
                    break;
                default:
                    HandleError("Unknown message type: " + messageType);
                    break;
            }
        }
        catch (Exception exception)
        {
            HandleError("Failed to parse WebSocket message: " + exception.Message);
            Debug.LogWarning("[NetworkManager] Parse WebSocket message failed: " + exception);
        }
    }

    private void HandleMockLoginResult(ProtocolMessage<LoginResultData> message)
    {
        Debug.Log("[NetworkManager] Received mock login_result: " + JsonUtility.ToJson(message));
        HandleLoginResult(message);
    }

    private void HandleMockGameStart(ProtocolMessage<GameStartData> message)
    {
        Debug.Log("[NetworkManager] Received mock game_start: " + JsonUtility.ToJson(message));
        HandleGameStart(message);
    }

    private void HandleMockBuildResult(ProtocolMessage<BuildResultData> message)
    {
        Debug.Log("[NetworkManager] Received mock build_result: " + JsonUtility.ToJson(message));
        HandleBuildResult(message);
    }

    private void HandleMockStateUpdate(ProtocolMessage<StateUpdateData> message)
    {
        HandleStateUpdate(message);
    }

    private void HandleMockGameOver(ProtocolMessage<GameOverData> message)
    {
        Debug.Log("[NetworkManager] Received mock game_over: " + JsonUtility.ToJson(message));
        HandleGameOver(message);
    }

    private void HandleLoginResult(ProtocolMessage<LoginResultData> message)
    {
        if (message == null)
        {
            Debug.LogWarning("[NetworkManager] Ignored null login_result.");
            return;
        }

        GameManager gameManager = GetGameManager();
        if (gameManager != null)
        {
            gameManager.SetLoginResult(message.data);
        }
        else
        {
            Debug.LogWarning("[NetworkManager] GameManager.Instance is missing. Login state was not saved.");
        }

        IsLoggedIn = message.data != null && message.data.success;
        SetStatus(message.data != null ? message.data.message : "login_result data is null");
        OnLoginResult?.Invoke(message.data);
    }

    private void HandleGameStart(ProtocolMessage<GameStartData> message)
    {
        if (message == null)
        {
            Debug.LogWarning("[NetworkManager] Ignored null game_start.");
            return;
        }

        GameManager gameManager = GetGameManager();
        if (gameManager != null)
        {
            gameManager.SetGameStart(message.game_id, message.data);
        }
        else
        {
            Debug.LogWarning("[NetworkManager] GameManager.Instance is missing. Game state was not saved.");
        }

        int initialGold = 0;
        int baseHp = 0;
        int towerConfigCount = 0;
        if (message.data != null)
        {
            initialGold = message.data.level != null ? message.data.level.initial_gold : 0;
            baseHp = message.data.base_hp;
            towerConfigCount = message.data.tower_config != null ? message.data.tower_config.Count : 0;
        }

        SetStatus("game_start received.");
        Debug.Log("[NetworkManager] game_start applied. game_id=" + message.game_id
            + ", initial_gold=" + initialGold
            + ", base_hp=" + baseHp
            + ", tower_config_count=" + towerConfigCount);
        OnGameStart?.Invoke(message.data);
    }

    private void HandleBuildResult(ProtocolMessage<BuildResultData> message)
    {
        if (message == null)
        {
            Debug.LogWarning("[NetworkManager] Ignored null build_result.");
            return;
        }

        GameManager gameManager = GetGameManager();
        if (message.data != null && message.data.player != null && gameManager != null)
        {
            gameManager.UpdatePlayerState(message.data.player, gameManager.base_hp);
        }

        SetStatus(message.data != null && message.data.success
            ? "build_result success."
            : "build_result failed.");

        if (message.data != null)
        {
            string instanceId = message.data.tower != null ? message.data.tower.instance_id : string.Empty;
            int gridX = message.data.tower != null ? message.data.tower.grid_x : 0;
            int gridY = message.data.tower != null ? message.data.tower.grid_y : 0;
            int gold = message.data.player != null ? message.data.player.gold : 0;
            Debug.Log("[NetworkManager] build_result applied. success=" + message.data.success
                + ", reason=" + message.data.reason
                + ", tower=" + instanceId
                + ", grid=" + gridX + "," + gridY
                + ", gold=" + gold);
        }

        OnBuildResult?.Invoke(message.data);
    }

    private void HandleStateUpdate(ProtocolMessage<StateUpdateData> message)
    {
        if (message == null)
        {
            Debug.LogWarning("[NetworkManager] Ignored null state_update.");
            return;
        }

        GameManager gameManager = GetGameManager();
        if (message.data != null && gameManager != null)
        {
            if (message.game_id > 0)
            {
                gameManager.game_id = message.game_id;
            }

            gameManager.UpdatePlayerState(message.data.player, message.data.base_hp);
        }

        int monsterCount = message.data != null && message.data.monsters != null ? message.data.monsters.Count : 0;
        int gold = message.data != null && message.data.player != null ? message.data.player.gold : 0;
        int score = message.data != null && message.data.player != null ? message.data.player.score : 0;
        int baseHp = message.data != null ? message.data.base_hp : 0;
        float gameTime = message.data != null ? message.data.game_time_sec : 0f;
        if (message.data == null || gameTime >= nextStateUpdateLogTime)
        {
            Debug.Log("[NetworkManager] state_update received. monsters=" + monsterCount
                + ", gold=" + gold
                + ", score=" + score
                + ", base_hp=" + baseHp);
            nextStateUpdateLogTime = gameTime + 1f;
        }

        OnStateUpdate?.Invoke(message.data);
    }

    private void HandleGameOver(ProtocolMessage<GameOverData> message)
    {
        if (message == null)
        {
            Debug.LogWarning("[NetworkManager] Ignored null game_over.");
            return;
        }

        GameManager gameManager = GetGameManager();
        if (gameManager != null)
        {
            gameManager.SetGameOver(message.data);
        }

        SetStatus("game_over received.");

        if (message.data != null)
        {
            int score = message.data.player != null ? message.data.player.score : 0;
            int killCount = message.data.player != null ? message.data.player.kill_count : 0;
            Debug.Log("[NetworkManager] game_over applied. is_win=" + message.data.is_win
                + ", score=" + score
                + ", kill_count=" + killCount
                + ", time_used=" + message.data.time_used
                + ", base_hp=" + message.data.base_hp);
        }

        OnGameOver?.Invoke(message.data);
    }

    private void HandleError(ProtocolMessage<ErrorData> message)
    {
        if (message == null || message.data == null)
        {
            HandleError("Server returned an empty error message.");
            return;
        }

        string text = string.IsNullOrEmpty(message.data.code)
            ? message.data.message
            : message.data.code + ": " + message.data.message;
        HandleError(text);
    }

    private void HandleError(string message)
    {
        string text = string.IsNullOrEmpty(message) ? "Unknown network error." : message;
        SetStatus(text);
        Debug.LogWarning("[NetworkManager] " + text);
    }

    private void HandleWebSocketConnected()
    {
        isConnected = true;
        SetStatus("WebSocket connected.");
        Debug.Log("[NetworkManager] WebSocket connected.");
    }

    private void HandleWebSocketClosed()
    {
        isConnected = false;

        if (string.IsNullOrEmpty(LastStatusMessage) || LastStatusMessage.IndexOf("failed", StringComparison.OrdinalIgnoreCase) < 0)
        {
            SetStatus("WebSocket closed.");
        }

        Debug.Log("[NetworkManager] WebSocket closed.");
    }

    private void HandleWebSocketError(string message)
    {
        isConnected = false;
        HandleError(message);
    }

    private GameManager GetGameManager()
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance;
        }

        return FindObjectOfType<GameManager>();
    }

    private void EnsureMockServerClient()
    {
        if (mockServerClient != null)
        {
            return;
        }

        mockServerClient = GetComponent<MockServerClient>();
        if (mockServerClient == null)
        {
            mockServerClient = gameObject.AddComponent<MockServerClient>();
        }
    }

    private void ResolveWebSocketClient(bool createIfMissing)
    {
        if (webSocketClient != null)
        {
            return;
        }

        webSocketClient = GetComponent<WebSocketClient>();
        if (webSocketClient == null && createIfMissing)
        {
            webSocketClient = gameObject.AddComponent<WebSocketClient>();
        }
    }

    private void SubscribeMockEvents()
    {
        if (mockServerClient == null)
        {
            return;
        }

        UnsubscribeMockEvents();
        mockServerClient.LoginResultReceived += HandleMockLoginResult;
        mockServerClient.GameStartReceived += HandleMockGameStart;
        mockServerClient.BuildResultReceived += HandleMockBuildResult;
        mockServerClient.StateUpdateReceived += HandleMockStateUpdate;
        mockServerClient.GameOverReceived += HandleMockGameOver;
    }

    private void UnsubscribeMockEvents()
    {
        if (mockServerClient == null)
        {
            return;
        }

        mockServerClient.LoginResultReceived -= HandleMockLoginResult;
        mockServerClient.GameStartReceived -= HandleMockGameStart;
        mockServerClient.BuildResultReceived -= HandleMockBuildResult;
        mockServerClient.StateUpdateReceived -= HandleMockStateUpdate;
        mockServerClient.GameOverReceived -= HandleMockGameOver;
    }

    private void SubscribeWebSocketEvents()
    {
        if (webSocketClient == null)
        {
            return;
        }

        UnsubscribeWebSocketEvents();
        webSocketClient.Connected += HandleWebSocketConnected;
        webSocketClient.Closed += HandleWebSocketClosed;
        webSocketClient.MessageReceived += HandleWebSocketMessage;
        webSocketClient.ErrorReceived += HandleWebSocketError;
    }

    private void UnsubscribeWebSocketEvents()
    {
        if (webSocketClient == null)
        {
            return;
        }

        webSocketClient.Connected -= HandleWebSocketConnected;
        webSocketClient.Closed -= HandleWebSocketClosed;
        webSocketClient.MessageReceived -= HandleWebSocketMessage;
        webSocketClient.ErrorReceived -= HandleWebSocketError;
    }

    private void SetStatus(string message)
    {
        LastStatusMessage = message;
        OnStatusMessage?.Invoke(message);
    }

    private string NextRequestId()
    {
        requestCounter++;
        return "req_" + requestCounter.ToString("000");
    }

    private static long CurrentTimestampMilliseconds()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }
}
