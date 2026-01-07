using UnityEngine;
using UnityEngine.Events;

public class SandAbsorber : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxAbsorbPerFrame = 5;
    [SerializeField] private float absorbInterval = 0f;  // Delay giữa các lần hút (giây)
    
    [Header("Scan Zone (pixels)")]
    [SerializeField] private int scanWidth = 20;
    [SerializeField] private int scanHeight = 0;  // 0 = full height
    
    [Header("Scan Offset (pixels)")]
    [SerializeField] private int offsetX = 0;
    [SerializeField] private int offsetY = 0;

    [Header("References")]
    [SerializeField] private SandSimulation sandSimulation;
    [SerializeField] private Renderer simulationRenderer;

    [Header("Events")]
    public UnityEvent<int> OnSandAbsorbed;

    private Bounds simulationBounds;
    private bool boundsInitialized = false;
    private bool referencesInitialized = false;
    private float lastAbsorbTime = 0f;

    void Start()
    {
        CacheReferences();
        InitializeBounds();
    }

    private void CacheReferences()
    {
        if (referencesInitialized) return;

        if (sandSimulation == null)
            sandSimulation = GameServiceLocator.Instance.SandSimulation;

        if (simulationRenderer == null && sandSimulation != null)
            simulationRenderer = sandSimulation.GetComponent<Renderer>();

        referencesInitialized = true;
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
            // Fallback bounds
            simulationBounds = new Bounds(Vector3.zero, new Vector3(10f, 10f, 0f));
            boundsInitialized = true;
        }
    }

    public void SetReferences(SandSimulation simulation)
    {
        sandSimulation = simulation;
        if (simulation != null)
            simulationRenderer = simulation.GetComponent<Renderer>();
        referencesInitialized = true;
        InitializeBounds();
    }

    /// <summary>
    /// Chuyển vị trí world X sang tọa độ simulation X
    /// </summary>
    private int WorldToSimX(float worldX)
    {
        if (!boundsInitialized) InitializeBounds();
        
        float normalizedX = (worldX - simulationBounds.min.x) / simulationBounds.size.x;
        return Mathf.Clamp(Mathf.RoundToInt(normalizedX * sandSimulation.Width), 0, sandSimulation.Width - 1);
    }

    /// <summary>
    /// Hút cát trong vùng scan tại vị trí X của bucket
    /// </summary>
    public int AbsorbSandAtColumn(float worldX, Color32 targetColor, int maxAmount)
    {
        if (sandSimulation == null || !boundsInitialized) return 0;

        var map = sandSimulation.GetCurrentWriteMap();
        int absorbed = 0;
        int width = sandSimulation.Width;
        int height = sandSimulation.Height;

        // Tính vị trí X trên simulation + offset
        int centerX = WorldToSimX(worldX) + offsetX;
        int halfWidth = scanWidth / 2;
        
        int startX = Mathf.Clamp(centerX - halfWidth, 0, width - 1);
        int endX = Mathf.Clamp(centerX + halfWidth, 0, width - 1);
        
        // Tính vùng Y (0 = full height)
        int startY = Mathf.Clamp(offsetY, 0, height - 1);
        int endY = (scanHeight <= 0) ? height - 1 : Mathf.Clamp(offsetY + scanHeight, 0, height - 1);

        // Quét từ dưới lên, trong phạm vi vùng scan
        for (int y = startY; y <= endY && absorbed < maxAmount; y++)
        {
            for (int x = startX; x <= endX && absorbed < maxAmount; x++)
            {
                int idx = y * width + x;
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

        // Kiểm tra delay giữa các lần hút
        if (absorbInterval > 0f && Time.time - lastAbsorbTime < absorbInterval)
            return 0;

        int remaining = bucket.Data.capacity - bucket.Data.currentFill;
        int toAbsorb = Mathf.Min(remaining, maxAbsorbPerFrame);

        // Lấy vị trí X của bucket trên world
        float bucketWorldX = bucket.transform.position.x;

        int absorbed = AbsorbSandAtColumn(bucketWorldX, bucket.Data.bucketColor, toAbsorb);

        if (absorbed > 0)
        {
            bucket.Data.TryAbsorb(absorbed);
            lastAbsorbTime = Time.time;
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

    void OnDrawGizmosSelected()
    {
        if (sandSimulation == null) return;
        if (!boundsInitialized) InitializeBounds();

        int width = sandSimulation.Width;
        int height = sandSimulation.Height;

        // Tính kích thước vùng scan trong world units
        float worldWidth = (float)scanWidth / width * simulationBounds.size.x;
        float worldHeight = (scanHeight <= 0) 
            ? simulationBounds.size.y 
            : (float)scanHeight / height * simulationBounds.size.y;
        
        // Tính offset trong world units
        float worldOffsetX = (float)offsetX / width * simulationBounds.size.x;
        float worldOffsetY = (float)offsetY / height * simulationBounds.size.y;
        
        // Vị trí trung tâm vùng scan (ở giữa simulation + offset)
        Vector3 center = simulationBounds.center;
        center.x += worldOffsetX;
        center.y = simulationBounds.min.y + worldOffsetY + worldHeight / 2f;
        
        Vector3 size = new Vector3(worldWidth, worldHeight, 0.1f);
        
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawCube(center, size);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}
