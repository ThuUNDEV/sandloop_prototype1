using UnityEngine;
using UnityEditor;
using System.IO;

public class ConveyorSystemSetupWindow : EditorWindow
{
    // References
    private GameObject sandSimulationObj;
    private Texture2D sourceImage;
    
    // Settings
    private float conveyorSpeed = 2f;
    private float conveyorYOffset = -3f;
    private float bucketTrayYOffset = -5f;
    private int trayColumns = 4;
    private int trayRows = 4;
    private float slotSpacingX = 1.2f;
    private float slotSpacingY = 1.2f;
    private int maxColorGroups = 12;
    private float colorThreshold = 30f;
    private int maxTotalBuckets = 16;
    
    // Bucket Prefab Settings
    private Sprite bucketSprite;
    private Vector2 bucketSize = new Vector2(1f, 1f);
    
    // State
    private Vector2 scrollPosition;
    private bool showAdvancedSettings = false;
    private string statusMessage = "";
    private MessageType statusType = MessageType.None;

    [MenuItem("Tools/Conveyor System Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<ConveyorSystemSetupWindow>("Conveyor Setup");
        window.minSize = new Vector2(400, 600);
    }

    private void OnEnable()
    {
        // Try to find existing SandSimulation
        var sandSim = FindObjectOfType<SandSimulation>();
        if (sandSim != null)
        {
            sandSimulationObj = sandSim.gameObject;
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawHeader();
        EditorGUILayout.Space(10);
        
        DrawSandSimulationSection();
        EditorGUILayout.Space(10);
        
        DrawConveyorSettings();
        EditorGUILayout.Space(10);
        
        DrawBucketTraySettings();
        EditorGUILayout.Space(10);
        
        DrawBucketPrefabSettings();
        EditorGUILayout.Space(10);
        
        if (showAdvancedSettings)
        {
            DrawAdvancedSettings();
            EditorGUILayout.Space(10);
        }
        
        DrawActionButtons();
        
        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Conveyor & Bucket System Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Công cụ tự động setup hệ thống Conveyor & Bucket.\n" +
            "1. Gán SandSimulation GameObject\n" +
            "2. Điều chỉnh settings\n" +
            "3. Click 'Setup All'",
            MessageType.Info
        );
    }

    private void DrawSandSimulationSection()
    {
        EditorGUILayout.LabelField("Sand Simulation", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        sandSimulationObj = (GameObject)EditorGUILayout.ObjectField(
            "Sand Simulation Object", 
            sandSimulationObj, 
            typeof(GameObject), 
            true
        );
        
        if (sandSimulationObj != null)
        {
            var sandSim = sandSimulationObj.GetComponent<SandSimulation>();
            if (sandSim == null)
            {
                EditorGUILayout.HelpBox("GameObject không có SandSimulation component!", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"  Size: {sandSim.Width} x {sandSim.Height}");
                
                var artGen = sandSimulationObj.GetComponent<SandArtGenerator>();
                if (artGen != null && artGen.sourceImage != null)
                {
                    EditorGUILayout.LabelField($"  Source Image: {artGen.sourceImage.name}");
                }
            }
        }
        
        sourceImage = (Texture2D)EditorGUILayout.ObjectField(
            "Source Image (Optional)", 
            sourceImage, 
            typeof(Texture2D), 
            false
        );
    }

    private void DrawConveyorSettings()
    {
        EditorGUILayout.LabelField("Conveyor Belt Settings", EditorStyles.boldLabel);
        
        conveyorSpeed = EditorGUILayout.Slider("Speed", conveyorSpeed, 0.5f, 10f);
        conveyorYOffset = EditorGUILayout.FloatField("Y Offset (từ tranh)", conveyorYOffset);
    }

    private void DrawBucketTraySettings()
    {
        EditorGUILayout.LabelField("Bucket Tray Settings (Grid)", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Grid Size", GUILayout.Width(80));
        trayColumns = EditorGUILayout.IntField("Columns", trayColumns);
        trayRows = EditorGUILayout.IntField("Rows", trayRows);
        EditorGUILayout.EndHorizontal();
        
        // Clamp values
        trayColumns = Mathf.Clamp(trayColumns, 1, 10);
        trayRows = Mathf.Clamp(trayRows, 1, 10);
        
        int totalSlots = trayColumns * trayRows;
        EditorGUILayout.LabelField($"  Total Slots: {totalSlots}", EditorStyles.miniLabel);
        
        EditorGUILayout.BeginHorizontal();
        slotSpacingX = EditorGUILayout.FloatField("Spacing X", slotSpacingX);
        slotSpacingY = EditorGUILayout.FloatField("Spacing Y", slotSpacingY);
        EditorGUILayout.EndHorizontal();
        
        bucketTrayYOffset = EditorGUILayout.FloatField("Y Offset (từ tranh)", bucketTrayYOffset);
        
        // Preview grid
        EditorGUILayout.Space(5);
        DrawGridPreview();
    }

    private void DrawGridPreview()
    {
        EditorGUILayout.LabelField("Preview:", EditorStyles.miniLabel);
        
        float cellSize = 15f;
        float previewWidth = trayColumns * cellSize + 10;
        float previewHeight = trayRows * cellSize + 10;
        
        Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
        
        // Draw background
        EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f));
        
        // Draw grid cells
        float startX = previewRect.x + 5;
        float startY = previewRect.y + 5;
        
        for (int row = 0; row < trayRows; row++)
        {
            for (int col = 0; col < trayColumns; col++)
            {
                Rect cellRect = new Rect(
                    startX + col * cellSize,
                    startY + row * cellSize,
                    cellSize - 2,
                    cellSize - 2
                );
                EditorGUI.DrawRect(cellRect, new Color(0.5f, 0.7f, 1f));
            }
        }
    }

    private void DrawBucketPrefabSettings()
    {
        EditorGUILayout.LabelField("Bucket Prefab Settings", EditorStyles.boldLabel);
        
        bucketSprite = (Sprite)EditorGUILayout.ObjectField(
            "Bucket Sprite (Optional)", 
            bucketSprite, 
            typeof(Sprite), 
            false
        );
        
        bucketSize = EditorGUILayout.Vector2Field("Bucket Size", bucketSize);
        
        if (bucketSprite == null)
        {
            EditorGUILayout.HelpBox("Sẽ tạo sprite mặc định (hình vuông trắng)", MessageType.Info);
        }
    }

    private void DrawAdvancedSettings()
    {
        EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.LabelField("Color Quantizer", EditorStyles.miniLabel);
        maxColorGroups = EditorGUILayout.IntSlider("Max Color Groups", maxColorGroups, 4, 20);
        colorThreshold = EditorGUILayout.Slider("Color Threshold", colorThreshold, 10f, 100f);
        maxTotalBuckets = EditorGUILayout.IntSlider("Max Total Buckets", maxTotalBuckets, 4, 32);
        
        EditorGUILayout.HelpBox(
            $"Tổng số xô tối đa: {maxTotalBuckets}\n" +
            $"Grid hiện tại: {trayColumns}x{trayRows} = {trayColumns * trayRows} slots\n" +
            "Nên đặt Max Total Buckets ≤ Grid slots",
            MessageType.Info
        );
    }

    private void DrawActionButtons()
    {
        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = sandSimulationObj != null;
        
        if (GUILayout.Button("Setup All", GUILayout.Height(40)))
        {
            SetupAll();
        }
        
        GUI.enabled = true;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Setup Components Only"))
        {
            SetupComponentsOnly();
        }
        
        if (GUILayout.Button("Create Bucket Prefab"))
        {
            CreateBucketPrefab();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Setup Conveyor"))
        {
            SetupConveyor();
        }
        
        if (GUILayout.Button("Setup Bucket Tray"))
        {
            SetupBucketTray();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        GUI.color = new Color(1f, 0.7f, 0.7f);
        if (GUILayout.Button("Remove All Conveyor Objects"))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Xóa tất cả objects của Conveyor System?", "Yes", "No"))
            {
                RemoveAllConveyorObjects();
            }
        }
        GUI.color = Color.white;
    }

    private void SetupAll()
    {
        Undo.SetCurrentGroupName("Setup Conveyor System");
        int group = Undo.GetCurrentGroup();
        
        try
        {
            SetupComponentsOnly();
            CreateBucketPrefab();
            SetupConveyor();
            SetupBucketTray();
            SetupGameManager();
            LinkAllReferences();
            
            SetStatus("Setup hoàn tất!", MessageType.Info);
        }
        catch (System.Exception e)
        {
            SetStatus($"Error: {e.Message}", MessageType.Error);
            Debug.LogException(e);
        }
        
        Undo.CollapseUndoOperations(group);
    }

    private void SetupComponentsOnly()
    {
        if (sandSimulationObj == null)
        {
            SetStatus("Chưa gán SandSimulation GameObject!", MessageType.Error);
            return;
        }
        
        // Add ColorQuantizer
        var colorQuantizer = sandSimulationObj.GetComponent<ColorQuantizer>();
        if (colorQuantizer == null)
        {
            colorQuantizer = Undo.AddComponent<ColorQuantizer>(sandSimulationObj);
        }
        
        // Configure ColorQuantizer via SerializedObject
        var so = new SerializedObject(colorQuantizer);
        so.FindProperty("maxColorGroups").intValue = maxColorGroups;
        so.FindProperty("colorThreshold").floatValue = colorThreshold;
        so.FindProperty("maxTotalBuckets").intValue = maxTotalBuckets;
        so.ApplyModifiedProperties();
        
        // Add SandAbsorber
        var sandAbsorber = sandSimulationObj.GetComponent<SandAbsorber>();
        if (sandAbsorber == null)
        {
            sandAbsorber = Undo.AddComponent<SandAbsorber>(sandSimulationObj);
        }
        
        // Configure SandAbsorber
        var soAbsorber = new SerializedObject(sandAbsorber);
        soAbsorber.FindProperty("sandSimulation").objectReferenceValue = sandSimulationObj.GetComponent<SandSimulation>();
        soAbsorber.FindProperty("colorQuantizer").objectReferenceValue = colorQuantizer;
        soAbsorber.FindProperty("simulationRenderer").objectReferenceValue = sandSimulationObj.GetComponent<Renderer>();
        soAbsorber.ApplyModifiedProperties();
        
        // Update SandArtGenerator if sourceImage is set
        if (sourceImage != null)
        {
            var artGen = sandSimulationObj.GetComponent<SandArtGenerator>();
            if (artGen != null)
            {
                var soArt = new SerializedObject(artGen);
                soArt.FindProperty("sourceImage").objectReferenceValue = sourceImage;
                soArt.ApplyModifiedProperties();
            }
        }
        
        SetStatus("Components đã được thêm vào SandSimulation!", MessageType.Info);
    }

    private GameObject CreateBucketPrefab()
    {
        // Ensure Prefabs folder exists
        string prefabFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        string prefabPath = $"{prefabFolder}/BucketPrefab.prefab";
        
        // Check if prefab already exists
        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            SetStatus("BucketPrefab đã tồn tại!", MessageType.Warning);
            return existingPrefab;
        }
        
        // Create bucket GameObject
        var bucketObj = new GameObject("BucketPrefab");
        
        // Add SpriteRenderer
        var spriteRenderer = bucketObj.AddComponent<SpriteRenderer>();
        if (bucketSprite != null)
        {
            spriteRenderer.sprite = bucketSprite;
        }
        else
        {
            // Create a default white square sprite
            spriteRenderer.sprite = CreateDefaultSprite();
        }
        spriteRenderer.sortingOrder = 10;
        
        // Add Collider for click detection
        var collider = bucketObj.AddComponent<BoxCollider2D>();
        collider.size = bucketSize;
        
        // Add Bucket component
        var bucket = bucketObj.AddComponent<Bucket>();
        
        // Configure Bucket via SerializedObject
        var so = new SerializedObject(bucket);
        so.FindProperty("bucketRenderer").objectReferenceValue = spriteRenderer;
        so.FindProperty("absorptionRadius").floatValue = 0.5f;
        so.FindProperty("absorptionRate").intValue = 5;
        so.ApplyModifiedProperties();
        
        // Create fill indicator (child)
        var fillObj = new GameObject("FillIndicator");
        fillObj.transform.SetParent(bucketObj.transform);
        fillObj.transform.localPosition = Vector3.zero;
        fillObj.transform.localScale = new Vector3(0.8f, 0f, 1f);
        
        var fillRenderer = fillObj.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = spriteRenderer.sprite;
        fillRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        fillRenderer.sortingOrder = 11;
        
        // Update bucket reference to fill indicator
        so = new SerializedObject(bucket);
        so.FindProperty("fillIndicator").objectReferenceValue = fillRenderer;
        so.FindProperty("fillMask").objectReferenceValue = fillObj.transform;
        so.ApplyModifiedProperties();
        
        // Save as prefab
        var prefab = PrefabUtility.SaveAsPrefabAsset(bucketObj, prefabPath);
        
        // Destroy temp object
        DestroyImmediate(bucketObj);
        
        SetStatus($"BucketPrefab đã tạo tại {prefabPath}", MessageType.Info);
        
        return prefab;
    }

    private Sprite CreateDefaultSprite()
    {
        // Create a simple white texture
        var texture = new Texture2D(64, 64);
        var colors = new Color[64 * 64];
        
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        // Save texture
        string texturePath = "Assets/Prefabs/BucketTexture.png";
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(texturePath, bytes);
        AssetDatabase.Refresh();
        
        // Import as sprite
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64;
            importer.SaveAndReimport();
        }
        
        return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
    }

