using System.Collections.Generic;
using UnityEngine;

public class StateRenderer : MonoBehaviour
{
    public Transform monsterRoot;
    public GameObject monsterPrefab;
    public BattleUI battleUI;
    public NetworkManager networkManager;
    public GameManager gameManager;

    private readonly Dictionary<string, MonsterView> monsters = new Dictionary<string, MonsterView>();
    private float nextStateLogTime;

    private void Start()
    {
        ResolveReferences();
        EnsureMonsterRoot();
        SubscribeNetworkEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkEvents();
    }

    private void HandleStateUpdate(StateUpdateData data)
    {
        ResolveReferences();

        if (data == null)
        {
            Debug.LogWarning("[StateRenderer] Ignored null state_update data.");
            return;
        }

        if (gameManager != null)
        {
            gameManager.UpdatePlayerState(data.player, data.base_hp);
        }

        if (battleUI != null)
        {
            battleUI.SetGameTime(data.game_time_sec);
            battleUI.Refresh();
        }

        foreach (var item in monsters)
        {
            if (item.Value != null)
            {
                item.Value.MarkMissingThisFrame();
            }
        }

        if (data.monsters != null)
        {
            for (int i = 0; i < data.monsters.Count; i++)
            {
                ApplyMonsterState(data.monsters[i]);
            }
        }

        RemoveMissingMonsters();

        int monsterCount = data.monsters != null ? data.monsters.Count : 0;
        int gold = data.player != null ? data.player.gold : 0;
        int score = data.player != null ? data.player.score : 0;
        if (data.game_time_sec >= nextStateLogTime)
        {
            Debug.Log("[StateRenderer] state_update rendered. monsters=" + monsterCount
                + ", gold=" + gold
                + ", score=" + score
                + ", base_hp=" + data.base_hp);
            nextStateLogTime = data.game_time_sec + 1f;
        }
    }

    private void ApplyMonsterState(MonsterStateData data)
    {
        if (data == null || string.IsNullOrEmpty(data.instance_id))
        {
            return;
        }

        MonsterView view;
        if (!monsters.TryGetValue(data.instance_id, out view) || view == null)
        {
            view = CreateMonsterView(data);
            monsters[data.instance_id] = view;
        }

        view.ApplyState(data, GetMonsterWorldPosition(data));
    }

    private MonsterView CreateMonsterView(MonsterStateData data)
    {
        EnsureMonsterRoot();

        GameObject monsterObject;
        if (monsterPrefab != null)
        {
            monsterObject = Instantiate(monsterPrefab, monsterRoot);
        }
        else
        {
            monsterObject = CreateDefaultMonsterObject();
            monsterObject.transform.SetParent(monsterRoot, false);
        }

        monsterObject.name = data.instance_id;
        monsterObject.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        var view = monsterObject.GetComponent<MonsterView>();
        if (view == null)
        {
            view = monsterObject.AddComponent<MonsterView>();
        }

        view.Init(data.instance_id, data.monster_id, data.hp, data.max_hp, GetMonsterWorldPosition(data));
        return view;
    }

    private Vector3 GetMonsterWorldPosition(MonsterStateData data)
    {
        MapConfigData map = BattleMapConfig.GetActiveMapConfig();
        return BattleMapConfig.GridToWorld(data.x, data.y, map);
    }

    private GameObject CreateDefaultMonsterObject()
    {
        var monsterObject = new GameObject("Monster");
        monsterObject.AddComponent<SpriteRenderer>();
        monsterObject.AddComponent<MonsterView>();
        return monsterObject;
    }

    private void RemoveMissingMonsters()
    {
        var removeKeys = new List<string>();
        foreach (var item in monsters)
        {
            if (item.Value == null || !item.Value.WasUpdatedThisFrame())
            {
                removeKeys.Add(item.Key);
            }
        }

        for (int i = 0; i < removeKeys.Count; i++)
        {
            string key = removeKeys[i];
            MonsterView view;
            if (monsters.TryGetValue(key, out view) && view != null)
            {
                Destroy(view.gameObject);
            }

            monsters.Remove(key);
        }
    }

    private void EnsureMonsterRoot()
    {
        if (monsterRoot != null)
        {
            return;
        }

        var root = GameObject.Find("MonsterRoot");
        if (root == null)
        {
            root = new GameObject("MonsterRoot");
            var mapRoot = GameObject.Find("MapRoot");
            root.transform.SetParent(mapRoot != null ? mapRoot.transform : transform, false);
        }

        monsterRoot = root.transform;
    }

    private void ResolveReferences()
    {
        if (NetworkManager.Instance != null)
        {
            networkManager = NetworkManager.Instance;
        }
        else if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();
        }

        if (GameManager.Instance != null)
        {
            gameManager = GameManager.Instance;
        }
        else if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (battleUI == null)
        {
            battleUI = FindObjectOfType<BattleUI>();
        }

        if (monsterRoot == null)
        {
            var root = GameObject.Find("MonsterRoot");
            if (root != null)
            {
                monsterRoot = root.transform;
            }
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
            networkManager.OnStateUpdate -= HandleStateUpdate;
            networkManager.OnStateUpdate += HandleStateUpdate;
        }
    }

    private void UnsubscribeNetworkEvents()
    {
        if (networkManager != null)
        {
            networkManager.OnStateUpdate -= HandleStateUpdate;
        }
    }
}
