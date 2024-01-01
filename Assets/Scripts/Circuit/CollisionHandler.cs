using System;
using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    public Vector3[] Rays;
    public Vector3[] ObjectRays;
    public float maxDist = 10;

    private CircuitCollider2D circuit;
    private CarCollider2D cars;

    private LayerMask down;
    private LayerMask up;
    private LayerMask carDown;
    private LayerMask carUp;

    void Awake()
    {
        down = LayerMask.GetMask("2D-Down", "2D-Border");
        up = LayerMask.GetMask("2D-Up", "2D-Border");
        carDown = LayerMask.GetMask("2D-Down", "2D-Border", "2D-CDown");
        carUp = LayerMask.GetMask("2D-Up", "2D-Border", "2D-CUp");
        circuit = transform.parent.GetComponentInChildren<CircuitCollider2D>();
        cars = transform.parent.GetComponentInChildren<CarCollider2D>();
        if (cars.GetCars().Count > 0) // Had to do this because Unity likes to run function in objects with an inconsistent order
        {
            foreach (var c in cars.GetCars())
            {
                c.SetHandler(this);
            }
        }
        else
        {
            cars.NotifyLateAwake(this);
        }
    }

    public void LigmaCarEngine()
    {
        foreach (var c in cars.GetCars())
        {
            c.SetHandler(this);
        }
    }

    public Vector2 GetCorrectDirection(int checkpoint)
    {
        return circuit.GetCircuitDirection(checkpoint);
    }

    public float GetProgress(int checkpoint, Vector2 pos)
    {
        return circuit.GetCheckpointProgress(checkpoint, pos, true);
    }

    public float GetProgressUnclamped(int checkpoint, Vector2 pos)
    {
        return circuit.GetCheckpointProgress(checkpoint, pos, false);
    }

    public float GetTotalProgress(int checkpoint)
    {
        return circuit.GetCheckpointTotalProgress(checkpoint);
    }

    public int GetCheckpointCount()
    {
        return circuit.GetCheckpointCount();
    }

    public float[] PerformRayCast(CarScript c)
    {
        int i;
        var ccars = cars.GetCars();
        for (i = 0; i < ccars.Count; i++)
        {
            if (ccars[i] == c) break;
        }
        var car2D = cars.transform.GetChild(i);
        var tmp = car2D.position;
        Vector3 cUp = car2D.up;
        Vector2 pos = new Vector2(tmp.x, tmp.y);
        float a = MathF.Atan2(cUp.y, cUp.x);
        Vector3 cRight = car2D.right;
        float[] results = new float[Rays.Length + ObjectRays.Length];
        for (i = 0; i < Rays.Length; i++)
        {
            Vector3 r = Rays[i];
            Vector2 dir = new Vector2(MathF.Cos(a + r.x), MathF.Sin(a + r.x));
            Vector2 dPos = new Vector2(cRight.x * r.y + cUp.x * r.z, cRight.y * r.y + cUp.y * r.z);
            var res = Physics2D.Raycast(pos + dPos, dir, 100000.0f, c.layer == CircuitLayer.Lower ? down : up,
                transform.position.z - 0.1f, transform.position.z + 0.1f);

            Debug.DrawLine(car2D.transform.position + new Vector3(dPos.x, dPos.y, 0),
                new Vector3(res.point.x, res.point.y, car2D.transform.position.z), Color.yellow);
            Debug.DrawRay(c.transform.position + new Vector3(dPos.x * cars.GetModelScale(), 0, dPos.y * cars.GetModelScale()),
                new Vector3(dir.x, 0, dir.y) * res.distance * cars.GetModelScale(), Color.yellow);
            results[i] = res.collider ? res.distance : maxDist;
            results[i] = 1 - Mathf.Clamp(results[i], 0, maxDist) / maxDist;
            //results[i] = 1 / (res.distance + 1);
        }
        for (i = 0; i < ObjectRays.Length; i++)
        {
            Vector3 r = ObjectRays[i];
            Vector2 dir = new Vector2(MathF.Cos(a + r.x), MathF.Sin(a + r.x));
            Vector2 dPos = new Vector2(cRight.x * r.y + cUp.x * r.z, cRight.y * r.y + cUp.y * r.z);
            var res = Physics2D.Raycast(pos + dPos, dir, 100000.0f, c.layer == CircuitLayer.Lower ? carDown : carUp,
                transform.position.z - 0.1f, transform.position.z + 0.1f);

            //Debug.DrawLine(car2D.transform.position + new Vector3(dPos.x, dPos.y, 0),
            //    new Vector3(res.point.x, res.point.y, car2D.transform.position.z), Color.red);
            //Debug.DrawRay(c.transform.position + new Vector3(dPos.x * cars.GetModelScale(), 0, dPos.y * cars.GetModelScale()),
            //    new Vector3(dir.x, 0, dir.y) * res.distance * cars.GetModelScale(), Color.red);
            results[i + Rays.Length] = res.collider ? res.distance : maxDist;
            results[i + Rays.Length] = 1 - Mathf.Clamp(results[i + Rays.Length], 0, maxDist) / maxDist;
            //results[i] = 1 / (res.distance + 1);
        }
        return results;
    }

    public CarCollider2D GetCars()
    {
        return cars;
    }
}