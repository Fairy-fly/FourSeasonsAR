Shader "FourSeasons/SpringStudy"
{
    Properties { _MainTex ("Turnaround", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Observer;
            struct fragmentInput {
                float4 position : SV_POSITION;
                float3 capture : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 weights : TEXCOORD2;
            };
            fragmentInput vert(appdata_full v)
            {
                fragmentInput o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.capture = float3(v.texcoord.xy, .5 + (.5-v.vertex.z*200.0)/280.0);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.weights = v.color.rgb;
                return o;
            }
            fixed4 frag(fragmentInput i) : SV_Target
            {
                float x = clamp(i.capture.x,.002,.998);
                float side = clamp(i.capture.z,.002,.998);
                float y = i.capture.y;
                fixed3 profile = tex2D(_MainTex,float2((1+side)/3,y)).rgb;
                fixed3 front = tex2D(_MainTex,float2(x/3,y)).rgb;
                fixed3 reverse = tex2D(_MainTex,float2((3-x)/3,y)).rgb;
                fixed3 pigment = front*i.weights.x + profile*i.weights.y + reverse*i.weights.z;
                // The measured side elevation supplies fine facial detail near a true profile.
                pigment = lerp(pigment,profile,smoothstep(.83,.98,abs(_Observer.x)));
                fixed3 axial = _Observer.z < 0 ? front : reverse;
                pigment = lerp(pigment,axial,smoothstep(.93,.995,abs(_Observer.z)));
                float light = saturate(dot(normalize(i.normal),normalize(float3(-.4,.65,-.8))));
                return fixed4(pigment * (.92 + .08*light),1);
            }
            ENDCG
        }
    }
}
