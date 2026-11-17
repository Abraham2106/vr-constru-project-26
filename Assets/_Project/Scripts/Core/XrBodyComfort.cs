using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace ProyectoVR.Core
{
    /// <summary>
    /// Altura y comodidad del recorrido VR:
    /// - En visor real pide seguimiento desde el piso, para ver la casa a la altura real de la persona.
    /// - La capsula del jugador sigue la altura de la cabeza y nunca crece contra un techo o viga.
    /// - Si la cabeza entra en una pared o techo, la vista se oscurece en lugar de mostrar el interior del muro.
    /// - Conecta la vineta de tunel a los movimientos continuos (sin esto nunca se activaba).
    /// </summary>
    [DefaultExecutionOrder(250)]
    [RequireComponent(typeof(XROrigin), typeof(CharacterController))]
    public class XrBodyComfort : MonoBehaviour
    {
        [SerializeField] bool requestFloorTracking = true;
        [Tooltip("Altura de los ojos en simulador y escritorio (en visor real se usa la altura real de la persona).")]
        [SerializeField] float simulatedEyeHeight = 1.55f;
        [Tooltip("Radio de la capsula: mas delgada pasa por puertas y pasillos sin rozar.")]
        [SerializeField] float bodyRadius = 0.22f;
        [Tooltip("Campo de vision vertical en simulador/escritorio. 60 se siente estrecho; el visor usa el suyo propio.")]
        [SerializeField] float desktopFieldOfView = 74f;
        [SerializeField] float minBodyHeight = 1.0f;
        [SerializeField] float maxBodyHeight = 2.05f;
        [Tooltip("Espacio entre los ojos y la parte superior de la capsula.")]
        [SerializeField] float headTop = 0.12f;
        [SerializeField] float headRadius = 0.11f;
        [SerializeField] float maxHeadFade = 0.92f;

        static readonly int HeadMask = WalkableLayerMask.Value;

        XROrigin _origin;
        CharacterController _body;
        TeleportFadeController _fade;
        float _headFade;

        void Awake()
        {
            _origin = GetComponent<XROrigin>();
            _body = GetComponent<CharacterController>();
            _fade = GetComponent<TeleportFadeController>();
            _origin.CameraYOffset = simulatedEyeHeight;
            _body.radius = bodyRadius;
        }

        void Start()
        {
            bool realHeadset = !DesktopPreview.IsActive && !DesktopPreview.IsSimulatorActive;
            if (requestFloorTracking && realHeadset)
                _origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            else if (_origin.CameraFloorOffsetObject != null)
            {
                // Simulador/escritorio: el modo de seguimiento queda "Unknown" y XROrigin no aplica la altura,
                // asi que los ojos quedaban a la altura guardada en la escena (1.36 m). Se fija la de un adulto.
                var offset = _origin.CameraFloorOffsetObject.transform;
                var p = offset.localPosition;
                offset.localPosition = new Vector3(p.x, _origin.CameraYOffset, p.z);
                if (_origin.Camera != null) _origin.Camera.fieldOfView = desktopFieldOfView;
            }
            BindVignette();
        }

        void LateUpdate()
        {
            FitBodyToHead();
            FadeWhenHeadInsideGeometry();
        }

        void FitBodyToHead()
        {
            if (_origin.Camera == null) return;
            float eyes = _origin.CameraInOriginSpaceHeight;
            float target = Mathf.Clamp(eyes + headTop, minBodyHeight, maxBodyHeight);
            float current = _body.height;
            if (target > current + 0.005f)
            {
                // Solo crecer si hay espacio: evita que la capsula quede dentro de un techo y salte.
                var feet = transform.position;
                var top = feet + Vector3.up * (current - _body.radius);
                if (Physics.SphereCast(top, _body.radius * 0.95f, Vector3.up, out var hit, target - current,
                        HeadMask, QueryTriggerInteraction.Ignore))
                    target = current + Mathf.Max(0f, hit.distance - 0.01f);
            }
            if (Mathf.Abs(target - current) < 0.005f) return;
            _body.height = target;
            var c = _body.center;
            _body.center = new Vector3(c.x, target * 0.5f, c.z);
        }

        void FadeWhenHeadInsideGeometry()
        {
            if (_fade == null || _fade.FadeImage == null || _fade.IsFading || _origin.Camera == null) return;
            bool inside = Physics.CheckSphere(_origin.Camera.transform.position, headRadius, HeadMask, QueryTriggerInteraction.Ignore);
            float target = inside ? maxHeadFade : 0f;
            if (Mathf.Approximately(_headFade, target)) return;
            _headFade = Mathf.MoveTowards(_headFade, target, Time.unscaledDeltaTime * 4f);
            var color = _fade.FadeImage.color;
            color.a = _headFade;
            _fade.FadeImage.color = color;
        }

        void BindVignette()
        {
            var vignette = GetComponentInChildren<TunnelingVignetteController>(true);
            if (vignette == null) return;
            var list = vignette.locomotionVignetteProviders;
            list.RemoveAll(p => p == null || p.locomotionProvider == null);
            var known = new HashSet<LocomotionProvider>();
            foreach (var p in list) known.Add(p.locomotionProvider);
            foreach (var provider in GetComponentsInChildren<LocomotionProvider>(true))
            {
                bool continuous = provider is ContinuousMoveProvider || provider is ContinuousTurnProvider;
                if (!continuous || known.Contains(provider)) continue;
                list.Add(new LocomotionVignetteProvider { locomotionProvider = provider, enabled = true });
            }
        }
    }
}
