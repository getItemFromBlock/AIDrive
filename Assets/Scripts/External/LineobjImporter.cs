using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// based of https://github.com/taylorgoolsby/lineobj-importer (by taylorgoolsby)
// but adapted to work on recent Unity AND to suport object with multiple meshes

[UnityEditor.AssetImporters.ScriptedImporter(9, "lineobj")]
public class LineobjImporter : UnityEditor.AssetImporters.ScriptedImporter
{
    public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
    {
        int lastSlash = ctx.assetPath.LastIndexOf('/');
        int lastDot = ctx.assetPath.LastIndexOf('.');
        string assetName = ctx.assetPath.Substring(lastSlash + 1, lastDot - lastSlash - 1);

        GameObject mainAsset = new GameObject();

        Dictionary<string, List<string[]>> obj = ParseObj(ctx.assetPath);
        bool triangleSubmeshExists;
        List<Mesh> meshes = ConstructMesh(obj, out triangleSubmeshExists);
        if (meshes == null || meshes.Count == 0) return;
        for (int i = 0; i < meshes.Count; i++)
        {
            if (meshes[i].name.Length <= 0)
            {
                meshes[i].name = assetName + i.ToString();
            }
            ctx.AddObjectToAsset(meshes[i].name, meshes[i]);
            if (i == 0)
            {
                MeshFilter meshFilter = mainAsset.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = meshes[0];
            }
        }
        /*
        Material[] materials = new Material[mesh.subMeshCount];
        Shader standard = Shader.Find("Standard");
        if (triangleSubmeshExists && mesh.subMeshCount == 2)
        {
            materials[0] = new Material(standard);
            materials[0].name = "Face Material";
            materials[1] = new Material(standard);
            materials[1].name = "Edge Material";
            ctx.AddObjectToAsset("Face Material", materials[0]);
            ctx.AddObjectToAsset("Edge Material", materials[1]);
        }
        else if (triangleSubmeshExists && mesh.subMeshCount == 1)
        {
            materials[0] = new Material(standard);
            materials[0].name = "Face Material";
            ctx.AddObjectToAsset("Face Material", materials[0]);
        }
        else if (!triangleSubmeshExists && mesh.subMeshCount == 1)
        {
            materials[0] = new Material(standard);
            materials[0].name = "Edge Material";
            ctx.AddObjectToAsset("Edge Material", materials[0]);
        }
        MeshRenderer renderer = mainAsset.AddComponent<MeshRenderer>();
        renderer.materials = materials;
        */
        ctx.AddObjectToAsset(assetName, mainAsset);
        ctx.SetMainObject(mainAsset);
    }

