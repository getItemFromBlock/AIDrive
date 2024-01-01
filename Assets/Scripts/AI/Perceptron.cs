using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class IncomingConnection
{
    public float weight;
    public float error_derivate = 0;
    public float acc_error_derivate = 0;
    public int num_acc_error = 0;
    public bool dead = false;
    public Perceptron source;
}

public enum ActivationType : byte
{
    Threshold = 0,
    Sigmoid,
    TanH,
    ReLU
}

public class Perceptron
{
    public float input_derivate = 0;
    public float acc_input_derivate = 0;
    public int num_acc_derivate = 0;
    public int serialized_id = 0;
    public float output_derivate = 0;
    public float current_value = 0;
    public float actual_value = 0;
    public float bias = 0.1f;
    public List<IncomingConnection> connections = new List<IncomingConnection>();
    public List<Perceptron> nextPerceptrons = new List<Perceptron>();
    public ActivationType activation_type = ActivationType.ReLU;

    public static float BETA = 1.0f;

    public Perceptron(Perceptron source, List<Perceptron> raw_neurons)
    {
        bias = source.bias;
        activation_type = source.activation_type;
        for (int i = 0; i < source.connections.Count; i++)
        {
            var sc = source.connections[i];
            IncomingConnection c = new IncomingConnection()
            {
                weight = sc.weight,
                source = raw_neurons[sc.source.serialized_id]
            };
            c.source.nextPerceptrons.Add(this);
            connections.Add(c);
        }
    }

    public Perceptron() { }

    public void Mutate(float amount)
    {
        int val = UnityEngine.Random.Range(0, connections.Count + 1);
        if (val == connections.Count)
        {
            bias += UnityEngine.Random.Range(-amount, amount);
        }
        else
        {
            connections[val].weight += UnityEngine.Random.Range(-amount, amount);
        }
    }

    public void RunActivationFunction()
    {
        switch (activation_type)
        {
            case ActivationType.Threshold:
                current_value = current_value >= 0 ? 1 : 0;
                break;
            case ActivationType.Sigmoid:
                if (current_value < -5) // Anti NAN condition
                {
                    current_value = 0;
                    break;
                }
                if (current_value > 5)
                {
                    current_value = 1;
                    break;
                }
                current_value = 1 / (1 + MathF.Exp(-BETA*current_value));
                break;
            case ActivationType.TanH:
                if (current_value < -5) // Anti NAN condition
                {
                    current_value = -1;
                    break;
                }
                if (current_value > 5)
                {
                    current_value = 1;
                    break;
                }
                float p = MathF.Exp(current_value);
                float n = MathF.Exp(-current_value);
                current_value = (p - n) / (p + n);
                break;
            case ActivationType.ReLU:
                current_value = MathF.Max(0, current_value);
                break;
            default:
                current_value = 0;
                break;
        }
    }

    public float RunDerivateFunction(float v)
    {
        float result;
        switch (activation_type)
        {
            case ActivationType.Threshold:
                result = 1;
                break;
            case ActivationType.Sigmoid:
                result = 1 / (1 + MathF.Exp(-BETA * v));
                result = result * (1 - result);
                break;
            case ActivationType.TanH:
                float p = MathF.Exp(v);
                float n = MathF.Exp(-v);
                result = (p - n) / (p + n);
                result = 1 - result * result;
                break;
            case ActivationType.ReLU:
                result = v <= 0 ? 0 : 1;
                break;
            default:
                result = 0;
                break;
        }
        return result;
    }

    public void UpdateValue()
    {
        actual_value = bias;
        for (int i = 0; i < connections.Count; i++)
        {
            actual_value += connections[i].weight * connections[i].source.current_value;
        }
        current_value = actual_value;
        RunActivationFunction();
    }

    public float GetIncomingWeight(Perceptron from)
    {
        for (int i = 0; i < connections.Count; i++)
        {
            if (connections[i].source == from) return connections[i].weight;
        }
        return 0;
    }

    public void Serialize(ref BinaryWriter bw)
    {
        bw.Write(bias);
        bw.Write((byte)activation_type);
        bw.Write(connections.Count);
        for (int i = 0; i < connections.Count; i++)
        {
            var c = connections[i];
            bw.Write(c.source.serialized_id);
            bw.Write(c.weight);
            bw.Write((byte)(c.dead ? 1 : 0));
        }
    }

    public bool Deserialize(ref BinaryReader br, ref List<Perceptron> raw_neurons)
    {
        bias = br.ReadSingle();
        byte type = br.ReadByte();
        if (type < 0 || type > (byte)ActivationType.ReLU) return false;
        activation_type = (ActivationType)type;
        int cCount = br.ReadInt32();
        if (cCount < 0) return false;
        if (cCount == 0) return true;
        connections.Capacity = cCount;
        for (int i = 0; i < cCount; i++)
        {
            var c = new IncomingConnection();
            int index = br.ReadInt32();
            if (index < 0 || index >= raw_neurons.Count) return false;
            c.source = raw_neurons[index];
            c.weight = br.ReadSingle();
            c.dead = br.ReadByte() != 0;
            c.source.nextPerceptrons.Add(this);
            connections.Add(c);
        }
        return true;
    }

}
