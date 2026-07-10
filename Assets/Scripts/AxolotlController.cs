using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class AxolotlController : NetworkBehaviour
{
    [Header("References")]
    public Rigidbody rootRigidbody;
    public Rigidbody bodyRigidbody; // el Rigidbody del objeto "Body"

    [Header("Movement")]
    public float moveForce = 30f;
    public float maxSpeed = 5f;
    public float jumpImpulse = 6f;
    public float airControl = 0.35f;

    [Header("Balance - Rotación (endereza el cuerpo)")]
    public float uprightSpring = 5000f;
    public float uprightDamper = 400f;
    public float maxAngularVelocity = 8f;

    [Header("Balance - Altura (suspensión vertical, estilo Gang Beasts)")]
    [Tooltip("Altura a la que la pelvis intenta 'flotar' sobre el piso detectado")]
    public float balanceHeight = 1.0f;
    [Tooltip("Fuerza del resorte vertical. Más alto = más rígido / menos se hunde")]
    public float balanceStrength = 800f;
    [Tooltip("Amortiguación del resorte vertical. Evita que rebote como un yoyo")]
    public float balanceDamper = 80f;

    [Header("Ground Check")]
    public float groundCheckDistance = 1.3f; // debe ser > balanceHeight
    public float groundCheckRadius = 0.35f;
    public LayerMask groundMask = ~0;

    private bool isGrounded;
    private float currentGroundDistance;
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
        MaintainBalanceHeight();
        MovePlayer();
    }

    private void CheckGround()
    {
        isGrounded = Physics.SphereCast(
            rootRigidbody.position + Vector3.up * 0.1f,
            groundCheckRadius,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        currentGroundDistance = isGrounded ? hit.distance + 0.1f : -1f;
    }

    // Corrige solo inclinación (pitch/roll), nunca yaw, así nunca compite
    // con el giro hacia donde caminás.
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

    // "Suspensión" vertical: empuja hacia arriba con fuerza proporcional a
    // qué tan por debajo de balanceHeight está la pelvis. Esto es lo que
    // evita que el ragdoll colapse al primer tropezón, sin volverlo rígido.
    private void MaintainBalanceHeight()
    {
        if (!isGrounded) return;

        float heightError = balanceHeight - currentGroundDistance;
        float springForce = heightError * balanceStrength;
        float dampingForce = rootRigidbody.linearVelocity.y * balanceDamper;

        rootRigidbody.AddForce(Vector3.up * (springForce - dampingForce), ForceMode.Force);
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

    private void MovePlayer()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 flatVelocity = new Vector3(rootRigidbody.linearVelocity.x, 0f, rootRigidbody.linearVelocity.z);
        float control = isGrounded ? 1f : airControl;

        if (flatVelocity.magnitude < maxSpeed)
        {
            // Repartir el empuje entre Root y Body evita que la parte baja
            // arranque antes que la de arriba y el ragdoll se vaya de boca.
            Vector3 push = moveDirection * moveForce * control;
            rootRigidbody.AddForce(push * 0.6f, ForceMode.Force);
            if (bodyRigidbody != null)
                bodyRigidbody.AddForce(push * 0.4f, ForceMode.Force);
        }

        Quaternion targetYaw = Quaternion.LookRotation(moveDirection);
        float yawError = Mathf.DeltaAngle(rootRigidbody.transform.eulerAngles.y, targetYaw.eulerAngles.y);
        float yawTorque = yawError * Mathf.Deg2Rad * (uprightSpring * 0.15f) - rootRigidbody.angularVelocity.y * (uprightDamper * 0.15f);
        rootRigidbody.AddTorque(Vector3.up * yawTorque, ForceMode.Acceleration);
    }
}
