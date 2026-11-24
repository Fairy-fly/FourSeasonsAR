using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using FingerTouch=UnityEngine.InputSystem.EnhancedTouch.Touch;

public sealed class WorldWalk : MonoBehaviour
{
    SpringVisit visit;
    ARPlaneManager planes;
    ARRaycastManager rays;
    ARAnchorManager anchors;
    AROcclusionManager depth;
    ARPlane surface, candidate;
    ARAnchor anchor;
    Transform actor;
    FlowerCompanion body;
    CompanionPetals effects;
    HoverAura aura;
    FootContactShade shade;
    bool useWalking;
    LineRenderer flower;
    Material markerInk;
    readonly List<ARRaycastHit> hits=new();
    string role;
    float stableFrom, downAt, goalAt, scale=1, lastPairAngle, lastPairDistance, walkSpeed;
    Vector2 down, goal, grabOffset;
    int finger=-1, generation;
    bool active, pending, dragging, downOnActor, pair, moving, hasDrag, paused, requireLift;
    Vector2 lastDrag;
    bool faceVisitor;
    bool needsRecheck;
    float boundaryInvalidAt=-1, rescanHintUntil;
    readonly ArFrameWindow frames=new();
    const float TurnDegreesPerSecond=115;
    public bool Active=>active;
    public string Role=>role;
    const float FootMargin=.058f;
    float Clearance=>FootMargin*scale;

    public void Open(SpringVisit owner,string id)
    {
        visit=owner;role=id;useWalking=MovementPreference.ExperimentalWalk;
        var origin=owner.images.gameObject;
        planes=GetOrAdd<ARPlaneManager>(origin);
        planes.requestedDetectionMode=PlaneDetectionMode.Horizontal;
        rays=GetOrAdd<ARRaycastManager>(origin);
        anchors=GetOrAdd<ARAnchorManager>(origin);
        depth=GetOrAdd<AROcclusionManager>(owner.arCamera.gameObject);
        depth.requestedEnvironmentDepthMode=EnvironmentDepthMode.Fastest;
        planes.enabled=rays.enabled=anchors.enabled=depth.enabled=true;
        EnhancedTouchSupport.Enable();
        active=true;paused=false;requireLift=true;
        markerInk=new Material(Shader.Find("Sprites/Default"));
        flower=new GameObject("落点花影").AddComponent<LineRenderer>();
        flower.material=markerInk;flower.loop=true;flower.useWorldSpace=false;
        flower.widthMultiplier=.003f;flower.positionCount=80;
        for(int i=0;i<80;i++)
        {
            float angle=i*Mathf.PI/40;
            float radius=.052f*(.84f+.16f*Mathf.Cos(5*angle));
            flower.SetPosition(i,new Vector3(radius*Mathf.Cos(angle),.002f,radius*Mathf.Sin(angle)));
        }
        flower.startColor=flower.endColor=new Color(1,.66f,.72f,.9f);
        flower.gameObject.SetActive(false);
        ResetSite();
    }
    static T GetOrAdd<T>(GameObject o) where T:Component=>o.GetComponent<T>()??o.AddComponent<T>();

    void CancelGesture()
    {
        if(body){SetDriving(0);SetHeld(false);}
        if(dragging && actor && surface && hasDrag) actor.position=World(lastDrag);
        walkSpeed=0;faceVisitor=false;moving=dragging=hasDrag=pair=downOnActor=false;finger=-1;
    }
    public void ResetSite()
    {
        generation++;pending=false;needsRecheck=false;boundaryInvalidAt=-1;rescanHintUntil=0;CancelGesture();
        if(actor)Destroy(actor.gameObject);
        if(anchor)Destroy(anchor.gameObject);
        actor=null;body=null;anchor=null;surface=null;candidate=null;scale=1;
        if(flower)flower.gameObject.SetActive(false);
        if(active)visit.page.WorldHint("缓慢移动手机，寻找地面或桌面");
    }
    public void Close()
    {
        if(!active)return;
        active=false;frames.Write();ResetSite();
        planes.enabled=rays.enabled=anchors.enabled=depth.enabled=false;
        Destroy(flower.gameObject);Destroy(markerInk);
        EnhancedTouchSupport.Disable();
    }
    void OnDestroy(){Close();}
    void OnApplicationPause(bool value)
    {
        paused=value;
        if(!active)return;
        if(value && pending)
        {
            generation++;
            pending=false;
        }
        SuspendPlacement();
    }

