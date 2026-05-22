using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 登录界面逻辑。
/// MVP 流程：输入昵称 -> 连接 FastAPI /ws -> login -> start_game -> 进入 BattleScene。
/// 不包含创建房间、加入房间、显示玩家列表。
/// </summary>
public class LoginUI : MonoBehaviour
{
    public InputField usernameInput;
    public Text statusText;
    public Button startButton;

    private void Start()
    {
        GameManager.EnsureInstance();
        WebSocketClient.EnsureInstance();

        if (startButton != null)
        {
            startButton.onClick.AddListener(HandleStartButtonClicked);
        }

        SetStatus("请输入昵称后开始游戏");
    }

    private void OnEnable()
    {
        WebSocketClient client = WebSocketClient.EnsureInstance();
        client.OnConnected += HandleConnected;
        client.OnDisconnected += HandleDisconnected;
        client.OnLoginResult += HandleLoginResult;
        client.OnGameStart += HandleGameStart;
        client.OnErrorMessage += HandleError;
    }

    private void OnDisable()
    {
        if (WebSocketClient.Instance == null)
        {
            return;
        }

        WebSocketClient client = WebSocketClient.Instance;
        client.OnConnected -= HandleConnected;
        client.OnDisconnected -= HandleDisconnected;
        client.OnLoginResult -= HandleLoginResult;
        client.OnGameStart -= HandleGameStart;
        client.OnErrorMessage -= HandleError;
    }

    /// <summary>
    /// 绑定到“开始游戏”按钮。
    /// 这里不直接 LoadScene，而是等服务端返回 game_start 后由 GameManager 切换场景。
    /// </summary>
    public void HandleStartButtonClicked()
    {
        string inputName = usernameInput == null ? "" : usernameInput.text;
        SetInteractable(false);
        SetStatus("正在连接服务器...");
        GameManager.EnsureInstance().ConnectLoginAndStart(inputName);
    }

    private void HandleConnected()
    {
        SetStatus("已连接服务器，正在登录...");
    }

    private void HandleDisconnected(string reason)
    {
        SetInteractable(true);
        SetStatus("连接已断开：" + reason);
    }

    private void HandleLoginResult(LoginResultMessage message)
    {
        if (message != null && message.success)
        {
            SetStatus("登录成功，正在开始游戏...");
        }
        else
        {
            SetInteractable(true);
            SetStatus("登录失败：" + (message == null ? "无返回" : message.message));
        }
    }

    private void HandleGameStart(GameStartMessage message)
    {
        if (message != null && message.success)
        {
            SetStatus("游戏开始");
        }
        else
        {
            SetInteractable(true);
            SetStatus("开始游戏失败：" + (message == null ? "无返回" : message.message));
        }
    }

    private void HandleError(ErrorMessage message)
    {
        SetInteractable(true);
        SetStatus("错误：" + (message == null ? "未知错误" : message.message));
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    private void SetInteractable(bool interactable)
    {
        if (startButton != null)
        {
            startButton.interactable = interactable;
        }

        if (usernameInput != null)
        {
            usernameInput.interactable = interactable;
        }
    }
}
