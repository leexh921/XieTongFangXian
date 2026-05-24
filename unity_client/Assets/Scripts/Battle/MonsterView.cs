using UnityEngine;

public class MonsterView : MonoBehaviour
{
    public string instance_id;
    public int monster_id;
    public int hp;
    public int max_hp;
    public float moveLerpSpeed = 10f;
    public SpriteRenderer spriteRenderer;
    public Transform hpBarRoot;
    public Transform hpFill;

    private static Sprite fallbackSprite;
    private static Sprite hpBarSprite;
    private const float HpBarWidth = 1.15f;
    private const float HpBarHeight = 0.12f;
    private Vector3 targetPosition;
    private bool updatedThisFrame;

    public void Init(string instanceId, int monsterId, int currentHp, int currentMaxHp, Vector2 position)
    {
        instance_id = instanceId;
        monster_id = monsterId;
        targetPosition = new Vector3(position.x, position.y, -0.2f);
        transform.position = targetPosition;
        EnsureRenderer();
        EnsureHpBar();
        SetHp(currentHp, currentMaxHp);
        MarkUpdatedThisFrame();
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveLerpSpeed);
    }

    public void ApplyState(MonsterStateData data)
    {
        if (data == null)
        {
            return;
        }

        instance_id = data.instance_id;
        monster_id = data.monster_id;
        targetPosition = new Vector3(data.x, data.y, -0.2f);
        SetHp(data.hp, data.max_hp);
        MarkUpdatedThisFrame();
    }

    public void SetHp(int currentHp, int currentMaxHp)
    {
        hp = currentHp;
        max_hp = Mathf.Max(1, currentMaxHp);
        UpdateHpBar();
    }

    public void UpdateHpBar()
    {
        EnsureHpBar();

        if (hpFill == null)
        {
            return;
        }

        float ratio = Mathf.Clamp01((float)hp / max_hp);
        hpFill.localScale = new Vector3(HpBarWidth * ratio, HpBarHeight, 1f);
        hpFill.localPosition = new Vector3(-HpBarWidth * (1f - ratio) * 0.5f, 0f, 0f);
    }

    public void MarkMissingThisFrame()
    {
        updatedThisFrame = false;
    }

    public void MarkUpdatedThisFrame()
    {
        updatedThisFrame = true;
    }

    public bool WasUpdatedThisFrame()
    {
        return updatedThisFrame;
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

        spriteRenderer.color = new Color(0.95f, 0.25f, 0.22f, 1f);
        spriteRenderer.sortingOrder = 20;
    }

    private void EnsureHpBar()
    {
        if (hpBarRoot == null)
        {
            Transform existingRoot = transform.Find("HpBar");
            if (existingRoot != null)
            {
                hpBarRoot = existingRoot;
            }
        }

        if (hpBarRoot == null)
        {
            var root = new GameObject("HpBar");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, 0.75f, -0.05f);
            hpBarRoot = root.transform;
        }

        if (hpFill == null)
        {
            Transform existingFill = hpBarRoot.Find("HpFill");
            if (existingFill != null)
            {
                hpFill = existingFill;
            }
        }

        if (hpBarRoot.Find("HpBg") == null)
        {
            var bg = new GameObject("HpBg");
            bg.transform.SetParent(hpBarRoot, false);
            bg.transform.localScale = new Vector3(HpBarWidth, HpBarHeight, 1f);

            var bgRenderer = bg.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = GetHpBarSprite();
            bgRenderer.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            bgRenderer.sortingOrder = 30;
        }

        if (hpFill == null)
        {
            var fill = new GameObject("HpFill");
            fill.transform.SetParent(hpBarRoot, false);
            hpFill = fill.transform;

            var fillRenderer = fill.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = GetHpBarSprite();
            fillRenderer.color = new Color(0.18f, 0.95f, 0.26f, 1f);
            fillRenderer.sortingOrder = 31;
        }
    }

    private static Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null)
        {
            return fallbackSprite;
        }

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "RuntimeMonsterFallbackTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        fallbackSprite.name = "RuntimeMonsterFallbackSprite";
        return fallbackSprite;
    }

    private static Sprite GetHpBarSprite()
    {
        if (hpBarSprite != null)
        {
            return hpBarSprite;
        }

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "RuntimeMonsterHpBarTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        hpBarSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        hpBarSprite.name = "RuntimeMonsterHpBarSprite";
        return hpBarSprite;
    }
}
