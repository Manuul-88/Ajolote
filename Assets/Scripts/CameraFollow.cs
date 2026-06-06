using Unity.Netcode;
using UnityEngine;

public class CameraFollow : NetworkBehaviour
{
    void Start()
    {
        if (!IsOwner)
            return;

        Camera.main.transform.SetParent(transform);

        Camera.main.transform.localPosition =
            new Vector3(0, 5, -8);

        Camera.main.transform.localRotation =
            Quaternion.Euler(20, 0, 0);
    }
}