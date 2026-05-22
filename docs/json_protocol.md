# 《协同防线》JSON 通信协议文档

> 版本：v1.0  
> 适用阶段：5.22 数据互通与 MVP 联调  
> 维护人：李潇涵  
> 目标：统一 Unity 客户端、Python WebSocket Server、游戏核心逻辑、Web 后台与数据库之间的数据字段。

---

## 1. 协议目标

本协议用于保证《协同防线》MVP 阶段的数据链路一致，避免 Unity、Server、核心逻辑、Web/数据库之间字段名不统一。

MVP 阶段只保证以下闭环：

```text
Web 后台配置塔、怪物、关卡、出怪时间轴
→ Server 开局读取配置
→ Unity 发送玩家操作
→ Server / 核心逻辑权威计算
→ Unity 接收状态并显示
→ 游戏结束写入 game_result
→ Web 后台展示排行榜
```

本版本不包含：掉线重连、多关卡切换、塔升级、塔出售、复杂怪物技能、复杂排行榜筛选、在线人数仪表盘、精美特效。

---

## 2. 字段命名规范

所有 JSON 字段统一使用 **下划线命名法**。

正确示例：

```text
player_id
tower_id
monster_id
room_code
game_id
grid_x
grid_y
base_hp
kill_count
```

不要使用：

```text
playerId
towerId
monsterId
roomCode
gameId
gridX
gridY
```

---

## 3. 通用消息格式

所有 WebSocket 消息统一使用以下结构：

```json
{
  "type": "message_type",
  "request_id": "uuid_or_timestamp",
  "room_code": "A001",
  "game_id": 10001,
  "player_id": 1,
  "timestamp": 1716200000000,
  "data": {}
}
```

| 字段 | 类型 | 是否必须 | 说明 |
|---|---|---|---|
| type | string | 是 | 消息类型，例如 `login_request`、`state_update` |
| request_id | string | 建议必须 | 一次请求的唯一编号，方便排查日志 |
| room_code | string | 房间相关消息必须 | 房间号，例如 `A001` |
| game_id | int | 游戏开始后必须 | 一局游戏编号，写入 `game_result.game_id` |
| player_id | int | 登录后必须 | 玩家编号，对应 `player.player_id` |
| timestamp | long | 建议必须 | 毫秒时间戳 |
| data | object | 是 | 具体业务数据 |

说明：

1. 登录前可以没有 `player_id`。
2. 创建房间前可以没有 `room_code`。
3. 游戏开始前可以没有 `game_id`。
4. `request_id` 用于把请求和返回结果对应起来，建议 Unity 每次请求都生成。

---

## 4. 基础数据对象

### 4.1 玩家对象 player

对应数据库表：`player`

```json
{
  "player_id": 1,
  "username": "李潇涵"
}
```

| JSON 字段 | 数据库字段 | 类型 | 说明 |
|---|---|---|---|
| player_id | player.player_id | int | 玩家 ID |
| username | player.username | string | 玩家昵称 |

MVP 阶段 Unity 只需要 `player_id` 和 `username`，不需要传输 `password_hash`、`created_at`。

---

### 4.2 防御塔配置 tower

对应数据库表：`tower`

```json
{
  "tower_id": 1,
  "name": "基础塔",
  "attack": 10,
  "range": 3,
  "cooldown": 1.0,
  "cost": 100,
  "refund_rate": 0.5
}
```

| JSON 字段 | 数据库字段 | 类型 | 说明 |
|---|---|---|---|
| tower_id | tower.tower_id | int | 防御塔 ID |
| name | tower.name | string | 防御塔名称 |
| attack | tower.attack | int | 攻击力 |
| range | tower.range | int | 攻击范围 |
| cooldown | tower.cooldown | float | 攻击冷却时间，单位秒 |
| cost | tower.cost | int | 建造消耗金币 |
| refund_rate | tower.refund_rate | float | 出售退款比例，MVP 暂不使用 |

