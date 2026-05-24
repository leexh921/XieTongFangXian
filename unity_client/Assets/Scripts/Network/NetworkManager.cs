using System;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    public event Action<LoginResultData> OnLoginResult;
    public event Action<GameStartData> OnGameStart;
    public event Action<BuildResultData> OnBuildResult;

    [Header("Connection")]
    public bool use_mock_server = true;
    public string server_url = "ws://192.168.221.81:8765/ws";
    public bool IsLoggedIn { get; private set; }
    public string LastStatusMessage { get; private set; }

    private MockServerClient mockServerClient;
    private bool isConnected;
    private int requestCounter;

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
    }

    private void OnEnable()
    {
        EnsureMockServerClient();
        SubscribeMockEvents();
    }

    private void OnDisable()
    {
        UnsubscribeMockEvents();
    }

    public void Connect()
    {
        if (use_mock_server)
        {
            EnsureMockServerClient();
            isConnected = true;
            LastStatusMessage = "Mock mode started.";
            Debug.Log("[NetworkManager] Mock mode started. No real WebSocket connection will be opened.");
            return;
        }

        isConnected = false;
        LastStatusMessage = "Real WebSocket not implemented yet.";
        Debug.LogWarning("[NetworkManager] Real WebSocket not implemented yet. Target URL: " + server_url);
    }

    public void Login(string username)
    {
        if (!isConnected)
        {
            Connect();
        }

        var request = new ProtocolMessage<LoginRequestData>
        {
            type = MessageTypes.LoginRequest,
            request_id = NextRequestId(),
            timestamp = CurrentTimestampMilliseconds(),
            data = new LoginRequestData
            {
                username = username
            }
        };

        if (use_mock_server)
        {
            LastStatusMessage = "Sending login_request to mock.";
            Debug.Log("[NetworkManager] Send login_request to mock: " + JsonUtility.ToJson(request));
            mockServerClient.SendLoginRequest(request);
            return;
        }

        LastStatusMessage = "Real WebSocket not implemented yet.";
        Debug.LogWarning("[NetworkManager] Real WebSocket not implemented yet. login_request was not sent.");
    }

    public void StartGame(int levelId)
    {
        if (!isConnected)
        {
            Connect();
        }

        int playerId = GameManager.Instance != null ? GameManager.Instance.player_id : 0;

        var request = new ProtocolMessage<StartGameRequestData>
        {
            type = MessageTypes.StartGameRequest,
            request_id = NextRequestId(),
            player_id = playerId,
            timestamp = CurrentTimestampMilliseconds(),
            data = new StartGameRequestData
            {
                level_id = levelId
            }
        };

        if (use_mock_server)
        {
            LastStatusMessage = "Sending start_game_request to mock.";
            Debug.Log("[NetworkManager] Send start_game_request to mock: " + JsonUtility.ToJson(request));
            mockServerClient.SendStartGameRequest(request);
            return;
        }

        LastStatusMessage = "Real WebSocket not implemented yet.";
        Debug.LogWarning("[NetworkManager] Real WebSocket not implemented yet. start_game_request was not sent.");
    }

    public void SendBuildRequest(int towerId, int gridX, int gridY)
    {
        if (!isConnected)
        {
            Connect();
        }

        int playerId = GameManager.Instance != null ? GameManager.Instance.player_id : 0;
        int gameId = GameManager.Instance != null ? GameManager.Instance.game_id : 0;

        var request = new ProtocolMessage<BuildRequestData>
        {
            type = MessageTypes.BuildRequest,
            request_id = NextRequestId(),
            game_id = gameId,
            player_id = playerId,
            timestamp = CurrentTimestampMilliseconds(),
            data = new BuildRequestData
            {
                tower_id = towerId,
                grid_x = gridX,
                grid_y = gridY
            }
        };

        if (use_mock_server)
        {
            LastStatusMessage = "Sending build_request to mock.";
            Debug.Log("[NetworkManager] Send build_request to mock: " + JsonUtility.ToJson(request));
            mockServerClient.SendBuildRequest(request);
            return;
        }

        LastStatusMessage = "Real WebSocket not implemented yet.";
        Debug.LogWarning("[NetworkManager] Real WebSocket not implemented yet. build_request was not sent.");
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

    private void HandleMockLoginResult(ProtocolMessage<LoginResultData> message)
    {
        if (message == null)
        {
            Debug.LogWarning("[NetworkManager] Ignored null login_result.");
            return;
        }

        Debug.Log("[NetworkManager] Received mock login_result: " + JsonUtility.ToJson(message));

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetLoginResult(message.data);
        }
        else
        {
            Debug.LogWarning("[NetworkManager] GameManager.Instance is missing. Login state was not saved.");
        }

        IsLoggedIn = message.data != null && message.data.success;
        LastStatusMessage = message.data != null ? message.data.message : "login_result data is null";
        OnLoginResult?.Invoke(message.data);
    }

    private void HandleMockGameStart(ProtocolMessage<GameStartData> message)
    {
        if (message == null)
        {
            Debug.LogWarning("[NetworkManager] Ignored null game_start.");
            return;
        }

        Debug.Log("[NetworkManager] Received mock game_start: " + JsonUtility.ToJson(message));

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameStart(message.game_id, message.data);
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

        LastStatusMessage = "game_start received.";
        Debug.Log("[NetworkManager] game_start applied. game_id=" + message.game_id
            + ", initial_gold=" + initialGold
            + ", base_hp=" + baseHp
            + ", tower_config_count=" + towerConfigCount);
        OnGameStart?.Invoke(message.data);
    }

    private void HandleMockBuildResult(ProtocolMessage<BuildResultData> message)
    {
        if (message == null)
        {
            Debug.LogWarning("[NetworkManager] Ignored null build_result.");
            return;
        }

        Debug.Log("[NetworkManager] Received mock build_result: " + JsonUtility.ToJson(message));

        if (message.data != null && message.data.player != null && GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePlayerState(message.data.player, GameManager.Instance.base_hp);
        }

        LastStatusMessage = message.data != null && message.data.success
            ? "build_result success."
            : "build_result failed.";
        OnBuildResult?.Invoke(message.data);
    }

    private void HandleMockStateUpdate(ProtocolMessage<StateUpdateData> message)
    {
        Debug.Log("[NetworkManager] Received mock state_update placeholder: " + JsonUtility.ToJson(message));

        if (message != null && message.data != null && GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePlayerState(message.data.player, message.data.base_hp);
        }
    }

    private void HandleMockGameOver(ProtocolMessage<GameOverData> message)
    {
        Debug.Log("[NetworkManager] Received mock game_over placeholder: " + JsonUtility.ToJson(message));

        if (message != null && GameManager.Instance != null)
        {
            GameManager.Instance.SetGameOver(message.data);
        }
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
