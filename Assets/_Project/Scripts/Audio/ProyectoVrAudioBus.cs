using UnityEngine;
using UnityEngine.Audio;

namespace ProyectoVR
{
    /// <summary>
    /// Referencias al AudioMixer principal del proyecto (generado por Agente 3).
    /// </summary>
    [CreateAssetMenu(fileName = "ProyectoVrAudioBus", menuName = "ProyectoVR/Audio Bus")]
    public class ProyectoVrAudioBus : ScriptableObject
    {
        public const string MixerAssetPath = "Assets/_Project/Audio/ProyectoVR_Main.mixer";
        public const string ParamExtAmbience = "ExtAmbience";

        public AudioMixer mixer;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup ambienteGroup;
        public AudioMixerGroup uiGroup;

        static ProyectoVrAudioBus _instance;

        public static ProyectoVrAudioBus Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = Resources.Load<ProyectoVrAudioBus>("ProyectoVrAudioBus");
                if (_instance == null)
                {
                    _instance = UnityEngine.Object.FindAnyObjectByType<ProyectoVrAudioBusLoader>()?.bus;
                }

                return _instance;
            }
        }

        public void SetExteriorAmbience(float normalizedOpenAmount)
        {
            if (mixer == null)
                return;

            var value = Mathf.Clamp01(normalizedOpenAmount);
            mixer.SetFloat(ParamExtAmbience, Mathf.Lerp(-24f, 0f, value));
        }

        public void RouteToSfx(AudioSource source)
        {
            if (source != null && sfxGroup != null)
                source.outputAudioMixerGroup = sfxGroup;
        }

        public void RouteToAmbiente(AudioSource source)
        {
            if (source != null && ambienteGroup != null)
                source.outputAudioMixerGroup = ambienteGroup;
        }

        public void RouteToUi(AudioSource source)
        {
            if (source != null && uiGroup != null)
                source.outputAudioMixerGroup = uiGroup;
        }
    }

    /// <summary>
    /// Opcional en escena para resolver el bus sin Resources.
    /// </summary>
    public class ProyectoVrAudioBusLoader : MonoBehaviour
    {
        public ProyectoVrAudioBus bus;
    }
}
