using TMPro;
using UnityEngine;

public class BattleUI : MonoBehaviour
{
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI baseHpText;
    public TextMeshProUGUI messageText;
    public GameManager gameManager;

    private void Start()
    {
        ResolveReferences();
        Refresh();
        ShowMessage("选择地块建造箭塔");
    }

    public void Refresh()
    {
        ResolveReferences();

        int gold = gameManager != null ? gameManager.gold : 0;
        int score = gameManager != null ? gameManager.score : 0;
        int baseHp = gameManager != null ? gameManager.base_hp : 0;

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
        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        }
    }
}
