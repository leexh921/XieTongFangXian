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
    [SerializeField] private float stateUpdateIntervalSeconds = 0.2f;
    [SerializeField] private float monsterSpawnIntervalSeconds = 1.8f;

    private int nextPlayerId = 1;
    private const int MockGameId = 10001;
    private const int BasicTowerId = 1;
    private const int InitialGold = 250;
    private const int InitialBaseHp = 10;
    private const int BasicTowerCost = 100;
    private const int BasicTowerAttack = 12;
    private const float BasicTowerRange = 1.8f;
    private const float BasicTowerCooldown = 0.8f;
    private const float TargetRadiusPadding = 0.35f;
    private const float GoldPerSecond = 0.5f;
    private const int BaseMonsterHp = 36;
    private const float BaseMonsterSpeed = 1.3f;
    private const int MonsterScoreValue = 20;
    private const int MonsterRewardGold = 15;
    private const int MonsterDamageToBase = 1;
    private const int MaxMonsters = 12;
    private const int DemoVictoryTimeSeconds = 60;
    private float currentGold = InitialGold;
    private int currentScore;
    private int currentKillCount;
    private int currentBaseHp = InitialBaseHp;
    private int currentPlayerId;
    private int currentLevelId = 1;
    private string currentUsername = string.Empty;
    private int towerSerial = 1;
    private int monsterSerial = 1;
    private float mockGameTime;
    private float nextMonsterSpawnTime;
    private float nextStateLogTime;
    private bool gameOverSent;
    private Coroutine gameLoopCoroutine;
    private readonly HashSet<string> occupiedTiles = new HashSet<string>();
    private readonly List<MockTower> mockTowers = new List<MockTower>();
    private readonly List<MockMonster> mockMonsters = new List<MockMonster>();

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
        ResetMockGameState(playerId, username, levelId);

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
                    base_hp = InitialBaseHp,
                    initial_gold = InitialGold,
                    gold_per_second = GoldPerSecond
                },
                player = new PlayerStateData
                {
                    player_id = playerId,
                    username = username,
                    gold = Mathf.FloorToInt(currentGold),
                    score = 0,
                    kill_count = 0
                },
                tower_config = new List<TowerConfigData>
                {
                    new TowerConfigData
                    {
                        tower_id = BasicTowerId,
                        name = "箭塔",
                        attack = BasicTowerAttack,
                        range = BasicTowerRange,
                        cooldown = BasicTowerCooldown,
                        cost = BasicTowerCost,
                        refund_rate = 0.5f
                    }
                },
                base_hp = InitialBaseHp
            }
        };

        Debug.Log("[MockServer] Send game_start: " + JsonUtility.ToJson(response));
        Emit(response, GameStartReceived);
        StartMockGameLoop();
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

        if (gameOverSent)
        {
            reason = "game_over";
        }
        else if (playerId <= 0)
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
            mockTowers.Add(new MockTower
            {
                state = tower,
                attack = BasicTowerAttack,
                range = BasicTowerRange,
                cooldown = BasicTowerCooldown,
                nextAttackTime = mockGameTime
            });
            towerSerial++;
        }

        bool success = string.IsNullOrEmpty(reason);
        string username = !string.IsNullOrEmpty(currentUsername) ? currentUsername : (GameManager.Instance != null ? GameManager.Instance.username : string.Empty);

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
                    gold = Mathf.FloorToInt(currentGold),
                    score = currentScore,
                    kill_count = currentKillCount
                }
            }
        };

        Debug.Log("[MockServer] Send build_result: " + JsonUtility.ToJson(response));
        Emit(response, BuildResultReceived);
    }

    private IEnumerator MockGameLoop()
    {
        SpawnMonster();

        while (!gameOverSent)
        {
            yield return new WaitForSeconds(stateUpdateIntervalSeconds);

            mockGameTime += stateUpdateIntervalSeconds;

            if (mockGameTime >= nextMonsterSpawnTime)
            {
                if (mockMonsters.Count < MaxMonsters)
                {
                    SpawnMonster();
                }

                nextMonsterSpawnTime = mockGameTime + monsterSpawnIntervalSeconds;
            }

            currentGold += GoldPerSecond * stateUpdateIntervalSeconds;
            UpdateMockMonsters(stateUpdateIntervalSeconds);
            EmitStateUpdate();

            if (currentBaseHp <= 0)
            {
                EmitGameOver(false);
                break;
            }

            if (mockGameTime >= DemoVictoryTimeSeconds && currentBaseHp > 0)
            {
                EmitGameOver(true);
                break;
            }
        }

        gameLoopCoroutine = null;
    }

    private void StartMockGameLoop()
    {
        if (gameLoopCoroutine != null)
        {
            StopCoroutine(gameLoopCoroutine);
        }

        gameLoopCoroutine = StartCoroutine(MockGameLoop());
    }

    private void ResetMockGameState(int playerId, string username, int levelId)
    {
        currentPlayerId = playerId;
        currentLevelId = levelId;
        currentUsername = username;
        currentGold = InitialGold;
        currentScore = 0;
        currentKillCount = 0;
        currentBaseHp = InitialBaseHp;
        mockGameTime = 0f;
        nextMonsterSpawnTime = monsterSpawnIntervalSeconds;
        nextStateLogTime = 0f;
        gameOverSent = false;
        towerSerial = 1;
        monsterSerial = 1;
        occupiedTiles.Clear();
        mockTowers.Clear();
        mockMonsters.Clear();
    }

    private void SpawnMonster()
    {
        int maxHp = GetMonsterMaxHp();
        var monster = new MockMonster
        {
            instanceId = "monster_" + MockGameId + "_" + monsterSerial,
            monsterId = 1,
            hp = maxHp,
            maxHp = maxHp,
            speed = GetMonsterSpeed(),
            pathIndex = 0,
            position = BattleMapConfig.GridToWorld(BattleMapConfig.PathGridPoints[0].x, BattleMapConfig.PathGridPoints[0].y)
        };
        monsterSerial++;
        mockMonsters.Add(monster);
        Debug.Log("[MockServer] Spawn monster: " + monster.instanceId);
    }

    private void UpdateMockMonsters(float deltaTime)
    {
        for (int i = mockMonsters.Count - 1; i >= 0; i--)
        {
            MockMonster monster = mockMonsters[i];

            if (MoveMonster(monster, deltaTime))
            {
                mockMonsters.RemoveAt(i);
                currentBaseHp = Mathf.Max(0, currentBaseHp - MonsterDamageToBase);
                Debug.Log("[MockServer] Monster reached base. base_hp=" + currentBaseHp);
            }
        }

        ApplyTowerAttacks();
        RemoveDeadMonsters();
    }

    private bool MoveMonster(MockMonster monster, float deltaTime)
    {
        if (monster.pathIndex >= BattleMapConfig.PathGridPoints.Length - 1)
        {
            return true;
        }

        Vector2Int targetGrid = BattleMapConfig.PathGridPoints[monster.pathIndex + 1];
        Vector2 target = BattleMapConfig.GridToWorld(targetGrid.x, targetGrid.y);
        Vector2 next = Vector2.MoveTowards(monster.position, target, monster.speed * deltaTime);
        monster.position = next;

        if (Vector2.Distance(monster.position, target) <= 0.001f)
        {
            monster.pathIndex++;
        }

        return monster.pathIndex >= BattleMapConfig.PathGridPoints.Length - 1;
    }

    private void ApplyTowerAttacks()
    {
        if (mockTowers.Count == 0 || mockMonsters.Count == 0)
        {
            return;
        }

        for (int i = 0; i < mockTowers.Count; i++)
        {
            MockTower tower = mockTowers[i];
            if (mockGameTime < tower.nextAttackTime)
            {
                continue;
            }

            MockMonster target = FindNearestMonsterInRange(tower);
            if (target == null)
            {
                continue;
            }

            target.hp -= tower.attack;
            tower.nextAttackTime = mockGameTime + tower.cooldown;
            Debug.Log("[MockServer] Tower attack. tower=" + tower.state.instance_id
                + ", target=" + target.instanceId
                + ", damage=" + tower.attack
                + ", hp=" + Mathf.Max(0, target.hp) + "/" + target.maxHp);
        }
    }

    private MockMonster FindNearestMonsterInRange(MockTower tower)
    {
        Vector3 towerPosition = BattleMapConfig.GridToWorld(tower.state.grid_x, tower.state.grid_y);
        float rangeWorld = tower.range * BattleMapConfig.CellSize + TargetRadiusPadding * BattleMapConfig.CellSize;
        float rangeSqr = rangeWorld * rangeWorld;
        float bestDistanceSqr = float.MaxValue;
        MockMonster bestTarget = null;

        for (int i = 0; i < mockMonsters.Count; i++)
        {
            MockMonster monster = mockMonsters[i];
            if (monster.hp <= 0)
            {
                continue;
            }

            float distanceSqr = ((Vector2)towerPosition - monster.position).sqrMagnitude;
            if (distanceSqr <= rangeSqr && distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                bestTarget = monster;
            }
        }

        return bestTarget;
    }

    private void RemoveDeadMonsters()
    {
        for (int i = mockMonsters.Count - 1; i >= 0; i--)
        {
            MockMonster monster = mockMonsters[i];
            if (monster.hp > 0)
            {
                continue;
            }

            mockMonsters.RemoveAt(i);
            currentScore += MonsterScoreValue;
            currentKillCount += 1;
            currentGold += MonsterRewardGold;
            Debug.Log("[MockServer] Monster killed: " + monster.instanceId);
        }
    }

    private int GetMonsterMaxHp()
    {
        if (mockGameTime >= 30f)
        {
            return 70;
        }

        if (mockGameTime >= 15f)
        {
            return 52;
        }

        return BaseMonsterHp;
    }

    private float GetMonsterSpeed()
    {
        return mockGameTime >= 30f ? 1.5f : BaseMonsterSpeed;
    }

    private void EmitStateUpdate()
    {
        var monsters = new List<MonsterStateData>();
        for (int i = 0; i < mockMonsters.Count; i++)
        {
            MockMonster monster = mockMonsters[i];
            monsters.Add(new MonsterStateData
            {
                instance_id = monster.instanceId,
                monster_id = monster.monsterId,
                hp = monster.hp,
                max_hp = monster.maxHp,
                x = monster.position.x,
                y = monster.position.y,
                path_index = monster.pathIndex
            });
        }

        var message = new ProtocolMessage<StateUpdateData>
        {
            type = MessageTypes.StateUpdate,
            game_id = MockGameId,
            player_id = currentPlayerId,
            timestamp = CurrentTimestampMilliseconds(),
            data = new StateUpdateData
            {
                game_time_sec = mockGameTime,
                base_hp = currentBaseHp,
                player = new PlayerStateData
                {
                    player_id = currentPlayerId,
                    username = currentUsername,
                gold = Mathf.FloorToInt(currentGold),
                score = currentScore,
                kill_count = currentKillCount
            },
                monsters = monsters,
                towers = GetTowerStates()
            }
        };

        if (mockGameTime >= nextStateLogTime)
        {
            Debug.Log("[MockServer] Push state_update: time=" + mockGameTime.ToString("0.0")
                + ", monsters=" + monsters.Count
                + ", towers=" + mockTowers.Count
                + ", gold=" + Mathf.FloorToInt(currentGold)
                + ", score=" + currentScore
                + ", base_hp=" + currentBaseHp);
            nextStateLogTime = mockGameTime + 1f;
        }

        Emit(message, StateUpdateReceived);
    }

    private void EmitGameOver(bool isWin)
    {
        if (gameOverSent)
        {
            return;
        }

        gameOverSent = true;

        var message = new ProtocolMessage<GameOverData>
        {
            type = MessageTypes.GameOver,
            game_id = MockGameId,
            player_id = currentPlayerId,
            timestamp = CurrentTimestampMilliseconds(),
            data = new GameOverData
            {
                level_id = currentLevelId,
                is_win = isWin,
                time_used = Mathf.FloorToInt(mockGameTime),
                base_hp = currentBaseHp,
                player = new PlayerStateData
                {
                    player_id = currentPlayerId,
                    username = currentUsername,
                    gold = Mathf.FloorToInt(currentGold),
                    score = currentScore,
                    kill_count = currentKillCount
                }
            }
        };

        Debug.Log("[MockServer] Mock game over. is_win=" + isWin
            + ", score=" + currentScore
            + ", kill_count=" + currentKillCount
            + ", time_used=" + Mathf.FloorToInt(mockGameTime)
            + ", base_hp=" + currentBaseHp);
        Emit(message, GameOverReceived);
    }

    private List<TowerStateData> GetTowerStates()
    {
        var towers = new List<TowerStateData>();
        for (int i = 0; i < mockTowers.Count; i++)
        {
            towers.Add(mockTowers[i].state);
        }

        return towers;
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

    private class MockMonster
    {
        public string instanceId;
        public int monsterId;
        public int hp;
        public int maxHp;
        public float speed;
        public int pathIndex;
        public Vector2 position;
    }

    private class MockTower
    {
        public TowerStateData state;
        public int attack;
        public float range;
        public float cooldown;
        public float nextAttackTime;
    }
}
