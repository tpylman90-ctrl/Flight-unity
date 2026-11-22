using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Creates the deterministic one-building inspection scene and Android APK.</summary>
public static class BuildingInspectionBuild
{
    private const string ScenePath = "Assets/Scenes/BuildingInspection.unity";
    private const string BuildingPath = "Assets/Resources/Inspection/HaloSpire.fbx";
    private const string SettingsPath = "Assets/Settings/InspectionURP.asset";
    private const string RendererPath = "Assets/Settings/InspectionRenderer.asset";
    private const string MaterialFolder = "Assets/Resources/InspectionMaterials";
    private const string ApkPath = "build/Android/Flight-unity-Inspection.apk";

    [MenuItem("Flight Inspection/Create Building Viewer")]
    public static void PrepareScene()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Settings");
        EnsureFolder(MaterialFolder);
        AssetDatabase.ImportAsset(BuildingPath, ImportAssetOptions.ForceUpdate);

        UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }

        UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(SettingsPath);
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, SettingsPath);
        }
        pipeline.renderScale = 1f;
        pipeline.supportsHDR = true;
        pipeline.msaaSampleCount = 2;
        pipeline.shadowDistance = 78f;
        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        EditorUtility.SetDirty(pipeline);

        Material ceramic = EnsureMaterial("Ceramic", new Color(0.48f, 0.64f, 0.73f), 0.52f, 0.30f);
        Material carbon = EnsureMaterial("Carbon", new Color(0.025f, 0.050f, 0.084f), 0.35f, 0.38f);
        Material glass = EnsureMaterial("Glass", new Color(0.028f, 0.16f, 0.25f), 0.62f, 0.18f);
        Material cyanWindow = EnsureMaterial("CyanWindow", new Color(0.025f, 0.28f, 0.38f), 0.18f, 0.24f, new Color(0.02f, 0.38f, 0.58f), 0.72f);
        Material warmWindow = EnsureMaterial("WarmWindow", new Color(0.56f, 0.22f, 0.07f), 0.12f, 0.26f, new Color(0.92f, 0.18f, 0.03f), 0.62f);
        Material cyanSignal = EnsureMaterial("CyanSignal", new Color(0.012f, 0.44f, 0.62f), 0.12f, 0.24f, new Color(0.01f, 0.50f, 0.82f), 1.25f);
        Material amberSignal = EnsureMaterial("AmberSignal", new Color(0.60f, 0.18f, 0.045f), 0.12f, 0.28f, new Color(0.86f, 0.12f, 0.018f), 0.82f);
        Material foundation = EnsureMaterial("Foundation", new Color(0.075f, 0.11f, 0.16f), 0.18f, 0.62f);
        Material ground = EnsureMaterial("Ground", new Color(0.040f, 0.071f, 0.11f), 0.12f, 0.72f);
        Material paving = EnsureMaterial("Paving", new Color(0.12f, 0.18f, 0.24f), 0.20f, 0.56f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupAtmosphere();
        CreateGround(ground, paving, cyanSignal, amberSignal);

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(BuildingPath);
        if (source == null) throw new Exception("Blender building FBX did not import: " + BuildingPath);
        GameObject building = (GameObject)PrefabUtility.InstantiatePrefab(source);
        building.name = "Halo Spire | Blender architectural study";
        building.transform.position = Vector3.zero;
        building.transform.rotation = Quaternion.identity;
        building.transform.localScale = Vector3.one;
        ApplyBuildingMaterials(building, ceramic, carbon, glass, cyanWindow, warmWindow, cyanSignal, amberSignal, foundation);

        var focus = new GameObject("Inspection Focus");
        focus.transform.position = new Vector3(0f, 30.5f, 0f);
        var cameraObject = new GameObject("Inspection Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 43f;
        camera.nearClipPlane = 0.08f;
        camera.farClipPlane = 260f;
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.AddComponent<AudioListener>();
        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;

        var viewer = new GameObject("Building Inspection Controls");
        viewer.AddComponent<BuildingInspectionViewer>();
        CreatePostProcessing();

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new Exception("Could not save building inspection scene.");
        AssetDatabase.SaveAssets();
        Debug.Log("Building inspection scene created: " + ScenePath);
    }

    private static void ApplyBuildingMaterials(GameObject building, Material ceramic, Material carbon,
        Material glass, Material cyanWindow, Material warmWindow, Material cyanSignal,
        Material amberSignal, Material foundation)
    {
        foreach (Renderer renderer in building.GetComponentsInChildren<Renderer>(true))
        {
            Material[] source = renderer.sharedMaterials;
            var mapped = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                string name = source[i] != null ? source[i].name.ToLowerInvariant() : "";
                if (name.Contains("illuminated cyan")) mapped[i] = cyanWindow;
                else if (name.Contains("warm interior")) mapped[i] = warmWindow;
                else if (name.Contains("cyan ceramic")) mapped[i] = cyanSignal;
                else if (name.Contains("amber light")) mapped[i] = amberSignal;
                else if (name.Contains("warm titanium")) mapped[i] = ceramic;
                else if (name.Contains("midnight composite")) mapped[i] = carbon;
                else if (name.Contains("arctic blue")) mapped[i] = glass;
                else if (name.Contains("basalt")) mapped[i] = foundation;
                else mapped[i] = ceramic;
            }
            renderer.sharedMaterials = mapped;
        }
    }

    private static void SetupAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0028f;
        RenderSettings.fogColor = new Color(0.18f, 0.29f, 0.42f);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.36f, 0.52f, 0.69f);
        RenderSettings.ambientEquatorColor = new Color(0.24f, 0.34f, 0.46f);
        RenderSettings.ambientGroundColor = new Color(0.045f, 0.073f, 0.11f);

        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader != null)
        {
            var sky = new Material(skyShader) { name = "Inspection | blue-hour sky" };
            if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", new Color(0.52f, 0.70f, 0.86f));
            if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", new Color(0.12f, 0.18f, 0.26f));
            if (sky.HasProperty("_AtmosphereThickness")) sky.SetFloat("_AtmosphereThickness", 1.15f);
            if (sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", 1.12f);
            RenderSettings.skybox = sky;
        }

        GameObject sunObject = new GameObject("Large soft key light");
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.78f, 0.88f, 1.0f);
        sun.intensity = 1.45f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.72f;
        sunObject.transform.rotation = Quaternion.Euler(39f, -32f, 0f);
        RenderSettings.sun = sun;
    }

    private static void CreateGround(Material ground, Material paving, Material cyan, Material amber)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Open inspection plaza | continuous ground";
        floor.transform.position = new Vector3(0f, -0.08f, 0f);
        floor.transform.localScale = new Vector3(36f, 1f, 36f);
        floor.GetComponent<Renderer>().sharedMaterial = ground;
        Object.DestroyImmediate(floor.GetComponent<Collider>());

        CreateRing("Outer cyan plaza inlay", 25.2f, 25.08f, 0.015f, cyan);
        CreateRing("Inner warm plaza inlay", 22.5f, 22.42f, 0.018f, amber);
        CreateRing("Foundation approach ring", 18.6f, 18.5f, 0.025f, paving);

        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2f / 16f;
            float radius = 27.5f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0.035f, Mathf.Sin(angle) * radius);
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "Radial plaza marker " + (i + 1);
            tile.transform.position = position;
            tile.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            tile.transform.localScale = new Vector3(0.52f, 0.08f, 2.0f);
            tile.GetComponent<Renderer>().sharedMaterial = i % 4 == 0 ? cyan : paving;
            Object.DestroyImmediate(tile.GetComponent<Collider>());
        }

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f + Mathf.PI / 8f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * 31f, 2.0f, Mathf.Sin(angle) * 31f);
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Low plaza beacon " + (i + 1);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(0.48f, 2.0f, 0.48f);
            marker.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? cyan : amber;
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }
    }

    private static void CreateRing(string name, float outerRadius, float innerRadius, float y, Material material)
    {
        const int segments = 160;
        var vertices = new Vector3[segments * 2];
        var triangles = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(a), z = Mathf.Sin(a);
            vertices[i * 2] = new Vector3(x * outerRadius, y, z * outerRadius);
            vertices[i * 2 + 1] = new Vector3(x * innerRadius, y, z * innerRadius);
            int next = (i + 1) % segments;
            int t = i * 6;
            triangles[t] = i * 2;
            triangles[t + 1] = next * 2 + 1;
            triangles[t + 2] = next * 2;
            triangles[t + 3] = i * 2;
            triangles[t + 4] = i * 2 + 1;
            triangles[t + 5] = next * 2 + 1;
        }
        var mesh = new Mesh { name = name + " mesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        var ring = new GameObject(name);
        ring.AddComponent<MeshFilter>().sharedMesh = mesh;
        ring.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void CreatePostProcessing()
    {
        const string profilePath = "Assets/Settings/InspectionVolumeProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }
        Bloom bloom;
        if (!profile.TryGet(out bloom)) bloom = profile.Add<Bloom>(true);
        bloom.threshold.value = 1.15f;
        bloom.intensity.value = 0.42f;
        bloom.scatter.value = 0.58f;
        ColorAdjustments grade;
        if (!profile.TryGet(out grade)) grade = profile.Add<ColorAdjustments>(true);
        grade.postExposure.value = 0.12f;
        grade.contrast.value = 9f;
        grade.saturation.value = 7f;

        var volumeObject = new GameObject("Subtle architectural highlight bloom");
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;
        volume.sharedProfile = profile;
        EditorUtility.SetDirty(profile);
    }

    private static Material EnsureMaterial(string name, Color color, float metallic, float smoothness,
        Color? emission = null, float emissionStrength = 0f)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (emission.HasValue && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value * emissionStrength);
        }
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string[] parts = path.Split('/');
        string parent = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string child = parent + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(child)) AssetDatabase.CreateFolder(parent, parts[i]);
            parent = child;
        }
    }

    /// <summary>Entry point called by the central Unity Android runner.</summary>
    public static void BuildAndroid()
    {
        PrepareScene();
        PlayerSettings.companyName = "Eclipse";
        PlayerSettings.productName = "Flight Unity Building Inspection";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.eclipse.flightunity.inspection");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.optimizedFramePacing = true;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        EditorUserBuildSettings.buildAppBundle = false;
        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
        BuildReport result = BuildPipeline.BuildPlayer(new[] { ScenePath }, ApkPath, BuildTarget.Android, BuildOptions.None);
        if (result.summary.result != BuildResult.Succeeded)
            throw new Exception("Android inspection build failed: " + result.summary.result);
        Debug.Log("Inspection APK generated at " + ApkPath + " (" + result.summary.totalSize + " bytes).");
    }
}
