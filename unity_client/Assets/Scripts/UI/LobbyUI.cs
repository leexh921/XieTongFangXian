using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI playerIdText;
    public TextMeshProUGUI statusText;
    public Button startGameButton;
    public NetworkManager networkManager;
    public GameManager gameManager;
    public string battleSceneName = "BattleScene";
    public float loadBattleDelaySeconds = 0.5f;

    private void Start()
    {
        ResolveReferences();
        RefreshPlayerInfo();

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (networkManager != null)
        {
            SubscribeNetworkEvents();
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeNetworkEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkEvents();

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
        }
    }

    private void OnStartGameClicked()
    {
        ResolveReferences();
        RefreshPlayerInfo();

        if (!HasLoginInfo())
        {
            SetStatus("尚未登录");
            return;
        }

        if (networkManager == null)
        {
            SetStatus("未找到 NetworkManager");
            return;
        }

        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }

        SetStatus("正在开始游戏...");
        networkManager.StartGame(1);
    }

    private void HandleGameStart(GameStartData gameStart)
    {
        if (startGameButton != null)
        {
            startGameButton.interactable = true;
        }

        ResolveReferences();

        if (gameStart == null)
        {
            SetStatus("游戏配置加载失败：服务器返回为空");
            return;
        }

        int gameId = gameManager != null ? gameManager.game_id : 0;
        if (gameId <= 0 && GameManager.Instance != null)
        {
            gameId = GameManager.Instance.game_id;
        }

        int initialGold = gameStart.level != null ? gameStart.level.initial_gold : 0;
        int baseHp = gameStart.base_hp;
        int towerCount = gameStart.tower_config != null ? gameStart.tower_config.Count : 0;

        RefreshPlayerInfo();
        SetStatus("游戏配置加载成功"
            + "\ngame_id: " + gameId
            + "\n初始金币: " + initialGold
            + "\n基地血量: " + baseHp
            + "\n可用塔数量: " + towerCount);

        if (gameId > 0)
        {
            StartCoroutine(LoadBattleAfterDelay());
        }
    }

    private void RefreshPlayerInfo()
    {
        if (gameManager == null)
        {
            ResolveReferences();
        }

        string username = HasLoginInfo() ? gameManager.username : "--";
        int playerId = HasLoginInfo() ? gameManager.player_id : 0;

        if (usernameText != null)
        {
            usernameText.text = "username: " + username;
        }

        if (playerIdText != null)
        {
            playerIdText.text = "player_id: " + playerId;
        }

        if (!HasLoginInfo())
        {
            SetStatus("尚未登录，请返回登录界面");
        }
        else if (statusText != null && string.IsNullOrEmpty(statusText.text))
        {
            SetStatus("已登录，可以开始游戏");
        }
    }

    private bool HasLoginInfo()
    {
        return gameManager != null
            && gameManager.player_id > 0
            && !string.IsNullOrEmpty(gameManager.username);
    }

    private void ResolveReferences()
    {
        if (NetworkManager.Instance != null)
        {
            networkManager = NetworkManager.Instance;
        }
        else if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();
        }

        if (GameManager.Instance != null)
        {
            gameManager = GameManager.Instance;
        }
        else if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    private void SubscribeNetworkEvents()
    {
        if (networkManager == null)
        {
            return;
        }

        networkManager.OnGameStart -= HandleGameStart;
        networkManager.OnGameStart += HandleGameStart;
    }

    private void UnsubscribeNetworkEvents()
    {
        if (networkManager != null)
        {
            networkManager.OnGameStart -= HandleGameStart;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log("[LobbyUI] " + message);
    }

    private IEnumerator LoadBattleAfterDelay()
    {
        yield return new WaitForSeconds(loadBattleDelaySeconds);

        if (!string.IsNullOrEmpty(battleSceneName))
        {
            SceneManager.LoadScene(battleSceneName);
        }
    }
}
