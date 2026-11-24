using UnityEngine;

// A small reusable pool for footprints and the hand gesture. The floor remains in the anchor's frame.
public sealed class CompanionPetals : MonoBehaviour
{
    readonly SpriteRenderer[] ink=new SpriteRenderer[16];
    readonly Vector3[] origin=new Vector3[16];
    readonly float[] birth=new float[16];
    readonly bool[] airborne=new bool[16];
    FlowerCompanion figure;
    Camera lens;
    float respondAt=-10;
    int cursor;
    bool autumn;
    public void Initialize(FlowerCompanion value,Camera camera,Sprite sprite,SpringSpirit.Season season)
    {
        figure=value;lens=camera;autumn=season==SpringSpirit.Season.Autumn;
        figure.Gait.Landed+=Stamp;
        for(int i=0;i<ink.Length;i++)
        {
            var go=new GameObject("季节印记");
            go.transform.SetParent(transform,false);
            ink[i]=go.AddComponent<SpriteRenderer>();ink[i].sprite=sprite;
            birth[i]=-10;go.SetActive(false);
        }
    }
    public void Respond(){if(Time.time-respondAt>1.45f)respondAt=Time.time;}
    void Emit(Vector3 at,bool air)
    {
        int i=cursor++%ink.Length;var r=ink[i];
        airborne[i]=air;birth[i]=Time.time;
        origin[i]=transform.InverseTransformPoint(at);
        r.gameObject.SetActive(true);
        r.transform.localScale=Vector3.one*(air?.016f:.030f)/Mathf.Max(.001f,r.sprite.bounds.size.x);
        if(!air)r.transform.rotation=Quaternion.AngleAxis(23*i,figure.transform.up)*Quaternion.LookRotation(figure.transform.up,figure.transform.forward);
    }
    void Stamp(Vector3 sole)=>Emit(sole+figure.transform.up*.001f,false);
    void OnDestroy(){if(figure && figure.Gait!=null)figure.Gait.Landed-=Stamp;}
    void LateUpdate()
    {
        if(!figure || !figure.gameObject.activeInHierarchy){foreach(var r in ink)if(r)r.gameObject.SetActive(false);return;}
        float delay=Time.time-respondAt;
        if(delay>.28f && delay<1f)
        {
            for(int j=0;j<6;j++)Emit(figure.Palm,true);
            respondAt=-10;
        }
        for(int i=0;i<ink.Length;i++)
        {
            var r=ink[i];if(!r.gameObject.activeSelf)continue;
            float t=(Time.time-birth[i])/(airborne[i]?.95f:1.65f);
            if(t>=1){r.gameObject.SetActive(false);continue;}
            Vector3 pos=origin[i];
            if(airborne[i])
            {
                pos+=new Vector3(Mathf.Cos(i*2.4f)*t*.080f,Mathf.Sin(t*Mathf.PI)*.055f,Mathf.Sin(i*2.4f)*t*.080f);
                if(autumn)
                {
                    float spin=i*2.4f+t*5.2f;
                    pos=origin[i]+new Vector3(Mathf.Cos(spin)*t*.09f, .043f*Mathf.Sin(t*Mathf.PI)-t*.062f, Mathf.Sin(spin)*t*.09f);
                    r.transform.rotation=lens.transform.rotation*Quaternion.Euler(t*95,0,t*240+i*23);
                }
                else r.transform.rotation=lens.transform.rotation*Quaternion.Euler(0,0,t*95+i*23);
            }
            r.transform.localPosition=pos;r.color=new Color(1,1,1,(airborne[i]?1:.55f)*(1-t*t));
        }
    }
}
