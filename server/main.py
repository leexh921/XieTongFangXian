# main.py

import asyncio
import websockets
from message_router import handle_message, online_players
from room_manager import remove_player

HOST = "0.0.0.0"
PORT = 8765


async def handler(websocket):
    print(f"[连接] 新客户端：{websocket.remote_address}")
    connected_player_id = None

    try:
        async for message in websocket:
            # 记录当前连接对应的player_id，用于断线清理
            import json
            try:
                msg = json.loads(message)
                pid = msg.get("player_id")
                if pid and pid not in [None]:
                    connected_player_id = pid
            except Exception:
                pass

            await handle_message(websocket, message)

    except websockets.exceptions.ConnectionClosed:
        print(f"[断线] player_id={connected_player_id}")
    finally:
        if connected_player_id:
            remove_player(connected_player_id)
            online_players.pop(connected_player_id, None)


async def main():
    print(f"[Server] 启动中，监听 {HOST}:{PORT}")
    async with websockets.serve(handler, HOST, PORT):
        print(f"[Server] 已启动，等待连接...")
        await asyncio.Future()  # 永久运行


if __name__ == "__main__":
    asyncio.run(main())