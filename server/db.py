# db.py

import pymysql

DB_CONFIG = {
    'host': '192.168.87.81',
    'user': 'root',
    'password': 'asd123456',      # 改成你的MySQL密码
    'database': 'cooperative_defense',
    'charset': 'utf8mb4',
    'port': 3306
}

def connect_db():
    return pymysql.connect(**DB_CONFIG, cursorclass=pymysql.cursors.DictCursor)


def load_level_config(level_id=1):
    """读取完整关卡配置，返回 level、tower_config、monster_config、wave_events"""
    conn = connect_db()
    cursor = conn.cursor()

    # 关卡基本信息
    cursor.execute("SELECT * FROM level WHERE level_id = %s", (level_id,))
    level = cursor.fetchone()

    # 塔配置
    cursor.execute("SELECT tower_id, name, attack, `range`, cooldown, cost, refund_rate FROM tower")
    tower_config = cursor.fetchall()

    # 怪物配置
    cursor.execute("SELECT * FROM monster")
    monster_config = {row['monster_id']: row for row in cursor.fetchall()}

    # 波次事件
    cursor.execute("""
        SELECT w.event_id, w.wave_number, w.spawn_time_sec, w.count, w.interval_sec,
               m.monster_id, m.name, m.hp, m.speed, m.reward_gold, m.score
        FROM wave_event w
        JOIN monster m ON w.monster_id = m.monster_id
        WHERE w.level_id = %s
        ORDER BY w.wave_number, w.spawn_time_sec
    """, (level_id,))
    wave_events = cursor.fetchall()

    cursor.close()
    conn.close()

    return level, tower_config, monster_config, wave_events


def write_game_result(game_id, room_code, level_id, is_win, players):
    """游戏结束后写入 game_result 表"""
    conn = connect_db()
    cursor = conn.cursor()
    for p in players:
        cursor.execute("""
            INSERT INTO game_result
                (game_id, room_code, player_id, level_id, score, kill_count, is_win, played_at)
            VALUES (%s, %s, %s, %s, %s, %s, %s, NOW())
        """, (game_id, room_code, p['player_id'], level_id,
              p['score'], p['kill_count'], 1 if is_win else 0))
    conn.commit()
    cursor.close()
    conn.close()