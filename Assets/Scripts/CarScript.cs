using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;

public class CarScript : MonoBehaviour
{
    public WheelCollider[] wheelsColliders;
    public GameObject[] wheelsMeshes;
    public float maxMotorTorque = 100;
    public float maxBrakeTorque = 100;
    public float maxSteeringAngle = 25;
    public float maxSpeed = 1000;
    public bool IsPlayerControlled = false;

    private Vector3 inputs = new Vector3();
    private float lastSteering = 0;
    private CameraScript mainCam;

    void SetInputs(float throttle, float brake, float steer)
    {
        inputs = new Vector3(Mathf.Clamp(throttle, -1, 1), Mathf.Clamp01(brake), Mathf.Clamp(steer, -1, 1));
    }

    void ApplyLocalPositionToVisuals(int index)
    {
        if (index >= wheelsColliders.Length || index >= wheelsMeshes.Length) return;
        Vector3 position;
        Quaternion rotation;
        wheelsColliders[index].GetWorldPose(out position, out rotation);

        wheelsMeshes[index].transform.position = position;
        wheelsMeshes[index].transform.rotation = rotation;
    }

    private void Start()
    {
        for (int i = 0; i < wheelsColliders.Length; i++)
        {
            wheelsColliders[i].rotationSpeed = 10.0f;
        }
        if (IsPlayerControlled) mainCam = GameObject.Find("MainCamera").GetComponent<CameraScript>();
    }

    private void FixedUpdate()
    {
        Vector3 controls;
        if (IsPlayerControlled)
        {
            controls = mainCam.ReadInputs();
        }
        else
        {
            controls = inputs;
        }
        controls.x *= maxMotorTorque;
        controls.y *= maxBrakeTorque;
        controls.z *= maxSteeringAngle;
        lastSteering = Mathf.MoveTowards(lastSteering, controls.z, 360 * Time.fixedDeltaTime);
        for (int i = 0; i < wheelsColliders.Length; i++)
        {
            float f = 1 - Mathf.Abs(wheelsColliders[i].rotationSpeed) / maxSpeed;
            wheelsColliders[i].motorTorque = controls.x * (f * f);
            wheelsColliders[i].brakeTorque = controls.y;
            if (i < 2)
            {
                wheelsColliders[i].steerAngle = lastSteering;
            }
            ApplyLocalPositionToVisuals(i);
        }
    }
}
