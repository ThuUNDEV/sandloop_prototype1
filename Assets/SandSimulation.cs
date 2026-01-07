using UnityEngine;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using System.Collections.Generic;
using System.Linq;

public class SandSimulation : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private FilterMode filterMode = FilterMode.Point;
    
    [Header("Rendering")]
    public Renderer targetRenderer;

    [Header("Image Source")]
    public ColorAnalysisData colorAnalysisData;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;
    [SerializeField] private bool writeDebugToFile = false;
    
    // Core Data
    private Texture2D texture;
    private NativeArray<Cell> mapDataA;
    private NativeArray<Cell> mapDataB;
    private NativeArray<Color32> textureBuffer;
    private bool useMapA = true;
    private int frameCounter = 0; // Đếm frame để kiểm soát tốc độ rơi

    // Color Analysis Data
    private List<ColorGroup> colorGroups = new List<ColorGroup>();
    private List<BucketData> bucketDataList = new List<BucketData>();
    [SerializeField]
    private int simulationFrameInterval;

    // Size từ colorAnalysisData
    private int width => colorAnalysisData != null ? colorAnalysisData.sourceWidth : 128;
    private int height => colorAnalysisData != null ? colorAnalysisData.sourceHeight : 128;

    // Public accessors
    public int Width => width;
    public int Height => height;
    public Bounds SimulationBounds => targetRenderer != null ? targetRenderer.bounds : new Bounds(Vector3.zero, new Vector3(10f, 10f, 0f));
    public Texture2D SourceTexture => colorAnalysisData != null ? colorAnalysisData.sourceTexture : null;
    
    // Color Analysis accessors
    public List<ColorGroup> ColorGroups => colorGroups;
    public List<BucketData> BucketDataList => bucketDataList;
    public bool HasPrecomputedData => colorAnalysisData != null;
    public float ColorThreshold => colorAnalysisData != null ? colorAnalysisData.colorThreshold : 30f;

    void Start()
    {
        InitializeBuffers();
        
        InitializeTexture();
        
        // Load precomputed data if available
        if (colorAnalysisData != null)
        {
            LoadFromPrecomputedData();
        }
        
        if (colorAnalysisData != null && colorAnalysisData.sourceTexture != null)
        {
            SpawnSandArt();
        }
    }

    void Update()
    {
        frameCounter++;
        
        // Chỉ chạy physics simulation mỗi N frame
        if (frameCounter >= simulationFrameInterval)
        {
            RunSimulation();
            frameCounter = 0;
        }
        
        // Luôn cập nhật texture để giữ render mượt
        UpdateTexture();

        // Debug: Nhấn D để phân tích cát còn lại
        if (Input.GetKeyDown(KeyCode.D))
        {
            DebugRemainingSand();
        }
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

    #region Sand Art Generation
    
    public void SpawnSandArt()
    {
        if (colorAnalysisData == null || colorAnalysisData.sourceTexture == null) return;

        Texture2D sourceImage = colorAnalysisData.sourceTexture;
        int imgWidth = sourceImage.width;
        int imgHeight = sourceImage.height;

        // Cache tất cả pixels một lần
        Color32[] sourcePixels = sourceImage.GetPixels32();
        
        NativeArray<Cell> targetMap = GetCurrentWriteMap();

        int spawnedPixels = 0;

        // Spawn cát từ ảnh - không cần scale vì simulation size = image size
        for (int y = 0; y < imgHeight; y++)
        {
            int rowOffset = y * width;
            int srcRowOffset = y * imgWidth;

            for (int x = 0; x < imgWidth; x++)
            {
                Color32 pixelColor = sourcePixels[srcRowOffset + x];

                if (pixelColor.a > 25)
                {
                    int idx = rowOffset + x;
                    targetMap[idx] = new Cell 
                    { 
                        type = 1,
                        color = pixelColor
                    };
                    spawnedPixels++;
                }
            }
        }
    }

    #endregion

    #region Color Analysis
    
    public void LoadFromPrecomputedData()
    {
        if (colorAnalysisData == null)
        {
            Debug.LogWarning("SandSimulation: No precomputed data assigned!");
            return;
        }

        colorGroups.Clear();
        bucketDataList.Clear();

        // Load color groups
        foreach (var groupData in colorAnalysisData.colorGroups)
        {
            var group = new ColorGroup(groupData.representativeColor, groupData.pixelCount);
            colorGroups.Add(group);
        }

        // Load bucket data
        foreach (var bucketSO in colorAnalysisData.buckets)
        {
            bucketDataList.Add(bucketSO.ToBucketData());
        }

        if (showDebugLog)
        {
            Debug.Log($"SandSimulation: Loaded {colorGroups.Count} color groups, {bucketDataList.Count} buckets from precomputed data");
        }
    }

    public void AnalyzeTexture(Texture2D textureToAnalyze)
    {
        if (colorAnalysisData != null)
        {
            LoadFromPrecomputedData();
            return;
        }

        Debug.LogWarning("SandSimulation: No precomputed ColorAnalysisData assigned!");
    }

    public void AnalyzeFromCurrentMap()
    {
        if (colorAnalysisData != null)
        {
            LoadFromPrecomputedData();
            return;
        }

        Debug.LogWarning("SandSimulation: No precomputed ColorAnalysisData assigned!");
    }

    public bool IsColorInGroup(Color32 color, Color32 groupColor)
    {
        if (colorGroups.Count == 0) return false;

        // Tìm nhóm gần nhất với màu cát
        ColorGroup closestGroup = null;
        float minDistance = float.MaxValue;
        
        foreach (var group in colorGroups)
        {
            float dist = ColorGroup.ColorDistance(color, group.representativeColor);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestGroup = group;
            }
        }

        if (closestGroup == null) return false;

        // Kiểm tra xem nhóm gần nhất có phải là nhóm của bucket không
        float distToGroupColor = ColorGroup.ColorDistance(closestGroup.representativeColor, groupColor);
        return distToGroupColor < 1f;
    }
    
    #endregion

    #region Public API
    
    public NativeArray<Cell> GetCurrentWriteMap()
    {
        return useMapA ? mapDataA : mapDataB;
    }

    /// <summary>
    /// Debug: Phân tích cát còn lại và kiểm tra xem có khớp với bucket nào không
    /// Gọi bằng phím D trong khi chạy game
    /// </summary>
    public void DebugRemainingSand()
    {
        var map = GetCurrentWriteMap();
        var remainingColors = new Dictionary<Color32, int>(new Color32Comparer());
        int totalRemaining = 0;

        // Đếm cát còn lại theo màu
        for (int i = 0; i < map.Length; i++)
        {
            if (map[i].type == 1)
            {
                var color = map[i].color;
                if (!remainingColors.ContainsKey(color))
                    remainingColors[color] = 0;
                remainingColors[color]++;
                totalRemaining++;
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("\n## Debug: Remaining Sand Analysis");
        sb.AppendLine($"**Time:** {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Total Remaining Sand:** {totalRemaining:N0}");
        sb.AppendLine();

        if (totalRemaining == 0)
        {
            sb.AppendLine("✓ All sand has been absorbed!");
            WriteDebugLog(sb.ToString(), true);
            Debug.Log("[SandDebug] All sand absorbed!");
            return;
        }

        // Phân tích màu còn lại
        sb.AppendLine("### Remaining Colors (top 20):");
        sb.AppendLine("| Color (RGB) | Count | Matches Bucket? | Matched Group |");
        sb.AppendLine("|-------------|-------|-----------------|---------------|");

        var sortedColors = remainingColors.OrderByDescending(kv => kv.Value).Take(20);
        int unmatchedTotal = 0;

        foreach (var kv in sortedColors)
        {
            var color = kv.Key;
            var count = kv.Value;
            
            // Kiểm tra xem màu này có khớp với bucket nào không
            string matchedBucket = "❌ No";
            string matchedGroup = "-";
            
            foreach (var bd in bucketDataList)
            {
                if (IsColorInGroup(color, bd.bucketColor))
                {
                    matchedBucket = "✓ Yes";
                    matchedGroup = $"RGB({bd.bucketColor.r},{bd.bucketColor.g},{bd.bucketColor.b})";
                    break;
                }
            }

            if (matchedBucket == "❌ No")
                unmatchedTotal += count;

            sb.AppendLine($"| ({color.r}, {color.g}, {color.b}) | {count:N0} | {matchedBucket} | {matchedGroup} |");
        }

        sb.AppendLine();
        sb.AppendLine($"**Unmatched sand total:** {unmatchedTotal:N0}");
        
        // Liệt kê các bucket và capacity còn lại
        sb.AppendLine();
        sb.AppendLine("### Bucket Status:");
        sb.AppendLine("| # | Color (RGB) | Capacity | Filled | Remaining |");
        sb.AppendLine("|---|-------------|----------|--------|-----------|");
        
        for (int i = 0; i < bucketDataList.Count; i++)
        {
            var bd = bucketDataList[i];
            int remaining = bd.capacity - bd.currentFill;
            string status = bd.IsFull ? "✓ Full" : $"{remaining:N0}";
            sb.AppendLine($"| {i} | ({bd.bucketColor.r}, {bd.bucketColor.g}, {bd.bucketColor.b}) | {bd.capacity:N0} | {bd.currentFill:N0} | {status} |");
        }

        WriteDebugLog(sb.ToString(), true);
        Debug.Log($"[SandDebug] Remaining sand: {totalRemaining}, Unmatched: {unmatchedTotal}. Check log file for details.");
    }

    // Comparer cho Color32 để dùng trong Dictionary
    private class Color32Comparer : IEqualityComparer<Color32>
    {
        public bool Equals(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        public int GetHashCode(Color32 c)
        {
            return (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
        }
    }
    
    #endregion

    #region Debug Logging

    private static string debugLogPath = null;

    public static string GetDebugLogPath()
    {
        if (debugLogPath == null)
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            debugLogPath = Path.Combine(Application.dataPath, $"SandDebug_{timestamp}.md");
        }
        return debugLogPath;
    }

    public static void WriteDebugLog(string content, bool append = true)
    {
        // Disabled - set writeDebugToFile = true in Inspector to enable
    }

    public void AppendBucketDebugLog(List<BucketData> bucketDataList)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Bucket Generation");
        sb.AppendLine();

        int totalCapacity = 0;
        foreach (var bd in bucketDataList)
        {
            totalCapacity += bd.capacity;
        }

        sb.AppendLine($"**Total Buckets:** {bucketDataList.Count}");
        sb.AppendLine($"**Total Capacity (sum):** {totalCapacity:N0}");
        sb.AppendLine();
        sb.AppendLine("| # | Color (RGB) | Capacity |");
        sb.AppendLine("|---|-------------|----------|");

        for (int i = 0; i < bucketDataList.Count; i++)
        {
            var bd = bucketDataList[i];
            sb.AppendLine($"| {i} | ({bd.bucketColor.r}, {bd.bucketColor.g}, {bd.bucketColor.b}) | {bd.capacity:N0} |");
        }
        sb.AppendLine();

        WriteDebugLog(sb.ToString(), true);
    }

    #endregion
}
