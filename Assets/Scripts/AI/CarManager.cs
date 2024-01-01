using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    public List<CarScript> Cars;
    public List<TextAsset> TrainedAIs;
    public CollisionHandler collisionHandler;
    public List<Color> PositionColors;
    public int PlayerCarIndex = 0;
    public TMP_Text PositionText;
    public float TextFadeTime = 0.5f;
    public List<string> StartTexts;
    public TMP_Text StartText;
    public GameCamera Camera;

    private float timer = 0;
    private float fadeTimer = 0;
    private Color fadeColor;

    void Start()
    {
        if (TrainedAIs.Count <= 1)
        {
            Debug.LogError("Please assign at least one trained AI to the TrainedAIs field");
            return;
        }
        timer = StartTexts.Count-1;
        fadeColor = StartText.color;
        Camera.InputLocked = true;
    }

    void FixedUpdate()
    {
        if (timer > 0)
        {
            int f = Mathf.FloorToInt(timer);
            timer -= Time.fixedDeltaTime;
            if (f != Mathf.FloorToInt(timer))
            {
                fadeColor.a = 1;
                StartText.color = fadeColor;
                StartText.text = StartTexts[StartTexts.Count-f-1];
                fadeTimer = TextFadeTime;
            }
            if (timer <= 0)
            {
                for (int i = 0; i < Cars.Count; i++)
                {
                    var car = Cars[i];
                    if (car.IsPlayerControlled) continue;
                    var AI = TrainedAIs[UnityEngine.Random.Range(0, TrainedAIs.Count - 1)];
                    MLPNetwork n = new MLPNetwork();
                    n.ReadFromFile(AI.bytes);
                    car.AIStart();
                    car.SetAINetwork(n);
                }
                Camera.InputLocked = false;
            }
        }
        if (fadeTimer > 0)
        {
            fadeColor.a = fadeTimer / TextFadeTime;
            fadeTimer -= Time.fixedDeltaTime;
            StartText.color = fadeColor;
        }
        else if (fadeTimer <= 0)
        {
            fadeColor.a = 0;
            StartText.color = fadeColor;
        }
        List<Tuple<float, int>> cars = new();
        for (int i = 0; i < Cars.Count; i++)
        {
            var c = Cars[i];
            int turn = c.GetTurn();
            int cp = c.GetCheckpoint() % collisionHandler.GetCheckpointCount();
            var tmp = c.transform.position;
            float p = collisionHandler.GetProgressUnclamped(cp, new Vector2(tmp.x, tmp.z));
            cars.Add(new Tuple<float, int>(p + 1000 * cp + 100000 * turn, i));
        }
        var result = cars.OrderByDescending(c => c.Item1).ToList();
        int index = 0;
        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].Item2 == PlayerCarIndex)
            {
                index = i;
                break;
            }
        }
        PositionText.text = (index + 1).ToString();
        PositionText.color = PositionColors[index];
    }
}
