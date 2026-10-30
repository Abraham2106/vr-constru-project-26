using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ProyectoVR;

// Editor-only temporary bake tool. Keeps lightmap assets and their GUIDs in place.
public static class BlenderIntegrationBake
{
    const string MapRoot = "Assets/_Project/Art/BlenderNuevo/Lighting";
    const string StatusPath = "Validation/BlenderIntegrationBake.status";
    const string ReportPath = "Validation/BlenderIntegrationBake.report.txt";
    static BakedDayNightLighting baked;
    static DayNightController controller;
    static BakedDayNightLighting.Scenario oldDay, oldNight;
    static Material originalSky, temporarySky;
    static Light sun;
    static Light[] access;
    static LightState[] lights;
    static AmbientMode ambientMode;
    static Color ambientSky, ambientEquator, ambientGround;
    static float ambientIntensity, reflectionIntensity;
    static bool oldIsNight, active, pendingStart;
    static int stage;
    static double lastActivity;
    static string backupRoot;
    static readonly Dictionary<string,string> backups = new();
    static readonly List<string> report = new();

    struct LightState
    {
        public Light light;
        public bool enabled;
        public Color color;
        public float intensity;
        public Quaternion rotation;
    }

    [MenuItem("Vivienda/Integracion Blender/Hornear dia y atardecer")]
    public static void Begin()
    {
        if (active || Lightmapping.isRunning || EditorApplication.isPlaying || EditorApplication.isCompiling)
            throw new InvalidOperationException("Editor ocupado.");
        if (SessionState.GetInt("Vivienda.RealismBake", 0) != 0)
            throw new InvalidOperationException("Otro horneado por escenarios esta activo.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Vivienda_Integracion_Blender.unity")
            throw new InvalidOperationException("Escena inesperada: " + scene.path);
        baked = UnityEngine.Object.FindAnyObjectByType<BakedDayNightLighting>();
        controller = UnityEngine.Object.FindAnyObjectByType<DayNightController>();
        if (!baked || !controller || baked.receivers.Any(x => !x))
            throw new InvalidOperationException("Faltan receptores o controlador de iluminacion.");
        oldDay = baked.day; oldNight = baked.night; oldIsNight = baked.IsNight;
        originalSky = RenderSettings.skybox;
        ambientMode = RenderSettings.ambientMode;
        ambientSky = RenderSettings.ambientSkyColor;
        ambientEquator = RenderSettings.ambientEquatorColor;
        ambientGround = RenderSettings.ambientGroundColor;
        ambientIntensity = RenderSettings.ambientIntensity;
        reflectionIntensity = RenderSettings.reflectionIntensity;
        lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
            .Select(x => new LightState { light=x, enabled=x.enabled, color=x.color,
                intensity=x.intensity, rotation=x.transform.rotation }).ToArray();
        var serialized = new SerializedObject(controller);
        sun = serialized.FindProperty("directionalLight").objectReferenceValue as Light;
        if (!sun) throw new InvalidOperationException("No se encontro el sol.");
        var list = serialized.FindProperty("exteriorNightLights");
        access = new Light[list.arraySize];
        for (int i=0;i<access.Length;i++) access[i] = list.GetArrayElementAtIndex(i).objectReferenceValue as Light;
        Directory.CreateDirectory("Validation");
        backupRoot = Path.GetFullPath("Library/BlenderIntegrationBakeBackup/" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(backupRoot);
        backups.Clear(); report.Clear();
        // Native bake rewrites its scene output too; retain these bytes for failure recovery.
        var nativeFolder = Path.Combine(Path.GetDirectoryName(scene.path), Path.GetFileNameWithoutExtension(scene.path));
        if (Directory.Exists(nativeFolder)) foreach (var file in Directory.GetFiles(nativeFolder, "*", SearchOption.AllDirectories)) Backup(file.Replace('\\','/'));
        if (Directory.Exists(MapRoot)) foreach (var file in Directory.GetFiles(MapRoot, "*", SearchOption.AllDirectories)) Backup(file.Replace('\\','/'));
        active = true; pendingStart = false; stage = 1;
        Lightmapping.bakeCompleted += Completed;
        EditorApplication.update += Tick;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        try { SetEnvironment(false); StartBake(); }
        catch (Exception ex) { Fail(ex); throw; }
    }

    static void Backup(string path)
    {
        path = path.Replace('\\','/');
        if (backups.ContainsKey(path)) return;
        if (!File.Exists(path)) { backups[path] = null; return; }
        var target = Path.Combine(backupRoot, path);
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        File.Copy(path,target,true); backups[path]=target;
    }

    static void StartBake()
    {
        pendingStart=false;lastActivity=EditorApplication.timeSinceStartup;
        File.WriteAllText(StatusPath,stage==1?"BAKING DAY":"BAKING NIGHT");
        if (!Lightmapping.BakeAsync()) throw new InvalidOperationException("BakeAsync no inicio.");
        Debug.Log("Blender integration: horneado " + (stage==1?"DIA":"NOCHE") + " iniciado.");
    }

    static void Tick()
    {
        if (!active || Lightmapping.isRunning || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (pendingStart)
        {
            try { SetEnvironment(true); StartBake(); }
            catch (Exception ex) { Fail(ex); }
        }
        else if (EditorApplication.timeSinceStartup-lastActivity>30)
            Fail(new InvalidOperationException("El horneado termino o se cancelo sin emitir bakeCompleted."));
    }

    static void Completed()
    {
        if(!active)return;
        try
        {
            var scenario = CaptureScenario(stage);
            report.Add((stage==1?"DAY":"NIGHT") + " maps=" + scenario.color.Length +
                " receivers="+scenario.indices.Length+" probes="+(scenario.probeCoefficients.Length/27));
            if(stage==1)
            {
                baked.day=scenario; stage=2; pendingStart=true;
                lastActivity=EditorApplication.timeSinceStartup;
                File.WriteAllText(StatusPath,"DAY DONE; NIGHT PENDING");
                return;
            }
            baked.night=scenario;
            EditorUtility.SetDirty(baked);
            Finish(true);
        }
        catch(Exception ex) { Fail(ex); }
    }

    static BakedDayNightLighting.Scenario CaptureScenario(int mode)
    {
        var maps=LightmapSettings.lightmaps;
        if(maps.Length==0)throw new InvalidOperationException("Horneado sin lightmaps.");
        var result=new BakedDayNightLighting.Scenario {
            color=new Texture2D[maps.Length], direction=new Texture2D[maps.Length],
            indices=baked.receivers.Select(x=>x.lightmapIndex).ToArray(),
            scaleOffsets=baked.receivers.Select(x=>x.lightmapScaleOffset).ToArray()
        };
        for(int i=0;i<maps.Length;i++)
        {
            result.color[i]=CopyMap(maps[i].lightmapColor,mode,i,"Color");
            result.direction[i]=CopyMap(maps[i].lightmapDir,mode,i,"Direction");
            if(!result.color[i]||!result.direction[i])throw new InvalidOperationException("Mapa color/direccion ausente.");
        }
        var probes=LightmapSettings.lightProbes;
        if(probes)
        {
            var sh=probes.bakedProbes;result.probeCoefficients=new float[sh.Length*27];
            for(int i=0;i<sh.Length;i++)for(int channel=0;channel<3;channel++)for(int c=0;c<9;c++)
                result.probeCoefficients[i*27+channel*9+c]=sh[i][channel,c];
        }
        return result;
    }

    static Texture2D CopyMap(Texture2D source,int mode,int index,string suffix)
    {
        if(!source)return null;
        string src=AssetDatabase.GetAssetPath(source);
        string dst=MapRoot+(mode==1?"/Dia/":"/Noche/")+index+"_"+suffix+Path.GetExtension(src);
        if(string.IsNullOrEmpty(src)||!File.Exists(src))throw new IOException("Mapa origen inexistente: "+src);
        Directory.CreateDirectory(Path.GetDirectoryName(dst));Backup(dst);Backup(dst+".meta");
        string guidBefore=AssetDatabase.AssetPathToGUID(dst);
        if(Path.GetFullPath(src)!=Path.GetFullPath(dst))File.Copy(src,dst,true); // Preserve .meta and importer settings, including Android compression.
        AssetDatabase.ImportAsset(dst,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        CopyNativeImportSettings(src, dst);
        string guidAfter=AssetDatabase.AssetPathToGUID(dst);
        if(!string.IsNullOrEmpty(guidBefore)&&guidBefore!=guidAfter)throw new InvalidOperationException("GUID modificado: "+dst);
        report.Add("MAP "+dst+" GUID="+guidAfter);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
    }

    // A copied lightmap must retain Unity's lightmap encoding and linear direction data.
    // Copying only the image bytes imports it as an ordinary color texture.
    public static void CopyNativeImportSettings(string source, string destination)
    {
        var input = AssetImporter.GetAtPath(source) as TextureImporter;
        var output = AssetImporter.GetAtPath(destination) as TextureImporter;
        if (!input || !output) throw new IOException("Importador de lightmap ausente.");
        var settings = new TextureImporterSettings();
        input.ReadTextureSettings(settings);
        output.SetTextureSettings(settings);
        output.textureCompression = input.textureCompression;
        output.compressionQuality = input.compressionQuality;
        output.maxTextureSize = input.maxTextureSize;
        output.SetPlatformTextureSettings(input.GetDefaultPlatformTextureSettings());
        output.SetPlatformTextureSettings(input.GetPlatformTextureSettings("Android"));
        output.SaveAndReimport();
    }

    static void SetEnvironment(bool night)
    {
        var data=new SerializedObject(controller);var snap=data.FindProperty(night?"night":"day");
        sun.color=snap.FindPropertyRelative("sunColor").colorValue;
        sun.intensity=snap.FindPropertyRelative("sunIntensity").floatValue;
        sun.transform.rotation=Quaternion.Euler(snap.FindPropertyRelative("sunEuler").vector3Value);sun.enabled=true;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=snap.FindPropertyRelative("ambientSky").colorValue;
        RenderSettings.ambientEquatorColor=snap.FindPropertyRelative("ambientEquator").colorValue;
        RenderSettings.ambientGroundColor=snap.FindPropertyRelative("ambientGround").colorValue;
        RenderSettings.ambientIntensity=snap.FindPropertyRelative("ambientIntensity").floatValue;
        RenderSettings.reflectionIntensity=snap.FindPropertyRelative("reflectionIntensity").floatValue;
        if (temporarySky) UnityEngine.Object.DestroyImmediate(temporarySky);
        var sourceSky = data.FindProperty(night ? "sunsetSkybox" : "skyboxMaterial").objectReferenceValue as Material;
        if (!sourceSky) sourceSky = originalSky;
        if (sourceSky) {
            temporarySky = new Material(sourceSky) { name="Blender integration temporary sky", hideFlags=HideFlags.HideAndDontSave };
            RenderSettings.skybox = temporarySky;
            // Match DayNightController: the sunset uses its own authored exposure.
            if (!night) {
                if(temporarySky.HasProperty("_Exposure")) temporarySky.SetFloat("_Exposure",snap.FindPropertyRelative("skyboxExposure").floatValue);
                if(temporarySky.HasProperty("_SkyTint")) temporarySky.SetColor("_SkyTint",snap.FindPropertyRelative("skyboxTint").colorValue);
            }
        }
        foreach(var light in access)if(light)light.enabled=night;
        DynamicGI.UpdateEnvironment();
    }

    static void Fail(Exception ex)
    {
        if(!active)return;
        report.Add("FAIL "+ex);Debug.LogException(ex);
        Finish(false);
    }

    static void BeforeReload()
    {
        if(!active)return;
        if(Lightmapping.isRunning)Lightmapping.Cancel();
        Fail(new InvalidOperationException("Recarga de assemblies: se restauro el estado previo."));
    }

    static void Finish(bool success)
    {
        active=false;pendingStart=false;
        Lightmapping.bakeCompleted-=Completed;EditorApplication.update-=Tick;
        AssemblyReloadEvents.beforeAssemblyReload-=BeforeReload;
        try
        {
            if(!success)
            {
                foreach(var pair in backups)if(pair.Value!=null)File.Copy(pair.Value,pair.Key,true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                baked.day=oldDay;baked.night=oldNight;
            }
        }
        finally
        {
            RenderSettings.skybox=originalSky;
            RenderSettings.ambientMode=ambientMode;RenderSettings.ambientSkyColor=ambientSky;
            RenderSettings.ambientEquatorColor=ambientEquator;RenderSettings.ambientGroundColor=ambientGround;
            RenderSettings.ambientIntensity=ambientIntensity;RenderSettings.reflectionIntensity=reflectionIntensity;
            foreach(var state in lights)if(state.light)
            {
                state.light.enabled=state.enabled;state.light.color=state.color;
                state.light.intensity=state.intensity;state.light.transform.rotation=state.rotation;
            }
            if(temporarySky)UnityEngine.Object.DestroyImmediate(temporarySky);
            temporarySky=null;
            if(baked)baked.Apply(success?false:oldIsNight);
            DynamicGI.UpdateEnvironment();
        }
        if(success)
        {
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(baked.gameObject.scene);
            if(!EditorSceneManager.SaveScene(baked.gameObject.scene))throw new IOException("No se pudo guardar la escena despues del bake.");
            report.Add("PASS environment restored; original sky material was never modified.");
        }
        File.WriteAllLines(ReportPath,report);
        File.WriteAllText(StatusPath,success?"DONE":"FAILED - RESTORED PREVIOUS MAPS AND SCENARIOS");
        Debug.Log("Blender integration bake: "+(success?"DONE":"FAILED; estado restaurado"));
    }
}