    private void SetupConveyor()
    {
        if (sandSimulationObj == null)
        {
            SetStatus("Chưa gán SandSimulation!", MessageType.Error);
            return;
        }
        
        // Check if conveyor already exists
        var existingConveyor = FindObjectOfType<ConveyorBelt>();
        if (existingConveyor != null)
        {
            SetStatus("ConveyorBelt đã tồn tại!", MessageType.Warning);
            return;
        }
        
        // Get simulation bounds
        var renderer = sandSimulationObj.GetComponent<Renderer>();
        Bounds bounds = renderer != null ? renderer.bounds : new Bounds(Vector3.zero, Vector3.one * 10);
        
        // Create conveyor
        var conveyorObj = new GameObject("ConveyorBelt");
        Undo.RegisterCreatedObjectUndo(conveyorObj, "Create ConveyorBelt");
        
        // Position below sand simulation
        float yPos = bounds.min.y + conveyorYOffset;
        conveyorObj.transform.position = new Vector3(bounds.center.x, yPos, 0);
        
        // Add sprite renderer for visual
        var spriteRenderer = conveyorObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateConveyorSprite(bounds.size.x);
        spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f);
        spriteRenderer.sortingOrder = 5;
        
        // Add ConveyorBelt component
        var conveyor = conveyorObj.AddComponent<ConveyorBelt>();
        
