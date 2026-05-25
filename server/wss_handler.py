# wss_handler.py

import json
import time
import asyncio
from db import get_or_create_player, load_level_config
from game_logic import init_game, build_tower, game_tick_generator
from result_writer import save_game_result


active_players = {}     # player_id -> {"player_id","username","websocket"}
active_games = {}       # game_id -> asyncio.Task

_game_states = {}       # game_id -> GameState
_tower_templates = {}   # game_id -> dict[tower_id, row]


def make_message(msg_type, **kwargs):
    msg = {
        "type": msg_type,
        "timestamp": int(time.time() * 1000),
    }
    for key in ("request_id", "game_id", "player_id", "data"):
        if key in kwargs:
            msg[key] = kwargs[key]
    return msg


async def send(ws, msg):
    try:
        await ws.send_text(json.dumps(msg, ensure_ascii=False))
    except Exception:
        pass


async def handle_ws_message(ws, raw):
    try:
        msg = json.loads(raw)
    except json.JSONDecodeError:
        await send(ws, make_message("error", data={"code": "invalid_json", "message": "invalid json"}))
        return

    msg_type = msg.get("type")
    request_id = msg.get("request_id", "")
    player_id = msg.get("player_id")
    game_id = msg.get("game_id")

    # --------------------------------------------------
    # login_request
    # --------------------------------------------------
    if msg_type == "login_request":
        username = msg.get("data", {}).get("username", "").strip()
        if not username:
            await send(ws, make_message("login_result", request_id=request_id,
                data={"success": False, "message": "username is empty"}))
            return

        pid, uname = get_or_create_player(username)
        active_players[pid] = {"player_id": pid, "username": uname, "websocket": ws}

        await send(ws, make_message("login_result", request_id=request_id,
            data={"success": True, "player_id": pid, "username": uname, "message": "login success"}))
        return

    # --------------------------------------------------
    # start_game_request
    # --------------------------------------------------
    if msg_type == "start_game_request":
        info = active_players.get(player_id)
        if not info:
            await send(ws, make_message("error", data={"code": "not_logged_in", "message": "please login first"}))
            return

        level_id = msg.get("data", {}).get("level_id", 1)
        import random
        gid = random.randint(10000, 99999)

        game_state, tower_templates, spawn_queue, meta = init_game(
            game_id=gid, username=info["username"], player_id=player_id, level_id=level_id,
        )

        _game_states[gid] = game_state
        _tower_templates[gid] = tower_templates

        level, tower_config, _, _ = load_level_config(level_id)

        await send(ws, make_message("game_start", game_id=gid, player_id=player_id,
            data={
                "level": {
                    "level_id": level["level_id"],
                    "name": level["level_name"],
                    "base_hp": level["base_hp"],
                    "initial_gold": level["initial_gold"],
                    "gold_per_second": level["gold_per_second"],
                },
                "player": {
                    "player_id": player_id, "username": info["username"],
                    "gold": level["initial_gold"], "score": 0, "kill_count": 0,
                },
                "tower_config": [
                    {
                        "tower_id": t["tower_id"], "name": t["name"],
                        "attack": t["attack"], "range": t["range_value"],
                        "cooldown": float(t["cooldown"]), "cost": t["cost"],
                        "refund_rate": float(t["refund_rate"]),
                    }
                    for t in tower_config
                ],
                "map": meta["map"],
                "base_hp": level["base_hp"],
            }
        ))

        task = asyncio.create_task(_run_game_for_player(ws, game_state, spawn_queue, meta))
        active_games[gid] = task
        return

    # --------------------------------------------------
    # build_request
    # --------------------------------------------------
    if msg_type == "build_request":
        if game_id not in active_games or game_id not in _game_states:
            await send(ws, make_message("error", data={"code": "game_not_started", "message": "game not started"}))
            return

        gs = _game_states[game_id]
        tt = _tower_templates[game_id]

        data = msg.get("data", {})
        result = build_tower(
            game_state=gs,
            tower_templates=tt,
            player_id=player_id,
            grid_x=data.get("grid_x", 0),
            grid_y=data.get("grid_y", 0),
            tower_id=data.get("tower_id"),
            game_id=game_id,
        )

        await send(ws, make_message("build_result", request_id=request_id,
            game_id=game_id, player_id=player_id, data=result))
        return

    # --------------------------------------------------
    # unknown
    # --------------------------------------------------
    await send(ws, make_message("error", data={"code": "invalid_message_type", "message": f"unknown type: {msg_type}"}))


async def _run_game_for_player(ws, game_state, spawn_queue, meta):
    gid = meta["game_id"]

    async for state in game_tick_generator(game_state, spawn_queue, meta):
        state["timestamp"] = int(time.time() * 1000)
        await send(ws, state)

        if state.get("type") == "game_over":
            save_game_result(meta, game_state, state["data"]["is_win"])
            break

    _game_states.pop(gid, None)
    _tower_templates.pop(gid, None)
    active_games.pop(gid, None)
