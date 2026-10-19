using ProyectoVR.Core;
using UnityEngine;

namespace Vivienda.Tools
{
    public class BillboardToCamera : MonoBehaviour
    {
        Camera targetCamera;

        void LateUpdate()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera == null)
                return;

            var toCamera = transform.position - targetCamera.transform.position;
            if (!DirectionUtil.TryLookRotation(toCamera, Vector3.up, out var look))
                return;
            transform.rotation = look;
        }
    }
}
