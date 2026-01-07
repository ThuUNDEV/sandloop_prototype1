using UnityEngine;
using Unity.Collections;

public class SandArtGenerator : MonoBehaviour
{
    [Header("Image Source")]
    public Texture2D sourceImage;
    
    [Header("Spawn Settings")]
    public bool autoSpawnOnStart = true;
    public float autoSpawnDelay = 0.1f;

    private SandSimulation simulation;
    private bool hasSpawned = false;

    void Start()
    {
        simulation = GetComponent<SandSimulation>();
        if (simulation == null)
        {
            Debug.LogError("SandArtGenerator requires SandSimulation component!");
            return;
        }

        if (autoSpawnOnStart && sourceImage != null)
        {
            if (autoSpawnDelay > 0)
            {
                Invoke(nameof(AutoSpawn), autoSpawnDelay);
            }
            else
            {
                AutoSpawn();
            }
        }
    }

    private void AutoSpawn()
    {
        if (!hasSpawned && sourceImage != null && simulation != null)
        {
            SpawnSandArt();
        }
    }

    public void SpawnSandArt()
    {
        if (sourceImage == null || simulation == null) return;

        hasSpawned = true;
        int imgWidth = sourceImage.width;
        int imgHeight = sourceImage.height;

        // Scale để ảnh vừa với simulation
        float scaleX = (float)simulation.Width / imgWidth;
        float scaleY = (float)simulation.Height / imgHeight;
        float scale = Mathf.Min(scaleX, scaleY);

        int scaledWidth = Mathf.RoundToInt(imgWidth * scale);
        int scaledHeight = Mathf.RoundToInt(imgHeight * scale);

        scaledWidth = Mathf.Min(scaledWidth, simulation.Width);
        scaledHeight = Mathf.Min(scaledHeight, simulation.Height);

        // Căn giữa
        int startX = (simulation.Width - scaledWidth) / 2;
        int startY = (simulation.Height - scaledHeight) / 2;

        // Cache tất cả pixels một lần - tối ưu hiệu năng
        Color32[] sourcePixels = sourceImage.GetPixels32();
        
        NativeArray<Cell> targetMap = simulation.GetCurrentWriteMap();
        int simWidth = simulation.Width;

        // Pre-calculate inverse scale
        float invScale = 1f / scale;

        // Spawn cát từ ảnh
        for (int y = 0; y < scaledHeight; y++)
        {
            int py = startY + y;
            if (py < 0 || py >= simulation.Height) continue;
            
            int srcY = Mathf.Min((int)(y * invScale), imgHeight - 1);
            int rowOffset = py * simWidth;
            int srcRowOffset = srcY * imgWidth;

            for (int x = 0; x < scaledWidth; x++)
            {
                int px = startX + x;
                if (px < 0 || px >= simWidth) continue;

                int srcX = Mathf.Min((int)(x * invScale), imgWidth - 1);
                Color32 pixelColor = sourcePixels[srcRowOffset + srcX];

                if (pixelColor.a > 25)
                {
                    int idx = rowOffset + px;
                    targetMap[idx] = new Cell 
                    { 
                        type = 1,
                        color = pixelColor
                    };
                }
            }
        }

        Debug.Log($"Spawned sand art: {sourceImage.name} ({scaledWidth}x{scaledHeight}) at ({startX},{startY})");
    }

    public void ResetSpawn()
    {
        hasSpawned = false;
    }
}
