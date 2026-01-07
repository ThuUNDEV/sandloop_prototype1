using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ColorAnalysisData", menuName = "Sand Simulation/Color Analysis Data")]
public class ColorAnalysisData : ScriptableObject
{
    [Header("Source Info")]
    public Texture2D sourceTexture;
    public string sourceImageName;
    public int sourceWidth;
    public int sourceHeight;
    public int totalPixels;

    [Header("Analysis Settings")]
    public int maxColorGroups = 12;
    public float colorThreshold = 30f;
    public int maxTotalBuckets = 16;

    [Header("Results")]
    public List<ColorGroupData> colorGroups = new List<ColorGroupData>();
    public List<BucketDataSO> buckets = new List<BucketDataSO>();

    [Header("Metadata")]
    public string analyzedDate;
}

[System.Serializable]
public class ColorGroupData
{
    public Color32 representativeColor;
    public int pixelCount;
    
    public ColorGroupData(Color32 color, int count)
    {
        representativeColor = color;
        pixelCount = count;
    }
}

[System.Serializable]
public class BucketDataSO
{
    public Color32 bucketColor;
    public int capacity;

    public BucketDataSO(Color32 color, int capacity)
    {
        this.bucketColor = color;
        this.capacity = capacity;
    }

    public BucketData ToBucketData()
    {
        return new BucketData(bucketColor, capacity);
    }
}
