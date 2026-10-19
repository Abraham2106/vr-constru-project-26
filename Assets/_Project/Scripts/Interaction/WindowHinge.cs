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
        float _targetAngle;
        float _currentAngle;
        bool _isOpen;
        Quaternion _closedRotation;
        Transform _hand;
        Vector3 _grabRadial;
        float _grabAngle;
        bool _dragging;
        public bool IsOpen => _isOpen;

        void Awake()
        {
            if(!enabled)return;
            if (hingePivot == null) hingePivot = transform;
            _closedRotation = hingePivot.localRotation;
            _isOpen = startOpen;
            _currentAngle = _targetAngle = startOpen ? openAngle : closedAngle;
            ApplyAngle();
            if (handleGrab != null) {handleGrab.selectEntered.AddListener(OnSelected);handleGrab.selectExited.AddListener(OnReleased);}
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
            _currentAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, animationSpeed * Time.deltaTime);
            ApplyAngle();
            bool open=_currentAngle>closedAngle+5;
            if(open!=_isOpen){_isOpen=open;WindowManager.NotifyWindowStateChanged();var clip=open?openClip:closeClip;if(openCloseAudio!=null&&clip!=null)openCloseAudio.PlayOneShot(clip);}
        }

        void ApplyAngle()
        {
            if (hingePivot != null) hingePivot.localRotation = _closedRotation * Quaternion.Euler(0f, _currentAngle - closedAngle, 0f);
        }
    }
}
