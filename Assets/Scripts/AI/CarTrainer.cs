using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

public class CarTrainer : MonoBehaviour
{
    public Vector2Int gridSize = new Vector2Int(5, 5);
    public int carsPerCircuit = 4;
    public int[] DefaultAILayers = { 27, 20, 18, 16, 14, 12, 3 };
    public TMP_Text GenText;
    public TMP_Text BestText;
    public TMP_Text TimerText;
    //public Button ViewSwitch;
    public NeuronNetworkViewer networkViewer;
    public TrainingCamera cam;
    public GameObject CircuitPrefab;
    public float MutationFactor = 0.1f;
    public int MutationAmount =3;
    public float MinEvaluationTime = 10.0f;
    public float MaxEvaluationTime = 120.0f;
    public float EvaluationIncrementEachTen = 10.0f;
    public int FirstGeneration = 0;
    public bool FocusBest = true;
    //public float[] ReproductionTable = { 0.3f, 0.2f, 0.15f, 0.1f, 0.08f, 0.07f, 0.05f, 0.03f, 0.01f, 0.01f};

    //private GraphRenderer graph;
    private int batch_size;
    private int generation = 0;
    private float timer = 0;
    private List<CarScript> cars = new List<CarScript>();
    private List<MLPNetwork> AIs = new List<MLPNetwork>();
    //private int[] reproductionValues;
    private int focusedCar = 0;
    private float currentEvaluationTime = 0;

    void Start()
    {
        currentEvaluationTime = MinEvaluationTime;
        batch_size = gridSize.x * gridSize.y * carsPerCircuit;
        for (int i = 0; i < gridSize.x; i++)
        {
            for (int j = 0; j < gridSize.y; j++)
            {
                Vector3 delta = new Vector3(200 * i, 0, -200 * j);
                GameObject c = Instantiate(CircuitPrefab, delta, Quaternion.identity, transform);
                CollisionHandler ch = c.GetComponentInChildren<CollisionHandler>();
                var ccars = ch.GetCars();
                foreach (var car in ccars.GetCars())
                {
                    if (car.IsPlayerControlled) continue;
                    cars.Add(car);
                    car.AIStart();
                }
            }
        }
        for (int i = 0; i < cars.Count; i++)
        {
            var car = cars[i];
            var n = new MLPNetwork(DefaultAILayers);
            AIs.Add(n);
            car.SetAINetwork(n);
        }
        if (FirstGeneration > 0)
        {
            Load(FirstGeneration);
        }
        UpdateCam();
        UpdateGen();
        UpdateScore(0);
    }

    void FixedUpdate()
    {
        int last = (int)timer;
        timer += Time.fixedDeltaTime;
        if ((int)timer != last)
        {
            bool valid = false;
            for (int i = 0; i < cars.Count; i++)
            {
                if (cars[i].IsFrozen()) continue;
                valid = true;
                break;
            }
            if (!valid)
            {
                timer = currentEvaluationTime + 1; // All cars are frozen, stop simulation
            }
            else if (FocusBest)
            {
                float max = float.MinValue;
                int index = 0;
                for (int i = 0; i < cars.Count; i++)
                {
                    float s = cars[i].GetScore();
                    if (s > max)
                    {
                        max = s;
                        index = i;
                    }
                }
                focusedCar = index;
                UpdateCam();
            }
        }
        if (timer > currentEvaluationTime)
        {
            Repopulate();
            for (int i = 0; i < cars.Count; i++)
            {
                var c = cars[i];
                c.Restart();
                c.SetAINetwork(AIs[i]);
            }
            timer = Time.time;
            generation++;
            if (generation % 20 == 0)
            {
                SaveAll();
            }
            if (generation % 10 == 0)
            {
                currentEvaluationTime = Mathf.Min(currentEvaluationTime + EvaluationIncrementEachTen, MaxEvaluationTime);
            }
            StartGeneration();
            UpdateGen();
            UpdateCam();
        }
        TimerText.text = "Time left: " + (currentEvaluationTime - timer).ToString("0.0");
    }

