using System.Collections.Generic;
using UnityEngine;

// Shared by the card encounter and the real-world placement rig. Movement is expressed in metres.
public sealed class FlowerCompanion : MonoBehaviour
{
    readonly Dictionary<string,Transform> joints = new();
    readonly Dictionary<string,Quaternion> rest = new();
    Transform visual;
    Material material;
    float age, response = -10;
    Vector3 visualRest;
    float compression;
    public CompanionProfile Profile { get; private set; }
    public static bool IsAvailable(string id) => CompanionProfile.Find(id)!=null;
    public PlantedWalk Gait { get; private set; }
    public void EnableGroundGait()
    {
        Gait=new PlantedWalk(transform,joints,Height,Profile);
    }
    public float Walking { get; set; }
    public float Lift { get; set; }
    public float Height { get; private set; }

    public void Initialize(string id,float height)
    {
        Height = height;
        Profile=CompanionProfile.Find(id) ?? throw new System.ArgumentException("Unknown companion "+id);
        string path = "Gallery/" + id + "/";
        var prefab = Resources.Load<GameObject>(path+"Companion");
        if (!prefab) throw new System.InvalidOperationException("Missing companion " + id);
        visual = Instantiate(prefab).transform;
        visual.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        var skin = visual.GetComponentInChildren<SkinnedMeshRenderer>();
        if (!skin) throw new System.InvalidOperationException("Missing skinned renderer " + id);
        material = new Material(Resources.Load<Shader>("Gallery/FigureStudio"));
        material.mainTexture = Resources.Load<Texture2D>(path+"Color");
        material.SetTexture("_Surface",Resources.Load<Texture2D>(path+"Surface"));
        material.SetTexture("_NormalMap",Resources.Load<Texture2D>(path+"Normal"));
        skin.sharedMaterial = material;
        skin.updateWhenOffscreen = true;
        foreach (Transform joint in skin.bones)
        {
            joints[joint.name]=joint;
            rest[joint.name]=joint.localRotation;
        }
        // Measure before parenting: a tracked card can be vertical or tilted.
        var bounds=skin.bounds;
        if(bounds.size.y < .0001f)throw new System.InvalidOperationException("Invalid skin bounds "+bounds);
        // The imported skinned culling bounds contain animation padding. Use the authored sole
        // and crown for placement; the culling box would leave the shoes over the floor.
        float scale=height/(Profile.Crown-Profile.Sole);
        Vector3 offset=visual.position-new Vector3(bounds.center.x,Profile.Sole,bounds.center.z);
        visual.SetParent(transform,false);
        visual.localScale*=scale;
        visual.localPosition=scale*offset;
        visualRest=visual.localPosition;
        Debug.Log("Encounter skin fit "+id+" bind="+bounds.ToString("F3")+" final="+skin.bounds.ToString("F3"));
        var hit=gameObject.AddComponent<CapsuleCollider>();
        hit.height=height;
        hit.radius=.18f*height;
        hit.center=.5f*height*Vector3.up;
        hit.direction=1;
        age=Time.time;
    }

    public Vector3 Palm => joints.TryGetValue("PalmL",out var joint)?joint.position:transform.position+transform.up*(Height*.6f);

    public Vector3 EffectPoint => Profile.Style switch
    {
        CompanionProfile.Kind.Summer => joints["PalmR"].position-transform.up*(Height*.16f),
        CompanionProfile.Kind.Winter or CompanionProfile.Kind.Time =>
            (Palm+joints["PalmR"].position)*.5f,
        _ => Palm
    };

    public void Respond()
    {
        if(Time.time-response>1.45f) response=Time.time;
    }

    void Pose(string name,Vector3 localAxis,float degrees)
    {
        if(!joints.TryGetValue(name,out var joint)) return;
        Vector3 axis=joint.parent.InverseTransformDirection(transform.TransformDirection(localAxis));
        joint.localRotation=Quaternion.AngleAxis(degrees,axis)*rest[name];
    }

