using UnityEngine;
using UnityEngine.UI;

// A press sends three short-lived glints along each side of the enamel rim.
[ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
public sealed class EnamelFlare : MaskableGraphic
{
    public EnamelFlare() { useLegacyMeshGeneration = false; }
    float age = 1f;
    const float Duration = .38f;
    public void Play()
    {
        raycastTarget = false;
        age = 0;
        enabled = true;
        SetVerticesDirty();
    }

    void Update()
    {
        if (Application.isPlaying) Sample(age + Time.unscaledDeltaTime);
    }

    public void Sample(float seconds)
    {
        age = seconds;
        if (age >= Duration) enabled = false;
        else SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (age >= Duration) return;
        Rect r = rectTransform.rect;
        float t = age / Duration;
        for (int side=-1;side<=1;side+=2)
        for (int i=0;i<3;i++)
        {
            float u=Mathf.Clamp01(t*1.45f-i*.15f);
            float glow=Mathf.Sin(u*Mathf.PI);
            Vector2 center=r.center+new Vector2(side*r.width*(.12f+.33f*u),r.height*(.33f+.08f*Mathf.Sin(u*Mathf.PI)));
            float radius=Mathf.Min(7,r.height*.16f)*glow;
            Color tint=new Color(1,.92f,.67f,glow);
            int n=mesh.currentVertCount;
            mesh.AddVert(center,tint,Vector2.zero);
            for(int j=0;j<8;j++)
            {
                float angle=j*Mathf.PI/4;
                float length=(j%2==0 ? 1f : .19f)*radius;
                mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*length,tint,Vector2.zero);
            }
            for(int j=0;j<8;j++) mesh.AddTriangle(n,n+1+j,n+1+(j+1)%8);
        }
    }
}