    void SuspendPlacement()
    {
        if(!needsRecheck)
        {
            CancelGesture();
            frames.Write();
        }
        needsRecheck=true;
        requireLift=true;
        boundaryInvalidAt=-1;
        if(actor)actor.gameObject.SetActive(false);
        if(flower)flower.gameObject.SetActive(false);
    }

    public void FindActor()
    {
        if(!actor){visit.page.WorldHint("慢慢移动手机，寻找地面或桌面");return;}
        if(needsRecheck){visit.page.WorldHint("正在找回原来的位置");return;}
        var v=visit.arCamera.WorldToViewportPoint(actor.position);
        visit.page.WorldHint(v.z<0?"花灵在身后，请缓慢转身":v.x<.3f?"向左转，寻找花灵":v.x>.7f?"向右转，寻找花灵":v.y<.3f?"向下看，花灵在脚边":"花灵就在前方");
        Marker(actor.position);goalAt=Time.time+.9f;
    }
    static Vector2 InPlane(ARPlane plane,Vector3 world)
    {
        var point=plane.transform.InverseTransformPoint(world);
        return new Vector2(point.x,point.z);
    }
    Vector2 Local(Vector3 world)=>InPlane(surface,world);
    Vector3 World(Vector2 p)=>surface.transform.TransformPoint(new Vector3(p.x,0,p.y));
    void Marker(Vector3 at)
    {
        flower.gameObject.SetActive(true);flower.transform.position=at;
        flower.transform.up=surface?surface.transform.up:Vector3.up;
    }
    static ARPlane Merged(ARPlane plane){while(plane && plane.subsumedBy)plane=plane.subsumedBy;return plane;}
    bool Hit(Vector2 screen,out ARPlane plane,out Pose pose,bool requireClearance=false)
    {
        plane=null;pose=default;
        if(!rays.Raycast(screen,hits,TrackableType.PlaneWithinPolygon))return false;
        foreach(var hit in hits)
        {
            var p=Merged(planes.GetPlane(hit.trackableId));
            if(!p || p.alignment!=PlaneAlignment.HorizontalUp || p.trackingState!=TrackingState.Tracking)continue;
            if(surface && p!=Merged(surface))continue;
            if(requireClearance && !FloorBoundary.Contains(p.boundary,InPlane(p,hit.pose.position),Clearance))continue;
            plane=p;pose=hit.pose;return true;
        }
        return false;
    }

    static string LostTrackingHint() => ARSession.notTrackingReason switch
    {
        NotTrackingReason.InsufficientLight => "请开灯，再慢慢移动手机",
        NotTrackingReason.InsufficientFeatures => "对准有纹理的地面或桌面",
        NotTrackingReason.ExcessiveMotion => "请慢慢移动手机",
        NotTrackingReason.CameraUnavailable => "相机暂不可用，请返回后再试",
        _ => "定位暂停，请缓慢移动手机重新定位"
    };

    bool RestorePlacement()
    {
        if(!FloorBoundary.TryRecover(surface.boundary,Local(actor.position),Clearance,out var point))
        {
            if(boundaryInvalidAt<0) boundaryInvalidAt=Time.unscaledTime;
            visit.page.WorldHint("正在找回原来的位置");
            if(Time.unscaledTime-boundaryInvalidAt>.55f)
            {
                ResetSite();
                rescanHintUntil=Time.unscaledTime+2.5f;
                visit.page.WorldHint("原位置不能使用，请重新放置");
            }
            return false;
        }
        actor.position=World(point);
        var forward=Vector3.ProjectOnPlane(actor.forward,surface.transform.up);
        if(forward.sqrMagnitude>.000001f)actor.rotation=Quaternion.LookRotation(forward,surface.transform.up);
        if(aura)aura.Floor=surface.transform;
        if(shade)shade.Floor=surface.transform;
        body.Gait?.Reset();
        needsRecheck=false;
        boundaryInvalidAt=-1;
        visit.page.WorldHint("轻触人物，她会回应你");
        Debug.Log("World restored "+role);
        return true;
    }

