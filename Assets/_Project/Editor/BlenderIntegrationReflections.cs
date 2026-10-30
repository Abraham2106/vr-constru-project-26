using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ProyectoVR;
public static class BlenderIntegrationReflections {
    [MenuItem("Vivienda/Integracion Blender/Actualizar reflejos dia y atardecer")]
    public static void Bake() {
        if(EditorApplication.isPlaying||Lightmapping.isRunning)throw new Exception("Editor ocupado");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/Vivienda_Integracion_Blender.unity")throw new Exception("Escena incorrecta");
        var controller=UnityEngine.Object.FindAnyObjectByType<DayNightController>();
        var baked=controller.GetComponent<BakedDayNightLighting>();
        var data=new SerializedObject(controller);
        var entries=data.FindProperty("reflectionScenarios");
        var sun=(Light)data.FindProperty("directionalLight").objectReferenceValue;
        var originalSky=RenderSettings.skybox;
        Material tempSky=null;
        var sunColor=sun.color;
        float sunIntensity=sun.intensity;
        var sunRotation=sun.transform.rotation;
        bool sunEnabled=sun.enabled;
        var ambientMode=RenderSettings.ambientMode;
        var ambientSky=RenderSettings.ambientSkyColor;
        var ambientEq=RenderSettings.ambientEquatorColor;
        var ambientGround=RenderSettings.ambientGroundColor;
        float ambientIntensity=RenderSettings.ambientIntensity,reflectionIntensity=RenderSettings.reflectionIntensity;
        var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        var enabled=lights.Select(x=>x.enabled).ToArray();
        string originalController=EditorJsonUtility.ToJson(controller);
        bool ok=false;
        var report=new System.Collections.Generic.List<string>();
        try {
            foreach(bool night in new[] { false, true }) {
                if(tempSky)UnityEngine.Object.DestroyImmediate(tempSky);
                var sourceSky=data.FindProperty(night?"sunsetSkybox":"skyboxMaterial").objectReferenceValue as Material;
                tempSky=new Material(sourceSky?sourceSky:originalSky);
                RenderSettings.skybox=tempSky;
                baked.Apply(night);
                var snapshot=data.FindProperty(night?"night":"day");
                sun.color=snapshot.FindPropertyRelative("sunColor").colorValue;
                sun.intensity=snapshot.FindPropertyRelative("sunIntensity").floatValue;
                sun.transform.rotation=Quaternion.Euler(snapshot.FindPropertyRelative("sunEuler").vector3Value);
                sun.enabled=true;
                RenderSettings.ambientMode=AmbientMode.Trilight;
                RenderSettings.ambientSkyColor=snapshot.FindPropertyRelative("ambientSky").colorValue;
                RenderSettings.ambientEquatorColor=snapshot.FindPropertyRelative("ambientEquator").colorValue;
                RenderSettings.ambientGroundColor=snapshot.FindPropertyRelative("ambientGround").colorValue;
                RenderSettings.ambientIntensity=snapshot.FindPropertyRelative("ambientIntensity").floatValue;
                RenderSettings.reflectionIntensity=snapshot.FindPropertyRelative("reflectionIntensity").floatValue;
                if(!night&&tempSky.HasProperty("_Exposure"))tempSky.SetFloat("_Exposure",snapshot.FindPropertyRelative("skyboxExposure").floatValue);
                if(!night&&tempSky.HasProperty("_SkyTint"))tempSky.SetColor("_SkyTint",snapshot.FindPropertyRelative("skyboxTint").colorValue);
                var access=data.FindProperty("exteriorNightLights");
                for(int i=0;i<access.arraySize;i++)if(access.GetArrayElementAtIndex(i).objectReferenceValue is Light l)l.enabled=night;
                for(int i=0;i<entries.arraySize;i++) {
                    var e=entries.GetArrayElementAtIndex(i);
                    var p=e.FindPropertyRelative("probe").objectReferenceValue as ReflectionProbe;
                    if(!p)continue;
                    string prefix="Probe_"+i;
                    string root="Assets/_Project/Art/BlenderNuevo/Lighting/Reflections";
                    Directory.CreateDirectory(root);
                    string path=root+"/"+prefix+(night?"_Noche.exr":"_Dia.exr");
                    if(!Lightmapping.BakeReflectionProbe(p,path))throw new Exception("No se pudo hornear "+p.name);
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                    var cube=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                    if(!cube)throw new Exception("Cubemap ausente "+path);
                    e.FindPropertyRelative(night?"night":"day").objectReferenceValue=cube;
                    report.Add("PASS "+p.name+" "+path+" "+cube.width+"px");
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                data.Update();
                entries=data.FindProperty("reflectionScenarios");
            }
            ok=true;
        }
        catch {
            EditorJsonUtility.FromJsonOverwrite(originalController,controller);
            throw;
        }
        finally {
            RenderSettings.skybox=originalSky;
            sun.color=sunColor;
            sun.intensity=sunIntensity;
            sun.transform.rotation=sunRotation;
            sun.enabled=sunEnabled;
            RenderSettings.ambientMode=ambientMode;
            RenderSettings.ambientSkyColor=ambientSky;
            RenderSettings.ambientEquatorColor=ambientEq;
            RenderSettings.ambientGroundColor=ambientGround;
            RenderSettings.ambientIntensity=ambientIntensity;
            RenderSettings.reflectionIntensity=reflectionIntensity;
            for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=enabled[i];
            baked.ApplyDay();
            UnityEngine.Object.DestroyImmediate(tempSky);
            if(ok) {
                var restored=new SerializedObject(controller);
                var es=restored.FindProperty("reflectionScenarios");
                for(int i=0;i<es.arraySize;i++) {
                    var e=es.GetArrayElementAtIndex(i);
                    var p=e.FindPropertyRelative("probe").objectReferenceValue as ReflectionProbe;
                    if(p)p.customBakedTexture=e.FindPropertyRelative("day").objectReferenceValue as Texture;
                }
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                EditorSceneManager.SaveScene(controller.gameObject.scene);
                Directory.CreateDirectory("Validation");
                File.WriteAllLines("Validation/BlenderIntegrationReflections.txt",report);
            }
        }
        foreach(var line in report)Debug.Log(line);
        Debug.Log("Reflejos actualizados conservando 6 escenarios y configuración de luz/ambiente.");
    }
}
