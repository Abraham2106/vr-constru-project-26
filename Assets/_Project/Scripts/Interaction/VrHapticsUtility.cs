using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ProyectoVR
{
    public static class VrHapticsUtility
    {
        public const float LightPulseAmplitude = 0.25f;
        public const float LightPulseDuration = 0.04f;
        public const float SwitchClickAmplitude = 0.35f;
        public const float SwitchClickDuration = 0.03f;

        public static void PulseInteractor(IXRSelectInteractor interactor, float amplitude, float duration)
        {
            if (interactor is not Object unityObj)
                return;

            var go = (unityObj as Component)?.gameObject;
            if (go == null)
                return;

            var player = go.GetComponentInParent<HapticImpulsePlayer>(true);
            player?.SendHapticImpulse(amplitude, duration);
        }

        public static void PulseFromInteractable(XRBaseInteractable interactable, float amplitude, float duration)
        {
            if (interactable == null || !interactable.isSelected)
                return;

            var interactor = interactable.firstInteractorSelecting;
            if (interactor != null)
                PulseInteractor(interactor, amplitude, duration);
        }
    }
}
