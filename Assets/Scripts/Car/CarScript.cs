using System.Collections.Generic;
using UnityEngine;

public enum CircuitLayer : byte
{
    Lower = 0,
    Upper,

    Count
}

public class CarScript : MonoBehaviour
{
    public WheelCollider[] wheelsColliders;
    public GameObject[] wheelsMeshes;
    public float maxMotorTorque = 100;
    public float maxBrakeTorque = 100;
    public float maxSteeringAngle = 25;
    public float maxSpeed = 1000;
    public CircuitLayer layer = CircuitLayer.Lower;
    public bool IsPlayerControlled = false;
    public float MaxStuckTime = 3.0f;
    public float ProgressScore = 0.1f;
    public float CheckPointScore = 300.0f;
    public float KillScore = -10000.0f;
    public float FlipScore = -10000.0f;
    public bool UseFreezing = false;
    public bool RespawnWhenAIGoesRogue = false;

    private Vector3 inputs = new Vector3();
    private float lastSteering = 0;
    private CameraBase mainCam;
    private MLPNetwork AINetwork;
    private CollisionHandler cHandler;
    private float stuckDelay = 0;
    private int checkPointID = -1;
    private Vector3 checkPointPos;
    private Quaternion checkPointRot;
    private Rigidbody rb;
    private Vector2 targetDir = Vector2.up;
    private Vector3 startPos;
    private Quaternion startRot;
    private Vector3 dCheckPointPos;
    private Quaternion dCheckPointRot;
    private int dCheckPointID;
    private Vector2 dtargetDir;
    private float score = 0;
    private float checkpointProgress = 0;
    private int borderLayer;
    private int turn = 0;
    private int lastValidCheckpoint = 0;
    private bool hasMoved = false;
    private bool frozen = false;

    void SetInputs(float throttle, float brake, float steer)
    {
        inputs = new Vector3(Mathf.Clamp(throttle, -1, 1), Mathf.Clamp01(brake), Mathf.Clamp(steer, -1, 1));
    }

