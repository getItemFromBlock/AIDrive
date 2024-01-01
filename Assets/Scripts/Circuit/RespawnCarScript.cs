using UnityEngine;

public class RespawnCarScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleObject(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleObject(collision.collider);
    }

    private void HandleObject(Collider c)
    {
        if (!c || !c.attachedRigidbody) return;
        var car = c.attachedRigidbody.GetComponent<CarScript>();
        if (!car) return;
        car.KillCar();
    }
}
