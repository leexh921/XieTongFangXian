# config.py

SERVER_HOST = "0.0.0.0"
SERVER_PORT = 8765

TICK_INTERVAL = 0.05        # 游戏主循环间隔(秒)
STATE_SYNC_INTERVAL = 0.125 # state_update 广播间隔(秒)，每秒8次

DB_CONFIG = {
    "host": "127.0.0.1",
    "user": "root",
    "password": "asd123456",
    "database": "cooperative_defense",
    "charset": "utf8mb4",
    "port": 3306,
}

MAP_CONFIG = {
    "map_id": 1,
    "name": "默认地图",
    "width": 14,
    "height": 8,
    "path_points": [
        {"x": 0, "y": 4},
        {"x": 1, "y": 4},
        {"x": 2, "y": 4},
        {"x": 3, "y": 4},
        {"x": 4, "y": 4},
        {"x": 5, "y": 4},
        {"x": 5, "y": 3},
        {"x": 5, "y": 2},
        {"x": 6, "y": 2},
        {"x": 7, "y": 2},
        {"x": 8, "y": 2},
        {"x": 9, "y": 2},
        {"x": 10, "y": 2},
        {"x": 11, "y": 2},
        {"x": 12, "y": 2},
        {"x": 12, "y": 1},
        {"x": 12, "y": 0},
        {"x": 13, "y": 0},
    ],
    "obstacles": [
        {"x": 2, "y": 6},
        {"x": 3, "y": 6},
        {"x": 9, "y": 5},
        {"x": 10, "y": 5},
        {"x": 1, "y": 1},
        {"x": 8, "y": 0},
    ],
    "castle": {"x": 13, "y": 0},
}

PATH_POINTS = [(p["x"], p["y"]) for p in MAP_CONFIG["path_points"]]
