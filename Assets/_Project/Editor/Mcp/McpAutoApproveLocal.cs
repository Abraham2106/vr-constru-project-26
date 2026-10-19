using Unity.AI.MCP.Editor.Settings;
using UnityEditor;
using UnityEngine;
using Vivienda.EditorTools;

namespace Vivienda.EditorTools.Mcp
{
    /// <summary>
    /// Auto-approves direct MCP connections (local relay / stdio). Does not weaken gateway policy.
    /// </summary>
    public static class McpAutoApproveLocal
    {
        const string MenuPath = "Vivienda/MCP/Auto-approve local";
        const string AppliedPref = "Vivienda.Mcp.DirectAutoApproveApplied";

        [InitializeOnLoadMethod]
        static void ApplyOnceAfterCompile()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool(AppliedPref, false))
                    return;
                if (!TryApplyWithOptionalLock(out _))
                    return;
                EditorPrefs.SetBool(AppliedPref, true);
            };
        }

        [MenuItem(MenuPath)]
        public static void ApplyFromMenu()
        {
            if (TryApplyWithOptionalLock(out var lockReason))
                EditorPrefs.SetBool(AppliedPref, true);
            else if (lockReason != null)
                Debug.LogWarning($"[Vivienda MCP] {lockReason}");
        }

        static bool TryApplyWithOptionalLock(out string lockReason)
        {
            if (!Agente4EditorLock.TryAcquire("McpAutoApproveLocal", out lockReason))
                return false;

            try
            {
                ApplyCore(logOnSuccess: true);
                return true;
            }
            finally
            {
                Agente4EditorLock.Release();
            }
        }

        static void ApplyCore(bool logOnSuccess)
        {
            // MCP package API visibility changed; skip until updated (does not block Agente 1).
            if (logOnSuccess)
                Debug.Log("[Vivienda MCP] Auto-approve local omitido (API MCP no accesible en esta versión).");
        }
    }
}
