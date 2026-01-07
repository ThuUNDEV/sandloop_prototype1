using UnityEngine;
using Unity.Collections;
using UnityEngine.Events;

public class SandAbsorber : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float scanWidth = 1f;
    [SerializeField] private float scanHeight = 10f;
    [SerializeField] private int maxAbsorbPerFrame = 5;
    [SerializeField] private bool scanColumnAbove = true;
    [SerializeField] private float scanYOffset = -0.5f;

    [Header("References")]
    [SerializeField] private SandSimulation sandSimulation;
    [SerializeField] private Renderer simulationRenderer;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Events")]
    public UnityEvent<int> OnSandAbsorbed;

    private Bounds simulationBounds;
    private bool boundsInitialized = false;
    private bool referencesInitialized = false;

    void Start()
    {
        CacheReferences();
        InitializeBounds();
    }

    private void CacheReferences()
    {
        if (referencesInitialized) return;

        // Sử dụng ServiceLocator thay vì FindObjectOfType nhiều lần
        if (sandSimulation == null)
            sandSimulation = GameServiceLocator.Instance.SandSimulation;

        if (simulationRenderer == null && sandSimulation != null)
            simulationRenderer = sandSimulation.GetComponent<Renderer>();

        referencesInitialized = true;
    }

    public void SetReferences(SandSimulation simulation, Renderer renderer)
    {
        sandSimulation = simulation;
        simulationRenderer = renderer;
        referencesInitialized = true;
        InitializeBounds();
    }

    private void InitializeBounds()
    {
        if (simulationRenderer != null)
        {
            simulationBounds = simulationRenderer.bounds;
            boundsInitialized = true;
        }
        else
        {
            simulationBounds = new Bounds(Vector3.zero, new Vector3(10f, 10f, 0f));
            boundsInitialized = true;
        }
    }

    public int AbsorbSand(Vector3 worldPosition, Color32 targetColor, int maxAmount)
    {
        if (sandSimulation == null || !boundsInitialized) return 0;

        Vector2Int simPos = WorldToSimulation(worldPosition);
        
        int scanWidthPixels = Mathf.CeilToInt(scanWidth * sandSimulation.Width / simulationBounds.size.x);
        int scanHeightPixels = scanColumnAbove 
            ? sandSimulation.Height - simPos.y 
            : Mathf.CeilToInt(scanHeight * sandSimulation.Height / simulationBounds.size.y);

        var map = sandSimulation.GetCurrentWriteMap();
        int absorbed = 0;

        int startX = simPos.x - scanWidthPixels / 2;
        int endX = simPos.x + scanWidthPixels / 2;
        int startY = simPos.y;
        int endY = scanColumnAbove ? sandSimulation.Height - 1 : simPos.y + scanHeightPixels;

        startX = Mathf.Clamp(startX, 0, sandSimulation.Width - 1);
        endX = Mathf.Clamp(endX, 0, sandSimulation.Width - 1);
        startY = Mathf.Clamp(startY, 0, sandSimulation.Height - 1);
        endY = Mathf.Clamp(endY, 0, sandSimulation.Height - 1);

        for (int y = startY; y <= endY && absorbed < maxAmount; y++)
        {
            for (int x = startX; x <= endX && absorbed < maxAmount; x++)
            {
                int idx = y * sandSimulation.Width + x;
                var cell = map[idx];

                if (cell.type != 1) continue;

                if (IsColorMatch(cell.color, targetColor))
                {
                    map[idx] = new Cell 
                    { 
                        type = 0, 
                        color = new Color32(0, 0, 0, 0), 
                        hasMoved = false 
                    };
                    absorbed++;
                }
            }
        }

        if (absorbed > 0)
        {
            OnSandAbsorbed?.Invoke(absorbed);
        }

        return absorbed;
    }

    public int AbsorbSandForBucket(Bucket bucket)
    {
        if (bucket == null || bucket.Data == null || bucket.Data.IsFull) 
            return 0;

        int remaining = bucket.Data.capacity - bucket.Data.currentFill;
        int toAbsorb = Mathf.Min(remaining, maxAbsorbPerFrame);

        // Apply offset to scan from bottom of bucket instead of center
        Vector3 scanPosition = bucket.transform.position + new Vector3(0f, scanYOffset, 0f);

        int absorbed = AbsorbSand(
            scanPosition, 
            bucket.Data.bucketColor, 
            toAbsorb
        );

        if (absorbed > 0)
        {
            bucket.Data.TryAbsorb(absorbed);
        }

        return absorbed;
    }

    private bool IsColorMatch(Color32 sandColor, Color32 bucketColor)
    {
        if (sandSimulation != null)
        {
            return sandSimulation.IsColorInGroup(sandColor, bucketColor);
        }

        float distance = ColorGroup.ColorDistance(sandColor, bucketColor);
        return distance <= 50f;
    }

    public Vector2Int WorldToSimulation(Vector3 worldPos)
    {
        if (!boundsInitialized) InitializeBounds();

        float normalizedX = (worldPos.x - simulationBounds.min.x) / simulationBounds.size.x;
        float normalizedY = (worldPos.y - simulationBounds.min.y) / simulationBounds.size.y;

        int simX = Mathf.Clamp(Mathf.RoundToInt(normalizedX * sandSimulation.Width), 0, sandSimulation.Width - 1);
        int simY = Mathf.Clamp(Mathf.RoundToInt(normalizedY * sandSimulation.Height), 0, sandSimulation.Height - 1);

        return new Vector2Int(simX, simY);
    }

    public Vector3 SimulationToWorld(int simX, int simY)
    {
        if (!boundsInitialized) InitializeBounds();

        float normalizedX = (float)simX / sandSimulation.Width;
        float normalizedY = (float)simY / sandSimulation.Height;

        float worldX = simulationBounds.min.x + normalizedX * simulationBounds.size.x;
        float worldY = simulationBounds.min.y + normalizedY * simulationBounds.size.y;

        return new Vector3(worldX, worldY, 0f);
    }

    public int CountSandInColumn(int simX, Color32 targetColor)
    {
        if (sandSimulation == null) return 0;

        var map = sandSimulation.GetCurrentWriteMap();
        int count = 0;

        for (int y = 0; y < sandSimulation.Height; y++)
        {
            int idx = y * sandSimulation.Width + simX;
            var cell = map[idx];

            if (cell.type == 1 && IsColorMatch(cell.color, targetColor))
            {
                count++;
            }
        }

        return count;
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        
        Vector3 size = new Vector3(scanWidth, scanColumnAbove ? 10f : scanHeight, 0.1f);
        Vector3 scanStart = transform.position + new Vector3(0f, scanYOffset, 0f);
        Vector3 center = scanStart + Vector3.up * size.y / 2f;
        
        Gizmos.DrawCube(center, size);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}
