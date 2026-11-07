using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ProyectoVR.UI
{
    // Unity UI pointer events are shared by mouse and XRI tracked-device input.
    public sealed class VrGlassMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerMoveHandler, IPointerDownHandler, IPointerUpHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public bool IsCard;
        RectTransform rect;
        VrGlassSurface surface;
        bool hovering, pressed, dragging;
        int owner;
        float pulse;
        Vector2 point;
        Vector3 grabOffset;
        Plane dragPlane;
        Quaternion restRotation;
        Vector3 restScale;

        void Awake()
        {
            rect = (RectTransform)transform;
            surface = GetComponent<VrGlassSurface>();
            restRotation = rect.localRotation;
            restScale = rect.localScale;
        }

        void OnDisable()
        {
            hovering = pressed = dragging = false;
            pulse = 0;
            rect.localRotation = restRotation;
            rect.localScale = restScale;
            if(surface != null){surface.ShineStrength=0;surface.SetVerticesDirty();}
        }

        void LateUpdate()
        {
            float blend = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            if (IsCard)
            {
                // Small angles avoid uncomfortable panel motion in a headset.
                var tilt = hovering && !dragging ? Quaternion.Euler(-point.y * 1.2f, point.x * 1.5f, 0) : Quaternion.identity;
                rect.localRotation = Quaternion.Slerp(rect.localRotation, restRotation * tilt, blend);
            }
            float scale = pressed ? .987f : hovering ? 1.006f : 1f;
            if(IsCard)scale=dragging?1.004f:1f;
            rect.localScale = Vector3.Lerp(rect.localScale, restScale * scale, blend);
            pulse = Mathf.MoveTowards(pulse, 0, Time.unscaledDeltaTime * 2.5f);
            if (surface != null)
            {
                float glow = Mathf.Max(pulse, pressed ? .12f : hovering ? .035f : 0);
                if(IsCard)glow*=.25f;
                if(Mathf.Abs(surface.ShineStrength-glow)>.001f){surface.ShineStrength=glow;surface.SetVerticesDirty();}
            }
        }

        void Locate(PointerEventData data)
        {
            Vector3 hit;
            var plane = new Plane(rect.forward,rect.position);
            if (!Hit(data,plane,out hit)) return;
            Vector3 local = rect.InverseTransformPoint(hit);
            point = new Vector2(Mathf.Clamp(local.x / (rect.rect.width*.5f),-1,1),Mathf.Clamp(local.y / (rect.rect.height*.5f),-1,1));
            if(surface!=null){surface.ShinePoint=point;surface.SetVerticesDirty();}
        }

        static bool Hit(PointerEventData data, Plane plane, out Vector3 hit)
        {
            var tracked=data as TrackedDeviceEventData;
            if(tracked!=null && tracked.rayPoints!=null)
                for(int i=1;i<tracked.rayPoints.Count;i++)
                {
                    var a=tracked.rayPoints[i-1];var delta=tracked.rayPoints[i]-a;
                    if(delta.sqrMagnitude<.000001f)continue;
                    float distance;
                    if(plane.Raycast(new Ray(a,delta.normalized),out distance)&&distance<=delta.magnitude+.001f){hit=a+delta.normalized*distance;return true;}
                }
            var camera=data.pressEventCamera!=null?data.pressEventCamera:data.enterEventCamera;
            if(camera!=null){var ray=camera.ScreenPointToRay(data.position);float distance;if(plane.Raycast(ray,out distance)){hit=ray.GetPoint(distance);return true;}}
            hit=Vector3.zero;return false;
        }

        public void OnPointerEnter(PointerEventData data){hovering=true;Locate(data);}
        public void OnPointerMove(PointerEventData data){Locate(data);}
        public void OnPointerExit(PointerEventData data){hovering=false;}
        public void OnPointerDown(PointerEventData data){if(data.button!=PointerEventData.InputButton.Left)return;owner=data.pointerId;pressed=true;pulse=.16f;Locate(data);}
        public void OnPointerUp(PointerEventData data){if(data.pointerId==owner)pressed=false;}
        public void OnInitializePotentialDrag(PointerEventData data){data.useDragThreshold=true;}
        public void OnBeginDrag(PointerEventData data)
        {
            if(!IsCard||data.button!=PointerEventData.InputButton.Left)return;
            Vector3 hit;dragPlane=new Plane(rect.forward,rect.position);
            if(!Hit(data,dragPlane,out hit))return;
            owner=data.pointerId;dragging=true;grabOffset=rect.parent.position-hit;
            var follow=GetComponentInParent<VrMenuFollowHead>();if(follow!=null)follow.enabled=false;
        }
        public void OnDrag(PointerEventData data)
        {
            if(!dragging||data.pointerId!=owner)return;
            Vector3 hit;if(Hit(data,dragPlane,out hit))rect.parent.position=hit+grabOffset;
        }
        public void OnEndDrag(PointerEventData data){if(data.pointerId==owner){dragging=false;pressed=false;}}
    }
}
