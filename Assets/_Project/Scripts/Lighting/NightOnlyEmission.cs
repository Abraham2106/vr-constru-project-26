using UnityEngine;

namespace ProyectoVR
{
    /// <summary>
    /// Enciende el brillo (emision) de tiras LED y balizas solo de noche.
    /// De dia el material queda apagado para que no brille con el sol.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class NightOnlyEmission : MonoBehaviour
    {
        [ColorUsage(false, true)] [SerializeField] Color nightEmission = new(2.4f, 1.6f, 0.85f);

        Renderer _renderer;
        MaterialPropertyBlock _block;
        DayNightController _controller;
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        void Start()
        {
            _controller = DayNightController.Instance;
            if (_controller != null)
            {
                _controller.OnNightStarted.AddListener(On);
                _controller.OnDayStarted.AddListener(Off);
            }
            Set(_controller != null && _controller.IsNight);
        }

        void OnDestroy()
        {
            if (_controller == null) return;
            _controller.OnNightStarted.RemoveListener(On);
            _controller.OnDayStarted.RemoveListener(Off);
        }

        void On() => Set(true);
        void Off() => Set(false);

        void Set(bool night)
        {
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(EmissionColor, night ? nightEmission : Color.black);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
