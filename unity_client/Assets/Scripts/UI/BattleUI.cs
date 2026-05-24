using TMPro;
using UnityEngine;

public class BattleUI : MonoBehaviour
{
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI baseHpText;
    public TextMeshProUGUI killCountText;
    public TextMeshProUGUI gameTimeText;
    public TextMeshProUGUI messageText;
    public GameManager gameManager;

    private float currentGameTimeSec;

    private void Start()
    {
        ResolveReferences();
        SubscribeNetworkEvents();
        Refresh();
        ShowMessage("选择地块建造箭塔");
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkEvents();
    }

    public void Refresh()
    {
        ResolveReferences();

        int gold = gameManager != null ? gameManager.gold : 0;
        int score = gameManager != null ? gameManager.score : 0;
        int baseHp = gameManager != null ? gameManager.base_hp : 0;
        int killCount = gameManager != null ? gameManager.kill_count : 0;

        if (goldText != null)
        {
            goldText.text = "Gold: " + gold;
        }

        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }

        if (baseHpText != null)
        {
            baseHpText.text = "Base HP: " + baseHp;
        }

        if (killCountText != null)
        {
            killCountText.text = "Kills: " + killCount;
        }

        if (gameTimeText != null)
        {
            gameTimeText.text = "Time: " + currentGameTimeSec.ToString("0.0");
        }

        if (gameManager != null && gameManager.is_game_over)
        {
            ShowMessage("游戏已结束");
        }
    }

    public void SetGameTime(float gameTimeSec)
    {
        currentGameTimeSec = gameTimeSec;
        if (gameTimeText != null)
        {
            gameTimeText.text = "Time: " + currentGameTimeSec.ToString("0.0");
        }
    }

    public void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        Debug.Log("[BattleUI] " + message);
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
    }

    private void HandleGameOver(GameOverData data)
    {
        Refresh();
        ShowMessage("游戏已结束");
    }

    private void SubscribeNetworkEvents()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameOver -= HandleGameOver;
            NetworkManager.Instance.OnGameOver += HandleGameOver;
        }
    }

    private void UnsubscribeNetworkEvents()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnGameOver -= HandleGameOver;
        }
    }
}
