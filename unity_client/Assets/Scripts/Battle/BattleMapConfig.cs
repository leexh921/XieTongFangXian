using System.Collections.Generic;
using UnityEngine;

public static class BattleMapConfig
{
    public const int Width = 14;
    public const int Height = 8;
    public const float CellSize = 0.8f;

    public static readonly Vector2Int[] PathGridPoints =
    {
        new Vector2Int(0, 4),
        new Vector2Int(1, 4),
        new Vector2Int(2, 4),
        new Vector2Int(3, 4),
        new Vector2Int(4, 4),
        new Vector2Int(5, 4),
        new Vector2Int(5, 3),
        new Vector2Int(5, 2),
        new Vector2Int(6, 2),
        new Vector2Int(7, 2),
        new Vector2Int(8, 2),
        new Vector2Int(9, 2),
        new Vector2Int(10, 2),
        new Vector2Int(11, 2),
        new Vector2Int(12, 2),
        new Vector2Int(12, 1),
        new Vector2Int(12, 0),
        new Vector2Int(13, 0)
    };

    public static readonly Vector2Int[] ObstacleGridPoints =
    {
        new Vector2Int(2, 6),
        new Vector2Int(3, 6),
        new Vector2Int(9, 5),
        new Vector2Int(10, 5),
        new Vector2Int(1, 1),
        new Vector2Int(8, 0)
    };

    public static Vector2Int CastleGridPoint
    {
        get { return PathGridPoints[PathGridPoints.Length - 1]; }
    }

    public static MapConfigData CreateDefaultMapConfig()
    {
        var pathPoints = new List<GridPointData>();
        for (int i = 0; i < PathGridPoints.Length; i++)
        {
            pathPoints.Add(ToGridPointData(PathGridPoints[i]));
        }

        var obstacles = new List<GridPointData>();
        for (int i = 0; i < ObstacleGridPoints.Length; i++)
        {
            obstacles.Add(ToGridPointData(ObstacleGridPoints[i]));
        }

        return new MapConfigData
        {
            map_id = 1,
            name = "默认地图",
            width = Width,
            height = Height,
            path_points = pathPoints,
            obstacles = obstacles,
            castle = ToGridPointData(CastleGridPoint)
        };
    }

    public static MapConfigData GetActiveMapConfig()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasServerMapConfig())
        {
            return GameManager.Instance.GetCurrentMapConfig();
        }

        return CreateDefaultMapConfig();
    }

    public static Vector3 GridToWorld(int gridX, int gridY)
    {
        return GridToWorld((float)gridX, gridY, GetActiveMapConfig());
    }

    public static Vector3 GridToWorld(float gridX, float gridY)
    {
        return GridToWorld(gridX, gridY, GetActiveMapConfig());
    }

    public static Vector3 GridToWorld(float gridX, float gridY, MapConfigData map)
    {
        MapConfigData resolvedMap = IsUsableMap(map) ? map : CreateDefaultMapConfig();
        float originX = -(resolvedMap.width - 1) * CellSize * 0.5f;
        float originY = -(resolvedMap.height - 1) * CellSize * 0.5f;
        return new Vector3(originX + gridX * CellSize, originY + gridY * CellSize, 0f);
    }

    public static bool IsPathGrid(int gridX, int gridY)
    {
        return IsPathGrid(gridX, gridY, GetActiveMapConfig());
    }

    public static bool IsPathGrid(int gridX, int gridY, MapConfigData map)
    {
        MapConfigData resolvedMap = IsUsableMap(map) ? map : CreateDefaultMapConfig();
        return Contains(resolvedMap.path_points, gridX, gridY);
    }

    public static bool IsObstacleGrid(int gridX, int gridY)
    {
        return IsObstacleGrid(gridX, gridY, GetActiveMapConfig());
    }

    public static bool IsObstacleGrid(int gridX, int gridY, MapConfigData map)
    {
        MapConfigData resolvedMap = IsUsableMap(map) ? map : CreateDefaultMapConfig();
        return Contains(resolvedMap.obstacles, gridX, gridY);
    }

    public static bool IsCastleGrid(int gridX, int gridY)
    {
        return IsCastleGrid(gridX, gridY, GetActiveMapConfig());
    }

    public static bool IsCastleGrid(int gridX, int gridY, MapConfigData map)
    {
        GridPointData castle = GetCastlePoint(map);
        return castle != null && castle.x == gridX && castle.y == gridY;
    }

    public static GridPointData GetCastlePoint(MapConfigData map)
    {
        MapConfigData resolvedMap = IsUsableMap(map) ? map : CreateDefaultMapConfig();
        if (resolvedMap.castle != null)
        {
            return resolvedMap.castle;
        }

        if (resolvedMap.path_points != null && resolvedMap.path_points.Count > 0)
        {
            return resolvedMap.path_points[resolvedMap.path_points.Count - 1];
        }

        return ToGridPointData(CastleGridPoint);
    }

    public static Vector2 GetPathPoint(MapConfigData map, int index)
    {
        MapConfigData resolvedMap = IsUsableMap(map) ? map : CreateDefaultMapConfig();
        int clampedIndex = Mathf.Clamp(index, 0, resolvedMap.path_points.Count - 1);
        GridPointData point = resolvedMap.path_points[clampedIndex];
        return new Vector2(point.x, point.y);
    }

    public static int GetPathPointCount(MapConfigData map)
    {
        MapConfigData resolvedMap = IsUsableMap(map) ? map : CreateDefaultMapConfig();
        return resolvedMap.path_points.Count;
    }

    public static bool IsUsableMap(MapConfigData map)
    {
        return map != null
            && map.width > 0
            && map.height > 0
            && map.path_points != null
            && map.path_points.Count > 0;
    }

    private static GridPointData ToGridPointData(Vector2Int point)
    {
        return new GridPointData
        {
            x = point.x,
            y = point.y
        };
    }

    private static bool Contains(List<GridPointData> points, int gridX, int gridY)
    {
        if (points == null)
        {
            return false;
        }

        for (int i = 0; i < points.Count; i++)
        {
            GridPointData point = points[i];
            if (point != null && point.x == gridX && point.y == gridY)
            {
                return true;
            }
        }

        return false;
    }
}
