using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    private CircuitCollider2D circuit;
    private CarCollider2D cars;

    void Start()
    {
        circuit = transform.parent.GetComponentInChildren<CircuitCollider2D>();
        cars = transform.parent.GetComponentInChildren<CarCollider2D>();
    }

    void Update()
    {
        
    }
}
