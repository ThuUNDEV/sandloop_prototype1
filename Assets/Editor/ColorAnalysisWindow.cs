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
        
        maxColorGroups = EditorGUILayout.IntSlider("Max Color Groups", maxColorGroups, 4, 20);
        colorThreshold = EditorGUILayout.Slider("Color Threshold", colorThreshold, 10f, 100f);
        maxTotalBuckets = EditorGUILayout.IntSlider("Max Total Buckets", maxTotalBuckets, 4, 32);
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = sourceTexture != null;
        if (GUILayout.Button("Analyze", GUILayout.Height(30)))
        {
            AnalyzeTexture();
        }

        GUI.enabled = hasAnalyzed && previewBuckets.Count > 0;
        if (GUILayout.Button("Save to ScriptableObject", GUILayout.Height(30)))
        {
            SaveToScriptableObject();
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

        // Analyze
        var colorGroups = new List<TempColorGroup>();
        Color32[] pixels = sourceTexture.GetPixels32();

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

        // Sort by pixel count
        colorGroups = colorGroups.OrderByDescending(g => g.pixelCount).ToList();

        // Convert to preview data
        previewGroups.Clear();
        foreach (var group in colorGroups)
        {
            previewGroups.Add(new ColorGroupData(group.representativeColor, group.pixelCount));
        }

        // Generate buckets
        GenerateBuckets(colorGroups);

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

    private void AddColorToGroups(Color32 color, List<TempColorGroup> groups)
    {
        foreach (var group in groups)
        {
            if (IsColorSimilar(color, group.representativeColor, colorThreshold))
            {
                group.AddColor(color);
                return;
            }
        }
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

            int capacityPerBucket = Mathf.CeilToInt((float)group.pixelCount / bucketsForGroup);

            for (int i = 0; i < bucketsForGroup; i++)
            {
                int remaining = group.pixelCount - (i * capacityPerBucket);
                int capacity = Mathf.Min(capacityPerBucket, remaining);

                if (capacity > 0)
                {
                    previewBuckets.Add(new BucketDataSO(group.representativeColor, capacity));
                }
            }

            remainingBuckets -= bucketsForGroup;
            remainingPixels -= group.pixelCount;
        }
    }

    private void SaveToScriptableObject()
    {
        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save Color Analysis Data",
            sourceTexture != null ? $"{sourceTexture.name}_ColorData" : "ColorAnalysisData",
            "asset",
            "Choose where to save the color analysis data"
        );

        if (string.IsNullOrEmpty(savePath)) return;

        var data = ScriptableObject.CreateInstance<ColorAnalysisData>();
        
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
            representativeColor = new Color32(
                (byte)(totalR / pixelCount),
                (byte)(totalG / pixelCount),
                (byte)(totalB / pixelCount),
                255
            );
        }
    }
}
