using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.Windows;

public class GameCamera : CameraBase
{
    public bool InputLocked = false;

    private float[] speedValues = new float[20];

    public override Vector3 ReadInputs()
    {
        return new Vector3(InputLocked ? 0 : movementInputs.x, movementInputs.y, movementInputs.z);
    }

    protected override void Start()
    {
        base.Start();
        for (int i = 0; i < speedValues.Length; i++)
        {
            speedValues[i] = 0;
        }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        float v = new Vector2(targetRB.velocity.x, targetRB.velocity.z).magnitude;
        float total = v;
        for (int i = speedValues.Length - 1; i > 0; i--)
        {
            total += speedValues[i];
            if (i < 1) continue;
            speedValues[i] = speedValues[i - 1];
        }
        speedValues[0] = v;
        v = total / (speedValues.Length + 1);
        v *= speedScale;
        v = Mathf.Min(v, maxSpeedAngle + UnityEngine.Random.Range(-3, 3));
        speedIndicator.localRotation = Quaternion.AngleAxis(baseSpeedAngle - v, Vector3.forward);
    }
}
