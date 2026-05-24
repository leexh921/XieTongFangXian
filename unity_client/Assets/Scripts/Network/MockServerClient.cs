using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MockServerClient : MonoBehaviour
{
    public event Action<ProtocolMessage<LoginResultData>> LoginResultReceived;
    public event Action<ProtocolMessage<GameStartData>> GameStartReceived;
    public event Action<ProtocolMessage<BuildResultData>> BuildResultReceived;
    public event Action<ProtocolMessage<StateUpdateData>> StateUpdateReceived;
    public event Action<ProtocolMessage<GameOverData>> GameOverReceived;
    public event Action<string> RawMessageReceived;

    [SerializeField] private float responseDelaySeconds = 0.2f;

    private int nextPlayerId = 1;
    private const int MockGameId = 10001;
    private const int BasicTowerId = 1;
    private const int BasicTowerCost = 100;
    private int currentGold = 300;
    private int towerSerial = 1;
    private readonly HashSet<string> occupiedTiles = new HashSet<string>();

    public void SendLoginRequest(ProtocolMessage<LoginRequestData> request)
    {
        Debug.Log("[MockServer] Received login_request: " + JsonUtility.ToJson(request));
        StartCoroutine(RespondLogin(request));
    }

    public void SendStartGameRequest(ProtocolMessage<StartGameRequestData> request)
    {
        Debug.Log("[MockServer] Received start_game_request: " + JsonUtility.ToJson(request));
        StartCoroutine(RespondStartGame(request));
    }

    public void SendBuildRequest(ProtocolMessage<BuildRequestData> request)
    {
        Debug.Log("[MockServer] Received build_request: " + JsonUtility.ToJson(request));
        StartCoroutine(RespondBuild(request));
    }

    public void PushBuildResult(ProtocolMessage<BuildResultData> message)
    {
        Debug.Log("[MockServer] Push build_result placeholder.");
        Emit(message, BuildResultReceived);
    }

    public void PushStateUpdate(ProtocolMessage<StateUpdateData> message)
    {
        Debug.Log("[MockServer] Push state_update placeholder.");
        Emit(message, StateUpdateReceived);
    }

    public void PushGameOver(ProtocolMessage<GameOverData> message)
    {
        Debug.Log("[MockServer] Push game_over placeholder.");
        Emit(message, GameOverReceived);
    }

    private IEnumerator RespondLogin(ProtocolMessage<LoginRequestData> request)
    {
        yield return new WaitForSeconds(responseDelaySeconds);

        string requestedUsername = request != null && request.data != null ? request.data.username : string.Empty;
        bool success = !string.IsNullOrWhiteSpace(requestedUsername);

        var response = new ProtocolMessage<LoginResultData>
        {
            type = MessageTypes.LoginResult,
            request_id = request != null ? request.request_id : string.Empty,
            timestamp = CurrentTimestampMilliseconds(),
            data = new LoginResultData
            {
                success = success,
                player_id = success ? nextPlayerId : 0,
                username = requestedUsername,
                message = success ? "mock login success" : "username is empty"
            }
        };

        if (success)
        {
            nextPlayerId++;
        }

        Debug.Log("[MockServer] Send login_result: " + JsonUtility.ToJson(response));
        Emit(response, LoginResultReceived);
    }

    private IEnumerator RespondStartGame(ProtocolMessage<StartGameRequestData> request)
    {
        yield return new WaitForSeconds(responseDelaySeconds);

        int playerId = request != null ? request.player_id : 0;
        if (playerId <= 0 && GameManager.Instance != null)
        {
            playerId = GameManager.Instance.player_id;
        }

        int levelId = request != null && request.data != null ? request.data.level_id : 1;
        string username = GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.username)
            ? GameManager.Instance.username
            : "Player";
        currentGold = 300;
        towerSerial = 1;
        occupiedTiles.Clear();

        var response = new ProtocolMessage<GameStartData>
        {
            type = MessageTypes.GameStart,
            request_id = request != null ? request.request_id : string.Empty,
            game_id = MockGameId,
            player_id = playerId,
            timestamp = CurrentTimestampMilliseconds(),
            data = new GameStartData
            {
                level = new LevelData
                {
                    level_id = levelId,
                    name = "第一关",
                    base_hp = 100,
                    initial_gold = 300,
                    gold_per_second = 1
                },
                player = new PlayerStateData
                {
                    player_id = playerId,
                    username = username,
                    gold = 300,
                    score = 0,
                    kill_count = 0
                },
                tower_config = new List<TowerConfigData>
                {
                    new TowerConfigData
                    {
                        tower_id = BasicTowerId,
                        name = "箭塔",
                        attack = 10,
                        range = 3f,
                        cooldown = 1f,
                        cost = BasicTowerCost,
                        refund_rate = 0.5f
                    }
                },
                base_hp = 100
            }
        };

        Debug.Log("[MockServer] Send game_start: " + JsonUtility.ToJson(response));
        Emit(response, GameStartReceived);
    }

    private IEnumerator RespondBuild(ProtocolMessage<BuildRequestData> request)
    {
        yield return new WaitForSeconds(responseDelaySeconds);

        int playerId = request != null ? request.player_id : 0;
        int towerId = request != null && request.data != null ? request.data.tower_id : 0;
        int gridX = request != null && request.data != null ? request.data.grid_x : 0;
        int gridY = request != null && request.data != null ? request.data.grid_y : 0;
        string reason = string.Empty;
        TowerStateData tower = null;

        if (playerId <= 0)
        {
            reason = "invalid_player";
        }
        else if (towerId != BasicTowerId)
        {
            reason = "invalid_tower";
        }
        else if (occupiedTiles.Contains(MakeTileKey(gridX, gridY)))
        {
            reason = "tile_occupied";
        }
        else if (currentGold < BasicTowerCost)
        {
            reason = "not_enough_gold";
        }
        else
        {
            currentGold -= BasicTowerCost;
            occupiedTiles.Add(MakeTileKey(gridX, gridY));
            tower = new TowerStateData
            {
                instance_id = "tower_" + MockGameId + "_" + towerSerial,
                tower_id = towerId,
                owner_player_id = playerId,
                grid_x = gridX,
                grid_y = gridY
            };
            towerSerial++;
        }

        bool success = string.IsNullOrEmpty(reason);
        string username = GameManager.Instance != null ? GameManager.Instance.username : string.Empty;
        int score = GameManager.Instance != null ? GameManager.Instance.score : 0;
        int killCount = GameManager.Instance != null ? GameManager.Instance.kill_count : 0;

        var response = new ProtocolMessage<BuildResultData>
        {
            type = MessageTypes.BuildResult,
            request_id = request != null ? request.request_id : string.Empty,
            game_id = request != null ? request.game_id : MockGameId,
            player_id = playerId,
            timestamp = CurrentTimestampMilliseconds(),
            data = new BuildResultData
            {
                success = success,
                reason = reason,
                tower = tower,
                player = new PlayerStateData
                {
                    player_id = playerId,
                    username = username,
                    gold = currentGold,
                    score = score,
                    kill_count = killCount
                }
            }
        };

        Debug.Log("[MockServer] Send build_result: " + JsonUtility.ToJson(response));
        Emit(response, BuildResultReceived);
    }

    private void Emit<TData>(ProtocolMessage<TData> message, Action<ProtocolMessage<TData>> typedEvent)
    {
        string json = JsonUtility.ToJson(message);
        RawMessageReceived?.Invoke(json);
        typedEvent?.Invoke(message);
    }

    private static long CurrentTimestampMilliseconds()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }

    private static string MakeTileKey(int gridX, int gridY)
    {
        return gridX + "," + gridY;
    }
}
