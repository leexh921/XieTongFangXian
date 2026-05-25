# http_api.py

import time
from fastapi import APIRouter
from pydantic import BaseModel
from config import DB_CONFIG
import pymysql

router = APIRouter(prefix="/api")


def _conn():
    return pymysql.connect(**DB_CONFIG, cursorclass=pymysql.cursors.DictCursor)


# ============================================================
# Tower CRUD
# ============================================================
class TowerBody(BaseModel):
    name: str
    cost: int
    attack: int
    range_value: float
    cooldown: float = 1.0
    refund_rate: float = 0.5
    description: str = ""
    is_active: int = 1

@router.get("/towers")
def list_towers():
    c = _conn().cursor()
    c.execute("SELECT * FROM tower ORDER BY tower_id")
    rows = c.fetchall()
    c.close()
    return rows

@router.post("/towers")
def create_tower(body: TowerBody):
    conn = _conn()
    c = conn.cursor()
    c.execute("""
        INSERT INTO tower (name, cost, attack, range_value, cooldown, refund_rate, description, is_active)
        VALUES (%s,%s,%s,%s,%s,%s,%s,%s)
    """, (body.name, body.cost, body.attack, body.range_value, body.cooldown,
          body.refund_rate, body.description, body.is_active))
    conn.commit()
    new_id = c.lastrowid
    c.close()
    conn.close()
    return {"tower_id": new_id}

@router.put("/towers/{id}")
def update_tower(id: int, body: TowerBody):
    conn = _conn()
    c = conn.cursor()
    c.execute("""
        UPDATE tower SET name=%s, cost=%s, attack=%s, range_value=%s,
        cooldown=%s, refund_rate=%s, description=%s, is_active=%s
        WHERE tower_id=%s
    """, (body.name, body.cost, body.attack, body.range_value, body.cooldown,
          body.refund_rate, body.description, body.is_active, id))
    conn.commit()
    c.close()
    conn.close()
    return {"ok": True}

@router.delete("/towers/{id}")
def delete_tower(id: int):
    conn = _conn()
    c = conn.cursor()
    c.execute("DELETE FROM tower WHERE tower_id=%s", (id,))
    conn.commit()
    c.close()
    conn.close()
    return {"ok": True}


# ============================================================
# Monster CRUD
# ============================================================
class MonsterBody(BaseModel):
    name: str
    hp: int
    speed: float
    score_value: int = 100
    reward_gold: int = 10
    damage_to_base: int = 1
    is_active: int = 1

@router.get("/monsters")
def list_monsters():
    c = _conn().cursor()
    c.execute("SELECT * FROM monster ORDER BY monster_id")
    rows = c.fetchall()
    c.close()
    return rows

@router.post("/monsters")
def create_monster(body: MonsterBody):
    conn = _conn()
    c = conn.cursor()
    c.execute("""
        INSERT INTO monster (name, hp, speed, score_value, reward_gold, damage_to_base, is_active)
        VALUES (%s,%s,%s,%s,%s,%s,%s)
    """, (body.name, body.hp, body.speed, body.score_value, body.reward_gold,
          body.damage_to_base, body.is_active))
    conn.commit()
    new_id = c.lastrowid
    c.close()
    conn.close()
    return {"monster_id": new_id}

@router.put("/monsters/{id}")
def update_monster(id: int, body: MonsterBody):
    conn = _conn()
    c = conn.cursor()
    c.execute("""
        UPDATE monster SET name=%s, hp=%s, speed=%s, score_value=%s,
        reward_gold=%s, damage_to_base=%s, is_active=%s
        WHERE monster_id=%s
    """, (body.name, body.hp, body.speed, body.score_value, body.reward_gold,
          body.damage_to_base, body.is_active, id))
    conn.commit()
    c.close()
    conn.close()
    return {"ok": True}

@router.delete("/monsters/{id}")
def delete_monster(id: int):
    conn = _conn()
    c = conn.cursor()
    c.execute("DELETE FROM monster WHERE monster_id=%s", (id,))
    conn.commit()
    c.close()
    conn.close()
    return {"ok": True}


# ============================================================
# Level CRUD
# ============================================================
class LevelBody(BaseModel):
    level_name: str
    initial_gold: int = 100
    base_hp: int = 10
    gold_per_second: float = 1.0
    description: str = ""
    is_active: int = 1

@router.get("/levels")
def list_levels():
    c = _conn().cursor()
    c.execute("SELECT * FROM level ORDER BY level_id")
    rows = c.fetchall()
    c.close()
    return rows

