using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 服务端状态渲染器。
/// 接收 state_update 后，同步场景里的怪物和防御塔对象。
/// 
/// 关键原则：
/// 1. 服务端是权威状态，Unity 不自行模拟核心逻辑。
/// 2. Unity 只负责把 monsters/towers 列表变成可见对象。
/// 3. 没有 prefab 时创建基础几何体占位，保证联调能先跑通。
/// </summary>
public class StateRenderer : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject monsterPrefab;
    public GameObject towerPrefab;

    [Header("Parents")]
    public Transform monsterParent;
    public Transform towerParent;

    private readonly Dictionary<string, MonsterView> monsters = new Dictionary<string, MonsterView>();
    private readonly Dictionary<string, TowerView> towers = new Dictionary<string, TowerView>();

    private void Start()
    {
        if (monsterParent == null)
        {
            monsterParent = transform;
        }

        if (towerParent == null)
        {
            towerParent = transform;
        }
    }

    private void OnEnable()
    {
        WebSocketClient client = WebSocketClient.EnsureInstance();
        client.OnGameStart += HandleGameStart;
        client.OnStateUpdate += ApplyState;
        client.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        if (WebSocketClient.Instance == null)
        {
            return;
        }

        WebSocketClient client = WebSocketClient.Instance;
        client.OnGameStart -= HandleGameStart;
        client.OnStateUpdate -= ApplyState;
        client.OnGameOver -= HandleGameOver;
    }

    /// <summary>
    /// 新一局开始时清空旧对象。
    /// 如果 game_start 中自带初始 state，则立即渲染第一帧。
    /// </summary>
    private void HandleGameStart(GameStartMessage message)
    {
        ClearAll();

        if (message != null && message.state != null)
        {
            ApplyGameState(message.state);
        }
    }

    /// <summary>
    /// state_update 入口函数。
    /// WebSocketClient 已经保证这个函数在 Unity 主线程调用。
    /// </summary>
    public void ApplyState(StateUpdateMessage message)
    {
        if (message == null || message.state == null)
        {
            return;
        }

        ApplyGameState(message.state);
    }

    /// <summary>
    /// 渲染完整 GameState。
    /// monsters 和 towers 都采用“有则更新、无则创建、消失则销毁”的方式。
    /// </summary>
    public void ApplyGameState(GameStateData state)
    {
        if (state == null)
        {
            return;
        }

        RenderMonsters(state.monsters);
        RenderTowers(state.towers);
    }

    private void HandleGameOver(GameOverMessage message)
    {
        // 结算界面会显示结果；场景对象可以保留最后一帧，也可以清空。
        // 为了演示清晰，这里保留最后一帧，不自动 ClearAll。
    }

    private void RenderMonsters(List<MonsterState> states)
    {
        HashSet<string> activeKeys = new HashSet<string>();

        if (states != null)
        {
            for (int i = 0; i < states.Count; i++)
            {
                MonsterState state = states[i];
                if (state == null)
                {
                    continue;
                }

                bool dead = !state.alive && state.max_hp > 0f && state.hp <= 0f;
                if (dead)
                {
                    continue;
                }

                string key = MakeMonsterKey(state, i);
                activeKeys.Add(key);

                MonsterView view;
                if (!monsters.TryGetValue(key, out view) || view == null)
                {
                    view = CreateMonsterView(key);
                    monsters[key] = view;
                    view.Bind(state, key);
                }
                else
                {
                    view.UpdateFromState(state);
                }
            }
        }

        RemoveMissingMonsters(activeKeys);
    }

    private void RenderTowers(List<TowerState> states)
    {
        HashSet<string> activeKeys = new HashSet<string>();

        if (states != null)
        {
            for (int i = 0; i < states.Count; i++)
            {
                TowerState state = states[i];
                if (state == null)
                {
                    continue;
                }

                string key = MakeTowerKey(state, i);
                activeKeys.Add(key);

                TowerView view;
                if (!towers.TryGetValue(key, out view) || view == null)
                {
                    view = CreateTowerView(key);
                    towers[key] = view;
                    view.Bind(state, key);
                }
                else
                {
                    view.UpdateFromState(state);
                }
            }
        }

        RemoveMissingTowers(activeKeys);
    }

    private MonsterView CreateMonsterView(string key)
    {
        GameObject obj;

        if (monsterPrefab != null)
        {
            obj = Instantiate(monsterPrefab, monsterParent);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            obj.transform.SetParent(monsterParent);
            obj.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
        }

        obj.name = "Monster_" + key;

        MonsterView view = obj.GetComponent<MonsterView>();
        if (view == null)
        {
            view = obj.AddComponent<MonsterView>();
        }

        view.SetColor(new Color(0.9f, 0.25f, 0.2f, 1f));
        return view;
    }

    private TowerView CreateTowerView(string key)
    {
        GameObject obj;

        if (towerPrefab != null)
        {
            obj = Instantiate(towerPrefab, towerParent);
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.transform.SetParent(towerParent);
            obj.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        }

        obj.name = "Tower_" + key;

        TowerView view = obj.GetComponent<TowerView>();
        if (view == null)
        {
            view = obj.AddComponent<TowerView>();
        }

        view.SetColor(new Color(0.2f, 0.45f, 0.9f, 1f));
        return view;
    }

    private void RemoveMissingMonsters(HashSet<string> activeKeys)
    {
        List<string> removeKeys = new List<string>();

        foreach (KeyValuePair<string, MonsterView> pair in monsters)
        {
            if (!activeKeys.Contains(pair.Key))
            {
                removeKeys.Add(pair.Key);
            }
        }

        for (int i = 0; i < removeKeys.Count; i++)
        {
            string key = removeKeys[i];
            if (monsters[key] != null)
            {
                Destroy(monsters[key].gameObject);
            }

            monsters.Remove(key);
        }
    }

    private void RemoveMissingTowers(HashSet<string> activeKeys)
    {
        List<string> removeKeys = new List<string>();

        foreach (KeyValuePair<string, TowerView> pair in towers)
        {
            if (!activeKeys.Contains(pair.Key))
            {
                removeKeys.Add(pair.Key);
            }
        }

        for (int i = 0; i < removeKeys.Count; i++)
        {
            string key = removeKeys[i];
            if (towers[key] != null)
            {
                Destroy(towers[key].gameObject);
            }

            towers.Remove(key);
        }
    }

    /// <summary>
    /// 清空场景中的怪物和塔。
    /// 新一局开始、返回登录页、重置调试场景时可以调用。
    /// </summary>
    public void ClearAll()
    {
        foreach (KeyValuePair<string, MonsterView> pair in monsters)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }

        foreach (KeyValuePair<string, TowerView> pair in towers)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value.gameObject);
            }
        }

        monsters.Clear();
        towers.Clear();
    }

    private string MakeMonsterKey(MonsterState state, int index)
    {
        if (!string.IsNullOrEmpty(state.monster_uid))
        {
            return state.monster_uid;
        }

        // 临时兜底：正式后端请发送 monster_uid，避免同类怪物位置变化时对象复用错误。
        return "monster_" + state.monster_id + "_" + index;
    }

    private string MakeTowerKey(TowerState state, int index)
    {
        if (!string.IsNullOrEmpty(state.tower_uid))
        {
            return state.tower_uid;
        }

        // 塔通常绑定 tile_id，所以 tile_id 可作为稳定 key。
        if (state.tile_id >= 0)
        {
            return "tile_" + state.tile_id;
        }

        return "tower_" + state.tower_id + "_" + index;
    }
}
