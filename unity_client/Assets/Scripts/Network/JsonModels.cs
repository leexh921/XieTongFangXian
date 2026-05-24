using System;
using System.Collections.Generic;

[Serializable]
public class ProtocolMessage
{
    public string type;
    public string request_id;
    public int game_id;
    public int player_id;
    public long timestamp;
}

[Serializable]
public class ProtocolMessage<TData> : ProtocolMessage
{
    public TData data;
}

[Serializable]
public class LoginRequestData
{
    public string username;
}

[Serializable]
public class LoginResultData
{
    public bool success;
    public int player_id;
    public string username;
    public string message;
}

[Serializable]
public class StartGameRequestData
{
    public int level_id;
}

[Serializable]
public class GameStartData
{
    public LevelData level;
    public PlayerStateData player;
    public List<TowerConfigData> tower_config;
    public int base_hp;
}

[Serializable]
public class LevelData
{
    public int level_id;
    public string name;
    public int base_hp;
    public int initial_gold;
    public int gold_per_second;
}

[Serializable]
public class PlayerStateData
{
    public int player_id;
    public string username;
    public int gold;
    public int score;
    public int kill_count;
}

[Serializable]
public class TowerConfigData
{
    public int tower_id;
    public string name;
    public int attack;
    public float range;
    public float cooldown;
    public int cost;
    public float refund_rate;
}

[Serializable]
public class MonsterStateData
{
    public string instance_id;
    public int monster_id;
    public int hp;
    public int max_hp;
    public float x;
    public float y;
    public int path_index;
}

[Serializable]
public class TowerStateData
{
    public string instance_id;
    public int tower_id;
    public int owner_player_id;
    public int grid_x;
    public int grid_y;
}

[Serializable]
public class StateUpdateData
{
    public float game_time_sec;
    public int base_hp;
    public PlayerStateData player;
    public List<MonsterStateData> monsters;
    public List<TowerStateData> towers;
}

[Serializable]
public class BuildRequestData
{
    public int tower_id;
    public int grid_x;
    public int grid_y;
}

[Serializable]
public class BuildResultData
{
    public bool success;
    public string reason;
    public TowerStateData tower;
    public PlayerStateData player;
}

[Serializable]
public class GameOverData
{
    public int level_id;
    public bool is_win;
    public int time_used;
    public int base_hp;
    public PlayerStateData player;
}

[Serializable]
public class ErrorData
{
    public string code;
    public string message;
}
