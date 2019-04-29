using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coloring : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        // create new colors array where the colors will be created.
        Color[] colors = new Color[vertices.Length];

        colors[0] = Color.black;
        colors[1] = Color.red;
        colors[2] = Color.yellow;
        colors[3] = Color.green;
        colors[4] = Color.blue;
        colors[5] = Color.magenta;
        colors[6] = Color.white;
        colors[7] = Color.cyan;


        // assign the array of colors to the Mesh.
        mesh.colors = colors;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
