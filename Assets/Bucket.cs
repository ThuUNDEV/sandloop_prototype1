using UnityEngine;
using UnityEngine.Events;

public enum BucketState
{
    InTray,
    OnConveyor,
    Completed
}

public class Bucket : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BucketData data;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer bucketRenderer;
    [SerializeField] private SpriteRenderer fillIndicator;
    [SerializeField] private Transform fillMask;

    [Header("Settings")]
    [SerializeField] private float absorptionRadius = 0.5f;
    [SerializeField] private int absorptionRate = 1;

    [Header("References")]
    [SerializeField] private ConveyorBelt conveyor;
    [SerializeField] private SandSimulation sandSimulation;
    [SerializeField] private SandAbsorber sandAbsorber;

    [Header("Events")]
    public UnityEvent OnBucketFull;
    public UnityEvent OnBucketClicked;

    private BucketState currentState = BucketState.InTray;

    public BucketData Data => data;
    public BucketState CurrentState => currentState;

    void Start()
    {
        if (bucketRenderer == null)
        {
            bucketRenderer = GetComponent<SpriteRenderer>();
        }

        if (conveyor != null && sandSimulation == null)
        {
            sandSimulation = conveyor.SandSimulation;
        }

        UpdateVisual();
    }

    void Update()
    {
        if (currentState == BucketState.OnConveyor)
        {
            TryAbsorbSand();
            UpdateFillIndicator();
        }
    }

    public void Initialize(BucketData bucketData, ConveyorBelt conveyorBelt, SandSimulation simulation, SandAbsorber absorber)
    {
        data = bucketData;
        conveyor = conveyorBelt;
        sandSimulation = simulation;
        sandAbsorber = absorber;

        UpdateVisual();
    }

    public void SetState(BucketState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case BucketState.InTray:
                break;
            case BucketState.OnConveyor:
                break;
            case BucketState.Completed:
                OnCompleted();
                break;
        }
    }

    private void UpdateVisual()
    {
        if (bucketRenderer != null && data != null)
        {
            bucketRenderer.color = data.bucketColor;
        }
    }

    private void UpdateFillIndicator()
    {
        if (fillIndicator == null || data == null) return;

        float fillPercent = data.FillPercent;
        
        if (fillMask != null)
        {
            fillMask.localScale = new Vector3(1f, fillPercent, 1f);
        }
        else
        {
            Color c = fillIndicator.color;
            c.a = fillPercent;
            fillIndicator.color = c;
        }
    }

    private void TryAbsorbSand()
    {
        if (data == null || data.IsFull) return;

        // Use SandAbsorber if available (preferred method)
        if (sandAbsorber != null)
        {
            int absorbed = sandAbsorber.AbsorbSandForBucket(this);
            if (absorbed > 0)
            {
                UpdateFillIndicator();
                if (data.IsFull)
                {
                    OnBucketFull?.Invoke();
                }
            }
            return;
        }

        // Fallback to legacy method
        if (sandSimulation == null) return;

        Vector3 worldPos = transform.position;
        
        int simX = WorldToSimX(worldPos.x);
        int simY = WorldToSimY(worldPos.y);
        
        int radiusInPixels = Mathf.CeilToInt(absorptionRadius * sandSimulation.Width / 10f);

        var map = sandSimulation.GetCurrentWriteMap();

        int absorbed2 = 0;
        for (int dy = -radiusInPixels; dy <= radiusInPixels && absorbed2 < absorptionRate; dy++)
        {
            for (int dx = -radiusInPixels; dx <= radiusInPixels && absorbed2 < absorptionRate; dx++)
            {
                int px = simX + dx;
                int py = simY + dy;

                if (px < 0 || px >= sandSimulation.Width || py < 0 || py >= sandSimulation.Height)
                    continue;

                int idx = py * sandSimulation.Width + px;
                var cell = map[idx];

                if (cell.type == 1)
                {
                    if (sandSimulation != null && !sandSimulation.IsColorInGroup(cell.color, data.bucketColor))
                        continue;

                    map[idx] = new Cell { type = 0, color = new Color32(0, 0, 0, 0), hasMoved = false };
                    
                    if (data.TryAbsorb(1))
                    {
                        absorbed2++;
                        
                        if (data.IsFull)
                        {
                            OnBucketFull?.Invoke();
                            return;
                        }
                    }
                }
            }
        }
    }

    private int WorldToSimX(float worldX)
    {
        return Mathf.Clamp(Mathf.RoundToInt((worldX + 5f) / 10f * sandSimulation.Width), 0, sandSimulation.Width - 1);
    }

    private int WorldToSimY(float worldY)
    {
        return Mathf.Clamp(Mathf.RoundToInt((worldY + 5f) / 10f * sandSimulation.Height), 0, sandSimulation.Height - 1);
    }

    public void OnCompleted()
    {
        currentState = BucketState.Completed;
        gameObject.SetActive(false);
    }

    void OnMouseDown()
    {
        if (currentState == BucketState.InTray)
        {
            OnBucketClicked?.Invoke();
            MoveToConveyor();
        }
    }

    public void MoveToConveyor()
    {
        if (conveyor != null && currentState == BucketState.InTray)
        {
            conveyor.AddBucket(this);
        }
    }

    public void ResetBucket()
    {
        if (data != null)
        {
            data.Reset();
        }
        currentState = BucketState.InTray;
        UpdateFillIndicator();
        gameObject.SetActive(true);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, absorptionRadius);
    }
}
