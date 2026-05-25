using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerCardUI : MonoBehaviour
{
    public Button button;
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI statText;
    public GameObject selectedMark;
    public int towerId;

    private TowerSelectionUI owner;
    private TowerConfigData config;
    private Image backgroundImage;

    public void Init(TowerConfigData towerConfig, TowerSelectionUI ownerUi, int currentGold)
    {
        EnsureReferences();

        owner = ownerUi;
        config = towerConfig;
        towerId = config != null ? config.tower_id : 0;
        bool canAfford = config != null && currentGold >= config.cost;

        if (nameText != null)
        {
            nameText.text = config != null ? config.name : "未知塔";
        }

        if (costText != null)
        {
            costText.text = config != null
                ? "造价：" + config.cost + (canAfford ? "" : "  金币不足")
                : "造价：-";
            costText.color = canAfford ? Color.white : new Color(1f, 0.55f, 0.45f, 1f);
        }

        if (statText != null)
        {
            statText.text = config != null
                ? "攻击：" + config.attack
                    + "\n范围：" + config.range.ToString("0.0")
                    + "\n冷却：" + config.cooldown.ToString("0.00") + "秒"
                : "攻击：-\n范围：-\n冷却：-";
        }

        if (iconImage != null)
        {
            Sprite sprite = VisualConfigManager.GetTowerSpriteForId(towerId);
            iconImage.sprite = sprite;
            iconImage.color = sprite != null ? Color.white : VisualConfigManager.GetTowerFallbackColor(towerId);
        }

        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
            button.interactable = canAfford;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = canAfford
                ? new Color(0.12f, 0.15f, 0.18f, 0.94f)
                : new Color(0.12f, 0.12f, 0.13f, 0.72f);
        }

        if (selectedMark != null)
        {
            selectedMark.SetActive(false);
        }
    }

    public void SetSelected(bool selected)
    {
        if (selectedMark != null)
        {
            selectedMark.SetActive(selected);
        }
    }

    private void HandleClick()
    {
        if (owner != null)
        {
            owner.OnTowerSelected(config);
        }
    }

    private void EnsureReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
        }

        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(300f, 112f);
        }

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredWidth = 300f;
        layoutElement.preferredHeight = 112f;
        layoutElement.minHeight = 112f;

        if (iconImage == null)
        {
            iconImage = FindOrCreateImage("Icon", new Vector2(54f, 54f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 6f));
        }

        if (nameText == null)
        {
            nameText = FindOrCreateText("NameText", 18f, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(106f, -18f), new Vector2(160f, 24f));
        }

        if (costText == null)
        {
            costText = FindOrCreateText("CostText", 14f, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(106f, -45f), new Vector2(160f, 22f));
        }

        if (statText == null)
        {
            statText = FindOrCreateText("StatText", 13f, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(106f, 32f), new Vector2(160f, 52f));
        }

        if (selectedMark == null)
        {
            selectedMark = FindOrCreateSelectedMark();
        }
    }

    private Image FindOrCreateImage(string childName, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            var childObject = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            childObject.transform.SetParent(transform, false);
            child = childObject.transform;
        }

        var rect = child.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return child.GetComponent<Image>();
    }

    private TextMeshProUGUI FindOrCreateText(string childName, float fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
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
        rect.sizeDelta = size;

        var text = child.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = true;
        return text;
    }

    private GameObject FindOrCreateSelectedMark()
    {
        Transform child = transform.Find("SelectedMark");
        if (child == null)
        {
            var childObject = new GameObject("SelectedMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            childObject.transform.SetParent(transform, false);
            child = childObject.transform;
        }

        var rect = child.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-13f, -13f);
        rect.sizeDelta = new Vector2(16f, 16f);

        var image = child.GetComponent<Image>();
        image.color = new Color(0.25f, 0.9f, 0.45f, 1f);
        return child.gameObject;
    }
}
