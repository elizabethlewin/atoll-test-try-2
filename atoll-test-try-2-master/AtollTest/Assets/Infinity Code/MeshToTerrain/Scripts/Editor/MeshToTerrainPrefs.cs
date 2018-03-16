﻿/*       INFINITY CODE 2013 - 2017         */
/*     http://www.infinity-code.com        */

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class MeshToTerrainPrefs 
{
    private const string appKey = "MTT_";

    private readonly int[] availableHeights = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };
    private readonly int[] availableResolutions = { 32, 64, 128, 256, 512, 1024, 2048 };

    private string[] availableResolutionsStr;
    private string[] availableHeightsStr;

    public int alphamapResolution = 512;
    public int baseMapResolution = 1024;
    public GameObject boundsGameObject;
    public MeshToTerrainBounds boundsType = MeshToTerrainBounds.autoDetect;
    public MeshToTerrainDirection direction;
    public int detailResolution = 1024;
    public bool generateTextures;
    public int heightmapResolution = 129;
    public List<GameObject> meshes = new List<GameObject>();
    public MeshToTerrainFindType meshFindType = MeshToTerrainFindType.gameObjects;
    public int meshLayer = 31;
    public int newTerrainCountX = 1;
    public int newTerrainCountY = 1;
    public int resolutionPerPatch = 16;
    public int smoothingFactor = 1;
    public List<Terrain> terrains = new List<Terrain>();
    public MeshToTerrainSelectTerrainType terrainType = MeshToTerrainSelectTerrainType.newTerrains;
    public Color textureEmptyColor = Color.white;
    public int textureHeight = 1024;
    public int textureWidth = 1024;
    public bool useHeightmapSmoothing = true;
    public MeshToTerrainYRange yRange = MeshToTerrainYRange.minimalRange;
    public int yRangeValue = 1000;

    private Vector2 scrollPos = Vector2.zero;
    private bool showMeshes = true;
    private bool showTerrains = true;
    private bool showTextures = true;
    private static bool hasThirdParty;
    private static bool hasRTP = false;

    private static Texture2D _helpIcon;
    private static GUIStyle _helpStyle;

    private static bool needFindIcon = true;
    public MeshToTerrainBoundsHelper boundsHelper;
    public bool showBoundSelector; 

    [SerializeField] 
    private bool hasBounds = false;

    [SerializeField] 
    public Bounds bounds;

    public MeshToTerrainHoles holes = MeshToTerrainHoles.minimumValue;

    public static Texture2D helpIcon
    {
        get
        {
            if (_helpIcon == null && needFindIcon)
            {
                string[] guids = AssetDatabase.FindAssets("HelpIcon t:texture");
                if (guids != null && guids.Length > 0)
                {
                    string iconPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _helpIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(iconPath, typeof(Texture2D));
                }
                needFindIcon = false;
            }
            return _helpIcon;
        }
    }

    public static GUIStyle helpStyle
    {
        get
        {
            if (_helpStyle == null)
            {
                _helpStyle = new GUIStyle();
                _helpStyle.margin = new RectOffset(0, 0, 2, 0);
            }
            return _helpStyle;
        }
    }

    public void Init()
    {
        Load();

        hasRTP = typeof(MeshToTerrain).Assembly.GetType("RTP_LODmanagerEditor") != null;
        hasThirdParty = hasRTP;
    }

    private static void AddCompilerDirective(object key)
    {
        string currentDefinitions =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);

        string[] defs = currentDefinitions.Split(';').Select(d => d.Trim(' ')).ToArray();

        if (defs.All(d => d != key.ToString()))
        {
            ArrayUtility.Add(ref defs, key.ToString());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", defs));
        }
    }

    private static void DeleteCompilerDirective(object key)
    {
        string currentDefinitions =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);

        string[] defs = currentDefinitions.Split(';').Select(d => d.Trim(' ')).ToArray();

        if (defs.Any(d => d == key.ToString()))
        {
            ArrayUtility.Remove(ref defs, key.ToString());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", defs));
        }
    }

    public static void DrawField(string label, ref int value, string tooltip, string href, string[] displayedOptions = null, int[] optionValues = null)
    {
        EditorGUILayout.BeginHorizontal();
        DrawHelpButton(tooltip, href);
        EditorGUILayout.LabelField(label, EditorStyles.label);
        if (displayedOptions != null) value = EditorGUILayout.IntPopup(string.Empty, value, displayedOptions, optionValues);
        else value = EditorGUILayout.IntField("", value);
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawHelpButton(string tooltip, string href = null)
    {
        if (GUILayout.Button(new GUIContent(helpIcon, tooltip),
            helpStyle, GUILayout.ExpandWidth(false)) && !string.IsNullOrEmpty(href))
            Application.OpenURL(href);
    }

    public static bool Foldout(bool value, string text)
    {
        return GUILayout.Toggle(value, text, EditorStyles.foldout);
    }

    private static int LimitPowTwo(int val, int min = 32, int max = 4096)
    {
        return Mathf.Clamp(Mathf.ClosestPowerOfTwo(val), min, max);
    }

    private void Load()
    {
        baseMapResolution = LoadPref("BaseMapRes", 1024);
        boundsGameObject = LoadPrefGameObject("BoundsGameObject", null);
        boundsType = (MeshToTerrainBounds) LoadPref("BoundsType", (int) MeshToTerrainBounds.autoDetect);
        if (boundsType == MeshToTerrainBounds.selectBounds)
        {
            bounds = LoadPref("Bounds", new Bounds(Vector3.zero, Vector3.one));
        }
        detailResolution = LoadPref("DetailMapRes", 1024);
        direction = (MeshToTerrainDirection)LoadPref("Direction", (int)MeshToTerrainDirection.normal);
        generateTextures = LoadPref("GenerateTextures", false);
        heightmapResolution = LoadPref("HeightMapRes", 129);
        meshes = LoadPref("Meshes", new List<GameObject>());
        holes = (MeshToTerrainHoles)LoadPref("Holes", (int)MeshToTerrainHoles.minimumValue);
        meshFindType = (MeshToTerrainFindType)LoadPref("MeshFindType", (int)MeshToTerrainFindType.gameObjects);
        meshLayer = LoadPref("MeshLayer", 31);
        newTerrainCountX = LoadPref("CountX", 1);
        newTerrainCountY = LoadPref("CountY", 1);
        resolutionPerPatch = LoadPref("ResPerPath", 16);
        showMeshes = LoadPref("ShowMeshes", true);
        showTerrains = LoadPref("ShowTerrains", true);
        showTextures = LoadPref("ShowTextures", true);
        smoothingFactor = LoadPref("SmoothingFactor", 1);
        terrains = LoadPref("Terrains", new List<Terrain>());
        terrainType = (MeshToTerrainSelectTerrainType)LoadPref("TerrainType", (int)MeshToTerrainSelectTerrainType.newTerrains);
        textureEmptyColor = LoadPref("TextureEmptyColor", Color.white);
        textureHeight = LoadPref("TextureHeight", 1024);
        textureWidth = LoadPref("TextureWidth", 1024);
        useHeightmapSmoothing = LoadPref("UseHeightmapSmoothing", true);
        yRange = (MeshToTerrainYRange)LoadPref("YRange", 0);
        yRangeValue = LoadPref("YRangeValue", yRangeValue);
    }

    private bool LoadPref(string id, bool defVal)
    {
        string key = appKey + id;
        return EditorPrefs.HasKey(key) ? EditorPrefs.GetBool(key) : defVal;
    }

    private Color LoadPref(string id, Color defVal)
    {
        return new Color(LoadPref(id + "_R", defVal.r), LoadPref(id + "_G", defVal.g), LoadPref(id + "_B", defVal.b), LoadPref(id + "_A", defVal.a));
    }

    private Bounds LoadPref(string id, Bounds defVal) 
    {
        return new Bounds(LoadPref(id + "_Center", defVal.center), LoadPref(id + "_Size", defVal.size));
    }

    private Vector3 LoadPref(string id, Vector3 defVal)
    {
        return new Vector3(LoadPref(id + "_X", defVal.x), LoadPref(id + "_Y", defVal.y), LoadPref(id + "_Z", defVal.z));
    }

    private float LoadPref(string id, float defVal)
    {
        string key = appKey + id;
        return EditorPrefs.HasKey(key) ? EditorPrefs.GetFloat(key) : defVal;
    }

    private int LoadPref(string id, int defVal)
    {
        string key = appKey + id;
        return EditorPrefs.HasKey(key) ? EditorPrefs.GetInt(key) : defVal;
    }

    private List<GameObject> LoadPref(string id, List<GameObject> defVals)
    {
        string key = appKey + id + "_Count";
        if (EditorPrefs.HasKey(key))
        {
            int count = EditorPrefs.GetInt(appKey + id + "_Count");
            List<GameObject> retVal = new List<GameObject>();
            for (int i = 0; i < count; i++) retVal.Add(EditorUtility.InstanceIDToObject(EditorPrefs.GetInt(appKey + id + "_" + i)) as GameObject);
            return retVal;
        }
        return defVals;
    }

    private List<Terrain> LoadPref(string id, List<Terrain> defVals)
    {
        string key = appKey + id + "_Count";
        if (EditorPrefs.HasKey(key))
        {
            int count = EditorPrefs.GetInt(appKey + id + "_Count");
            List<Terrain> retVal = new List<Terrain>();
            for (int i = 0; i < count; i++) retVal.Add(EditorUtility.InstanceIDToObject(EditorPrefs.GetInt(appKey + id + "_" + i)) as Terrain);
            return retVal;
        }
        return defVals;
    }

    private GameObject LoadPrefGameObject(string id, GameObject defVal)
    {
        int goID = LoadPref(id, -1);
        if (goID == -1) return defVal;
        GameObject go = EditorUtility.InstanceIDToObject(goID) as GameObject;
        return go ?? defVal;
    }

    public void OnGUI()
    {
        OnToolbarGUI();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        OnMeshesGUI();
        OnTerrainsGUI();
        OnTexturesGUI();

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Start")) MeshToTerrain.phase = MeshToTerrainPhase.prepare;
    }

    private void OnMeshesGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showMeshes = Foldout(showMeshes, "Meshes: ");
        if (showMeshes)
        {
            meshFindType = (MeshToTerrainFindType)EditorGUILayout.EnumPopup("Mesh select type: ", meshFindType);

            if (meshFindType == MeshToTerrainFindType.gameObjects) OnMeshesGUIGameObjects();
            else if (meshFindType == MeshToTerrainFindType.layers) meshLayer = EditorGUILayout.LayerField("Layer: ", meshLayer);

            direction = (MeshToTerrainDirection) EditorGUILayout.EnumPopup("Direction: ", direction);
            if (direction == MeshToTerrainDirection.reversed) GUILayout.Label("Use the reverse direction, if that model has inverted the normal.");

            yRange = (MeshToTerrainYRange) EditorGUILayout.EnumPopup("Y Range: ", yRange);
            if (yRange == MeshToTerrainYRange.fixedValue) yRangeValue = EditorGUILayout.IntField("Y Range Value: ", yRangeValue);
        }
        EditorGUILayout.EndVertical();
    }

    private void OnMeshesGUIGameObjects()
    {
        meshes.RemoveAll(m => m == null);
        for (int i = 0; i < meshes.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            meshes[i] = (GameObject)EditorGUILayout.ObjectField("Mesh " + (i + 1) + ": ", meshes[i], typeof(GameObject), true);
            if (GUILayout.Button("X", GUILayout.ExpandWidth(false))) meshes[i] = null;
            EditorGUILayout.EndHorizontal();
        }
        meshes.RemoveAll(m => m == null);

        GameObject newMesh = (GameObject)EditorGUILayout.ObjectField("Mesh GameObject: ", null, typeof(GameObject), true);
        if (newMesh != null)
        {
            if (!meshes.Contains(newMesh)) meshes.Add(newMesh);
            else EditorUtility.DisplayDialog("Warning", "GameObject already added", "OK");
        }
    }

    private static void OnForum()
    {
        Process.Start("http://forum.infinity-code.com/");
    }

    private static void OnProductPage()
    {
        Process.Start("http://infinity-code.com/products/mesh-to-terrain");
    }

    private static void OnSendMail()
    {
        Process.Start("mailto:support@infinity-code.com?subject=Mesh to Terrain");
    }

    private void OnTerrainsGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        showTerrains = Foldout(showTerrains, "Terrains: ");
        if (showTerrains)
        {
            terrainType = (MeshToTerrainSelectTerrainType)EditorGUILayout.EnumPopup("Type: ", terrainType);
            if (terrainType == MeshToTerrainSelectTerrainType.existTerrains) OnTerrainsGUIExists();
            else OnTerrainsGUINew();
            useHeightmapSmoothing = GUILayout.Toggle(useHeightmapSmoothing, "Use smoothing of height maps.");

            if (useHeightmapSmoothing)
            {
                smoothingFactor = EditorGUILayout.IntField("Smoothing factor: ", smoothingFactor);
                if (smoothingFactor < 1) smoothingFactor = 1;
                else if (smoothingFactor > 128) smoothingFactor = 128;
            }

            holes = (MeshToTerrainHoles)EditorGUILayout.EnumPopup("Holes: ", holes);
        }
        EditorGUILayout.EndVertical();
    }

    private void OnTerrainsGUINew()
    {
        if (availableResolutionsStr == null || availableHeightsStr == null)
        {
            availableHeightsStr = availableHeights.Select(h => h.ToString()).ToArray();
            availableResolutionsStr = availableResolutions.Select(r => r.ToString()).ToArray();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Count terrains. X: ", GUILayout.ExpandWidth(false));
        newTerrainCountX = Mathf.Max(EditorGUILayout.IntField(newTerrainCountX, GUILayout.ExpandWidth(false)), 1);
        GUILayout.Label("Y: ", GUILayout.ExpandWidth(false));
        newTerrainCountY = Mathf.Max(EditorGUILayout.IntField(newTerrainCountY, GUILayout.ExpandWidth(false)), 1);
        GUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        boundsType = (MeshToTerrainBounds)EditorGUILayout.EnumPopup("Bounds: ", boundsType);

        if (EditorGUI.EndChangeCheck())
        {
            if (boundsType == MeshToTerrainBounds.selectBounds)
            {
                
            }
            else if (boundsHelper != null)
            {
                Object.DestroyImmediate(boundsHelper.gameObject);
            }
        }

        if (boundsType == MeshToTerrainBounds.fromGameobject) boundsGameObject = (GameObject)EditorGUILayout.ObjectField("Bounds GameObject: ", boundsGameObject, typeof(GameObject), true);
        else if (boundsType == MeshToTerrainBounds.selectBounds)
        {
            if (boundsHelper == null) showBoundSelector = false;

            showBoundSelector = GUILayout.Toggle(showBoundSelector, "Show Selector", GUI.skin.button);
            if (showBoundSelector && boundsHelper == null)
            {
                GameObject helperGO = new GameObject("Mesh To Terrain Helper");
                helperGO.transform.position = Vector3.zero;
                boundsHelper = helperGO.AddComponent<MeshToTerrainBoundsHelper>();

                boundsHelper.bounds = hasBounds? bounds: new Bounds();
                boundsHelper.OnBoundChanged += delegate
                {
                    bounds = boundsHelper.bounds;
                    MeshToTerrain.RepaintWindow();
                };

                boundsHelper.OnDestroyed += MeshToTerrain.RepaintWindow;

                if (!hasBounds)
                {
                    bool findedBounds = false;

                    foreach (GameObject meshGO in meshes)
                    {
                        MeshRenderer[] meshRenderers = meshGO.GetComponentsInChildren<MeshRenderer>();

                        foreach (MeshRenderer meshRenderer in meshRenderers)
                        {
                            if (!findedBounds)
                            {
                                findedBounds = true;
                                boundsHelper.bounds = meshRenderer.bounds;
                            }
                            else boundsHelper.bounds.Encapsulate(meshRenderer.bounds);
                        }
                    }

                    if (!findedBounds) boundsHelper.bounds = new Bounds(Vector3.zero, Vector3.one);
                    bounds = boundsHelper.bounds;
                    hasBounds = true;
                }
                
                Selection.activeGameObject = helperGO;
            }
            else if (!showBoundSelector && boundsHelper != null)
            {
                Object.DestroyImmediate(boundsHelper.gameObject);
                boundsHelper = null;
            }

            EditorGUI.BeginChangeCheck();
            bounds = EditorGUILayout.BoundsField("Bounds: ", bounds);

            if (EditorGUI.EndChangeCheck() && boundsHelper != null) boundsHelper.bounds = bounds;

            if (GUILayout.Button("Detect Bounds"))
            {
                bool findedBounds = false;

                Bounds newBounds = new Bounds();

                foreach (GameObject meshGO in meshes)
                {
                    MeshRenderer[] meshRenderers = meshGO.GetComponentsInChildren<MeshRenderer>();

                    foreach (MeshRenderer meshRenderer in meshRenderers)
                    {
                        if (!findedBounds)
                        {
                            findedBounds = true;
                            newBounds = meshRenderer.bounds;
                        }
                        else newBounds.Encapsulate(meshRenderer.bounds);
                    }
                }

                if (findedBounds)
                {
                    bounds = newBounds;
                }
                if (boundsHelper != null)
                {
                    boundsHelper.bounds = bounds;
                    if (findedBounds) SceneView.RepaintAll();
                }
            }
        }

        const string detailTooltip = "The resolution of the map that controls grass and detail meshes. For performance reasons (to save on draw calls) the lower you set this number the better.";
        const string resolutionPerPatchTooltip = "Specifies the size in pixels of each individually rendered detail patch. A larger number reduces draw calls, but might increase triangle count since detail patches are culled on a per batch basis. A recommended value is 16. If you use a very large detail object distance and your grass is very sparse, it makes sense to increase the value.";
        const string basemapTooltip = "Resolution of the composite texture used on the terrain when viewed from a distance greater than the Basemap Distance.";
        const string heightmapTooltip = "Pixel resolution of the terrains heightmap.";
        const string alphamapTooltip = "Resolution of the splatmap that controls the blending of the different terrain textures.";
        const string helpHref = "http://docs.unity3d.com/Manual/terrain-OtherSettings.html";

        DrawField("Heightmap Resolution:", ref heightmapResolution, heightmapTooltip, helpHref, availableHeightsStr, availableHeights);
        DrawField("Detail Resolution:", ref detailResolution, detailTooltip, helpHref);
        DrawField("Control Texture Resolution:", ref alphamapResolution, alphamapTooltip, helpHref, availableResolutionsStr, availableResolutions);
        DrawField("Base Texture Resolution:", ref baseMapResolution, basemapTooltip, helpHref, availableResolutionsStr, availableResolutions);
        DrawField("Resolution Per Patch:", ref resolutionPerPatch, resolutionPerPatchTooltip, helpHref);

        if (resolutionPerPatch < 1) resolutionPerPatch = 1;
        detailResolution = detailResolution / resolutionPerPatch * resolutionPerPatch;
    }

    private void OnTerrainsGUIExists()
    {
        for (int i = 0; i < terrains.Count; i++) terrains[i] = (Terrain)EditorGUILayout.ObjectField(terrains[i], typeof(Terrain), true);
        terrains.RemoveAll(t => t == null);

        Terrain newTerrain = (Terrain)EditorGUILayout.ObjectField(null, typeof(Terrain), true);
        if (newTerrain != null)
        {
            if (!terrains.Contains(newTerrain)) terrains.Add(newTerrain);
            else EditorUtility.DisplayDialog("Warning", "Terrain already added", "OK");
        }
    }

    private void OnTexturesGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal();
        showTextures = GUILayout.Toggle(showTextures, GUIContent.none, EditorStyles.foldout, GUILayout.ExpandWidth(false));
        generateTextures = GUILayout.Toggle(generateTextures, "Textures");
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(!generateTextures);
        if (showTextures)
        {
#if !UNITY_5_4_OR_NEWER
            int maxTextureSize = 4096;
#else
            int maxTextureSize = 8192;
#endif
            textureWidth = LimitPowTwo(EditorGUILayout.IntField("Width: ", textureWidth), 32, maxTextureSize);
            textureHeight = LimitPowTwo(EditorGUILayout.IntField("Height: ", textureHeight), 32, maxTextureSize);
            textureEmptyColor = EditorGUILayout.ColorField("Empty color: ", textureEmptyColor);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndVertical();
    }

    private static void OnToolbarGUI()
    {
        GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton);

        GUILayout.BeginHorizontal();
        GUILayout.Label("", buttonStyle);

        OnThirdPartyExts(buttonStyle);
        OnToolbarHelp(buttonStyle);

        GUILayout.EndHorizontal();
    }

    private static void OnToolbarHelp(GUIStyle buttonStyle)
    {
        if (GUILayout.Button("Help", buttonStyle, GUILayout.ExpandWidth(false)))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Product Page"), false, OnProductPage);
            menu.AddItem(new GUIContent("Documentation"), false, OnViewDocs);
            menu.AddItem(new GUIContent("Forum"), false, OnForum);
            menu.AddItem(new GUIContent("Support"), false, OnSendMail);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Check Updates"), false, MeshToTerrainUpdater.OpenWindow);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("About"), false, MeshToTerrainAboutWindow.OpenWindow);
            menu.ShowAsContext();
        }
    }

    private static void OnThirdPartyExts(GUIStyle buttonStyle)
    {
        if (hasThirdParty && GUILayout.Button("Third-party", buttonStyle, GUILayout.ExpandWidth(false)))
        {
            GenericMenu menu = new GenericMenu();

            if (hasRTP)
            {
#if RTP
                menu.AddItem(new GUIContent("Disable Relief Terrain Pack"), false, DeleteCompilerDirective, "RTP");
#else
                menu.AddItem(new GUIContent("Enable Relief Terrain Pack"), false, AddCompilerDirective, "RTP");
#endif
            }
            menu.ShowAsContext();
        }
    }

    private static void OnViewDocs()
    {
        Process.Start("http://infinity-code.com/docs/mesh-terrain");
    }

    public void Save()
    {
        SetPref("BaseMapRes", baseMapResolution);
        SetPref("BoundsGameObject", boundsGameObject);
        SetPref("BoundsType", (int)boundsType);
        if (boundsType == MeshToTerrainBounds.selectBounds) SetPref("Bounds", bounds);
        SetPref("CountX", newTerrainCountX);
        SetPref("CountY", newTerrainCountY);
        SetPref("DetailMapRes", detailResolution);
        SetPref("Direction", (int)direction);
        SetPref("GenerateTextures", generateTextures);
        SetPref("HeightMapRes", heightmapResolution);
        SetPref("Meshes", meshes);
        SetPref("MeshFindType", (int)meshFindType);
        SetPref("MeshLayer", meshLayer);
        SetPref("ResPerPath", resolutionPerPatch);
        SetPref("ShowMeshes", showMeshes);
        SetPref("ShowTerrains", showTerrains);
        SetPref("ShowTextures", showTextures);
        SetPref("SmoothingFactor", smoothingFactor);
        SetPref("Holes", (int)holes);
        SetPref("Terrains", terrains);
        SetPref("TerrainType", (int)terrainType);
        SetPref("TextureEmptyColor", textureEmptyColor);
        SetPref("TextureHeight", textureHeight);
        SetPref("TextureWidth", textureWidth);
        SetPref("UseHeightmapSmoothing", useHeightmapSmoothing);
        SetPref("YRange", (int)yRange);
        SetPref("YRangeValue", yRangeValue);
    }

    private void SetPref(string id, bool val)
    {
        EditorPrefs.SetBool(appKey + id, val);
    }

    private void SetPref(string id, Color val)
    {
        SetPref(id + "_R", val.r);
        SetPref(id + "_G", val.g);
        SetPref(id + "_B", val.b);
        SetPref(id + "_A", val.a);
    }

    private void SetPref(string id, Vector3 val)
    {
        SetPref(id + "_X", val.x);
        SetPref(id + "_Y", val.y);
        SetPref(id + "_Z", val.z);
    }

    private void SetPref(string id, Bounds val)
    {
        SetPref(id + "_Center", val.center);
        SetPref(id + "_Size", val.size);
    }

    private void SetPref(string id, float val)
    {
        EditorPrefs.SetFloat(appKey + id, val);
    }

    private void SetPref(string id, int val)
    {
        EditorPrefs.SetInt(appKey + id, val);
    }

    private void SetPref(string id, List<GameObject> vals)
    {
        if (vals != null)
        {
            EditorPrefs.SetInt(appKey + id + "_Count", vals.Count);

            for (int i = 0; i < vals.Count; i++)
            {
                Object val = vals[i];
                if (val != null) EditorPrefs.SetInt(appKey + id + "_" + i, val.GetInstanceID());
            }
        }
        else EditorPrefs.SetInt(appKey + id + "_Count", 0);
    }

    private void SetPref(string id, List<Terrain> vals)
    {
        if (vals != null)
        {
            EditorPrefs.SetInt(appKey + id + "_Count", vals.Count);

            for (int i = 0; i < vals.Count; i++)
            {
                Object val = vals[i];
                if (val != null) EditorPrefs.SetInt(appKey + id + "_" + i, val.GetInstanceID());
            }
        }
        else EditorPrefs.SetInt(appKey + id + "_Count", 0);
    }

    private void SetPref(string id, Object val)
    {
        if (val != null) EditorPrefs.SetInt(appKey + id, val.GetInstanceID());
    }
}