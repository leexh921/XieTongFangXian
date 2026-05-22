# game_logic.py

import asyncio
from models import GameState, Player, Monster, Tower
from db import load_level_config
from config import PATH_POINTS, TICK_INTERVAL


def init_game(game_id, username, player_id, level_id=1):
    level, tower_config, monster_config, wave_events = load_level_config(level_id)

    game_state = GameState(path_points=PATH_POINTS, base_hp=level["base_hp"])

    game_state.players[player_id] = Player(
        player_id=player_id,
        username=username,
        gold=level["initial_gold"],
        score=0,
        kill_count=0,
    )

    tower_templates = {}
    for t in tower_config:
        tower_templates[t["tower_id"]] = t

    spawn_queue = []
    for row in wave_events:
        for i in range(row["count"]):
            spawn_time = row["spawn_time"] + i * float(row["interval_time"])
            m = Monster(
                monster_id=row["monster_id"],
                name=row["name"],
                hp=row["hp"],
                speed=float(row["speed"]),
                reward_gold=row["reward_gold"],
                score_value=row["score_value"],
                damage_to_base=row["damage_to_base"],
                x=PATH_POINTS[0][0],
                y=PATH_POINTS[0][1],
                path_index=0,
                is_alive=False,
            )
            spawn_queue.append((spawn_time, m))
    spawn_queue.sort(key=lambda x: x[0])

    meta = {
        "game_id": game_id,
        "username": username,
        "player_id": player_id,
        "level_id": level_id,
        "gold_per_second": level["gold_per_second"],
        "level": level,
    }

    return game_state, tower_templates, spawn_queue, meta


def build_tower(game_state, tower_templates, player_id, grid_x, grid_y, tower_id, game_id):
    tile_key = f"{grid_x},{grid_y}"

    if tile_key not in game_state.tiles:
        game_state.tiles[tile_key] = {"occupied": False, "owner_player_id": None}

    tile = game_state.tiles[tile_key]
    player = game_state.players.get(player_id)
    template = tower_templates.get(tower_id)

    if not player:
        return {"success": False, "reason": "invalid_player"}
    if not template:
        return {"success": False, "reason": "invalid_tower"}
    if tile["occupied"]:
        return {"success": False, "reason": "tile_occupied"}
    if player.gold < template["cost"]:
        return {"success": False, "reason": "not_enough_gold"}

    player.gold -= template["cost"]
    tile["occupied"] = True
    tile["owner_player_id"] = player_id

    game_state.tower_counter += 1
    instance_id = f"tower_{game_id}_{game_state.tower_counter}"

    tower = Tower(
        tower_id=template["tower_id"],
        attack=template["attack"],
        tower_range=template["range_value"],
        cooldown=float(template["cooldown"]),
        cost=template["cost"],
        refund_rate=float(template.get("refund_rate", 0.5)),
        owner_player_id=player_id,
        grid_x=grid_x,
        grid_y=grid_y,
    )
    tower.instance_id = instance_id
    game_state.towers.append(tower)

    return {
        "success": True, "reason": "",
        "tower": {
            "instance_id": instance_id,
            "tower_id": tower_id,
            "owner_player_id": player_id,
            "grid_x": grid_x,
            "grid_y": grid_y,
        },
        "player": {
            "player_id": player_id,
            "gold": player.gold,
            "score": player.score,
            "kill_count": player.kill_count,
        },
    }


