# 《协同防线》数据库设计文档

> 版本：v1.0  
> 适用阶段：5.22 数据互通与 MVP 联调  
> 维护人：李潇涵、崔国庆  
> 目标：统一 Web 后台、MySQL 数据库、Server 读取配置、Server 写入排行榜结果之间的数据字段。

---

## 1. 数据库设计目标

本数据库用于支撑《协同防线》MVP 的最小闭环：

```text
Web 后台录入配置
→ MySQL 保存塔、怪物、关卡、出怪时间轴
→ Server 开局读取配置
→ Server / 核心逻辑进行权威计算
→ 游戏结束写入 game_result
→ Web 后台展示排行榜
```

MVP 阶段数据库只保存：

1. 玩家信息。
2. 防御塔配置。
3. 怪物配置。
4. 关卡配置。
5. 出怪时间轴。
6. 游戏结算结果。

数据库不保存：

1. 战斗中每只怪物的实时坐标。
2. 战斗中每座塔的实时状态。
3. 每一帧的游戏状态。
4. 复杂日志。
5. 掉线重连状态。
6. 塔升级、出售记录。

---

## 2. 表总览

| 表名 | 中文含义 | MVP 用途 | 主要负责人 |
|---|---|---|---|
| player | 玩家表 | 保存玩家 ID、昵称、登录相关信息 | Web/DB + Server |
| tower | 防御塔配置表 | 后台配置塔造价、攻击力、射程、冷却 | Web/DB |
| monster | 怪物配置表 | 后台配置怪物血量、速度、奖励 | Web/DB |
| level | 关卡配置表 | 后台配置基地血量、初始金币、金币增长 | Web/DB |
| wave_event | 出怪事件表 | 配置第几秒生成什么怪、生成几只 | Web/DB |
| game_result | 游戏结果表 | 游戏结束后保存个人成绩，用于排行榜 | Server + Web/DB |

---

## 3. 表结构设计

### 3.1 player 玩家表

表名：`player`

用途：保存玩家基础信息。MVP 阶段 Unity 只输入昵称，不做正式账号密码登录。

| 字段名 | 类型 | 键 | 是否必须 | 说明 |
|---|---|---|---|---|
| player_id | INT | PK | 是 | 玩家 ID，自增主键 |
| username | VARCHAR(50) |  | 是 | 玩家昵称，Unity 登录后显示 |
| password_hash | VARCHAR(255) |  | 否 | 密码哈希，MVP 可暂不校验 |
| created_at | DATETIME |  | 是 | 创建时间 |

MVP 登录规则：

1. Unity 只发送 `username`。
2. Server 收到后查询 `player.username`。
3. 如果已存在，返回已有 `player_id`。
4. 如果不存在，可自动创建玩家。
5. `password_hash` MVP 阶段可填空字符串或测试值，不参与登录判断。

---

### 3.2 tower 防御塔配置表

表名：`tower`

用途：由 Web 后台配置防御塔属性，Server 开局读取，用于建塔判断和塔攻击计算。

| 字段名 | 类型 | 键 | 是否必须 | 说明 |
|---|---|---|---|---|
| tower_id | INT | PK | 是 | 防御塔 ID，自增主键 |
| name | VARCHAR(50) |  | 是 | 防御塔名称 |
| attack | INT |  | 是 | 攻击力 |
| range | INT |  | 是 | 攻击范围 |
| cooldown | FLOAT |  | 是 | 攻击冷却时间，单位秒 |
| cost | INT |  | 是 | 建造消耗金币 |
| refund_rate | FLOAT |  | 否 | 出售退款比例，MVP 暂不使用 |

MVP 使用字段：

```text
tower_id
name
attack
range
cooldown
cost
```

`refund_rate` 只作为预留字段，不开发塔出售功能。

---

### 3.3 monster 怪物配置表

表名：`monster`

用途：由 Web 后台配置怪物属性，Server / 核心逻辑根据 `wave_event.monster_id` 生成怪物实例。

| 字段名 | 类型 | 键 | 是否必须 | 说明 |
|---|---|---|---|---|
| monster_id | INT | PK | 是 | 怪物 ID，自增主键 |
| name | VARCHAR(50) |  | 是 | 怪物名称 |
| hp | INT |  | 是 | 怪物初始血量 |
| speed | FLOAT |  | 是 | 怪物移动速度 |
| reward_gold | INT |  | 是 | 击杀后奖励金币 |
| score | INT |  | 是 | 击杀后奖励分数 |

MVP 不做复杂怪物技能，只使用普通怪属性。

---

### 3.4 level 关卡配置表

表名：`level`

