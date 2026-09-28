using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathManager : MonoBehaviour
{
    public Tilemap pathTilemap;

    public List<Vector3> GetPath()
    {
        List<Vector3> path = new List<Vector3>();

        Vector3Int startCell = FindStartCell();

        if (!pathTilemap.HasTile(startCell))
        {
            Debug.LogError("Could not find a starting path tile.");
            return path;
        }

        Vector3Int current = startCell;
        Vector3Int previous = new Vector3Int(
            int.MinValue,
            int.MinValue,
            0
        );

        Vector3Int[] directions =
        {
            Vector3Int.up,
            Vector3Int.right,
            Vector3Int.down,
            Vector3Int.left
        };

        while (true)
        {
            path.Add(pathTilemap.GetCellCenterWorld(current));

            bool foundNext = false;

            foreach (Vector3Int direction in directions)
            {
                Vector3Int next = current + direction;

                // Don't go back to the previous tile
                if (next == previous)
                    continue;

                if (pathTilemap.HasTile(next))
                {
                    previous = current;
                    current = next;
                    foundNext = true;
                    break;
                }
            }

            // No connected path tile means we reached the end
            if (!foundNext)
                break;
        }

        return path;
    }

    private Vector3Int FindStartCell()
    {
        BoundsInt bounds = pathTilemap.cellBounds;

        Vector3Int leftmostEndpoint = new Vector3Int(
            int.MaxValue,
            0,
            0
        );

        bool foundEndpoint = false;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!pathTilemap.HasTile(cell))
                continue;

            // An endpoint only has one neighboring path tile
            if (CountPathNeighbors(cell) == 1)
            {
                // Keep the endpoint that is furthest left
                if (!foundEndpoint || cell.x < leftmostEndpoint.x)
                {
                    leftmostEndpoint = cell;
                    foundEndpoint = true;
                }
            }
        }

        if (!foundEndpoint)
        {
            Debug.LogError("No path endpoint found!");
        }

        return leftmostEndpoint;
    }

    private int CountPathNeighbors(Vector3Int cell)
    {
        int count = 0;

        Vector3Int[] directions =
        {
            Vector3Int.up,
            Vector3Int.right,
            Vector3Int.down,
            Vector3Int.left
        };

        foreach (Vector3Int direction in directions)
        {
            if (pathTilemap.HasTile(cell + direction))
            {
                count++;
            }
        }

        return count;
    }
}