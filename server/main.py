# main.py

import uvicorn
from fastapi import FastAPI, WebSocket
from http_api import router
from wss_handler import handle_ws_message
from config import SERVER_HOST, SERVER_PORT

app = FastAPI(title="协同防线 Server")

app.include_router(router)


@app.websocket("/ws")
async def ws_endpoint(websocket: WebSocket):
    await websocket.accept()
    try:
        while True:
            raw = await websocket.receive_text()
            await handle_ws_message(websocket, raw)
    except Exception:
        pass


if __name__ == "__main__":
    uvicorn.run(app, host=SERVER_HOST, port=SERVER_PORT)
