using UnityEngine;
using UnityEngine.InputSystem;

namespace ProyectoVR.UI
{
    public class VrMenuToggleInput : MonoBehaviour
    {
        [SerializeField] VrWorldMenuController m_Menu;
        [SerializeField] InputActionReference m_ToggleAction;

        void Update()
        {
            if (m_ToggleAction != null && m_ToggleAction.action.WasPressedThisFrame())
                m_Menu?.ToggleVisible();
        }
    }
}
