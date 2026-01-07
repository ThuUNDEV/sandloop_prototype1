using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class ColorAnalysisWindow : EditorWindow
{
    private Texture2D sourceTexture;
    private ColorAnalysisData existingData;
    
    private int maxColorGroups = 12;
    private float colorThreshold = 30f;
    private int maxTotalBuckets = 16;
    private bool autoThreshold = true;
    private float calculatedThreshold = 0f;
    
    private Vector2 scrollPosition;
    private List<ColorGroupData> previewGroups = new List<ColorGroupData>();
    private List<BucketDataSO> previewBuckets = new List<BucketDataSO>();
    private bool hasAnalyzed = false;
    private float analysisTime = 0f;

    [MenuItem("Tools/Color Analysis (Pre-compute)")]
    public static void ShowWindow()
    {
        var window = GetWindow<ColorAnalysisWindow>("Color Analysis");
        window.minSize = new Vector2(400, 500);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        EditorGUILayout.Space(10);

        DrawSourceSection();
        EditorGUILayout.Space(10);

        DrawSettingsSection();
        EditorGUILayout.Space(10);

        DrawActionButtons();
        EditorGUILayout.Space(10);

        if (hasAnalyzed)
        {
            DrawResultsPreview();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Color Analysis (Pre-compute)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Phân tích màu sắc trước khi chạy game.\n" +
            "Kết quả sẽ được lưu vào ScriptableObject.",
            MessageType.Info
        );
    }

    private void DrawSourceSection()
    {
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField(
            "Source Texture",
            sourceTexture,
            typeof(Texture2D),
            false
        );

        if (sourceTexture != null)
        {
            EditorGUILayout.LabelField($"  Size: {sourceTexture.width} x {sourceTexture.height}");
            EditorGUILayout.LabelField($"  Pixels: {sourceTexture.width * sourceTexture.height:N0}");
        }

        EditorGUILayout.Space(5);
        
        existingData = (ColorAnalysisData)EditorGUILayout.ObjectField(
            "Existing Data (Optional)",
            existingData,
            typeof(ColorAnalysisData),
            false
        );

        if (existingData != null)
        {
            EditorGUILayout.LabelField($"  Source: {existingData.sourceImageName}");
            EditorGUILayout.LabelField($"  Analyzed: {existingData.analyzedDate}");
            EditorGUILayout.LabelField($"  Buckets: {existingData.buckets.Count}");
        }
    }

    private void DrawSettingsSection()
    {
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        
        maxColorGroups = EditorGUILayout.IntSlider("Max Color Groups", maxColorGroups, 2, 20);
        
        autoThreshold = EditorGUILayout.Toggle("Auto Calculate Threshold", autoThreshold);
        
        if (autoThreshold)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Color Threshold (Auto)", calculatedThreshold);
            EditorGUI.EndDisabledGroup();
            
            if (calculatedThreshold > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Threshold tự động: {calculatedThreshold:F1}\n" +
                    "Đảm bảo tất cả pixel đều thuộc một nhóm màu.",
                    MessageType.Info
                );
            }
        }
        else
        {
            colorThreshold = EditorGUILayout.Slider("Color Threshold", colorThreshold, 10f, 150f);
        }
        
        maxTotalBuckets = EditorGUILayout.IntSlider("Max Total Buckets", maxTotalBuckets, 4, 32);
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = sourceTexture != null;
        
        // Nếu có existingData, nút sẽ là "Analyze & Save"
        string analyzeButtonText = existingData != null ? "Analyze & Update" : "Analyze";
        if (GUILayout.Button(analyzeButtonText, GUILayout.Height(30)))
        {
            AnalyzeTexture();
            
            // Tự động lưu vào existingData nếu có
            if (existingData != null && hasAnalyzed)
            {
                SaveToExistingData();
            }
        }

        GUI.enabled = hasAnalyzed && previewBuckets.Count > 0 && existingData == null;
        if (GUILayout.Button("Save as New", GUILayout.Height(30)))
        {
            SaveToNewScriptableObject();
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (hasAnalyzed)
        {
            EditorGUILayout.LabelField($"Analysis time: {analysisTime:F2}ms", EditorStyles.miniLabel);
        }
    }

    private void DrawResultsPreview()
    {
        EditorGUILayout.LabelField("Results Preview", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField($"Color Groups: {previewGroups.Count}");
        EditorGUILayout.LabelField($"Total Buckets: {previewBuckets.Count}");

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Color Groups:", EditorStyles.miniLabel);

        foreach (var group in previewGroups)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Draw color preview
            Rect colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
            EditorGUI.DrawRect(colorRect, group.representativeColor);
            
            EditorGUILayout.LabelField(
                $"RGB({group.representativeColor.r}, {group.representativeColor.g}, {group.representativeColor.b}) - {group.pixelCount:N0} pixels"
            );
            
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Buckets:", EditorStyles.miniLabel);

        // Group buckets by color for display
        var bucketsByColor = previewBuckets
            .GroupBy(b => b.bucketColor)
            .Select(g => new { Color = g.Key, Count = g.Count(), TotalCapacity = g.Sum(b => b.capacity) });

        foreach (var group in bucketsByColor)
        {
            EditorGUILayout.BeginHorizontal();
            
            Rect colorRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
            EditorGUI.DrawRect(colorRect, group.Color);
            
            EditorGUILayout.LabelField($"{group.Count} buckets, total capacity: {group.TotalCapacity:N0}");
            
            EditorGUILayout.EndHorizontal();
        }
    }

    private void AnalyzeTexture()
    {
        if (sourceTexture == null) return;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Make texture readable
        string path = AssetDatabase.GetAssetPath(sourceTexture);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        bool wasReadable = true;
        
        if (importer != null && !importer.isReadable)
        {
            wasReadable = false;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        Color32[] pixels = sourceTexture.GetPixels32();

        // Nếu auto threshold, sử dụng K-means style clustering
        if (autoThreshold)
        {
            AnalyzeWithAutoThreshold(pixels);
        }
        else
        {
            AnalyzeWithFixedThreshold(pixels);
        }

        // Restore texture settings
        if (!wasReadable && importer != null)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        stopwatch.Stop();
        analysisTime = stopwatch.ElapsedMilliseconds;
        hasAnalyzed = true;

        Repaint();
    }

    private void AnalyzeWithFixedThreshold(Color32[] pixels)
    {
        var colorGroups = new List<TempColorGroup>();

        foreach (var pixel in pixels)
        {
            if (pixel.a < 25) continue;
            AddColorToGroups(pixel, colorGroups);
        }

        // Merge if needed
        while (colorGroups.Count > maxColorGroups)
        {
            MergeSmallestGroups(colorGroups);
        }

        FinalizeGroups(colorGroups, pixels);
    }

    private void AnalyzeWithAutoThreshold(Color32[] pixels)
    {
        // Bước 1: Thu thập tất cả màu không trong suốt
        var uniqueColors = new Dictionary<Color32, int>(new Color32Comparer());
        
        foreach (var pixel in pixels)
        {
            if (pixel.a < 25) continue;
            
            if (!uniqueColors.ContainsKey(pixel))
                uniqueColors[pixel] = 0;
            uniqueColors[pixel]++;
        }

        if (uniqueColors.Count == 0) return;

        // Bước 2: Khởi tạo các nhóm với màu phổ biến nhất (không tính pixel)
        var sortedColors = uniqueColors.OrderByDescending(kv => kv.Value).ToList();
        var colorGroups = new List<TempColorGroup>();

        // Chọn màu đầu tiên cho mỗi nhóm (cách xa nhau nhất)
        colorGroups.Add(new TempColorGroup(sortedColors[0].Key, true)); // emptyInit = true
        
        while (colorGroups.Count < maxColorGroups && colorGroups.Count < sortedColors.Count)
        {
            // Tìm màu xa nhất so với tất cả các nhóm hiện có
            Color32 farthestColor = sortedColors[0].Key;
            float maxMinDistance = 0;

            foreach (var kv in sortedColors)
            {
                float minDistToAnyGroup = float.MaxValue;
                foreach (var group in colorGroups)
                {
                    float dist = ColorDistance(kv.Key, group.representativeColor);
                    if (dist < minDistToAnyGroup)
                        minDistToAnyGroup = dist;
                }

                if (minDistToAnyGroup > maxMinDistance)
                {
                    maxMinDistance = minDistToAnyGroup;
                    farthestColor = kv.Key;
                }
            }

            if (maxMinDistance > 1f)
            {
                colorGroups.Add(new TempColorGroup(farthestColor, true)); // emptyInit = true
            }
            else
            {
                break;
            }
        }

        // Bước 3: Gán tất cả pixel vào nhóm gần nhất và tính threshold
        float maxDistanceUsed = 0;

        foreach (var kv in sortedColors)
        {
            Color32 color = kv.Key;
            int count = kv.Value;

            TempColorGroup closestGroup = null;
            float minDistance = float.MaxValue;

            foreach (var group in colorGroups)
            {
                float distance = ColorDistance(color, group.representativeColor);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestGroup = group;
                }
            }

            if (closestGroup != null)
            {
                for (int i = 0; i < count; i++)
                {
                    closestGroup.AddColor(color);
                }

                if (minDistance > maxDistanceUsed)
                {
                    maxDistanceUsed = minDistance;
                }
            }
        }

        // Cập nhật threshold tự động
        calculatedThreshold = maxDistanceUsed + 1f; // +1 để đảm bảo bao gồm tất cả
        colorThreshold = calculatedThreshold;

        FinalizeGroups(colorGroups, pixels);
        
        Debug.Log($"[ColorAnalysis] Auto threshold: {calculatedThreshold:F1} (max distance: {maxDistanceUsed:F1})");
    }

    private void FinalizeGroups(List<TempColorGroup> colorGroups, Color32[] pixels)
    {
        // Sort by pixel count
        colorGroups = colorGroups.OrderByDescending(g => g.pixelCount).ToList();

        // Convert to preview data
        previewGroups.Clear();
        foreach (var group in colorGroups)
        {
            previewGroups.Add(new ColorGroupData(group.representativeColor, group.pixelCount));
        }

        // Verify total
        int totalGroupedPixels = colorGroups.Sum(g => g.pixelCount);
        int totalNonTransparent = pixels.Count(p => p.a >= 25);
        
        if (totalGroupedPixels != totalNonTransparent)
        {
            Debug.LogWarning($"[ColorAnalysis] Pixel mismatch! Grouped: {totalGroupedPixels}, Actual: {totalNonTransparent}");
        }
        else
        {
            Debug.Log($"[ColorAnalysis] ✓ All {totalGroupedPixels} pixels assigned to {colorGroups.Count} groups");
        }

        // Generate buckets
        GenerateBuckets(colorGroups);
    }

    // Comparer cho Color32
    private class Color32Comparer : IEqualityComparer<Color32>
    {
        public bool Equals(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b;
        }

        public int GetHashCode(Color32 c)
        {
            return (c.r << 16) | (c.g << 8) | c.b;
        }
    }

    private void AddColorToGroups(Color32 color, List<TempColorGroup> groups)
    {
        // Tìm nhóm trong threshold trước
        foreach (var group in groups)
        {
            if (IsColorSimilar(color, group.representativeColor, colorThreshold))
            {
                group.AddColor(color);
                return;
            }
        }

        // Nếu không tìm thấy và đã đạt max groups, gán vào nhóm gần nhất
        if (groups.Count >= maxColorGroups)
        {
            TempColorGroup closestGroup = null;
            float minDistance = float.MaxValue;

            foreach (var group in groups)
            {
                float distance = ColorDistance(color, group.representativeColor);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestGroup = group;
                }
            }

            if (closestGroup != null)
            {
                closestGroup.AddColor(color);
                return;
            }
        }

        // Tạo nhóm mới nếu chưa đạt max
        groups.Add(new TempColorGroup(color));
    }

    private bool IsColorSimilar(Color32 a, Color32 b, float threshold)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db) <= threshold;
    }

    private void MergeSmallestGroups(List<TempColorGroup> groups)
    {
        groups.Sort((a, b) => a.pixelCount.CompareTo(b.pixelCount));

        var smallest = groups[0];
        groups.RemoveAt(0);

        float minDistance = float.MaxValue;
        TempColorGroup closest = null;

        foreach (var group in groups)
        {
            float distance = ColorDistance(smallest.representativeColor, group.representativeColor);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = group;
            }
        }

        if (closest != null)
        {
            closest.Merge(smallest);
        }
    }

    private float ColorDistance(Color32 a, Color32 b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    private void GenerateBuckets(List<TempColorGroup> colorGroups)
    {
        previewBuckets.Clear();

        int totalPixels = colorGroups.Sum(g => g.pixelCount);
        if (totalPixels == 0) return;

        int remainingBuckets = maxTotalBuckets;
        int remainingPixels = totalPixels;

        foreach (var group in colorGroups)
        {
            if (remainingBuckets <= 0) break;

            float ratio = (float)group.pixelCount / remainingPixels;
            int bucketsForGroup = Mathf.Max(1, Mathf.RoundToInt(ratio * remainingBuckets));
            bucketsForGroup = Mathf.Min(bucketsForGroup, remainingBuckets);

            // Tính capacity đều cho mỗi bucket
            int baseCapacity = group.pixelCount / bucketsForGroup;
            int extraPixels = group.pixelCount % bucketsForGroup;

            int actualBucketsCreated = 0;
            for (int i = 0; i < bucketsForGroup; i++)
            {
                // Phân bổ pixel dư cho các bucket đầu tiên
                int capacity = baseCapacity + (i < extraPixels ? 1 : 0);

                if (capacity > 0)
                {
                    previewBuckets.Add(new BucketDataSO(group.representativeColor, capacity));
                    actualBucketsCreated++;
                }
            }

            remainingBuckets -= actualBucketsCreated;
            remainingPixels -= group.pixelCount;
        }

        // Verify total capacity matches total pixels
        int totalCapacity = previewBuckets.Sum(b => b.capacity);
        if (totalCapacity != totalPixels)
        {
            Debug.LogWarning($"[ColorAnalysis] Capacity mismatch! Total: {totalPixels}, Buckets capacity: {totalCapacity}, Diff: {totalPixels - totalCapacity}");
        }
    }

    private void SaveToExistingData()
    {
        if (existingData == null) return;

        // Tính tổng từ buckets (source of truth)
        int totalFromBuckets = previewBuckets.Sum(b => b.capacity);
        int totalFromGroups = previewGroups.Sum(g => g.pixelCount);

        if (totalFromBuckets != totalFromGroups)
        {
            Debug.LogWarning($"[ColorAnalysis] Mismatch detected! Buckets: {totalFromBuckets}, Groups: {totalFromGroups}. Using bucket total.");
        }

        // Cập nhật dữ liệu vào existingData
        existingData.sourceTexture = sourceTexture;
        existingData.sourceImageName = sourceTexture != null ? sourceTexture.name : "Unknown";
        existingData.sourceWidth = sourceTexture != null ? sourceTexture.width : 0;
        existingData.sourceHeight = sourceTexture != null ? sourceTexture.height : 0;
        existingData.totalPixels = totalFromBuckets; // Sử dụng tổng từ buckets
        
        existingData.maxColorGroups = maxColorGroups;
        existingData.colorThreshold = colorThreshold;
        existingData.maxTotalBuckets = maxTotalBuckets;
        
        existingData.colorGroups = new List<ColorGroupData>(previewGroups);
        existingData.buckets = new List<BucketDataSO>(previewBuckets);
        
        existingData.analyzedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Đánh dấu dirty và lưu
        EditorUtility.SetDirty(existingData);
        AssetDatabase.SaveAssets();
        
        EditorGUIUtility.PingObject(existingData);
        Debug.Log($"[ColorAnalysis] Updated existing data: {AssetDatabase.GetAssetPath(existingData)}");
        Debug.Log($"[ColorAnalysis] Total pixels saved: {totalFromBuckets}, Buckets: {previewBuckets.Count}");
    }

    private void SaveToNewScriptableObject()
    {
        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save Color Analysis Data",
            sourceTexture != null ? $"{sourceTexture.name}_ColorData" : "ColorAnalysisData",
            "asset",
            "Choose where to save the color analysis data"
        );

        if (string.IsNullOrEmpty(savePath)) return;

        var data = ScriptableObject.CreateInstance<ColorAnalysisData>();
        
        data.sourceTexture = sourceTexture;
        data.sourceImageName = sourceTexture != null ? sourceTexture.name : "Unknown";
        data.sourceWidth = sourceTexture != null ? sourceTexture.width : 0;
        data.sourceHeight = sourceTexture != null ? sourceTexture.height : 0;
        data.totalPixels = previewGroups.Sum(g => g.pixelCount);
        
        data.maxColorGroups = maxColorGroups;
        data.colorThreshold = colorThreshold;
        data.maxTotalBuckets = maxTotalBuckets;
        
        data.colorGroups = new List<ColorGroupData>(previewGroups);
        data.buckets = new List<BucketDataSO>(previewBuckets);
        
        data.analyzedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        AssetDatabase.CreateAsset(data, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        existingData = data;
        EditorGUIUtility.PingObject(data);

        Debug.Log($"Color Analysis Data saved to: {savePath}");
    }

    private class TempColorGroup
    {
        public Color32 representativeColor;
        public int pixelCount;
        private int totalR, totalG, totalB;

        public TempColorGroup(Color32 color)
        {
            representativeColor = color;
            pixelCount = 1;
            totalR = color.r;
            totalG = color.g;
            totalB = color.b;
        }

        // Constructor cho auto-threshold mode (không tính pixel đầu tiên)
        public TempColorGroup(Color32 color, bool emptyInit)
        {
            representativeColor = color;
            pixelCount = 0;
            totalR = 0;
            totalG = 0;
            totalB = 0;
        }

        public void AddColor(Color32 color)
        {
            pixelCount++;
            totalR += color.r;
            totalG += color.g;
            totalB += color.b;
            UpdateRepresentativeColor();
        }

        public void Merge(TempColorGroup other)
        {
            pixelCount += other.pixelCount;
            totalR += other.totalR;
            totalG += other.totalG;
            totalB += other.totalB;
            UpdateRepresentativeColor();
        }

        private void UpdateRepresentativeColor()
        {
            if (pixelCount == 0) return;
            
            representativeColor = new Color32(
                (byte)(totalR / pixelCount),
                (byte)(totalG / pixelCount),
                (byte)(totalB / pixelCount),
                255
            );
        }
    }
}
