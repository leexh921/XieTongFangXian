using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    [Header("Grid")]
    public int width = 10;
    public int height = 6;
    public float cellSize = 1.2f;
    public Transform tileRoot;
    public GameObject tilePrefab;

    [Header("References")]
    public NetworkManager networkManager;
    public GameManager gameManager;
    public BattleUI battleUI;
    public int selectedTowerId = 1;
    public bool markTileOccupiedOnBuildResult;

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
    public Sprite emptySprite;

    [Header("Layout")]
    public string[] mapLayout =
    {
        "BBBBBBBBBB",
        "BBOBBBBBBB",
        "PPPPPPBBBB",
        "BBBBBPBBBB",
        "BBBBBPPPPP",
        "BBBBBBBBBB"
    };

    private readonly Dictionary<string, TileButton> tiles = new Dictionary<string, TileButton>();
    private Sprite fallbackSprite;

    private void Start()
    {
        ResolveReferences();
        SubscribeNetworkEvents();
        GenerateMap();

        if (battleUI != null)
        {
            battleUI.Refresh();
            battleUI.ShowMessage("点击浅绿色地块请求建塔");
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
        EnsureFallbackSprite();
        ClearExistingTiles();

        if (mapLayout == null || mapLayout.Length == 0)
        {
            mapLayout = new[] { "BBBBBBBBBB", "PPPPPPBBBB", "BBBBBBBBBB" };
        }

        height = mapLayout.Length;
        width = 0;
        for (int row = 0; row < mapLayout.Length; row++)
        {
            if (!string.IsNullOrEmpty(mapLayout[row]) && mapLayout[row].Length > width)
            {
                width = mapLayout[row].Length;
            }
        }

        for (int row = 0; row < height; row++)
        {
            string rowText = mapLayout[row] ?? string.Empty;
            for (int x = 0; x < width; x++)
            {
                int y = height - 1 - row;
                char code = x < rowText.Length ? rowText[x] : 'E';
                TileType type = ParseTileType(code);
                CreateTile(x, y, type);
            }
        }
    }

    public void OnTileClicked(int gridX, int gridY)
    {
        ResolveReferences();

        string message = "建塔请求已发送: " + gridX + "," + gridY;
        Debug.Log("[MapManager] " + message + ", tower_id=" + selectedTowerId);

        if (battleUI != null)
        {
            battleUI.ShowMessage(message);
        }

        if (networkManager != null)
        {
            networkManager.SendBuildRequest(selectedTowerId, gridX, gridY);
        }
        else
        {
            Debug.LogWarning("[MapManager] Missing NetworkManager. build_request was not sent.");
        }
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
        TileButton tile;
        if (tiles.TryGetValue(MakeKey(gridX, gridY), out tile) && tile != null)
        {
            tile.is_occupied = true;
            tile.SetVisual(GetSpriteForTile(tile.tileType, gridX, gridY), new Color(0.38f, 0.72f, 0.42f, 1f));
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
                if (markTileOccupiedOnBuildResult)
                {
                    MarkTileOccupied(buildResult.tower.grid_x, buildResult.tower.grid_y);
                }

                ShowTileMessage("建塔成功: " + buildResult.tower.grid_x + "," + buildResult.tower.grid_y);
            }
            else
            {
                ShowTileMessage("建塔成功");
            }
        }
        else
        {
            ShowTileMessage("建塔失败: " + buildResult.reason);
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
        float originX = -(width - 1) * cellSize * 0.5f;
        float originY = -(height - 1) * cellSize * 0.5f;
        tileObject.transform.localPosition = new Vector3(originX + gridX * cellSize, originY + gridY * cellSize, 0f);
        tileObject.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 1f);

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
        tile.SetVisual(GetSpriteForTile(type, gridX, gridY), GetColorForTile(type));

        tiles[MakeKey(gridX, gridY)] = tile;
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
        fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        fallbackSprite.name = "RuntimeTileFallbackSprite";
    }

    private void ClearExistingTiles()
    {
        tiles.Clear();
        if (tileRoot == null)
        {
            return;
        }

        for (int i = tileRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(tileRoot.GetChild(i).gameObject);
        }
    }

    private TileType ParseTileType(char code)
    {
        switch (code)
        {
            case 'B':
            case 'b':
                return TileType.Buildable;
            case 'P':
            case 'p':
                return TileType.Path;
            case 'O':
            case 'o':
                return TileType.Obstacle;
            default:
                return TileType.Empty;
        }
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
        else
        {
            selected = emptySprite;
        }

        return selected != null ? selected : fallbackSprite;
    }

    private Sprite GetBuildableSprite(int x, int y)
    {
        if (x == 0 && y == 0) return buildableBottomLeftSprite != null ? buildableBottomLeftSprite : buildableCenterSprite;
        if (x == width - 1 && y == 0) return buildableBottomRightSprite != null ? buildableBottomRightSprite : buildableCenterSprite;
        if (x == 0 && y == height - 1) return buildableTopLeftSprite != null ? buildableTopLeftSprite : buildableCenterSprite;
        if (x == width - 1 && y == height - 1) return buildableTopRightSprite != null ? buildableTopRightSprite : buildableCenterSprite;
        if (y == height - 1) return buildableTopSprite != null ? buildableTopSprite : buildableCenterSprite;
        if (y == 0) return buildableBottomSprite != null ? buildableBottomSprite : buildableCenterSprite;
        if (x == 0) return buildableLeftSprite != null ? buildableLeftSprite : buildableCenterSprite;
        if (x == width - 1) return buildableRightSprite != null ? buildableRightSprite : buildableCenterSprite;
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
            default:
                return new Color(0.12f, 0.14f, 0.18f, 0.45f);
        }
    }

    private void ResolveReferences()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.Instance != null ? NetworkManager.Instance : FindObjectOfType<NetworkManager>();
        }

        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        }

        if (battleUI == null)
        {
            battleUI = FindObjectOfType<BattleUI>();
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
}
