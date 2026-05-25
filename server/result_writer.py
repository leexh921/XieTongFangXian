# result_writer.py

from db import write_game_result


def save_game_result(meta, game_state, is_win):
    player = list(game_state.players.values())[0]
    write_game_result(
        game_id=meta["game_id"],
        username=meta["username"],
        level_id=meta["level_id"],
        score=player.score,
        kill_count=player.kill_count,
        time_used=int(game_state.time_elapsed),
        is_win=is_win,
    )
