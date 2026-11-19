using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace ProyectoVR.Core
{
    public sealed class FurnitureAwareBodyManipulator : CharacterControllerBodyManipulator
    {
        public const string FurnitureLayer = "Mobiliario";
        int furnitureMask;

        public override void OnLinkedToBody(XRMovableBody body)
        {
            base.OnLinkedToBody(body);
            furnitureMask = LayerMask.GetMask(FurnitureLayer);
        }

        public override CollisionFlags MoveBody(Vector3 motion)
        {
            var controller = characterController;
            if (controller == null || !controller.enabled || furnitureMask == 0)
                return base.MoveBody(motion);

            var up = linkedBody.originTransform.up;
            var horizontalMotion = Vector3.ProjectOnPlane(motion, up);
            var stepOffset = controller.stepOffset;
            if (stepOffset <= 0f || horizontalMotion.sqrMagnitude < 0.000001f)
                return base.MoveBody(motion);

            var scale = controller.transform.lossyScale;
            var radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var feet = linkedBody.GetBodyGroundWorldPosition() + up * radius;
            var touchesFurniture = Physics.CheckCapsule(feet, feet + horizontalMotion,
                radius + controller.skinWidth, furnitureMask, QueryTriggerInteraction.Ignore);
            if (!touchesFurniture)
                return base.MoveBody(motion);

            // Keep native collision and sliding while preventing a low table from becoming a step.
            controller.stepOffset = 0f;
            try
            {
                return base.MoveBody(motion);
            }
            finally
            {
                controller.stepOffset = stepOffset;
            }
        }
    }
}
