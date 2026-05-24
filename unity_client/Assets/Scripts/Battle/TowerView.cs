using UnityEngine;

public class TowerView : MonoBehaviour
{
    public string instance_id;
    public int tower_id;
    public int owner_player_id;
    public int grid_x;
    public int grid_y;
    public SpriteRenderer spriteRenderer;
    public LineRenderer rangeLineRenderer;

    private static Sprite fallbackSprite;
    private static Material rangeMaterial;
    private Color defaultColor = new Color(0.2f, 0.45f, 0.95f, 1f);

    public void Init(string instanceId, int towerId, int ownerPlayerId, int gridX, int gridY)
    {
        instance_id = instanceId;
        tower_id = towerId;
        owner_player_id = ownerPlayerId;
        grid_x = gridX;
        grid_y = gridY;

        EnsureRenderer();
        SetDefaultVisual();
        UpdateAttackRangeVisual();
    }

    public void SetSelectedVisual(bool selected)
    {
        EnsureRenderer();
        spriteRenderer.color = selected ? new Color(1f, 0.86f, 0.2f, 1f) : defaultColor;
    }

    public void SetDefaultVisual()
    {
        EnsureRenderer();
        spriteRenderer.color = defaultColor;
        spriteRenderer.sortingOrder = 10;
    }

    private void UpdateAttackRangeVisual()
    {
        float range = GetTowerRange();
        if (range <= 0f)
        {
            return;
        }

        EnsureRangeLineRenderer();
        float radius = range * BattleMapConfig.CellSize;
        const int segmentCount = 64;
        rangeLineRenderer.positionCount = segmentCount;
        rangeLineRenderer.loop = true;
        rangeLineRenderer.useWorldSpace = true;
        rangeLineRenderer.widthMultiplier = 0.03f;
        rangeLineRenderer.sortingOrder = 8;

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = Mathf.PI * 2f * i / segmentCount;
            Vector3 point = transform.position + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0.03f);
            rangeLineRenderer.SetPosition(i, point);
        }
    }

    private float GetTowerRange()
    {
        if (GameManager.Instance == null || GameManager.Instance.tower_config == null)
        {
            return 0f;
        }

        for (int i = 0; i < GameManager.Instance.tower_config.Count; i++)
        {
            TowerConfigData config = GameManager.Instance.tower_config[i];
            if (config != null && config.tower_id == tower_id)
            {
                return config.range;
            }
        }

        return 0f;
    }

    private void EnsureRangeLineRenderer()
    {
        if (rangeLineRenderer == null)
        {
            rangeLineRenderer = GetComponent<LineRenderer>();
        }

        if (rangeLineRenderer == null)
        {
            rangeLineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        Material material = GetRangeMaterial();
        if (material != null)
        {
            rangeLineRenderer.material = material;
        }

        rangeLineRenderer.startColor = new Color(0.25f, 0.7f, 1f, 0.55f);
        rangeLineRenderer.endColor = new Color(0.25f, 0.7f, 1f, 0.55f);
    }

    private void EnsureRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = GetFallbackSprite();
        }

        spriteRenderer.sortingOrder = 10;
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "RuntimeTowerFallbackTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        fallbackSprite.name = "RuntimeTowerFallbackSprite";
        return fallbackSprite;
    }

    private static Material GetRangeMaterial()
    {
        if (rangeMaterial != null)
        {
            return rangeMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            Debug.LogWarning("[TowerView] Missing shader for tower range line.");
            return null;
        }

        rangeMaterial = new Material(shader);
        rangeMaterial.name = "RuntimeTowerRangeMaterial";
        rangeMaterial.color = new Color(0.25f, 0.7f, 1f, 0.55f);
        return rangeMaterial;
    }
}