@router.post("/levels")
def create_level(body: LevelBody):
    conn = _conn()
    c = conn.cursor()
    c.execute("""
        INSERT INTO level (level_name, initial_gold, base_hp, gold_per_second, description, is_active)
        VALUES (%s,%s,%s,%s,%s,%s)
    """, (body.level_name, body.initial_gold, body.base_hp, body.gold_per_second,
          body.description, body.is_active))
    conn.commit()
    new_id = c.lastrowid
    c.close()
    conn.close()
    return {"level_id": new_id}

@router.put("/levels/{id}")
def update_level(id: int, body: LevelBody):
    conn = _conn()
    c = conn.cursor()
    c.execute("""
        UPDATE level SET level_name=%s, initial_gold=%s, base_hp=%s,
        gold_per_second=%s, description=%s, is_active=%s
        WHERE level_id=%s
    """, (body.level_name, body.initial_gold, body.base_hp, body.gold_per_second,
          body.description, body.is_active, id))
    conn.commit()
    c.close()
    conn.close()
    return {"ok": True}

@router.delete("/levels/{id}")
def delete_level(id: int):
    conn = _conn()
    c = conn.cursor()
    c.execute("DELETE FROM level WHERE level_id=%s", (id,))
    conn.commit()
    c.close()
    conn.close()
    return {"ok": True}


# ============================================================
# Wave Event CRUD
# ============================================================
class WaveEventBody(BaseModel):
    level_id: int
    wave_number: int = 1
    spawn_time: float
    monster_id: int
    count: int = 1
    interval_time: float = 0.5
    is_active: int = 1

@router.get("/levels/{level_id}/waves")
def list_waves(level_id: int):
    c = _conn().cursor()
    c.execute("SELECT w.*, m.name FROM wave_event w JOIN monster m ON w.monster_id=m.monster_id WHERE w.level_id=%s ORDER BY w.spawn_time ASC", (level_id,))
    rows = c.fetchall()
    c.close()
    return rows

@router.post("/wave-events")
def create_wave(body: WaveEventBody):
    conn = _conn()
    c = conn.cursor()
    c.execute("""
        INSERT INTO wave_event (level_id, wave_number, spawn_time, monster_id, count, interval_time, is_active)
        VALUES (%s,%s,%s,%s,%s,%s,%s)
    """, (body.level_id, body.wave_number, body.spawn_time, body.monster_id,
          body.count, body.interval_time, body.is_active))
    conn.commit()
    new_id = c.lastrowid
    c.close()
    conn.close()
    return {"event_id": new_id}

@router.put("/wave-events/{id}")
def update_wave(id: int, body: WaveEventBody):
    conn = _conn()
    c = conn.cursor()
    c.execute("""
        UPDATE wave_event SET level_id=%s, wave_number=%s, spawn_time=%s,
        monster_id=%s, count=%s, interval_time=%s, is_active=%s
        WHERE event_id=%s
    """, (body.level_id, body.wave_number, body.spawn_time, body.monster_id,
          body.count, body.interval_time, body.is_active, id))
    conn.commit()
    c.close()
    conn.close()
    return {"ok": True}

@router.delete("/wave-events/{id}")
def delete_wave(id: int):
    conn = _conn()
    c = conn.cursor()
    c.execute("DELETE FROM wave_event WHERE event_id=%s", (id,))
    conn.commit()
    c.close()
    conn.close()
    return {"ok": True}


# ============================================================
# Player CRUD
# ============================================================
class PlayerBody(BaseModel):
    username: str

@router.get("/players")
def list_players():
    c = _conn().cursor()
    c.execute("SELECT * FROM player ORDER BY player_id")
    rows = c.fetchall()
    c.close()
    return rows

@router.post("/players")
def create_player(body: PlayerBody):
    conn = _conn()
    c = conn.cursor()
    c.execute("INSERT INTO player (username) VALUES (%s)", (body.username,))
    conn.commit()
    new_id = c.lastrowid
    c.close()
    conn.close()
    return {"player_id": new_id}

@router.put("/players/{id}")
def update_player(id: int, body: PlayerBody):
    conn = _conn()
    c = conn.cursor()
    c.execute("UPDATE player SET username=%s WHERE player_id=%s", (body.username, id))
    conn.commit()
    c.close()
    conn.close()
    return {"ok": True}

@router.delete("/players/{id}")
def delete_player(id: int):
    conn = _conn()
    c = conn.cursor()
    c.execute("DELETE FROM player WHERE player_id=%s", (id,))
    conn.commit()
    c.close()
    conn.close()
    return {"ok": True}

# ============================================================
# Leaderboard
# ============================================================
@router.get("/leaderboard")
def leaderboard(limit: int = 100):
    from db import get_leaderboard
    rows = get_leaderboard(limit)
    return {
        "type": "leaderboard_response",
        "timestamp": int(time.time() * 1000),
        "data": {"ranking": rows},
    }
