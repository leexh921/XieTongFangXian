# 协同防线 — JSON 通信协议文档 v2.2

> 版本：v2.2（单人 MVP 版）
> 传输：WebSocket，JSON 文本
> 方向：Unity ↔ FastAPI Server

---

## 1. 通用消息格式

所有 WebSocket 消息统一使用以下结构：

```json
{
  "type": "message_type",
  "request_id": "uuid_or_string",
  "game_id": 10001,
  "player_id": 1,
  "timestamp": 1716200000000,
  "data": {}
}
```

### 字段说明

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `type` | string | 是 | 消息类型，见下文枚举 |
| `request_id` | string | 请求类消息必填 | 用于请求-响应配对 |
| `game_id` | int | 游戏中必填 | 游戏会话 ID |
| `player_id` | int | 登录后必填 | 玩家 ID |
| `timestamp` | int | 是 | 毫秒级 Unix 时间戳 |
| `data` | object | 视 type 而定 | 消息体 |

---

## 2. 消息类型清单

| type | 方向 | 说明 |
|---|---|---|
| `login_request` | Unity → Server | 登录请求 |
| `login_result` | Server → Unity | 登录结果 |
| `start_game_request` | Unity → Server | 开始游戏请求 |
| `game_start` | Server → Unity | 游戏开始 + 配置下发 |
| `build_request` | Unity → Server | 建塔请求 |
| `build_result` | Server → Unity | 建塔结果 |
| `state_update` | Server → Unity | 状态同步（高频） |
| `game_over` | Server → Unity | 游戏结束 |
| `error` | Server → Unity | 错误消息 |

---

## 3. 详细消息定义

### 3.1 login_request / login_result

#### login_request（Unity → Server）

```json
{
  "type": "login_request",
  "request_id": "req_001",
  "timestamp": 1716200000000,
  "data": {
    "username": "李潇涵"
  }
}
```

#### login_result — 成功（Server → Unity）

```json
{
  "type": "login_result",
  "request_id": "req_001",
  "timestamp": 1716200000100,
  "data": {
    "success": true,
    "player_id": 1,
    "username": "李潇涵",
    "message": "login success"
  }
}
```

#### login_result — 失败

```json
{
  "type": "login_result",
  "request_id": "req_001",
  "timestamp": 1716200000100,
  "data": {
    "success": false,
    "message": "username is empty"
  }
}
```

---

### 3.2 start_game_request / game_start

#### start_game_request（Unity → Server）

```json
{
  "type": "start_game_request",
  "request_id": "req_002",
  "player_id": 1,
  "timestamp": 1716200003000,
  "data": {
    "level_id": 1
  }
}
```

#### game_start（Server → Unity）

```json
{
  "type": "game_start",
  "game_id": 10001,
  "player_id": 1,
  "timestamp": 1716200003100,
  "data": {
    "level": {
      "level_id": 1,
      "name": "第一关",
      "base_hp": 100,
      "initial_gold": 300,
      "gold_per_second": 1
    },
    "player": {
      "player_id": 1,
      "username": "李潇涵",
      "gold": 300,
      "score": 0,
      "kill_count": 0
    },
    "tower_config": [
      {
        "tower_id": 1,
        "name": "基础塔",
        "attack": 10,
        "range": 3,
        "cooldown": 1.0,
        "cost": 100,
        "refund_rate": 0.5
      }
    ],
    "map": {
      "map_id": 1,
      "name": "默认地图",
      "width": 14,
      "height": 8,
      "path_points": [
        { "x": 0, "y": 4 },
        { "x": 1, "y": 4 },
        { "x": 2, "y": 4 },
        { "x": 3, "y": 4 },
        { "x": 4, "y": 4 },
        { "x": 5, "y": 4 },
        { "x": 5, "y": 3 },
        { "x": 5, "y": 2 },
        { "x": 6, "y": 2 },
        { "x": 7, "y": 2 },
        { "x": 8, "y": 2 },
        { "x": 9, "y": 2 },
        { "x": 10, "y": 2 },
        { "x": 11, "y": 2 },
        { "x": 12, "y": 2 },
        { "x": 12, "y": 1 },
        { "x": 12, "y": 0 },
        { "x": 13, "y": 0 }
      ],
      "obstacles": [
        { "x": 2, "y": 6 },
        { "x": 3, "y": 6 },
        { "x": 9, "y": 5 },
        { "x": 10, "y": 5 },
        { "x": 1, "y": 1 },
        { "x": 8, "y": 0 }
      ],
      "castle": { "x": 13, "y": 0 }
    },
    "base_hp": 100
  }
}
```

