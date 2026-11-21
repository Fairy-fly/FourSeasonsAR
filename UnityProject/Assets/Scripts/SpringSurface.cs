using UnityEngine;
using UnityEngine.UI;

// The outline also supplies UVs so the paper corners and the petal can sit over live video.
[ExecuteAlways]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SpringSurface : MaskableGraphic
{
    protected SpringSurface() { useLegacyMeshGeneration = false; }

    public Texture texture;
    public Vector2[] outline;
    public override Texture mainTexture => texture ? texture : Texture2D.whiteTexture;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        mesh.AddVert(rect.center, color, Vector2.one * .5f);
        if (outline != null)
        {
            foreach (Vector2 uv in outline)
                mesh.AddVert(rect.min + Vector2.Scale(rect.size, uv), color, uv);
        }
        else
        {
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            for (int corner = 0; corner < 4; corner++)
            {
                var center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
                    corner < 2 ? rect.yMax - radius : rect.yMin + radius);
                for (int step = 0; step <= 10; step++)
                {
                    float angle = (corner * 90 + step * 9) * Mathf.Deg2Rad;
                    Vector2 p = center + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    mesh.AddVert(p, color, Rect.PointToNormalized(rect, p));
                }
            }
        }
        int count = mesh.currentVertCount - 1;
        for (int i = 1; i <= count; i++) mesh.AddTriangle(0, i, i % count + 1);
    }
}
