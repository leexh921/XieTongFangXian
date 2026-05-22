using System;
using System.Collections.Generic;

/// <summary>
/// 只用于先读取 type 字段，然后 WebSocketClient 再根据 type 解析成具体消息。
/// 注意：这里字段名必须和 JSON 字段一致，所以使用 type 而不是 Type。
/// </summary>
[Serializable]
public class BaseMessage
{
    public string type;
}

/// <summary>
/// 登录请求。
/// Unity 登录界面输入昵称后发送，服务端只需要记录本局玩家名，不再创建多人房间。
/// </summary>
[Serializable]
public class LoginRequest
{
    public string type = MessageTypes.Login;
    public string username;
}

/// <summary>
/// 开始新一局。
/// 重要规则：服务端收到 start_game 时读取 MySQL 最新配置，
/// 所以 Web 后台修改配置后，下一局开始才生效。
/// </summary>
[Serializable]
public class StartGameRequest
{
    public string type = MessageTypes.StartGame;
    public string username;
    public int level_id = 1;
}

/// <summary>
/// 建塔请求。
/// tile_id 是逻辑地块编号；x/y/z 是 Unity 中点击地块的位置，后端可选使用。
/// 如果后端只信任 tile_id，也可以忽略 x/y/z。
/// </summary>
[Serializable]
public class BuildRequest
{
    public string type = MessageTypes.BuildRequest;
    public string game_id;
    public int tile_id;
    public int tower_id = 1;
    public float x;
    public float y;
    public float z;
}

/// <summary>
/// 登录结果。
/// success 为 false 时，message 给 UI 展示失败原因。
/// </summary>
[Serializable]
public class LoginResultMessage
{
    public string type;
    public bool success;
    public string username;
    public string message;
}

/// <summary>
/// 一局开始时的初始化数据。
/// 服务端可以把本局 game_id、初始状态和配置快照一起发给 Unity。
/// </summary>
[Serializable]
public class GameStartMessage
{
    public string type;
    public bool success = true;
    public string game_id;
    public int level_id;
    public string message;
    public GameConfigSnapshot config;
    public GameStateData state;
}

/// <summary>
/// 建塔结果。
/// 成功时 state_update 里也应包含新塔；失败时用 message 告诉玩家原因，例如金币不足。
/// </summary>
[Serializable]
public class BuildResultMessage
{
    public string type;
    public bool success;
    public string game_id;
    public int tile_id;
    public int tower_id;
    public string message;
    public TowerState tower;
}

/// <summary>
/// 服务端每个 Tick 推送的局内状态。
/// Unity 只负责根据这个状态渲染，不在客户端自行判定胜负。
/// </summary>
[Serializable]
public class StateUpdateMessage
{
    public string type;
    public string game_id;
    public float server_time;
    public GameStateData state;
}

/// <summary>
/// 游戏结束消息。
/// 服务端发送它之前或同时，应把成绩写入 MySQL 的 game_result 表。
/// </summary>
[Serializable]
public class GameOverMessage
{
    public string type;
    public string game_id;
    public bool win;
    public string username;
    public int score;
    public int gold;
    public int base_hp;
    public float duration;
    public string message;
}

/// <summary>
/// 通用错误消息。
/// 用于协议不合法、game_id 不存在、数据库读取失败等情况。
/// </summary>
[Serializable]
public class ErrorMessage
{
    public string type;
    public string code;
    public string message;
}

/// <summary>
/// 一局开始时的配置快照。
/// Unity 不直接改这些配置，只用来展示或做本地辅助渲染。
/// </summary>
[Serializable]
public class GameConfigSnapshot
{
    public List<TowerConfig> towers = new List<TowerConfig>();
    public List<MonsterConfig> monsters = new List<MonsterConfig>();
    public LevelConfig level;
    public List<WaveEventConfig> wave_events = new List<WaveEventConfig>();
}

[Serializable]
public class TowerConfig
{
    public int tower_id;
    public string name;
    public int cost;
    public float attack;
    public float range;
    public float cooldown;
}

[Serializable]
public class MonsterConfig
{
    public int monster_id;
    public string name;
    public float hp;
    public float speed;
    public int reward_gold;
    public int reward_score;
}

[Serializable]
public class LevelConfig
{
    public int level_id;
    public string name;
    public int initial_gold;
    public int base_hp;
}

[Serializable]
public class WaveEventConfig
{
    public int event_id;
    public int level_id;
    public float spawn_time;
    public int monster_id;
    public int count;
    public float interval;
}

/// <summary>
/// 局内状态根对象。
/// server_time 用于 UI 展示或调试；真正渲染主要看 player、monsters、towers。
/// </summary>
[Serializable]
public class GameStateData
{
    public string game_id;
    public float server_time;
    public int wave_index;
    public bool is_over;
    public PlayerState player;
    public List<MonsterState> monsters = new List<MonsterState>();
    public List<TowerState> towers = new List<TowerState>();
}

[Serializable]
public class PlayerState
{
    public string username;
    public int gold;
    public int score;
    public int base_hp;
}

/// <summary>
/// 怪物状态。
/// monster_uid 必须由服务端保证本局唯一；如果暂时没有，StateRenderer 会用临时 key 兜底，
/// 但正式联调时建议后端一定发送 monster_uid，避免渲染对象跳动。
/// </summary>
[Serializable]
public class MonsterState
{
    public string monster_uid;
    public int monster_id;
    public float x;
    public float y;
    public float z;
    public float hp;
    public float max_hp;
    public int path_index;
    public bool alive = true;
}

/// <summary>
/// 防御塔状态。
/// tower_uid 建议由服务端生成；tile_id 用于判断哪个地块已经被占用。
/// </summary>
[Serializable]
public class TowerState
{
    public string tower_uid;
    public int tower_id;
    public int tile_id;
    public float x;
    public float y;
    public float z;
    public float range;
    public float cooldown;
}