    void LateUpdate()
    {
        if(!visual) return;
        Sample(Time.time-age,Time.time-response,Time.deltaTime);
    }

    public void Sample(float t,float reactionAge,float dt)
    {
        float reach=Mathf.SmoothStep(0,1,Mathf.Clamp01((reactionAge-.12f)/.33f))*
                    (1-Mathf.SmoothStep(0,1,Mathf.Clamp01((reactionAge-.82f)/.58f)));
        float greeting=Mathf.Sin(Mathf.Clamp01(reactionAge/1.45f)*Mathf.PI);
        float entrance=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.6f));
        float step=0;
        if(Gait!=null)
        {
            if(Lift>0)Gait.Reset();
            float arc=Gait.Advance(dt,Walking>.01f);
            step=Gait.Sway;
            float target=Walking>.01f || Gait.SwingFoot>=0 ? .022f+.009f*arc : 0;
            compression=Mathf.MoveTowards(compression,target,dt*.19f);
            visual.localPosition=visualRest+Height*(Vector3.down*compression+Vector3.right*(step*.006f));
        }
        switch(Profile.Style)
        {
            case CompanionProfile.Kind.Spring: PoseSpring(t,greeting,reach,entrance,step); break;
            case CompanionProfile.Kind.Autumn: PoseAutumn(t,reactionAge,greeting,reach,entrance,step); break;
            case CompanionProfile.Kind.Summer: CarryLantern(t,greeting,reach,step); break;
            case CompanionProfile.Kind.Winter: CupSnow(t,greeting,reach,step); break;
            case CompanionProfile.Kind.Time: BowAndPause(t,reactionAge,greeting,reach,step); break;
            case CompanionProfile.Kind.Star: TraceStar(t,reactionAge,greeting,reach,step); break;
        }
        Pose("ThighL",Vector3.right,-10*Lift);
        Pose("ThighR",Vector3.right,-10*Lift);
        Pose("ShinL",Vector3.right,15*Lift);
        Pose("ShinR",Vector3.right,15*Lift);
        Pose("FootL",Vector3.right,0);
        Pose("FootR",Vector3.right,0);
        if(Lift==0)Gait?.Solve(visual);
    }

    void PoseSpring(float t,float greeting,float reach,float entrance,float step)
    {
        Pose("Chest",new Vector3(-.42f,1,0).normalized,1.5f*Mathf.Sin(t*1.8f)+6*entrance+28*greeting);
        Pose("Head",Vector3.up,4*Mathf.Sin(t*.9f)-23*greeting);
        Pose("UpperArmL",Vector3.forward,-31*reach+step*4);
        Pose("ForearmL",Vector3.right,-34*reach);
        Pose("PalmL",Vector3.up,28*greeting);
        Pose("UpperArmR",Vector3.forward,8*greeting-step*4);
        Pose("CurlL",Vector3.forward,2*Mathf.Sin(t*1.9f)+3*step);
        Pose("CurlR",Vector3.forward,2*Mathf.Sin(t*1.9f+.7f)-3*step);
        Pose("HemL",Vector3.right,4*step+2*greeting);
        Pose("HemR",Vector3.right,-4*step-2*greeting);
    }

    void PoseAutumn(float t,float reactionAge,float greeting,float reach,float entrance,float step)
    {
        // Open the hanging arm away from the face, then curl the wrist as the leaves release.
        float flick=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,Profile.Release,reactionAge))*
            (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.61f,.90f,reactionAge)));
        Pose("Hips",Vector3.up,-5*greeting);
        Pose("Chest",new Vector3(.12f,1,-.20f),3*Mathf.Sin(t*1.6f)-12*entrance+16*greeting);
        Pose("Head",Vector3.up,4*Mathf.Sin(t*1.25f)-8*greeting);
        Pose("UpperArmL",Vector3.forward,-22*reach+4*step);
        Pose("ForearmL",new Vector3(-.57f,.82f,0),14*reach);
        Pose("PalmL",Vector3.forward,-21*flick);
        Pose("UpperArmR",Vector3.right,3*step);
        Pose("CurlL",Vector3.forward,2*Mathf.Sin(t*2)+4*greeting);
        Pose("CurlR",Vector3.forward,2*Mathf.Sin(t*2+.7f)-2*greeting);
        Pose("HemL",Vector3.forward,-4*greeting-4*step);
        Pose("HemR",Vector3.forward,4*greeting+4*step);
    }

    void CarryLantern(float t,float greeting,float reach,float step)
    {
        Pose("Chest",Vector3.up,3*Mathf.Sin(t*1.4f)+9*greeting);
        Pose("Head",new Vector3(.5f,1,.12f),6*Mathf.Sin(t*.9f)-9*greeting);
        Pose("UpperArmR",Vector3.forward,4*step-5*reach);
        Pose("ForearmR",Vector3.right,8*reach-4*step);
        Pose("PalmR",Vector3.forward,5*Mathf.Sin(t*2.2f)+11*reach);
        Pose("UpperArmL",Vector3.forward,-13*reach+5*step);
        Pose("ForearmL",Vector3.up,16*reach);
        Pose("HemL",Vector3.right,8*step);
        Pose("HemR",Vector3.right,-8*step);
    }

    void CupSnow(float t,float greeting,float reach,float step)
    {
        Pose("Chest",Vector3.right,3*Mathf.Sin(t*1.6f)+9*greeting);
        Pose("Head",Vector3.right,8*greeting);
        Pose("UpperArmL",Vector3.forward,12*reach+3*step);
        Pose("ForearmL",new Vector3(-.66f,.65f,-.34f),28*reach);
        Pose("PalmL",Vector3.up,-9*reach);
        Pose("ForearmR",Vector3.right,16*reach-3*step);
        Pose("PalmR",Vector3.forward,15*reach);
        Pose("HemL",Vector3.right,4*step);
        Pose("HemR",Vector3.right,-4*step);
    }

    void BowAndPause(float t,float reactionAge,float greeting,float reach,float step)
    {
        float hold=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.26f,.54f,reactionAge))
            *(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,1.26f,reactionAge)));
        Pose("Chest",Vector3.right,1.4f*Mathf.Sin(t*1.4f)+15*greeting);
        Pose("Head",Vector3.right,12*greeting);
        Pose("ForearmL",Vector3.right,-25*hold);
        Pose("ForearmR",Vector3.right,-23*hold);
        Pose("PalmL",Vector3.up,19*reach);
        Pose("PalmR",Vector3.up,-19*reach);
        Pose("HemL",Vector3.right,5*step);
        Pose("HemR",Vector3.right,-5*step);
        Pose("CurlL",Vector3.forward,3*step);
        Pose("CurlR",Vector3.forward,-3*step);
    }

    void TraceStar(float t,float reactionAge,float greeting,float reach,float step)
    {
        float sweep=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.74f,reactionAge));
        Pose("Chest",Vector3.up,3*Mathf.Sin(t*1.8f)-17*greeting);
        Pose("Head",Vector3.up,4*Mathf.Sin(t*1.2f)+19*greeting);
        Pose("UpperArmL",Vector3.forward,-20*reach);
        Pose("ForearmL",new Vector3(-.65f,.75f,0),18*reach);
        Pose("PalmL",Vector3.up,36*reach*sweep);
        Pose("ForearmR",Vector3.right,-7*reach);
        Pose("CurlL",Vector3.forward,4*greeting+5*step);
        Pose("CurlR",Vector3.forward,-4*greeting-5*step);
        Pose("HemL",Vector3.right,7*step);
        Pose("HemR",Vector3.right,-7*step);
    }

    void OnDestroy()
    {
        if(!material)return;
        if(Application.isPlaying) Destroy(material); else DestroyImmediate(material);
    }
}