    async void Place(ARPlane plane,Pose hit)
    {
        int token=generation;pending=true;
        Result<ARAnchor> result;
        try
        {
            result=await anchors.TryAddAnchorAsync(new Pose(hit.position,Quaternion.identity));
        }
        catch(System.InvalidOperationException)
        {
            if(active && token==generation)
            {
                pending=false;
                rescanHintUntil=Time.unscaledTime+1.5f;
                visit.page.WorldHint("定位还不稳定，请重新点选花影");
            }
            return;
        }
        if(!active || token!=generation)
        {
            if(result.status.IsSuccess() && result.value)Destroy(result.value.gameObject);
            return;
        }
        pending=false;
        if(!result.status.IsSuccess() || paused || ARSession.state!=ARSessionState.SessionTracking ||
            !plane || plane.trackingState!=TrackingState.Tracking ||
            !FloorBoundary.Contains(plane.boundary,InPlane(plane,hit.position),Clearance))
        {
            if(result.status.IsSuccess() && result.value)Destroy(result.value.gameObject);
            visit.page.WorldHint("定位还不稳定，请重新点选花影");return;
        }
        anchor=result.value;surface=plane;needsRecheck=false;
        Vector3 towards=Vector3.ProjectOnPlane(visit.arCamera.transform.position-hit.position,surface.transform.up);
        Quaternion heading=towards.sqrMagnitude>.00001f
            ? Quaternion.LookRotation(towards,surface.transform.up) : anchor.transform.rotation;
        SpawnActor(hit.position,heading);
        flower.gameObject.SetActive(false);
        visit.page.WorldHint(useWalking?"点地面让她走过去，长按可拖动":"点地面让她漂浮移动，长按可拖动");
        Debug.Log("World placed "+role);
    }

    void SpawnActor(Vector3 position,Quaternion heading)
    {
        actor=new GameObject("现实花灵").transform;
        actor.SetParent(anchor.transform,false);
        actor.SetPositionAndRotation(position,heading);
        actor.localScale=Vector3.one*scale;
        body=actor.gameObject.AddComponent<FlowerCompanion>();
        body.Initialize(role,.26f);
        if(useWalking)
        {
            body.EnableGroundGait();
            shade=actor.gameObject.AddComponent<FootContactShade>();
            shade.Initialize(body,surface.transform);
        }
        else
        {
            body.EnableHover();
            aura=actor.gameObject.AddComponent<HoverAura>();
            aura.Initialize(body,surface.transform);
        }
        var marks=new GameObject("同行花印");
        marks.transform.SetParent(anchor.transform,false);
        effects=marks.AddComponent<CompanionPetals>();
        var character=System.Array.Find(visit.characters,item=>item.referenceName==role);
        effects.Initialize(body,visit.arCamera,character ? character.petal : null);
    }

    public void SwitchRole()
    {
        if(!active)return;
        if(actor && (needsRecheck || !anchor || !surface || anchor.trackingState!=TrackingState.Tracking ||
            surface.trackingState!=TrackingState.Tracking || ARSession.state!=ARSessionState.SessionTracking))
        {
            visit.page.WorldHint("正在找回原来的位置");
            return;
        }
        var target=CompanionProfile.NextOwned(role,visit.Album);
        if(target==null)return;
        CancelGesture();
        requireLift=true;
        role=target.Id;
        if(actor && anchor && surface)
        {
            Vector3 position=actor.position;
            Quaternion heading=actor.rotation;
            actor.gameObject.SetActive(false);
            effects.gameObject.SetActive(false);
            Destroy(actor.gameObject);
            Destroy(effects.gameObject);
            SpawnActor(position,heading);
            flower.gameObject.SetActive(false);
        }
        visit.page.WorldHint("同行 · "+target.Name);
        Debug.Log("World swapped "+role);
    }

