using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ColorQuantizer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxColorGroups = 12;
    [SerializeField] private float colorThreshold = 30f;
    [SerializeField] private int maxTotalBuckets = 16;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private List<ColorGroup> colorGroups = new List<ColorGroup>();
    private List<BucketData> bucketDataList = new List<BucketData>();

    public List<ColorGroup> ColorGroups => colorGroups;
    public List<BucketData> BucketDataList => bucketDataList;

    public void AnalyzeTexture(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogError("ColorQuantizer: Texture is null!");
            return;
        }

        colorGroups.Clear();
        
        Color32[] pixels = texture.GetPixels32();
        
        foreach (var pixel in pixels)
        {
            if (pixel.a < 25) continue;
            
            AddColorToGroups(pixel);
        }

        if (colorGroups.Count > maxColorGroups)
        {
            MergeSmallestGroups();
        }

        colorGroups = colorGroups.OrderByDescending(g => g.pixelCount).ToList();

        GenerateBucketData();

        if (showDebugLog)
        {
            LogResults();
        }
    }

    public void AnalyzeFromSandSimulation(SandSimulation simulation)
    {
        if (simulation == null)
        {
            Debug.LogError("ColorQuantizer: SandSimulation is null!");
            return;
        }

        colorGroups.Clear();

        var map = simulation.GetCurrentWriteMap();
        
        for (int i = 0; i < map.Length; i++)
        {
            var cell = map[i];
            if (cell.type == 1)
            {
                AddColorToGroups(cell.color);
            }
        }

        if (colorGroups.Count > maxColorGroups)
        {
            MergeSmallestGroups();
        }

        colorGroups = colorGroups.OrderByDescending(g => g.pixelCount).ToList();

        GenerateBucketData();

        if (showDebugLog)
        {
            LogResults();
        }
    }

    private void AddColorToGroups(Color32 color)
    {
        foreach (var group in colorGroups)
        {
            if (group.IsColorSimilar(color, colorThreshold))
            {
                group.AddColor(color);
                return;
            }
        }

        colorGroups.Add(new ColorGroup(color));
    }

    private void MergeSmallestGroups()
    {
        while (colorGroups.Count > maxColorGroups)
        {
            colorGroups = colorGroups.OrderBy(g => g.pixelCount).ToList();

            var smallest = colorGroups[0];
            colorGroups.RemoveAt(0);

            float minDistance = float.MaxValue;
            ColorGroup closestGroup = null;

            foreach (var group in colorGroups)
            {
                float distance = ColorGroup.ColorDistance(
                    smallest.representativeColor, 
                    group.representativeColor
                );
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestGroup = group;
                }
            }

            if (closestGroup != null)
            {
                foreach (var color in smallest.colors)
                {
                    closestGroup.AddColor(color);
                }
            }
        }
    }

    private void GenerateBucketData()
    {
        bucketDataList.Clear();

        int totalPixels = colorGroups.Sum(g => g.pixelCount);
        
        if (totalPixels == 0 || colorGroups.Count == 0) return;

        // Phân bổ số xô theo tỷ lệ pixel của mỗi nhóm màu
        // Tổng số xô không vượt quá maxTotalBuckets
        int remainingBuckets = maxTotalBuckets;
        int remainingPixels = totalPixels;

        foreach (var group in colorGroups)
        {
            if (remainingBuckets <= 0) break;

            // Tính số xô cho nhóm này theo tỷ lệ
            float ratio = (float)group.pixelCount / remainingPixels;
            int bucketsForGroup = Mathf.Max(1, Mathf.RoundToInt(ratio * remainingBuckets));
            bucketsForGroup = Mathf.Min(bucketsForGroup, remainingBuckets);

            // Chia đều capacity cho các xô trong nhóm
            int capacityPerBucket = Mathf.CeilToInt((float)group.pixelCount / bucketsForGroup);

            for (int i = 0; i < bucketsForGroup; i++)
            {
                int remaining = group.pixelCount - (i * capacityPerBucket);
                int capacity = Mathf.Min(capacityPerBucket, remaining);
                
                if (capacity > 0)
                {
                    bucketDataList.Add(new BucketData(group.representativeColor, capacity));
                }
            }

            remainingBuckets -= bucketsForGroup;
            remainingPixels -= group.pixelCount;
        }

        Debug.Log($"ColorQuantizer: Created {bucketDataList.Count} buckets for {totalPixels} pixels");
    }

    public ColorGroup FindMatchingGroup(Color32 color)
    {
        foreach (var group in colorGroups)
        {
            if (group.IsColorSimilar(color, colorThreshold))
            {
                return group;
            }
        }
        return null;
    }

    public bool IsColorInGroup(Color32 color, Color32 groupColor)
    {
        foreach (var group in colorGroups)
        {
            if (ColorGroup.ColorDistance(group.representativeColor, groupColor) < 1f)
            {
                return group.IsColorSimilar(color, colorThreshold);
            }
        }
        return false;
    }

    private void LogResults()
    {
        Debug.Log($"=== ColorQuantizer Results ===");
        Debug.Log($"Total color groups: {colorGroups.Count}");
        Debug.Log($"Total buckets needed: {bucketDataList.Count}");
        
        foreach (var group in colorGroups)
        {
            Color c = group.representativeColor;
            Debug.Log($"  Group: RGB({group.representativeColor.r}, {group.representativeColor.g}, {group.representativeColor.b}) - {group.pixelCount} pixels");
        }
    }

    [ContextMenu("Test Analyze (Editor)")]
    public void TestAnalyze()
    {
        var artGenerator = GetComponent<SandArtGenerator>();
        if (artGenerator != null && artGenerator.sourceImage != null)
        {
            AnalyzeTexture(artGenerator.sourceImage);
        }
        else
        {
            Debug.LogWarning("No SandArtGenerator or sourceImage found!");
        }
    }
}
