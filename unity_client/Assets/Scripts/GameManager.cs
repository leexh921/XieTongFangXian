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
    public bool is_game_started;
    public bool is_game_over;

    [Header("Config")]
    public List<TowerConfigData> tower_config = new List<TowerConfigData>();

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
        is_game_started = true;
        is_game_over = false;

        if (gameStart.player != null)
        {
            UpdatePlayerState(gameStart.player, gameStart.base_hp);
        }

        tower_config = gameStart.tower_config != null
            ? new List<TowerConfigData>(gameStart.tower_config)
            : new List<TowerConfigData>();

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
        is_game_started = false;
        is_game_over = false;
        tower_config.Clear();

        Debug.Log("[GameManager] Game state reset.");
    }
}