| 字段 | 说明 |
|---|---|
| `level` | 关卡基础配置 |
| `player` | 本玩家初始状态 |
| `tower_config` | 可用防御塔列表，Unity 据此生成建塔面板 |
| `map` | 地图逻辑配置，包含宽高、路径、障碍、堡垒 |
| `base_hp` | 基地初始血量 |

### map 对象

| 字段 | 说明 |
|---|---|
| `map_id` | 地图 ID |
| `name` | 地图名称 |
| `width` / `height` | 地图逻辑网格宽高 |
| `path_points[]` | 有序路径点，怪物沿这些 grid 坐标移动 |
| `obstacles[]` | 障碍格子坐标 |
| `castle` | 堡垒格子坐标；缺省时可使用路径最后一点 |

---

### 3.3 build_request / build_result

#### build_request（Unity → Server）

```json
{
  "type": "build_request",
  "request_id": "req_003",
  "game_id": 10001,
  "player_id": 1,
  "timestamp": 1716200005000,
  "data": {
    "tower_id": 1,
    "grid_x": 3,
    "grid_y": 5
  }
}
```

| data 字段 | 类型 | 说明 |
|---|---|---|
| `tower_id` | int | 防御塔类型 ID |
| `grid_x` | int | 地块 X 坐标 |
| `grid_y` | int | 地块 Y 坐标 |

#### build_result — 成功（Server → Unity）

```json
{
  "type": "build_result",
  "request_id": "req_003",
  "game_id": 10001,
  "player_id": 1,
  "timestamp": 1716200005100,
  "data": {
    "success": true,
    "reason": "",
    "tower": {
      "instance_id": "tower_10001_1",
      "tower_id": 1,
      "owner_player_id": 1,
      "grid_x": 3,
      "grid_y": 5
    },
    "player": {
      "player_id": 1,
      "gold": 200,
      "score": 0,
      "kill_count": 0
    }
  }
}
```

#### build_result — 失败

```json
{
  "type": "build_result",
  "request_id": "req_003",
  "game_id": 10001,
  "player_id": 1,
  "timestamp": 1716200005100,
  "data": {
    "success": false,
    "reason": "not_enough_gold",
    "tower": null,
    "player": {
      "player_id": 1,
      "gold": 50,
      "score": 0,
      "kill_count": 0
    }
  }
}
```

### reason 枚举

| 值 | 说明 |
|---|---|
| `""` | 成功 |
| `not_enough_gold` | 金币不足 |
| `tile_occupied` | 地块已被占用 |
| `invalid_tower` | 无效的防御塔 ID |
| `invalid_player` | 无效的玩家 |

---

### 3.4 state_update（Server → Unity）

Server 每秒广播 8 次游戏状态。Unity 只根据此消息渲染，不做权威计算。

```json
{
  "type": "state_update",
  "game_id": 10001,
  "timestamp": 1716200010000,
  "data": {
    "game_time_sec": 10.25,
    "base_hp": 90,
    "player": {
      "player_id": 1,
      "username": "李潇涵",
      "gold": 210,
      "score": 20,
      "kill_count": 1
    },
    "monsters": [
      {
        "instance_id": "monster_10001_1",
        "monster_id": 1,
        "hp": 30,
        "max_hp": 50,
        "x": 1.5,
        "y": 0.0,
        "path_index": 2
      }
    ],
    "towers": [
      {
        "instance_id": "tower_10001_1",
        "tower_id": 1,
        "owner_player_id": 1,
        "grid_x": 3,
        "grid_y": 5
      }
    ]
  }
}
```

| 字段 | 说明 |
|---|---|
| `game_time_sec` | 游戏已运行时间(秒) |
| `base_hp` | 基地剩余血量 |
| `player` | 本玩家状态（单人模式只含自己） |
| `monsters[]` | 当前存活怪物列表 |
| `towers[]` | 当前场上防御塔列表 |

### monster 对象

| 字段 | 说明 |
|---|---|
| `instance_id` | 怪物实例 ID，格式 `monster_{game_id}_{序号}` |
| `monster_id` | 怪物类型 ID |
| `hp` | 当前血量 |
| `max_hp` | 最大血量 |
| `x` / `y` | 当前浮点 grid 坐标 |
| `path_index` | 当前所在路径点索引 |

### tower 对象

| 字段 | 说明 |
|---|---|
| `instance_id` | 塔实例 ID，格式 `tower_{game_id}_{序号}` |
| `tower_id` | 防御塔类型 ID |
| `owner_player_id` | 建造者玩家 ID |
| `grid_x` / `grid_y` | 所在地块坐标 |

---

