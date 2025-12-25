using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using InsideMatter.UI;
using InsideMatter.Puzzle;

public class SetupWhiteboard : EditorWindow
{
    [MenuItem("Tools/Setup Whiteboard UI")]
    public static void Setup()
    {
        // 1. Find Frontboard
        GameObject frontboard = GameObject.Find("Frontboard");
        if (frontboard == null)
        {
            Debug.LogError("Could not find GameObject named 'Frontboard' in the scene context.");
            return;
        }

        // 2. Create Canvas Host
        string containerName = "WhiteboardUI_Canvas";
        GameObject uiHost = GameObject.Find(containerName);
        if (uiHost != null) DestroyImmediate(uiHost);
        
        uiHost = new GameObject(containerName);
        uiHost.transform.SetParent(frontboard.transform);
        
        // Position slightly in front of the board
        // Assuming Board is Z-forward or standard. We place it at local Z -0.05 or similar?
        // Let's guess standard orientation: Z is forward.
        uiHost.transform.localPosition = new Vector3(0, 0, -0.02f); 
        uiHost.transform.localRotation = Quaternion.Euler(0, 180, 0); // Face outward
        uiHost.transform.localScale = Vector3.one * 0.002f; // Scale down for World Space

        // 3. Setup Canvas
        Canvas canvas = uiHost.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = uiHost.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        uiHost.AddComponent<GraphicRaycaster>(); // Important for VR interaction? 
        // Note: VR Interaction requires TrackedDeviceGraphicRaycaster usually.
        // But let's stick to standard for now, user might have XR UI setup.
        // We will add the component if XR Toolkit is present.
        #if ENABLE_VR || UNITY_XR_MANAGEMENT_CONSTANTS
        // Try adding TrackedDeviceGraphicRaycaster via Reflection or name if direct type not available in potential asmdef mess
        // But commonly:
        // uiHost.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
        #endif

        RectTransform rect = uiHost.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1000, 600); // 16:9 ish high res

        // 4. Create Background (Chalkboard Look)
        CreateCleanUI(uiHost.transform);

        // 5. Add Controller
        WhiteboardController controller = uiHost.AddComponent<WhiteboardController>();
        AssignReferences(controller, uiHost.transform);
        
        Debug.Log("Whiteboard UI setup complete on 'Frontboard'.");
    }

    private static void CreateCleanUI(Transform parent)
    {
        // === MENU PAGE ===
        GameObject menuPage = CreatePanel(parent, "MenuPage");
        
        CreateText(menuPage.transform, "Title", "MAIN MENU", new Vector2(0, 200), 80, FontStyles.Bold);
        
        GameObject listContainer = CreatePanel(menuPage.transform, "LevelList");
        RectTransform listRect = listContainer.GetComponent<RectTransform>();
        listRect.sizeDelta = new Vector2(800, 300);
        listRect.anchoredPosition = new Vector2(0, -50);
        
        VerticalLayoutGroup vlg = listContainer.AddComponent<VerticalLayoutGroup>();
        vlg.childControlHeight = false; // Buttons fixed height
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 20;

        // === TASK PAGE ===
        GameObject taskPage = CreatePanel(parent, "TaskPage");
        taskPage.SetActive(false);

        CreateText(taskPage.transform, "TaskTitle", "TASK TITLE", new Vector2(0, 220), 60, FontStyles.Bold);
        CreateText(taskPage.transform, "Formula", "H2O", new Vector2(0, 140), 100, FontStyles.Bold);
        CreateText(taskPage.transform, "Description", "Build water etc...", new Vector2(0, 0), 40, FontStyles.Normal);
        CreateText(taskPage.transform, "AtomList", "H: 2\nO: 1", new Vector2(-300, -150), 40, FontStyles.Normal); // Left side

        // Back Button
        GameObject backBtn = CreateButton(taskPage.transform, "BackToMenu", "BACK", new Vector2(350, -220));
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
        ctrl.GetType().GetField("menuPage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ctrl, root.Find("MenuPage").gameObject);
        ctrl.GetType().GetField("taskPage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ctrl, root.Find("TaskPage").gameObject);
        ctrl.GetType().GetField("levelListContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ctrl, root.Find("MenuPage/LevelList"));
        
        Transform taskPage = root.Find("TaskPage");
        ctrl.GetType().GetField("taskTitleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ctrl, taskPage.Find("TaskTitle").GetComponent<TextMeshProUGUI>());
        ctrl.GetType().GetField("moleculeFormulaText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ctrl, taskPage.Find("Formula").GetComponent<TextMeshProUGUI>());
        ctrl.GetType().GetField("taskDescriptionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ctrl, taskPage.Find("Description").GetComponent<TextMeshProUGUI>());
        ctrl.GetType().GetField("atomListText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ctrl, taskPage.Find("AtomList").GetComponent<TextMeshProUGUI>());
        ctrl.GetType().GetField("backToMenuButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ctrl, taskPage.Find("BackToMenu").GetComponent<Button>());
        
        // Dummy Prefab for Button (Create one to reference)
        GameObject prefab = CreateButton(root, "LevelButtonPrefab", "Level X", Vector2.zero);
        CreatePrefabFromObject(prefab, "Assets/Prefabs/UI/LevelButton.prefab");
        ctrl.GetType().GetField("levelButtonPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(ctrl, AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LevelButton.prefab"));
        DestroyImmediate(prefab);
    }
    
    private static void CreatePrefabFromObject(GameObject obj, string path)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        
        PrefabUtility.SaveAsPrefabAsset(obj, path);
    }
}