    void Update()
    {
        if(!active)return;
        var touches=FingerTouch.activeTouches;
        if(paused || ARSession.state!=ARSessionState.SessionTracking)
        {
            SuspendPlacement();
            candidate=null;
            visit.page.WorldHint(LostTrackingHint());
            return;
        }
        if(!actor)
        {
            if(pending || !AllowNewTouch(touches.Count))return;
            var center=new Vector2(Screen.width*.5f,Screen.height*.48f);
            bool found=Hit(center,out var p,out var hit,true);
            if(!found || candidate!=p){candidate=p;stableFrom=Time.time;}
            bool ready=found && Time.time-stableFrom>.35f;
            flower.gameObject.SetActive(ready);
            if(ready || Time.unscaledTime>=rescanHintUntil)
                visit.page.WorldHint(ready?"点花影，让她站在这里":"对准有纹理的地面或桌面，缓慢移动手机");
            if(ready)
            {
                Marker(hit.position);
                if(touches.Count==1 && touches[0].phase==UnityEngine.InputSystem.TouchPhase.Began &&
                    !visit.page.WorldConsumes(touches[0].screenPosition) && Hit(touches[0].screenPosition,out var tapped,out var target,true) &&
                    tapped==p && Vector3.Distance(target.position,hit.position)<.10f)Place(p,target);
            }
            return;
        }
        if(!surface || !anchor){ResetSite();return;}
        var plane=Merged(surface);
        if(plane!=surface){SuspendPlacement();surface=plane;}
        if(surface.trackingState!=TrackingState.Tracking || anchor.trackingState!=TrackingState.Tracking)
        {
            SuspendPlacement();
            visit.page.WorldHint("正在找回原来的位置");
            return;
        }
        if(!needsRecheck && !FloorBoundary.Contains(surface.boundary,Local(actor.position),Clearance))
            SuspendPlacement();
        if(needsRecheck && !RestorePlacement())return;
        actor.gameObject.SetActive(true);
        frames.Sample(Time.unscaledDeltaTime);
        if(!AllowNewTouch(touches.Count))return;
        if(touches.Count>=2)
        {
            var a=touches[0].screenPosition;var b=touches[1].screenPosition;
            if(visit.page.WorldConsumes(a)||visit.page.WorldConsumes(b)){CancelGesture();requireLift=true;return;}
            var diff=b-a;float angle=Mathf.Atan2(diff.y,diff.x)*Mathf.Rad2Deg;
            if(!pair){CancelGesture();pair=true;}
            else
            {
                float next=Mathf.Clamp(scale*diff.magnitude/Mathf.Max(1,lastPairDistance),.65f,1.5f);
                if(FloorBoundary.Contains(surface.boundary,Local(actor.position),FootMargin*next)){scale=next;actor.localScale=Vector3.one*scale;}
                actor.Rotate(surface.transform.up,-Mathf.DeltaAngle(lastPairAngle,angle),Space.World);
                body.Gait?.Reset();
            }
            lastPairDistance=diff.magnitude;lastPairAngle=angle;return;
        }
        if(pair){CancelGesture();requireLift=true;return;}
        if(touches.Count==1)HandleTouch(touches[0]);
        if(faceVisitor)
        {
            var direction=Vector3.ProjectOnPlane(visit.arCamera.transform.position-actor.position,surface.transform.up);
            if(direction.sqrMagnitude<.000001f)faceVisitor=false;
            else
            {
                var heading=Quaternion.LookRotation(direction,surface.transform.up);
                actor.rotation=Quaternion.RotateTowards(actor.rotation,heading,TurnDegreesPerSecond*Time.deltaTime);
                faceVisitor=Quaternion.Angle(actor.rotation,heading)>.2f;
            }
        }
        if(moving)
        {
            var from=Local(actor.position);
            float remaining=Vector2.Distance(from,goal);
            var direction=Vector3.ProjectOnPlane(World(goal)-actor.position,surface.transform.up);
            if(direction.sqrMagnitude>.000001f)
                actor.rotation=Quaternion.RotateTowards(actor.rotation,Quaternion.LookRotation(direction,surface.transform.up),TurnDegreesPerSecond*Time.deltaTime);
            float alignment=Vector3.Dot(actor.forward,direction.normalized);
            float topSpeed=body.Height*body.Profile.Pace*scale;
            float braking=Mathf.Sqrt(2*topSpeed*2.8f*remaining);
            walkSpeed=Mathf.MoveTowards(walkSpeed,Mathf.Min(topSpeed,braking)*Mathf.InverseLerp(.25f,.94f,alignment),topSpeed*3*Time.deltaTime);
            var next=Vector2.MoveTowards(from,goal,Time.deltaTime*walkSpeed);
            if(!FloorBoundary.CanTravel(surface.boundary,from,next,Clearance)){moving=false;SetDriving(0);walkSpeed=0;}
            else
            {
                actor.position=World(next);
                moving=(goal-next).sqrMagnitude>.000001f;
                SetDriving(moving?walkSpeed/topSpeed:0);
                if(!moving)walkSpeed=0;
            }
        }
        if(!moving && !dragging && Time.time>goalAt)flower.gameObject.SetActive(false);
    }

