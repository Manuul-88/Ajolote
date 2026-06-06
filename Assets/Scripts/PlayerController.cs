using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    public float speed = 5f;

    void Update()
    {
        if (!IsOwner)
            return;

        Vector2 move = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                move.y += 1;

            if (Keyboard.current.sKey.isPressed)
                move.y -= 1;

            if (Keyboard.current.aKey.isPressed)
                move.x -= 1;

            if (Keyboard.current.dKey.isPressed)
                move.x += 1;
        }

        Vector3 direction =
            new Vector3(move.x, 0, move.y);

        transform.Translate(
            direction.normalized *
            speed *
            Time.deltaTime,
            Space.World);
    }
}