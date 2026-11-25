using UnityEngine;
using Kind=CompanionProfile.Kind;

public sealed partial class FlowerCompanion
{
    const float SignatureDuration=1.45f;
    const float AlternateDuration=2.05f;
    static readonly string[] ActionJoints={
        "Hips","Chest","Head","UpperArmL","UpperArmR","ForearmL","ForearmR",
        "PalmL","PalmR","CurlL","CurlR","HemL","HemR"
    };
    float responseTime=float.NegativeInfinity;
    bool nextAlternate,posedAlternate;

    public bool AlternateReaction { get; private set; }
    public int ReactionCount { get; private set; }
    public float ReactionAge=>Time.time-responseTime;
    public float ReactionDuration=>AlternateReaction?AlternateDuration:SignatureDuration;
    public float ReactionRelease=>AlternateReaction ? Profile.Style switch
    {
        Kind.Spring=>.72f,
        Kind.Summer=>.64f,
        Kind.Autumn=>.82f,
        Kind.Winter=>.83f,
        Kind.Time=>.90f,
        _=>.74f
    } : Profile.Release;

    public bool Respond(bool tapped=true)=>TryRespond(Time.time,tapped);

    // Dragging can play the original settling gesture without consuming the next tap.
    public bool TryRespond(float now,bool tapped=true)
    {
        if(now-responseTime<ReactionDuration)return false;
        responseTime=now;
        AlternateReaction=tapped && nextAlternate;
        if(tapped)
        {
            nextAlternate=!nextAlternate;
            ReactionCount++;
        }
        return true;
    }

    void ResetActionPose(bool alternate)
    {
        if(alternate || posedAlternate)
            foreach(string joint in ActionJoints)
                if(joints.TryGetValue(joint,out var bone))bone.localRotation=rest[joint];
        posedAlternate=alternate;
    }

    void Accent(string joint,Vector3 axis,float degrees)
    {
        if(!joints.TryGetValue(joint,out var bone))return;
        var localAxis=bone.parent.InverseTransformDirection(transform.TransformDirection(axis));
        bone.localRotation=Quaternion.AngleAxis(degrees,localAxis)*bone.localRotation;
    }

