using UnityEngine;
using Kind=CompanionProfile.Kind;

// Footprints and flying particles stay in the placement anchor's frame.
[DefaultExecutionOrder(50)]
public sealed class CompanionPetals : MonoBehaviour
{
    readonly SpriteRenderer[] ink=new SpriteRenderer[18];
    readonly Vector3[] origin=new Vector3[18];
    readonly float[] birth=new float[18];
    readonly bool[] airborne=new bool[18];
    FlowerCompanion figure;
    Camera lens;
    Sprite floorInk,airInk,generated;
    Texture2D generatedTexture;
    SpriteRenderer lantern;
    float respondAt=-10;
    int cursor;

    public void Initialize(FlowerCompanion value, Camera camera, Sprite seasonSprite)
    {
        figure=value;
        lens=camera;
        Kind style=value.Profile.Style;
        if(style is Kind.Summer or Kind.Time or Kind.Star)
            generated=CreateLight(style);
        airInk=generated ? generated : seasonSprite;
        floorInk=seasonSprite ? seasonSprite : generated;
        figure.Gait.Landed+=Stamp;
        for(int i=0;i<ink.Length;i++)
        {
            var go=new GameObject("季节印记");
            go.transform.SetParent(transform,false);
            ink[i]=go.AddComponent<SpriteRenderer>();
            birth[i]=-10;
            go.SetActive(false);
        }
        if(style==Kind.Summer)
        {
            var go=new GameObject("灯芯");
            go.transform.SetParent(transform,false);
            lantern=go.AddComponent<SpriteRenderer>();
            lantern.sprite=airInk;
        }
    }

    Sprite CreateLight(Kind style)
    {
        const int size=32;
        var data=new Color32[size*size];
        for(int y=0;y<size;y++)
        for(int x=0;x<size;x++)
        {
            float dx=Mathf.Abs((x+.5f)/(size*.5f)-1);
            float dy=Mathf.Abs((y+.5f)/(size*.5f)-1);
            float radius=Mathf.Sqrt(dx*dx+dy*dy);
            float opacity=style==Kind.Star
                ? Mathf.Max(1-radius*2.1f,Mathf.Max(1-6*dx-dy,1-dx-6*dy))
                : style==Kind.Time ? 1-dx-dy : Mathf.Pow(Mathf.Clamp01(1-radius),2.1f);
            data[y*size+x]=new Color32(255,255,255,(byte)(255*Mathf.Clamp01(opacity)));
        }
        generatedTexture=new Texture2D(size,size,TextureFormat.RGBA32,false);
        generatedTexture.SetPixels32(data);
        generatedTexture.Apply(false,true);
        return Sprite.Create(generatedTexture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
    }

    public void Respond(){if(Time.time-respondAt>1.45f)respondAt=Time.time;}

    void Emit(Vector3 point,bool air)
    {
        var sprite=air ? airInk : floorInk;
        if(!sprite)return;
        int i=cursor++%ink.Length;
        var r=ink[i];
        r.sprite=sprite;
        airborne[i]=air;
        birth[i]=Time.time;
        origin[i]=transform.InverseTransformPoint(point);
        r.gameObject.SetActive(true);
        if(!air)r.transform.rotation=Quaternion.AngleAxis(23*i,figure.transform.up)*Quaternion.LookRotation(figure.transform.up,figure.transform.forward);
    }

    void Stamp(Vector3 sole)=>Emit(sole+figure.transform.up*.001f,false);

    void LateUpdate()
    {
        if(!figure || !figure.gameObject.activeInHierarchy)
        {
            foreach(var r in ink)if(r)r.gameObject.SetActive(false);
            if(lantern)lantern.enabled=false;
            respondAt=-10;
            return;
        }
        Kind style=figure.Profile.Style;
        float physicalScale=figure.transform.lossyScale.y;
        float delay=Time.time-respondAt;
        if(lantern)
        {
            lantern.enabled=true;
            lantern.transform.SetPositionAndRotation(figure.EffectPoint,lens.transform.rotation);
            lantern.transform.localScale=Vector3.one*physicalScale*(delay<1.4f?.111f:.083f);
            lantern.color=new Color(1,.92f,.44f,.42f+.14f*Mathf.Sin(Time.time*5));
        }
        if(delay>=figure.Profile.Release && delay<1.4f)
        {
            Vector3 source=figure.EffectPoint;
            if(style==Kind.Autumn)source+=figure.transform.right*(figure.Height*.04f*physicalScale);
            int count=style is Kind.Star or Kind.Time ? 11 : 8;
            for(int j=0;j<count;j++)Emit(source,true);
            respondAt=-10;
        }

        Color motif=style switch
        {
            Kind.Time=>new Color(1,.77f,.34f),
            Kind.Star=>new Color(.74f,.66f,1),
            Kind.Summer=>new Color(.90f,1,.57f),
            Kind.Winter=>new Color(.72f,.91f,1),
            _=>Color.white
        };
        for(int i=0;i<ink.Length;i++)
        {
            var r=ink[i];
            if(!r.gameObject.activeSelf)continue;
            bool air=airborne[i];
            float t=(Time.time-birth[i])/(air?1.08f:1.65f);
            if(t>=1){r.gameObject.SetActive(false);continue;}
            Vector3 offset=air?AirPath(style,t,i):Vector3.zero;
            float diameter=air?.016f:.030f;
            if(style==Kind.Summer && !air)diameter*=.4f+t*2.5f;
            r.transform.localPosition=origin[i]+physicalScale*offset;
            r.transform.localScale=Vector3.one*(physicalScale*diameter/Mathf.Max(.001f,r.sprite.bounds.size.x));
            if(air)r.transform.rotation=lens.transform.rotation*Quaternion.Euler(0,0,t*150+i*23);
            float opacity=air ? 1-t*t : .55f*(1-t);
            if(style==Kind.Star && air) opacity*=Mathf.Clamp01(t*7-(i%5)*.44f);
            r.color=new Color(motif.r,motif.g,motif.b,opacity);
        }
    }

    static Vector3 AirPath(Kind style,float t,int i)
    {
        float angle=i*2.39996f;
        float radius=.080f*t;
        float altitude=.055f*Mathf.Sin(t*Mathf.PI);
        switch(style)
        {
            case Kind.Autumn:
                angle+=5.2f*t;
                altitude-=.062f*t;
                break;
            case Kind.Summer:
                angle+=2.4f*t;
                radius*=.66f;
                altitude=.11f*t;
                break;
            case Kind.Winter:
                // Bring a small cloud into the cupped hands, then let it fall.
                radius=.054f*Mathf.Abs(1-2*t);
                altitude=.028f*Mathf.Sin(t*2*Mathf.PI);
                break;
            case Kind.Time:
                angle+=3.4f*Mathf.Max(0,t-.31f);
                radius=.031f+.021f*t;
                altitude=.013f*Mathf.Sin(angle)-.12f*Mathf.Max(0,t-.31f);
                break;
            case Kind.Star:
                angle=1.55f+(i%9)*.24f;
                radius=.020f+(i%9)*.010f;
                altitude=radius*Mathf.Sin(angle);
                return new Vector3(radius*Mathf.Cos(angle),altitude,t*.035f);
        }
        return new Vector3(Mathf.Cos(angle)*radius,altitude,Mathf.Sin(angle)*radius);
    }

    void OnDestroy()
    {
        if(figure && figure.Gait!=null)figure.Gait.Landed-=Stamp;
        if(generated)Destroy(generated);
        if(generatedTexture)Destroy(generatedTexture);
    }
}