        // Create start point
        var startPoint = new GameObject("ConveyorStart");
        startPoint.transform.SetParent(conveyorObj.transform);
        startPoint.transform.localPosition = new Vector3(-bounds.size.x / 2, 0, 0);
        
        // Create end point
        var endPoint = new GameObject("ConveyorEnd");
        endPoint.transform.SetParent(conveyorObj.transform);
        endPoint.transform.localPosition = new Vector3(bounds.size.x / 2, 0, 0);
        
        // Configure conveyor
        var so = new SerializedObject(conveyor);
        so.FindProperty("speed").floatValue = conveyorSpeed;
        so.FindProperty("startPoint").objectReferenceValue = startPoint.transform;
        so.FindProperty("endPoint").objectReferenceValue = endPoint.transform;
        so.FindProperty("sandSimulation").objectReferenceValue = sandSimulationObj.GetComponent<SandSimulation>();
        so.FindProperty("beltRenderer").objectReferenceValue = spriteRenderer;
        so.ApplyModifiedProperties();
        
        SetStatus("ConveyorBelt đã được tạo!", MessageType.Info);
    }

    private Sprite CreateConveyorSprite(float width)
    {
        int texWidth = Mathf.CeilToInt(width * 32);
        int texHeight = 32;
        
        var texture = new Texture2D(texWidth, texHeight);
        var colors = new Color[texWidth * texHeight];
        
        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                bool stripe = ((x / 8) % 2 == 0);
                colors[y * texWidth + x] = stripe ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.3f, 0.3f, 0.3f);
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Repeat;
        
        string texturePath = "Assets/Prefabs/ConveyorTexture.png";
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(texturePath, bytes);
        AssetDatabase.Refresh();
        
        var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }
        
        return AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
    }

    private void SetupBucketTray()
    {
        if (sandSimulationObj == null)
        {
            SetStatus("Chưa gán SandSimulation!", MessageType.Error);
            return;
        }
        
        // Check if tray already exists
        var existingTray = FindObjectOfType<BucketTray>();
        if (existingTray != null)
        {
            SetStatus("BucketTray đã tồn tại!", MessageType.Warning);
            return;
        }
        
        // Get simulation bounds
        var renderer = sandSimulationObj.GetComponent<Renderer>();
        Bounds bounds = renderer != null ? renderer.bounds : new Bounds(Vector3.zero, Vector3.one * 10);
        
        // Create bucket tray
        var trayObj = new GameObject("BucketTray");
        Undo.RegisterCreatedObjectUndo(trayObj, "Create BucketTray");
        
        // Position below conveyor
        float yPos = bounds.min.y + bucketTrayYOffset;
        trayObj.transform.position = new Vector3(bounds.center.x, yPos, 0);
        
        // Add BucketTray component
        var tray = trayObj.AddComponent<BucketTray>();
        
        // Get bucket prefab
        var bucketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BucketPrefab.prefab");
        
        // Configure tray
        var so = new SerializedObject(tray);
        so.FindProperty("columns").intValue = trayColumns;
        so.FindProperty("rows").intValue = trayRows;
        so.FindProperty("slotSpacingX").floatValue = slotSpacingX;
        so.FindProperty("slotSpacingY").floatValue = slotSpacingY;
        so.FindProperty("bucketPrefab").objectReferenceValue = bucketPrefab;
        so.FindProperty("conveyor").objectReferenceValue = FindObjectOfType<ConveyorBelt>();
        so.FindProperty("colorQuantizer").objectReferenceValue = sandSimulationObj.GetComponent<ColorQuantizer>();
        so.FindProperty("sandAbsorber").objectReferenceValue = sandSimulationObj.GetComponent<SandAbsorber>();
        so.ApplyModifiedProperties();
        
        SetStatus("BucketTray đã được tạo!", MessageType.Info);
    }

    private void SetupGameManager()
    {
        // Check if already exists
        var existing = FindObjectOfType<ConveyorGameManager>();
        if (existing != null)
        {
            return;
        }
        
        var managerObj = new GameObject("ConveyorGameManager");
        Undo.RegisterCreatedObjectUndo(managerObj, "Create GameManager");
        
        // Add components
        var gameManager = managerObj.AddComponent<ConveyorGameManager>();
        var bucketManager = managerObj.AddComponent<BucketManager>();
        
        // References will be auto-found in Start()
    }

    private void LinkAllReferences()
    {
        // Get all components
        var sandSim = sandSimulationObj?.GetComponent<SandSimulation>();
        var colorQuantizer = sandSimulationObj?.GetComponent<ColorQuantizer>();
        var sandAbsorber = sandSimulationObj?.GetComponent<SandAbsorber>();
        var conveyor = FindObjectOfType<ConveyorBelt>();
        var bucketTray = FindObjectOfType<BucketTray>();
        var gameManager = FindObjectOfType<ConveyorGameManager>();
        var bucketManager = FindObjectOfType<BucketManager>();
        
        // Link SandAbsorber
        if (sandAbsorber != null)
        {
            var so = new SerializedObject(sandAbsorber);
            so.FindProperty("sandSimulation").objectReferenceValue = sandSim;
            so.FindProperty("colorQuantizer").objectReferenceValue = colorQuantizer;
            so.FindProperty("simulationRenderer").objectReferenceValue = sandSimulationObj?.GetComponent<Renderer>();
            so.ApplyModifiedProperties();
        }
        
        // Link ConveyorBelt
        if (conveyor != null)
        {
            var so = new SerializedObject(conveyor);
            so.FindProperty("sandSimulation").objectReferenceValue = sandSim;
            so.ApplyModifiedProperties();
        }
        
        // Link BucketTray
        if (bucketTray != null)
        {
            var so = new SerializedObject(bucketTray);
            so.FindProperty("conveyor").objectReferenceValue = conveyor;
            so.FindProperty("colorQuantizer").objectReferenceValue = colorQuantizer;
            so.FindProperty("sandAbsorber").objectReferenceValue = sandAbsorber;
            so.ApplyModifiedProperties();
        }
        
        // Link GameManager
        if (gameManager != null)
        {
            var so = new SerializedObject(gameManager);
            so.FindProperty("sandSimulation").objectReferenceValue = sandSim;
            so.FindProperty("sandArtGenerator").objectReferenceValue = sandSimulationObj?.GetComponent<SandArtGenerator>();
            so.FindProperty("colorQuantizer").objectReferenceValue = colorQuantizer;
            so.FindProperty("conveyor").objectReferenceValue = conveyor;
            so.FindProperty("bucketTray").objectReferenceValue = bucketTray;
            so.FindProperty("bucketManager").objectReferenceValue = bucketManager;
            so.FindProperty("sandAbsorber").objectReferenceValue = sandAbsorber;
            so.ApplyModifiedProperties();
        }
        
        // Link BucketManager
        if (bucketManager != null)
        {
            var so = new SerializedObject(bucketManager);
            so.FindProperty("colorQuantizer").objectReferenceValue = colorQuantizer;
            so.FindProperty("bucketTray").objectReferenceValue = bucketTray;
            so.FindProperty("conveyor").objectReferenceValue = conveyor;
            so.FindProperty("sandSimulation").objectReferenceValue = sandSim;
            so.FindProperty("sandArtGenerator").objectReferenceValue = sandSimulationObj?.GetComponent<SandArtGenerator>();
            so.ApplyModifiedProperties();
        }
        
        Debug.Log("All references linked!");
    }

    private void RemoveAllConveyorObjects()
    {
        // Remove game objects
        var conveyor = FindObjectOfType<ConveyorBelt>();
        if (conveyor != null) Undo.DestroyObjectImmediate(conveyor.gameObject);
        
        var tray = FindObjectOfType<BucketTray>();
        if (tray != null) Undo.DestroyObjectImmediate(tray.gameObject);
        
        var gameManager = FindObjectOfType<ConveyorGameManager>();
        if (gameManager != null) Undo.DestroyObjectImmediate(gameManager.gameObject);
        
        // Remove components from SandSimulation
        if (sandSimulationObj != null)
        {
            var colorQuantizer = sandSimulationObj.GetComponent<ColorQuantizer>();
            if (colorQuantizer != null) Undo.DestroyObjectImmediate(colorQuantizer);
            
            var sandAbsorber = sandSimulationObj.GetComponent<SandAbsorber>();
            if (sandAbsorber != null) Undo.DestroyObjectImmediate(sandAbsorber);
        }
        
        SetStatus("Đã xóa tất cả Conveyor objects!", MessageType.Info);
    }

    private void SetStatus(string message, MessageType type)
    {
        statusMessage = message;
        statusType = type;
        Repaint();
    }
}
