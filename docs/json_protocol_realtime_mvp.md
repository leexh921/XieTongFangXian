# 《协同防线》JSON 通信协议文档（MVP 单人实时配置版）

> 版本：v2.0  
> 通信方式：WebSocket + Web 后端事件通知  
> 当前模式：单人塔防 MVP  
> 核心目标：Web 后台修改数据库后，Unity 能通过 Server 实时收到配置更新并刷新表现。  
> 核心原则：Unity 只负责表现和操作入口，Server 负责权威计算，Web 后台负责配置数据修改。

---

## 1. 系统通信关系

本项目涉及三类通信：

```text
Unity 客户端  ←WebSocket→  游戏逻辑 Server
Web 后台      →事件通知→    游戏逻辑 Server
Web 后台      ←→            MySQL 数据库
游戏逻辑 Server ←→          MySQL 数据库
```

注意：

1. Unity 不直接连接数据库。
2. Unity 不直接连接 Web 后台。
3. Web 后台修改配置后，必须通知 Server。
4. Server 收到配置变更事件后，重新读取数据库。
5. Server 再通过 WebSocket 向 Unity 推送最新配置。
6. Unity 根据 Server 推送的 `config_update` 实时刷新显示。

---

## 2. 数据流总览

### 2.1 开始游戏数据流

```text
Unity 输入昵称
        ↓
Unity 发送 login_request
        ↓
Server 返回 login_success
        ↓
Unity 发送 start_game
        ↓
Server 读取 MySQL 中的 level / tower / monster / wave_event
        ↓
Server 返回 game_config
        ↓
Unity 初始化战斗场景
        ↓
Server 按时间轴推送 state_update
```

### 2.2 实时配置更新数据流

```text
Web 后台修改 tower / monster / level / wave_event
        ↓
Web 后台保存数据到 MySQL
        ↓
Web 后端触发 config_changed 事件
        ↓
Server 收到事件后重新读取数据库
        ↓
Server 更新运行中的配置缓存
        ↓
Server 推送 config_update 给 Unity
        ↓
Unity 立即刷新塔面板、局内提示和后续表现
```

### 2.3 游戏结算数据流

```text
Server 判断游戏结束
        ↓
Server 生成 game_over
        ↓
Server 写入 game_result
        ↓
Unity 显示结算界面
        ↓
Web 后台读取 game_result 展示排行榜
```

---

## 3. 消息类型总览

| type | 方向 | 作用 |
|---|---|---|
| `login_request` | Unity → Server | 玩家输入昵称并登录 |
| `login_success` | Server → Unity | 登录成功 |
| `login_failed` | Server → Unity | 登录失败 |
| `start_game` | Unity → Server | 请求开始单人游戏 |
| `game_config` | Server → Unity | 下发初始关卡、塔、怪物、出怪配置 |
| `config_changed` | Web 后台 → Server | 通知 Server 数据库配置已变化 |
| `config_update` | Server → Unity | 通知 Unity 配置已实时更新 |
| `build_request` | Unity → Server | 请求在某地块建塔 |
| `build_result` | Server → Unity | 返回建塔成功或失败 |
| `state_update` | Server → Unity | 同步怪物、塔、金币、分数、基地血量 |
| `game_over` | Server → Unity | 游戏结束结算 |
| `error` | Server → Unity | 通用错误消息 |

---

## 4. 字段命名规范

统一使用 **小写 + 下划线**，并尽量与数据库字段保持一致。

推荐：

```text
range_value
gold_per_second
wave_number
spawn_time
interval_time
kill_count
```

不推荐混用：

```text
range
goldPerSecond
waveNumber
spawnTime
```

---

## 5. 通用字段说明

