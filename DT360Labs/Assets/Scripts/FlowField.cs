using System.Collections.Generic;
using UnityEngine;

public class FlowField : MonoBehaviour
{
    public static FlowField Instance; // Makes this easily accessible to characters

    public bool showGrid = false;
    private List<GameObject> visualObjects = new List<GameObject>();
    private GameObject visualsParent;

    [Header("Grid Settings")]
    public Vector2Int gridSize = new Vector2Int(50, 50); // Make this bigger to cover your map
    public float cellSize = 2f;
    public Vector3 gridOrigin = Vector3.zero; // Bottom-left corner of your map

    [Header("Physics Layers")]
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    private int[,] distanceField;
    public Vector3[,] flowDirection; // Public so agents can read it
    private bool[,] isObstacle;

    private Vector3 currentTargetPos;
    private bool hasTarget = false;

    [Header("Build Visual Materials")]
    public Material greenArrowMaterial;
    public Material redObstacleMaterial;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // Right Click to set target and calculate Flow Field
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
            {
                currentTargetPos = hit.point;
                hasTarget = true;

                // 1. Calculate the new math
                GenerateFlowField(currentTargetPos);

                // 2. NEW: Instantly redraw the visuals if the grid is currently turned ON
                if (showGrid)
                {
                    DrawRealVisualsForBuild();
                }
            }
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            showGrid = !showGrid;

            if (showGrid && hasTarget)
            {
                DrawRealVisualsForBuild();
            }
            else
            {
                ClearRealVisuals();
            }
        }

        if (showGrid && flowDirection != null)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    if (flowDirection[x, y] != Vector3.zero)
                    {
                        // Get the cell's position, and raise it 2 meters into the air so it doesn't clip into the ground!
                        Vector3 startPos = GetWorldPosFromCell(x, y) + (Vector3.up * 2f);

                        // Draw a thick green line pointing in the flow direction
                        Debug.DrawRay(startPos, flowDirection[x, y] * 1.5f, Color.green);
                    }
                }
            }
        }
    }

    private void GenerateFlowField(Vector3 targetWorldPos)
    {
        distanceField = new int[gridSize.x, gridSize.y];
        flowDirection = new Vector3[gridSize.x, gridSize.y];
        isObstacle = new bool[gridSize.x, gridSize.y];

        int obstacleCount = 0; // NEW: Let's count the walls!

        // 1. Map Obstacles using Physics!
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                distanceField[x, y] = 9999;
                flowDirection[x, y] = Vector3.zero;

                Vector3 cellCenter = GetWorldPosFromCell(x, y);

                if (Physics.CheckSphere(cellCenter, (cellSize / 2f) + 1.0f, obstacleLayer))
                {
                    isObstacle[x, y] = true;
                    obstacleCount++; // Count every wall we find
                }
            }
        }

        Debug.Log($"Grid Scanned! Found {obstacleCount} obstacle cells out of {gridSize.x * gridSize.y} total cells.");

        Vector2Int targetCell = GetCellFromWorldPos(targetWorldPos);

        if (!IsValidCell(targetCell))
        {
            Debug.LogError("ABORTED: You clicked OUTSIDE the grid bounds!");
            return;
        }

        if (isObstacle[targetCell.x, targetCell.y])
        {
            Debug.LogError("ABORTED: You clicked directly on an Obstacle!");
            return;
        }

        // 2. Integration Field (BFS)
        Queue<Vector2Int> cellsToCheck = new Queue<Vector2Int>();
        distanceField[targetCell.x, targetCell.y] = 0;
        cellsToCheck.Enqueue(targetCell);

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        int cellsExplored = 0;

        while (cellsToCheck.Count > 0)
        {
            Vector2Int current = cellsToCheck.Dequeue();
            cellsExplored++; // THIS is the counter we missed!

            foreach (Vector2Int dir in dirs)
            {
                Vector2Int neighbor = current + dir;

                if (IsValidCell(neighbor) && !isObstacle[neighbor.x, neighbor.y])
                {
                    if (distanceField[neighbor.x, neighbor.y] == 9999)
                    {
                        distanceField[neighbor.x, neighbor.y] = distanceField[current.x, current.y] + 1;
                        cellsToCheck.Enqueue(neighbor);
                    }
                }
            }
        }

        Debug.Log($"BFS Complete! The flood reached {cellsExplored} out of {gridSize.x * gridSize.y} cells.");

        // 3. Vector Field (Arrows)
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                if (isObstacle[x, y]) continue;

                int bestDistance = distanceField[x, y];
                Vector2Int bestDirection = Vector2Int.zero;

                foreach (Vector2Int dir in dirs)
                {
                    Vector2Int neighbor = new Vector2Int(x, y) + dir;

                    if (IsValidCell(neighbor) && !isObstacle[neighbor.x, neighbor.y])
                    {
                        if (distanceField[neighbor.x, neighbor.y] < bestDistance)
                        {
                            bestDistance = distanceField[neighbor.x, neighbor.y];
                            bestDirection = dir;
                        }
                    }
                }
                flowDirection[x, y] = new Vector3(bestDirection.x, 0, bestDirection.y).normalized;
            }
        }
    }
    // Agent will call this to know which way to go
    public Vector3 GetFlowDirection(Vector3 worldPos)
    {
        if (!hasTarget) return Vector3.zero;
        Vector2Int cell = GetCellFromWorldPos(worldPos);
        if (IsValidCell(cell)) return flowDirection[cell.x, cell.y];
        return Vector3.zero;
    }

    private bool IsValidCell(Vector2Int cell) => cell.x >= 0 && cell.x < gridSize.x && cell.y >= 0 && cell.y < gridSize.y;

    private Vector2Int GetCellFromWorldPos(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPos.z - gridOrigin.z) / cellSize);
        return new Vector2Int(x, y);
    }

    private Vector3 GetWorldPosFromCell(int x, int y)
    {
        // Notice we raycast down to find the terrain height!
        Vector3 pos = new Vector3(x * cellSize + (cellSize / 2f) + gridOrigin.x, 100f, y * cellSize + (cellSize / 2f) + gridOrigin.z);
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 200f, groundLayer))
        {
            pos.y = hit.point.y; // Snap to terrain height
        }
        else pos.y = 0;

        return pos;
    }
    void OnDrawGizmos()
    {
        if (!showGrid || distanceField == null) return;

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3 centerPos = GetWorldPosFromCell(x, y);

                // Draw a red box for obstacles, white for walkable
                Gizmos.color = isObstacle[x, y] ? new Color(1, 0, 0, 0.4f) : new Color(1, 1, 1, 0.1f);
                Gizmos.DrawWireCube(centerPos, new Vector3(cellSize, 0.1f, cellSize));

                // Draw the Flow Arrow
                if (flowDirection[x, y] != Vector3.zero)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawRay(centerPos, flowDirection[x, y] * (cellSize * 0.4f));
                    Gizmos.DrawSphere(centerPos + flowDirection[x, y] * (cellSize * 0.4f), cellSize * 0.1f);
                }

                // Draw the Weight Number (Only works in the Unity Editor!)
