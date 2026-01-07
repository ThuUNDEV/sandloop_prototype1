using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BucketData
{
    public Color32 bucketColor;
    public int capacity;
    public int currentFill;
    
    public bool IsFull => currentFill >= capacity;
    public float FillPercent => capacity > 0 ? (float)currentFill / capacity : 0f;

    public BucketData(Color32 color, int capacity)
    {
        this.bucketColor = color;
        this.capacity = capacity;
        this.currentFill = 0;
    }

    public bool TryAbsorb(int amount = 1)
    {
        if (IsFull) return false;
        
        int canAbsorb = Mathf.Min(amount, capacity - currentFill);
        currentFill += canAbsorb;
        return true;
    }

    public void Reset()
    {
        currentFill = 0;
    }
}

[System.Serializable]
public class ColorGroup
{
    public Color32 representativeColor;
    public List<Color32> colors;
    public int pixelCount;

    public ColorGroup(Color32 color)
    {
        representativeColor = color;
        colors = new List<Color32> { color };
        pixelCount = 1;
    }

    public ColorGroup(Color32 color, int count)
    {
        representativeColor = color;
        colors = new List<Color32> { color };
        pixelCount = count;
    }

    public void AddColor(Color32 color)
    {
        colors.Add(color);
        pixelCount++;
        UpdateRepresentativeColor();
    }

    private void UpdateRepresentativeColor()
    {
        int r = 0, g = 0, b = 0;
        foreach (var c in colors)
        {
            r += c.r;
            g += c.g;
            b += c.b;
        }
        int count = colors.Count;
        representativeColor = new Color32(
            (byte)(r / count),
            (byte)(g / count),
            (byte)(b / count),
            255
        );
    }

    public bool IsColorSimilar(Color32 color, float threshold)
    {
        float distance = ColorDistance(representativeColor, color);
        return distance <= threshold;
    }

    public static float ColorDistance(Color32 a, Color32 b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }
}
