# message_router.py

import json
import asyncio
from room_manager import (
    create_room, join_room, get_room, get_player_list,
    set_websocket, get_room_by_player, remove_player
)
from game_logic import init_game, build_tower, game_tick_generator
from db import write_game_result

# 已登录玩家缓存 player_id -> {"player_id": int, "username": str, "websocket": ws}
online_players = {}


async def send(websocket, msg):
    await websocket.send(json.dumps(msg, ensure_ascii=False))


async def broadcast(room_code, msg):
    """广播给房间内所有玩家"""
    room = get_room(room_code)
    if not room:
        return
    data = json.dumps(msg, ensure_ascii=False)
    for ws in room["websockets"].values():
        if ws:
            try:
                await ws.send(data)
            except Exception:
                pass


async def handle_message(websocket, raw):
    try:
        msg = json.loads(raw)
    except Exception:
        await send(websocket, {"type": "error", "data": {"code": "invalid_json", "message": "invalid json"}})
        return

    msg_type = msg.get("type")
    player_id = msg.get("player_id")
    request_id = msg.get("request_id", "")

    # ---------- 登录 ----------
    if msg_type == "login_request":
        username = msg.get("data", {}).get("username", "").strip()
        if not username:
            await send(websocket, {
                "type": "login_result",
                "request_id": request_id,
                "data": {"success": False, "message": "username is empty"}
            })
            return

        # 简单处理：用用户名哈希作为player_id，实际应查数据库
        from db import connect_db
        conn = connect_db()
        cursor = conn.cursor()
        cursor.execute("SELECT player_id, username FROM player WHERE username = %s", (username,))
        row = cursor.fetchone()
        if not row:
            # 自动注册
            import hashlib
            cursor.execute(
                "INSERT INTO player (username, password_hash) VALUES (%s, %s)",
                (username, hashlib.md5(username.encode()).hexdigest())
            )
            conn.commit()
            new_id = cursor.lastrowid
            row = {"player_id": new_id, "username": username}
        cursor.close()
        conn.close()

        pid = row["player_id"]
        online_players[pid] = {"player_id": pid, "username": row["username"], "websocket": websocket}

        await send(websocket, {
            "type": "login_result",
            "request_id": request_id,
            "data": {"success": True, "player_id": pid, "username": row["username"], "message": "login success"}
        })
        return

    # ---------- 创建房间 ----------
    if msg_type == "create_room_request":
        info = online_players.get(player_id)
        if not info:
            await send(websocket, {"type": "error", "data": {"code": "not_logged_in", "message": "please login first"}})
            return
        room_code = create_room(player_id, info["username"])
        room = get_room(room_code)
        room["websockets"][player_id] = websocket
        player_list = get_player_list(room_code)
        await send(websocket, {
            "type": "create_room_result",
            "request_id": request_id,
            "player_id": player_id,
            "room_code": room_code,
            "data": {"success": True, "room_code": room_code, "player_list": player_list}
        })
        return

    # ---------- 加入房间 ----------
    if msg_type == "join_room_request":
        info = online_players.get(player_id)
        room_code = msg.get("data", {}).get("room_code", "")
        if not info:
            await send(websocket, {"type": "error", "data": {"code": "not_logged_in", "message": "please login first"}})
            return
        success, reason = join_room(room_code, player_id, info["username"])
        if not success:
            await send(websocket, {
                "type": "join_room_result",
                "request_id": request_id,
                "data": {"success": False, "message": reason}
            })
            return
        room = get_room(room_code)
        room["websockets"][player_id] = websocket
        player_list = get_player_list(room_code)
        await send(websocket, {
            "type": "join_room_result",
            "request_id": request_id,
            "player_id": player_id,
            "room_code": room_code,
            "data": {"success": True, "room_code": room_code, "player_list": player_list}
        })
        # 广播玩家列表更新
        await broadcast(room_code, {
            "type": "player_list_update",
            "room_code": room_code,
            "data": {
                "player_count": len(player_list),
                "max_player_count": 4,
                "player_list": player_list
            }
        })
        return

    # ---------- 开始游戏 ----------
    if msg_type == "start_game_request":
        room_code = msg.get("room_code", "")
        level_id = msg.get("data", {}).get("level_id", 1)
        room = get_room(room_code)
        if not room:
            await send(websocket, {"type": "error", "data": {"code": "room_not_found", "message": "room not found"}})
            return
        if room["host_player_id"] != player_id:
            await send(websocket, {"type": "error", "data": {"code": "not_host", "message": "only host can start"}})
            return

        import random
        game_id = random.randint(10000, 99999)
        player_list = get_player_list(room_code)
        game_state, tower_templates, spawn_queue, meta = init_game(
            game_id=game_id,
            room_code=room_code,
            player_list=player_list,
            level_id=level_id
        )
        room["game_state"] = game_state
        room["tower_templates"] = tower_templates
        room["spawn_queue"] = spawn_queue
        room["game_id"] = game_id
        room["level_id"] = level_id
        room["is_started"] = True
        room["meta"] = meta

        # 广播 game_start
        from db import load_level_config
        level, tower_config, _, _ = load_level_config(level_id)
        await broadcast(room_code, {
            "type": "game_start",
            "room_code": room_code,
            "game_id": game_id,
            "data": {
                "level": {
                    "level_id": level["level_id"],
                    "name": level["name"],
                    "base_hp": level["base_hp"],
                    "initial_gold": level["initial_gold"],
                    "gold_per_second": level["gold_per_second"]
                },
                "players": [
                    {"player_id": p["player_id"], "username": p["username"],
                     "gold": level["initial_gold"], "score": 0, "kill_count": 0}
                    for p in player_list
                ],
                "tower_config": [
                    {"tower_id": t["tower_id"], "name": t["name"], "attack": t["attack"],
                     "range": t["range"], "cooldown": float(t["cooldown"]),
                     "cost": t["cost"], "refund_rate": float(t["refund_rate"])}
                    for t in tower_config
                ],
                "base_hp": level["base_hp"]
            }
        })

        # 启动游戏循环
        asyncio.create_task(run_game_loop(room_code))
        return

    # ---------- 建塔 ----------
    if msg_type == "build_request":
        room_code = msg.get("room_code", "")
        game_id = msg.get("game_id")
        data = msg.get("data", {})
        room = get_room(room_code)
        if not room or not room["is_started"]:
            await send(websocket, {"type": "error", "data": {"code": "game_not_started", "message": "game not started"}})
            return
        result = build_tower(
            game_state=room["game_state"],
            tower_templates=room["tower_templates"],
            player_id=player_id,
            grid_x=data.get("grid_x", 0),
            grid_y=data.get("grid_y", 0),
            tower_id=data.get("tower_id"),
            game_id=room["game_id"]
        )
        await broadcast(room_code, {
            "type": "build_result",
            "request_id": request_id,
            "room_code": room_code,
            "game_id": game_id,
            "player_id": player_id,
            "data": result
        })
        return

    # 未知消息类型
    await send(websocket, {
        "type": "error",
        "data": {"code": "invalid_message_type", "message": f"unknown type: {msg_type}"}
    })


async def run_game_loop(room_code):
    """游戏主循环，跑完后写入数据库"""
    room = get_room(room_code)
    if not room:
        return
    async for state in game_tick_generator(
        room["game_state"], room["spawn_queue"], room["meta"]
    ):
        await broadcast(room_code, state)
        if state.get("type") == "game_over":
            # 写入数据库
            write_game_result(
                game_id=room["game_id"],
                room_code=room_code,
                level_id=room["level_id"],
                is_win=state["data"]["is_win"],
                players=state["data"]["results"]
            )
            break