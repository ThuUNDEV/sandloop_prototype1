using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

public class SandSimulation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int width = 128;
    [SerializeField] private int height = 128;
    [SerializeField] private FilterMode filterMode = FilterMode.Point;
    
    [Header("Rendering")]
    public Renderer targetRenderer;
    
    // Core Data
    private Texture2D texture;
    private NativeArray<Cell> mapDataA;
    private NativeArray<Cell> mapDataB;
    private NativeArray<Color32> textureBuffer;
    private bool useMapA = true;

    // Public accessors
    public int Width => width;
    public int Height => height;
    public Bounds SimulationBounds => targetRenderer != null ? targetRenderer.bounds : new Bounds(Vector3.zero, new Vector3(10f, 10f, 0f));

    void Start()
    {
        InitializeBuffers();
        InitializeTexture();
    }

    void Update()
    {
        RunSimulation();
        UpdateTexture();
    }

    void OnDestroy()
    {
        if (mapDataA.IsCreated) mapDataA.Dispose();
        if (mapDataB.IsCreated) mapDataB.Dispose();
        if (textureBuffer.IsCreated) textureBuffer.Dispose();
    }

    void InitializeBuffers()
    {
        int totalPixels = width * height;
        mapDataA = new NativeArray<Cell>(totalPixels, Allocator.Persistent);
        mapDataB = new NativeArray<Cell>(totalPixels, Allocator.Persistent);
        textureBuffer = new NativeArray<Color32>(totalPixels, Allocator.Persistent);
    }

    void InitializeTexture()
    {
        texture = new Texture2D(width, height);
        texture.filterMode = filterMode;
        targetRenderer.material.mainTexture = texture;
    }

    void RunSimulation()
    {
        var readMap = useMapA ? mapDataA : mapDataB;
        var writeMap = useMapA ? mapDataB : mapDataA;

        // Physics Job
        var physicsJob = new SandPhysicsJob
        {
            width = width,
            height = height,
            readMap = readMap,
            writeMap = writeMap,
            randomSeed = Time.time
        };
        JobHandle physicsHandle = physicsJob.Schedule();

        // Render Job
        var renderJob = new SandRenderJob
        {
            mapData = writeMap,
            textureOut = textureBuffer
        };
        JobHandle renderHandle = renderJob.Schedule(width * height, 64, physicsHandle);

        renderHandle.Complete();
        useMapA = !useMapA;
    }

    void UpdateTexture()
    {
        texture.SetPixelData(textureBuffer, 0);
        texture.Apply();
    }

    // Public API for external scripts
    public NativeArray<Cell> GetCurrentWriteMap()
    {
        return useMapA ? mapDataA : mapDataB;
    }

    public NativeArray<Cell> GetCurrentReadMap()
    {
        return useMapA ? mapDataB : mapDataA;
    }

    public void SetCell(int x, int y, Cell cell)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        
        var map = GetCurrentWriteMap();
        int idx = y * width + x;
        map[idx] = cell;
    }

    public Cell GetCell(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return new Cell { type = 0 };
        
        var map = GetCurrentWriteMap();
        int idx = y * width + x;
        return map[idx];
    }

    public void ClearCell(int x, int y)
    {
        SetCell(x, y, new Cell { type = 0, color = new Color32(0, 0, 0, 0), hasMoved = false });
    }

    public void ClearAllSand()
    {
        var map = GetCurrentWriteMap();
        for (int i = 0; i < map.Length; i++)
        {
            if (map[i].type == 1)
            {
                map[i] = new Cell { type = 0, color = new Color32(0, 0, 0, 0), hasMoved = false };
            }
        }
    }

    public int CountSandPixels()
    {
        var map = GetCurrentWriteMap();
        int count = 0;
        for (int i = 0; i < map.Length; i++)
        {
            if (map[i].type == 1) count++;
        }
        return count;
    }

    public Vector2Int WorldToSimulation(Vector3 worldPos)
    {
        Bounds bounds = SimulationBounds;
        float normalizedX = (worldPos.x - bounds.min.x) / bounds.size.x;
        float normalizedY = (worldPos.y - bounds.min.y) / bounds.size.y;

        int simX = Mathf.Clamp(Mathf.RoundToInt(normalizedX * width), 0, width - 1);
        int simY = Mathf.Clamp(Mathf.RoundToInt(normalizedY * height), 0, height - 1);

        return new Vector2Int(simX, simY);
    }

    public Vector3 SimulationToWorld(int simX, int simY)
    {
        Bounds bounds = SimulationBounds;
        float normalizedX = (float)simX / width;
        float normalizedY = (float)simY / height;

        float worldX = bounds.min.x + normalizedX * bounds.size.x;
        float worldY = bounds.min.y + normalizedY * bounds.size.y;

        return new Vector3(worldX, worldY, 0f);
    }
}
