using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TowerSpriteEntry
{
    public int tower_id;
    public Sprite sprite;
}

[Serializable]
public class MonsterSpriteEntry
{
    public int monster_id;
    public Sprite sprite;
}

public class VisualConfigManager : MonoBehaviour
{
    public List<TowerSpriteEntry> towerSprites = new List<TowerSpriteEntry>();
    public List<MonsterSpriteEntry> monsterSprites = new List<MonsterSpriteEntry>();
    public Sprite defaultTowerSprite;
    public Sprite defaultMonsterSprite;

    private static VisualConfigManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public static VisualConfigManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<VisualConfigManager>();
            }

            if (instance == null)
            {
                var managerObject = new GameObject("VisualConfigManager");
                var battleManagers = GameObject.Find("BattleManagers");
                if (battleManagers != null)
                {
                    managerObject.transform.SetParent(battleManagers.transform, false);
                }

                instance = managerObject.AddComponent<VisualConfigManager>();
            }

            return instance;
        }
    }

    public Sprite GetTowerSprite(int towerId)
    {
        for (int i = 0; i < towerSprites.Count; i++)
        {
            TowerSpriteEntry entry = towerSprites[i];
            if (entry != null && entry.tower_id == towerId && entry.sprite != null)
            {
                return entry.sprite;
            }
        }

        return defaultTowerSprite;
    }

    public Sprite GetMonsterSprite(int monsterId)
    {
        for (int i = 0; i < monsterSprites.Count; i++)
        {
            MonsterSpriteEntry entry = monsterSprites[i];
            if (entry != null && entry.monster_id == monsterId && entry.sprite != null)
            {
                return entry.sprite;
            }
        }

        return defaultMonsterSprite;
    }

    public static Sprite GetTowerSpriteForId(int towerId)
    {
        return Instance != null ? Instance.GetTowerSprite(towerId) : null;
    }

    public static Sprite GetMonsterSpriteForId(int monsterId)
    {
        return Instance != null ? Instance.GetMonsterSprite(monsterId) : null;
    }

    public static Color GetTowerFallbackColor(int towerId)
    {
        switch (towerId)
        {
            case 2:
                return new Color(0.15f, 0.75f, 0.95f, 1f);
            default:
                return new Color(0.2f, 0.45f, 0.95f, 1f);
        }
    }

    public static Color GetMonsterFallbackColor(int monsterId)
    {
        switch (monsterId)
        {
            case 2:
                return new Color(0.64f, 0.22f, 0.88f, 1f);
            default:
                return new Color(0.95f, 0.25f, 0.22f, 1f);
        }
    }
}
