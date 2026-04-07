using UnityEngine;
using System.Collections; // Required for Coroutines

public class CellularAutomata : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 50;
    public int height = 50;

    [Header("Cellular Automata Rules")]
    [Range(0, 100)]
    public int randomFillPercent = 45;
    public int smoothingIterations = 5;

    [Header("Animation Settings")]
    [Tooltip("How many seconds to wait between each step of the animation.")]
    public float delayBetweenSteps = 1.0f;

    [Header("Visuals")]
    public GameObject wallPrefab;

    private int[,] map;
    private GameObject[,] cellObjects; // Stores all our physical wall blocks
    private Transform wallParent;
    private bool isGenerating = false; // Prevents running multiple animations at once

    void Start()
    {
        if (wallPrefab == null)
        {
            Debug.LogError("Please assign a Wall Prefab in the inspector!");
            return;
        }

        StartGeneration();
    }

    void Update()
    {
        // Check if the 'R' key was pressed this frame
        if (Input.GetKeyDown(KeyCode.R))
        {
            // If an animation is currently running, stop it
            if (isGenerating)
            {
                StopAllCoroutines();
                isGenerating = false;
            }

            // Start a fresh cave generation
            StartGeneration();
        }
    }

    // Call this to begin the animation
    public void StartGeneration()
    {
        if (!isGenerating)
        {
            StartCoroutine(AnimateMapGeneration());
        }
    }

    IEnumerator AnimateMapGeneration()
    {
        isGenerating = true;

        // 1. Setup the physical grid of hidden game objects
        InitializeVisualGrid();

        // 2. Generate the initial random noise
        map = new int[width, height];
        RandomFillMap();

        // Update visuals and pause so we can see the starting noise
        UpdateVisuals();
        yield return new WaitForSeconds(delayBetweenSteps);

        // 3. Animate each smoothing step
        for (int i = 0; i < smoothingIterations; i++)
        {
            SmoothMap();
            UpdateVisuals();

            // Pause execution so the player can watch the cave form
            yield return new WaitForSeconds(delayBetweenSteps);
        }

        isGenerating = false;
    }

    void InitializeVisualGrid()
    {
        // Cleanup old grid if we are running this a second time
        if (wallParent != null) Destroy(wallParent.gameObject);

        wallParent = new GameObject("Generated Cave View").transform;
        cellObjects = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(-width / 2f + x + 0.5f, -height / 2f + y + 0.5f, 0);
                GameObject newWall = Instantiate(wallPrefab, pos, Quaternion.identity, wallParent);

                // Hide it initially
                newWall.SetActive(false);

                // Save a reference to it so we can easily turn it on/off later
                cellObjects[x, y] = newWall;
            }
        }
    }

    void UpdateVisuals()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // If map is 1 (wall), turn the object ON. If 0 (empty), turn it OFF.
                bool isWall = (map[x, y] == 1);
                cellObjects[x, y].SetActive(isWall);
            }
        }
    }

    void RandomFillMap()
    {
        System.Random pseudoRandom = new System.Random(Time.time.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    map[x, y] = 1;
                else
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
            }
        }
    }

    void SmoothMap()
    {
        int[,] newMap = (int[,])map.Clone();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallTiles = GetSurroundingWallCount(x, y);

                if (neighborWallTiles > 4)
                    newMap[x, y] = 1;
                else if (neighborWallTiles < 4)
                    newMap[x, y] = 0;
            }
        }

        map = newMap;
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (neighbourX >= 0 && neighbourX < width && neighbourY >= 0 && neighbourY < height)
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }
}
