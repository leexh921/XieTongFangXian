using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简单网格地图生成器。
/// 如果已经手工摆好了地块，可以关闭 createOnStart，只用 Inspector 手动设置 TileButton.tileId。
/// </summary>
public class MapManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public Transform tileParent;
    public int width = 8;
    public int height = 5;
    public float tileSize = 1.5f;
    public bool createOnStart = true;

    private readonly List<GameObject> generatedTiles = new List<GameObject>();

    private void Start()
    {
        if (createOnStart)
        {
            GenerateMap();
        }
    }

    /// <summary>
    /// 生成 width * height 个地块，并从 0 开始分配 tileId。
    /// 后端如果用不同编号规则，需要和这里保持一致。
    /// </summary>
    public void GenerateMap()
    {
        ClearGeneratedTiles();

        if (tileParent == null)
        {
            tileParent = transform;
        }

        int tileId = 0;
        Vector3 origin = new Vector3(
            -(width - 1) * tileSize * 0.5f,
            0f,
            -(height - 1) * tileSize * 0.5f);

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 position = origin + new Vector3(x * tileSize, 0f, z * tileSize);
                GameObject tileObject = CreateTileObject(position, tileId);
                generatedTiles.Add(tileObject);
                tileId++;
            }
        }
    }

    /// <summary>
    /// 清空本脚本生成的地块。
    /// 只清理 generatedTiles 列表中的对象，不会误删美术手工摆放的地图对象。
    /// </summary>
    public void ClearGeneratedTiles()
    {
        for (int i = generatedTiles.Count - 1; i >= 0; i--)
        {
            if (generatedTiles[i] != null)
            {
                Destroy(generatedTiles[i]);
            }
        }

        generatedTiles.Clear();
    }

    private GameObject CreateTileObject(Vector3 position, int tileId)
    {
        GameObject tileObject;

        if (tilePrefab != null)
        {
            tileObject = Instantiate(tilePrefab, position, Quaternion.identity, tileParent);
        }
        else
        {
            tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tileObject.transform.SetParent(tileParent);
            tileObject.transform.position = position;
            tileObject.transform.localScale = new Vector3(tileSize * 0.9f, 0.15f, tileSize * 0.9f);
        }

        tileObject.name = "Tile_" + tileId;

        TileButton tileButton = tileObject.GetComponent<TileButton>();
        if (tileButton == null)
        {
            tileButton = tileObject.AddComponent<TileButton>();
        }

        tileButton.tileId = tileId;

        return tileObject;
    }
}
