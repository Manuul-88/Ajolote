using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class AxolotlHands : NetworkBehaviour
{
    [Header("Arm Joints (tensión del hombro)")]
    public ConfigurableJoint leftArmJoint;
    public ConfigurableJoint rightArmJoint;

    [Header("Manos (para detectar y sujetar objetos)")]
    public Rigidbody leftHandRigidbody;
    public Rigidbody rightHandRigidbody;
    public float grabRadius = 0.35f;
    public LayerMask grabbableMask = ~0;

    [Header("Fuerza de agarre")]
    public float relaxedSpring = 800f;
    public float tenseSpring = 5000f;
    public float springLerpSpeed = 20f;

    private FixedJoint leftGrabJoint;
    private FixedJoint rightGrabJoint;
    private float leftCurrentSpring, rightCurrentSpring;

    void Start()
    {
        leftCurrentSpring = relaxedSpring;
        rightCurrentSpring = relaxedSpring;
    }

    void Update()
    {
        if (!IsOwner || Mouse.current == null) return;

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
        Rigidbody hand = isLeft ? leftHandRigidbody : rightHandRigidbody;
        if (hand == null) return;

        Collider[] hits = Physics.OverlapSphere(hand.position, grabRadius, grabbableMask, QueryTriggerInteraction.Ignore);
        Rigidbody target = null;
        float closest = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody == null) continue;
            if (hit.attachedRigidbody == hand) continue;
            if (hit.attachedRigidbody.transform.root == transform.root) continue; 

            float dist = Vector3.Distance(hand.position, hit.attachedRigidbody.position);
            if (dist < closest) { closest = dist; target = hit.attachedRigidbody; }
        }

        if (target != null)
        {
            FixedJoint joint = hand.gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = target;
            joint.breakForce = 8000f;
            joint.breakTorque = 8000f;

            if (isLeft) leftGrabJoint = joint;
            else rightGrabJoint = joint;
        }
    }

    private void ReleaseGrab(bool isLeft)
    {
        if (isLeft && leftGrabJoint != null) { Destroy(leftGrabJoint); leftGrabJoint = null; }
        else if (!isLeft && rightGrabJoint != null) { Destroy(rightGrabJoint); rightGrabJoint = null; }
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