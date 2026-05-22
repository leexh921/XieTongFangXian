using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结算界面。
/// 收到 game_over 后显示结果；点击再来一局会重新发送 start_game，
/// 因此 Web 后台刚修改的配置会在下一局生效。
/// </summary>
public class ResultUI : MonoBehaviour
{
    public GameObject rootPanel;
    public Text resultText;
    public Text scoreText;
    public Text detailText;
    public Button restartButton;

    private void Start()
    {
        SetVisible(false);

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    private void OnEnable()
    {
        WebSocketClient.EnsureInstance().OnGameOver += ShowResult;
    }

    private void OnDisable()
    {
        if (WebSocketClient.Instance != null)
        {
            WebSocketClient.Instance.OnGameOver -= ShowResult;
        }
    }

    /// <summary>
    /// 根据服务端 game_over 消息显示结算。
    /// 成绩写库由服务端负责，Unity 这里只负责展示。
    /// </summary>
    public void ShowResult(GameOverMessage message)
    {
        if (message == null)
        {
            return;
        }

        SetVisible(true);
        SetText(resultText, message.win ? "胜利" : "失败");
        SetText(scoreText, "最终分数：" + message.score);
        SetText(detailText,
            "玩家：" + message.username +
            "\n金币：" + message.gold +
            "\n基地血量：" + message.base_hp +
            "\n用时：" + message.duration.ToString("0.0") + " 秒");
    }

    /// <summary>
    /// 再来一局。
    /// 不重新登录，不重新连接，只要求服务端创建新 game_id。
    /// </summary>
    public void RestartGame()
    {
        SetVisible(false);
        GameManager.EnsureInstance().RestartGame();
    }

    private void SetVisible(bool visible)
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(visible);
        }
    }

    private void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
