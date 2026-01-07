using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public enum GameState
{
    Idle,
    Initializing,
    Playing,
    Paused,
    Completed
}

public class ConveyorGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SandSimulation sandSimulation;
    [SerializeField] private SandArtGenerator sandArtGenerator;
    [SerializeField] private ColorQuantizer colorQuantizer;
    [SerializeField] private ConveyorBelt conveyor;
    [SerializeField] private BucketTray bucketTray;
    [SerializeField] private BucketManager bucketManager;
    [SerializeField] private SandAbsorber sandAbsorber;

    [Header("Game Settings")]
    [SerializeField] private bool autoStartOnSpawn = true;
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private KeyCode startKey = KeyCode.Return;
    [SerializeField] private KeyCode pauseKey = KeyCode.P;
    [SerializeField] private KeyCode resetKey = KeyCode.R;

    [Header("UI References (Optional)")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject completePanel;

    [Header("Events")]
    public UnityEvent OnGameInitialized;
    public UnityEvent OnGameStarted;
    public UnityEvent OnGamePaused;
    public UnityEvent OnGameResumed;
    public UnityEvent OnGameCompleted;
    public UnityEvent OnGameReset;

    private GameState currentState = GameState.Idle;
    private bool sandArtSpawned = false;
    private bool referencesInitialized = false;

    public GameState CurrentState => currentState;
    public bool IsPlaying => currentState == GameState.Playing;

    void Start()
    {
        CacheAllReferences();
        SetupEventListeners();
        UpdateUIState();
    }

    void Update()
    {
        HandleInput();
        CheckSandArtSpawn();
    }

    private void CacheAllReferences()
    {
        if (referencesInitialized) return;

        // Sử dụng ServiceLocator thay vì FindObjectOfType nhiều lần
        var locator = GameServiceLocator.Instance;
        
        if (sandSimulation == null) sandSimulation = locator.SandSimulation;
        if (sandArtGenerator == null) sandArtGenerator = locator.SandArtGenerator;
        if (colorQuantizer == null) colorQuantizer = locator.ColorQuantizer;
        if (conveyor == null) conveyor = locator.Conveyor;
        if (bucketTray == null) bucketTray = locator.BucketTray;
        if (bucketManager == null) bucketManager = locator.BucketManager;
        if (sandAbsorber == null) sandAbsorber = locator.SandAbsorber;

        // Pass references to dependent components
        if (bucketTray != null)
        {
            bucketTray.SetReferences(conveyor, colorQuantizer, sandAbsorber);
        }

        referencesInitialized = true;
    }

    private void SetupEventListeners()
    {
        if (bucketManager != null)
        {
            bucketManager.OnAllSandAbsorbed.AddListener(OnAllSandAbsorbedHandler);
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(startKey))
        {
            if (currentState == GameState.Idle && sandArtSpawned)
            {
                StartGame();
            }
        }

        if (Input.GetKeyDown(pauseKey))
        {
            if (currentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
        }

        if (Input.GetKeyDown(resetKey))
        {
            ResetGame();
        }
    }

    private float lastSandCheckTime = 0f;
    private const float SAND_CHECK_INTERVAL = 0.2f; // Check mỗi 0.2 giây thay vì mỗi frame

    private void CheckSandArtSpawn()
    {
        if (sandArtSpawned || sandSimulation == null) return;

        // Throttle: chỉ check định kỳ thay vì mỗi frame
        if (Time.time - lastSandCheckTime < SAND_CHECK_INTERVAL) return;
        lastSandCheckTime = Time.time;

        // Quick check: chỉ cần tìm 1 pixel sand thay vì đếm tất cả
        if (HasAnySand())
        {
            sandArtSpawned = true;
            OnSandArtDetected();
        }
    }

    private bool HasAnySand()
    {
        if (sandSimulation == null) return false;
        
        var map = sandSimulation.GetCurrentWriteMap();
        // Chỉ cần tìm 1 pixel sand là đủ
        for (int i = 0; i < map.Length; i++)
        {
            if (map[i].type == 1) return true;
        }
        return false;
    }

    private void OnSandArtDetected()
    {
        Debug.Log("ConveyorGameManager: Sand art detected!");

        if (autoStartOnSpawn)
        {
            StartCoroutine(DelayedStart());
        }
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(startDelay);
        StartGame();
    }

    public void StartGame()
    {
        if (currentState != GameState.Idle) return;

        currentState = GameState.Initializing;
        Debug.Log("ConveyorGameManager: Initializing game...");

        // Analyze colors - skip if already has precomputed data
        if (colorQuantizer.HasPrecomputedData && colorQuantizer.BucketDataList.Count > 0)
        {
            // Already loaded from precomputed data
        }
        else if (sandArtGenerator != null && sandArtGenerator.sourceImage != null)
        {
            colorQuantizer.AnalyzeTexture(sandArtGenerator.sourceImage);
        }
        else
        {
            colorQuantizer.AnalyzeFromSandSimulation(sandSimulation);
        }

        // Generate buckets
        bucketTray.GenerateBuckets();

        currentState = GameState.Playing;
        
        OnGameInitialized?.Invoke();
        OnGameStarted?.Invoke();
        
        UpdateUIState();
        Debug.Log("ConveyorGameManager: Game started!");
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Paused;
        Time.timeScale = 0f;
        
        OnGamePaused?.Invoke();
        UpdateUIState();
        Debug.Log("ConveyorGameManager: Game paused");
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        currentState = GameState.Playing;
        Time.timeScale = 1f;
        
        OnGameResumed?.Invoke();
        UpdateUIState();
        Debug.Log("ConveyorGameManager: Game resumed");
    }

    public void ResetGame()
    {
        Time.timeScale = 1f;
        
        currentState = GameState.Idle;
        sandArtSpawned = false;

        // Clear buckets
        if (bucketTray != null)
        {
            bucketTray.ClearAllBuckets();
        }

        // Clear sand simulation
        if (sandSimulation != null)
        {
            ClearSandSimulation();
        }

        OnGameReset?.Invoke();
        UpdateUIState();
        Debug.Log("ConveyorGameManager: Game reset");
    }

    private void ClearSandSimulation()
    {
        var map = sandSimulation.GetCurrentWriteMap();
        for (int i = 0; i < map.Length; i++)
        {
            map[i] = new Cell { type = 0, color = new Color32(0, 0, 0, 0), hasMoved = false };
        }
    }

    private void OnAllSandAbsorbedHandler()
    {
        CompleteGame();
    }

    private void CompleteGame()
    {
        currentState = GameState.Completed;
        
        OnGameCompleted?.Invoke();
        UpdateUIState();
        Debug.Log("ConveyorGameManager: Game completed! All sand absorbed!");
    }

    private int CountSandPixels()
    {
        if (sandSimulation == null) return 0;

        var map = sandSimulation.GetCurrentWriteMap();
        int count = 0;

        for (int i = 0; i < map.Length; i++)
        {
            if (map[i].type == 1) count++;
        }

        return count;
    }

    private void UpdateUIState()
    {
        if (startPanel != null)
            startPanel.SetActive(currentState == GameState.Idle);

        if (gamePanel != null)
            gamePanel.SetActive(currentState == GameState.Playing || currentState == GameState.Paused);

        if (completePanel != null)
            completePanel.SetActive(currentState == GameState.Completed);
    }

    public GameStats GetGameStats()
    {
        var stats = new GameStats();
        stats.state = currentState;
        stats.totalSand = CountSandPixels();
        
        if (bucketTray != null)
        {
            stats.totalBuckets = bucketTray.TotalBuckets;
            stats.activeBuckets = bucketTray.ActiveBuckets;
            stats.completedBuckets = bucketTray.GetCompletedCount();
        }

        if (colorQuantizer != null)
        {
            stats.colorGroups = colorQuantizer.ColorGroups.Count;
        }

        return stats;
    }

    [ContextMenu("Debug: Print Stats")]
    public void PrintStats()
    {
        var stats = GetGameStats();
        Debug.Log($"=== Game Stats ===");
        Debug.Log($"State: {stats.state}");
        Debug.Log($"Sand remaining: {stats.totalSand}");
        Debug.Log($"Buckets: {stats.completedBuckets}/{stats.totalBuckets} completed");
        Debug.Log($"Color groups: {stats.colorGroups}");
    }
}

[System.Serializable]
public struct GameStats
{
    public GameState state;
    public int totalSand;
    public int totalBuckets;
    public int activeBuckets;
    public int completedBuckets;
    public int colorGroups;
}
