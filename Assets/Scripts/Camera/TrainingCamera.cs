using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TrainingCamera : CameraBase
{
    public TMP_Text Score;
    public TMP_Text Inputs;
    public TMP_Text TargetText;
    public int targetID = 0;

    private CarScript car;

    protected override void Start()
    {
        base.Start();
        car = target.GetComponent<CarScript>();
    }

    public override void SetTarget(CarScript t)
    {
        base.SetTarget(t);
        car = t;
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        float v = new Vector2(targetRB.velocity.x, targetRB.velocity.z).magnitude;
        v *= speedScale;
        v = Mathf.Min(v, maxSpeedAngle + UnityEngine.Random.Range(-3, 3));
        speedIndicator.localRotation = Quaternion.AngleAxis(baseSpeedAngle - v, Vector3.forward);

        Score.text = "Score: " + car.GetScore().ToString("0.00");
        Vector3 inputs = car.ReadInputs();
        Inputs.text = "T: " + inputs.x.ToString("0.0") + " B: " + inputs.y.ToString("0.0") + " S: " + inputs.z.ToString("0.0");
        TargetText.text = "Target: " + targetID.ToString();
    }
}
