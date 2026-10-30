using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Rebuilds the reviewed material registry without changing material GUIDs or scene bindings.</summary>
public static class BlenderMaterialLibrary
{
    const string Root = "Assets/_Project/Art/BlenderNuevo";

    [Serializable] class Registry { public Spec[] unique; }
    [Serializable] class Spec
    {
        public string id, @base, normal, ms;
        public float metallic, smoothness;
        public float[] scale, offset;
    }

    [MenuItem("Vivienda/Integracion Blender/Reconstruir biblioteca de materiales")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying || Lightmapping.isRunning)
            throw new InvalidOperationException("Editor ocupado.");
        var registry = JsonUtility.FromJson<Registry>(File.ReadAllText(Root + "/materiales_preparados.json"));
        if (registry?.unique == null || registry.unique.Length == 0)
            throw new InvalidDataException("Registro de materiales vacio.");
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) throw new InvalidOperationException("URP Lit no esta disponible.");
        foreach (var s in registry.unique)
        {
            ConfigureTexture(s.@base, false, true);
            ConfigureTexture(s.normal, true, false);
            ConfigureTexture(s.ms, false, false);
            var path = Root + "/Materials/Importados/M_" + s.id + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(s.@base));
            m.SetTextureScale("_BaseMap", new Vector2(s.scale[0], s.scale[1]));
            m.SetTextureOffset("_BaseMap", new Vector2(s.offset[0], s.offset[1]));
            m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(s.normal));
            m.SetFloat("_BumpScale", 1);
            m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(s.ms));
            m.SetFloat("_Metallic", s.metallic);
            m.SetFloat("_Smoothness", string.IsNullOrEmpty(s.ms) ? s.smoothness : 1);
            if (string.IsNullOrEmpty(s.ms)) m.DisableKeyword("_METALLICSPECGLOSSMAP");
            else m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Bakes" }))
            ConfigureTexture(AssetDatabase.GUIDToAssetPath(guid), false, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"Biblioteca Blender: {registry.unique.Length} materiales reconstruidos.");
    }

    static void ConfigureTexture(string path, bool normal, bool srgb)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (!path.StartsWith(Root + "/", StringComparison.Ordinal))
            throw new InvalidDataException("Textura fuera de la biblioteca: " + path);
        var t = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!t) throw new FileNotFoundException("Textura no importada", path);
        var type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        int androidSize = normal || !srgb ? 1024 : 2048;
        var android = t.GetPlatformTextureSettings("Android");
        bool changed = t.textureType != type || t.sRGBTexture != srgb || t.maxTextureSize != 2048 ||
            !android.overridden || android.maxTextureSize != androidSize || android.format != TextureImporterFormat.ASTC_6x6;
        if (!changed) return;
        t.textureType = type; t.sRGBTexture = srgb; t.maxTextureSize = 2048;
        android.overridden = true; android.maxTextureSize = androidSize; android.format = TextureImporterFormat.ASTC_6x6;
        t.SetPlatformTextureSettings(android);
        t.SaveAndReimport();
    }
}
