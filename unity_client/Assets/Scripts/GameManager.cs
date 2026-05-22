using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity 全局状态管理。
/// 当前 MVP 是单人游戏，所以只保存 username、game_id、level_id 等单局信息，
/// 不保存 room_code、player_list、host 等多人房间字段。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Server")]
    public string websocketUrl = "ws://127.0.0.1:8000/ws";

    [Header("Game")]
    public string username = "Player";
    public string gameId = "";
    public int levelId = 1;
    public int selectedTowerId = 1;
    public string battleSceneName = "BattleScene";
    public bool autoStartAfterLogin = true;

    public GameStartMessage LastGameStart { get; private set; }
    public StateUpdateMessage LastStateUpdate { get; private set; }
    public GameOverMessage LastGameOver { get; private set; }
    public bool IsGameRunning { get; private set; }

    private bool subscribed;

    /// <summary>
    /// 确保场景里有 GameManager。
    /// LoginScene 如果忘记放全局对象，也可以由 UI 脚本自动创建。
    /// </summary>
    public static GameManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameManager existing = FindObjectOfType<GameManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject holder = new GameObject("GameManager");
        return holder.AddComponent<GameManager>();
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
        WebSocketClient.EnsureInstance();
    }

    private void OnEnable()
    {
        SubscribeNetworkEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkEvents();
    }

    /// <summary>
    /// 设置玩家昵称。
    /// 空字符串会自动替换成 Player，避免后端写入 game_result 时 username 为空。
    /// </summary>
    public void SetUsername(string input)
    {
        username = string.IsNullOrWhiteSpace(input) ? "Player" : input.Trim();
    }

    /// <summary>
    /// 登录按钮建议调用这个函数。
    /// 流程：连接 /ws -> 发送 login -> 登录成功后自动 start_game。
    /// </summary>
    public async void ConnectLoginAndStart(string inputUsername)
    {
        SetUsername(inputUsername);

        WebSocketClient client = WebSocketClient.EnsureInstance();
        await client.ConnectAsync(websocketUrl);

        if (client.IsConnected)
        {
            client.SendLogin(username);
        }
    }

    /// <summary>
    /// 手动开始一局。
    /// 用于结算界面的“再来一局”，或调试时跳过登录直接开始。
    /// </summary>
    public void StartGame()
    {
        WebSocketClient client = WebSocketClient.EnsureInstance();

        if (!client.IsConnected)
        {
            Debug.LogWarning("Cannot start game: WebSocket is not connected.");
            return;
        }

        LastGameStart = null;
        LastStateUpdate = null;
        LastGameOver = null;
        IsGameRunning = false;

        client.SendStartGame(username, levelId);
    }

    /// <summary>
    /// 从 TileButton 调用，向服务端请求在某个地块建塔。
    /// 真正是否成功由服务端判断，例如金币是否足够、地块是否已占用。
    /// </summary>
    public void BuildTower(int tileId, Vector3 worldPosition, int towerIdOverride = -1)
    {
        if (string.IsNullOrEmpty(gameId))
        {
            Debug.LogWarning("Cannot build tower: gameId is empty. Start a game first.");
            return;
        }

        int towerId = towerIdOverride > 0 ? towerIdOverride : selectedTowerId;
        WebSocketClient.EnsureInstance().SendBuildRequest(gameId, tileId, towerId, worldPosition);
    }

    /// <summary>
    /// 切换当前选择的塔类型。
    /// 以后如果 UI 做多个塔按钮，可以直接调用这个函数。
    /// </summary>
    public void SelectTower(int towerId)
    {
        if (towerId <= 0)
        {
            Debug.LogWarning("towerId must be positive.");
            return;
        }

        selectedTowerId = towerId;
    }

    /// <summary>
    /// 结算后重新开始。
    /// 注意这会重新发送 start_game，因此会读取 Web 后台保存到 MySQL 的最新配置。
    /// </summary>
    public void RestartGame()
    {
        StartGame();
    }

    private void SubscribeNetworkEvents()
    {
        if (subscribed)
        {
            return;
        }

        WebSocketClient client = WebSocketClient.EnsureInstance();
        client.OnLoginResult += HandleLoginResult;
        client.OnGameStart += HandleGameStart;
        client.OnStateUpdate += HandleStateUpdate;
        client.OnGameOver += HandleGameOver;
        subscribed = true;
    }

    private void UnsubscribeNetworkEvents()
    {
        if (!subscribed || WebSocketClient.Instance == null)
        {
            return;
        }

        WebSocketClient client = WebSocketClient.Instance;
        client.OnLoginResult -= HandleLoginResult;
        client.OnGameStart -= HandleGameStart;
        client.OnStateUpdate -= HandleStateUpdate;
        client.OnGameOver -= HandleGameOver;
        subscribed = false;
    }

    private void HandleLoginResult(LoginResultMessage message)
    {
        if (message != null && message.success)
        {
            if (!string.IsNullOrEmpty(message.username))
            {
                username = message.username;
            }

            if (autoStartAfterLogin)
            {
                StartGame();
            }
        }
        else
        {
            string reason = message == null ? "empty login result" : message.message;
            Debug.LogWarning("Login failed: " + reason);
        }
    }

    private void HandleGameStart(GameStartMessage message)
    {
        if (message == null || !message.success)
        {
            Debug.LogWarning("Game start failed: " + (message == null ? "empty message" : message.message));
            return;
        }

        LastGameStart = message;
        LastGameOver = null;
        gameId = message.game_id;
        IsGameRunning = true;

        if (!string.IsNullOrEmpty(battleSceneName) &&
            SceneManager.GetActiveScene().name != battleSceneName)
        {
            SceneManager.LoadScene(battleSceneName);
        }
    }

    private void HandleStateUpdate(StateUpdateMessage message)
    {
        LastStateUpdate = message;

        if (message != null && !string.IsNullOrEmpty(message.game_id))
        {
            gameId = message.game_id;
        }
    }

    private void HandleGameOver(GameOverMessage message)
    {
        LastGameOver = message;
        IsGameRunning = false;

        if (message != null && !string.IsNullOrEmpty(message.game_id))
        {
            gameId = message.game_id;
        }
    }
}
