using System;
using System.Collections.Generic;
using UnityEngine;

// Foot contacts live in the placement anchor's coordinates so AR relocalization moves both feet with the floor.
public sealed class PlantedWalk
{
    sealed class Leg
    {
        public Transform thigh, shin, ankle;
        public Vector3 restAnkle, contact;
        public Quaternion restOrientation;
        public Quaternion thighRest, shinRest, ankleRest;
    }

    readonly Transform root, anchor;
    readonly Leg[] legs = new Leg[2];
    readonly float height;
    Vector3 previous, swingFrom, swingTo;
    float phase;
    int swing = -1, next;
    bool engaged, firstStep;
    public event Action<Vector3> Landed;
    public float Sway { get; private set; }
    public int SwingFoot => swing;

    public PlantedWalk(Transform root, Dictionary<string,Transform> bones, float height)
    {
        this.root=root;
        this.height=height;
        anchor=root.parent;
        for(int i=0;i<2;i++)
        {
            string suffix=i==0?"L":"R";
            var leg=new Leg { thigh=bones["Thigh"+suffix], shin=bones["Shin"+suffix], ankle=bones["Foot"+suffix] };
            leg.restAnkle=root.InverseTransformPoint(leg.ankle.position);
            leg.restOrientation=Quaternion.Inverse(root.rotation)*leg.ankle.rotation;
            leg.thighRest=leg.thigh.localRotation;
            leg.shinRest=leg.shin.localRotation;
            leg.ankleRest=leg.ankle.localRotation;
            legs[i]=leg;
        }
        Reset();
    }

    Vector3 InAnchor(Vector3 world) => anchor ? anchor.InverseTransformPoint(world) : world;
    Vector3 InWorld(Vector3 point) => anchor ? anchor.TransformPoint(point) : point;
    Vector3 Rest(int i) => InAnchor(root.TransformPoint(legs[i].restAnkle));
    public Vector3 Ankle(int i) => legs[i].ankle.position;

    public void Reset()
    {
        previous=InAnchor(root.position);
        swing=-1;
        next=legs[0].restAnkle.z < legs[1].restAnkle.z ? 0 : 1;
        firstStep=true;
        engaged=false;
        Sway=0;
        for(int i=0;i<2;i++)legs[i].contact=Rest(i);
    }

    public float Advance(float dt,bool walking)
    {
        Vector3 position=InAnchor(root.position);
        Vector3 travel=position-previous;
        previous=position;
        float unit=height*root.lossyScale.y;
        if(travel.magnitude>.65f*unit){Reset();return 0;}
        engaged|=walking;
        if(!engaged)return 0;
        float span=.086f*unit;
        if(swing<0)
        {
            float error=Vector3.Distance(legs[next].contact,Rest(next));
            int other=1-next;
            if(!walking && Vector3.Distance(legs[other].contact,Rest(other))>error)
            {
                next=other;
                error=Vector3.Distance(legs[next].contact,Rest(next));
            }
            if(error>(walking?(firstStep?.10f:.65f)*span:.009f*unit))
            {
                firstStep=false;
                swing=next;
                swingFrom=legs[swing].contact;
                swingTo=Rest(swing)+(walking?travel.normalized*span*1.48f:Vector3.zero);
                phase=0;
            }
        }
        if(swing>=0)
        {
            // Advance by measured ground travel. Complete an unfinished step gently when braking.
            float advance=walking ? Mathf.Max(travel.magnitude/span,dt*1.3f) : dt*4.5f;
            phase=Mathf.Min(1,phase+advance);
            if(phase>=1)
            {
                legs[swing].contact=swingTo;
                var sole=root.InverseTransformPoint(InWorld(swingTo));
                sole.y=0;
                Landed?.Invoke(root.TransformPoint(sole));
                next=1-swing;
                swing=-1;
            }
        }
        float arc=swing<0?0:Mathf.Sin(phase*Mathf.PI);
        Sway=(swing==0?1:-1)*arc;
        return arc;
    }

    Vector3 Target(int i)
    {
        if(i!=swing)return InWorld(legs[i].contact);
        Vector3 target=InWorld(Vector3.Lerp(swingFrom,swingTo,Mathf.SmoothStep(0,1,phase)));
        return target+root.up*(height*root.lossyScale.y*.047f*Mathf.Sin(Mathf.PI*phase));
    }

    public void Solve(Transform visual)
    {
        if(!engaged)return;
        // Lower the hips just enough for the shorter reach of the staggered rear leg.
        // The shoe stays on its contact even at the end of the supporting phase.
        float lower=0;
        for(int i=0;i<2;i++)
        {
            var leg=legs[i];
            Vector3 reach=leg.thigh.position-Target(i);
            float length=Vector3.Distance(leg.thigh.position,leg.shin.position)+Vector3.Distance(leg.shin.position,leg.ankle.position);
            float lateral=Vector3.ProjectOnPlane(reach,root.up).sqrMagnitude;
            float vertical=Mathf.Sqrt(Mathf.Max(0,length*length*.994f-lateral));
            lower=Mathf.Max(lower,Vector3.Dot(reach,root.up)-vertical);
        }
        visual.position-=root.up*Mathf.Min(lower,height*root.lossyScale.y*.12f);
        for(int i=0;i<2;i++)
        {
            Leg leg=legs[i];
            var target=Target(i);
            leg.thigh.localRotation=leg.thighRest;
            leg.shin.localRotation=leg.shinRest;
            leg.ankle.localRotation=leg.ankleRest;
            Vector3 hip=leg.thigh.position;
            float a=Vector3.Distance(hip,leg.shin.position);
            float b=Vector3.Distance(leg.shin.position,leg.ankle.position);
            Vector3 direction=(target-hip).normalized;
            float d=Mathf.Clamp(Vector3.Distance(hip,target),Mathf.Abs(a-b)+.00001f,a+b-.00001f);
            float along=(a*a-b*b+d*d)/(2*d);
            Vector3 pole=Vector3.ProjectOnPlane(root.forward,direction).normalized;
            Vector3 knee=hip+direction*along+pole*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            leg.thigh.rotation=Quaternion.FromToRotation(leg.shin.position-hip,knee-hip)*leg.thigh.rotation;
            leg.shin.rotation=Quaternion.FromToRotation(leg.ankle.position-leg.shin.position,hip+direction*d-leg.shin.position)*leg.shin.rotation;
            // A planted sole stays parallel to the floor while the knee bends above it.
            leg.ankle.rotation=root.rotation*leg.restOrientation;
        }
    }
}
