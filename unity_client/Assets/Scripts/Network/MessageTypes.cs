/// <summary>
/// Unity 与 FastAPI WebSocket 之间约定的消息类型常量。
/// 
/// 统一放在这里的原因：
/// 1. 避免不同脚本手写字符串时写错。
/// 2. 后端如果调整 type 字段，只需要改这一处。
/// 3. 当前项目是单人 MVP，不包含 room、join_room、player_list 等多人房间消息。
/// </summary>
public static class MessageTypes
{
    // Client -> Server
    public const string Login = "login";
    public const string StartGame = "start_game";
    // 如果后端最终把建塔消息定为 "build"，只需要把这一行改成 "build"。
    public const string BuildRequest = "build_request";

    // Server -> Client
    public const string LoginResult = "login_result";
    public const string GameStart = "game_start";
    public const string StateUpdate = "state_update";
    public const string BuildResult = "build_result";
    public const string GameOver = "game_over";
    public const string Error = "error";
}
