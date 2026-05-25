using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private readonly List<TowerCardUI> cards = new List<TowerCardUI>();

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
        TowerSelectionUI existing = FindExistingInScene();
        if (existing != null)
        {
            return existing;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
        }

        var popup = new GameObject("TowerBuildPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TowerSelectionUI));
        popup.transform.SetParent(canvas.transform, false);
        TowerSelectionUI ui = popup.GetComponent<TowerSelectionUI>();
        ui.popupRoot = popup;
        ui.EnsureRuntimeLayout();
        ui.Hide();
        return ui;
    }

    public void ShowForTile(int gridX, int gridY, Vector3 worldPosition)
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
            titleText.text = "选择要建造的防御塔";
        }

        if (selectedTileText != null)
        {
            selectedTileText.text = "位置：" + gridX + ", " + gridY;
        }

        SetMessage("");
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

        List<TowerConfigData> configs = gameManager != null ? gameManager.GetTowerConfigs() : null;
        if (configs == null || configs.Count == 0)
        {
            SetMessage("暂无可用防御塔");
            return;
        }

        int currentGold = gameManager != null ? gameManager.gold : 0;
        for (int i = 0; i < configs.Count; i++)
        {
            TowerConfigData config = configs[i];
            if (config == null)
            {
                continue;
            }

            TowerCardUI card = CreateCard();
            card.Init(config, this, currentGold);
            cards.Add(card);
        }
    }

    public void OnTowerSelected(TowerConfigData tower)
    {
        ResolveReferences();

        if (!hasPendingTile)
        {
            ShowBuildMessage("请先选择可建造地块");
            return;
        }

        if (tower == null)
        {
            ShowBuildMessage("无效的防御塔");
            return;
        }

        int currentGold = gameManager != null ? gameManager.gold : 0;
        if (currentGold < tower.cost)
        {
            ShowBuildMessage("金币不足");
            RefreshTowerCards();
            return;
        }

        if (networkManager == null)
        {
            ShowBuildMessage("网络管理器未初始化");
            Debug.LogWarning("[TowerSelectionUI] Missing NetworkManager. build_request was not sent.");
            return;
        }

        ShowBuildMessage("正在建造……");
        Debug.Log("[TowerSelectionUI] Send build_request, tower_id=" + tower.tower_id + ", grid=" + pendingGridX + "," + pendingGridY);
        networkManager.SendBuildRequest(tower.tower_id, pendingGridX, pendingGridY);
    }

    public void OnBuildResult(BuildResultData data)
    {
        if (data == null)
        {
            ShowBuildMessage("建塔失败：服务器返回为空");
            return;
        }

        if (data.success)
        {
            if (battleUI != null)
            {
                battleUI.ShowMessage("建塔成功");
            }

            Hide();
            return;
        }

        string message = GetBuildFailureMessage(data.reason);
        ShowBuildMessage(message);
        RefreshTowerCards();
    }

    private static TowerSelectionUI FindExistingInScene()
    {
        TowerSelectionUI[] all = Resources.FindObjectsOfTypeAll<TowerSelectionUI>();
        for (int i = 0; i < all.Length; i++)
        {
            TowerSelectionUI ui = all[i];
            if (ui != null && ui.gameObject.scene.IsValid() && ui.gameObject.scene.isLoaded)
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
            cardObject = Instantiate(towerCardPrefab, towerCardRoot);
        }
        else
        {
            cardObject = CreateDefaultCardObject();
            cardObject.transform.SetParent(towerCardRoot, false);
        }

        TowerCardUI card = cardObject.GetComponent<TowerCardUI>();
        if (card == null)
        {
            card = cardObject.AddComponent<TowerCardUI>();
        }

        return card;
    }

    private GameObject CreateDefaultCardObject()
    {
        var cardObject = new GameObject("TowerCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(TowerCardUI), typeof(LayoutElement));
        var rect = cardObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300f, 112f);

        var layoutElement = cardObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 300f;
        layoutElement.preferredHeight = 112f;
        layoutElement.minHeight = 112f;
        return cardObject;
    }

    private void ClearCards()
    {
        cards.Clear();
        if (towerCardRoot == null)
        {
            return;
        }

        for (int i = towerCardRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(towerCardRoot.GetChild(i).gameObject);
        }
    }

    private void ShowBuildMessage(string message)
    {
        SetMessage(message);
        if (battleUI != null)
        {
            battleUI.ShowMessage(message);
        }
    }

    private void SetMessage(string message)
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

        if (popupRect == null && popupRoot != null)
        {
            popupRect = popupRoot.GetComponent<RectTransform>();
        }

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

        if (battleUI == null)
        {
            battleUI = FindObjectOfType<BattleUI>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void EnsureRuntimeLayout()
    {
        ResolveReferences();

        if (popupRect != null)
        {
            popupRect.anchorMin = new Vector2(1f, 0.5f);
            popupRect.anchorMax = new Vector2(1f, 0.5f);
            popupRect.pivot = new Vector2(1f, 0.5f);
            popupRect.anchoredPosition = new Vector2(-28f, -10f);
            popupRect.sizeDelta = new Vector2(360f, 430f);
        }

        Image panelImage = popupRoot != null ? popupRoot.GetComponent<Image>() : GetComponent<Image>();
        if (panelImage == null && popupRoot != null)
        {
            panelImage = popupRoot.AddComponent<Image>();
        }

        if (panelImage != null)
        {
            panelImage.color = new Color(0.06f, 0.08f, 0.1f, 0.92f);
        }

        titleText = titleText != null
            ? titleText
            : FindOrCreateText("TitleText", "选择要建造的防御塔", 22f, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -28f), new Vector2(-40f, 32f));

        selectedTileText = selectedTileText != null
            ? selectedTileText
            : FindOrCreateText("SelectedTileText", "", 15f, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -62f), new Vector2(-40f, 24f));

        if (towerCardRoot == null)
        {
            Transform root = transform.Find("TowerCardRoot");
            if (root == null)
            {
                var rootObject = new GameObject("TowerCardRoot", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                rootObject.transform.SetParent(transform, false);
                root = rootObject.transform;
            }

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.offsetMin = new Vector2(22f, 66f);
            rootRect.offsetMax = new Vector2(-22f, -92f);

            var layout = root.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = root.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            towerCardRoot = root;
        }

        messageText = messageText != null
            ? messageText
            : FindOrCreateText("MessageText", "", 15f, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 45f), new Vector2(-40f, 30f));

        if (closeButton == null)
        {
            closeButton = FindOrCreateCloseButton();
        }

        closeButton.onClick.RemoveListener(Hide);
        closeButton.onClick.AddListener(Hide);
    }

    private TextMeshProUGUI FindOrCreateText(string childName, string text, float fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            var childObject = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            childObject.transform.SetParent(transform, false);
            child = childObject.transform;
        }

        var rect = child.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var label = child.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.enableWordWrapping = true;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    private Button FindOrCreateCloseButton()
    {
        Transform child = transform.Find("CloseButton");
        if (child == null)
        {
            var childObject = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            childObject.transform.SetParent(transform, false);
            child = childObject.transform;
        }

        var rect = child.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-16f, -14f);
        rect.sizeDelta = new Vector2(34f, 30f);

        var image = child.GetComponent<Image>();
        image.color = new Color(0.16f, 0.19f, 0.23f, 0.95f);

        Button button = child.GetComponent<Button>();
        Transform labelTransform = child.Find("Text");
        if (labelTransform == null)
        {
            var labelObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(child, false);
            labelTransform = labelObject.transform;
        }

        var labelRect = labelTransform.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelTransform.GetComponent<TextMeshProUGUI>();
        label.text = "×";
        label.fontSize = 20f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private void PositionPopup(Vector3 worldPosition)
    {
        if (popupRect == null)
        {
            return;
        }

        Canvas canvas = popupRect.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay || mainCamera == null)
        {
            popupRect.anchorMin = new Vector2(1f, 0.5f);
            popupRect.anchorMax = new Vector2(1f, 0.5f);
            popupRect.pivot = new Vector2(1f, 0.5f);
            popupRect.anchoredPosition = new Vector2(-28f, -10f);
            return;
        }

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(mainCamera, worldPosition);
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, canvas.worldCamera, out localPoint))
        {
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0f, 0.5f);
            popupRect.anchoredPosition = localPoint + new Vector2(26f, 0f);
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
            networkManager.OnBuildResult -= OnBuildResult;
            networkManager.OnBuildResult += OnBuildResult;
        }
    }

    private void UnsubscribeNetworkEvents()
    {
        if (networkManager != null)
        {
            networkManager.OnBuildResult -= OnBuildResult;
        }
    }

    private string GetBuildFailureMessage(string reason)
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
                return string.IsNullOrEmpty(reason) ? "未知原因" : reason;
        }
    }
}
