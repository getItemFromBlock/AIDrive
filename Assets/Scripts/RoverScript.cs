using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoverScript : MonoBehaviour
{
    public Rigidbody[] wheelsBodies;
    public HingeJoint[] wheels;
    public HingeJoint[] rJoints;

    void Start()
    {
        for (int i = 0; i < wheelsBodies.Length; i++)
        {
            wheelsBodies[i].maxAngularVelocity = 1000;
        }
    }

    private void FixedUpdate()
    {
        Vector3 controls = new Vector3();
        controls.z -= Input.GetKey(KeyCode.D) ? 1.0f : 0.0f;
        controls.z += Input.GetKey(KeyCode.A) ? 1.0f : 0.0f;
        controls.x += Input.GetKey(KeyCode.W) ? 1.0f : 0.0f;
        controls.x -= Input.GetKey(KeyCode.S) ? 1.0f : 0.0f;
        /*
        controls = Quaternion.AngleAxis(Rotation.y, Vector3.up) * controls;
        controls = controls * Mathf.Min(controls.magnitude, 1.0f) * 100;
        rb.AddTorque(controls, ForceMode.Acceleration);
        */
        JointMotor m = new JointMotor();
        m.force = 30;
        m.freeSpin = true;
        m.targetVelocity = controls.x * 1000;
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].motor = m;
            if (i == 1) m.targetVelocity = -m.targetVelocity;
        }
        JointSpring m2 = new JointSpring();
        m2.spring = 1000;
        m2.damper = 100;
        m2.targetPosition = controls.z * -25;
        for (int i = 0; i < rJoints.Length; i++)
        {
            rJoints[i].spring = m2;
            if (i == 1) m2.targetPosition = -m2.targetPosition;
        }
    }
}
