using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreCollision : MonoBehaviour
{
    [SerializeField]
    Collider thisCollider;

    [SerializeField]
    Collider[] colliderToIgnore;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (Collider otherCollider in colliderToIgnore)
        {
            Physics.IgnoreCollision(thisCollider, otherCollider);
        }
    }


}
