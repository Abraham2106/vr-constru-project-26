using System.Linq;
using UnityEditor;
using UnityEngine;
using Vivienda.Tools;

public static class Agente4PaintableWallTagger
{
    [MenuItem("Vivienda/Agente 4/Tag muros pintables (PaintableWall)")]
    public static void TagPaintableWalls()
    {
        var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var added = 0;
        foreach (var r in renderers)
        {
            if (r.GetComponent<PaintableWall>() != null)
                continue;
            var matName = r.sharedMaterial != null ? r.sharedMaterial.name : r.gameObject.name;
            if (!IsPaintableName(matName) && !IsPaintableName(r.gameObject.name))
                continue;
            Undo.AddComponent<PaintableWall>(r.gameObject);
            added++;
        }

        Debug.Log($"[Agente4] PaintableWall añadido a {added} objetos.");
    }

    static bool IsPaintableName(string name)
    {
        var n = name.ToLowerInvariant();
        return n.Contains("wall") || n.Contains("plaster") || n.Contains("blanco") ||
               n.Contains("microcemento") || n.Contains("pintur") || n.Contains("paint");
    }
}
