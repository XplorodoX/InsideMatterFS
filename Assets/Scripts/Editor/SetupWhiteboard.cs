using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using InsideMatter.UI;
using InsideMatter.Puzzle;
using System.Reflection; // Added for robustness

public class SetupWhiteboard : EditorWindow
{
    [MenuItem("Tools/Setup Whiteboard UI")]
    public static void Setup()
    {
        // 1. Find Frontboard (Try exact name, then variations, then selection)
        GameObject frontboard = GameObject.Find("FrontBoard");
        if (frontboard == null) frontboard = GameObject.Find("Frontboard");
        if (frontboard == null) frontboard = Selection.activeGameObject;

        if (frontboard == null)
        {
            Debug.LogError("Could not find 'FrontBoard' automatically. Please SELECT the board in the Scene view and run this tool again.");
            EditorUtility.DisplayDialog("Setup Error", "Could not find 'FrontBoard'.\nPlease select the whiteboard object in the Scene and try again.", "OK");
            return;
        }

        // 2. Create Canvas Host
        string containerName = "WhiteboardUI_Canvas";
        GameObject uiHost = GameObject.Find(containerName);
        if (uiHost != null) DestroyImmediate(uiHost);
        
        uiHost = new GameObject(containerName);
        uiHost.transform.SetParent(frontboard.transform);
        
        // --- SMART POSITIONING & SIZING ---
        float worldScale = 0.002f; // Scale for UI World Space (Standard for clear text)
        Vector2 canvasSize = new Vector2(1000, 600); // Default fallback

        MeshFilter meshFilter = frontboard.GetComponentInChildren<MeshFilter>();
        Renderer rend = frontboard.GetComponentInChildren<Renderer>();
        
        if (meshFilter != null)
        {
            // Use Local Bounds for exact sizing
            Bounds localBounds = meshFilter.sharedMesh.bounds;
            Vector3 parentScale = frontboard.transform.lossyScale;
            
            // Calculate real dimensions
            float width = localBounds.size.x * parentScale.x;
            float height = localBounds.size.y * parentScale.y;
            
            // Safety check: sometimes boards are modeled along Z or X. 
            // Assuming standard Quad/Plane: X = width, Y = height.
            // If Z is larger than X, maybe it's rotated?
            // Let's assume X/Y plane for the face.
            
            // Add padding (5cm margin)
            float margin = 0.05f;
            float usableWidth = Mathf.Max(0.1f, width - margin * 2);
            float usableHeight = Mathf.Max(0.1f, height - margin * 2);

            // Calculate Canvas pixel size
            canvasSize = new Vector2(usableWidth / worldScale, usableHeight / worldScale);

            // Position at center of bounds (World Space) + slight offset
            // We use Renderer bounds center for world position to be safe against pivot offsets
            if (rend != null)
                uiHost.transform.position = rend.bounds.center;
            else
                uiHost.transform.position = frontboard.transform.position;

            // Rotation: Standard 180 flip for UI facing back? 
            // Or align with object forward?
            // Let's stick to user previous success, just adding size logic.
            uiHost.transform.rotation = frontboard.transform.rotation * Quaternion.Euler(0, 180, 0);
            
            // Move "forward" in local space of the UI
            uiHost.transform.Translate(Vector3.back * 0.02f, Space.Self); 
        }
        else if (rend != null)
        {
             // Fallback to Bounds if no mesh filter (e.g. primitive)
             uiHost.transform.position = rend.bounds.center;
             uiHost.transform.rotation = frontboard.transform.rotation * Quaternion.Euler(0, 180, 0);
             uiHost.transform.Translate(Vector3.back * 0.05f, Space.Self);
             // Guess size from bounds (unreliable if rotated)
             canvasSize = new Vector2(rend.bounds.size.x / worldScale, rend.bounds.size.y / worldScale);
        }
        else
        {
             uiHost.transform.localPosition = new Vector3(0, 0, -0.05f); 
             uiHost.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
        
        uiHost.transform.localScale = Vector3.one * worldScale;

        // 3. Setup Canvas
        Canvas canvas = uiHost.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = uiHost.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        #if ENABLE_VR || UNITY_XR_MANAGEMENT_CONSTANTS
        // Attempt to add Tracked Device Graphic Raycaster for VR support
        // We use Reflection to avoid compiler errors if package is missing
        System.Type vrRaycaster = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
        if (vrRaycaster != null)
        {
            uiHost.AddComponent(vrRaycaster);
        }
        else
        {
            uiHost.AddComponent<GraphicRaycaster>();
        }
        #else
        uiHost.AddComponent<GraphicRaycaster>();
        #endif

        RectTransform rect = uiHost.GetComponent<RectTransform>();
        rect.sizeDelta = canvasSize; 

        // 4. Create Background
        CreateCleanUI(uiHost.transform, canvasSize);

        // 5. Add Controller
        WhiteboardController controller = uiHost.AddComponent<WhiteboardController>();
        AssignReferences(controller, uiHost.transform);
        
        Debug.Log("Whiteboard UI setup complete on 'Frontboard'.");
    }

    private static void CreateCleanUI(Transform parent, Vector2 size)
    {
        // === 1. HOME PAGE (Main Menu) ===
        GameObject homePage = CreatePanel(parent, "HomePage");
        
        CreateText(homePage.transform, "Title", "MAIN MENU", new Vector2(0, size.y * 0.3f), size.y * 0.15f, FontStyles.Bold);
        CreateText(homePage.transform, "Subtitle", "Select Mode", new Vector2(0, size.y * 0.15f), size.y * 0.08f, FontStyles.Normal);

        // Buttons
        float btnW = size.x * 0.4f;
        float btnH = size.y * 0.2f;
        
        // Free Play Button
        GameObject freePlayBtn = CreateButton(homePage.transform, "FreePlayBtn", "FREE PLAY", new Vector2(-size.x * 0.25f, -size.y * 0.1f));
        SetButtonSize(freePlayBtn, new Vector2(btnW, btnH));
        
        // Levels Button
        GameObject levelsBtn = CreateButton(homePage.transform, "LevelsBtn", "LEVELS", new Vector2(size.x * 0.25f, -size.y * 0.1f));
        SetButtonSize(levelsBtn, new Vector2(btnW, btnH));


        // === 2. LEVEL SELECT PAGE ===
        GameObject levelSelectPage = CreatePanel(parent, "LevelSelectPage");
        levelSelectPage.SetActive(false);
        
        CreateText(levelSelectPage.transform, "Title", "SELECT LEVEL", new Vector2(0, size.y * 0.35f), size.y * 0.15f, FontStyles.Bold);
        
        // List Container
        GameObject listContainer = CreatePanel(levelSelectPage.transform, "LevelList");
        RectTransform listRect = listContainer.GetComponent<RectTransform>();
        listRect.sizeDelta = new Vector2(size.x * 0.8f, size.y * 0.5f);
        listRect.anchoredPosition = new Vector2(0, -size.y * 0.05f);
        
        VerticalLayoutGroup vlg = listContainer.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = false; 
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = size.y * 0.05f;

        // Back to Home Button
        GameObject homeBtn = CreateButton(levelSelectPage.transform, "BackToHome", "BACK", new Vector2(0, -size.y * 0.35f));


        // === 3. TASK PAGE ===
        GameObject taskPage = CreatePanel(parent, "TaskPage");
        taskPage.SetActive(false);

        // Task Title
        CreateText(taskPage.transform, "TaskTitle", "TASK TITLE", new Vector2(0, size.y * 0.35f), size.y * 0.1f, FontStyles.Bold);
        CreateText(taskPage.transform, "Formula", "H2O", new Vector2(0, size.y * 0.15f), size.y * 0.2f, FontStyles.Bold);
        CreateText(taskPage.transform, "Description", "Build water...", new Vector2(0, 0), size.y * 0.08f, FontStyles.Normal);
        CreateText(taskPage.transform, "AtomList", "H: 2\nO: 1", new Vector2(-size.x * 0.3f, -size.y * 0.2f), size.y * 0.08f, FontStyles.Normal); 

        // Back to Levels Button
        GameObject menuBtn = CreateButton(taskPage.transform, "BackToMenu", "BACK", new Vector2(0, -size.y * 0.35f));
    }
    
    private static void SetButtonSize(GameObject btnObj, Vector2 size)
    {
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = size;
    }


    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, Vector2 pos, float size, FontStyles style)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        
        TextMeshProUGUI txt = obj.AddComponent<TextMeshProUGUI>();
        txt.text = content;
        txt.fontSize = size;
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontStyle = style;
        txt.color = Color.white;
        
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(800, 150);

