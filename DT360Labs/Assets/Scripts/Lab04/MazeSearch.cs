using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MazeSearch : MonoBehaviour
{
    [Header("Path Visuals")]
    [Tooltip("Drag a colored sphere or cube prefab here to draw the path.")]
    public GameObject pathPrefab;

    private List<GameObject> pathObjects = new List<GameObject>();

    public void StartFindingPath(int[,] map, int width, int height, float delay)
    {
        ClearPath();
        StartCoroutine(FindAndDrawLongestPath(map, width, height, delay));
    }

    public void ClearPath()
    {
        foreach (GameObject p in pathObjects)
        {
            if (p != null) Destroy(p);
        }
        pathObjects.Clear();
    }

    IEnumerator FindAndDrawLongestPath(int[,] map, int width, int height, float delay)
    {
        Vector2Int arbitraryStart = new Vector2Int(1, 1);
        Vector2Int startOfLongestPath = BFS_FurthestPoint(map, width, height, arbitraryStart, out _);
        Vector2Int endOfLongestPath = BFS_FurthestPoint(map, width, height, startOfLongestPath, out Dictionary<Vector2Int, Vector2Int> cameFrom);

        List<Vector2Int> longestPath = new List<Vector2Int>();
        Vector2Int current = endOfLongestPath;

        while (current != startOfLongestPath)
        {
            longestPath.Add(current);
            current = cameFrom[current];
        }
        longestPath.Add(startOfLongestPath);

        longestPath.Reverse();

        foreach (Vector2Int pathNode in longestPath)
        {
            Vector3 pos = new Vector3(-width / 2f + pathNode.x + 0.5f, -height / 2f + pathNode.y + 0.5f, 0);
            GameObject newPathDot = Instantiate(pathPrefab, pos, Quaternion.identity);
            pathObjects.Add(newPathDot);

            if (delay > 0) yield return new WaitForSeconds(delay);
        }
    }

    Vector2Int BFS_FurthestPoint(int[,] map, int width, int height, Vector2Int start, out Dictionary<Vector2Int, Vector2Int> cameFrom)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = start;

        Vector2Int furthestPoint = start;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            furthestPoint = current;

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;

                if (neighbor.x >= 0 && neighbor.x < width && neighbor.y >= 0 && neighbor.y < height)
                {
                    if (map[neighbor.x, neighbor.y] == 0 && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return furthestPoint;
    }
}