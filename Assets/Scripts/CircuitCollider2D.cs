using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitCollider2D : MonoBehaviour
{
    public List<Mesh> BorderMeshes;
    public Material borderMat;
    public bool ShouldUpdate = false;

    private void Start()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(transform.childCount-1).gameObject);
        }
        if (BorderMeshes == null || BorderMeshes.Count == 0) return;
        for (int i = 0; i < BorderMeshes.Count; i++)
        {
            var obj = new GameObject(BorderMeshes[i].name);
            obj.transform.SetParent(transform, false);
            List<Vector2> points = new List<Vector2>();
            var mesh = BorderMeshes[i];
            List<int> indices = new List<int>(mesh.GetIndices(0));
            List<int> p = new List<int>();
            bool edited = true;
            while (indices.Count > 0 && edited)
            {
                edited = false;
                for (int j = 0; j < indices.Count; j += 2)
                {
                    int p1 = indices[j];
                    int p2 = indices[j + 1];
                    if (p.Count < 2)
                    {
                        p.Add(p1);
                        p.Add(p2);
                        indices.RemoveRange(j, 2);
                        edited = true;
                    }
                    else
                    {
                        if (p[p.Count - 1] == p1)
                        {
                            p.Add(p2);
                            indices.RemoveRange(j, 2);
                            edited = true;
                        }
                        else if (p[p.Count - 1] == p2)
                        {
                            p.Add(p1);
                            indices.RemoveRange(j, 2);
                            edited = true;
                        }
                        else if (p[0] == p2)
                        {
                            p.Insert(0, p1);
                            indices.RemoveRange(j, 2);
                            edited = true;
                        }
                        else if (p[0] == p1)
                        {
                            p.Insert(0, p2);
                            indices.RemoveRange(j, 2);
                            edited = true;
                        }
                    }
                }
            }
            for (int j = 0; j < p.Count; j++)
            {
                Vector3 v = mesh.vertices[p[j]];
                points.Add(new Vector2(-v.x, v.z));
            }
            var edgeCollider = obj.AddComponent<EdgeCollider2D>();
            edgeCollider.SetPoints(points);
            edgeCollider.isTrigger = true;
            var obj2 = new GameObject("Mesh");
            obj2.transform.SetParent(obj.transform, false);
            obj2.transform.localRotation = Quaternion.AngleAxis(-90, Vector3.right);
            obj2.transform.localScale = new Vector3(-1, 0, 1);
            obj2.AddComponent<MeshRenderer>().material = borderMat;
            obj2.AddComponent<MeshFilter>().mesh = mesh;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (ShouldUpdate)
        {
            ShouldUpdate = false;
            Start();
        }
    }
}
