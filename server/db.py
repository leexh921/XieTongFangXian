# db.py

import pymysql
from config import DB_CONFIG


def get_connection():
    return pymysql.connect(**DB_CONFIG, cursorclass=pymysql.cursors.DictCursor)


def get_or_create_player(username):
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("SELECT player_id, username FROM player WHERE username = %s", (username,))
    row = cursor.fetchone()
    if not row:
        cursor.execute("INSERT INTO player (username) VALUES (%s)", (username,))
        conn.commit()
        row = {"player_id": cursor.lastrowid, "username": username}
    cursor.close()
    conn.close()
    return row["player_id"], row["username"]


def load_level_config(level_id):
    conn = get_connection()
    cursor = conn.cursor()

    cursor.execute("SELECT * FROM level WHERE level_id = %s AND is_active = 1", (level_id,))
    level = cursor.fetchone()

    cursor.execute(
        "SELECT tower_id, name, cost, attack, range_value, cooldown, refund_rate "
        "FROM tower WHERE is_active = 1"
    )
    tower_config = cursor.fetchall()

    cursor.execute("SELECT * FROM monster WHERE is_active = 1")
    monster_config = {row["monster_id"]: row for row in cursor.fetchall()}

    cursor.execute("""
        SELECT w.event_id, w.wave_number, w.spawn_time, w.count, w.interval_time,
               m.monster_id, m.name, m.hp, m.speed, m.reward_gold, m.score_value, m.damage_to_base
        FROM wave_event w
        JOIN monster m ON w.monster_id = m.monster_id
        WHERE w.level_id = %s AND w.is_active = 1 AND m.is_active = 1
        ORDER BY w.spawn_time ASC
    """, (level_id,))
    wave_events = cursor.fetchall()

    cursor.close()
    conn.close()
    return level, tower_config, monster_config, wave_events


def write_game_result(game_id, username, level_id, score, kill_count, time_used, is_win):
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("""
        INSERT INTO game_result (game_id, username, level_id, score, kill_count, time_used, is_win)
        VALUES (%s, %s, %s, %s, %s, %s, %s)
    """, (game_id, username, level_id, score, kill_count, int(time_used), 1 if is_win else 0))
    conn.commit()
    cursor.close()
    conn.close()


def get_leaderboard(limit=100):
    conn = get_connection()
    cursor = conn.cursor()
    cursor.execute("""
        SELECT result_id, game_id, username, level_id, score, kill_count,
               time_used, is_win, played_at
        FROM game_result
        ORDER BY score DESC, time_used ASC
        LIMIT %s
    """, (limit,))
    rows = cursor.fetchall()
    cursor.close()
    conn.close()
    return rows
