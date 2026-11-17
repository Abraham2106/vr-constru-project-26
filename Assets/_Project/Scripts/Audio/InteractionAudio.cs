using UnityEngine;

namespace ProyectoVR
{
    /// <summary>
    /// Sonidos cortos de puertas, ventanas e interruptores: 3D, suaves, se apagan con la distancia
    /// y nunca se acumulan (limite global de voces simultaneas).
    /// </summary>
    public static class InteractionAudio
    {
        const int MaxVoices = 3;
        static readonly float[] VoiceEnds = new float[MaxVoices];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { for (int i = 0; i < MaxVoices; i++) VoiceEnds[i] = 0f; }

        /// <summary>Deja la fuente como sonido 3D que se escucha cerca y desaparece a ~maxDistance metros.</summary>
        public static void Configure(AudioSource source, float minDistance = 0.8f, float maxDistance = 7f)
        {
            if (source == null) return;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.dopplerLevel = 0f;
            source.spread = 30f;
            ProyectoVrAudioBus.Instance?.RouteToSfx(source);
        }

        /// <summary>Reproduce un disparo con leve variacion de tono. Devuelve false si se descarto.</summary>
        public static bool Play(AudioSource source, AudioClip clip, float volume = 0.5f, float pitchVariation = 0.06f)
        {
            if (source == null || clip == null || !source.isActiveAndEnabled) return false;
            float now = Time.unscaledTime;
            int slot = -1;
            for (int i = 0; i < MaxVoices; i++) if (VoiceEnds[i] <= now) { slot = i; break; }
            if (slot < 0) return false;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.PlayOneShot(clip, volume);
            VoiceEnds[slot] = now + clip.length / Mathf.Max(0.5f, source.pitch);
            return true;
        }
    }
}
