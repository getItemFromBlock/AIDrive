using UnityEngine;

public class ChangeLayerScript : MonoBehaviour
{
    public CircuitLayer targetLayer;

    private void OnTriggerEnter(Collider collision)
    {
        if (!collision) return;
        var car = collision.attachedRigidbody.GetComponent<CarScript>();
        if (!car) return;
        car.layer = targetLayer;
    }
}