        return txt;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(250, 60);
        rt.anchoredPosition = pos;

        CreateText(btnObj.transform, "Label", label, Vector2.zero, 30, FontStyles.Bold);

        return btnObj;
    }

    private static void AssignReferences(WhiteboardController ctrl, Transform root)
    {
        SetPrivateField(ctrl, "homePage", root.Find("HomePage").gameObject);
        SetPrivateField(ctrl, "levelSelectPage", root.Find("LevelSelectPage").gameObject);
        SetPrivateField(ctrl, "taskPage", root.Find("TaskPage").gameObject);
        SetPrivateField(ctrl, "levelListContainer", root.Find("LevelSelectPage/LevelList"));
        
        Transform homePage = root.Find("HomePage");
        // We will assign these to the Controller, but wait, Controller doesn't have fields for them yet!
        // I need to add them to Controller. BUT I can't edit controller here.
        // I will add the onClick listeners here in the Editor script using UnityEventTools!
        
        var freePlayBtn = homePage.Find("FreePlayBtn").GetComponent<Button>();
        var levelsBtn = homePage.Find("LevelsBtn").GetComponent<Button>();
        
        UnityEditor.Events.UnityEventTools.AddPersistentListener(freePlayBtn.onClick, ctrl.StartFreePlay);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(levelsBtn.onClick, ctrl.ShowLevelSelection);
        
        Transform levelSelectPage = root.Find("LevelSelectPage");
        var backToHomeBtn = levelSelectPage.Find("BackToHome").GetComponent<Button>();
        // Controller now has backToHomeButton field, let's use that if possible, or just listener
        UnityEditor.Events.UnityEventTools.AddPersistentListener(backToHomeBtn.onClick, ctrl.ShowHome);
        
        Transform taskPage = root.Find("TaskPage");
        SetPrivateField(ctrl, "taskTitleText", taskPage.Find("TaskTitle").GetComponent<TextMeshProUGUI>());
        SetPrivateField(ctrl, "moleculeFormulaText", taskPage.Find("Formula").GetComponent<TextMeshProUGUI>());
        SetPrivateField(ctrl, "taskDescriptionText", taskPage.Find("Description").GetComponent<TextMeshProUGUI>());
        SetPrivateField(ctrl, "atomListText", taskPage.Find("AtomList").GetComponent<TextMeshProUGUI>());
        SetPrivateField(ctrl, "backToMenuButton", taskPage.Find("BackToMenu").GetComponent<Button>());
        
        // Dummy Prefab for Button
        GameObject prefab = CreateButton(root, "LevelButtonPrefab", "Level X", Vector2.zero);
        string path = "Assets/Prefabs/UI/LevelButton.prefab";
        CreatePrefabFromObject(prefab, path);
        SetPrivateField(ctrl, "levelButtonPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(path));
        DestroyImmediate(prefab);
    }
    
    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
        else
        {
            Debug.LogError($"Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }
    
    private static void CreatePrefabFromObject(GameObject obj, string path)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        
        PrefabUtility.SaveAsPrefabAsset(obj, path);
    }
}
