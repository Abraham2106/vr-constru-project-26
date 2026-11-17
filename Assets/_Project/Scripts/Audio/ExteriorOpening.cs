using System.Collections.Generic;
using UnityEngine;

namespace ProyectoVR
{
    /// <summary>
    /// Ventana o puerta que da a la calle. Mientras esta abierta, el sonido de la calle entra por ella
    /// (fuente 3D en el centro de la abertura, mas fuerte cuanto mas abierta). Cerrada no suena nada,
    /// y al alejarse se apaga. Solo suenan las dos aberturas abiertas mas cercanas, para que no se acumule.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExteriorOpening : MonoBehaviour
    {
        [SerializeField] AudioClip streetLoop;
        [Range(0f, 1f)] [SerializeField] float maxVolume = 0.08f;
        [Tooltip("Hasta esta distancia se escucha a volumen pleno.")]
        [SerializeField] float fullVolumeDistance = 1.0f;
        [Tooltip("A esta distancia el viento deja de escucharse.")]
        [SerializeField] float silentDistance = 5f;
        [SerializeField] float fadeSpeed = 0.06f;

        const int MaxAudibleOpenings = 2;
        static readonly List<ExteriorOpening> Active = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active.Clear();

        DoorHinge _door;
        WindowHinge _window;
        AudioSource _source;
        Transform _listener;

        public Vector3 Center { get; private set; }
        public float Openness => _door != null ? _door.Openness : _window != null ? _window.Openness : 0f;

        void Awake()
        {
            _door = GetComponent<DoorHinge>();
            _window = GetComponent<WindowHinge>();
            Center = OpeningUtility.Center(_door != null ? _door.Pivot : _window != null ? _window.Pivot : transform);

            var go = new GameObject("Sonido calle");
            go.transform.SetParent(transform, true);
            go.transform.position = Center;
            _source = go.AddComponent<AudioSource>();
            _source.clip = streetLoop;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.volume = 0f;
            _source.spatialBlend = 1f;
            _source.rolloffMode = AudioRolloffMode.Linear;
            _source.minDistance = fullVolumeDistance;
            _source.maxDistance = silentDistance;
            _source.dopplerLevel = 0f;
            _source.spread = 90f; // entra por toda la abertura, no por un punto
            ProyectoVrAudioBus.Instance?.RouteToAmbiente(_source);
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() { Active.Remove(this); if (_source != null) _source.Stop(); }

        void Update()
        {
            if (_source == null || _source.clip == null) return;
            float open = Openness;
            // Solo adentro: afuera ya se escucha el viento general y no debe sumarse.
            bool indoors = _listener == null || IndoorDetector.IsIndoors(_listener.position);
            float target = open > 0.02f && IsAmongNearest() && indoors ? maxVolume * Mathf.Clamp01(open * 1.5f) : 0f;
            _source.volume = Mathf.MoveTowards(_source.volume, target, fadeSpeed * Time.deltaTime);

            if (_source.volume > 0.0005f && !_source.isPlaying)
            {
                _source.time = Random.Range(0f, _source.clip.length * 0.9f);
                _source.Play();
            }
            else if (_source.volume <= 0.0005f && _source.isPlaying && target <= 0f)
                _source.Stop();
        }

        bool IsAmongNearest()
        {
            if (_listener == null) { var cam = Camera.main; if (cam == null) return true; _listener = cam.transform; }
            var p = _listener.position;
            float mine = (Center - p).sqrMagnitude;
            if (mine > silentDistance * silentDistance) return false;
            int closer = 0;
            foreach (var other in Active)
                if (other != this && other.Openness > 0.02f && (other.Center - p).sqrMagnitude < mine && ++closer >= MaxAudibleOpenings)
                    return false;
            return true;
        }
    }

    public static class OpeningUtility
    {
        /// <summary>Centro real de la abertura (las raices de varias ventanas estan en el origen del mundo).</summary>
        public static Vector3 Center(Transform root)
        {
            if (root == null) return Vector3.zero;
            bool any = false;
            var bounds = new Bounds(root.position, Vector3.zero);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is LineRenderer || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
            }
            return any ? bounds.center : root.position;
        }
    }
}
