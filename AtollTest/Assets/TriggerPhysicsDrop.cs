using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerPhysicsDrop : MonoBehaviour
{
    [SerializeField] private Collider _terrainCollider;

    void OnTriggerEnter(Collider other)
    {
        _terrainCollider.enabled = false;
    }
}
