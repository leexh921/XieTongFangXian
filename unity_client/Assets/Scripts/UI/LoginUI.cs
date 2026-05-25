using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public Button loginButton;
    public TextMeshProUGUI statusText;
    public NetworkManager networkManager;
    public GameManager gameManager;
    public string lobbySceneName = "LobbyScene";
    public float loadLobbyDelaySeconds = 0.5f;

    private void Start()
    {
        ResolveReferences();

        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnLoginClicked);
            loginButton.onClick.AddListener(OnLoginClicked);
        }

        if (networkManager != null)
        {
            networkManager.OnLoginResult -= HandleLoginResult;
            networkManager.OnLoginResult += HandleLoginResult;
            networkManager.OnStatusMessage -= HandleNetworkStatus;
            networkManager.OnStatusMessage += HandleNetworkStatus;
        }

        SetStatus("请输入昵称");
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnLoginResult -= HandleLoginResult;
            networkManager.OnStatusMessage -= HandleNetworkStatus;
        }

        if (loginButton != null)
        {
            loginButton.onClick.RemoveListener(OnLoginClicked);
        }
    }

    private void OnLoginClicked()
    {
        ResolveReferences();

        if (networkManager == null)
        {
            SetStatus("未找到 NetworkManager");
            return;
        }

        if (usernameInput == null)
        {
            SetStatus("未绑定昵称输入框");
            return;
        }

        string username = usernameInput.text.Trim();
        if (string.IsNullOrEmpty(username))
        {
            SetStatus("请输入昵称");
            return;
        }

        if (loginButton != null)
        {
            loginButton.interactable = false;
        }

        SetStatus("正在连接服务器...");
        networkManager.Connect();
        networkManager.Login(username);
    }

    private void HandleLoginResult(LoginResultData loginResult)
    {
        if (loginButton != null)
        {
            loginButton.interactable = true;
        }

        if (loginResult == null)
        {
            SetStatus("登录失败：服务器返回为空");
            return;
        }

        ResolveReferences();

        if (!loginResult.success)
        {
            SetStatus("登录失败：" + loginResult.message);
            return;
        }

        int playerId = gameManager != null ? gameManager.player_id : loginResult.player_id;
        string username = gameManager != null ? gameManager.username : loginResult.username;

        SetStatus("登录成功\nplayer_id: " + playerId + "\nusername: " + username);
        StartCoroutine(LoadLobbyAfterDelay());
    }

    private void HandleNetworkStatus(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        SetStatus(message);

        if (loginButton != null
            && networkManager != null
            && !networkManager.use_mock_server
            && IsFailureStatus(message))
        {
            loginButton.interactable = true;
        }
    }

    private void ResolveReferences()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.Instance != null ? NetworkManager.Instance : FindObjectOfType<NetworkManager>();
        }

        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }

        Debug.Log("[LoginUI] " + message);
    }

    private bool IsFailureStatus(string message)
    {
        return message.IndexOf("failed", System.StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf("error", System.StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf("not connected", System.StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf("closed", System.StringComparison.OrdinalIgnoreCase) >= 0
            || message.Contains("失败")
            || message.Contains("未连接");
    }

    private IEnumerator LoadLobbyAfterDelay()
    {
        yield return new WaitForSeconds(loadLobbyDelaySeconds);

        if (!string.IsNullOrEmpty(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
    }
}