## 3.4.1 坐标语义

| 字段 | 语义 |
|---|---|
| `build_request.grid_x / grid_y` | 整数 grid 坐标 |
| `state_update.monsters[].x / y` | 浮点 grid 坐标，不是 Unity 世界坐标 |
| `state_update.towers[].grid_x / grid_y` | 整数 grid 坐标 |
| `tower_config[].range` | grid 单位攻击范围 |
| `game_start.data.map.path_points / obstacles / castle` | grid 坐标 |

Server 不发送 Unity 世界坐标，也不关心 Unity 的 `CellSize`、摄像机和世界偏移。Unity 根据 `game_start.data.map` 生成地图，并负责将 grid 坐标转换成 Unity world 坐标显示。

### 3.5 game_over（Server → Unity）

```json
{
  "type": "game_over",
  "game_id": 10001,
  "timestamp": 1716200060000,
  "data": {
    "level_id": 1,
    "is_win": true,
    "time_used": 60,
    "base_hp": 20,
    "player": {
      "player_id": 1,
      "username": "李潇涵",
      "score": 120,
      "kill_count": 6
    }
  }
}
```

| 字段 | 说明 |
|---|---|
| `level_id` | 关卡 ID |
| `is_win` | true=胜利，false=失败 |
| `time_used` | 本局用时(秒) |
| `base_hp` | 最终基地血量 |
| `player` | 本玩家最终数据 |

---

### 3.6 error（Server → Unity）

```json
{
  "type": "error",
  "request_id": "req_003",
  "game_id": 10001,
  "player_id": 1,
  "timestamp": 1716200005200,
  "data": {
    "code": "not_enough_gold",
    "message": "金币不足"
  }
}
```

### code 枚举

| 值 | 说明 |
|---|---|
| `invalid_json` | JSON 解析失败 |
| `not_logged_in` | 未登录 |
| `invalid_message_type` | 未知的 type |
| `game_not_started` | 游戏未开始 |
| `not_enough_gold` | 金币不足 |
| `tile_occupied` | 地块已被占用 |

---

## 4. 完整消息时序

```
Unity                          Server
  │                               │
  │── login_request ─────────────→│
  │←── login_result ──────────────│
  │                               │
  │── start_game_request ────────→│
  │←── game_start ────────────────│   (读取 DB 配置)
  │                               │
  │←── state_update ──────────────│   (每秒 8 次)
  │←── state_update ──────────────│
  │                               │
  │── build_request ─────────────→│
  │←── build_result ──────────────│
  │←── state_update ──────────────│   (含新塔)
  │                               │
  │        ... 重复 ...           │
  │                               │
  │←── game_over ─────────────────│   (Server 写入 DB)
  │                               │
```

---

## 5. Unity 端必须处理的字段清单

| 字段 | 来源消息 | 说明 |
|---|---|---|
| `player_id` | login_result | 全局保存 |
| `username` | login_result | 全局保存 |
| `game_id` | game_start | 游戏开始时保存 |
| `level.name` | game_start | 关卡名称 |
| `level.base_hp` | game_start | 基地血量初始值 |
| `level.initial_gold` | game_start | 初始金币 |
| `tower_config[]` | game_start | 建塔面板数据源 |
| `tower.tower_id` | game_start | 塔类型 ID |
| `tower.cost` | game_start | 造价 |
| `tower.attack` | game_start | 攻击力 |
| `tower.range` | game_start | 攻击范围 |
| `tower.cooldown` | game_start | 攻击间隔 |
| `map.width / map.height` | game_start | 地图逻辑宽高 |
| `map.path_points[]` | game_start | 地图路径 |
| `map.obstacles[]` | game_start | 障碍格子 |
| `map.castle` | game_start | 堡垒格子 |
| `data.player.gold` | state_update | 当前金币 |
| `data.player.score` | state_update | 当前得分 |
| `data.player.kill_count` | state_update | 击杀数 |
| `data.base_hp` | state_update | 基地当前血量 |
| `monsters[].instance_id` | state_update | 怪物实例 ID |
| `monsters[].monster_id` | state_update | 怪物类型 |
| `monsters[].hp` | state_update | 怪物血量 |
| `monsters[].x / .y` | state_update | 怪物浮点 grid 坐标 |
| `towers[].instance_id` | state_update | 塔实例 ID |
| `towers[].grid_x / .grid_y` | state_update | 塔所在坐标 |
| `build_result.success` | build_result | 建塔是否成功 |
| `game_over.is_win` | game_over | 胜负 |
| `game_over.player.score` | game_over | 最终得分 |
| `error.code` | error | 错误码 |