    public Vector3 ReadInputs()
    {
        return inputs;
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

    public void SetAINetwork(MLPNetwork input)
    {
        AINetwork = input;
    }

    public MLPNetwork GetAINetwork()
    {
        return AINetwork;
    }

    public void Restart()
    {
        hasMoved = false;
        stuckDelay = 0;
        checkPointID = dCheckPointID;
        checkPointPos = dCheckPointPos;
        checkPointRot = dCheckPointRot;
        frozen = false;
        rb.isKinematic = false;
        rb.transform.position = startPos;
        rb.transform.rotation = startRot;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        targetDir = dtargetDir;
        for (int i = 0; i < wheelsColliders.Length; i++)
        {
            wheelsColliders[i].rotationSpeed = 0.0f;
        }
        inputs = Vector3.zero;
        lastSteering = 0;
        layer = CircuitLayer.Lower;
        AINetwork = null;
        score = 0;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (IsPlayerControlled)
        {
            mainCam = GameObject.Find("MainCamera").GetComponent<CameraBase>();
        }
        borderLayer = LayerMask.NameToLayer("Border");
    }

    public void AIStart()
    {
        startPos = transform.position;
        startRot = transform.rotation;
    }

    private void FixedUpdate()
    {
        if (UseFreezing && frozen) return;
        bool forward = true;
        if (!IsPlayerControlled && AINetwork != null)
        {
            List<float> inputs = new List<float>(29);
            var rays = cHandler.PerformRayCast(this);
            for (int i = 0; i < 12; i++)
            {
                inputs.Add(rays[i]);
            }
            Vector3 euler = rb.rotation.eulerAngles;
            if (euler.x > 180.0f) euler.x -= 360.0f;
            if (euler.z > 180.0f) euler.z -= 360.0f;
            inputs.Add(euler.x / 180);
            inputs.Add(euler.z / 180);
            Vector3 f = transform.forward;
            Vector2 d = new Vector2(f.x, f.z);
            forward = Vector2.Dot(d, targetDir) >= 0;
            inputs.Add(forward ? 1 : -1);
            inputs.Add(Vector2.Dot(new Vector2(rb.velocity.x, rb.velocity.z), d));
            inputs.Add(lastSteering);
            AddWheelStates(ref inputs);
            for (int i = 12; i < 20; i++)
            {
                inputs.Add(rays[i]);
            }
            AINetwork.GenerateOutput(inputs);
            var outputs = AINetwork.GetOutputs();
            SetInputs(outputs[0], outputs[1], outputs[2]);
        }
        HandleControls();
        Vector2 p = new Vector2(transform.position.x, transform.position.z);
        float cPos = cHandler.GetProgress(checkPointID, p);
        float inc = ProgressScore * (cPos - checkpointProgress);
        checkpointProgress = cPos;
        if (!forward && inc > 0) inc = 0;
        score += inc;
        if (Mathf.Abs(score) > 50) hasMoved = true;
        if (!WheelsOnGround())
        {
            stuckDelay += Time.fixedDeltaTime;
        }
        else
        {
            stuckDelay = 0;
        }
        if (stuckDelay > MaxStuckTime)
        {
            score += FlipScore;
            RespawnCar();
        }
    }

    private void HandleControls()
    {
        Vector3 controls;
        if (IsPlayerControlled)
        {
            if (!mainCam) mainCam = GameObject.Find("MainCamera").GetComponent<CameraBase>();
            controls = mainCam.ReadInputs();
        }
        else
        {
            controls = inputs;
        }
        float c = controls.x;
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

    private void AddWheelStates(ref List<float> inputs)
    {
        for (int i = 0; i < wheelsColliders.Length; i++)
        {
            inputs.Add(wheelsColliders[i].isGrounded ? 1 : 0);
        }
    }

    private bool WheelsOnGround()
    {
        int c = 0;
        int states = 0;
        for (int i = 0; i < wheelsColliders.Length; i++)
        {
            if (wheelsColliders[i].isGrounded)
            {
                c++;
                states |= 1 << i;
            }
        }
        // Either we have less that 2 wheels on the ground, or we have the two front/back wheels not grounded
        return !(c < 2 || (c == 2 && (states == 0b1100 || states == 0b0011)));
    }

    private void RespawnCar()
    {
        stuckDelay = 0;
        frozen = false;
        rb.isKinematic = false;
        rb.transform.position = checkPointPos;
        rb.transform.rotation = checkPointRot;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public void KillCar()
    {
        score += KillScore;

        RespawnCar();
    }

    public void SetCheckPoint(int id, Vector3 respawnPos, float respawnRot)
    {
        if (id == checkPointID) return;
        checkPointPos = respawnPos;
        checkPointRot = Quaternion.AngleAxis(respawnRot, Vector3.up);
        if (checkPointID < 0)
        {
            lastValidCheckpoint = id;
            checkPointID = id;
            targetDir = cHandler.GetCorrectDirection(checkPointID);
            dCheckPointPos = checkPointPos;
            dCheckPointRot = checkPointRot;
            dCheckPointID = id;
            dtargetDir = targetDir;
            checkpointProgress = cHandler.GetProgress(id, new Vector2(transform.position.x, transform.position.z));
            return;
        }
        bool progressed = (id > checkPointID &&
            (id != cHandler.GetCheckpointCount() || checkPointID > cHandler.GetCheckpointCount() / 2))
            || (id == 1 && checkPointID == cHandler.GetCheckpointCount());
        if (progressed)
        {
            score += CheckPointScore;
            score += (cHandler.GetTotalProgress(checkPointID) - checkpointProgress) * ProgressScore;
            checkpointProgress = 0;
            if (cHandler.GetCheckpointCount() == id && lastValidCheckpoint == id - 1)
            {
                turn++;
            }
            lastValidCheckpoint = id;
        }
        else
        {
            score -= CheckPointScore;
            if (!IsPlayerControlled && RespawnWhenAIGoesRogue)
            {
                RespawnCar();
                return;
            }
            checkpointProgress = cHandler.GetProgress(id, new Vector2(transform.position.x, transform.position.z));
        }
        checkPointID = id;
        targetDir = cHandler.GetCorrectDirection(checkPointID);
    }

    public void SetHandler(CollisionHandler c)
    {
        cHandler = c;
    }

    public float GetScore()
    {
        return score;
    }

    public void PunishIfIdle(float minScore)
    {
        if (hasMoved) return;
        score = minScore;
    }

    public void AddBonus(float value)
    {
        score += value;
    }

    public bool HasMoved()
    {
        return hasMoved;
    }

    /*
    private void OnCollisionStay(Collision collision)
    {
        if (collision == null || collision.collider.gameObject.layer != borderLayer) return;
        score += BorderScore * Time.fixedDeltaTime;
    }
    */

    private void OnCollisionEnter(Collision collision)
    {
        if (!UseFreezing || frozen || collision == null || collision.collider.gameObject.layer != borderLayer) return;
        hasMoved = true;
        frozen = true;
        rb.isKinematic = true;
    }

    public bool IsFrozen()
    {
        return frozen;
    }

    public int GetTurn()
    {
        return turn;
    }

    public int GetCheckpoint()
    {
        return checkPointID;
    }
}
