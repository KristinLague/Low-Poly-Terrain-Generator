using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGenerator script = (TerrainGenerator) target;
        
        if(DrawDefaultInspector())
        {
            script.Initiate();
        }

        if (GUILayout.Button("New Seed"))
        {
            script.seed = Random.Range(0, 100);
            script.Initiate();
        }

        if (GUILayout.Button("Save Mesh as Asset"))
        {
            script.SaveMesh();
        }
    }
}
