using System.Collections.Generic;
using UnityEngine;

public class GraphRenderer : MonoBehaviour
{
    public Vector2 graphSize;

    private Mesh lineMesh;
    private bool shouldUpdate = false;
    private List<float> values = new List<float>();

    public void Start()
    {
        lineMesh = new Mesh();
        lineMesh.name = "LineMesh";
        GetComponent<MeshFilter>().mesh = lineMesh;
    }

    public void ClearData()
    {
        values.Clear();
    }

    public void AddData(List<float> data)
    {
        values.AddRange(data);
    }

    public void AddData(float data)
    {
        values.Add(data);
    }

    public void UpdateData()
    {
        shouldUpdate = true;
    }

    private void Update()
    {
        if (!shouldUpdate) return;
        shouldUpdate = false;
        List<int> indices = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        colors.Add(Color.white);
        colors.Add(Color.white);
        colors.Add(Color.white);
        colors.Add(Color.white);
        vertices.Add(new Vector3(-graphSize.x / 2, -graphSize.y / 2, 10));
        vertices.Add(new Vector3(graphSize.x / 2, -graphSize.y / 2, 10));
        vertices.Add(new Vector3(graphSize.x / 2, graphSize.y / 2, 10));
        vertices.Add(new Vector3(-graphSize.x / 2, graphSize.y / 2, 10));
        indices.Add(vertices.Count - 1);
        indices.Add(vertices.Count - 2);
        indices.Add(vertices.Count - 2);
        indices.Add(vertices.Count - 3);
        indices.Add(vertices.Count - 3);
        indices.Add(vertices.Count - 4);
        indices.Add(vertices.Count - 4);
        indices.Add(vertices.Count - 1);
        if (values.Count > 1)
        {
            float max = 0;
            int div = (int)(1 + (values.Count) / graphSize.x);
            List<float> values2 = new List<float>();
            for (int i = 0; i < values.Count; i += div)
            {
                float value = 0;
                for (int j = 0; j < div && (j + i * div) < values.Count; j++)
                {
                    value += values[i * div + j];
                }
                value /= div;
                values2.Add(value);
                if (i == 0)
                {
                    max = value;
                }
                else
                {
                    max = Mathf.Max(max, value);
                }
            }
            for (int i = 0; i < values2.Count; i++)
            {
                float value = values2[i] / max;
                Vector3 pos = new Vector3(((i / (float)(values2.Count - 1)) - 0.5f) * graphSize.x, (value - 0.5f) * graphSize.y);
                Vector3 d = new Vector3(0, 0, 10);
                vertices.Add(pos + d);
                colors.Add(Color.red);
                if (i == 0) continue;
                indices.Add(vertices.Count - 1);
                indices.Add(vertices.Count - 2);
            }
        }
        lineMesh.Clear();
        lineMesh.SetVertices(vertices);
        lineMesh.SetColors(colors);
        lineMesh.SetIndices(indices, MeshTopology.Lines, 0);
    }
}
