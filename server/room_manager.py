# room_manager.py

import random
import string

# 房间存储
# room_code -> {
#   "room_code": str,
#   "host_player_id": int,
#   "players": [{"player_id": int, "username": str, "websocket": ws}],
#   "game_state": GameState or None,
#   "game_id": int or None,
#   "level_id": int,
#   "is_started": bool
# }
rooms = {}

# player_id -> room_code
player_room = {}

MAX_PLAYERS = 4


def _gen_room_code():
    """生成4位房间码，如 A001"""
    while True:
        code = random.choice(string.ascii_uppercase) + \
               ''.join(random.choices(string.digits, k=3))
        if code not in rooms:
            return code


def _gen_game_id():
    return random.randint(10000, 99999)


def create_room(player_id, username):
    """创建房间，返回 room_code"""
    room_code = _gen_room_code()
    rooms[room_code] = {
        "room_code": room_code,
        "host_player_id": player_id,
        "players": [{"player_id": player_id, "username": username}],
        "websockets": {player_id: None},  # player_id -> websocket
        "game_state": None,
        "tower_templates": None,
        "spawn_queue": None,
        "game_id": None,
        "level_id": 1,
        "is_started": False
    }
    player_room[player_id] = room_code
    return room_code


def join_room(room_code, player_id, username):
    """加入房间，返回 (success, reason)"""
    if room_code not in rooms:
        return False, "room_not_found"
    room = rooms[room_code]
    if room["is_started"]:
        return False, "game_already_started"
    if len(room["players"]) >= MAX_PLAYERS:
        return False, "room_full"
    # 防止重复加入
    for p in room["players"]:
        if p["player_id"] == player_id:
            return False, "already_in_room"
    room["players"].append({"player_id": player_id, "username": username})
    room["websockets"][player_id] = None
    player_room[player_id] = room_code
    return True, ""


def set_websocket(player_id, websocket):
    """登录后绑定 websocket"""
    room_code = player_room.get(player_id)
    if room_code and room_code in rooms:
        rooms[room_code]["websockets"][player_id] = websocket


def get_room_by_player(player_id):
    room_code = player_room.get(player_id)
    if room_code:
        return rooms.get(room_code)
    return None


def get_room(room_code):
    return rooms.get(room_code)


def get_player_list(room_code):
    room = rooms.get(room_code)
    if not room:
        return []
    return [{"player_id": p["player_id"], "username": p["username"]}
            for p in room["players"]]


def remove_player(player_id):
    """玩家断线时移除"""
    room_code = player_room.pop(player_id, None)
    if not room_code or room_code not in rooms:
        return
    room = rooms[room_code]
    room["players"] = [p for p in room["players"] if p["player_id"] != player_id]
    room["websockets"].pop(player_id, None)
    if not room["players"]:
        del rooms[room_code]