#if UNITY_EDITOR
                if (!isObstacle[x, y] && distanceField[x, y] != 9999)
                {
                    // Raise the text slightly so it doesn't clip into the ground
                    Vector3 textPos = centerPos + Vector3.up * 0.5f;
                    UnityEditor.Handles.Label(textPos, distanceField[x, y].ToString());
                }
#endif
            }
        }
    }
    // This will tell us EXACTLY what the grid thinks about the Agent's feet
    public void PrintAgentStatus(Vector3 agentPos)
    {
        if (distanceField == null) return;

        Vector2Int cell = GetCellFromWorldPos(agentPos);

        if (!IsValidCell(cell))
        {
            Debug.LogWarning("AGENT STATUS: Out of Bounds!");
        }
        else if (isObstacle[cell.x, cell.y])
        {
            Debug.LogWarning("AGENT STATUS: Standing on an Obstacle! (Check physics layers)");
        }
        else
        {
            Debug.Log($"AGENT STATUS: Cell {cell} | Distance to Target: {distanceField[cell.x, cell.y]} | Arrow: {flowDirection[cell.x, cell.y]}");
        }
    }

    private void ClearRealVisuals()
    {
        if (visualsParent != null) Destroy(visualsParent);
        visualObjects.Clear();
    }

    private void DrawRealVisualsForBuild()
    {
        // 1. Clear the old drawings first
        ClearRealVisuals();

        // Create a folder in the hierarchy to hold all these objects cleanly
        visualsParent = new GameObject("FlowField_BuildVisuals");

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3 centerPos = GetWorldPosFromCell(x, y);

                // 2. Draw Obstacles as Red flat planes
                if (isObstacle[x, y])
                {
                    GameObject wallMarker = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    wallMarker.transform.SetParent(visualsParent.transform);

                    // Lay it flat on the ground
                    wallMarker.transform.position = centerPos + (Vector3.up * 0.2f);
                    wallMarker.transform.rotation = Quaternion.Euler(90, 0, 0);
                    wallMarker.transform.localScale = new Vector3(cellSize, cellSize, 1);

                    Destroy(wallMarker.GetComponent<Collider>()); // Remove collider
                    wallMarker.GetComponent<MeshRenderer>().material = redObstacleMaterial;
                    visualObjects.Add(wallMarker);
                    continue;
                }

                // 3. Draw Arrows as stretched green 3D Cubes
                if (flowDirection[x, y] != Vector3.zero)
                {
                    GameObject arrowLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    arrowLine.transform.SetParent(visualsParent.transform);

                    // Hover it slightly above the grass
                    arrowLine.transform.position = centerPos + (Vector3.up * 0.3f);

                    // Point the cube in the flow direction
                    arrowLine.transform.rotation = Quaternion.LookRotation(flowDirection[x, y]);

                    // Stretch it into a long, thin line
                    arrowLine.transform.localScale = new Vector3(0.2f, 0.2f, cellSize * 0.8f);

                    Destroy(arrowLine.GetComponent<Collider>()); // Remove physics
                    arrowLine.GetComponent<MeshRenderer>().material = greenArrowMaterial;
                    visualObjects.Add(arrowLine);
                }

                // 4. NEW: Draw the Weight Numbers as 3D Text!
                if (!isObstacle[x, y] && distanceField[x, y] != 9999)
                {
                    GameObject textObj = new GameObject("WeightText");
                    textObj.transform.SetParent(visualsParent.transform);

                    // Hover it slightly higher than the arrow so they don't overlap
                    textObj.transform.position = centerPos + (Vector3.up * 0.6f);

                    // Lay the text flat facing the sky so your top-down camera can read it easily
                    textObj.transform.rotation = Quaternion.Euler(90, 0, 0);

                    // Add the 3D Text component
                    TextMesh tm = textObj.AddComponent<TextMesh>();
                    tm.text = distanceField[x, y].ToString();

                    // Format the text so it fits nicely inside the grid cell
                    tm.characterSize = 0.15f;
                    tm.fontSize = 25;
                    tm.anchor = TextAnchor.MiddleCenter;
                    tm.alignment = TextAlignment.Center;
                    tm.color = Color.white;

                    visualObjects.Add(textObj);
                }
            }
        }
    }
}

