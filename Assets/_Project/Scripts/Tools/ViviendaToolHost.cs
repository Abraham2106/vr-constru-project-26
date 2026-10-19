using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vivienda.Tools
{
    /// <summary>Punto de integración XR: rayo del mando derecho + activación por menú.</summary>
    [DisallowMultipleComponent]
    public class ViviendaToolHost : MonoBehaviour
    {
        public static ViviendaToolHost Instance { get; private set; }

        [Header("Raycast")]
        [SerializeField] Transform rightControllerRay;
        [SerializeField] LayerMask raycastMask = ~0;

        [Header("Herramientas")]
        [SerializeField] MeasureTool measureTool;
        [SerializeField] WallPaintTool wallPaintTool;

        [SerializeField] InputActionReference triggerAction;
        [SerializeField] InputActionReference secondaryAction;

        readonly Dictionary<string, IViviendaTool> toolsById = new();
        IViviendaTool activeTool;

        public MeasureTool Measure => measureTool;
        public WallPaintTool WallPaint => wallPaintTool;
        public string ActiveToolId => activeTool?.ToolId ?? ViviendaToolIds.None;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ViviendaToolHost] Instancia duplicada; se ignora.");
                return;
            }
            Instance = this;
            RegisterTools();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void RegisterTools()
        {
            toolsById.Clear();
            if (measureTool == null)
                measureTool = GetComponentInChildren<MeasureTool>(true) ?? gameObject.AddComponent<MeasureTool>();
            if (wallPaintTool == null)
                wallPaintTool = GetComponentInChildren<WallPaintTool>(true) ?? gameObject.AddComponent<WallPaintTool>();

            Register(measureTool);
            Register(wallPaintTool);
            DeactivateAll();
        }

        void Register(IViviendaTool tool)
        {
            if (tool == null || string.IsNullOrEmpty(tool.ToolId))
                return;
            toolsById[tool.ToolId] = tool;
        }

        float suppressedUntil;
        public void SuppressInput(){suppressedUntil=Time.unscaledTime+0.3f;}

        public void ActivateTool(string toolId)
        {
            SuppressInput();
            DeactivateAll();
            if (string.IsNullOrEmpty(toolId))
                return;

            if (toolsById.TryGetValue(toolId, out var tool))
            {
                tool.SetActive(true);
                activeTool = tool;
            }
        }

        public void DeactivateAll()
        {
            foreach (var kv in toolsById)
                kv.Value.SetActive(false);
            activeTool = null;
        }

        public void NotifyTriggerPressed()
        {
            if (activeTool == null || Time.unscaledTime < suppressedUntil || ProyectoVR.UI.VrWorldMenuController.AnyMenuVisible)
                return;

            var hit = ToolRaycastUtility.RaycastFrom(rightControllerRay, raycastMask);
            if (hit.HasHit && hit.Target != null && hit.Target.GetComponentInParent<ProyectoVR.LightSwitch>() != null) return;
            activeTool.OnTriggerDown(hit);
        }

        public void NotifySecondaryPressed()
        {
            if(Time.unscaledTime < suppressedUntil || ProyectoVR.UI.VrWorldMenuController.AnyMenuVisible)return;
            activeTool?.OnSecondaryDown();
        }

        void Update()
        {
            if (triggerAction != null && triggerAction.action.WasPressedThisFrame()) NotifyTriggerPressed();
            if (secondaryAction != null && secondaryAction.action.WasPressedThisFrame()) NotifySecondaryPressed();
        }
    }
}
