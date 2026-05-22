using UnityEngine;

/// <summary>
/// 地块点击脚本。
/// 挂在 Tile.prefab 或场景中的地块对象上，需要 Collider 才能收到 OnMouseDown。
/// 点击后只发送 build_request，是否成功由服务端返回 build_result 决定。
/// </summary>
public class TileButton : MonoBehaviour
{
    public int tileId;
    public int towerIdOverride = -1;
    public bool occupied;

    [Header("Visual")]
    public Renderer tileRenderer;
    public Color normalColor = new Color(0.25f, 0.65f, 0.35f, 1f);
    public Color hoverColor = new Color(0.35f, 0.8f, 0.45f, 1f);
    public Color occupiedColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    private void Awake()
    {
        if (tileRenderer == null)
        {
            tileRenderer = GetComponent<Renderer>();
        }

        ApplyColor(normalColor);
    }

    private void OnEnable()
    {
        WebSocketClient.EnsureInstance().OnBuildResult += HandleBuildResult;
        WebSocketClient.EnsureInstance().OnStateUpdate += HandleStateUpdate;
    }

    private void OnDisable()
    {
        if (WebSocketClient.Instance == null)
        {
            return;
        }

        WebSocketClient.Instance.OnBuildResult -= HandleBuildResult;
        WebSocketClient.Instance.OnStateUpdate -= HandleStateUpdate;
    }

    /// <summary>
    /// 鼠标点击地块。
    /// 移动端或新输入系统后续可以改成 UI Button 或 Physics Raycast 调用 TryBuild。
    /// </summary>
    private void OnMouseDown()
    {
        TryBuild();
    }

    private void OnMouseEnter()
    {
        if (!occupied)
        {
            ApplyColor(hoverColor);
        }
    }

    private void OnMouseExit()
    {
        ApplyColor(occupied ? occupiedColor : normalColor);
    }

    /// <summary>
    /// 主动发起建塔请求。
    /// 如果本地已知道 occupied，直接拦截，减少无效请求。
    /// </summary>
    public void TryBuild()
    {
        if (occupied)
        {
            Debug.Log("Tile already occupied: " + tileId);
            return;
        }

        GameManager.EnsureInstance().BuildTower(tileId, transform.position, towerIdOverride);
    }

    /// <summary>
    /// 根据建塔结果更新本地地块状态。
    /// 这里只做 UI 反馈，最终仍以 state_update 里的 towers 列表为准。
    /// </summary>
    private void HandleBuildResult(BuildResultMessage message)
    {
        if (message == null || !message.success || message.tile_id != tileId)
        {
            return;
        }

        SetOccupied(true);
    }

    /// <summary>
    /// 每次 state_update 都检查服务端 towers 列表。
    /// 如果某个 tower.tile_id 等于当前地块，就认为该地块已被占用。
    /// </summary>
    private void HandleStateUpdate(StateUpdateMessage message)
    {
        if (message == null || message.state == null || message.state.towers == null)
        {
            return;
        }

        bool hasTower = false;

        for (int i = 0; i < message.state.towers.Count; i++)
        {
            TowerState tower = message.state.towers[i];
            if (tower != null && tower.tile_id == tileId)
            {
                hasTower = true;
                break;
            }
        }

        SetOccupied(hasTower);
    }

    public void SetOccupied(bool value)
    {
        occupied = value;
        ApplyColor(occupied ? occupiedColor : normalColor);
    }

    private void ApplyColor(Color color)
    {
        if (tileRenderer != null)
        {
            tileRenderer.material.color = color;
        }
    }
}
