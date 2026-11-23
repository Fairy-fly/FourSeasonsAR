using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SpringModelImport
{
    [MenuItem("Four Seasons/Rebuild spring display mesh")]
    public static void Rebuild()
    {
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var colors = new List<Color>();
        var indices = new List<int>();
        float Number(string text) => float.Parse(text, CultureInfo.InvariantCulture);
        var source=Path.Combine(Application.dataPath,"../../derived/characters/SpringStudy.obj");
        foreach (string line in File.ReadLines(source))
        {
            var words = line.Split(' ');
            switch (words[0])
            {
                case "v":
                    vertices.Add(new Vector3(Number(words[1]), Number(words[2]), -Number(words[3])));
                    colors.Add(new Color(Number(words[4]),Number(words[5]),Number(words[6]),1));
                    break;
                case "vt":
                    uv.Add(new Vector2(Number(words[1]), Number(words[2])));
                    break;
                case "f":
                    // This authored OBJ uses matching position and UV indices.
                    foreach (int i in new[]{1,3,2})
                        indices.Add(int.Parse(words[i].Split('/')[0])-1);
                    break;
            }
        }
        const string path = "Assets/Resources/Models/SpringStudy.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool fresh = !mesh;
        if (fresh) mesh = new Mesh { name = "SpringStudy" };
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0,uv);
        mesh.SetColors(colors);
        mesh.SetTriangles(indices,0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (fresh) AssetDatabase.CreateAsset(mesh,path);
        else EditorUtility.SetDirty(mesh);
        var texture = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/Models/SpringTurnaround.png");
        texture.npotScale = TextureImporterNPOTScale.None;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.textureCompression = TextureImporterCompression.Uncompressed;
        texture.SaveAndReimport();
        AssetDatabase.SaveAssets();
        Debug.Log($"Spring model imported: {vertices.Count} vertices, {indices.Count/3} triangles, bounds {mesh.bounds.size}");
    }
}