| 字段 | 类型 | 说明 |
|---|---|---|
| `type` | string | 消息类型，所有消息必须包含 |
| `username` | string | 玩家昵称 |
| `game_id` | string | 本局游戏 ID |
| `level_id` | int | 关卡 ID |
| `level_name` | string | 关卡名称 |
| `tower_id` | int | 防御塔 ID |
| `monster_id` | int | 怪物实例 ID或怪物配置 ID，按上下文区分 |
| `monster_type` | int | 怪物类型 ID，对应数据库 `monster.monster_id` |
| `tile_id` | int | 地块 ID |
| `wave_number` | int | 当前波次 |
| `success` | bool | 是否成功 |
| `reason` | string | 失败原因 |
| `score` | int | 玩家分数 |
| `kill_count` | int | 击杀数量 |
| `gold` | int/float | 当前金币 |
| `gold_left` | int/float | 建塔后剩余金币 |
| `base_hp` | int | 基地血量 |
| `time` | float | 当前游戏时间 |
| `time_used` | int | 本局用时，单位秒 |
| `is_win` | bool | 是否胜利 |

---

# 6. 登录流程

## 6.1 Unity → Server：login_request

玩家输入昵称后，Unity 发送登录请求。

```json
{
  "type": "login_request",
  "username": "playerA"
}
```

### 字段说明

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `type` | string | 是 | 固定为 `login_request` |
| `username` | string | 是 | 玩家昵称 |

---

## 6.2 Server → Unity：login_success

```json
{
  "type": "login_success",
  "username": "playerA"
}
```

### Unity 收到后

1. 保存 `username` 到 `GameManager`。
2. 显示连接成功。
3. 进入开始游戏界面。

---

## 6.3 Server → Unity：login_failed

```json
{
  "type": "login_failed",
  "reason": "username_empty"
}
```

### 常见 reason

| reason | 说明 |
|---|---|
| `username_empty` | 昵称为空 |
| `server_error` | 服务端异常 |

---

# 7. 开始游戏流程

## 7.1 Unity → Server：start_game

Unity 点击“开始游戏”后发送。

```json
{
  "type": "start_game",
  "username": "playerA",
  "level_id": 1
}
```

### 字段说明

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `type` | string | 是 | 固定为 `start_game` |
| `username` | string | 是 | 玩家昵称 |
| `level_id` | int | 是 | 关卡 ID，MVP 默认 1 |

---

## 7.2 Server → Unity：game_config

Server 收到 `start_game` 后，从数据库读取 `level`、`tower`、`monster`、`wave_event`，并下发初始配置。

```json
{
  "type": "game_config",
  "game_id": "G202605220001",
  "level": {
    "level_id": 1,
    "level_name": "第一关",
    "initial_gold": 100,
    "base_hp": 10,
    "gold_per_second": 1.0
  },
  "towers": [
    {
      "tower_id": 1,
      "name": "普通塔",
      "cost": 50,
      "attack": 20,
      "range_value": 3.0,
      "cooldown": 1.0,
      "refund_rate": 0.5,
      "description": "基础防御塔"
    }
  ],
  "monsters": [
    {
      "monster_id": 1,
      "name": "小怪",
      "hp": 100,
      "speed": 1.0,
      "score_value": 100,
      "reward_gold": 10,
      "damage_to_base": 1
    }
  ],
  "wave_events": [
    {
      "event_id": 1,
      "level_id": 1,
      "wave_number": 1,
      "spawn_time": 3.0,
      "monster_id": 1,
      "count": 3,
      "interval_time": 0.5
    }
  ]
}
```

### Unity 收到后

1. 保存 `game_id`。
2. 初始化金币、基地血量。
3. 根据 `towers` 生成或刷新防御塔选择按钮。
4. 保存 `monsters` 配置，用于显示怪物类型。
5. 可以显示即将到来的波次信息。
6. 进入战斗场景。

---

# 8. 实时配置更新流程

## 8.1 Web 后台 → Server：config_changed

Web 后台保存 `tower`、`monster`、`level` 或 `wave_event` 后，向 Server 发送配置变更事件。

