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
        [SerializeField] float snapSpeed = 180f;
        [SerializeField] bool hingeOnPositiveY = true;
        [SerializeField] AudioClip latchClip;
        float _currentAngle;
        float _targetAngle;
        Quaternion _closedRotation;
        Transform _hand;
        Vector3 _grabRadial;
        float _grabAngle;
        bool _dragging;
        public float CurrentAngle => _currentAngle;

        void Awake()
        {
            if (!enabled) return;
            if (hingePivot == null) hingePivot = transform;
            _closedRotation = hingePivot.localRotation;
            _currentAngle = _targetAngle = minAngle;
            if (handleGrab != null) {handleGrab.selectEntered.AddListener(OnSelected);handleGrab.selectExited.AddListener(OnReleased);}
            if (latchAudio != null) { latchAudio.playOnAwake = false; latchAudio.loop = false; }
        }

        void OnDestroy()
        {
            if (handleGrab != null) {handleGrab.selectEntered.RemoveListener(OnSelected);handleGrab.selectExited.RemoveListener(OnReleased);}
        }

        void OnSelected(SelectEnterEventArgs args)
        {
            if(ProyectoVR.UI.VrWorldMenuController.AnyMenuVisible)return;
            VrHapticsUtility.PulseInteractor(args.interactorObject, 0.25f, 0.04f);
            _hand=HingeGrabUtility.Hand(args.interactorObject);
            float distance=float.PositiveInfinity;
            if(_hand!=null)foreach(var c in handleGrab.colliders)if(c!=null)distance=Mathf.Min(distance,Vector3.Distance(_hand.position,c.ClosestPoint(_hand.position)));
            _dragging=distance<.45f&&HingeGrabUtility.Radial(_hand,hingePivot,out _grabRadial);
            if(_dragging){_grabAngle=_currentAngle;_targetAngle=_currentAngle;}else Toggle();
        }
        void OnReleased(SelectExitEventArgs args){if(!_dragging)return;_dragging=false;_targetAngle=_currentAngle<minAngle+snapShutThreshold?minAngle:_currentAngle;}

        public void Toggle()
        {
            _targetAngle = _targetAngle > minAngle + snapShutThreshold ? minAngle : maxAngle;
        }

        void Update()
        {
            if(_dragging&&HingeGrabUtility.Radial(_hand,hingePivot,out var radial)){float delta=Vector3.SignedAngle(_grabRadial,radial,Vector3.up)*(hingeOnPositiveY?1f:-1f);_targetAngle=Mathf.Clamp(_grabAngle+delta,minAngle,maxAngle);}
            if (Mathf.Approximately(_currentAngle, _targetAngle)) return;
            float previous = _currentAngle;
            _currentAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, snapSpeed * Time.deltaTime);
            if (hingePivot != null)
                hingePivot.localRotation = _closedRotation * Quaternion.AngleAxis(_currentAngle - minAngle, hingeOnPositiveY ? Vector3.up : Vector3.down);
            if (previous > minAngle + 0.01f && Mathf.Approximately(_currentAngle, minAngle) && latchAudio != null)
            {
                var clip = latchClip != null ? latchClip : latchAudio.clip;
                if (clip != null) latchAudio.PlayOneShot(clip);
            }
        }

        public void ConfigureRuntime(Transform pivot, Transform leaf, XRBaseInteractable grab, AudioSource latch)
        {
            hingePivot = pivot; doorLeaf = leaf; handleGrab = grab; latchAudio = latch;
        }
    }
}