MVP 必须使用：`tower_id`、`attack`、`range`、`cooldown`、`cost`。  
`refund_rate` 只保留字段，不做塔出售功能。

---

### 4.3 怪物配置 monster

对应数据库表：`monster`

```json
{
  "monster_id": 1,
  "name": "普通怪",
  "hp": 50,
  "speed": 1.5,
  "reward_gold": 10,
  "score": 20
}
```

| JSON 字段 | 数据库字段 | 类型 | 说明 |
|---|---|---|---|
| monster_id | monster.monster_id | int | 怪物配置 ID |
| name | monster.name | string | 怪物名称 |
| hp | monster.hp | int | 初始血量 |
| speed | monster.speed | float | 移动速度 |
| reward_gold | monster.reward_gold | int | 击杀后奖励金币 |
| score | monster.score | int | 击杀后奖励分数 |

---

### 4.4 关卡配置 level

对应数据库表：`level`

```json
{
  "level_id": 1,
  "name": "第一关",
  "base_hp": 100,
  "initial_gold": 300,
  "gold_per_second": 1
}
```

| JSON 字段 | 数据库字段 | 类型 | 说明 |
|---|---|---|---|
| level_id | level.level_id | int | 关卡 ID |
| name | level.name | string | 关卡名称 |
| base_hp | level.base_hp | int | 基地初始血量 |
| initial_gold | level.initial_gold | int | 每名玩家初始金币 |
| gold_per_second | level.gold_per_second | int | 每秒金币增长 |

MVP 阶段只使用 `level_id = 1` 的默认关卡。

---

### 4.5 出怪事件 wave_event

对应数据库表：`wave_event`

```json
{
  "event_id": 1,
  "level_id": 1,
  "wave_number": 1,
  "spawn_time_sec": 5,
  "monster_id": 1,
  "count": 3,
  "interval": 1
}
```

| JSON 字段 | 数据库字段 | 类型 | 说明 |
|---|---|---|---|
| event_id | wave_event.event_id | int | 出怪事件 ID |
| level_id | wave_event.level_id | int | 所属关卡 ID |
| wave_number | wave_event.wave_number | int | 第几波 |
| spawn_time_sec | wave_event.spawn_time_sec | int | 开局后第几秒开始出怪 |
| monster_id | wave_event.monster_id | int | 生成的怪物类型 |
| count | wave_event.count | int | 生成数量 |
| interval | wave_event.interval | int | 每只怪物生成间隔，单位秒 |

---

## 5. Unity → Server 消息

### 5.1 登录请求 login_request

Unity 输入昵称后发送。

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

### 5.2 登录结果 login_result

Server 返回。

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

失败示例：

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

MVP 登录规则：

1. 不校验密码。
2. 如果 `username` 已存在，返回已有 `player_id`。
3. 如果 `username` 不存在，Server 可以自动创建玩家记录。

---

### 5.3 创建房间 create_room_request

```json
{
  "type": "create_room_request",
  "request_id": "req_002",
  "player_id": 1,
  "timestamp": 1716200001000,
  "data": {}
}
```

### 5.4 创建房间结果 create_room_result

```json
{
  "type": "create_room_result",
  "request_id": "req_002",
  "player_id": 1,
  "room_code": "A001",
  "timestamp": 1716200001100,
  "data": {
    "success": true,
    "room_code": "A001",
    "player_list": [
      {
        "player_id": 1,
        "username": "李潇涵"
      }
    ]
  }
}
```

---

### 5.5 加入房间 join_room_request

```json
{
  "type": "join_room_request",
  "request_id": "req_003",
  "player_id": 2,
  "timestamp": 1716200002000,
  "data": {
    "room_code": "A001"
  }
}
```

### 5.6 加入房间结果 join_room_result

