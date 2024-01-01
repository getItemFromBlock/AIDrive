using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NeuronNetworkViewer : MonoBehaviour
{
    public GameObject LayerPrefab;
    public GameObject NeuronPrefab;
    public MeshFilter TargetMesh;
    public Vector2 Scale;
    public Camera targetCam;
    public bool visible = false;

    private Mesh lineMesh;
    private MLPNetwork network = null;
    private byte shouldUpdate = 2; // Looks like Unity needs an extra frame to initialize everything the first time its launched...
    private RawImage renderTarget;

    private static int subdivisions = 10;

    public void Start()
    {
        lineMesh = new Mesh();
        lineMesh.name = "LineMesh";
        TargetMesh.mesh = lineMesh;
        renderTarget = GetComponent<RawImage>();
        renderTarget.enabled = visible;
    }

    public void SetNetwork(MLPNetwork n)
    {
        network = n;
        shouldUpdate = 1; 
    }

    public void UpdateNetwork()
    {
        shouldUpdate = 1;
    }

    private Vector4 GetPosAngle(Vector3 start, Vector3 end, float t)
    {
        t = t - 0.5f;
        float exp = MathF.Exp(-10*t);
        float p = 1 / (1 + exp);
        float d = MathF.Atan(10*exp/((1+exp)*(1+exp)));
        if (t <= -0.499f || t >= 0.499f)
        {
            t = MathF.Sign(t) * 0.5f;
            p = t > 0 ? 1 : 0;
            d = 0;
        }
        t = t + 0.5f;
        float fact = (end.y - start.y) / (end.x - start.x);
        fact = MathF.Sign(fact) * MathF.Pow(MathF.Abs(fact), 0.5f);
        return new Vector4(
            Mathf.Lerp(start.x, end.x, t),
            Mathf.Lerp(start.y, end.y, p),
            Mathf.Lerp(start.z, end.z, t),
            d * fact + MathF.PI / 2);
    }

    private void Update()
    {
        if (shouldUpdate <= 0 || network == null)
        {
            if (targetCam.enabled) targetCam.enabled = false;
            return;
        }
        if (!targetCam.enabled) targetCam.enabled = true;
        shouldUpdate--;
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(transform.childCount - 1).gameObject);
        }
        var layers = network.GetLayers();
        for (int i = 0; i < layers.Count; i++)
        {
            GameObject l = Instantiate(LayerPrefab, transform);
            List<Perceptron> layer = layers[i];
            for (int j = 0; j < layer.Count; j++)
            {
                GameObject p = Instantiate(NeuronPrefab, l.transform);
                float f = Mathf.Clamp(layer[j].bias, -0.2f, 0.2f) * 2;
                p.GetComponent<Image>().color = layer[j].bias > 0 ? Color.Lerp(Color.white, Color.green, f) : Color.Lerp(Color.white, Color.blue, -f);
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(visible);
        }
        List<int> indices = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        float depth = 0;
        for (int i = 1; i < layers.Count; i++)
        {
            List<Perceptron> layer = layers[i];
            for (int j = 0; j < layer.Count; j++)
            {
                Perceptron p = layer[j];
                Vector3 currentPos = (transform.GetChild(i).GetChild(j).position - transform.position);
                currentPos.x *= Scale.x;
                currentPos.y *= Scale.y;
                for (int k = 0; k < p.connections.Count; k++)
                {
                    IncomingConnection c = p.connections[k];
                    if (c.dead) continue;
                    int l;
                    for (l = 0; l < layers[i - 1].Count; l++)
                    {
                        if (layers[i - 1][l] == c.source) break;
                    }
                    Vector3 dest = (transform.GetChild(i - 1).GetChild(l).position - transform.position);
                    dest.x *= Scale.x;
                    dest.y *= Scale.y;
                    Color col = Color.Lerp(Color.blue, Color.green, MathF.Sign(c.weight) * MathF.Pow(MathF.Abs(c.weight), 0.25f) * 0.5f + 0.5f);
                    for (int m = 0; m <= subdivisions; m++)
                    {
                        Vector4 v = GetPosAngle(currentPos, dest, m * 1.0f / subdivisions);
                        float fact = MathF.Max(MathF.Min(MathF.Abs(c.weight) * 10, 10), 0.5f);
                        float cos = fact * MathF.Cos(v.w);
                        float sin = fact * MathF.Sin(v.w);
                        Vector3 f = new Vector3(cos, sin, 0);
                        Vector3 d = new Vector3(0, 0, depth);
                        Vector3 npos = v;
                        vertices.Add(npos + f + d);
                        vertices.Add(npos - f + d);
                        colors.Add(col);
                        colors.Add(col);
                        if (m > 0)
                        {
                            indices.Add(vertices.Count-1);
                            indices.Add(vertices.Count-2);
                            indices.Add(vertices.Count-4);
                            indices.Add(vertices.Count-3);
                        }
                    }
                    depth += 0.001f;
                }
            }
        }
        lineMesh.Clear();
        lineMesh.SetVertices(vertices);
        lineMesh.SetColors(colors);
        lineMesh.SetIndices(indices, MeshTopology.Quads, 0);
    }

    public void ToggleActive()
    {
        visible = !renderTarget.enabled;
        renderTarget.enabled = visible;
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(visible);
        }
    }
}
