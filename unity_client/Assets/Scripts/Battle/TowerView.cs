using UnityEngine;

/// <summary>
/// 单个防御塔显示脚本。
/// 攻击、冷却、目标选择全部由服务端 game_logic 计算；
/// Unity 这里只展示塔的位置和可选攻击范围。
/// </summary>
public class TowerView : MonoBehaviour
{
    public string towerUid;
    public int towerId;
    public int tileId;
    public Renderer bodyRenderer;
    public Transform rangeVisual;

    private void Awake()
    {
        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponentInChildren<Renderer>();
        }
    }

    /// <summary>
    /// 首次绑定服务端塔状态。
    /// </summary>
    public void Bind(TowerState state, string key)
    {
        towerUid = key;
        UpdateFromState(state);
    }

    /// <summary>
    /// 根据服务端状态刷新塔的位置和攻击范围显示。
    /// 通常塔不会频繁移动，但保留这个函数方便以后做升级、换模型。
    /// </summary>
    public void UpdateFromState(TowerState state)
    {
        if (state == null)
        {
            return;
        }

        towerId = state.tower_id;
        tileId = state.tile_id;
        transform.position = new Vector3(state.x, state.y, state.z);

        if (rangeVisual != null && state.range > 0f)
        {
            rangeVisual.localScale = new Vector3(state.range * 2f, 0.02f, state.range * 2f);
        }
    }

    /// <summary>
    /// 显示或隐藏范围圈。
    /// 可以在鼠标悬停塔时调用 true，离开时调用 false。
    /// </summary>
    public void SetRangeVisible(bool visible)
    {
        if (rangeVisual != null)
        {
            rangeVisual.gameObject.SetActive(visible);
        }
    }

    public void SetColor(Color color)
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.material.color = color;
        }
    }
}
