using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class CarCollider2D : MonoBehaviour
{
    public GameObject Circuit;
    public Material carMat;
    public Mesh carMesh;
    public Transform CarsFolder;
    public bool ShowCars = true;

    private List<CarScript> cars = new List<CarScript>();
    private float scale;
    private CollisionHandler ch = null;
    private int layerUp;
    private int layerDown;

    public float GetModelScale()
    {
        return scale;
    }

    private void Awake()
    {
        layerUp = LayerMask.NameToLayer("2D-CUp");
        layerDown = LayerMask.NameToLayer("2D-CDown");
        scale = Circuit.transform.localScale.x;
        if (CarsFolder == null || CarsFolder.childCount == 0) return;
        for (int i = 0; i < CarsFolder.childCount; i++)
        {
            var car = CarsFolder.GetChild(i);
            var c = car.GetComponent<CarScript>();
            cars.Add(c);
            var obj = new GameObject(car.name);
            obj.transform.SetParent(transform, false);
            Vector3 dp = (c.transform.position - Circuit.transform.position) / scale;
            obj.transform.localPosition = new Vector3(dp.x, dp.z, 0);
            Vector3 rot = c.transform.rotation * Vector3.forward;
            rot.y = 0;
            obj.transform.localRotation = Quaternion.AngleAxis(MathF.Atan2(-rot.x, rot.z) * Mathf.Rad2Deg, Vector3.forward);
            BoxCollider collider = car.GetComponentInChildren<BoxCollider>();
            var box = obj.AddComponent<BoxCollider2D>();
            box.size = new Vector2(collider.size.x * collider.transform.localScale.x, collider.size.z * collider.transform.localScale.z) / scale;
            box.isTrigger = false;
            if (!ShowCars) continue;
            var obj2 = new GameObject("Mesh");
            obj2.transform.SetParent(obj.transform, false);
            obj2.transform.localRotation = Quaternion.AngleAxis(-90, Vector3.right);
            obj2.transform.localScale = new Vector3(box.size.x, 0, box.size.y) / 10;
            obj2.AddComponent<MeshRenderer>().material = carMat;
            obj2.AddComponent<MeshFilter>().mesh = carMesh;
        }
        if (ch != null)
        {
            ch.LigmaCarEngine();
        }
    }

    public void NotifyLateAwake(CollisionHandler c)
    {
        ch = c;
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < cars.Count; i++)
        {
            Vector3 dp = (cars[i].transform.position - Circuit.transform.position) / scale;
            var c = transform.GetChild(i);
            c.gameObject.layer = cars[i].layer == CircuitLayer.Lower ? layerDown : layerUp;
            c.localPosition = new Vector3(dp.x, dp.z, 0);
            Vector3 rot = cars[i].transform.rotation * Vector3.forward;
            rot.y = 0;
            if (rot.sqrMagnitude <= 0.0001f) continue;
            c.transform.localRotation = Quaternion.AngleAxis(MathF.Atan2(-rot.x, rot.z) * Mathf.Rad2Deg, Vector3.forward);
        }
    }

    public CarScript GetCar(GameObject c)
    {
        for (int i = 0; i < cars.Count; i++)
        {
            if (cars[i].gameObject == c) return cars[i];
        }
        return null;
    }

    public List<CarScript> GetCars()
    {
        return cars;
    }
}
