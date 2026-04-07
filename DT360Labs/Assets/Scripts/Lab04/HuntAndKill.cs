using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HuntAndKill : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 51;
    public int height = 51;

    [Header("Animation Settings")]
    public float delayBetweenSteps = 0.01f;

    [Header("Visuals & References")]
    public GameObject wallPrefab;

    [Tooltip("Drag the GameObject that has the MazePathfinder script attached here.")]
    public MazeSearch pathfinderScript; 

    private int[,] map;
    private GameObject[,] cellObjects;
    private Transform wallParent;
    private bool isGenerating = false;

    void Start()
    {
        StartGeneration();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (isGenerating) StopAllCoroutines();
            StartGeneration();
        }
    }

    public void StartGeneration()
    {
        if (!isGenerating) StartCoroutine(AnimateMapGeneration());
    }

    IEnumerator AnimateMapGeneration()
    {
        isGenerating = true;

        if (width % 2 == 0) width++;
        if (height % 2 == 0) height++;

        InitializeVisualGrid();

        map = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x, y] = 1;
            }
        }

        UpdateVisuals();
        yield return new WaitForSeconds(0.5f);

        bool mazeComplete = false;
        Vector2Int currentCell = new Vector2Int(1, 1);
        map[currentCell.x, currentCell.y] = 0;
        UpdateSingleVisual(currentCell);

        while (!mazeComplete)
        {
            bool walking = true;
            while (walking)
            {
                List<Vector2Int> unvisitedNeighbors = GetNeighbors(currentCell, 1);

                if (unvisitedNeighbors.Count > 0)
                {
                    Vector2Int nextCell = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                    Vector2Int wall = currentCell + (nextCell - currentCell) / 2;
                    map[wall.x, wall.y] = 0;
                    map[nextCell.x, nextCell.y] = 0;

                    UpdateSingleVisual(wall);
                    UpdateSingleVisual(nextCell);

                    currentCell = nextCell;

                    if (delayBetweenSteps > 0) yield return new WaitForSeconds(delayBetweenSteps);
                }
                else walking = false;
            }

            bool foundNewStart = false;
            for (int y = 1; y < height - 1; y += 2)
            {
                for (int x = 1; x < width - 1; x += 2)
                {
                    if (map[x, y] == 1)
                    {
                        List<Vector2Int> visitedNeighbors = GetNeighbors(new Vector2Int(x, y), 0);

                        if (visitedNeighbors.Count > 0)
                        {
                            Vector2Int vNeighbor = visitedNeighbors[Random.Range(0, visitedNeighbors.Count)];
                            Vector2Int wall = new Vector2Int(x, y) + (vNeighbor - new Vector2Int(x, y)) / 2;

                            map[wall.x, wall.y] = 0;
                            map[x, y] = 0;

                            UpdateSingleVisual(wall);
                            UpdateSingleVisual(new Vector2Int(x, y));

                            currentCell = new Vector2Int(x, y);
                            foundNewStart = true;

                            if (delayBetweenSteps > 0) yield return new WaitForSeconds(delayBetweenSteps);
                            break;
                        }
                    }
                }
                if (foundNewStart) break;
            }

            if (!foundNewStart) mazeComplete = true;
        }

        isGenerating = false;

        if (pathfinderScript != null)
        {
            pathfinderScript.StartFindingPath(map, width, height, delayBetweenSteps);
        }
    }

    List<Vector2Int> GetNeighbors(Vector2Int cell, int targetValue)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        if (cell.x > 1 && map[cell.x - 2, cell.y] == targetValue) neighbors.Add(new Vector2Int(cell.x - 2, cell.y));
        if (cell.x < width - 2 && map[cell.x + 2, cell.y] == targetValue) neighbors.Add(new Vector2Int(cell.x + 2, cell.y));
        if (cell.y > 1 && map[cell.x, cell.y - 2] == targetValue) neighbors.Add(new Vector2Int(cell.x, cell.y - 2));
        if (cell.y < height - 2 && map[cell.x, cell.y + 2] == targetValue) neighbors.Add(new Vector2Int(cell.x, cell.y + 2));
        return neighbors;
    }

    void InitializeVisualGrid()
    {
        if (wallParent != null) Destroy(wallParent.gameObject);

        if (pathfinderScript != null) pathfinderScript.ClearPath();

        wallParent = new GameObject("Generated Maze View").transform;
        cellObjects = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(-width / 2f + x + 0.5f, -height / 2f + y + 0.5f, 0);
                GameObject newWall = Instantiate(wallPrefab, pos, Quaternion.identity, wallParent);
                cellObjects[x, y] = newWall;
            }
        }
    }

    void UpdateVisuals()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cellObjects[x, y].SetActive(map[x, y] == 1);
    }

    void UpdateSingleVisual(Vector2Int pos)
    {
        cellObjects[pos.x, pos.y].SetActive(map[pos.x, pos.y] == 1);
    }
}