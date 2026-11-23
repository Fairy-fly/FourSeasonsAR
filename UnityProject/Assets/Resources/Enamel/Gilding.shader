Shader "FourSeasons/AlbumGilding"
{
    Properties { [PerRendererData] _MainTex ("Artwork", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _AlbumReviewClock;
            struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            V vert(appdata_full a) {
                V o; o.pos=UnityObjectToClipPos(a.vertex); o.uv=a.texcoord.xy; o.color=a.color; return o;
            }
            fixed4 frag(V i):SV_Target {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;
                // Two narrow highlights meet in the inlay, with a rest between passes.
                half sweep=frac((_AlbumReviewClock > 0 ? _AlbumReviewClock : _Time.y)*.115)*1.65-.17;
                half distance=abs((1-abs(i.uv.x*2-1)) + .085*(i.uv.y-.5) - sweep);
                half glint=1-smoothstep(.006,.092,distance);
                half gold=saturate(3*(c.r-c.b)+max(max(c.r,c.g),c.b)-.42);
                c.rgb+=half3(.18,.14,.09)*gold*glint*glint;
                return c;
            }
            ENDCG
        }
    }
}
