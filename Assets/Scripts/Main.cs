using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ThreeDeeBear.Models.Ply;
using System.IO;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        PlyResult result = PlyHandler.GetResult(File.ReadAllBytes("line.ply"));

        if (result.triangles != null)
        {
            GameObject g = new GameObject();
            g.name = result.meshName == "" ? "Default" : result.meshName;
            Mesh mesh = new Mesh();
            mesh.vertices = result.vertices.ToArray();
             mesh.triangles = result.triangles.ToArray();
            if (result.normals != null) mesh.normals = result.normals.ToArray();
            if (result.colors != null) mesh.SetColors(result.colors.ToArray());
            mesh.name = result.meshName;
            MeshFilter mf = g.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            MeshRenderer mr = g.AddComponent<MeshRenderer>();
            Material material = new Material(Shader.Find("Standard"));
            mr.material = material;
            mr.material.color = result.meshColor;
        }

        if (result.lines != null)
        {
            Camera.main.transform.position = new Vector3(0, 2, 0);
            Camera.main.transform.Rotate(new Vector3(90, 0, 0), Space.World);
            for (var i =0; i<result.lines.Count; ++i)
            {
                GameObject g = new GameObject();
                g.name = result.meshName == "" ? "Default" + i.ToString() : result.meshName;
                LineRenderer linerender = g.AddComponent<LineRenderer>();
                linerender.material = new Material(Shader.Find("Sprites/Default"));
                linerender.positionCount = result.lines[i].Count;
                for (var j = 0; j < result.lines[i].Count; ++j)
                {
                    linerender.SetPosition(j, result.vertices[result.lines[i][j]]);
                }
                linerender.startColor = result.lineColors[i];
                linerender.endColor = result.lineColors[i];
                linerender.startWidth = 0.02f;
                linerender.endWidth = 0.02f;
            }

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