    private void Repopulate()
    {
        float min = int.MaxValue;
        bool moved = false;
        for (int i = 0; i < batch_size; i++)
        {
            var c = cars[i];
            min = Mathf.Min(c.GetScore(), min);
            if (c.HasMoved()) moved = true;
        }
        if (moved)
        {
            min -= 10;
            for (int i = 0; i < batch_size; i++)
            {
                cars[i].PunishIfIdle(min);
            }
        }
        List<CarScript> result = cars.OrderByDescending(c => c.GetScore()).ToList();
        UpdateScore(result[0].GetScore());
        result[0].AddBonus(Mathf.Min(generation * 10, 1000));
        float max = result[0].GetScore();
        min = result[batch_size - 1].GetScore();
        float range = max - min;
        float average = 0;
        for (int i = batch_size - 1; i >= 0; i--)
        {
            average += result[i].GetScore();
        }
        average /= batch_size;
        average = (average - min) / range;
        List<MLPNetwork> newAIs = new List<MLPNetwork>();
        int total = 0;
        for (int i = batch_size - 1; i >= 0; i--)
        {
            float d = (result[i].GetScore() - min) / range;
            d /= average;
            float d2 = Mathf.Floor(d);
            float d3 = d - d2;
            int q = (int)(d2 + (UnityEngine.Random.value <= d3 ? 1 : 0));
            total += q;
            if (i == 0)
            {
                q += (batch_size - total) + 10;
                if (q <= 0) q = 1; // Makes sure to always include the best car
            }
            for (int j = 0; j < q; j++)
            {
                var network = result[i].GetAINetwork();
                network.UpdateNeuronIDs();
                newAIs.Add(new MLPNetwork(network, MutationFactor, MutationAmount));
            }
        }
        newAIs.Reverse();
        AIs = newAIs;
        if (AIs.Count > cars.Count) // Can happen because of random
        {
            AIs.RemoveRange(cars.Count, AIs.Count - cars.Count);
        }
    }

    private void UpdateCam()
    {
        if (networkViewer) networkViewer.SetNetwork(AIs[focusedCar]);
        if (cam)
        {
            cam.SetTarget(cars[focusedCar]);
            cam.targetID = focusedCar;
        }
    }

    public void StartGeneration()
    {
        timer = 0;
    }

    public void SaveAll()
    {
        string folder = Application.persistentDataPath + "/AI_Networks/Gen_" + generation.ToString("D7") + '/';
        for (int i = 0; i < AIs.Count; i++)
        {
            string f = folder + "Network_" + i.ToString("D4") + ".ainet";
            if (!AIs[i].SaveToFile(f))
            {
                Debug.LogError("Could not save file " + f + "!");
                break;
            }
        }
    }

    public void Load(int g)
    {
        string folder = Application.persistentDataPath + "/AI_Networks/Gen_" + g.ToString("D7");
        string[] files = Directory.GetFiles(folder, "Network_????.ainet", SearchOption.TopDirectoryOnly);
        if (files.Length <= 0)
        {
            Debug.LogError("Invalid location " + folder + "!");
            return;
        }
        else if (files.Length != AIs.Count)
        {
            Debug.LogWarning("Invalid network count! " + AIs.Count + " expected files, " + files.Length + " found");
        }
        for (int i = 0; i < AIs.Count; i++)
        {
            string file = i >= files.Length ? files[0] : files[i];
            if (!AIs[i].ReadFromFile(file))
            {
                Debug.LogError("Could not read file " + file + "!");
                break;
            }
            int c = AIs[i].GetLayers()[0].Count();
            if (c < DefaultAILayers[0])
            {
                AIs[i].InflateToFit(DefaultAILayers[0]);
            }
            else if (c > DefaultAILayers[0])
            {
                AIs[i].ShrinkToFit(DefaultAILayers[0]);
            }
            cars[i].SetAINetwork(AIs[i]);
        }
        generation = g;
        UpdateCam();
    }

    void UpdateGen()
    {
        GenText.text = "Generation: " + (generation + 1);
    }

    void UpdateScore(float s)
    {
        BestText.text = "Best score: " + s.ToString("0.00");
    }

    public void SelectLeft()
    {
        focusedCar++;
        if (focusedCar >= batch_size) focusedCar = 0;
        UpdateCam();
    }

    public void SelectRight()
    {
        focusedCar--;
        if (focusedCar < 0) focusedCar = batch_size - 1;
        UpdateCam();
    }

    public void ToggleFocus()
    {
        FocusBest = !FocusBest;
    }
}
