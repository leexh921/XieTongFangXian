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

PATH_POINTS = [(0, 0), (5, 0), (5, 5), (10, 5)]
