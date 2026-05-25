using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player")]
    public string username;
    public int player_id;

    [Header("Game")]
    public int game_id;
    public int gold;
    public int score;
    public int kill_count;
    public int base_hp;
    public int time_used;
    public bool is_win;
    public bool is_game_started;
    public bool is_game_over;

    [Header("Config")]
    public List<TowerConfigData> tower_config = new List<TowerConfigData>();
    public MapConfigData current_map_config;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetLoginResult(LoginResultData loginResult)
    {
        if (loginResult == null)
        {
            Debug.LogWarning("[GameManager] SetLoginResult ignored null data.");
            return;
        }

        if (!loginResult.success)
        {
            Debug.LogWarning("[GameManager] Login failed: " + loginResult.message);
            return;
        }

        player_id = loginResult.player_id;
        username = loginResult.username;

        Debug.Log("[GameManager] Login state saved. player_id=" + player_id + ", username=" + username);
    }

    public void SetGameStart(int newGameId, GameStartData gameStart)
    {
        if (gameStart == null)
        {
            Debug.LogWarning("[GameManager] SetGameStart ignored null data.");
            return;
        }

        game_id = newGameId;
        base_hp = gameStart.base_hp;
        time_used = 0;
        is_win = false;
        is_game_started = true;
        is_game_over = false;

        if (gameStart.player != null)
        {
            UpdatePlayerState(gameStart.player, gameStart.base_hp);
        }

        tower_config = gameStart.tower_config != null
            ? new List<TowerConfigData>(gameStart.tower_config)
            : new List<TowerConfigData>();

        current_map_config = gameStart.map;
        if (HasServerMapConfig())
        {
            Debug.Log("[GameManager] Server map saved. map_id=" + current_map_config.map_id
                + ", size=" + current_map_config.width + "x" + current_map_config.height
                + ", path_points=" + current_map_config.path_points.Count);
        }
        else
        {
            Debug.LogWarning("[GameManager] game_start did not include a usable map. BattleScene will use fallback map.");
        }

        Debug.Log("[GameManager] Game started. game_id=" + game_id + ", tower_config_count=" + tower_config.Count);
    }

    public void UpdatePlayerState(PlayerStateData playerState, int newBaseHp)
    {
        if (playerState != null)
        {
            player_id = playerState.player_id;
            if (!string.IsNullOrEmpty(playerState.username))
            {
                username = playerState.username;
            }

            gold = playerState.gold;
            score = playerState.score;
            kill_count = playerState.kill_count;
        }

        base_hp = newBaseHp;
    }

    public void SetGameOver(GameOverData gameOver)
    {
        if (gameOver == null)
        {
            Debug.LogWarning("[GameManager] SetGameOver ignored null data.");
            return;
        }

        is_game_over = true;
        is_game_started = false;
        is_win = gameOver.is_win;
        time_used = gameOver.time_used;
        base_hp = gameOver.base_hp;

        if (gameOver.player != null)
        {
            UpdatePlayerState(gameOver.player, gameOver.base_hp);
        }

        Debug.Log("[GameManager] Game over. is_win=" + gameOver.is_win + ", score=" + score);
    }

    public void ResetGameState()
    {
        game_id = 0;
        gold = 0;
        score = 0;
        kill_count = 0;
        base_hp = 0;
        time_used = 0;
        is_win = false;
        is_game_started = false;
        is_game_over = false;
        tower_config.Clear();
        current_map_config = null;

        Debug.Log("[GameManager] Game state reset.");
    }

    public MapConfigData GetCurrentMapConfig()
    {
        return current_map_config;
    }

    public TowerConfigData GetTowerConfigById(int towerId)
    {
        if (tower_config == null)
        {
            return null;
        }

        for (int i = 0; i < tower_config.Count; i++)
        {
            TowerConfigData config = tower_config[i];
            if (config != null && config.tower_id == towerId)
            {
                return config;
            }
        }

        return null;
    }

    public List<TowerConfigData> GetTowerConfigs()
    {
        return tower_config != null
            ? new List<TowerConfigData>(tower_config)
            : new List<TowerConfigData>();
    }

    public bool HasServerMapConfig()
    {
        return current_map_config != null
            && current_map_config.width > 0
            && current_map_config.height > 0
            && current_map_config.path_points != null
            && current_map_config.path_points.Count > 0;
    }
}