```json
{
  "type": "config_changed",
  "table": "tower",
  "action": "update",
  "level_id": 1,
  "changed_ids": [1],
  "changed_at": "2026-05-22 15:30:00"
}
```

### 字段说明

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `type` | string | 是 | 固定为 `config_changed` |
| `table` | string | 是 | 变化的表名：`tower`、`monster`、`level`、`wave_event` |
| `action` | string | 是 | 操作类型：`create`、`update`、`delete` |
| `level_id` | int | 否 | 影响的关卡，默认 1 |
| `changed_ids` | array | 否 | 变化的数据 ID 列表 |
| `changed_at` | string | 否 | 修改时间 |

### Server 收到后

1. 不直接相信消息里的配置值。
2. 根据 `table` 和 `level_id` 重新读取数据库。
3. 更新 Server 内部配置缓存。
4. 按配置生效规则更新当前游戏逻辑。
5. 向 Unity 推送 `config_update`。

---

## 8.2 Server → Unity：config_update

Server 重新读取数据库并更新缓存后，推送最新配置给 Unity。

```json
{
  "type": "config_update",
  "level_id": 1,
  "changed_table": "tower",
  "level": {
    "level_id": 1,
    "level_name": "第一关",
    "initial_gold": 100,
    "base_hp": 10,
    "gold_per_second": 1.0
  },
  "towers": [
    {
      "tower_id": 1,
      "name": "普通塔",
      "cost": 40,
      "attack": 30,
      "range_value": 3.5,
      "cooldown": 1.0,
      "refund_rate": 0.5,
      "description": "后台实时更新后的普通塔"
    }
  ],
  "monsters": [
    {
      "monster_id": 1,
      "name": "小怪",
      "hp": 100,
      "speed": 1.0,
      "score_value": 100,
      "reward_gold": 10,
      "damage_to_base": 1
    }
  ],
  "wave_events": [
    {
      "event_id": 1,
      "level_id": 1,
      "wave_number": 1,
      "spawn_time": 3.0,
      "monster_id": 1,
      "count": 3,
      "interval_time": 0.5
    }
  ],
  "message": "后台配置已更新"
}
```

### Unity 收到后

1. 刷新塔选择面板，例如价格、攻击力、射程。
2. 刷新怪物配置缓存。
3. 刷新未来出怪事件提示。
4. 显示提示文本，例如“后台配置已更新”。
5. 后续显示和状态渲染以新配置为准。

---

## 8.3 配置实时生效规则

| 配置项 | 是否实时生效 | 规则 |
|---|---|---|
| `tower.cost` | 是 | 下次建塔使用新价格 |
| `tower.attack` | 是 | 已建塔后续攻击使用新攻击力 |
| `tower.range_value` | 是 | 已建塔后续索敌使用新射程 |
| `tower.cooldown` | 是 | 已建塔后续攻击使用新冷却 |
| `tower.refund_rate` | 暂不使用 | MVP 不做卖塔，字段保留 |
| `monster.speed` | 是 | 场上同类型怪物后续移动可按新速度 |
| `monster.hp` | 部分 | 新生成怪物使用新血量；已在场怪物不回满血 |
| `monster.score_value` | 是 | 后续击杀按新分数 |
| `monster.reward_gold` | 是 | 后续击杀按新金币奖励 |
| `monster.damage_to_base` | 是 | 后续到达终点的怪物按新扣血值 |
| `wave_event` | 是 | 只影响尚未触发的未来出怪事件 |
| `level.gold_per_second` | 是 | 后续金币自然增长按新值 |
| `level.initial_gold` | 否 | 下一局生效 |
| `level.base_hp` | 否 | 下一局生效 |

---

# 9. 建塔流程

## 9.1 Unity → Server：build_request

玩家点击地块后，Unity 发送建塔请求。

```json
{
  "type": "build_request",
  "username": "playerA",
  "tile_id": 12,
  "tower_id": 1
}
```

