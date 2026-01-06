using UnityEngine;
using Unity.Collections;

public class SandArtGenerator : MonoBehaviour
{
    [Header("Image Source")]
    public Texture2D sourceImage;
    public KeyCode spawnKey = KeyCode.Space;
    
    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    public float imageScale = 0.9f;

    private SandSimulation simulation;

    void Start()
    {
        simulation = GetComponent<SandSimulation>();
        if (simulation == null)
        {
            Debug.LogError("SandArtGenerator requires SandSimulation component!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(spawnKey) && sourceImage != null && simulation != null)
        {
            SpawnSandArt();
        }
    }

    void SpawnSandArt()
    {
        int imgWidth = sourceImage.width;
        int imgHeight = sourceImage.height;

        // Tính toán scale để ảnh vừa với simulation
        float maxWidth = simulation.Width * imageScale;
        float maxHeight = simulation.Height * imageScale;
        
        float scaleX = maxWidth / imgWidth;
        float scaleY = maxHeight / imgHeight;
        float scale = Mathf.Min(scaleX, scaleY);

        int scaledWidth = Mathf.RoundToInt(imgWidth * scale);
        int scaledHeight = Mathf.RoundToInt(imgHeight * scale);

        scaledWidth = Mathf.Min(scaledWidth, simulation.Width);
        scaledHeight = Mathf.Min(scaledHeight, simulation.Height);

        // Căn giữa
        int startX = (simulation.Width - scaledWidth) / 2;
        int startY = (simulation.Height - scaledHeight) / 2;

        NativeArray<Cell> targetMap = simulation.GetCurrentWriteMap();

        // Spawn cát từ ảnh
        for (int y = 0; y < scaledHeight; y++)
        {
            for (int x = 0; x < scaledWidth; x++)
            {
                float srcX = x / scale;
                float srcY = y / scale;
                
                Color pixelColor = sourceImage.GetPixelBilinear(srcX / imgWidth, srcY / imgHeight);

                if (pixelColor.a > 0.1f)
                {
                    int px = startX + x;
                    int py = startY + y;

                    if (px >= 0 && px < simulation.Width && py >= 0 && py < simulation.Height)
                    {
                        int idx = py * simulation.Width + px;
                        
                        Color32 sandColor = pixelColor;
                        targetMap[idx] = new Cell 
                        { 
                            type = 1,
                            color = sandColor
                        };
                    }
                }
            }
        }

        Debug.Log($"Spawned sand art: {sourceImage.name} ({scaledWidth}x{scaledHeight}) at ({startX},{startY})");
    }
}
