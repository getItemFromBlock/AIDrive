using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MLPNetwork
{
    private List<float> cached_outputs = new List<float>();
    private List<List<Perceptron>> layers = new List<List<Perceptron>>();

    public float LearnRate = 0.3f;

    public MLPNetwork()
    {
    }

    public MLPNetwork(int[] layerCount)
    {
        for (int i = 0; i < layerCount.Length; i++)
        {
            List<Perceptron> layer = new List<Perceptron>();
            for (int j = 0; j < layerCount[i]; j++)
            {
                layer.Add(new Perceptron());
            }
            layers.Add(layer);
        }
        BindLayers();
    }

    public MLPNetwork(MLPNetwork source, float mutationFactor, int mutationCount)
    {
        List<Perceptron> raw_neurons = new List<Perceptron>();
        layers = new List<List<Perceptron>>(source.layers.Count);
        for (int i = 0; i < source.layers.Count; i++)
        {
            var l = source.layers[i];
            List<Perceptron> layer = new List<Perceptron>(l.Count);
            for (int j = 0; j < l.Count; j++)
            {
                Perceptron p = new Perceptron(l[j], raw_neurons);
                raw_neurons.Add(p);
                layer.Add(p);
            }
            layers.Add(layer);
        }
        for (int i = 0; i < mutationCount; i++)
        {
            int index = UnityEngine.Random.Range(layers[0].Count, raw_neurons.Count);
            raw_neurons[index].Mutate(mutationFactor);
        }
    }

    void BindLayers()
    {
        for (int i = 0; i < layers[0].Count; i++)
        {
            layers[0][i].bias = 0;
        }
        for (int i = 1; i < layers.Count; i++)
        {
            List<Perceptron> layer = layers[i];
            for (int j = 0; j < layer.Count; j++)
            {
                layer[j].bias = UnityEngine.Random.Range(-0.2f, 0.2f);
            }
            List<Perceptron> layerP = layers[i-1];
            List<Perceptron> layerN = i == layers.Count - 1 ? null : layers[i+1];
            for (int j = 0; j < layer.Count; j++)
            {
                Perceptron p = layer[j];
                for (int k = 0; k < layerP.Count; k++)
                {
                    IncomingConnection c = new IncomingConnection();
                    c.weight = UnityEngine.Random.Range(-0.5f, 0.5f);
                    c.source = layerP[k];
                    p.connections.Add(c);
                }
                if (layerN == null)
                {
                    p.activation_type = ActivationType.TanH;
                }
                else
                {
                    p.activation_type = ActivationType.ReLU;
                    for (int k = 0; k < layerN.Count; k++)
                    {
                        p.nextPerceptrons.Add(layerN[k]);
                    }
                }
            }
        }
    }

    public void GenerateOutput(List<float> inputs)
    {
        for (int i = 0; i < inputs.Count && i < layers[0].Count; i++)
        {
            layers[0][i].current_value = inputs[i];
        }
        for (int i = 1; i < layers.Count; i++)
        {
            var layer = layers[i];
            for (int j = 0; j < layer.Count; j++)
            {
                layer[j].UpdateValue();
            }
        }
        cached_outputs.Clear();
        var last = layers[layers.Count - 1];
        for (int i = 0; i < last.Count; i++)
        {
            cached_outputs.Add(last[i].current_value);
        }
    }

    public List<float> GetOutputs()
    {
        return cached_outputs;
    }

    public void LearnPattern(List<float> inputs, List<float> expectedOutputs)
    {
        GenerateOutput(inputs);
        BackPropagation(expectedOutputs);
    }

    public void UpdateWeights()
    {
        UpdateWeights(LearnRate);
    }

    private void BackPropagation(List<float> expectedOutputs)
    {
        for (int i = layers.Count - 1; i > 0; i--)
        {
            var layer = layers[i];
            for (int j = 0; j < layer.Count; j++)
            {
                Perceptron p = layer[j];
                if (i == layers.Count - 1)
                {
                    p.output_derivate = p.current_value - expectedOutputs[Mathf.Min(j, expectedOutputs.Count - 1)];
                }
                p.input_derivate = p.output_derivate * p.RunDerivateFunction(p.actual_value);
                p.acc_input_derivate += p.input_derivate;
                p.num_acc_derivate++;

                for (int k = 0; ++k < p.connections.Count; ++k)
                {
                    var c = p.connections[k];
                    if (c.dead) continue;
                    c.error_derivate = p.input_derivate * c.source.current_value;
                    c.acc_error_derivate += c.error_derivate;
                    c.num_acc_error++;
                }
            }
            if (i == 1) break;
            var prev = layers[i - 1];
            for (int j = 0; j < prev.Count; j++)
            {
                Perceptron p = prev[j];
                p.output_derivate = 0;
                for (int k = 0; k < p.nextPerceptrons.Count; k++)
                {
                    Perceptron output = p.nextPerceptrons[k];
                    p.output_derivate += output.GetIncomingWeight(p) * output.input_derivate;
                }
            }
        }
    }

    private void UpdateWeights(float learnRate)
    {
        for (int i = 1; i < layers.Count; i++)
        {
            var layer = layers[i];
            for (int j = 0; j < layer.Count; j++)
            {
                Perceptron p = layer[j];
                if (p.num_acc_derivate > 0)
                {
                    p.bias -= learnRate * p.acc_input_derivate / p.num_acc_derivate;
                    p.acc_input_derivate = 0;
                    p.num_acc_derivate = 0;
                }
                for (int k = 0; k < p.connections.Count; k++)
                {
                    var c = p.connections[k];
                    if (c.dead) continue;
                    if (c.num_acc_error > 0)
                    {
                        c.weight -= (learnRate / c.num_acc_error) * c.acc_error_derivate;
                        c.acc_error_derivate = 0;
                        c.num_acc_error = 0;
                    }
                }
            }
        }
    }

    public List<List<Perceptron>> GetLayers()
    {
        return layers;
    }

    public void UpdateNeuronIDs()
    {
        int nCounter = 0;
        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            for (int j = 0; j < layer.Count; j++)
            {
                layer[j].serialized_id = nCounter++;
            }
        }
    }

    public bool SaveToFile(string path)
    {
        UpdateNeuronIDs();

        FileStream file;
        if (File.Exists(path))
        {
            file = File.OpenWrite(path);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            file = File.Create(path);
        }
        if (!file.CanWrite) return false;
        BinaryWriter bw = new BinaryWriter(file);
        bw.Write(layers.Count);
        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            bw.Write(layer.Count);
            for(int j = 0; j < layer.Count; j++)
            {
                layer[j].Serialize(ref bw);
            }
        }
        
        file.Close();
        return true;
    }

    public bool ReadFromFile(string path)
    {
        layers.Clear();
        FileStream file;
        if (!File.Exists(path)) return false;
        file = File.OpenRead(path);
        BinaryReader br = new BinaryReader(file);
        bool res = ReadFromFile(br);
        if (res) file.Close();
        return res;
    }

    public bool ReadFromFile(byte[] data)
    {
        layers.Clear();
        MemoryStream s = new MemoryStream(data, false);
        BinaryReader br = new BinaryReader(s);
        return ReadFromFile(br);
    }

    public bool ReadFromFile(BinaryReader br)
    {
        int lCount = br.ReadInt32();
        if (lCount <= 0) return false;
        layers.Capacity = lCount;
        List<Perceptron> raw_neurons = new List<Perceptron>();
        for (int i = 0; i < lCount; i++)
        {
            var layer = new List<Perceptron>();
            int pCount = br.ReadInt32();
            if (pCount <= 0) return false;
            layer.Capacity = pCount;
            for (int j = 0; j < pCount; j++)
            {
                Perceptron p = new Perceptron();
                if (!p.Deserialize(ref br, ref raw_neurons)) return false;
                raw_neurons.Add(p);
                layer.Add(p);
            }
            layers.Add(layer);
        }
        return true;
    }

    public void ShrinkToFit(int inputCount)
    {
        UpdateNeuronIDs();
        if (layers[0].Count <= inputCount) return;
        var l = layers[1];
        for (int i = 0; i < l.Count; i++)
        {
            var p = l[i];
            for (int j = 0; j < p.connections.Count; j++)
            {
                var c = p.connections[j];
                if (c.source.serialized_id < inputCount) continue;
                p.connections.RemoveAt(j);
                j--;
            }
        }
        l = layers[0];
        for (int i = l.Count - 1; i >= inputCount; i--)
        {
            l.RemoveAt(i);
        }
    }

    public void InflateToFit(int inputCount)
    {
        var l = layers[0];
        var l1 = layers[1];
        if (l.Count >= inputCount) return;
        for (int i = l.Count; i < inputCount; i++)
        {
            Perceptron p = new Perceptron
            {
                bias = 0,
                activation_type = ActivationType.ReLU
            };
            for (int j = 0; j < l1.Count; j++)
            {
                var p1 = l1[j];
                IncomingConnection c = new IncomingConnection
                {
                    source = p,
                    weight = UnityEngine.Random.Range(-0.1f, 0.1f)
                };
                p1.connections.Add(c);
                p.nextPerceptrons.Add(p1);
            }
            l.Add(p);
        }
        UpdateNeuronIDs();
    }
}