### 字段说明

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `type` | string | 是 | 固定为 `build_request` |
| `username` | string | 是 | 玩家昵称 |
| `tile_id` | int | 是 | 被点击地块 ID |
| `tower_id` | int | 是 | 选择建造的塔 ID |

### Unity 注意事项

Unity 点击地块后不能直接生成塔。  
必须等待 Server 返回 `build_result`，且 `success = true` 后，才能生成 `Tower.prefab`。

---

## 9.2 Server → Unity：build_result 成功

```json
{
  "type": "build_result",
  "success": true,
  "tile_id": 12,
  "tower_id": 1,
  "gold_left": 50
}
```

### Unity 收到后

1. 在 `tile_id = 12` 的地块上生成 `Tower.prefab`。
2. 更新金币为 `gold_left`。
3. 将该地块标记为已占用。

---

## 9.3 Server → Unity：build_result 失败

```json
{
  "type": "build_result",
  "success": false,
  "tile_id": 12,
  "reason": "gold_not_enough"
}
```

### 常见 reason

| reason | 说明 |
|---|---|
| `gold_not_enough` | 金币不足 |
| `tile_occupied` | 地块已被占用 |
| `invalid_tower` | 塔 ID 不存在或未启用 |
| `invalid_tile` | 地块 ID 不合法 |
| `game_not_started` | 游戏尚未开始 |

---

# 10. 状态同步流程

## 10.1 Server → Unity：state_update

Server 定时发送给 Unity。  
MVP 阶段推荐每秒 5-10 次；如果不稳定，可以先每秒 2-5 次。

```json
{
  "type": "state_update",
  "game_id": "G202605220001",
  "time": 12.5,
  "level_id": 1,
  "wave_number": 1,
  "base_hp": 10,
  "player": {
    "username": "playerA",
    "gold": 65,
    "score": 300,
    "kill_count": 2
  },
  "monsters": [
    {
      "monster_id": 101,
      "monster_type": 1,
      "x": 3.5,
      "y": 6.2,
      "hp": 80,
      "max_hp": 100
    }
  ],
  "towers": [
    {
      "tile_id": 12,
      "tower_id": 1
    }
  ]
}
```

### 字段说明

| 字段 | 类型 | 说明 |
|---|---|---|
| `game_id` | string | 本局游戏 ID |
| `time` | float | 当前游戏时间 |
| `level_id` | int | 当前关卡 |
| `wave_number` | int | 当前波次 |
| `base_hp` | int | 当前基地血量 |
| `player` | object | 当前玩家状态 |
| `monsters` | array | 当前场上怪物列表 |
| `towers` | array | 当前场上防御塔列表 |

### player 字段说明

| 字段 | 类型 | 说明 |
|---|---|---|
| `username` | string | 玩家昵称 |
| `gold` | int/float | 当前金币 |
| `score` | int | 当前得分 |
| `kill_count` | int | 当前击杀数 |

### monsters 字段说明

| 字段 | 类型 | 说明 |
|---|---|---|
| `monster_id` | int | 怪物实例 ID |
| `monster_type` | int | 怪物类型 ID，对应数据库 `monster.monster_id` |
| `x` | float | Unity 2D 横坐标 |
| `y` | float | Unity 2D 纵坐标 |
| `hp` | int | 当前血量 |
| `max_hp` | int | 最大血量 |

### towers 字段说明

| 字段 | 类型 | 说明 |
|---|---|---|
| `tile_id` | int | 地块 ID |
| `tower_id` | int | 防御塔 ID |

### Unity 收到后

1. 根据 `monster_id` 判断怪物是否已经存在。
2. 不存在则生成 `Monster.prefab`。
3. 已存在则更新怪物位置和血量。
4. 如果某个旧怪物不在最新 `monsters` 列表中，则删除或隐藏。
5. 更新金币、分数、击杀数、基地血量 UI。
6. 根据 `towers` 确保场上塔显示正确。

---

# 11. 游戏结束流程

