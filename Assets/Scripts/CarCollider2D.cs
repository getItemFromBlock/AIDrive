using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarCollider2D : MonoBehaviour
{
    public GameObject Circuit;
    public List<CarScript> Cars;
    public Material carMat;
    public Mesh carMesh;

    private void Start()
    {
        if (Cars == null || Cars.Count == 0) return;
        for (int i = 0; i < Cars.Count; i++)
        {
            var obj = new GameObject(Cars[i].gameObject.name);
            obj.transform.SetParent(transform, false);
            BoxCollider collider = Cars[i].GetComponentInChildren<BoxCollider>();
            var box = obj.AddComponent<BoxCollider2D>();
            box.size = new Vector2(collider.size.x * collider.transform.localScale.x, collider.size.z * collider.transform.localScale.z) / Circuit.transform.localScale.x;
            box.isTrigger = true;
            var obj2 = new GameObject("Mesh");
            obj2.transform.SetParent(obj.transform, false);
            obj2.transform.localRotation = Quaternion.AngleAxis(-90, Vector3.right);
            obj2.transform.localScale = new Vector3(box.size.x, 0, box.size.y) / Circuit.transform.localScale.x;
            obj2.AddComponent<MeshRenderer>().material = carMat;
            obj2.AddComponent<MeshFilter>().mesh = carMesh;
        }
    }

    private void Update()
    {
        for (int i = 0; i < Cars.Count; i++)
        {
            Vector3 dp = (Cars[i].transform.position - Circuit.transform.position) / Circuit.transform.localScale.x;
            var c = transform.GetChild(i);
            c.transform.localPosition = new Vector3(dp.x, dp.z, 0);
            Vector3 rot = Cars[i].transform.rotation * Vector3.forward;
            rot.y = 0;
            if (rot.sqrMagnitude <= 0.0001f) continue;
            c.transform.localRotation = Quaternion.AngleAxis(MathF.Atan2(-rot.x, rot.z) * Mathf.Rad2Deg, Vector3.forward);
        }
    }
}