    void SetDriving(float amount)
    {
        body.Glide=amount;
        body.Walking=useWalking && amount>.01f ? 1 : 0;
    }
    void SetHeld(bool held)
    {
        body.Held=held;
        body.Lift=useWalking && held ? 1 : 0;
    }

    bool AllowNewTouch(int count)
    {
        if(requireLift && count>0)return false;
        requireLift=false;
        return true;
    }

    void HandleTouch(FingerTouch touch)
    {
        var pos=touch.screenPosition;
        if(touch.phase==UnityEngine.InputSystem.TouchPhase.Began)
        {
            if(visit.page.WorldConsumes(pos))return;
            finger=touch.touchId;down=pos;downAt=Time.time;
            downOnActor=Physics.Raycast(visit.arCamera.ScreenPointToRay(pos),out var h) && h.collider.GetComponentInParent<FlowerCompanion>()==body;
            if(downOnActor){moving=false;walkSpeed=0;SetDriving(0);}
            return;
        }
        if(finger!=touch.touchId)return;
        if(touch.phase==UnityEngine.InputSystem.TouchPhase.Canceled){CancelGesture();return;}
        bool valid=Hit(pos,out _,out var hit);
        if(downOnActor && !dragging && Time.time-downAt>.32f && valid)
        {
            faceVisitor=false;dragging=true;SetHeld(true);lastDrag=Local(actor.position);hasDrag=true;
            grabOffset=lastDrag-Local(hit.position);
        }
        if(dragging)
        {
            if(valid)
            {
                lastDrag=FloorBoundary.ReachableEnd(surface.boundary,lastDrag,Local(hit.position)+grabOffset,Clearance);
                actor.position=World(lastDrag)+(useWalking?surface.transform.up*.045f:Vector3.zero);Marker(World(lastDrag));
            }
            if(touch.phase==UnityEngine.InputSystem.TouchPhase.Ended){CancelGesture();body.Respond();}
            return;
        }
        if(touch.phase!=UnityEngine.InputSystem.TouchPhase.Ended)return;
        finger=-1;
        if(Vector2.Distance(pos,down)>Screen.width*.035f || visit.page.WorldConsumes(pos))return;
        if(downOnActor){faceVisitor=true;body.Respond();effects.Respond();return;}
        if(!valid){visit.page.WorldHint("请选择同一块地面或桌面内的位置");return;}
        faceVisitor=false;
        goal=FloorBoundary.ReachableEnd(surface.boundary,Local(actor.position),Local(hit.position),Clearance);
        Marker(World(goal));goalAt=Time.time+.8f;moving=true;
        visit.page.WorldHint("轻触人物，她会回应你");
    }
}
