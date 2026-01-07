using UnityEngine;

/// <summary>
/// Singleton service locator để cache references một lần, tránh FindObjectOfType nhiều lần
/// </summary>
public class GameServiceLocator : MonoBehaviour
{
    private static GameServiceLocator _instance;
    public static GameServiceLocator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameServiceLocator>();
                if (_instance == null)
                {
                    var go = new GameObject("GameServiceLocator");
                    _instance = go.AddComponent<GameServiceLocator>();
                }
            }
            return _instance;
        }
    }

    [Header("Core References")]
    [SerializeField] private SandSimulation _sandSimulation;
    [SerializeField] private ConveyorBelt _conveyor;
    [SerializeField] private BucketTray _bucketTray;
    [SerializeField] private BucketManager _bucketManager;
    [SerializeField] private SandAbsorber _sandAbsorber;
    [SerializeField] private ConveyorGameManager _gameManager;

    private bool _initialized = false;

    // Public accessors - lazy initialization
    public SandSimulation SandSimulation => GetOrFind(ref _sandSimulation);
    public ConveyorBelt Conveyor => GetOrFind(ref _conveyor);
    public BucketTray BucketTray => GetOrFind(ref _bucketTray);
    public BucketManager BucketManager => GetOrFind(ref _bucketManager);
    public SandAbsorber SandAbsorber => GetOrFind(ref _sandAbsorber);
    public ConveyorGameManager GameManager => GetOrFind(ref _gameManager);

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        // Cache tất cả references một lần trong Awake
        InitializeAll();
    }

    private void InitializeAll()
    {
        if (_initialized) return;

        if (_sandSimulation == null) _sandSimulation = FindObjectOfType<SandSimulation>();
        if (_conveyor == null) _conveyor = FindObjectOfType<ConveyorBelt>();
        if (_bucketTray == null) _bucketTray = FindObjectOfType<BucketTray>();
        if (_bucketManager == null) _bucketManager = FindObjectOfType<BucketManager>();
        if (_sandAbsorber == null) _sandAbsorber = FindObjectOfType<SandAbsorber>();
        if (_gameManager == null) _gameManager = FindObjectOfType<ConveyorGameManager>();

        _initialized = true;
    }

    private T GetOrFind<T>(ref T cached) where T : Component
    {
        if (cached == null)
        {
            cached = FindObjectOfType<T>();
        }
        return cached;
    }

    /// <summary>
    /// Force reinitialize all references
    /// </summary>
    public void Reinitialize()
    {
        _initialized = false;
        InitializeAll();
    }
}
