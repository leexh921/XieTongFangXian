-- =====================================================
-- 协同防线 — 数据库建表脚本 (MVP 单人版)
-- =====================================================

DROP TABLE IF EXISTS game_result;
DROP TABLE IF EXISTS wave_event;
DROP TABLE IF EXISTS monster;
DROP TABLE IF EXISTS tower;
DROP TABLE IF EXISTS level;
DROP TABLE IF EXISTS player;

-- =====================================================
-- 1. player：玩家表
-- =====================================================
CREATE TABLE player (
    player_id  INT AUTO_INCREMENT PRIMARY KEY,
    username   VARCHAR(50) NOT NULL UNIQUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- =====================================================
-- 2. level：关卡配置表
-- =====================================================
CREATE TABLE level (
    level_id        INT AUTO_INCREMENT PRIMARY KEY,
    level_name      VARCHAR(50)  NOT NULL,
    initial_gold    INT          NOT NULL DEFAULT 100,
    base_hp         INT          NOT NULL DEFAULT 10,
    gold_per_second FLOAT        NOT NULL DEFAULT 1   COMMENT '每秒金币增长',
    description     VARCHAR(255),
    is_active       TINYINT(1)   NOT NULL DEFAULT 1
);

-- =====================================================
-- 3. tower：防御塔配置表
-- =====================================================
CREATE TABLE tower (
    tower_id    INT AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(50)  NOT NULL,
    cost        INT          NOT NULL,
    attack      INT          NOT NULL,
    range_value FLOAT        NOT NULL             COMMENT '攻击范围',
    cooldown    FLOAT        NOT NULL DEFAULT 1.0 COMMENT '攻击间隔(秒)',
    refund_rate FLOAT        NOT NULL DEFAULT 0.5 COMMENT '出售返金比例(MVP保留)',
    description VARCHAR(255),
    is_active   TINYINT(1)   NOT NULL DEFAULT 1
);

-- =====================================================
-- 4. monster：怪物配置表
-- =====================================================
CREATE TABLE monster (
    monster_id     INT AUTO_INCREMENT PRIMARY KEY,
    name           VARCHAR(50) NOT NULL,
    hp             INT         NOT NULL,
    speed          FLOAT       NOT NULL,
    score_value    INT         NOT NULL DEFAULT 100,
    reward_gold    INT         NOT NULL DEFAULT 10,
    damage_to_base INT         NOT NULL DEFAULT 1  COMMENT '到达终点扣基地血量',
    is_active      TINYINT(1)  NOT NULL DEFAULT 1
);

-- =====================================================
-- 5. wave_event：出怪时间轴表
-- =====================================================
CREATE TABLE wave_event (
    event_id      INT AUTO_INCREMENT PRIMARY KEY,
    level_id      INT       NOT NULL,
    wave_number   INT       NOT NULL DEFAULT 1,
    spawn_time    FLOAT     NOT NULL                  COMMENT '游戏开始后第几秒触发',
    monster_id    INT       NOT NULL,
    count         INT       NOT NULL DEFAULT 1,
    interval_time FLOAT     NOT NULL DEFAULT 0.5      COMMENT '多只怪之间生成间隔(秒)',
    is_active     TINYINT(1)NOT NULL DEFAULT 1,
    FOREIGN KEY (level_id)   REFERENCES level(level_id),
    FOREIGN KEY (monster_id) REFERENCES monster(monster_id)
);

-- =====================================================
-- 6. game_result：游戏结果 / 排行榜表
-- =====================================================
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
