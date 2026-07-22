using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class AxolotlController : NetworkBehaviour
{
    [Header("References")]
    public Rigidbody rootRigidbody;
    public Rigidbody bodyRigidbody; // Rigidbody del "Body"/chest, si es distinto del root

    [Header("Movement")]
    public float moveForce = 30f;
    public float maxSpeed = 5f;
    public float jumpImpulse = 6f;
    public float airControl = 0.35f;

    [Header("Movement - Joint direccional (estilo tutorial)")]
    [Tooltip("ConfigurableJoint del root anclado al mundo (Connected Body = None). En Player.prefab es el mismo joint que usaba NetworkPlayer (fileID 973742572033113448).")]
    public ConfigurableJoint mainJoint;
    [Tooltip("Velocidad de giro en grados/segundo hacia la dirección de movimiento.")]
    public float turnSpeed = 300f;

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

        if (mainJoint == null)
            Debug.LogWarning($"[AxolotlController] {name}: mainJoint no está asignado. El giro direccional no va a funcionar.");
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

    // Corrige solo inclinación (pitch/roll), nunca yaw. El yaw ahora lo maneja
    // el mainJoint (ver MovePlayer), así que estos dos sistemas no compiten:
    // KeepUpright endereza, el joint gira hacia donde caminás.
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
            // Si bodyRigidbody == rootRigidbody (como en Player.prefab ahora
            // mismo) esto simplemente aplica el 100% de la fuerza al mismo
            // objeto, sin duplicar nada.
            Vector3 push = moveDirection * moveForce * control;
            rootRigidbody.AddForce(push * 0.6f, ForceMode.Force);
            if (bodyRigidbody != null && bodyRigidbody != rootRigidbody)
                bodyRigidbody.AddForce(push * 0.4f, ForceMode.Force);
        }

        // --- Giro estilo tutorial ---
        // En vez de calcular torque de yaw a mano, movemos el targetRotation
        // del joint anclado al mundo. Al pasar "rootRigidbody.transform.up"
        // como vector "up" del LookRotation, el target de pitch/roll siempre
        // coincide con la inclinación actual del cuerpo, así el Slerp Drive
        // del joint prácticamente solo corrige yaw y no pelea con KeepUpright.
        if (mainJoint != null)
        {
            Quaternion desiredDirection = Quaternion.LookRotation(moveDirection, rootRigidbody.transform.up);
            mainJoint.targetRotation = Quaternion.RotateTowards(
                mainJoint.targetRotation, desiredDirection, turnSpeed * Time.fixedDeltaTime);
        }
    }
}