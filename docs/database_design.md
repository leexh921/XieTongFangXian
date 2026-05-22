# 协同防线 — 数据库设计文档 v2.1

> 版本：v2.1（单人 MVP 版）
> 数据库：MySQL
> ORM：无（Server 通过 pymysql 直连）

---

## 1. 设计目标

本项目数据库服务于两类数据：

1. **配置数据** — Web 后台录入和维护，驱动游戏内容
2. **结果数据** — Server 在游戏结束后写入，Web 后台展示排行榜

---

## 2. 数据表总览

| 表名 | 作用 |
|---|---|
| `player` | 玩家基础信息（MVP 可选，也可 game_result 直存 username） |
| `tower` | 防御塔配置 |
| `monster` | 怪物配置 |
| `level` | 关卡基础配置 |
| `wave_event` | 出怪时间轴配置 |
| `game_result` | 游戏成绩与排行榜 |

---

## 3. 表结构

### 3.1 player — 玩家表

```sql
CREATE TABLE player (
    player_id  INT AUTO_INCREMENT PRIMARY KEY,
    username   VARCHAR(50) NOT NULL UNIQUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

| 字段 | 类型 | 说明 |
|---|---|---|
| `player_id` | INT | 主键，自增 |
| `username` | VARCHAR(50) | 玩家昵称，唯一 |
| `created_at` | DATETIME | 创建时间 |

---

### 3.2 tower — 防御塔配置表

```sql
CREATE TABLE tower (
    tower_id    INT AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(50)  NOT NULL,
    cost        INT          NOT NULL,
    attack      INT          NOT NULL,
    range_value FLOAT        NOT NULL             COMMENT '攻击范围',
    cooldown    FLOAT        NOT NULL DEFAULT 1.0 COMMENT '攻击间隔(秒)',
    refund_rate FLOAT        NOT NULL DEFAULT 0.5 COMMENT '出售返金比例(MVP 保留字段)',
    description VARCHAR(255),
    is_active   TINYINT(1)   NOT NULL DEFAULT 1   COMMENT '是否启用'
);
```

| 字段 | 类型 | 说明 |
|---|---|---|
| `tower_id` | INT | 主键 |
| `name` | VARCHAR(50) | 防御塔名称 |
| `cost` | INT | 建造消耗金币 |
| `attack` | INT | 攻击力 |
| `range_value` | FLOAT | 攻击范围 |
| `cooldown` | FLOAT | 攻击间隔，单位秒 |
| `refund_rate` | FLOAT | 出售返金比例（MVP 暂不出售功能） |
| `description` | VARCHAR(255) | 描述，可选 |
| `is_active` | TINYINT(1) | 1=启用，0=禁用 |

---

### 3.3 monster — 怪物配置表

```sql
CREATE TABLE monster (
    monster_id     INT AUTO_INCREMENT PRIMARY KEY,
    name           VARCHAR(50) NOT NULL,
    hp             INT         NOT NULL,
    speed          FLOAT       NOT NULL,
    score_value    INT         NOT NULL DEFAULT 100,
    reward_gold    INT         NOT NULL DEFAULT 10,
    damage_to_base INT         NOT NULL DEFAULT 1    COMMENT '到达终点扣基地血量',
    is_active      TINYINT(1)  NOT NULL DEFAULT 1
);
```

| 字段 | 类型 | 说明 |
|---|---|---|
| `monster_id` | INT | 主键 |
| `name` | VARCHAR(50) | 怪物名称 |
| `hp` | INT | 最大血量 |
| `speed` | FLOAT | 移动速度 |
| `score_value` | INT | 击杀得分 |
| `reward_gold` | INT | 击杀奖励金币 |
| `damage_to_base` | INT | 到达终点后基地扣血量 |
| `is_active` | TINYINT(1) | 1=启用，0=禁用 |

---

### 3.4 level — 关卡配置表

```sql
CREATE TABLE level (
    level_id        INT AUTO_INCREMENT PRIMARY KEY,
    level_name      VARCHAR(50)  NOT NULL,
    initial_gold    INT          NOT NULL DEFAULT 100,
    base_hp         INT          NOT NULL DEFAULT 10,
    gold_per_second FLOAT        NOT NULL DEFAULT 1   COMMENT '每秒金币增长',
    description     VARCHAR(255),
    is_active       TINYINT(1)   NOT NULL DEFAULT 1
);
```

| 字段 | 类型 | 说明 |
|---|---|---|
| `level_id` | INT | 主键 |
| `level_name` | VARCHAR(50) | 关卡名称 |
| `initial_gold` | INT | 初始金币 |
| `base_hp` | INT | 基地初始血量 |
| `gold_per_second` | FLOAT | 每秒金币增长 |
| `description` | VARCHAR(255) | 关卡说明 |
| `is_active` | TINYINT(1) | 1=启用，0=禁用 |

---

### 3.5 wave_event — 出怪时间轴表

```sql
CREATE TABLE wave_event (
    event_id      INT AUTO_INCREMENT PRIMARY KEY,
    level_id      INT       NOT NULL,
    wave_number   INT       NOT NULL DEFAULT 1,
    spawn_time    FLOAT     NOT NULL                  COMMENT '游戏开始后第几秒触发',
    monster_id    INT       NOT NULL,
    count         INT       NOT NULL DEFAULT 1,
    interval_time FLOAT     NOT NULL DEFAULT 0.5      COMMENT '多只怪之间间隔(秒)',
    is_active     TINYINT(1)NOT NULL DEFAULT 1,
    FOREIGN KEY (level_id)   REFERENCES level(level_id),
    FOREIGN KEY (monster_id) REFERENCES monster(monster_id)
);
```

| 字段 | 类型 | 说明 |
|---|---|---|
| `event_id` | INT | 主键 |
| `level_id` | INT | 外键，所属关卡 |
| `wave_number` | INT | 第几波 |
| `spawn_time` | FLOAT | 游戏开始后第几秒触发 |
| `monster_id` | INT | 外键，怪物类型 |
| `count` | INT | 生成数量 |
| `interval_time` | FLOAT | 多只怪之间生成间隔(秒) |
| `is_active` | TINYINT(1) | 1=启用，0=禁用 |

---

### 3.6 game_result — 游戏结果 / 排行榜表

```sql
CREATE TABLE game_result (
    result_id  INT AUTO_INCREMENT PRIMARY KEY,
    game_id    VARCHAR(36),
    username   VARCHAR(50) NOT NULL,
    level_id   INT         NOT NULL DEFAULT 1,
    score      INT         NOT NULL DEFAULT 0,
    kill_count INT         NOT NULL DEFAULT 0,
    time_used  INT         NOT NULL DEFAULT 0          COMMENT '本局用时(秒)',
    is_win     TINYINT(1)  NOT NULL DEFAULT 0,
    played_at  DATETIME    DEFAULT CURRENT_TIMESTAMP
);
```

| 字段 | 类型 | 说明 |
|---|---|---|
| `result_id` | INT | 主键 |
| `game_id` | VARCHAR(36) | 本局游戏 ID |
| `username` | VARCHAR(50) | 玩家昵称 |
| `level_id` | INT | 关卡 ID |
| `score` | INT | 本局得分 |
| `kill_count` | INT | 击杀数 |
| `time_used` | INT | 本局用时(秒) |
| `is_win` | TINYINT(1) | 是否胜利，1=胜利，0=失败 |
| `played_at` | DATETIME | 记录时间 |

---

## 4. 表关系

```
level  1 ──── N  wave_event
monster 1 ──── N  wave_event