    static float Ease(float t,float start,float end)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,end,t));
    static float Phrase(float t,float start,float ready,float release,float end)=>
        Ease(t,start,ready)*(1-Ease(t,release,end));

    void PoseAlternate(float t)
    {
        float prepare=Phrase(t,0,.29f,.55f,.80f);
        float extend=Phrase(t,.22f,.67f,1.12f,1.84f);
        float follow=Phrase(t,.64f,1.04f,1.35f,1.98f);
        switch(Profile.Style)
        {
            case Kind.Spring: // Reach sideways to catch a petal, then tip the open palm toward the visitor.
                Accent("Chest",new Vector3(.23f,-1,.36f),13*extend);
                Accent("Head",new Vector3(.46f,1,-.24f),15*extend-5*follow);
                Accent("UpperArmL",new Vector3(-.46f,0,-.89f),28*extend);
                Accent("ForearmL",Vector3.right,-19*extend);
                Accent("PalmL",Vector3.up,24*follow);
                Accent("UpperArmR",Vector3.forward,10*prepare+9*follow);
                Accent("CurlL",Vector3.forward,7*follow);
                Accent("HemL",Vector3.right,5*follow);
                break;
            case Kind.Summer: // Raise the lantern, look toward it, and float the free hand underneath.
                Accent("Chest",Vector3.up,15*extend-7*follow);
                Accent("Head",new Vector3(-.33f,.90f,.24f),22*extend-8*follow);
                Accent("UpperArmR",new Vector3(-.83f,0,.56f),22*extend);
                Accent("ForearmR",Vector3.right,-24*extend);
                Accent("PalmR",new Vector3(.20f,1,-.28f),19*follow);
                Accent("UpperArmL",Vector3.forward,-14*follow);
                Accent("ForearmL",Vector3.right,-13*follow);
                Accent("HemR",Vector3.right,-6*follow);
                break;
            case Kind.Autumn: // Sweep both sleeves at waist height and counter-turn through the shoulders.
                Accent("Hips",Vector3.up,-5*prepare+7*extend-6*follow);
                Accent("Chest",new Vector3(.20f,1,-.19f),-14*prepare+25*extend-17*follow);
                Accent("Head",Vector3.up,-14*extend+13*follow);
                Accent("UpperArmL",Vector3.forward,-18*extend);
                Accent("ForearmL",new Vector3(-.24f,.91f,-.25f),22*extend-9*follow);
                Accent("PalmL",Vector3.forward,-19*follow);
                Accent("UpperArmR",new Vector3(-.43f,0,.90f),14*follow);
                Accent("CurlR",Vector3.forward,-6*follow);
                Accent("HemL",Vector3.forward,-9*extend+9*follow);
                Accent("HemR",Vector3.forward,9*extend-9*follow);
                break;
            case Kind.Winter: // Lean toward the cupped snow, then open the arms as the flakes drift away.
                Accent("Chest",Vector3.right,10*prepare+9*extend-4*follow);
                Accent("Head",Vector3.right,9*prepare+8*extend-5*follow);
                Accent("UpperArmL",new Vector3(-.21f,0,-.98f),21*follow);
                Accent("UpperArmR",new Vector3(-.21f,0,.98f),21*follow);
                Accent("ForearmL",new Vector3(-.76f,.64f,0),18*extend-9*follow);
                Accent("ForearmR",new Vector3(-.76f,-.64f,0),17*extend-9*follow);
                Accent("PalmL",Vector3.up,18*follow);
                Accent("PalmR",Vector3.up,-18*follow);
                break;
            case Kind.Time: // Keep one hand steady and use the other to sweep the face of an imaginary clock.
                Accent("Chest",Vector3.up,-12*prepare+17*follow);
                Accent("Head",new Vector3(.60f,1,0),-13*prepare+19*follow);
                Accent("UpperArmL",new Vector3(-.22f,0,-.98f),20*extend);
                Accent("ForearmL",new Vector3(-.48f,.87f,0),23*extend-17*follow);
                Accent("PalmL",Vector3.forward,-20*extend+37*follow);
                Accent("ForearmR",Vector3.right,-10*prepare);
                Accent("CurlR",Vector3.forward,5*follow);
                break;
            case Kind.Star: // Present the star, then look up as the arms unfold beneath the arc.
                Accent("Chest",new Vector3(-.48f,.88f,0),13*extend+8*follow);
                Accent("Head",new Vector3(-.85f,-.48f,0),21*follow);
                Accent("UpperArmL",new Vector3(-.43f,0,-.90f),23*follow);
                Accent("UpperArmR",new Vector3(-.36f,0,.93f),21*follow);
                Accent("ForearmL",Vector3.right,-19*extend+10*follow);
                Accent("ForearmR",Vector3.right,-16*extend+9*follow);
                Accent("PalmL",Vector3.up,22*follow);
                Accent("PalmR",Vector3.up,-19*follow);
                Accent("CurlL",Vector3.forward,6*follow);
                break;
        }
    }

    public Vector3 ReactionPoint=>AlternateReaction && Profile.Style==Kind.Time?Palm:EffectPoint;

    // Positions in character space, at the standard 26 cm display scale.
    public static Vector3 AlternateParticlePath(Kind style,float t,int index)
    {
        float phase=index*2.39996f;
        float ray=(index%7-3)/3f;
        float span=.68f+.32f*Mathf.Cos(phase);
        switch(style)
        {
            case Kind.Spring:
                // A petal arc opens side to side, then falls.
                return new Vector3(.084f*t*ray,.060f*Mathf.Sin(t*Mathf.PI)*span-.035f*t,.050f*t*span);
            case Kind.Summer:
                float spiral=phase+4.4f*t;
                return new Vector3(.043f*t*Mathf.Cos(spiral),.116f*t*span,.043f*t*Mathf.Sin(spiral));
            case Kind.Autumn:
                float sweep=phase+7*t;
                return new Vector3(.095f*t*Mathf.Cos(sweep),.037f*Mathf.Sin(t*Mathf.PI)-.046f*t,.072f*t*Mathf.Sin(sweep));
            case Kind.Winter:
                return new Vector3(.054f*t*ray,.048f*Mathf.Sin(t*Mathf.PI)*span-.032f*t,.112f*t*span);
            case Kind.Time:
                float dial=index*.57f+.75f*t;
                float radius=.068f*Ease(t,0,.55f);
                return new Vector3(radius*Mathf.Cos(dial),radius*Mathf.Sin(dial),.033f*t);
            default:
                float arc=.15f+Mathf.PI*(index%13)/13;
                return new Vector3(.096f*t*Mathf.Cos(arc),.116f*t*Mathf.Sin(arc),.052f*t*span);
        }
    }
}
