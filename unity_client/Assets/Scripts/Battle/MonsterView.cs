using UnityEngine;

/// <summary>
/// 单个怪物的显示脚本。
/// 它不负责寻路、不负责扣血，只把服务端发来的坐标和血量表现出来。
/// </summary>
public class MonsterView : MonoBehaviour
{
    public string monsterUid;
    public int monsterId;
    public float moveLerpSpeed = 12f;
    public Transform healthBarFill;
    public Renderer bodyRenderer;

    private Vector3 targetPosition;
    private float hp;
    private float maxHp = 1f;

    private void Awake()
    {
        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponentInChildren<Renderer>();
        }

        targetPosition = transform.position;
    }

    private void Update()
    {
        // 用插值让服务端 Tick 推送的位置看起来更平滑。
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * moveLerpSpeed);
    }

    /// <summary>
    /// 首次绑定服务端怪物状态。
    /// key 由 StateRenderer 生成，正式联调建议后端发送 monster_uid。
    /// </summary>
    public void Bind(MonsterState state, string key)
    {
        monsterUid = key;
        UpdateFromState(state);
        transform.position = targetPosition;
    }

    /// <summary>
    /// 根据服务端状态刷新位置和血量。
    /// 如果 max_hp 没发，使用当前 hp 作为最大血量兜底。
    /// </summary>
    public void UpdateFromState(MonsterState state)
    {
        if (state == null)
        {
            return;
        }

        monsterId = state.monster_id;
        targetPosition = new Vector3(state.x, state.y, state.z);
        hp = state.hp;
        maxHp = state.max_hp > 0f ? state.max_hp : Mathf.Max(hp, 1f);
        UpdateHealthBar();
    }

    /// <summary>
    /// 设置怪物颜色。
    /// 美术资源到位后可以不用这个函数，直接换材质或动画。
    /// </summary>
    public void SetColor(Color color)
    {
        if (bodyRenderer != null)
        {
            bodyRenderer.material.color = color;
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null)
        {
            return;
        }

        float ratio = maxHp <= 0f ? 0f : Mathf.Clamp01(hp / maxHp);
        Vector3 scale = healthBarFill.localScale;
        scale.x = ratio;
        healthBarFill.localScale = scale;
    }
}
