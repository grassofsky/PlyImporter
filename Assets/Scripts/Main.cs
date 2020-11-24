using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThreeDeeBear.Models.Ply;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Mesh mesh = PlyHandler.GetMesh("rabit-binary.ply");

        GameObject g = new GameObject();
        mesh.name = g.name = "mesh";
        MeshFilter mf = g.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        MeshRenderer mr = g.AddComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Standard"));
        mr.material = material;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
