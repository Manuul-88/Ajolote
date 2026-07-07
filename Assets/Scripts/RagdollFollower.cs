using UnityEngine;

public class BasicRagdollMovement : MonoBehaviour
{
    public Rigidbody rootRigidbody;
    public Animator ghostAnimator; // El animator del fantasma
    public float moveSpeed = 15f;

    void FixedUpdate()
    {
        // 1. Lees el control (Flechas o WASD)
        float moveH = Input.GetAxis("Horizontal");
        float moveV = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(moveH, 0, moveV).normalized;

        // 2. Si te estás moviendo, empujas la pelvis con físicas
        if (moveDirection.magnitude > 0.1f)
        {
            rootRigidbody.AddForce(moveDirection * moveSpeed, ForceMode.Force);

            // Le decimos al fantasma que reproduzca la animación de caminar
            ghostAnimator.SetBool("isWalking", true);
        }
        else
        {
            ghostAnimator.SetBool("isWalking", false);
        }
    }
}