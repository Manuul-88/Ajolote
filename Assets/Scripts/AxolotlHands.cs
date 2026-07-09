using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode; // <-- Necesario

public class AxolotlHands : NetworkBehaviour // <-- Heredamos de NetworkBehaviour
{
    [Header("Arm Joints")]
    public ConfigurableJoint leftArmJoint;
    public ConfigurableJoint rightArmJoint;

    [Header("Strength Settings")]
    public float relaxedSpring = 800f;
    public float tenseSpring = 5000f;

    void Update()
    {
        // ¡REGLA DE ORO! Solo el dueño usa su ratón
        if (!IsOwner) return;
        if (Mouse.current == null) return;

        // Clic Izquierdo para el brazo izquierdo
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetArmStrength(leftArmJoint, tenseSpring);
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            SetArmStrength(leftArmJoint, relaxedSpring);
        }

        // Clic Derecho para el brazo derecho
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            SetArmStrength(rightArmJoint, tenseSpring);
        }
        else if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            SetArmStrength(rightArmJoint, relaxedSpring);
        }
    }

    private void SetArmStrength(ConfigurableJoint joint, float springValue)
    {
        if (joint == null) return;

        JointDrive drive = joint.angularXDrive;
        drive.positionSpring = springValue;

        joint.angularXDrive = drive;
        joint.angularYZDrive = drive;
    }
}