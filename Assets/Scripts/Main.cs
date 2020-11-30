using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ThreeDeeBear.Models.Ply;
using System.IO;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        PlyResult result = PlyHandler.GetResult(File.ReadAllBytes("rabit.ply"));

        GameObject g = new GameObject();
        g.name = result.MeshResult.name;
        MeshFilter mf = g.AddComponent<MeshFilter>();
        mf.mesh = result.MeshResult;
        MeshRenderer mr = g.AddComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Standard"));
        mr.material = material;
        mr.material.color = result.MeshColor;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