用途：配置关卡基础参数。MVP 阶段只需要 1 个关卡。

| 字段名 | 类型 | 键 | 是否必须 | 说明 |
|---|---|---|---|---|
| level_id | INT | PK | 是 | 关卡 ID，自增主键 |
| name | VARCHAR(50) |  | 是 | 关卡名称 |
| base_hp | INT |  | 是 | 基地初始血量 |
| initial_gold | INT |  | 是 | 每个玩家初始金币 |
| gold_per_second | INT |  | 是 | 每秒自动增长金币 |

MVP 默认使用：

```text
level_id = 1
```

---

### 3.5 wave_event 出怪事件表

表名：`wave_event`

用途：配置某个关卡中第几秒生成什么怪物、生成几只、间隔多久生成。

| 字段名 | 类型 | 键 | 是否必须 | 说明 |
|---|---|---|---|---|
| event_id | INT | PK | 是 | 出怪事件 ID，自增主键 |
| level_id | INT | FK | 是 | 所属关卡，对应 `level.level_id` |
| wave_number | INT |  | 是 | 第几波 |
| spawn_time_sec | INT |  | 是 | 开局后第几秒开始出怪 |
| monster_id | INT | FK | 是 | 怪物类型，对应 `monster.monster_id` |
| count | INT |  | 是 | 本次事件生成数量 |
| interval | INT |  | 是 | 每只怪物生成间隔，单位秒 |

关系：

```text
wave_event.level_id   → level.level_id
wave_event.monster_id → monster.monster_id
```

注意：`interval` 在 MySQL 中可能与关键字接近，写 SQL 时建议使用反引号：`interval`。

---

### 3.6 game_result 游戏结果表

表名：`game_result`

用途：游戏结束后，Server 为每个玩家写入一条成绩记录。Web 后台通过该表展示排行榜。

| 字段名 | 类型 | 键 | 是否必须 | 说明 |
|---|---|---|---|---|
| result_id | INT | PK | 是 | 结果 ID，自增主键 |
| player_id | INT | FK | 是 | 玩家 ID，对应 `player.player_id` |
| level_id | INT | FK | 是 | 关卡 ID，对应 `level.level_id` |
| score | INT |  | 是 | 玩家最终分数 |
| is_win | TINYINT(1) |  | 是 | 是否胜利，1 胜利，0 失败 |
| played_at | DATETIME |  | 是 | 游戏结束时间 |
| game_id | INT |  | 是 | 一局游戏 ID，同一局 4 名玩家相同 |
| kill_count | INT |  | 是 | 玩家击杀数量 |
| room_code | VARCHAR(10) |  | 是 | 房间号 |

关系：

```text
game_result.player_id → player.player_id
game_result.level_id  → level.level_id
```

重要约定：

1. `game_result` 不存 `username`。
2. 排行榜展示用户名时，通过 `player_id` 关联 `player.username`。
3. `game_result` 不存 `time_used`。
4. `time_used` 只在 `game_over` 中给 Unity 结算界面显示。
5. 同一局游戏中，每个玩家一条 `game_result` 记录。

---

## 4. 表关系说明

```text
level 1 ──── N wave_event
monster 1 ── N wave_event
player 1 ─── N game_result
level 1 ──── N game_result
```

说明：

1. 一个关卡可以有多条出怪事件。
2. 一种怪物可以被多条出怪事件使用。
3. 一个玩家可以有多条游戏成绩。
4. 一个关卡可以产生多条游戏成绩。
5. `tower` 当前是独立配置表，不需要和其他表建立外键。
6. `game_id` 用于标识同一局游戏，但不单独建表。

---

## 5. Server 读取与写入边界

### 5.1 登录阶段

Server 需要访问：

```text
player
```

用途：

```text
根据 username 查询或创建 player，返回 player_id。
```

---

### 5.2 开始游戏阶段

Server 收到 `start_game_request` 后读取：

```text
level
tower
monster
wave_event
```

读取用途：

| 表名 | 用途 |
|---|---|
| level | 初始化基地血量、玩家初始金币、金币增长 |
| tower | 建塔消耗、攻击力、射程、冷却 |
| monster | 怪物血量、速度、击杀奖励 |
| wave_event | 出怪时间轴 |

---

### 5.3 战斗过程阶段

战斗过程主要使用 Server 内存状态，不频繁写数据库。

运行时状态包括：

```text
玩家当前金币
玩家当前分数
玩家当前击杀数
基地当前血量
怪物实例位置
怪物实例血量
塔实例位置
地块占用状态
```

这些状态不需要单独建表。

---

### 5.4 游戏结束阶段