async def game_tick_generator(game_state, spawn_queue, meta):
    gold_per_second = meta["gold_per_second"]
    game_id = meta["game_id"]

    while not game_state.is_game_over:
        game_state.time_elapsed += TICK_INTERVAL

        # 金币增长
        game_state.gold_accumulator += TICK_INTERVAL * gold_per_second
        if game_state.gold_accumulator >= 1.0:
            added = int(game_state.gold_accumulator)
            for p in game_state.players.values():
                p.gold += added
            game_state.gold_accumulator -= added

        # 出怪
        while spawn_queue and spawn_queue[0][0] <= game_state.time_elapsed:
            _, monster = spawn_queue.pop(0)
            monster.is_alive = True
            monster.x, monster.y = PATH_POINTS[0]
            monster.path_index = 0
            monster._dist_on_segment = 0.0
            game_state.monster_counter += 1
            monster.instance_id = f"monster_{game_id}_{game_state.monster_counter}"
            game_state.monsters.append(monster)

        # 怪物移动
        for m in game_state.monsters:
            if not m.is_alive:
                continue
            m.advance(PATH_POINTS, TICK_INTERVAL)

        # 到达终点扣血
        for m in game_state.monsters:
            if not m.is_alive:
                continue
            if m.path_index >= len(PATH_POINTS) - 1:
                game_state.base_hp -= m.damage_to_base
                m.is_alive = False

        # 塔攻击
        for tower in game_state.towers:
            if tower.cooldown_timer > 0:
                tower.cooldown_timer -= TICK_INTERVAL
                continue
            for monster in game_state.monsters:
                if not monster.is_alive:
                    continue
                dx = tower.grid_x - monster.x
                dy = tower.grid_y - monster.y
                if (dx * dx + dy * dy) ** 0.5 <= tower.tower_range:
                    monster.hp -= tower.attack
                    tower.cooldown_timer = tower.cooldown
                    if monster.hp <= 0 and not monster.is_rewarded:
                        monster.is_alive = False
                        monster.is_rewarded = True
                        owner = game_state.players.get(tower.owner_player_id)
                        if owner:
                            owner.gold += monster.reward_gold
                            owner.score += monster.score_value
                            owner.kill_count += 1
                    break

        # 清理死怪
        game_state.monsters = [m for m in game_state.monsters if m.is_alive]

        # 游戏结束判定
        if game_state.base_hp <= 0:
            game_state.is_game_over = True
            yield _make_game_over(game_state, meta, is_win=False)
            return

        alive = [m for m in game_state.monsters if m.is_alive]
        if not spawn_queue and not alive and game_state.time_elapsed > 0:
            game_state.is_game_over = True
            yield _make_game_over(game_state, meta, is_win=True)
            return

        yield _make_state_update(game_state, meta)
        await asyncio.sleep(TICK_INTERVAL)


def _player_dicts(game_state):
    return [
        {
            "player_id": p.player_id,
            "username": p.username,
            "gold": p.gold,
            "score": p.score,
            "kill_count": p.kill_count,
        }
        for p in game_state.players.values()
    ]


def _make_state_update(game_state, meta):
    return {
        "type": "state_update",
        "game_id": meta["game_id"],
        "data": {
            "game_time_sec": round(game_state.time_elapsed, 2),
            "base_hp": game_state.base_hp,
            "player": _player_dicts(game_state)[0],
            "monsters": [
                {
                    "instance_id": m.instance_id,
                    "monster_id": m.monster_id,
                    "hp": m.hp,
                    "max_hp": m.max_hp,
                    "x": round(m.x, 2),
                    "y": round(m.y, 2),
                    "path_index": m.path_index,
                }
                for m in game_state.monsters if m.is_alive
            ],
            "towers": [
                {
                    "instance_id": t.instance_id,
                    "tower_id": t.tower_id,
                    "owner_player_id": t.owner_player_id,
                    "grid_x": t.grid_x,
                    "grid_y": t.grid_y,
                }
                for t in game_state.towers
            ],
        },
    }


def _make_game_over(game_state, meta, is_win):
    player = _player_dicts(game_state)[0]
    return {
        "type": "game_over",
        "game_id": meta["game_id"],
        "data": {
            "level_id": meta["level_id"],
            "is_win": is_win,
            "time_used": int(game_state.time_elapsed),
            "base_hp": game_state.base_hp,
            "player": player,
        },
    }
