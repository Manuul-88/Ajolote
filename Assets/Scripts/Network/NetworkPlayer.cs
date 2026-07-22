using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayer : MonoBehaviour
{
    [SerializeField]
    Rigidbody rigidbody3D;

    [SerializeField]
    ConfigurableJoint mainJoint;

    //input
    Vector2 moveInputVector = Vector2.zero;
    bool isJumpButtonPressed = false;

    //controller settings
    float moveSpeed = 3;

    //states
    bool isGrounded = false;

    //raycast
    RaycastHit[] raycastHits = new RaycastHit[10];

    // Start is called once before the first execution of Update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //move input
        moveInputVector.x = Input.GetAxis("Horizontal");
        moveInputVector.y = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
            isJumpButtonPressed = true;
    }

    private void FixedUpdate()
    {
        //ground check
        isGrounded = false;
        int hitCount = Physics.RaycastNonAlloc(transform.position, Vector3.down, raycastHits, 1.1f);
        for (int i = 0; i < hitCount; i++)
        {
            if (raycastHits[i].collider != null && raycastHits[i].collider.gameObject != gameObject)
            {
                isGrounded = true;
                break;
            }
        }

        //apply extra gravity to character to make it less floaty
        if (!isGrounded)
            rigidbody3D.AddForce(Vector3.down * 10);

        float inputMagnitude = moveInputVector.magnitude;

        if (inputMagnitude > 0.1f)
        {
            Quaternion desiredDirection = Quaternion.LookRotation(new Vector3(moveInputVector.x, 0, moveInputVector.y * -1), transform.up);

            //rotate target towardsdirections
            mainJoint.targetRotation = Quaternion.RotateTowards(mainJoint.targetRotation, desiredDirection, Time.fixedDeltaTime * 300);

            Vector3 localVelocity = transform.forward * Vector3.Dot(transform.forward, rigidbody3D.linearVelocity);

            float localForwardVelocity = localVelocifyVsForward.magnitude;

            if (localForwardVelocity < maxSpeed)
            {
                rigidbody3D.AddForce(transform.forward * inputMagnitude * 30);
            }
        }
        if(isGrounded && isJumpButtonPressed)
        {
            rigidbody3D.AddForce(Vector3.up * 20, ForceMode.Impulse);
            isJumpButtonPressed = false;
        }
    }
}