Server 需要写入：

```text
game_result
```

每个玩家写入一条记录：

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

---

## 6. Web 后台录入要求

Web 后台今天必须支持录入和查看以下表：

```text
level
tower
monster
wave_event
game_result
```

后台必须做到：

1. 可以新增、修改、删除防御塔配置。
2. 可以新增、修改、删除怪物配置。
3. 可以新增、修改、删除关卡配置。
4. 可以新增、修改、删除出怪事件。
5. 可以查看游戏结果。
6. 可以按分数展示排行榜。

MVP 阶段不要求复杂筛选，只需要按分数排序。

---

## 7. 5.22 最小测试数据

### 7.1 level 表

| level_id | name | base_hp | initial_gold | gold_per_second |
|---|---|---:|---:|---:|
| 1 | 第一关 | 100 | 300 | 1 |

---

### 7.2 tower 表

| tower_id | name | attack | range | cooldown | cost | refund_rate |
|---|---|---:|---:|---:|---:|---:|
| 1 | 基础塔 | 10 | 3 | 1.0 | 100 | 0.5 |

说明：今天只用 `tower_id = 1` 联调，不新增复杂塔。

---

### 7.3 monster 表

| monster_id | name | hp | speed | reward_gold | score |
|---|---|---:|---:|---:|---:|
| 1 | 普通怪 | 50 | 1.5 | 10 | 20 |

说明：今天只用 `monster_id = 1` 联调，不做复杂怪物技能。

---

### 7.4 wave_event 表

| event_id | level_id | wave_number | spawn_time_sec | monster_id | count | interval |
|---|---:|---:|---:|---:|---:|---:|
| 1 | 1 | 1 | 3 | 1 | 3 | 1 |
| 2 | 1 | 2 | 10 | 1 | 5 | 1 |
| 3 | 1 | 3 | 20 | 1 | 8 | 1 |

用于测试：

```text
第 3 秒开始出 3 只怪
第 10 秒开始出 5 只怪
第 20 秒开始出 8 只怪
```

---

### 7.5 player 表

如果 Server 已实现昵称登录自动创建玩家，可以不手动录入。

如果 Server 暂时没有自动创建，先手动录入：

| player_id | username | password_hash | created_at |
|---|---|---|---|
| 1 | 李潇涵 | test | 2026-05-22 20:00:00 |
| 2 | 雅丽娜 | test | 2026-05-22 20:00:00 |
| 3 | 范慧轩 | test | 2026-05-22 20:00:00 |
| 4 | 颜欣沂 | test | 2026-05-22 20:00:00 |

---

### 7.6 game_result 表

先手动录入几条，用于测试排行榜页面：

| result_id | player_id | level_id | score | is_win | played_at | game_id | kill_count | room_code |
|---|---:|---:|---:|---:|---|---:|---:|---|
| 1 | 1 | 1 | 120 | 1 | 2026-05-22 20:00:00 | 10001 | 6 | A001 |
| 2 | 2 | 1 | 80 | 1 | 2026-05-22 20:00:00 | 10001 | 4 | A001 |
| 3 | 3 | 1 | 60 | 0 | 2026-05-22 20:05:00 | 10002 | 3 | A002 |

---

## 8. 排行榜规则

MVP 排行榜按以下规则排序：

```sql
ORDER BY score DESC, played_at ASC
```

含义：

1. 分数高的排前面。
2. 分数相同，先完成的排前面。

排行榜展示字段建议：

```text
rank
username
score
kill_count
is_win
level_name
room_code
played_at
```

排行榜查询需要关联：

```text
game_result.player_id = player.player_id
game_result.level_id = level.level_id
```

---

## 9. 建表 SQL 建议

