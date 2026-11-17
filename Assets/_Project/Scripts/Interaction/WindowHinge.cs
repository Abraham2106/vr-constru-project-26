using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ProyectoVR
{
    [DisallowMultipleComponent]
    public class WindowHinge : MonoBehaviour
    {
        [SerializeField] Transform hingePivot;
        [SerializeField] XRBaseInteractable handleGrab;
        [SerializeField] AudioSource openCloseAudio;
        [SerializeField] AudioClip openClip;
        [SerializeField] AudioClip closeClip;
        [SerializeField] float closedAngle;
        [SerializeField] float openAngle = 75f;
        [SerializeField] float animationSpeed = 120f;
        [SerializeField] bool startOpen;
        [Range(0f, 1f)] [SerializeField] float soundVolume = 0.35f;
        [SerializeField] float soundMaxDistance = 6f;
        float _velocity;
        float _lastSoundTime = -10f;
        float _targetAngle;
        float _currentAngle;
        bool _isOpen;
        Quaternion _closedRotation;
        Transform _hand;
        Vector3 _grabRadial;
        float _grabAngle;
        bool _dragging;
        public bool IsOpen => _isOpen;
        public float Openness => Mathf.Approximately(openAngle, closedAngle) ? 0f : Mathf.Clamp01((_currentAngle - closedAngle) / (openAngle - closedAngle));
        public Transform Pivot => hingePivot != null ? hingePivot : transform;

        void Awake()
        {
            if(!enabled)return;
            if (hingePivot == null) hingePivot = transform;
            _closedRotation = hingePivot.localRotation;
            _isOpen = startOpen;
            _currentAngle = _targetAngle = startOpen ? openAngle : closedAngle;
            ApplyAngle();
            if (handleGrab != null) {handleGrab.selectEntered.AddListener(OnSelected);handleGrab.selectExited.AddListener(OnReleased);}
            // Varias ventanas tienen la raiz en el origen del mundo: el sonido debe salir de la ventana, no de ahi.
            var center = OpeningUtility.Center(hingePivot);
            if (openCloseAudio != null && (openCloseAudio.transform.position - center).sqrMagnitude > .25f)
            {
                var go = new GameObject("Sonido ventana");
                go.transform.SetParent(transform, true);
                go.transform.position = center;
                var moved = go.AddComponent<AudioSource>();
                moved.volume = openCloseAudio.volume;
                openCloseAudio = moved;
            }
            InteractionAudio.Configure(openCloseAudio, .6f, soundMaxDistance);
            WindowManager.RegisterWindow(this);
        }

        void OnDestroy()
        {
            WindowManager.UnregisterWindow(this);
            if (handleGrab != null) {handleGrab.selectEntered.RemoveListener(OnSelected);handleGrab.selectExited.RemoveListener(OnReleased);}
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            if(ProyectoVR.UI.VrWorldMenuController.AnyMenuVisible)return;
            VrHapticsUtility.PulseInteractor(args.interactorObject, 0.25f, 0.04f);
            _hand=HingeGrabUtility.Hand(args.interactorObject);float distance=float.PositiveInfinity;
            if(_hand!=null)foreach(var c in handleGrab.colliders)if(c!=null)distance=Mathf.Min(distance,Vector3.Distance(_hand.position,c.ClosestPoint(_hand.position)));
            _dragging=distance<.45f&&HingeGrabUtility.Radial(_hand,hingePivot,out _grabRadial);
            if(_dragging){_grabAngle=_currentAngle;_targetAngle=_currentAngle;}else Toggle();
        }
        void OnReleased(SelectExitEventArgs args){if(!_dragging)return;_dragging=false;_targetAngle=_currentAngle<closedAngle+3?closedAngle:_currentAngle;}

        public void Toggle()
        {
            _targetAngle = _targetAngle>closedAngle+3?closedAngle:openAngle;
        }

        void Update()
        {
            if(_dragging&&HingeGrabUtility.Radial(_hand,hingePivot,out var radial))_targetAngle=Mathf.Clamp(_grabAngle+Vector3.SignedAngle(_grabRadial,radial,Vector3.up),closedAngle,openAngle);
            if (Mathf.Approximately(_currentAngle, _targetAngle)) return;
            _currentAngle = Mathf.SmoothDamp(_currentAngle, _targetAngle, ref _velocity, _dragging ? .05f : .25f, _dragging ? 720f : animationSpeed);
            if (Mathf.Abs(_currentAngle - _targetAngle) < .05f) { _currentAngle = _targetAngle; _velocity = 0f; }
            ApplyAngle();
            bool open=_currentAngle>closedAngle+2;
            if(open!=_isOpen){_isOpen=open;WindowManager.NotifyWindowStateChanged();if(Time.time-_lastSoundTime>.3f&&InteractionAudio.Play(openCloseAudio,open?openClip:closeClip,soundVolume))_lastSoundTime=Time.time;}
        }

        void ApplyAngle()
        {
            if (hingePivot != null) hingePivot.localRotation = _closedRotation * Quaternion.Euler(0f, _currentAngle - closedAngle, 0f);
        }
    }
}
