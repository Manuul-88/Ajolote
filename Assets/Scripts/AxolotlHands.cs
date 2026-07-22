using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class AxolotlHands : NetworkBehaviour
{
    [Header("Antebrazos (tienen Rigidbody, son donde se agrega el FixedJoint)")]
    [Tooltip("Rigidbody del hueso LeftForeArm.")]
    public Rigidbody leftArmRigidbody;
    [Tooltip("Rigidbody del hueso RightForeArm.")]
    public Rigidbody rightArmRigidbody;

    [Header("Puntos de mano (huesos LeftHand/RightHand, sin física propia)")]
    [Tooltip("Transform del hueso LeftHand. En este rig no tiene Rigidbody: se usa solo para ubicar el punto de agarre con más precisión que el antebrazo.")]
    public Transform leftHandPoint;
    public Transform rightHandPoint;

    [Header("Joints de hombro (tensión al agarrar)")]
    [Tooltip("ConfigurableJoint del hueso LeftArm (hombro izquierdo).")]
    public ConfigurableJoint leftArmJoint;
    [Tooltip("ConfigurableJoint del hueso RightArm (hombro derecho).")]
    public ConfigurableJoint rightArmJoint;

    [Header("Detección de agarre")]
    public float grabRadius = 0.4f;
    public LayerMask grabbableMask = ~0; // Everything por defecto

    [Header("Fuerza de agarre (tensión del hombro)")]
    public float relaxedSpring = 800f;
    public float tenseSpring = 5000f;
    public float springLerpSpeed = 20f;

    private FixedJoint leftGrabJoint;
    private FixedJoint rightGrabJoint;
    private float leftCurrentSpring, rightCurrentSpring;

    void Awake()
    {
        // Diagnóstico: si estos campos quedan sin asignar en el Inspector,
        // el agarre nunca va a funcionar aunque el resto esté bien.
        if (leftArmRigidbody == null)
            Debug.LogWarning($"[AxolotlHands] {name}: leftArmRigidbody no está asignado en el Inspector.");
        if (rightArmRigidbody == null)
            Debug.LogWarning($"[AxolotlHands] {name}: rightArmRigidbody no está asignado en el Inspector.");
        if (leftHandPoint == null)
            Debug.LogWarning($"[AxolotlHands] {name}: leftHandPoint no está asignado en el Inspector.");
        if (rightHandPoint == null)
            Debug.LogWarning($"[AxolotlHands] {name}: rightHandPoint no está asignado en el Inspector.");
        if (leftArmJoint == null)
            Debug.LogWarning($"[AxolotlHands] {name}: leftArmJoint no está asignado en el Inspector.");
        if (rightArmJoint == null)
            Debug.LogWarning($"[AxolotlHands] {name}: rightArmJoint no está asignado en el Inspector.");
    }

    void Start()
    {
        leftCurrentSpring = relaxedSpring;
        rightCurrentSpring = relaxedSpring;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (Mouse.current == null)
        {
            Debug.LogWarning("[AxolotlHands] Mouse.current es null: revisa Project Settings > Input System Package y que exista un dispositivo de mouse.");
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame) TryGrab(true);
        else if (Mouse.current.leftButton.wasReleasedThisFrame) ReleaseGrab(true);

        if (Mouse.current.rightButton.wasPressedThisFrame) TryGrab(false);
        else if (Mouse.current.rightButton.wasReleasedThisFrame) ReleaseGrab(false);
    }

    void FixedUpdate()
    {
        leftCurrentSpring = Mathf.Lerp(leftCurrentSpring, leftGrabJoint != null ? tenseSpring : relaxedSpring, springLerpSpeed * Time.fixedDeltaTime);
        rightCurrentSpring = Mathf.Lerp(rightCurrentSpring, rightGrabJoint != null ? tenseSpring : relaxedSpring, springLerpSpeed * Time.fixedDeltaTime);

        SetArmStrength(leftArmJoint, leftCurrentSpring);
        SetArmStrength(rightArmJoint, rightCurrentSpring);
    }

    private void TryGrab(bool isLeft)
    {
        if (!IsOwner) return;

        Rigidbody armBody = isLeft ? leftArmRigidbody : rightArmRigidbody;
        Transform handPoint = isLeft ? leftHandPoint : rightHandPoint;
        FixedJoint existingJoint = isLeft ? leftGrabJoint : rightGrabJoint;

        if (armBody == null || handPoint == null)
        {
            Debug.LogWarning($"[AxolotlHands] No se puede agarrar: falta {(isLeft ? "leftArmRigidbody/leftHandPoint" : "rightArmRigidbody/rightHandPoint")} en el Inspector.");
            return;
        }

        // Del tutorial: si ya estamos agarrando algo con esta mano, no busques otro objetivo.
        if (existingJoint != null) return;

        Collider[] hits = Physics.OverlapSphere(handPoint.position, grabRadius, grabbableMask, QueryTriggerInteraction.Ignore);
        Collider targetCollider = null;
        Rigidbody target = null;
        float closest = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody == null) continue;
            if (hit.attachedRigidbody == armBody) continue;
            if (hit.attachedRigidbody.transform.root == transform.root) continue; // no te agarrás a vos mismo

            float dist = Vector3.Distance(handPoint.position, hit.attachedRigidbody.position);
            if (dist < closest) { closest = dist; target = hit.attachedRigidbody; targetCollider = hit; }
        }

        if (target != null)
        {
            // El FixedJoint se agrega sobre el Rigidbody del antebrazo (el hueso
            // "mano" no tiene física propia en este rig), pero el punto de anclaje
            // se calcula desde handPoint para que el agarre se sienta en la mano
            // y no en el centro del antebrazo.
            FixedJoint joint = armBody.gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = target;
            joint.breakForce = 8000f;
            joint.breakTorque = 8000f;

            // --- Del tutorial: anclar en el punto exacto de contacto ---
            // --- en vez de dejar que Unity centre los objetos entre sí ---
            joint.autoConfigureConnectedAnchor = false;
            Vector3 grabPointWorld = targetCollider.ClosestPoint(handPoint.position);
            joint.connectedAnchor = target.transform.InverseTransformPoint(grabPointWorld);

            if (isLeft) leftGrabJoint = joint; else rightGrabJoint = joint;
        }
    }

    private void ReleaseGrab(bool isLeft)
    {
        if (isLeft && leftGrabJoint != null) { Destroy(leftGrabJoint); leftGrabJoint = null; }
        else if (!isLeft && rightGrabJoint != null) { Destroy(rightGrabJoint); rightGrabJoint = null; }
    }

    // Detecta si el joint usa Slerp Drive (como los hombros en Player.prefab)
    // o X/YZ Drive, y escribe en el drive correcto. Los joints de LeftArm y
    // RightArm en tu prefab están en modo Slerp (m_RotationDriveMode: 1):
    // escribir solo en angularXDrive/angularYZDrive ahí no tenía ningún efecto.
    private void SetArmStrength(ConfigurableJoint joint, float springValue)
    {
        if (joint == null) return;

        if (joint.rotationDriveMode == RotationDriveMode.Slerp)
        {
            JointDrive slerpDrive = joint.slerpDrive;
            slerpDrive.positionSpring = springValue;
            joint.slerpDrive = slerpDrive;
        }
        else
        {
            JointDrive drive = joint.angularXDrive;
            drive.positionSpring = springValue;
            joint.angularXDrive = drive;
            joint.angularYZDrive = drive;
        }
    }
}