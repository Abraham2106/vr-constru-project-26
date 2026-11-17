using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProyectoVR
{
    [DisallowMultipleComponent]
    public class DoorHinge : MonoBehaviour
    {
        [SerializeField] Transform hingePivot;
        [SerializeField] Transform doorLeaf;
        [SerializeField] XRBaseInteractable handleGrab;
        [SerializeField] AudioSource latchAudio;
        [SerializeField] float minAngle;
        [SerializeField] float maxAngle = 100f;
        [SerializeField] float snapShutThreshold = 3f;
        [SerializeField] bool hingeOnPositiveY = true;

        [Header("Movimiento")]
        [Tooltip("Velocidad maxima de giro en grados por segundo.")]
        [SerializeField] float maxSpeed = 110f;
        [Tooltip("Tiempo aproximado para llegar al angulo final (acelera y frena suave).")]
        [SerializeField] float smoothTime = 0.3f;
        [SerializeField] float dragSmoothTime = 0.05f;

        [Header("Sonido")]
        [SerializeField] AudioClip openClip;
        [SerializeField] AudioClip closeClip;
        [Tooltip("Respaldo si no hay clip de cierre asignado.")]
        [SerializeField] AudioClip latchClip;
        [Range(0f, 1f)] [SerializeField] float soundVolume = 0.45f;
        [Tooltip("Distancia en metros a la que el sonido deja de escucharse.")]
        [SerializeField] float soundMaxDistance = 7f;

        const float ClosedEpsilon = 0.5f;
        const float SoundCooldown = 0.3f;

        static CharacterController s_player;

        float _currentAngle;
        float _targetAngle;
        float _velocity;
        float _lastSoundTime = -10f;
        Quaternion _closedRotation;
        Transform _hand;
        Vector3 _grabRadial;
        float _grabAngle;
        bool _dragging;
        Collider[] _leafColliders;
        bool _ignoringPlayer;

        public float CurrentAngle => _currentAngle;
        public bool IsMoving => _dragging || !Mathf.Approximately(_currentAngle, _targetAngle);

        void Awake()
        {
            if (!enabled) return;
            if (hingePivot == null) hingePivot = transform;
            _closedRotation = hingePivot.localRotation;
            _currentAngle = _targetAngle = minAngle;
            _leafColliders = hingePivot.GetComponentsInChildren<Collider>(true);
            if (handleGrab != null) { handleGrab.selectEntered.AddListener(OnSelected); handleGrab.selectExited.AddListener(OnReleased); }
            InteractionAudio.Configure(latchAudio, 0.8f, soundMaxDistance);
        }

        void OnDestroy()
        {
            if (handleGrab != null) { handleGrab.selectEntered.RemoveListener(OnSelected); handleGrab.selectExited.RemoveListener(OnReleased); }
        }

        void OnDisable()
        {
            if (_ignoringPlayer) SetIgnorePlayer(false);
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            if (ProyectoVR.UI.VrWorldMenuController.AnyMenuVisible) return;
            VrHapticsUtility.PulseInteractor(args.interactorObject, 0.25f, 0.04f);
            _hand = HingeGrabUtility.Hand(args.interactorObject);
            float distance = float.PositiveInfinity;
            if (_hand != null) foreach (var c in handleGrab.colliders) if (c != null) distance = Mathf.Min(distance, Vector3.Distance(_hand.position, c.ClosestPoint(_hand.position)));
            _dragging = distance < .45f && HingeGrabUtility.Radial(_hand, hingePivot, out _grabRadial);
            if (_dragging) { _grabAngle = _currentAngle; _targetAngle = _currentAngle; } else Toggle();
        }

        void OnReleased(SelectExitEventArgs args)
        {
            if (!_dragging) return;
            _dragging = false;
            _targetAngle = _currentAngle < minAngle + snapShutThreshold ? minAngle : _currentAngle;
        }

        public void Toggle()
        {
            _targetAngle = _targetAngle > minAngle + snapShutThreshold ? minAngle : maxAngle;
        }

        void Update()
        {
            if (_dragging && HingeGrabUtility.Radial(_hand, hingePivot, out var radial))
            {
                float delta = Vector3.SignedAngle(_grabRadial, radial, Vector3.up) * (hingeOnPositiveY ? 1f : -1f);
                _targetAngle = Mathf.Clamp(_grabAngle + delta, minAngle, maxAngle);
            }

            UpdatePlayerCollision();
            if (Mathf.Approximately(_currentAngle, _targetAngle)) return;

            float previous = _currentAngle;
            _currentAngle = Mathf.SmoothDamp(_currentAngle, _targetAngle, ref _velocity,
                _dragging ? dragSmoothTime : smoothTime, _dragging ? 720f : maxSpeed);
            if (Mathf.Abs(_currentAngle - _targetAngle) < 0.05f) { _currentAngle = _targetAngle; _velocity = 0f; }

            if (hingePivot != null)
                hingePivot.localRotation = _closedRotation * Quaternion.AngleAxis(_currentAngle - minAngle, hingeOnPositiveY ? Vector3.up : Vector3.down);

            bool wasClosed = previous <= minAngle + ClosedEpsilon;
            bool isClosed = _currentAngle <= minAngle + ClosedEpsilon;
            if (wasClosed && !isClosed) PlaySound(openClip != null ? openClip : latchClip);
            else if (!wasClosed && isClosed) PlaySound(closeClip != null ? closeClip : latchClip);
        }

        void PlaySound(AudioClip clip)
        {
            if (Time.time - _lastSoundTime < SoundCooldown) return;
            if (InteractionAudio.Play(latchAudio, clip, soundVolume)) _lastSoundTime = Time.time;
        }

        // Mientras la hoja se mueve no empuja al jugador (el CharacterController la "trepaba" y lo levantaba).
        // La colision vuelve cuando la puerta esta quieta y ya no hay solape con el jugador.
        void UpdatePlayerCollision()
        {
            if (s_player == null) s_player = FindAnyObjectByType<CharacterController>();
            if (s_player == null || _leafColliders == null) return;
            if (IsMoving) { if (!_ignoringPlayer) SetIgnorePlayer(true); }
            else if (_ignoringPlayer && !OverlapsPlayer()) SetIgnorePlayer(false);
        }

        bool OverlapsPlayer()
        {
            var playerBounds = s_player.bounds;
            foreach (var c in _leafColliders)
                if (c != null && c.enabled && !c.isTrigger && c.bounds.Intersects(playerBounds)) return true;
            return false;
        }

        void SetIgnorePlayer(bool ignore)
        {
            _ignoringPlayer = ignore;
            if (s_player == null || _leafColliders == null) return;
            foreach (var c in _leafColliders)
                if (c != null && !c.isTrigger) Physics.IgnoreCollision(s_player, c, ignore);
        }

        public void ConfigureRuntime(Transform pivot, Transform leaf, XRBaseInteractable grab, AudioSource latch)
        {
            hingePivot = pivot; doorLeaf = leaf; handleGrab = grab; latchAudio = latch;
        }
    }
}
