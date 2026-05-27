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
    public VisualConfigManager visualConfigManager;
    public Sprite[] animationFrames;
    public float animationFps = 12f;

    private static Sprite fallbackSprite;
    private static Sprite hpBarSprite;
    private const int MonsterSortingOrder = 20;
    private const string MonsterAFramesPath = "Assets/Art/Monsters/MonsterA/Skull/Skull_Run.png";
    private const string MonsterBFramesPath = "Assets/Art/Monsters/MonsterB/Troll/Troll_Walk.png";
    private const float HpBarWidth = 1.15f;
    private const float HpBarHeight = 0.12f;
    private Vector3 targetPosition;
    private bool updatedThisFrame;
    private bool useManualAnimation;
    private float animationTimer;
    private int animationFrameIndex;

    public void Init(string instanceId, int monsterId, int currentHp, int currentMaxHp, Vector3 worldPosition)
    {
        instance_id = instanceId;
        monster_id = monsterId;
        targetPosition = new Vector3(worldPosition.x, worldPosition.y, -0.2f);
        transform.position = targetPosition;
        EnsureRenderer();
        SetupAnimationVisual();
        if (!useManualAnimation && !HasAnimatorVisual())
        {
            ApplyMonsterSprite();
        }

        EnsureHpBar();
        SetHp(currentHp, currentMaxHp);
        MarkUpdatedThisFrame();
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveLerpSpeed);
        UpdateManualAnimation();
    }

    public void ApplyState(MonsterStateData data, Vector3 worldPosition)
    {
        if (data == null)
        {
            return;
        }

        instance_id = data.instance_id;
        if (monster_id != data.monster_id)
        {
            monster_id = data.monster_id;
            EnsureRenderer();
            SetupAnimationVisual();
            if (!useManualAnimation && !HasAnimatorVisual())
            {
                ApplyMonsterSprite();
            }
        }

        targetPosition = new Vector3(worldPosition.x, worldPosition.y, -0.2f);
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
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer.sprite == null && !TryStartAnimatorVisual())
        {
            spriteRenderer.sprite = GetFallbackSprite();
        }

        ApplySortingOrderToRenderers(MonsterSortingOrder);
    }

    private void ApplyMonsterSprite()
    {
        if (visualConfigManager == null)
        {
            visualConfigManager = VisualConfigManager.Instance;
        }

        Sprite sprite = visualConfigManager != null ? visualConfigManager.GetMonsterSprite(monster_id) : null;
        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
        }
        else
        {
            spriteRenderer.sprite = GetFallbackSprite();
            spriteRenderer.color = VisualConfigManager.GetMonsterFallbackColor(monster_id);
        }
    }

    private void SetupAnimationVisual()
    {
        EnsureAnimationFrames();

        if (animationFrames != null && animationFrames.Length > 0)
        {
            Animator animator = GetAnimator();
            if (animator != null)
            {
                animator.enabled = false;
            }

            useManualAnimation = true;
            animationFrameIndex = Mathf.Clamp(animationFrameIndex, 0, animationFrames.Length - 1);
            spriteRenderer.sprite = animationFrames[animationFrameIndex];
            spriteRenderer.color = Color.white;
            return;
        }

        useManualAnimation = false;
        TryStartAnimatorVisual();
    }

    private void UpdateManualAnimation()
    {
        if (!useManualAnimation || animationFrames == null || animationFrames.Length == 0 || spriteRenderer == null)
        {
            return;
        }

        animationTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, animationFps);
        while (animationTimer >= frameDuration)
        {
            animationTimer -= frameDuration;
            animationFrameIndex = (animationFrameIndex + 1) % animationFrames.Length;
            spriteRenderer.sprite = animationFrames[animationFrameIndex];
        }
    }

    private void EnsureAnimationFrames()
    {
        if (animationFrames != null && animationFrames.Length > 0)
        {
            return;
        }

#if UNITY_EDITOR
        string path = monster_id == 2 ? MonsterBFramesPath : MonsterAFramesPath;
        UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        var sprites = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite != null)
            {
                sprites.Add(sprite);
            }
        }

        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        animationFrames = sprites.ToArray();
#endif
    }

    private bool HasAnimatorVisual()
    {
        Animator animator = GetAnimator();
        return animator != null && animator.runtimeAnimatorController != null;
    }

    private bool TryStartAnimatorVisual()
    {
        Animator animator = GetAnimator();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        animator.enabled = true;
        animator.Rebind();
        animator.Update(0f);
        return true;
    }

    private Animator GetAnimator()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            return animator;
        }

        return GetComponentInChildren<Animator>();
    }

    private void ApplySortingOrderToRenderers(int sortingOrder)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0)
        {
            return;
        }

        int lowestOrder = renderers[0].sortingOrder;
        for (int i = 0; i < renderers.Length; i++)
        {
            lowestOrder = Mathf.Min(lowestOrder, renderers[i].sortingOrder);
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = sortingOrder + (renderers[i].sortingOrder - lowestOrder);
        }
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
