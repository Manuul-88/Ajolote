using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class AxolotlController : NetworkBehaviour
{
    [Header("References")]
    public Rigidbody rootRigidbody;

    [Header("Movement Settings")]
    public float walkForce = 1500f;
    public float maxSpeed = 5f;
    public float jumpForce = 1000f; // Subimos la fuerza del salto

    [Header("Balance Settings")]
    public float uprightTorque = 25000f; // ¡Mucha más fuerza abdominal para levantarse!
    public float groundCheckDistance = 1.2f; // Ajusta esto según el tamaño de tu cápsula

    private bool isGrounded;

    void Awake()
    {
        if (rootRigidbody != null)
        {
            // Le damos libertad para girar rápido y enderezarse
            rootRigidbody.maxAngularVelocity = 40f;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner && rootRigidbody != null)
        {
            rootRigidbody.isKinematic = true;
        }
    }

    void Update()
    {
        if (!IsOwner || rootRigidbody == null) return;

        CheckGround(); // Revisamos si tocamos el piso con el nuevo método

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rootRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
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
        // Tira un rayo invisible desde la pelvis hacia abajo. Si choca, estamos en el piso.
        isGrounded = Physics.Raycast(rootRigidbody.position, Vector3.down, groundCheckDistance);
    }

    private void KeepUpright()
    {
        Quaternion currentRotation = rootRigidbody.transform.rotation;
        Quaternion targetRotation = Quaternion.FromToRotation(rootRigidbody.transform.up, Vector3.up) * currentRotation;

        Vector3 axis;
        float angle;
        targetRotation.ToAngleAxis(out angle, out axis);

        if (angle > 180f) angle -= 360f;
        if (angle != 0)
        {
            Vector3 torque = axis.normalized * (angle * Mathf.Deg2Rad * uprightTorque);
            rootRigidbody.AddTorque(torque - rootRigidbody.angularVelocity, ForceMode.Acceleration);
        }
    }

    private void MovePlayer()
    {
        if (Keyboard.current == null) return;

        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current.wKey.isPressed) moveZ += 1f;
        if (Keyboard.current.sKey.isPressed) moveZ -= 1f;
        if (Keyboard.current.dKey.isPressed) moveX += 1f;
        if (Keyboard.current.aKey.isPressed) moveX -= 1f;

        Vector3 moveDirection = new Vector3(moveX, 0f, moveZ).normalized;

        if (moveDirection.magnitude >= 0.1f && rootRigidbody.linearVelocity.magnitude < maxSpeed)
        {
            rootRigidbody.AddForce(moveDirection * walkForce * Time.fixedDeltaTime, ForceMode.VelocityChange);

            Quaternion lookRotation = Quaternion.LookRotation(moveDirection);
            rootRigidbody.transform.rotation = Quaternion.Slerp(rootRigidbody.transform.rotation, lookRotation, Time.fixedDeltaTime * 10f);
        }
    }
}