# game_logic.py

import asyncio
import copy
from models import GameState, Player, Monster, Tower
from db import load_level_config

# 路径点（后续可由Unity地图传入）
PATH_POINTS = [(0, 0), (5, 0), (5, 5), (10, 5)]
TICK_INTERVAL = 0.05


def init_game(game_id, room_code, player_list, level_id=1):
    """
    初始化游戏状态
    player_list: [{"player_id": 1, "username": "xxx"}, ...]
    返回 (game_state, tower_templates, spawn_queue, meta)
    """
    level, tower_config, monster_config, wave_events = load_level_config(level_id)

    initial_gold = level['initial_gold']
    gold_per_second = level['gold_per_second']
    base_hp = level['base_hp']

    game_state = GameState(path_points=PATH_POINTS, base_hp=base_hp)

    # 初始化玩家
    for p in player_list:
        game_state.players[p['player_id']] = Player(
            player_id=p['player_id'],
            username=p['username'],
            gold=initial_gold,
            score=0,
            kill_count=0
        )

    # 塔模板
    tower_templates = {}
    for t in tower_config:
        tower_templates[t['tower_id']] = t

    # 生成出怪队列（考虑 count 和 interval_sec）
    spawn_queue = []
    for row in wave_events:
        for i in range(row['count']):
            spawn_time = row['spawn_time_sec'] + i * float(row['interval_sec'])
            m = Monster(
                monster_id=row['monster_id'],
                name=row['name'],
                hp=row['hp'],
                speed=float(row['speed']),
                reward_gold=row['reward_gold'],
                score=row['score'],
                x=PATH_POINTS[0][0],
                y=PATH_POINTS[0][1],
                path_index=0,
                is_alive=False
            )
            spawn_queue.append((spawn_time, m))
    spawn_queue.sort(key=lambda x: x[0])

    meta = {
        'game_id': game_id,
        'room_code': room_code,
        'level_id': level_id,
        'gold_per_second': gold_per_second,
    }

    return game_state, tower_templates, spawn_queue, meta


def build_tower(game_state, tower_templates, player_id, grid_x, grid_y, tower_id, game_id):
    """建塔，返回结果dict"""
    tile_key = f"{grid_x},{grid_y}"

    if tile_key not in game_state.tiles:
        game_state.tiles[tile_key] = {"occupied": False, "owner_player_id": None, "tower_id": None}

    tile = game_state.tiles[tile_key]
    player = game_state.players.get(player_id)
    template = tower_templates.get(tower_id)

    if not player:
        return {"success": False, "reason": "invalid_player"}
    if not template:
        return {"success": False, "reason": "invalid_tower"}
    if tile["occupied"]:
        return {"success": False, "reason": "tile_occupied"}
    if player.gold < template['cost']:
        return {"success": False, "reason": "not_enough_gold"}

    player.gold -= template['cost']
    tile["occupied"] = True
    tile["owner_player_id"] = player_id

    game_state.tower_counter += 1
    instance_id = f"tower_{game_id}_{game_state.tower_counter}"

    tower = Tower(
        tower_id=template['tower_id'],
        attack=template['attack'],
        tower_range=template['range'],
        cooldown=float(template['cooldown']),
        cost=template['cost'],
        refund_rate=float(template['refund_rate']),
        x=grid_x,
        y=grid_y,
        owner_player_id=player_id,
        grid_x=grid_x,
        grid_y=grid_y
    )
    tower.instance_id = instance_id
    tile["tower_id"] = tower_id
    game_state.towers.append(tower)

    return {
        "success": True,
        "reason": "",
        "tower": {
            "instance_id": instance_id,
            "tower_id": tower_id,
            "owner_player_id": player_id,
            "grid_x": grid_x,
            "grid_y": grid_y
        },
        "player": {
            "player_id": player_id,
            "gold": player.gold,
            "score": player.score,
            "kill_count": player.kill_count
        }
    }


