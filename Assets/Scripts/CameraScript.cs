using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System;

public class CameraScript : MonoBehaviour
{
    public float distance = 5;
    public Vector3 Rotation = new Vector3(20,0,0);
    public Transform target;

    private Vector2 delta = new Vector2();
    private Vector3 movementInputs = new Vector3();
    private Vector3 lastRot = new Vector3();
    private Vector2 gDelta = new Vector2();
    private bool gCamLocked = true;

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            delta = Input.mousePosition;
        }
        if (Input.GetMouseButton(1))
        {
            Vector2 dif = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - delta;
            delta = Input.mousePosition;
            Rotation.x -= dif.y * 0.5f;
            Rotation.y += dif.x * 0.5f;
        }
        if (!gCamLocked)
        {
            Rotation.x -= gDelta.y * Time.deltaTime * 180.0f;
            Rotation.y += gDelta.x * Time.deltaTime * 180.0f;
        }
        Rotation.x = Mathf.Clamp(Rotation.x, -90, 90);
        Rotation.y = Rotation.y % 360.0f;
        float tmp = distance;
        distance -= Input.mouseScrollDelta.y;
        if (distance <= 0)
        {
            distance = 0;
            if (tmp > 0)
            {
                Rotation = Vector3.zero;
            }
        }
        else if (tmp <= 0)
        {
            Rotation = new Vector3(20, 0, 0);
        }
        Vector3 rot = target.rotation * Vector3.forward;
        rot.y = 0;
        if (rot.sqrMagnitude <= 0.0001f)
        {
            rot = lastRot;
        }
        else
        {
            lastRot = rot;
        }
        float angle = MathF.Atan2(rot.x, rot.z);
        Quaternion r = Quaternion.Euler(Rotation + new Vector3(0, angle * Mathf.Rad2Deg, 0));
        transform.position = target.position + Vector3.up + rot - r * new Vector3(0,0,distance);
        transform.rotation = r;
    }

    public Vector3 ReadInputs()
    {
        return movementInputs;
    }

    public void UpdateSteering(InputAction.CallbackContext context)
    {
        movementInputs.z = context.ReadValue<float>();
    }

    public void UpdateThrottle(InputAction.CallbackContext context)
    {
        movementInputs.x = context.ReadValue<float>();
    }

    public void UpdateBraking(InputAction.CallbackContext context)
    {
        movementInputs.y = context.ReadValue<float>();
    }

    public void UpdateCamera(InputAction.CallbackContext context)
    {
        gDelta = context.ReadValue<Vector2>();
    }

    public void LockCamera(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        gCamLocked = !gCamLocked;
    }

    public void SwitchCamera(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (distance > 0)
        {
            distance = 0;
            Rotation = Vector3.zero;
        }
        else
        {
            Rotation = new Vector3(20,0,0);
            distance = 5;
        }
    }
}
