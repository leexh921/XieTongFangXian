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

    public static Vector3 GridToWorld(int gridX, int gridY)
    {
        float originX = -(Width - 1) * CellSize * 0.5f;
        float originY = -(Height - 1) * CellSize * 0.5f;
        return new Vector3(originX + gridX * CellSize, originY + gridY * CellSize, 0f);
    }

    public static bool IsPathGrid(int gridX, int gridY)
    {
        return Contains(PathGridPoints, gridX, gridY);
    }

    public static bool IsObstacleGrid(int gridX, int gridY)
    {
        return Contains(ObstacleGridPoints, gridX, gridY);
    }

    public static bool IsCastleGrid(int gridX, int gridY)
    {
        Vector2Int castle = CastleGridPoint;
        return castle.x == gridX && castle.y == gridY;
    }

    private static bool Contains(Vector2Int[] points, int gridX, int gridY)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].x == gridX && points[i].y == gridY)
            {
                return true;
            }
        }

        return false;
    }
}