```sql
CREATE TABLE player (
  player_id INT PRIMARY KEY AUTO_INCREMENT,
  username VARCHAR(50) NOT NULL,
  password_hash VARCHAR(255) DEFAULT '',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE tower (
  tower_id INT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(50) NOT NULL,
  attack INT NOT NULL,
  `range` INT NOT NULL,
  cooldown FLOAT NOT NULL,
  cost INT NOT NULL,
  refund_rate FLOAT DEFAULT 0.5
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE monster (
  monster_id INT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(50) NOT NULL,
  hp INT NOT NULL,
  speed FLOAT NOT NULL,
  reward_gold INT NOT NULL,
  score INT NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE level (
  level_id INT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(50) NOT NULL,
  base_hp INT NOT NULL,
  initial_gold INT NOT NULL,
  gold_per_second INT NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE wave_event (
  event_id INT PRIMARY KEY AUTO_INCREMENT,
  level_id INT NOT NULL,
  wave_number INT NOT NULL,
  spawn_time_sec INT NOT NULL,
  monster_id INT NOT NULL,
  count INT NOT NULL,
  `interval` INT NOT NULL,
  CONSTRAINT fk_wave_level FOREIGN KEY (level_id) REFERENCES level(level_id),
  CONSTRAINT fk_wave_monster FOREIGN KEY (monster_id) REFERENCES monster(monster_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE game_result (
  result_id INT PRIMARY KEY AUTO_INCREMENT,
  player_id INT NOT NULL,
  level_id INT NOT NULL,
  score INT NOT NULL,
  is_win TINYINT(1) NOT NULL,
  played_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  game_id INT NOT NULL,
  kill_count INT NOT NULL DEFAULT 0,
  room_code VARCHAR(10) NOT NULL,
  CONSTRAINT fk_result_player FOREIGN KEY (player_id) REFERENCES player(player_id),
  CONSTRAINT fk_result_level FOREIGN KEY (level_id) REFERENCES level(level_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

说明：

1. SQL 中 `range` 和 `interval` 使用反引号，避免与数据库关键字或函数产生冲突。
2. 如果 Django Model 使用字段名 `range` 或 `interval`，也要注意迁移生成的 SQL 是否正常。
3. MVP 阶段不单独建立 `game` 表。

---

## 10. 初始测试数据 SQL 建议

```sql
INSERT INTO level (level_id, name, base_hp, initial_gold, gold_per_second)
VALUES (1, '第一关', 100, 300, 1);

INSERT INTO tower (tower_id, name, attack, `range`, cooldown, cost, refund_rate)
VALUES (1, '基础塔', 10, 3, 1.0, 100, 0.5);

INSERT INTO monster (monster_id, name, hp, speed, reward_gold, score)
VALUES (1, '普通怪', 50, 1.5, 10, 20);

INSERT INTO wave_event (event_id, level_id, wave_number, spawn_time_sec, monster_id, count, `interval`)
VALUES
(1, 1, 1, 3, 1, 3, 1),
(2, 1, 2, 10, 1, 5, 1),
(3, 1, 3, 20, 1, 8, 1);

INSERT INTO player (player_id, username, password_hash, created_at)
VALUES
(1, '李潇涵', 'test', '2026-05-22 20:00:00'),
(2, '雅丽娜', 'test', '2026-05-22 20:00:00'),
(3, '范慧轩', 'test', '2026-05-22 20:00:00'),
(4, '颜欣沂', 'test', '2026-05-22 20:00:00');

INSERT INTO game_result (result_id, player_id, level_id, score, is_win, played_at, game_id, kill_count, room_code)
VALUES
(1, 1, 1, 120, 1, '2026-05-22 20:00:00', 10001, 6, 'A001'),
(2, 2, 1, 80, 1, '2026-05-22 20:00:00', 10001, 4, 'A001'),
(3, 3, 1, 60, 0, '2026-05-22 20:05:00', 10002, 3, 'A002');
```

---

## 11. Django Model 字段对应建议

如果 Web 后台使用 Django Admin，建议模型名称如下：

| 数据库表 | Django Model 建议名称 |
|---|---|
| player | Player |
| tower | Tower |
| monster | Monster |
| level | Level |
| wave_event | WaveEvent |
| game_result | GameResult |

`GameResult` 中建议通过外键关联：

```text
player → Player
level  → Level
```

排行榜页面查询时使用：

```text
GameResult.objects.select_related('player', 'level').order_by('-score', 'played_at')
```

---

## 12. 5.22 验收标准

Web/数据库同学今晚必须交付：

1. 数据库表已创建。
2. Django Admin 可以打开。
3. `level` 有 1 条测试数据。
4. `tower` 有 1 条测试数据。
5. `monster` 有 1 条测试数据。
6. `wave_event` 有 3 条测试数据。
7. `game_result` 可以写入和展示。
8. 排行榜能按 `score DESC, played_at ASC` 排序。
9. Server 能读取 `level`、`tower`、`monster`、`wave_event`。
10. Server 能在游戏结束后写入 `game_result`。

---

## 13. PM 风险提醒

1. 今天不要再改表结构，除非字段名明显错误。
2. 不要给 `game_result` 加 `username`，否则会和 `player` 表重复。
3. 不要要求数据库保存怪物实时坐标和塔实时坐标。
4. 不要做多关卡配置页面，先只用 `level_id = 1`。
5. 不要做复杂排行榜筛选，先按分数展示。
6. `tower.refund_rate` 保留但不使用。
7. `player.password_hash` 保留但 MVP 不校验。