    private List<Mesh> ConstructMesh(Dictionary<string, List<string[]>> data, out bool triangleSubmeshExists)
    {
        List<string[]> f = data["f"];
        List<string[]> e = data["e"];
        triangleSubmeshExists = false;
        if (e.Count == 0 && f.Count == 0)
        {
            return null;
        }
        Mesh current = new Mesh();
        current.subMeshCount = 1;
        List<Mesh> result = new List<Mesh>();

        List<string[]> v = data["v"];
        Vector3[] vertices = new Vector3[v.Count];
        for (int i = 0; i < v.Count; i++)
        {
            string[] raw = v[i];

            //Debug.Log(raw[0]);
            float x = float.Parse(raw[0], CultureInfo.InvariantCulture);
            float y = float.Parse(raw[1], CultureInfo.InvariantCulture);
            float z = float.Parse(raw[2], CultureInfo.InvariantCulture);
            vertices[i] = new Vector3(x, y, z);
        }
        current.vertices = vertices;

        bool hasVertices = false;
        if (f.Count > 0)
        {
            List<int> triangleIndices = new List<int>();
            for (int i = 0; i < f.Count; i++)
            {
                string[] raw = f[i];
                if (raw[0][0] == 'N')
                {
                    if (hasVertices)
                    {
                        current.SetIndices(triangleIndices, MeshTopology.Triangles, 0, false);
                        current.RecalculateNormals();
                        current.RecalculateBounds();
                        hasVertices = false;
                        triangleIndices = new List<int>();
                        result.Add(current);
                        current = new Mesh();
                        current.subMeshCount = 1;
                        current.vertices = vertices;
                    }
                    current.name = raw[1];
                    continue;
                }
                hasVertices = true;
                string s1 = raw[0];
                string s2 = raw[1];
                string s3 = raw[2];
                if (s1.Contains("//"))
                {
                    s1 = s1.Remove(s1.IndexOf("//"));
                }
                if (s2.Contains("//"))
                {
                    s2 = s2.Remove(s2.IndexOf("//"));
                }
                if (s3.Contains("//"))
                {
                    s3 = s3.Remove(s3.IndexOf("//"));
                }
                int v1 = int.Parse(s1, CultureInfo.InvariantCulture) - 1;
                int v2 = int.Parse(s2, CultureInfo.InvariantCulture) - 1;
                int v3 = int.Parse(s3, CultureInfo.InvariantCulture) - 1;
                triangleIndices.Add(v1);
                triangleIndices.Add(v2);
                triangleIndices.Add(v3);
            }
            current.SetIndices(triangleIndices, MeshTopology.Triangles, 0, false);
            current.RecalculateNormals();
            current.RecalculateBounds();
            result.Add(current);
            current = new Mesh();
            current.subMeshCount = 1;
            current.vertices = vertices;
        }
        hasVertices = false;
        if (e.Count > 0)
        {
            List<int> edgeIndices = new List<int>();
            for (int i = 0; i < e.Count; i++)
            {
                string[] raw = e[i];
                if (raw[0][0] == 'N')
                {
                    if (hasVertices)
                    {
                        current.SetIndices(edgeIndices, MeshTopology.Lines, 0, false);
                        current.RecalculateBounds();
                        hasVertices = false;
                        edgeIndices = new List<int>();
                        result.Add(current);
                        current = new Mesh();
                        current.subMeshCount = 1;
                        current.vertices = vertices;
                    }
                    current.name = raw[1];
                    continue;
                }
                hasVertices = true;
                int v1 = int.Parse(raw[0], CultureInfo.InvariantCulture) - 1;
                int v2 = int.Parse(raw[1], CultureInfo.InvariantCulture) - 1;
                edgeIndices.Add(v1);
                edgeIndices.Add(v2);
            }
            current.SetIndices(edgeIndices, MeshTopology.Lines, 0, false);
            current.RecalculateBounds();
            result.Add(current);
            current = new Mesh();
            current.subMeshCount = 1;
            current.vertices = vertices;
        }
        return result;
    }

    /*
    Converts obj text file into json-like structure:
        {v: [], vn: [], f: [], e: []}
     */
    private Dictionary<string, List<string[]>> ParseObj(string filepath)
    {
        Dictionary<string, List<string[]>> result = new Dictionary<string, List<string[]>>();
        List<string[]> v = new List<string[]>();
        List<string[]> vn = new List<string[]>();
        List<string[]> f = new List<string[]>();
        List<string[]> e = new List<string[]>();

        using (StreamReader sr = File.OpenText(filepath))
        {
            string s = string.Empty;
            string[] line;
            while ((s = sr.ReadLine()) != null)
            {
                if (s.StartsWith("v "))
                {
                    line = s.Split(' ');
                    string[] lineData = { line[1], line[2], line[3] };
                    v.Add(lineData);
                }
                else if (s.StartsWith("vn "))
                {
                    line = s.Split(' ');
                    string[] lineData = { line[1], line[2], line[3] };
                    vn.Add(lineData);
                }
                else if (s.StartsWith("f "))
                {
                    line = s.Split(' ');
                    if (line.Length > 4)
                    {
                        Debug.LogError("Your model must be exported with triangulated faces.");
                        continue;
                    }
                    string[] lineData = { line[1], line[2], line[3] };
                    f.Add(lineData);
                }
                else if (s.StartsWith("l "))
                {
                    line = s.Split(' ');
                    string[] lineData = { line[1], line[2] };
                    e.Add(lineData);
                }
                else if (s.StartsWith("o "))
                {
                    line = s.Split(' ');
                    string[] lineData = { "N", line[1] };
                    e.Add(lineData);
                }
            }
        }

        result.Add("v", v);
        result.Add("vn", vn);
        result.Add("f", f);
        result.Add("e", e);
        return result;
    }

    // for debugging
    private void LogObj(Dictionary<string, List<string[]>> obj)
    {
        string result = "";
        result += "{\n";

        result += LogChild(obj, "v");
        result += LogChild(obj, "vn");
        result += LogChild(obj, "f");
        result += LogChild(obj, "e");

        result += "}";
        Debug.Log(result);
    }

    private string LogChild(Dictionary<string, List<string[]>> obj, string key)
    {
        string result = "";
        string ind = "  ";
        result += ind + key + ": [\n";
        foreach (string[] sarr in obj[key])
        {
            result += ind + ind + "[";
            foreach (string s in sarr)
            {
                result += s + ", ";
            }
            result += "]\n";
        }
        result += ind + "]\n";
        return result;
    }
}
