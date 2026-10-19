using UnityEngine;

namespace ProyectoVR.UI
{
    [RequireComponent(typeof(AudioSource))]
    public class UiClickSound : MonoBehaviour
    {
        [SerializeField] AudioClip m_ClickClip;
        AudioSource m_Source;

        void Awake()
        {
            m_Source = GetComponent<AudioSource>();
            m_Source.playOnAwake = false;
            m_Source.spatialBlend = 0f;
        }

        public void PlayClick()
        {
            if (m_ClickClip == null || m_Source == null)
                return;
            m_Source.PlayOneShot(m_ClickClip, 0.65f);
        }
    }
}
