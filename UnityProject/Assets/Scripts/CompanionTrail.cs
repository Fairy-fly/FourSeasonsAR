using UnityEngine;
using Kind=CompanionProfile.Kind;

// The trail sheds sprites in the anchor's coordinate system, so particles stay behind in space.
public sealed class CompanionTrail
{
    const float Lifetime=.82f;
    readonly Transform anchor;
    readonly Camera camera;
    readonly Color tint;
    readonly Particle[] particles=new Particle[28];
    readonly float spriteWidth;
    Vector3 lastRoot,lastSource;
    float remainder;
    bool sampling;
    int cursor;

    struct Particle
    {
        public SpriteRenderer renderer;
        public Vector3 start,drift;
        public float scale,birth,phase;
    }

    public CompanionTrail(Transform parent,Camera lens,Sprite sprite,Kind theme)
    {
        anchor=parent;
        camera=lens;
        spriteWidth=Mathf.Max(.001f,sprite.bounds.size.x);
        tint=theme switch
        {
            Kind.Spring=>new Color(1,.64f,.78f),
            Kind.Summer=>new Color(.78f,1,.63f),
            Kind.Autumn=>new Color(1,.76f,.41f),
            Kind.Winter=>new Color(.72f,.91f,1),
            Kind.Time=>new Color(1,.83f,.49f),
            _=>new Color(.84f,.75f,1)
        };
        for(int i=0;i<particles.Length;i++)
        {
            var go=new GameObject("同行拖尾粒子");
            go.transform.SetParent(anchor,false);
            var renderer=go.AddComponent<SpriteRenderer>();
            renderer.sprite=sprite;
            renderer.sortingOrder=-1;
            go.SetActive(false);
            particles[i].renderer=renderer;
        }
    }

    public void Sample(FlowerCompanion figure,float time)
    {
        if(figure.Glide>.05f || figure.Held) RecordMovement(figure,time);
        else { sampling=false; remainder=0; }
        for(int i=0;i<particles.Length;i++)
        {
            ref var particle=ref particles[i];
            var renderer=particle.renderer;
            if(!renderer.gameObject.activeSelf)continue;
            float age=(time-particle.birth)/Lifetime;
            if(age>=1){ renderer.gameObject.SetActive(false); continue; }
            renderer.transform.localPosition=particle.start+age*particle.drift;
            renderer.transform.rotation=camera.transform.rotation*Quaternion.Euler(0,0,particle.phase*29+age*110);
            float diameter=particle.scale*Mathf.Lerp(.0135f,.005f,age);
            renderer.transform.localScale=Vector3.one*(diameter/spriteWidth);
            float fade=(1-age)*(1-age);
            float glimmer=.81f+.19f*Mathf.Sin(age*12+particle.phase);
            renderer.color=new Color(tint.r,tint.g,tint.b,.88f*fade*glimmer);
        }
    }

    void RecordMovement(FlowerCompanion figure,float time)
    {
        var body=figure.transform;
        float scale=body.lossyScale.y;
        Vector3 up=anchor.InverseTransformDirection(body.up);
        Vector3 root=anchor.InverseTransformPoint(body.position);
        Vector3 source=anchor.InverseTransformPoint(figure.PoseRoot.position+body.up*figure.Height*.26f*scale);
        Vector3 travel=Vector3.ProjectOnPlane(root-lastRoot,up);
        float distance=travel.magnitude;
        if(!sampling || distance>.25f*scale)
        {
            sampling=true;
            remainder=0;
        }
        else if(distance>.0001f)
        {
            float spacing=.013f*scale;
            Vector3 across=Vector3.Cross(up,travel/distance);
            float cursorDistance=spacing-remainder;
            for(int count=0;count<4 && cursorDistance<=distance;count++,cursorDistance+=spacing)
                Emit(Vector3.Lerp(lastSource,source,cursorDistance/distance),up,across,scale,time);
            remainder=(remainder+distance)%spacing;
        }
        lastRoot=root;
        lastSource=source;
    }

    void Emit(Vector3 source,Vector3 up,Vector3 across,float scale,float time)
    {
        int index=cursor++%particles.Length;
        ref var particle=ref particles[index];
        float phase=index*2.39996f;
        float side=Mathf.Sin(phase);
        particle.birth=time;
        particle.scale=(.79f+.20f*Mathf.Cos(phase))*scale;
        particle.phase=phase;
        particle.start=source+scale*(.014f*side*across+.011f*Mathf.Cos(phase)*up);
        particle.drift=scale*(.038f*side*across+(index%3!=0?.026f:-.014f)*up);
        particle.renderer.gameObject.SetActive(true);
    }

    public void Clear()
    {
        foreach(var particle in particles)particle.renderer.gameObject.SetActive(false);
        sampling=false;
        remainder=0;
    }
}
