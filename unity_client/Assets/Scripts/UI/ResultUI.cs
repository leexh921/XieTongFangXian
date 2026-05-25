using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultUI : MonoBehaviour
{
    public GameObject resultPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI killCountText;
    public TextMeshProUGUI timeUsedText;
    public TextMeshProUGUI baseHpText;
    public Button restartButton;
    public Button backToLobbyButton;
    public GameManager gameManager;
    public NetworkManager networkManager;
    public string lobbySceneName = "LobbyScene";

    private void Start()
    {
        ResolveReferences();
        HideResultPanel();
        SubscribeNetworkEvents();
        BindButtons();
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkEvents();

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(ReturnToLobby);
        }

        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveListener(ReturnToLobby);
        }
    }

    public void ShowResult(GameOverData data)
    {
        ResolveReferences();

        if (data == null)
        {
            Debug.LogWarning("[ResultUI] Ignored null game_over data.");
            return;
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        PlayerStateData player = data.player;
        int score = player != null ? player.score : (gameManager != null ? gameManager.score : 0);
        int killCount = player != null ? player.kill_count : (gameManager != null ? gameManager.kill_count : 0);

        if (titleText != null)
        {
            titleText.text = data.is_win ? "胜利" : "失败";
        }

        if (scoreText != null)
        {
            scoreText.text = "得分: " + score;
        }

        if (killCountText != null)
        {
            killCountText.text = "击杀: " + killCount;
        }

        if (timeUsedText != null)
        {
            timeUsedText.text = "用时: " + data.time_used + " 秒";
        }

        if (baseHpText != null)
        {
            baseHpText.text = "基地生命: " + data.base_hp;
        }

        Debug.Log("[ResultUI] Show result. is_win=" + data.is_win
            + ", score=" + score
            + ", kill_count=" + killCount
            + ", time_used=" + data.time_used
            + ", base_hp=" + data.base_hp);
    }

    private void HideResultPanel()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void ReturnToLobby()
    {
        ResolveReferences();

        if (gameManager != null)
        {
            gameManager.ResetGameState();
        }

        if (!string.IsNullOrEmpty(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
    }

    private void BindButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(ReturnToLobby);
            restartButton.onClick.AddListener(ReturnToLobby);
        }

        if (backToLobbyButton != null)
        {
            backToLobbyButton.onClick.RemoveListener(ReturnToLobby);
            backToLobbyButton.onClick.AddListener(ReturnToLobby);
        }
    }

    private void HandleGameOver(GameOverData data)
    {
        ShowResult(data);
    }

    private void ResolveReferences()
    {
        if (GameManager.Instance != null)
        {
            gameManager = GameManager.Instance;
        }
        else if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (NetworkManager.Instance != null)
        {
            networkManager = NetworkManager.Instance;
        }
        else if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();
        }
    }

    private void SubscribeNetworkEvents()
    {
        if (networkManager == null)
        {
            ResolveReferences();
        }

        if (networkManager != null)
        {
            networkManager.OnGameOver -= HandleGameOver;
            networkManager.OnGameOver += HandleGameOver;
        }
    }

    private void UnsubscribeNetworkEvents()
    {
        if (networkManager != null)
        {
            networkManager.OnGameOver -= HandleGameOver;
        }
    }
}