```json
{
  "type": "join_room_result",
  "request_id": "req_003",
  "player_id": 2,
  "room_code": "A001",
  "timestamp": 1716200002100,
  "data": {
    "success": true,
    "room_code": "A001",
    "player_list": [
      {
        "player_id": 1,
        "username": "李潇涵"
      },
      {
        "player_id": 2,
        "username": "雅丽娜"
      }
    ]
  }
}
```

---

### 5.7 房间玩家列表广播 player_list_update

当有人创建房间或加入房间时，Server 向房间内所有 Unity 客户端广播。

```json
{
  "type": "player_list_update",
  "room_code": "A001",
  "timestamp": 1716200002200,
  "data": {
    "player_count": 2,
    "max_player_count": 4,
    "player_list": [
      {
        "player_id": 1,
        "username": "李潇涵"
      },
      {
        "player_id": 2,
        "username": "雅丽娜"
      }
    ]
  }
}
```

Unity 至少显示：`room_code`、`player_count`、`player_list.username`。

---

### 5.8 开始游戏请求 start_game_request

房主点击开始游戏后发送。

```json
{
  "type": "start_game_request",
  "request_id": "req_004",
  "player_id": 1,
  "room_code": "A001",
  "timestamp": 1716200003000,
  "data": {
    "level_id": 1
  }
}
```

### 5.9 游戏开始广播 game_start

Server 读取 Web 后台配置后广播给 Unity。

```json
{
  "type": "game_start",
  "room_code": "A001",
  "game_id": 10001,
  "timestamp": 1716200003100,
  "data": {
    "level": {
      "level_id": 1,
      "name": "第一关",
      "base_hp": 100,
      "initial_gold": 300,
      "gold_per_second": 1
    },
    "players": [
      {
        "player_id": 1,
        "username": "李潇涵",
        "gold": 300,
        "score": 0,
        "kill_count": 0
      },
      {
        "player_id": 2,
        "username": "雅丽娜",
        "gold": 300,
        "score": 0,
        "kill_count": 0
      }
    ],
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
    "base_hp": 100
  }
}
```

说明：`wave_event` 不需要发给 Unity。出怪由 Server / 核心逻辑控制，Unity 只接收 `state_update` 显示结果。

---

## 6. 战斗过程消息

### 6.1 建塔请求 build_request

Unity 点击地块后发送给 Server。

