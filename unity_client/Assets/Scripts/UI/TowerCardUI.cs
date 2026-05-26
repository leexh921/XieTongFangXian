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

    public void Init(
        TowerConfigData towerConfig,
        TowerSelectionUI ownerUi,
        int currentGold
    )
    {
        EnsureReferences();

        owner = ownerUi;
        config = towerConfig;

        towerId =
            config != null
                ? config.tower_id
                : 0;

        bool canAfford =
            config != null
            && currentGold >= config.cost;

        if (nameText != null)
        {
            nameText.text =
                config != null
                    ? config.name
                    : "未知塔";
        }

        if (costText != null)
        {
            costText.text =
                config != null
                    ? "造价：" + config.cost
                        + (
                            canAfford
                                ? ""
                                : "  金币不足"
                        )
                    : "造价：-";

            // 金币不足时自动红色
            if (!canAfford)
            {
                costText.color = Color.red;
            }
        }

        if (statText != null)
        {
            statText.text =
                config != null
                    ? "攻击：" + config.attack
                        + "\n范围："
                        + config.range.ToString("0.0")
                        + "\n冷却："
                        + config.cooldown.ToString("0.00")
                        + "秒"
                    : "攻击：-\n范围：-\n冷却：-";
        }

        if (iconImage != null)
        {
            Sprite sprite =
                VisualConfigManager
                    .GetTowerSpriteForId(
                        towerId
                    );

            iconImage.sprite = sprite;

            // 不再自动修改颜色
            // Inspector 控制
        }

        if (button != null)
        {
            button.onClick.RemoveListener(
                HandleClick
            );

            button.onClick.AddListener(
                HandleClick
            );

            button.interactable =
                canAfford;
        }

        // 不再自动修改背景颜色
        // backgroundImage.color 删除

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
            button =
                GetComponent<Button>();
        }

        if (button == null)
        {
            button =
                gameObject.AddComponent<Button>();
        }

        if (backgroundImage == null)
        {
            backgroundImage =
                GetComponent<Image>();
        }

        if (backgroundImage == null)
        {
            backgroundImage =
                gameObject.AddComponent<Image>();
        }

        // 不再自动设置卡片大小
        // 现在由 Unity Inspector 控制

        LayoutElement layoutElement =
            GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement =
                gameObject.AddComponent<LayoutElement>();
        }

        // 不再自动设置宽高
        // Inspector 控制

        if (iconImage == null)
        {
            iconImage =
                FindOrCreateImage(
                    "Icon"
                );
        }

        if (nameText == null)
        {
            nameText =
                FindOrCreateText(
                    "NameText",
                    18f
                );
        }

        if (costText == null)
        {
            costText =
                FindOrCreateText(
                    "CostText",
                    14f
                );
        }

        if (statText == null)
        {
            statText =
                FindOrCreateText(
                    "StatText",
                    13f
                );
        }

        if (selectedMark == null)
        {
            selectedMark =
                FindOrCreateSelectedMark();
        }
    }

    private Image FindOrCreateImage(
        string childName
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
                    typeof(Image)
                );

            childObject.transform.SetParent(
                transform,
                false
            );

            child =
                childObject.transform;
        }

        return child.GetComponent<Image>();
    }

    private TextMeshProUGUI
        FindOrCreateText(
            string childName,
            float fontSize
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

        var text =
            child.GetComponent<TextMeshProUGUI>();

        text.fontSize = fontSize;

        // 不再自动修改颜色
        // text.color = Color.white;

        text.alignment =
            TextAlignmentOptions.Left;

        text.enableWordWrapping = true;

        return text;
    }

    private GameObject
        FindOrCreateSelectedMark()
    {
        Transform child =
            transform.Find(
                "SelectedMark"
            );

        if (child == null)
        {
            var childObject =
                new GameObject(
                    "SelectedMark",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );

            childObject.transform.SetParent(
                transform,
                false
            );

            child =
                childObject.transform;
        }

        // 不再自动设置位置
        // 不再自动设置大小
        // 不再自动设置颜色

        return child.gameObject;
    }
}