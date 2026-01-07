using UnityEngine;
using System.Collections.Generic;

public class BucketTray : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int columns = 4;
    [SerializeField] private int rows = 4;
    [SerializeField] private float slotSpacingX = 1.2f;
    [SerializeField] private float slotSpacingY = 1.2f;

    [Header("Prefabs")]
    [SerializeField] private GameObject bucketPrefab;

    [Header("References")]
    [SerializeField] private ConveyorBelt conveyor;
    [SerializeField] private SandSimulation sandSimulation;
    [SerializeField] private SandAbsorber sandAbsorber;
    [SerializeField] private Transform slotsContainer;

    private List<Transform> slots = new List<Transform>();
    private List<Bucket> allBuckets = new List<Bucket>();
    private Queue<Bucket> waitingBuckets = new Queue<Bucket>();

    public List<Bucket> AllBuckets => allBuckets;
    public int TotalBuckets => allBuckets.Count;
    public int ActiveBuckets => allBuckets.FindAll(b => b.gameObject.activeInHierarchy).Count;
    public int SlotCount => columns * rows;
    public int Columns => columns;
    public int Rows => rows;

    private bool isInitialized = false;

    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;
        
        if (slotsContainer == null)
        {
            slotsContainer = transform;
        }

        CreateSlots();
        isInitialized = true;
    }

    public void SetReferences(ConveyorBelt conveyorRef, SandSimulation simulationRef, SandAbsorber absorberRef)
    {
        conveyor = conveyorRef;
        sandSimulation = simulationRef;
        sandAbsorber = absorberRef;
    }

    private void CreateSlots()
    {
        slots.Clear();

        // Clear existing slot objects
        foreach (Transform child in slotsContainer)
        {
            if (child.name.StartsWith("Slot_"))
            {
                Destroy(child.gameObject);
            }
        }

        // Calculate grid positions (centered)
        float totalWidth = (columns - 1) * slotSpacingX;
        float totalHeight = (rows - 1) * slotSpacingY;
        float startX = -totalWidth / 2f;
        float startY = totalHeight / 2f; // Start from top

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;
                var slot = new GameObject($"Slot_{index}");
                slot.transform.SetParent(slotsContainer);
                
                float x = startX + col * slotSpacingX;
                float y = startY - row * slotSpacingY; // Go down each row
                
                slot.transform.localPosition = new Vector3(x, y, 0f);
                slots.Add(slot.transform);
            }
        }
    }

    public void GenerateBuckets()
    {
        if (sandSimulation == null)
        {
            Debug.LogError("BucketTray: SandSimulation not assigned!");
            return;
        }

        if (bucketPrefab == null)
        {
            Debug.LogError("BucketTray: Bucket prefab not assigned!");
            return;
        }

        // Ensure initialized
        Initialize();

        ClearAllBuckets();

        var bucketDataList = sandSimulation.BucketDataList;

        // Write debug log to markdown file
        sandSimulation.AppendBucketDebugLog(bucketDataList);

        for (int i = 0; i < bucketDataList.Count; i++)
        {
            var bucketData = bucketDataList[i];
            var bucketObj = Instantiate(bucketPrefab, slotsContainer);
            var bucket = bucketObj.GetComponent<Bucket>();

            if (bucket != null)
            {
                bucket.Initialize(bucketData, conveyor, sandSimulation, sandAbsorber);
                allBuckets.Add(bucket);

                if (i < slots.Count)
                {
                    bucketObj.transform.position = slots[i].position;
                }
                else
                {
                    bucketObj.SetActive(false);
                    waitingBuckets.Enqueue(bucket);
                }
            }
        }

        Debug.Log($"BucketTray: Generated {allBuckets.Count} buckets, {waitingBuckets.Count} waiting");
    }

    public void OnBucketUsed(Bucket bucket)
    {
        int slotIndex = GetSlotIndex(bucket);
        
        if (slotIndex >= 0 && waitingBuckets.Count > 0)
        {
            var nextBucket = waitingBuckets.Dequeue();
            nextBucket.transform.position = slots[slotIndex].position;
            nextBucket.gameObject.SetActive(true);
            nextBucket.ResetBucket();
        }
    }

    private int GetSlotIndex(Bucket bucket)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (Vector3.Distance(bucket.transform.position, slots[i].position) < 0.1f)
            {
                return i;
            }
        }
        return -1;
    }

    public void ClearAllBuckets()
    {
        foreach (var bucket in allBuckets)
        {
            if (bucket != null)
            {
                Destroy(bucket.gameObject);
            }
        }
        allBuckets.Clear();
        waitingBuckets.Clear();
    }

    public Bucket GetBucketAtSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return null;

        foreach (var bucket in allBuckets)
        {
            if (bucket.gameObject.activeInHierarchy && 
                bucket.CurrentState == BucketState.InTray &&
                Vector3.Distance(bucket.transform.position, slots[index].position) < 0.1f)
            {
                return bucket;
            }
        }
        return null;
    }

    public void ClickSlot(int index)
    {
        var bucket = GetBucketAtSlot(index);
        if (bucket != null)
        {
            bucket.MoveToConveyor();
            OnBucketUsed(bucket);
        }
    }

    public int GetCompletedCount()
    {
        int count = 0;
        foreach (var bucket in allBuckets)
        {
            if (bucket.CurrentState == BucketState.Completed)
            {
                count++;
            }
        }
        return count;
    }

    public bool AllBucketsCompleted()
    {
        return GetCompletedCount() == allBuckets.Count && allBuckets.Count > 0;
    }

    void OnDrawGizmos()
    {
        int slotCount = columns * rows;

        if (slots == null || slots.Count == 0)
        {
            // Calculate grid positions (centered)
            float totalWidth = (columns - 1) * slotSpacingX;
            float totalHeight = (rows - 1) * slotSpacingY;
            float startX = -totalWidth / 2f;
            float startY = totalHeight / 2f;

            Gizmos.color = Color.yellow;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    float x = startX + col * slotSpacingX;
                    float y = startY - row * slotSpacingY;
                    Vector3 pos = transform.position + new Vector3(x, y, 0f);
                    Gizmos.DrawWireCube(pos, new Vector3(1f, 1f, 0.1f));
                }
            }
        }
        else
        {
            Gizmos.color = Color.green;
            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    Gizmos.DrawWireCube(slot.position, new Vector3(1f, 1f, 0.1f));
                }
            }
        }
    }

    public Bucket GetBucketAtPosition(int col, int row)
    {
        int index = row * columns + col;
        return GetBucketAtSlot(index);
    }

    public void ClickSlotAt(int col, int row)
    {
        int index = row * columns + col;
        ClickSlot(index);
    }
}
