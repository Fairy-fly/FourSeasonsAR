using UnityEngine;
using Kind=CompanionProfile.Kind;

// The character bobs above a separate translucent ground sprite.
[DefaultExecutionOrder(70)]
public sealed class HoverAura : MonoBehaviour
{
    FlowerCompanion companion;
    SpriteRenderer aura;
    Sprite sprite;
    Texture2D texture;
    public Transform Floor { get; set; }
    public SpriteRenderer Mark => aura;

    public void Initialize(FlowerCompanion body,Transform floor)
    {
        companion=body;
        Floor=floor;
        var mark=new GameObject("贴地光晕");
        mark.transform.SetParent(transform,false);
        aura=mark.AddComponent<SpriteRenderer>();
        aura.sortingOrder=-2;
        sprite=MakeSprite(body.Profile.Style);
        aura.sprite=sprite;
        Sample(0);
    }

    void LateUpdate()=>Sample(Time.time);

    public void Sample(float seconds)
    {
        if(!Floor || !aura)return;
        Vector3 normal=Floor.up;
        Vector3 fromPlane=transform.position-Floor.position;
        Vector3 onPlane=transform.position-normal*Vector3.Dot(fromPlane,normal);
        aura.transform.SetPositionAndRotation(onPlane+normal*.0016f,
            Floor.rotation*Quaternion.Euler(90,0,seconds*(companion.Profile.Style==Kind.Time?8:14)));
        float swell=.5f+.5f*Mathf.Sin(seconds*2.1f+.4f);
        float size=companion.Height*(.396f+.041f*swell);
        aura.transform.localScale=Vector3.one*size;
        aura.color=new Color(1,1,1,.81f+.17f*swell);
    }

    Sprite MakeSprite(Kind style)
    {
        const int size=96;
        var pixels=new Color32[size*size];
        Color tone=style switch
        {
            Kind.Spring=>new Color(1,.63f,.78f),
            Kind.Summer=>new Color(.64f,1,.78f),
            Kind.Autumn=>new Color(1,.72f,.39f),
            Kind.Winter=>new Color(.65f,.87f,1),
            Kind.Time=>new Color(1,.80f,.47f),
            _=>new Color(.80f,.69f,1)
        };
        for(int y=0;y<size;y++)
        for(int x=0;x<size;x++)
        {
            float u=(x+.5f)/(size*.5f)-1;
            float v=(y+.5f)/(size*.5f)-1;
            float radius=Mathf.Sqrt(u*u+v*v);
            float angle=Mathf.Atan2(v,u);
            float shade=.33f*Mathf.Pow(Mathf.Clamp01(1-radius/.79f),1.35f);
            float mark=Motif(style,radius,angle);
            float bloom=.20f*Band(radius,.58f,.27f);
            float glow=Mathf.Clamp01(mark+bloom);
            float alpha=glow+shade*(1-glow);
            Color pigment=(tone*glow+new Color(.16f,.11f,.16f)*shade*(1-glow))
                / Mathf.Max(.0001f,alpha);
            pigment.a=alpha;
            pixels[y*size+x]=pigment;
        }
        texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Season hover aura"};
        texture.wrapMode=TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false,true);
        return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
    }

    static float Motif(Kind style,float r,float angle)
    {
        float ripple=.58f*Band(r,.67f,.047f);
        switch(style)
        {
            case Kind.Spring:
                return .62f*Band(r,.72f+.10f*Mathf.Cos(5*angle),.067f)+.20f*Band(r,.48f,.055f);
            case Kind.Summer:
                return ripple+.42f*Band(r,.43f+.028f*Mathf.Sin(3*angle),.050f);
            case Kind.Autumn:
                return ripple*(.42f+.58f*Mathf.Pow(Mathf.Abs(Mathf.Cos(3*angle)),2))+
                    .29f*Band(r,.84f+.055f*Mathf.Sin(4*angle),.056f);
            case Kind.Winter:
                return .56f*Band(r,.72f-.085f*Mathf.Abs(Mathf.Sin(3*angle)),.051f)+
                    .28f*Band(r,.55f,.23f)*Mathf.Pow(Mathf.Abs(Mathf.Cos(3*angle)),14);
            case Kind.Time:
                return .40f*Band(r,.54f,.045f)+.55f*Band(r,.78f,.043f)+
                    .42f*Band(r,.66f,.091f)*Mathf.Pow(Mathf.Abs(Mathf.Cos(6*angle)),18);
            default:
                return .47f*Band(r,.67f+.13f*Mathf.Sin(2*angle),.056f)+
                    .52f*Band(r,.77f,.08f)*Mathf.Pow(Mathf.Abs(Mathf.Cos(2*angle)),21);
        }
    }

    static float Band(float radius,float center,float width)
        => 1-Mathf.SmoothStep(0,1,Mathf.Abs(radius-center)/width);

    void OnDestroy()
    {
        if(Application.isPlaying){Destroy(sprite);Destroy(texture);}
        else{DestroyImmediate(sprite);DestroyImmediate(texture);}
    }
}
