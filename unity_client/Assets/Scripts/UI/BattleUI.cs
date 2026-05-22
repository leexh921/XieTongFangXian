using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗界面 UI。
/// 它只展示服务端 state_update 中的金币、分数、基地血量、波次等信息。
/// 不在客户端自行计算金币或胜负，避免和服务端状态不一致。
/// </summary>
public class BattleUI : MonoBehaviour
{
    public Text goldText;
    public Text scoreText;
    public Text baseHpText;
    public Text waveText;
    public Text messageText;

    private void Start()
    {
        GameManager.EnsureInstance();
        WebSocketClient.EnsureInstance();

        if (GameManager.Instance.LastStateUpdate != null)
        {
            ApplyState(GameManager.Instance.LastStateUpdate);
        }
    }

    private void OnEnable()
    {
        WebSocketClient client = WebSocketClient.EnsureInstance();
        client.OnStateUpdate += ApplyState;
        client.OnBuildResult += HandleBuildResult;
        client.OnGameOver += HandleGameOver;
        client.OnErrorMessage += HandleError;
    }

    private void OnDisable()
    {
        if (WebSocketClient.Instance == null)
        {
            return;
        }

        WebSocketClient client = WebSocketClient.Instance;
        client.OnStateUpdate -= ApplyState;
        client.OnBuildResult -= HandleBuildResult;
        client.OnGameOver -= HandleGameOver;
        client.OnErrorMessage -= HandleError;
    }

    /// <summary>
    /// 根据服务端状态刷新 UI。
    /// 如果 player 为空，说明服务端暂时没有发完整状态，UI 保持当前显示。
    /// </summary>
    public void ApplyState(StateUpdateMessage message)
    {
        if (message == null || message.state == null || message.state.player == null)
        {
            return;
        }

        PlayerState player = message.state.player;
        SetText(goldText, "金币：" + player.gold);
        SetText(scoreText, "分数：" + player.score);
        SetText(baseHpText, "基地血量：" + player.base_hp);
        SetText(waveText, "波次：" + message.state.wave_index);
    }

    private void HandleBuildResult(BuildResultMessage message)
    {
        if (message == null)
        {
            return;
        }

        SetText(messageText, message.success ? "建塔成功" : "建塔失败：" + message.message);
    }

    private void HandleGameOver(GameOverMessage message)
    {
        if (message == null)
        {
            return;
        }

        string result = message.win ? "胜利" : "失败";
        SetText(messageText, result + "  分数：" + message.score);
    }

    private void HandleError(ErrorMessage message)
    {
        SetText(messageText, "错误：" + (message == null ? "未知错误" : message.message));
    }

    private void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
