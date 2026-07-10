using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class AxolotlController : NetworkBehaviour
{
    [Header("References")]
    public Rigidbody rootRigidbody;

    [Header("Movement")]
    public float moveForce = 40f;
    public float maxSpeed = 5f;
    public float jumpImpulse = 6f;
    public float airControl = 0.35f; 

    [Header("Balance (PD Controller)")]
    public float uprightSpring = 3500f; 
    public float uprightDamper = 250f;  
    public float maxAngularVelocity = 10f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.9f;
    public float groundCheckRadius = 0.35f;
    public LayerMask groundMask = ~0;

    private bool isGrounded;
    private Vector2 moveInput;

    void Awake()
    {
        if (rootRigidbody != null)
            rootRigidbody.maxAngularVelocity = maxAngularVelocity;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner && rootRigidbody != null)
            rootRigidbody.isKinematic = true;
    }

    void Update()
    {
        if (!IsOwner || rootRigidbody == null) return;

        CheckGround();
        ReadMoveInput();

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rootRigidbody.AddForce(Vector3.up * jumpImpulse, ForceMode.VelocityChange);
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner || rootRigidbody == null) return;

        KeepUpright();
        MovePlayer();
    }

    private void CheckGround()
    {
        isGrounded = Physics.SphereCast(
            rootRigidbody.position + Vector3.up * 0.1f,
            groundCheckRadius,
            Vector3.down,
            out _,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);
    }

    private void ReadMoveInput()
    {
        if (Keyboard.current == null) { moveInput = Vector2.zero; return; }

        float x = 0f, z = 0f;
        if (Keyboard.current.wKey.isPressed) z += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.aKey.isPressed) x -= 1f;
        moveInput = new Vector2(x, z).normalized;
    }

    private void KeepUpright()
    {
        Quaternion current = rootRigidbody.transform.rotation;
        Quaternion toUpright = Quaternion.FromToRotation(rootRigidbody.transform.up, Vector3.up) * current;

        toUpright.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;

        if (Mathf.Abs(angle) > 0.01f)
        {
            Vector3 torque = axis.normalized * (angle * Mathf.Deg2Rad * uprightSpring);
            Vector3 damping = rootRigidbody.angularVelocity * uprightDamper;
            rootRigidbody.AddTorque(torque - damping, ForceMode.Acceleration);
        }
    }

    private void MovePlayer()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 flatVelocity = new Vector3(rootRigidbody.linearVelocity.x, 0f, rootRigidbody.linearVelocity.z);
        float control = isGrounded ? 1f : airControl;

        if (flatVelocity.magnitude < maxSpeed)
        {
            rootRigidbody.AddForce(moveDirection * moveForce * control, ForceMode.Force);
        }

        Quaternion targetYaw = Quaternion.LookRotation(moveDirection);
        Quaternion currentYaw = Quaternion.Euler(0f, rootRigidbody.transform.eulerAngles.y, 0f);
        float yawError = Mathf.DeltaAngle(currentYaw.eulerAngles.y, targetYaw.eulerAngles.y);

        float yawTorque = yawError * Mathf.Deg2Rad * (uprightSpring * 0.2f) - rootRigidbody.angularVelocity.y * (uprightDamper * 0.2f);
        rootRigidbody.AddTorque(Vector3.up * yawTorque, ForceMode.Acceleration);
    }
}