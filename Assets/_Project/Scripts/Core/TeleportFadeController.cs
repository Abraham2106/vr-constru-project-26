using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProyectoVR.Core
{
    /// <summary>
    /// Fade a negro en teletransporte (cámara HMD).
    /// </summary>
    public class TeleportFadeController : MonoBehaviour
    {
        [SerializeField] Image m_FadeImage;
        [SerializeField] float m_FadeDuration = 0.25f;

        Coroutine m_Routine;

        public bool IsFading => m_Routine != null;

        public void BindFadeImage(Image image)
        {
            m_FadeImage = image;
            if (m_FadeImage != null)
            {
                var c = m_FadeImage.color;
                c.a = 0f;
                m_FadeImage.color = c;
            }
        }

        public void RunFadeTeleport(System.Action midAction)
        {
            if (m_FadeImage == null)
            {
                midAction?.Invoke();
                return;
            }

            if (m_Routine != null)
                StopCoroutine(m_Routine);
            m_Routine = StartCoroutine(FadeRoutine(midAction));
        }

        IEnumerator FadeRoutine(System.Action midAction)
        {
            yield return FadeTo(1f);
            midAction?.Invoke();
            yield return FadeTo(0f);
            m_Routine = null;
        }

        IEnumerator FadeTo(float targetAlpha)
        {
            var c = m_FadeImage.color;
            var start = c.a;
            var t = 0f;
            while (t < m_FadeDuration)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(start, targetAlpha, t / m_FadeDuration);
                m_FadeImage.color = c;
                yield return null;
            }

            c.a = targetAlpha;
            m_FadeImage.color = c;
        }
    }
}
