using Unity.Collections;
using UnityEngine;

// The center path must stay inside the polygon with clearance for shoes and moving feet.
public static class FloorBoundary
{
    public static bool Contains(NativeArray<Vector2> polygon, Vector2 point, float margin)
    {
        if (!polygon.IsCreated || polygon.Length<3) return false;
        bool inside=false;
        float clearance=margin*margin;
        for (int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
        {
            Vector2 a=polygon[j], b=polygon[i];
            if (DistanceSquared(point,a,b)<=clearance) return false;
            if ((a.y>point.y)!=(b.y>point.y) && point.x<(b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x)
                inside=!inside;
        }
        return inside;
    }

    static float DistanceSquared(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 direction=b-a;
        float length=direction.sqrMagnitude;
        float fraction=length>0 ? Mathf.Clamp01(Vector2.Dot(point-a,direction)/length) : 0;
        return (point-a-direction*fraction).sqrMagnitude;
    }

    static float Cross(Vector2 a, Vector2 b) => a.x*b.y-a.y*b.x;

    static bool Crosses(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float area=Cross(b-a,d-c);
        if (Mathf.Abs(area)<1e-10f) return false;
        float u=Cross(c-a,d-c)/area;
        float v=Cross(c-a,b-a)/area;
        return u>=0 && u<=1 && v>=0 && v<=1;
    }

    public static bool CanTravel(NativeArray<Vector2> polygon, Vector2 from, Vector2 to, float margin)
    {
        if (!Contains(polygon,from,margin) || !Contains(polygon,to,margin)) return false;
        float clearance=margin*margin;
        for (int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
        {
            Vector2 a=polygon[j], b=polygon[i];
            if (Crosses(from,to,a,b) || DistanceSquared(a,from,to)<=clearance ||
                DistanceSquared(b,from,to)<=clearance) return false;
        }
        return true;
    }

    public static Vector2 ReachableEnd(NativeArray<Vector2> polygon, Vector2 from, Vector2 to, float margin)
    {
        if (CanTravel(polygon,from,to,margin)) return to;
        if (!Contains(polygon,from,margin)) return from;

        float low=0, high=1;
        for (int i=0;i<17;i++)
        {
            float f=(low+high)*.5f;
            if (CanTravel(polygon,from,Vector2.Lerp(from,to,f),margin)) low=f;
            else high=f;
        }
        return Vector2.Lerp(from,to,low);
    }

    public static bool TryRecover(NativeArray<Vector2> polygon, Vector2 around, float margin, out Vector2 point)
    {
        point=around;
        if (Contains(polygon,point,margin)) return true;

        // An updated boundary can move a few centimeters. Search near the same placement
        // and ask for a new placement if its plane no longer supports the character.
        for (int ring=1;ring<=8;ring++)
        for (int i=0;i<24;i++)
        {
            float angle=i*Mathf.PI/12;
            Vector2 candidate=around+.01f*ring*new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            if (!Contains(polygon,candidate,margin)) continue;
            point=candidate;
            return true;
        }
        return false;
    }
}
