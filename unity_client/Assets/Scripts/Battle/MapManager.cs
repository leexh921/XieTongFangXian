using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Grid")]
    public int width = BattleMapConfig.Width;
    public int height = BattleMapConfig.Height;
    public float cellSize = BattleMapConfig.CellSize;
    public Transform tileRoot;
    public GameObject tilePrefab;
    public Transform towerRoot;
    public GameObject towerPrefab;

    [Header("References")]
    public NetworkManager networkManager;
    public GameManager gameManager;
    public BattleUI battleUI;
    public TowerSelectionUI towerSelectionUI;

    [Header("Buildable Sprites")]
    public Sprite buildableCenterSprite;
    public Sprite buildableTopSprite;
    public Sprite buildableBottomSprite;
    public Sprite buildableLeftSprite;
    public Sprite buildableRightSprite;
    public Sprite buildableTopLeftSprite;
    public Sprite buildableTopRightSprite;
    public Sprite buildableBottomLeftSprite;
    public Sprite buildableBottomRightSprite;

    [Header("Special Sprites")]
    public Sprite pathSprite;
    public Sprite obstacleSprite;
    public Sprite castleSprite;
    public Sprite emptySprite;

    [Header("Tile Scale")]
    public Vector3 buildableScale = Vector3.one;
    public Vector3 pathScale = Vector3.one;
    public Vector3 obstacleScale = Vector3.one;
    public Vector3 castleScale = Vector3.one;

    [Header("Overlay")]
    public Transform overlayRoot;

    [Header("Overlay Prefabs")]
    public GameObject obstaclePrefab;
    public GameObject pathPrefab;
    public GameObject castlePrefab;

    private readonly Dictionary<string, TileButton> tiles = new Dictionary<string, TileButton>();
    private readonly Dictionary<Vector2Int, TileButton> tileMap = new Dictionary<Vector2Int, TileButton>();
    private readonly Dictionary<string, TowerView> towerInstances = new Dictionary<string, TowerView>();
    private readonly HashSet<Vector2Int> occupiedTowerTiles = new HashSet<Vector2Int>();

    private MapConfigData activeMap;
    private Sprite fallbackSprite;

    private void Start()
    {
        ResolveReferences();
        SubscribeNetworkEvents();
        EnsureTowerRoot();
        GenerateMap();
        FitCameraToMap();

        if (battleUI != null)
        {
            battleUI.Refresh();
            battleUI.ShowMessage("点击浅绿色地块后选择要建造的防御塔");
        }
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(world.x, world.y);
        Collider2D hit = Physics2D.OverlapPoint(point);

        if (hit == null)
        {
            return;
        }

        TileButton tile = hit.GetComponent<TileButton>();
        if (tile != null)
        {
            tile.HandleClick();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeNetworkEvents();
    }

    public void GenerateMap()
    {
        EnsureTileRoot();
        EnsureOverlayRoot();
        EnsureFallbackSprite();
        ClearExistingTiles();

        activeMap = BattleMapConfig.GetActiveMapConfig();

        if (gameManager != null
            && gameManager.GetCurrentMapConfig() != null
            && !BattleMapConfig.IsUsableMap(gameManager.GetCurrentMapConfig()))
        {
            Debug.LogWarning("[MapManager] Server map_config is invalid or has empty path_points. Fallback map will be used.");
            activeMap = BattleMapConfig.CreateDefaultMapConfig();
        }

        if (activeMap.castle == null)
        {
            activeMap.castle = BattleMapConfig.GetCastlePoint(activeMap);
            Debug.LogWarning("[MapManager] map.castle is missing. Using last path point as castle.");
        }

        width = activeMap.width;
        height = activeMap.height;
        cellSize = BattleMapConfig.CellSize;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 永远先生成草地底图
                CreateTile(x, y, TileType.Buildable);

                TileButton tile = GetTile(x, y);
                if (tile == null)
                {
                    continue;
                }

                TileType overlayType = GetTileTypeFromConfig(x, y);

                if (overlayType == TileType.Obstacle)
                {
                    tile.tileType = TileType.Obstacle;
                    tile.is_buildable = false;

                    CreateOverlay(obstaclePrefab, x, y, obstacleScale, "Obstacle");
                }
                else if (overlayType == TileType.Path)
                {
                    tile.tileType = TileType.Path;
                    tile.is_buildable = false;

                    CreateOverlay(pathPrefab, x, y, pathScale, "Path");
                }
                else if (overlayType == TileType.Castle)
                {
                    tile.tileType = TileType.Castle;
                    tile.is_buildable = false;

                    CreateOverlay(castlePrefab, x, y, castleScale, "Castle");
                }
            }
        }
    }

    public void OnTileClicked(int gridX, int gridY)
    {
        ResolveReferences();

        if (gameManager != null && gameManager.is_game_over)
        {
            ShowTileMessage("游戏已结束");
            return;
        }

        TileButton tile = GetTile(gridX, gridY);
        if (tile == null)
        {
            ShowTileMessage("地块不存在: " + gridX + "," + gridY);
            return;
        }

        if (tile.is_occupied)
        {
            ShowTileMessage("该地块已有塔");
            return;
        }

        if (tile.tileType != TileType.Buildable || !tile.is_buildable)
        {
            ShowTileMessage(GetInvalidBuildMessage(tile));
            return;
        }

        if (towerSelectionUI == null)
        {
            towerSelectionUI = TowerSelectionUI.FindOrCreate();
        }

        if (towerSelectionUI == null)
        {
            ShowTileMessage("建塔弹窗未初始化");
            return;
        }

        Debug.Log("[MapManager] Open build popup at " + gridX + "," + gridY);
        towerSelectionUI.ShowForTile(gridX, gridY, tile.transform.position);
        ShowTileMessage("请选择要建造的防御塔");
    }

    public void ShowTileMessage(string message)
    {
        if (battleUI != null)
        {
            battleUI.ShowMessage(message);
        }
    }

    public void MarkTileOccupied(int gridX, int gridY)
    {
        TileButton tile = GetTile(gridX, gridY);
        if (tile != null)
        {
            tile.SetOccupied(true);
            tile.SetVisual(
                GetSpriteForTile(tile.tileType, gridX, gridY),
                new Color(0.38f, 0.72f, 0.42f, 1f)
            );
        }
    }

    private void HandleBuildResult(BuildResultData buildResult)
    {
        ResolveReferences();

        if (buildResult == null)
        {
            ShowTileMessage("建塔失败：服务器返回为空");
            return;
        }

        if (battleUI != null)
        {
            battleUI.Refresh();
        }

        if (buildResult.success)
        {
            if (buildResult.tower != null)
            {
                CreateTowerFromBuildResult(buildResult.tower);
            }
            else
            {
                ShowTileMessage("建塔成功");
            }
        }
        else
        {
            ShowTileMessage("建塔失败: " + GetBuildFailureMessage(buildResult.reason));
        }
    }

    private void CreateTile(int gridX, int gridY, TileType type)
    {
        GameObject tileObject;

        if (tilePrefab != null)
        {
            tileObject = Instantiate(tilePrefab, tileRoot);
        }
        else
        {
            tileObject = CreateDefaultTileObject();
            tileObject.transform.SetParent(tileRoot, false);
        }

        tileObject.name = "Tile_" + gridX + "_" + gridY + "_" + type;
        tileObject.transform.position = BattleMapConfig.GridToWorld(gridX, gridY, activeMap);
        tileObject.transform.localScale = GetScaleForTile(type);

        var tile = tileObject.GetComponent<TileButton>();
        if (tile == null)
        {
            tile = tileObject.AddComponent<TileButton>();
        }

        var renderer = tileObject.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = tileObject.AddComponent<SpriteRenderer>();
        }

        var collider2d = tileObject.GetComponent<BoxCollider2D>();
        if (collider2d == null)
        {
            collider2d = tileObject.AddComponent<BoxCollider2D>();
        }

        tile.spriteRenderer = renderer;
        tile.Init(gridX, gridY, type, this);

        // 底图永远显示草地
        tile.SetVisual(
            GetBuildableSprite(gridX, gridY),
            GetColorForTile(TileType.Buildable)
        );

        tiles[MakeKey(gridX, gridY)] = tile;
        tileMap[new Vector2Int(gridX, gridY)] = tile;
    }

    private void CreateOverlay(GameObject prefab, int gridX, int gridY, Vector3 scale, string objectName)
    {
        if (prefab == null)
        {
            return;
        }

        EnsureOverlayRoot();

        if (overlayRoot == null)
        {
            return;
        }

        GameObject obj = Instantiate(prefab, overlayRoot);

        obj.name = objectName + "_" + gridX + "_" + gridY;
        obj.transform.position = BattleMapConfig.GridToWorld(gridX, gridY, activeMap);
        obj.transform.localScale = scale;

        // 非常重要：覆盖物不允许遮挡点击
        Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private void CreateTowerFromBuildResult(TowerStateData towerData)
    {
        if (towerData == null)
        {
            ShowTileMessage("建塔失败：缺少塔数据");
            return;
        }

        if (!string.IsNullOrEmpty(towerData.instance_id)
            && towerInstances.ContainsKey(towerData.instance_id))
        {
            ShowTileMessage("建塔成功: " + towerData.grid_x + "," + towerData.grid_y);
            return;
        }

        var tilePosition = new Vector2Int(towerData.grid_x, towerData.grid_y);
        TileButton tile = GetTile(towerData.grid_x, towerData.grid_y);

        if (tile == null)
        {
            ShowTileMessage("建塔失败：地块不存在 " + towerData.grid_x + "," + towerData.grid_y);
            return;
        }

        if (occupiedTowerTiles.Contains(tilePosition) || tile.is_occupied)
        {
            MarkTileOccupied(towerData.grid_x, towerData.grid_y);
            ShowTileMessage("该地块已有塔: " + towerData.grid_x + "," + towerData.grid_y);
            return;
        }

        EnsureTowerRoot();

        GameObject towerObject;

        if (towerPrefab != null)
        {
            towerObject = Instantiate(towerPrefab, towerRoot);
        }
        else
        {
            towerObject = CreateDefaultTowerObject();
            towerObject.transform.SetParent(towerRoot, false);
        }

        towerObject.name = string.IsNullOrEmpty(towerData.instance_id)
            ? "Tower_" + towerData.grid_x + "_" + towerData.grid_y
            : towerData.instance_id;

        towerObject.transform.position = new Vector3(
            tile.transform.position.x,
            tile.transform.position.y,
            -0.1f
        );

        towerObject.transform.localScale = new Vector3(
            cellSize * 0.55f,
            cellSize * 0.55f,
            1f
        );

        var towerView = towerObject.GetComponent<TowerView>();
        if (towerView == null)
        {
            towerView = towerObject.AddComponent<TowerView>();
        }

        towerView.Init(
            towerData.instance_id,
            towerData.tower_id,
            towerData.owner_player_id,
            towerData.grid_x,
            towerData.grid_y
        );

        string instanceKey = string.IsNullOrEmpty(towerData.instance_id)
            ? "tower_" + towerData.grid_x + "_" + towerData.grid_y
            : towerData.instance_id;

        towerInstances[instanceKey] = towerView;
        occupiedTowerTiles.Add(tilePosition);
        MarkTileOccupied(towerData.grid_x, towerData.grid_y);

        if (battleUI != null)
        {
            battleUI.Refresh();
        }

        ShowTileMessage("建塔成功: " + towerData.grid_x + "," + towerData.grid_y);
    }

    private GameObject CreateDefaultTileObject()
    {
        var tileObject = new GameObject("Tile");
        var renderer = tileObject.AddComponent<SpriteRenderer>();
        renderer.sprite = fallbackSprite;
        tileObject.AddComponent<BoxCollider2D>();
        tileObject.AddComponent<TileButton>();
        return tileObject;
    }

    private GameObject CreateDefaultTowerObject()
    {
        var towerObject = new GameObject("Tower");
        towerObject.AddComponent<SpriteRenderer>();
        towerObject.AddComponent<TowerView>();
        return towerObject;
    }

    private void EnsureTileRoot()
    {
        if (tileRoot != null)
        {
            return;
        }

        var root = GameObject.Find("TileRoot");
        if (root == null)
        {
            root = new GameObject("TileRoot");
            root.transform.SetParent(transform, false);
        }

        tileRoot = root.transform;
    }

    private void EnsureTowerRoot()
    {
        if (towerRoot != null)
        {
            return;
        }

        var root = GameObject.Find("TowerRoot");
        if (root == null)
        {
            root = new GameObject("TowerRoot");
            var mapRoot = GameObject.Find("MapRoot");
            root.transform.SetParent(mapRoot != null ? mapRoot.transform : transform, false);
        }

        towerRoot = root.transform;
    }

    private void EnsureOverlayRoot()
    {
        if (overlayRoot != null)
        {
            return;
        }

        var root = GameObject.Find("OverlayRoot");
        if (root == null)
        {
            root = new GameObject("OverlayRoot");
            root.transform.SetParent(transform, false);
        }

        overlayRoot = root.transform;
    }

    private void EnsureFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return;
        }

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "RuntimeTileFallbackTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        fallbackSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );

        fallbackSprite.name = "RuntimeTileFallbackSprite";
    }

    private void ClearExistingTiles()
    {
        tiles.Clear();
        tileMap.Clear();

        if (tileRoot != null)
        {
            for (int i = tileRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(tileRoot.GetChild(i).gameObject);
            }
        }

        if (overlayRoot != null)
        {
            for (int i = overlayRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(overlayRoot.GetChild(i).gameObject);
            }
        }
    }

    private TileType GetTileTypeFromConfig(int gridX, int gridY)
    {
        if (BattleMapConfig.IsCastleGrid(gridX, gridY, activeMap))
        {
            return TileType.Castle;
        }

        if (BattleMapConfig.IsObstacleGrid(gridX, gridY, activeMap))
        {
            return TileType.Obstacle;
        }

        if (BattleMapConfig.IsPathGrid(gridX, gridY, activeMap))
        {
            return TileType.Path;
        }

        return TileType.Buildable;
    }

    private Sprite GetSpriteForTile(TileType type, int x, int y)
    {
        Sprite selected = null;

        if (type == TileType.Buildable)
        {
            selected = GetBuildableSprite(x, y);
        }
        else if (type == TileType.Path)
        {
            selected = pathSprite;
        }
        else if (type == TileType.Obstacle)
        {
            selected = obstacleSprite;
        }
        else if (type == TileType.Castle)
        {
            selected = castleSprite;
        }
        else
        {
            selected = emptySprite;
        }

        return selected != null ? selected : fallbackSprite;
    }

    private Sprite GetBuildableSprite(int x, int y)
    {
        if (x == 0 && y == 0)
        {
            return buildableBottomLeftSprite != null ? buildableBottomLeftSprite : buildableCenterSprite;
        }

        if (x == width - 1 && y == 0)
        {
            return buildableBottomRightSprite != null ? buildableBottomRightSprite : buildableCenterSprite;
        }

        if (x == 0 && y == height - 1)
        {
            return buildableTopLeftSprite != null ? buildableTopLeftSprite : buildableCenterSprite;
        }

        if (x == width - 1 && y == height - 1)
        {
            return buildableTopRightSprite != null ? buildableTopRightSprite : buildableCenterSprite;
        }

        if (y == height - 1)
        {
            return buildableTopSprite != null ? buildableTopSprite : buildableCenterSprite;
        }

        if (y == 0)
        {
            return buildableBottomSprite != null ? buildableBottomSprite : buildableCenterSprite;
        }

        if (x == 0)
        {
            return buildableLeftSprite != null ? buildableLeftSprite : buildableCenterSprite;
        }

        if (x == width - 1)
        {
            return buildableRightSprite != null ? buildableRightSprite : buildableCenterSprite;
        }

        return buildableCenterSprite;
    }

    private Color GetColorForTile(TileType type)
    {
        switch (type)
        {
            case TileType.Buildable:
                return new Color(0.62f, 0.9f, 0.62f, 1f);

            case TileType.Path:
                return new Color(0.78f, 0.58f, 0.28f, 1f);

            case TileType.Obstacle:
                return new Color(0.45f, 0.47f, 0.5f, 1f);

            case TileType.Castle:
                return new Color(0.25f, 0.26f, 0.58f, 1f);

            default:
                return new Color(0.12f, 0.14f, 0.18f, 0.45f);
        }
    }

    private Vector3 GetScaleForTile(TileType type)
    {
        switch (type)
        {
            case TileType.Buildable:
                return buildableScale;

            case TileType.Path:
                return pathScale;

            case TileType.Obstacle:
                return obstacleScale;

            case TileType.Castle:
                return castleScale;

            default:
                return Vector3.one;
        }
    }

    private void FitCameraToMap()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        mainCamera.orthographic = true;
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);

        MapConfigData map = BattleMapConfig.IsUsableMap(activeMap)
            ? activeMap
            : BattleMapConfig.GetActiveMapConfig();

        float mapWorldWidth = map.width * BattleMapConfig.CellSize;
        float mapWorldHeight = map.height * BattleMapConfig.CellSize;
        float aspect = mainCamera.aspect > 0f ? mainCamera.aspect : 16f / 9f;

        float sizeByHeight = mapWorldHeight * 0.5f + 0.9f;
        float sizeByWidth = mapWorldWidth * 0.5f / aspect + 0.9f;

        mainCamera.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }

    private void ResolveReferences()
    {
        if (NetworkManager.Instance != null)
        {
            networkManager = NetworkManager.Instance;
        }
        else if (networkManager == null)
        {
            networkManager = FindObjectOfType<NetworkManager>();
        }

        if (GameManager.Instance != null)
        {
            gameManager = GameManager.Instance;
        }
        else if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (battleUI == null)
        {
            battleUI = FindObjectOfType<BattleUI>();
        }

        if (towerSelectionUI == null)
        {
            towerSelectionUI = FindObjectOfType<TowerSelectionUI>();
        }

        if (towerSelectionUI == null)
        {
            towerSelectionUI = TowerSelectionUI.FindOrCreate();
        }

        if (towerRoot == null)
        {
            var root = GameObject.Find("TowerRoot");
            if (root != null)
            {
                towerRoot = root.transform;
            }
        }
    }

    private void SubscribeNetworkEvents()
    {
        if (networkManager == null)
        {
            ResolveReferences();
        }

        if (networkManager != null)
        {
            networkManager.OnBuildResult -= HandleBuildResult;
            networkManager.OnBuildResult += HandleBuildResult;
        }
    }

    private void UnsubscribeNetworkEvents()
    {
        if (networkManager != null)
        {
            networkManager.OnBuildResult -= HandleBuildResult;
        }
    }

    private string MakeKey(int gridX, int gridY)
    {
        return gridX + "," + gridY;
    }

    private TileButton GetTile(int gridX, int gridY)
    {
        TileButton tile;

        if (tileMap.TryGetValue(new Vector2Int(gridX, gridY), out tile))
        {
            return tile;
        }

        tiles.TryGetValue(MakeKey(gridX, gridY), out tile);
        return tile;
    }

    private string GetBuildFailureMessage(string reason)
    {
        switch (reason)
        {
            case "not_enough_gold":
                return "金币不足";

            case "tile_occupied":
                return "该地块已有塔";

            case "invalid_tower":
                return "无效的防御塔";

            case "invalid_player":
                return "无效玩家";

            case "game_over":
                return "游戏已结束";

            default:
                return string.IsNullOrEmpty(reason) ? "未知原因" : reason;
        }
    }

    private string GetInvalidBuildMessage(TileButton tile)
    {
        if (tile == null)
        {
            return "地块不存在";
        }

        if (tile.is_occupied)
        {
            return "该地块已有塔";
        }

        switch (tile.tileType)
        {
            case TileType.Path:
                return "路径不可建造";

            case TileType.Obstacle:
                return "障碍物不可建造";

            case TileType.Castle:
                return "堡垒位置不可建造";

            default:
                return "不可建造";
        }
    }
}