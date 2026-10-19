using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace ProyectoVR {
 public static class HingeGrabUtility {
  public static Transform Hand(IXRSelectInteractor interactor){var c=interactor as Component;if(c==null)return null;for(var p=c.transform;p!=null;p=p.parent)if(p.name=="Left Controller"||p.name=="Right Controller")return p;return c.transform;}
  public static bool Radial(Transform hand,Transform pivot,out Vector3 direction){direction=hand!=null&&pivot!=null?Vector3.ProjectOnPlane(hand.position-pivot.position,Vector3.up):Vector3.zero;if(direction.sqrMagnitude<.000001f)return false;direction.Normalize();return true;}
 }
}