async def game_tick_generator(game_state, spawn_queue, meta):
    """核心游戏循环，每tick yield一条消息"""
    gold_per_second = meta['gold_per_second']
    game_id = meta['game_id']
    gold_accumulator = 0.0

    while not game_state.is_game_over:
        game_state.time_elapsed += TICK_INTERVAL
        gold_accumulator += TICK_INTERVAL * gold_per_second

        # 金币增长
        if gold_accumulator >= 1.0:
            for p in game_state.players.values():
                p.gold += int(gold_accumulator)
            gold_accumulator -= int(gold_accumulator)

        # 出怪
        while spawn_queue and spawn_queue[0][0] <= game_state.time_elapsed:
            _, monster = spawn_queue.pop(0)
            monster.is_alive = True
            monster.x, monster.y = PATH_POINTS[0]
            monster.path_index = 0
            game_state.monster_counter += 1
            monster.instance_id = f"monster_{game_id}_{game_state.monster_counter}"
            game_state.monsters.append(monster)

        # 怪物移动
        for m in game_state.monsters:
            if not m.is_alive:
                continue
            if m.path_index < len(PATH_POINTS) - 1:
                m.path_index += 1
                m.x, m.y = PATH_POINTS[m.path_index]
            else:
                m.is_alive = False
                game_state.base_hp -= 1

        # 塔攻击
        for tower in game_state.towers:
            if tower.cooldown_timer > 0:
                tower.cooldown_timer -= TICK_INTERVAL
                continue
            for monster in game_state.monsters:
                if not monster.is_alive:
                    continue
                dx = tower.x - monster.x
                dy = tower.y - monster.y
                distance = (dx**2 + dy**2) ** 0.5
                if distance <= tower.tower_range:
                    monster.hp -= tower.attack
                    tower.cooldown_timer = tower.cooldown
                    monster.last_hit_player = game_state.players.get(tower.owner_player_id)
                    if monster.hp <= 0 and not monster.is_rewarded:
                        monster.is_alive = False
                        monster.is_rewarded = True
                        if monster.last_hit_player:
                            monster.last_hit_player.gold += monster.reward_gold
                            monster.last_hit_player.score += monster.score
                            monster.last_hit_player.kill_count += 1
                    break

        # 游戏结束判定
        alive_monsters = [m for m in game_state.monsters if m.is_alive]

        if game_state.base_hp <= 0:
            game_state.is_game_over = True
            yield _make_game_over(game_state, is_win=False)
            return

        if not spawn_queue and not alive_monsters:
            game_state.is_game_over = True
            yield _make_game_over(game_state, is_win=True)
            return

        # 正常state_update
        yield {
            "type": "state_update",
            "room_code": meta['room_code'],
            "game_id": game_id,
            "timestamp": int(game_state.time_elapsed * 1000),
            "data": {
                "game_time_sec": round(game_state.time_elapsed, 2),
                "base_hp": game_state.base_hp,
                "players": [
                    {
                        "player_id": p.player_id,
                        "username": p.username,
                        "gold": p.gold,
                        "score": p.score,
                        "kill_count": p.kill_count
                    }
                    for p in game_state.players.values()
                ],
                "monsters": [
                    {
                        "instance_id": m.instance_id,
                        "monster_id": m.monster_id,
                        "hp": m.hp,
                        "max_hp": m.max_hp,
                        "x": m.x,
                        "y": m.y,
                        "path_index": m.path_index
                    }
                    for m in game_state.monsters if m.is_alive
                ],
                "towers": [
                    {
                        "instance_id": t.instance_id,
                        "tower_id": t.tower_id,
                        "owner_player_id": t.owner_player_id,
                        "grid_x": t.grid_x,
                        "grid_y": t.grid_y
                    }
                    for t in game_state.towers
                ]
            }
        }

        await asyncio.sleep(TICK_INTERVAL)


def _make_game_over(game_state, is_win):
    return {
        "type": "game_over",
        "data": {
            "is_win": is_win,
            "base_hp": game_state.base_hp,
            "results": [
                {
                    "player_id": p.player_id,
                    "username": p.username,
                    "score": p.score,
                    "kill_count": p.kill_count
                }
                for p in game_state.players.values()
            ]
        }
    }