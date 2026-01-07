using UnityEngine;

/// <summary>
/// Allows drawing and erasing sand particles in the simulation using mouse input
/// </summary>
public class SandDrawer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SandSimulation sandSimulation;
    [SerializeField] private Camera mainCamera;

    [Header("Drawing Settings")]
    [SerializeField] private int brushSize = 3;
    [SerializeField] private Color32 sandColor = new Color32(255, 220, 150, 255);
    [SerializeField] private bool randomizeColor = true;
    [SerializeField] private float colorVariation = 0.1f;

    [Header("Input Settings")]
    [SerializeField] private KeyCode drawKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode eraseKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode eraseSinglePixelKey = KeyCode.Mouse2; // Middle mouse button
    [SerializeField] private KeyCode clearAllKey = KeyCode.C;

    [Header("Advanced")]
    [SerializeField] private bool drawCircle = true;
    [SerializeField] private float drawRate = 0.01f; // Seconds between draws (0 = every frame)
    private float lastDrawTime;

    private void Start()
    {
        // Auto-find references if not assigned
        if (sandSimulation == null)
            sandSimulation = FindObjectOfType<SandSimulation>();
        
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (sandSimulation == null)
        {
            Debug.LogError("SandDrawer: SandSimulation not found!");
            enabled = false;
        }

        if (mainCamera == null)
        {
            Debug.LogError("SandDrawer: Camera not found!");
            enabled = false;
        }
    }

    private void Update()
    {
        // Clear all sand
        if (Input.GetKeyDown(clearAllKey))
        {
            sandSimulation.ClearAllSand();
            Debug.Log("All sand cleared!");
        }

        // Check draw rate throttling
        if (Time.time - lastDrawTime < drawRate)
            return;

        // Draw sand
        if (Input.GetKey(drawKey))
        {
            DrawAtMousePosition();
            lastDrawTime = Time.time;
        }

        // Erase sand
        if (Input.GetKey(eraseKey))
        {
            EraseAtMousePosition();
            lastDrawTime = Time.time;
        }

        // Erase single pixel
        if (Input.GetKey(eraseSinglePixelKey))
        {
            EraseSinglePixelAtMousePosition();
            lastDrawTime = Time.time;
        }
    }

    /// <summary>
    /// Draw sand at the current mouse position
    /// </summary>
    public void DrawAtMousePosition()
    {
        Vector3 worldPos = GetMouseWorldPosition();
        if (worldPos == Vector3.zero) return;

        Vector2Int simPos = sandSimulation.WorldToSimulation(worldPos);
        DrawSand(simPos.x, simPos.y);
    }

    /// <summary>
    /// Erase sand at the current mouse position
    /// </summary>
    public void EraseAtMousePosition()
    {
        Vector3 worldPos = GetMouseWorldPosition();
        if (worldPos == Vector3.zero) return;

        Vector2Int simPos = sandSimulation.WorldToSimulation(worldPos);
        EraseSand(simPos.x, simPos.y);
    }

    /// <summary>
    /// Erase a single pixel at the current mouse position
    /// </summary>
    public void EraseSinglePixelAtMousePosition()
    {
        Vector3 worldPos = GetMouseWorldPosition();
        if (worldPos == Vector3.zero) return;

        Vector2Int simPos = sandSimulation.WorldToSimulation(worldPos);
        sandSimulation.ClearCell(simPos.x, simPos.y);
    }

    /// <summary>
    /// Draw sand at specific simulation coordinates
    /// </summary>
    public void DrawSand(int centerX, int centerY)
    {
        if (drawCircle)
        {
            DrawCircleBrush(centerX, centerY, brushSize, true);
        }
        else
        {
            DrawSquareBrush(centerX, centerY, brushSize, true);
        }
    }

    /// <summary>
    /// Erase sand at specific simulation coordinates
    /// </summary>
    public void EraseSand(int centerX, int centerY)
    {
        if (drawCircle)
        {
            DrawCircleBrush(centerX, centerY, brushSize, false);
        }
        else
        {
            DrawSquareBrush(centerX, centerY, brushSize, false);
        }
    }

    /// <summary>
    /// Draw or erase using a square brush
    /// </summary>
    private void DrawSquareBrush(int centerX, int centerY, int size, bool draw)
    {
        for (int dy = -size; dy <= size; dy++)
        {
            for (int dx = -size; dx <= size; dx++)
            {
                int x = centerX + dx;
                int y = centerY + dy;

                if (draw)
                {
                    Cell sandCell = CreateSandCell();
                    sandSimulation.SetCell(x, y, sandCell);
                }
                else
                {
                    sandSimulation.ClearCell(x, y);
                }
            }
        }
    }

    /// <summary>
    /// Draw or erase using a circular brush
    /// </summary>
    private void DrawCircleBrush(int centerX, int centerY, int radius, bool draw)
    {
        int radiusSquared = radius * radius;

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                // Check if point is within circle
                if (dx * dx + dy * dy <= radiusSquared)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (draw)
                    {
                        Cell sandCell = CreateSandCell();
                        sandSimulation.SetCell(x, y, sandCell);
                    }
                    else
                    {
                        sandSimulation.ClearCell(x, y);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Create a sand cell with optional color randomization
    /// </summary>
    private Cell CreateSandCell()
    {
        Color32 color = sandColor;

        if (randomizeColor)
        {
            float variation = colorVariation;
            color.r = (byte)Mathf.Clamp(sandColor.r + Random.Range(-variation * 255, variation * 255), 0, 255);
            color.g = (byte)Mathf.Clamp(sandColor.g + Random.Range(-variation * 255, variation * 255), 0, 255);
            color.b = (byte)Mathf.Clamp(sandColor.b + Random.Range(-variation * 255, variation * 255), 0, 255);
        }

        return new Cell
        {
            type = 1, // Sand
            color = color,
            hasMoved = false
        };
    }

    /// <summary>
    /// Get mouse position in world coordinates
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector3.zero;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Bounds bounds = sandSimulation.SimulationBounds;

        // Create a plane at z=0 (assuming 2D simulation)
        Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, bounds.center.z));

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            
            // Check if within simulation bounds
            if (bounds.Contains(hitPoint))
            {
                return hitPoint;
            }
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Set the brush size dynamically
    /// </summary>
    public void SetBrushSize(int size)
    {
        brushSize = Mathf.Max(1, size);
    }

    /// <summary>
    /// Set the sand color dynamically
    /// </summary>
    public void SetSandColor(Color32 color)
    {
        sandColor = color;
    }

    /// <summary>
    /// Toggle between circle and square brush
    /// </summary>
    public void SetCircleBrush(bool circle)
    {
        drawCircle = circle;
    }

    // Optional: Display brush size info
    private void OnGUI()
    {
        if (sandSimulation == null) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(10, 10, 300, 25), $"Brush Size: {brushSize} (Scroll to change)", style);
        GUI.Label(new Rect(10, 35, 400, 25), $"LMB: Draw | RMB: Erase | MMB: Erase 1px | {clearAllKey}: Clear All", style);
        // GUI.Label(new Rect(10, 60, 300, 25), $"Sand Count: {sandSimulation.CountSandPixels()}", style);
        GUI.Label(new Rect(10, 85, 400, 25), $"[-/+] Speed | [L] Toggle Limit | [P] Slow Mode", style);

        // Allow brush size adjustment with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            brushSize = Mathf.Clamp(brushSize + (int)Mathf.Sign(scroll), 1, 20);
        }
    }
}