## 11.1 Server → Unity：game_over

Server 判断游戏结束后发送给 Unity，同时写入数据库 `game_result`。

```json
{
  "type": "game_over",
  "game_id": "G202605220001",
  "username": "playerA",
  "level_id": 1,
  "score": 2600,
  "kill_count": 12,
  "time_used": 180,
  "is_win": true
}
```

### 字段说明

| 字段 | 类型 | 说明 |
|---|---|---|
| `game_id` | string | 本局游戏 ID |
| `username` | string | 玩家昵称 |
| `level_id` | int | 关卡 ID |
| `score` | int | 最终得分 |
| `kill_count` | int | 击杀数量 |
| `time_used` | int | 本局用时，单位秒 |
| `is_win` | bool | 是否胜利 |

### Server 写入 game_result

对应数据库字段：

```text
game_id
username
level_id
score
kill_count
time_used
is_win
played_at
```

其中 `played_at` 由数据库默认生成。

### Unity 收到后

1. 停止局内操作。
2. 显示结算界面。
3. 展示胜负、得分、击杀数和用时。
4. 提示排行榜可在 Web 后台查看。

---

# 12. 通用错误消息

## 12.1 Server → Unity：error

```json
{
  "type": "error",
  "reason": "unknown_message_type",
  "message": "未知消息类型"
}
```

### 常见 reason

| reason | 说明 |
|---|---|
| `unknown_message_type` | 未知消息类型 |
| `invalid_json` | JSON 格式错误 |
| `server_error` | 服务端异常 |
| `database_error` | 数据库读取失败 |
| `config_reload_failed` | 配置重载失败 |

---

# 13. 当前不实现的协议

MVP 单人版不使用以下多人协议：

```text
create_room
join_room
room_update
ready
player_list
room_code
```

MVP 暂不实现以下扩展协议：

```text
sell_tower_request
sell_tower_result
upgrade_tower_request
upgrade_tower_result
```

说明：

1. 当前不做多人房间，所以不需要 `create_room`、`join_room`。
2. 当前不做卖塔和升级塔，所以 `refund_rate` 只作为数据库保留字段。
3. 当前重点是 Web 实时配置、建塔、出怪、攻击、结算和排行榜闭环。

---

# 14. 推荐联调顺序

1. Unity 连接 Server。
2. Unity 发送 `login_request`。
3. Server 返回 `login_success`。
4. Unity 发送 `start_game`。
5. Server 读取数据库配置。
6. Server 返回 `game_config`。
7. Server 开始发送 `state_update`。
8. Unity 显示怪物和 UI。
9. Unity 点击地块发送 `build_request`。
10. Server 返回 `build_result`。
11. Unity 显示塔。
12. Web 后台修改 `tower.attack`。
13. Web 后台触发 `config_changed`。
14. Server 重新读取数据库。
15. Server 推送 `config_update`。
16. Unity 立即刷新塔配置显示。
17. Server 后续攻击按新配置计算。
18. Server 判断游戏结束并发送 `game_over`。
19. Server 写入 `game_result`。
20. Web 后台查看排行榜。

---

# 15. 最小验收标准

JSON 协议部分验收时，需要满足：

1. Unity 能发送 `login_request`。
2. Server 能返回 `login_success`。
3. Unity 能发送 `start_game`。
4. Server 能返回 `game_config`，且字段包含 `gold_per_second`、`refund_rate`、`wave_number`。
5. Unity 能发送 `build_request`。
6. Server 能返回 `build_result`。
7. Server 能持续发送 `state_update`。
8. `state_update` 能包含金币、分数、击杀数、基地血量、当前波次。
9. Web 后台修改配置后能触发 `config_changed`。
10. Server 能推送 `config_update`。
11. Unity 能根据 `config_update` 立即刷新显示。
12. Server 能发送 `game_over`。
13. `game_over` 能包含 `level_id`、`kill_count`。
14. Server 能写入 `game_result`。
