using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class BucketManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ColorQuantizer colorQuantizer;
    [SerializeField] private BucketTray bucketTray;
    [SerializeField] private ConveyorBelt conveyor;
    [SerializeField] private SandSimulation sandSimulation;
    [SerializeField] private SandArtGenerator sandArtGenerator;

    [Header("Settings")]
    [SerializeField] private bool autoInitialize = true;
    [SerializeField] private KeyCode initializeKey = KeyCode.Return;

    [Header("Events")]
    public UnityEvent OnGameStart;
    public UnityEvent OnAllSandAbsorbed;
    public UnityEvent<int, int> OnProgressUpdate;

    private bool isGameActive = false;
    private int totalSandPixels = 0;
    private int absorbedPixels = 0;

    public bool IsGameActive => isGameActive;
    public float Progress => totalSandPixels > 0 ? (float)absorbedPixels / totalSandPixels : 0f;

    void Start()
    {
        FindReferences();
    }

    void Update()
    {
        if (Input.GetKeyDown(initializeKey) && !isGameActive)
        {
            InitializeGame();
        }

        if (isGameActive)
        {
            CheckGameProgress();
        }
    }

    private void FindReferences()
    {
        if (colorQuantizer == null)
            colorQuantizer = FindObjectOfType<ColorQuantizer>();
        
        if (bucketTray == null)
            bucketTray = FindObjectOfType<BucketTray>();
        
        if (conveyor == null)
            conveyor = FindObjectOfType<ConveyorBelt>();
        
        if (sandSimulation == null)
            sandSimulation = FindObjectOfType<SandSimulation>();

        if (sandArtGenerator == null)
            sandArtGenerator = FindObjectOfType<SandArtGenerator>();
    }

    public void InitializeGame()
    {
        if (sandArtGenerator != null && sandArtGenerator.sourceImage != null)
        {
            colorQuantizer.AnalyzeTexture(sandArtGenerator.sourceImage);
        }
        else if (sandSimulation != null)
        {
            colorQuantizer.AnalyzeFromSandSimulation(sandSimulation);
        }
        else
        {
            Debug.LogError("BucketManager: No source to analyze!");
            return;
        }

        totalSandPixels = CalculateTotalSandPixels();
        absorbedPixels = 0;

        bucketTray.GenerateBuckets();

        isGameActive = true;
        OnGameStart?.Invoke();

        Debug.Log($"BucketManager: Game started! Total sand: {totalSandPixels}, Buckets: {bucketTray.TotalBuckets}");
    }

    private int CalculateTotalSandPixels()
    {
        int total = 0;
        foreach (var group in colorQuantizer.ColorGroups)
        {
            total += group.pixelCount;
        }
        return total;
    }

    public void OnSandAbsorbed(int amount)
    {
        absorbedPixels += amount;
        OnProgressUpdate?.Invoke(absorbedPixels, totalSandPixels);

        if (absorbedPixels >= totalSandPixels)
        {
            OnGameComplete();
        }
    }

    private void CheckGameProgress()
    {
        if (bucketTray.AllBucketsCompleted())
        {
            int remainingSand = CountRemainingSand();
            if (remainingSand == 0)
            {
                OnGameComplete();
            }
        }
    }

    private int CountRemainingSand()
    {
        if (sandSimulation == null) return 0;

        var map = sandSimulation.GetCurrentWriteMap();
        int count = 0;

        for (int i = 0; i < map.Length; i++)
        {
            if (map[i].type == 1)
            {
                count++;
            }
        }

        return count;
    }

    private void OnGameComplete()
    {
        isGameActive = false;
        OnAllSandAbsorbed?.Invoke();
        Debug.Log("BucketManager: All sand absorbed! Game Complete!");
    }

    public void ResetGame()
    {
        isGameActive = false;
        absorbedPixels = 0;
        totalSandPixels = 0;
        bucketTray.ClearAllBuckets();
    }

    public BucketStats GetStats()
    {
        return new BucketStats
        {
            totalBuckets = bucketTray.TotalBuckets,
            activeBuckets = bucketTray.ActiveBuckets,
            completedBuckets = bucketTray.GetCompletedCount(),
            totalSand = totalSandPixels,
            absorbedSand = absorbedPixels,
            remainingSand = CountRemainingSand()
        };
    }
}

[System.Serializable]
public struct BucketStats
{
    public int totalBuckets;
    public int activeBuckets;
    public int completedBuckets;
    public int totalSand;
    public int absorbedSand;
    public int remainingSand;
}
