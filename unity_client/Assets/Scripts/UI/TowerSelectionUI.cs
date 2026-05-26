using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerSelectionUI : MonoBehaviour
{
    public GameObject popupRoot;
    public Transform towerCardRoot;
    public GameObject towerCardPrefab;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI selectedTileText;
    public TextMeshProUGUI messageText;
    public Button closeButton;
    public BattleUI battleUI;
    public GameManager gameManager;
    public NetworkManager networkManager;
    public RectTransform popupRect;
    public Camera mainCamera;

    private int pendingGridX;
    private int pendingGridY;
    private bool hasPendingTile;

    private readonly List<TowerCardUI> cards =
        new List<TowerCardUI>();

    private void Start()
    {
        ResolveReferences();

        EnsureRuntimeLayout();

        SubscribeNetworkEvents();

        Hide();
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkEvents();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
        }
    }

    public static TowerSelectionUI FindOrCreate()
    {
        TowerSelectionUI existing =
            FindExistingInScene();

        if (existing != null)
        {
            return existing;
        }

        Canvas canvas =
            FindObjectOfType<Canvas>();

        if (canvas == null)
        {
            var canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

            canvas =
                canvasObject.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(1280f, 720f);
        }

        var popup = new GameObject(
            "TowerBuildPopup",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(TowerSelectionUI)
        );

        popup.transform.SetParent(
            canvas.transform,
            false
        );

        TowerSelectionUI ui =
            popup.GetComponent<TowerSelectionUI>();

        ui.popupRoot = popup;

        ui.EnsureRuntimeLayout();

        ui.Hide();

        return ui;
    }

    public void ShowForTile(
        int gridX,
        int gridY,
        Vector3 worldPosition
    )
    {
        ResolveReferences();

        EnsureRuntimeLayout();

        SubscribeNetworkEvents();

        pendingGridX = gridX;
        pendingGridY = gridY;

        hasPendingTile = true;

        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text =
                "选择要建造的防御塔";
        }

        if (selectedTileText != null)
        {
            selectedTileText.text =
                "位置：" + gridX + "," + gridY;
        }

        SetMessage("");

        // 不再自动修改位置
        PositionPopup(worldPosition);

        RefreshTowerCards();
    }

    public void Hide()
    {
        hasPendingTile = false;

        pendingGridX = 0;
        pendingGridY = 0;

        SetMessage("");

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    public void RefreshTowerCards()
    {
        ResolveReferences();

        EnsureRuntimeLayout();

        ClearCards();

        List<TowerConfigData> configs =
            gameManager != null
                ? gameManager.GetTowerConfigs()
                : null;

        if (configs == null || configs.Count == 0)
        {
            SetMessage("暂无可用防御塔");
            return;
        }

        int currentGold =
            gameManager != null
                ? gameManager.gold
                : 0;

        for (int i = 0; i < configs.Count; i++)
        {
            TowerConfigData config =
                configs[i];

            if (config == null)
            {
                continue;
            }

            TowerCardUI card =
                CreateCard();

            card.Init(
                config,
                this,
                currentGold
            );

            cards.Add(card);
        }
    }

    public void OnTowerSelected(
        TowerConfigData tower
    )
    {
        ResolveReferences();

        if (!hasPendingTile)
        {
            ShowBuildMessage(
                "请先选择可建造地块"
            );

            return;
        }

        if (tower == null)
        {
            ShowBuildMessage(
                "无效的防御塔"
            );

            return;
        }

        int currentGold =
            gameManager != null
                ? gameManager.gold
                : 0;

        if (currentGold < tower.cost)
        {
            ShowBuildMessage("金币不足");

            RefreshTowerCards();

            return;
        }

        if (networkManager == null)
        {
            ShowBuildMessage(
                "网络管理器未初始化"
            );

            Debug.LogWarning(
                "[TowerSelectionUI] Missing NetworkManager. build_request was not sent."
            );

            return;
        }

        ShowBuildMessage("正在建造……");

        Debug.Log(
            "[TowerSelectionUI] Send build_request, tower_id="
            + tower.tower_id
            + ", grid="
            + pendingGridX
            + ","
            + pendingGridY
        );

        networkManager.SendBuildRequest(
            tower.tower_id,
            pendingGridX,
            pendingGridY
        );
    }

    public void OnBuildResult(
        BuildResultData data
    )
    {
        if (data == null)
        {
            ShowBuildMessage(
                "建塔失败：服务器返回为空"
            );

            return;
        }

        if (data.success)
        {
            if (battleUI != null)
            {
                battleUI.ShowMessage(
                    "建塔成功"
                );
            }

            Hide();

            return;
        }

        string message =
            GetBuildFailureMessage(
                data.reason
            );

        ShowBuildMessage(message);

        RefreshTowerCards();
    }

    private static TowerSelectionUI
        FindExistingInScene()
    {
        TowerSelectionUI[] all =
            Resources.FindObjectsOfTypeAll<TowerSelectionUI>();

        for (int i = 0; i < all.Length; i++)
        {
            TowerSelectionUI ui =
                all[i];

            if (
                ui != null
                && ui.gameObject.scene.IsValid()
                && ui.gameObject.scene.isLoaded
            )
            {
                return ui;
            }
        }

        return null;
    }

    private TowerCardUI CreateCard()
    {
        GameObject cardObject;

        if (towerCardPrefab != null)
        {
            cardObject = Instantiate(
                towerCardPrefab,
                towerCardRoot
            );
        }
        else
        {
            cardObject =
                CreateDefaultCardObject();

            cardObject.transform.SetParent(
                towerCardRoot,
                false
            );
        }

        TowerCardUI card =
            cardObject.GetComponent<TowerCardUI>();

        if (card == null)
        {
            card =
                cardObject.AddComponent<TowerCardUI>();
        }

        return card;
    }

    private GameObject
        CreateDefaultCardObject()
    {
        var cardObject = new GameObject(
            "TowerCard",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(TowerCardUI),
            typeof(LayoutElement)
        );

        return cardObject;
    }

    private void ClearCards()
    {
        cards.Clear();

        if (towerCardRoot == null)
        {
            return;
        }

        for (
            int i = towerCardRoot.childCount - 1;
            i >= 0;
            i--
        )
        {
            Destroy(
                towerCardRoot
                    .GetChild(i)
                    .gameObject
            );
        }
    }

    private void ShowBuildMessage(
        string message
    )
    {
        SetMessage(message);

        if (battleUI != null)
        {
            battleUI.ShowMessage(message);
        }
    }

    private void SetMessage(
        string message
    )
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    private void ResolveReferences()
    {
        if (popupRoot == null)
        {
            popupRoot = gameObject;
        }

        if (
            popupRect == null
            && popupRoot != null
        )
        {
            popupRect =
                popupRoot.GetComponent<RectTransform>();
        }

        if (GameManager.Instance != null)
        {
            gameManager =
                GameManager.Instance;
        }
        else if (gameManager == null)
        {
            gameManager =
                FindObjectOfType<GameManager>();
        }

        if (NetworkManager.Instance != null)
        {
            networkManager =
                NetworkManager.Instance;
        }
        else if (networkManager == null)
        {
            networkManager =
                FindObjectOfType<NetworkManager>();
        }

        if (battleUI == null)
        {
            battleUI =
                FindObjectOfType<BattleUI>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void EnsureRuntimeLayout()
    {
        ResolveReferences();

        // 不再自动设置位置
        // 不再自动设置大小
        // 不再自动设置颜色
        // 全部由 Unity Inspector 控制

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Hide
            );

            closeButton.onClick.AddListener(
                Hide
            );
        }
    }

    private TextMeshProUGUI
        FindOrCreateText(
            string childName,
            string text,
            float fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta
        )
    {
        Transform child =
            transform.Find(childName);

        if (child == null)
        {
            var childObject =
                new GameObject(
                    childName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI)
                );

            childObject.transform.SetParent(
                transform,
                false
            );

            child =
                childObject.transform;
        }

        var label =
            child.GetComponent<TextMeshProUGUI>();

        label.text = text;

        label.fontSize = fontSize;

        label.enableWordWrapping = true;

        label.alignment =
            TextAlignmentOptions.Center;

        return label;
    }

    private Button
        FindOrCreateCloseButton()
    {
        Transform child =
            transform.Find("CloseButton");

        if (child == null)
        {
            var childObject =
                new GameObject(
                    "CloseButton",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button)
                );

            childObject.transform.SetParent(
                transform,
                false
            );

            child =
                childObject.transform;
        }

        Button button =
            child.GetComponent<Button>();

        return button;
    }

    private void PositionPopup(
        Vector3 worldPosition
    )
    {
        // 不再自动修改位置
        // 完全由 Unity Inspector 控制
    }

    private void SubscribeNetworkEvents()
    {
        if (networkManager == null)
        {
            ResolveReferences();
        }

        if (networkManager != null)
        {
            networkManager.OnBuildResult
                -= OnBuildResult;

            networkManager.OnBuildResult
                += OnBuildResult;
        }
    }

    private void UnsubscribeNetworkEvents()
    {
        if (networkManager != null)
        {
            networkManager.OnBuildResult
                -= OnBuildResult;
        }
    }

    private string
        GetBuildFailureMessage(
            string reason
        )
    {
        switch (reason)
        {
            case "not_enough_gold":
                return "金币不足";

            case "tile_occupied":
                return "该地块已有塔";

            case "invalid_tower":
                return "无效的防御塔";

            case "invalid_player":
                return "无效玩家";

            case "game_over":
                return "游戏已结束";

            default:
                return string.IsNullOrEmpty(reason)
                    ? "未知原因"
                    : reason;
        }
    }
}