using UnityEngine;

public class CheckPointScript : MonoBehaviour
{
    public int ID = 0;
    public Vector3 RespawnDeltaPos = Vector3.up;
    public bool IsStart = false;

    private float yRot;

    [HideInInspector] public float scoreValue;
    [HideInInspector] public Vector2 direction;
    [HideInInspector] public Vector2 position;

    public void Start()
    {
        yRot = transform.localRotation.eulerAngles.z + 90;
        if (IsStart)
        {
            var cars = transform.parent.parent.parent.GetComponentInChildren<CarCollider2D>();
            if (!cars)
            {
                Debug.LogError("No cars ?");
            }
            var c = cars.GetCars();
            for (int i = 0; i < c.Count; i++)
            {
                c[i].SetCheckPoint(ID, transform.position + RespawnDeltaPos, yRot);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other || !other.attachedRigidbody) return;
        var car = other.attachedRigidbody.GetComponent<CarScript>();
        if (!car) return;
        car.SetCheckPoint(ID, transform.position + RespawnDeltaPos, yRot);
    }
}