```json
{
  "type": "build_request",
  "request_id": "req_005",
  "room_code": "A001",
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

| 字段 | 类型 | 说明 |
|---|---|---|
| tower_id | int | 要建造的防御塔 ID |
| grid_x | int | 地块横坐标 |
| grid_y | int | 地块纵坐标 |

建塔是否成功只能由 Server / 核心逻辑判断，Unity 不允许本地直接扣金币或生成塔。

---

### 6.2 建塔结果 build_result

Server 判断金币、地块占用、防御塔配置后返回，并广播给房间内所有玩家。

成功示例：

```json
{
  "type": "build_result",
  "request_id": "req_005",
  "room_code": "A001",
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

失败示例：

```json
{
  "type": "build_result",
  "request_id": "req_005",
  "room_code": "A001",
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

`reason` 建议枚举：

| reason | 说明 |
|---|---|
| not_enough_gold | 金币不足 |
| grid_occupied | 地块已被占用 |
| invalid_grid | 非法地块 |
| invalid_tower_id | 防御塔 ID 不存在 |
| game_not_started | 游戏未开始 |
| game_already_over | 游戏已结束 |

---

### 6.3 状态同步 state_update

Server 固定频率广播游戏状态。MVP 阶段建议每秒 5～10 次，不要追求高频同步。

```json
{
  "type": "state_update",
  "room_code": "A001",
  "game_id": 10001,
  "timestamp": 1716200010000,
  "data": {
    "game_time_sec": 10,
    "base_hp": 90,
    "players": [
      {
        "player_id": 1,
        "username": "李潇涵",
        "gold": 210,
        "score": 20,
        "kill_count": 1
      },
      {
        "player_id": 2,
        "username": "雅丽娜",
        "gold": 300,
        "score": 0,
        "kill_count": 0
      }
    ],
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

Unity 只根据 `state_update` 显示怪物、塔、金币、血量和分数，不自行计算最终结果。

---

## 7. 游戏结束消息

### 7.1 游戏结束广播 game_over

Server 判断胜负后广播。

```json
{
  "type": "game_over",
  "room_code": "A001",
  "game_id": 10001,
  "timestamp": 1716200060000,
  "data": {
    "level_id": 1,
    "is_win": true,
    "time_used": 60,
    "base_hp": 20,
    "results": [
      {
        "player_id": 1,
        "username": "李潇涵",
        "score": 120,
        "kill_count": 6
      },
      {
        "player_id": 2,
        "username": "雅丽娜",
        "score": 80,
        "kill_count": 4
      }
    ]
  }
}
```

说明：

1. `time_used` 只用于 Unity 结算界面显示。
2. 当前数据库 `game_result` 表没有 `time_used` 字段，因此 MVP 阶段不写入数据库。
3. `results` 中每个玩家最终会写入一条 `game_result`。

---

### 7.2 Server 写入 game_result 的数据格式

对应数据库表：`game_result`

每个玩家写入一条记录。

```json
{
  "game_id": 10001,
  "player_id": 1,
  "level_id": 1,
  "score": 120,
  "is_win": 1,
  "played_at": "2026-05-22 20:30:00",
  "kill_count": 6,
  "room_code": "A001"
}
```

| JSON 字段 | 数据库字段 | 类型 | 说明 |
|---|---|---|---|
| game_id | game_result.game_id | int | 一局游戏 ID |
| player_id | game_result.player_id | int | 玩家 ID |
| level_id | game_result.level_id | int | 关卡 ID |
| score | game_result.score | int | 最终个人分数 |
| is_win | game_result.is_win | int / bool | 是否胜利，1 胜利，0 失败 |
| played_at | game_result.played_at | datetime | 游戏结束时间 |
| kill_count | game_result.kill_count | int | 击杀数量 |
| room_code | game_result.room_code | string | 房间号 |

注意：`game_result` 不存 `username`，排行榜通过 `game_result.player_id` 关联 `player.username`。

---

## 8. Server 读取配置格式

Server 开局时需要从数据库读取以下配置：`level`、`tower`、`monster`、`wave_event`。

```json
{
  "level": {
    "level_id": 1,
    "name": "第一关",
    "base_hp": 100,
    "initial_gold": 300,
    "gold_per_second": 1
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
  "monster_config": [
    {
      "monster_id": 1,
      "name": "普通怪",
      "hp": 50,
      "speed": 1.5,
      "reward_gold": 10,
      "score": 20
    }
  ],
  "wave_events": [
    {
      "event_id": 1,
      "level_id": 1,
      "wave_number": 1,
      "spawn_time_sec": 5,
      "monster_id": 1,
      "count": 3,
      "interval": 1
    }
  ]
}
```

---

## 9. 排行榜接口数据格式

Web 后台展示排行榜时建议返回以下格式。

```json
{
  "type": "leaderboard_response",
  "timestamp": 1716200100000,
  "data": {
    "ranking": [
      {
        "rank": 1,
        "result_id": 1,
        "game_id": 10001,
        "room_code": "A001",
        "player_id": 1,
        "username": "李潇涵",
        "level_id": 1,
        "level_name": "第一关",
        "score": 120,
        "kill_count": 6,
        "is_win": true,
        "played_at": "2026-05-22 20:30:00"
      }
    ]
  }
}
```

MVP 排行榜排序规则：

```sql
ORDER BY score DESC, played_at ASC
```

即：先按分数从高到低排序，分数相同则先完成的排前面。

---

## 10. 错误消息 error

所有模块通用错误格式：

```json
{
  "type": "error",
  "request_id": "req_005",
  "room_code": "A001",
  "game_id": 10001,
  "player_id": 1,
  "timestamp": 1716200005200,
  "data": {
    "code": "invalid_message_type",
    "message": "unknown message type"
  }
}
```

`code` 建议枚举：

| code | 说明 |
|---|---|
| invalid_message_type | 未知消息类型 |
| missing_required_field | 缺少必要字段 |
| invalid_room_code | 房间不存在 |
| room_full | 房间已满 |
| player_not_in_room | 玩家不在房间中 |
| game_not_started | 游戏未开始 |
| game_already_over | 游戏已结束 |
| server_internal_error | 服务端内部错误 |

---

## 11. 运行时字段说明

以下字段不是数据库字段，由 Server / 核心逻辑在运行时生成：

```text
monster.instance_id
monster.x
monster.y
monster.path_index
tower.instance_id
tower.owner_player_id
tower.grid_x
tower.grid_y
```

数据库只保存配置数据和最终成绩，战斗中的怪物实例、塔实例、坐标、血量等状态保存在 Server 内存中。

---

## 12. MVP 字段确认清单

### 12.1 Unity 必须确认

```text
player_id
username
room_code
game_id
tower_id
grid_x
grid_y
base_hp
gold
score
kill_count
monster.instance_id
monster.x
monster.y
tower.instance_id
```

### 12.2 Server 必须确认

```text
type
request_id
player_id
room_code
game_id
level_id
build_request
build_result
state_update
game_over
```

### 12.3 核心逻辑必须确认

```text
level_id
base_hp
initial_gold
gold_per_second
tower_id
monster_id
wave_event
grid_x
grid_y
owner_player_id
score
kill_count
is_win
```

### 12.4 Web/数据库必须确认

```text
player.player_id
player.username
tower.tower_id
monster.monster_id
level.level_id
wave_event.event_id
game_result.result_id
game_result.game_id
game_result.player_id
game_result.level_id
game_result.score
game_result.is_win
game_result.played_at
game_result.kill_count
game_result.room_code
```

---

## 13. 最小联调顺序

不要跳步骤，建议按以下顺序联调：

```text
1. Unity login_request → Server login_result
2. Unity create_room_request → Server create_room_result
3. Unity join_room_request → Server join_room_result
4. Server 广播 player_list_update
5. Unity start_game_request → Server 读取 DB 配置
6. Server 广播 game_start
7. Server / 核心逻辑根据 wave_event 生成怪物
8. Server 广播 state_update
9. Unity 发送 build_request
10. Server / 核心逻辑返回 build_result
11. Server 广播新的 state_update
12. Server 生成 game_over
13. Server 写入 game_result
14. Web 后台展示排行榜
```

---

## 14. MVP 范围控制

必须保留：

1. 登录。
2. 创建 / 加入房间。
3. 玩家列表同步。
4. Web 配置塔、怪物、关卡、出怪时间轴。
5. Server 开局读取配置。
6. 建塔请求与结果。
7. 状态同步。
8. 游戏结束。
9. 写入 `game_result`。
10. Web 排行榜展示。

暂不做：

1. 塔升级。
2. 塔出售。
3. 掉线重连。
4. 多关卡选择。
5. 多种复杂怪物技能。
6. 多种复杂塔效果。
7. 复杂排行榜筛选。
8. 在线人数仪表盘。
9. 精美特效。

---

## 15. PM 强制约定

1. Unity 不做权威计算。
2. Unity 不直接扣金币。
3. Unity 不直接判断建塔成功。
4. Unity 不直接判断怪物死亡。
5. Unity 不直接判断胜负。
6. 所有最终结果以 Server 广播为准。
7. `game_result` 不存 `username`。
8. `time_used` 不写入数据库。
9. 5.22 只使用 1 个关卡、1 种塔、1 种怪物、3 条出怪事件进行联调。
