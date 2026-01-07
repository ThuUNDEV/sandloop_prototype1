using UnityEngine;
using System.Collections.Generic;

public class ConveyorBelt : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer beltRenderer;
    [SerializeField] private float textureScrollSpeed = 1f;

    [Header("References")]
    [SerializeField] private SandSimulation sandSimulation;

    private List<Bucket> bucketsOnBelt = new List<Bucket>();
    private Material beltMaterial;
    private float textureOffset = 0f;
    private bool isInitialized = false;

    public Transform StartPoint => startPoint;
    public Transform EndPoint => endPoint;
    public float Speed => speed;
    public SandSimulation SandSimulation => sandSimulation;

    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        if (beltRenderer != null)
        {
            beltMaterial = beltRenderer.material;
        }

        ValidateSetup();
        isInitialized = true;
    }

    void Update()
    {
        MoveBuckets();
        AnimateBelt();
    }

    private void ValidateSetup()
    {
        if (startPoint == null || endPoint == null)
        {
            Debug.LogWarning("ConveyorBelt: Start/End points not set! Creating default points.");
            CreateDefaultPoints();
        }

        // Only use ServiceLocator as fallback if not assigned in Inspector
        if (sandSimulation == null)
        {
            sandSimulation = GameServiceLocator.Instance.SandSimulation;
        }
    }

    public void SetSandSimulation(SandSimulation simulation)
    {
        sandSimulation = simulation;
    }

    private void CreateDefaultPoints()
    {
        if (startPoint == null)
        {
            var startObj = new GameObject("ConveyorStart");
            startObj.transform.SetParent(transform);
            startObj.transform.localPosition = new Vector3(-5f, 0f, 0f);
            startPoint = startObj.transform;
        }

        if (endPoint == null)
        {
            var endObj = new GameObject("ConveyorEnd");
            endObj.transform.SetParent(transform);
            endObj.transform.localPosition = new Vector3(5f, 0f, 0f);
            endPoint = endObj.transform;
        }
    }

    private void MoveBuckets()
    {
        for (int i = bucketsOnBelt.Count - 1; i >= 0; i--)
        {
            var bucket = bucketsOnBelt[i];
            
            if (bucket == null || !bucket.gameObject.activeInHierarchy)
            {
                bucketsOnBelt.RemoveAt(i);
                continue;
            }

            Vector3 direction = (endPoint.position - startPoint.position).normalized;
            bucket.transform.position += direction * speed * Time.deltaTime;

            if (HasReachedEnd(bucket.transform.position))
            {
                HandleBucketAtEnd(bucket);
            }
        }
    }

    private bool HasReachedEnd(Vector3 position)
    {
        Vector3 toEnd = endPoint.position - startPoint.position;
        Vector3 toPos = position - startPoint.position;
        
        return Vector3.Dot(toPos, toEnd.normalized) >= toEnd.magnitude;
    }

    private void HandleBucketAtEnd(Bucket bucket)
    {
        if (bucket.Data.IsFull)
        {
            bucket.OnCompleted();
            bucketsOnBelt.Remove(bucket);
        }
        else
        {
            bucket.transform.position = startPoint.position;
        }
    }

    private void AnimateBelt()
    {
        if (beltMaterial == null) return;

        textureOffset += textureScrollSpeed * Time.deltaTime;
        beltMaterial.mainTextureOffset = new Vector2(textureOffset, 0f);
    }

    public void AddBucket(Bucket bucket)
    {
        if (bucket == null) return;
        
        if (!bucketsOnBelt.Contains(bucket))
        {
            bucket.transform.position = startPoint.position;
            bucket.SetState(BucketState.OnConveyor);
            bucketsOnBelt.Add(bucket);
        }
    }

    public void RemoveBucket(Bucket bucket)
    {
        bucketsOnBelt.Remove(bucket);
    }

    public int BucketCount => bucketsOnBelt.Count;

    public Vector3 GetPositionOnBelt(float t)
    {
        return Vector3.Lerp(startPoint.position, endPoint.position, t);
    }
}