game_result  独立记录每局结果
tower        独立配置表
monster      独立配置表
player       可选，通过 username 与 game_result 软关联
```

---

## 5. Server 操作数据库

### 5.1 开局读取

```sql
-- 关卡
SELECT * FROM level WHERE level_id = ? AND is_active = 1;

-- 防御塔
SELECT * FROM tower WHERE is_active = 1;

-- 怪物
SELECT * FROM monster WHERE is_active = 1;

-- 出怪时间轴
SELECT * FROM wave_event
WHERE level_id = ? AND is_active = 1
ORDER BY spawn_time ASC;
```

### 5.2 登录

```sql
-- 查询已有玩家
SELECT player_id, username FROM player WHERE username = ?;

-- 不存在则插入
INSERT INTO player (username) VALUES (?);
```

### 5.3 结算写入

```sql
INSERT INTO game_result (game_id, username, level_id, score, kill_count, time_used, is_win)
VALUES (?, ?, ?, ?, ?, ?, ?);
```

### 5.4 排行榜查询

```sql
SELECT username, level_id, score, kill_count, time_used, is_win, played_at
FROM game_result
ORDER BY score DESC, time_used ASC
LIMIT 100;
```

---

## 6. DB 列名 ↔ 协议 JSON 字段映射

Server 代码层做映射，DB 列名和协议 JSON 字段名不完全相同：

| DB 列 | 协议 JSON 字段 | 说明 |
|---|---|---|
| `tower.range_value` | `range` | 攻击范围 |
| `tower.cooldown` | `cooldown` | 攻击间隔 |
| `monster.score_value` | `score` | 击杀得分 |
| `monster.reward_gold` | `reward_gold` | 击杀奖励 |
| `monster.damage_to_base` | — | Server 内部使用 |
| `level.level_name` | `name` | 关卡名称 |
| `level.initial_gold` | `initial_gold` | 初始金币 |
| `wave_event.spawn_time` | — | Server 内部使用 |
| `wave_event.interval_time` | — | Server 内部使用 |
| `wave_event.wave_number` | — | Server 内部使用 |

---

## 7. 初始测试数据

```sql
INSERT INTO level    VALUES (1, '第一关', 300, 100, 1, 'MVP 测试关卡', 1);
INSERT INTO tower    VALUES (1, '基础塔', 100, 10, 3, 1.0, 0.5, '基础防御塔', 1);
INSERT INTO monster  VALUES (1, '普通怪', 50, 1.5, 20, 10, 1, 1);

INSERT INTO wave_event (level_id, wave_number, spawn_time, monster_id, count, interval_time, is_active)
VALUES
(1, 1, 5,  1, 3, 1, 1),
(1, 2, 15, 1, 5, 1, 1),
(1, 3, 30, 1, 4, 1, 1);
```

---

## 8. MVP 不做的内容

| 内容 | 原因 |
|---|---|
| 多人房间表 | 单人模式 |
| 复杂账号密码 | 昵称登录 |
| 塔升级/出售 | 非核心闭环 |
| 多关卡运营 | MVP 只 1 关 |
| 在线统计表 | 不影响主流程 |
| 复杂排行榜筛选 | score DESC 已够用 |
