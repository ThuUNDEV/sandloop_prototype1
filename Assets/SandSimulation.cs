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
}
