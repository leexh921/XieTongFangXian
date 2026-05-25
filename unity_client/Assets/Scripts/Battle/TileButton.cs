using UnityEngine;

public enum TileType
{
    Buildable,
    Path,
    Obstacle,
    Castle,
    Empty
}

public class TileButton : MonoBehaviour
{
    public int grid_x;
    public int grid_y;
    public TileType tileType = TileType.Buildable;
    public bool is_buildable = true;
    public bool is_occupied;
    public MapManager mapManager;
    public SpriteRenderer spriteRenderer;

    private int lastClickFrame = -1;

    public void Init(int gridX, int gridY, TileType type, MapManager manager)
    {
        grid_x = gridX;
        grid_y = gridY;
        tileType = type;
        is_buildable = type == TileType.Buildable;
        is_occupied = false;
        mapManager = manager;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        var collider2d = GetComponent<BoxCollider2D>();
        if (collider2d == null)
        {
            collider2d = gameObject.AddComponent<BoxCollider2D>();
        }

        collider2d.offset = Vector2.zero;
        collider2d.size = Vector2.one;
        collider2d.isTrigger = false;
    }

    public void SetVisual(Sprite sprite, Color color)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }

        spriteRenderer.color = color;
    }

    public void SetOccupied(bool occupied)
    {
        is_occupied = occupied;
    }

    public void HandleClick()
    {
        if (lastClickFrame == Time.frameCount)
        {
            return;
        }

        lastClickFrame = Time.frameCount;

        if (is_occupied)
        {
            NotifyInvalidClick("该地块已有塔");
            return;
        }

        if (tileType == TileType.Path)
        {
            NotifyInvalidClick("路径不可建造");
            return;
        }

        if (tileType == TileType.Obstacle)
        {
            NotifyInvalidClick("障碍物不可建造");
            return;
        }

        if (tileType == TileType.Castle)
        {
            NotifyInvalidClick("堡垒位置不可建造");
            return;
        }

        if (tileType != TileType.Buildable || !is_buildable)
        {
            NotifyInvalidClick("不可建造");
            return;
        }

        if (mapManager != null)
        {
            mapManager.OnTileClicked(grid_x, grid_y);
        }
        else
        {
            Debug.LogWarning("[TileButton] Missing MapManager for tile " + grid_x + "," + grid_y);
        }
    }

    private void OnMouseDown()
    {
        HandleClick();
    }

    private void NotifyInvalidClick(string message)
    {
        Debug.Log("[TileButton] " + message + ": " + grid_x + "," + grid_y);

        if (mapManager != null)
        {
            mapManager.ShowTileMessage(message + ": " + grid_x + "," + grid_y);
        }
    }
}
