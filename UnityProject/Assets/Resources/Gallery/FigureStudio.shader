Shader "FourSeasons/FigureStudio"
{
    Properties
    {
        _MainTex ("Pigment", 2D) = "white" {}
        [Normal] _NormalMap ("Detail", 2D) = "bump" {}
        _Surface ("Roughness (G), Metal (B)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _NormalMap, _Surface;
            struct Varyings {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normal : TEXCOORD1;
                half4 tangent : TEXCOORD2;
            };
            Varyings vert(appdata_tan v)
            {
                Varyings o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent = half4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w * unity_WorldTransformParams.w);
                return o;
            }
            fixed4 frag(Varyings i, fixed facing : VFACE) : SV_Target
            {
                half3 baseNormal = normalize(i.normal);
                half3 tangent = normalize(i.tangent.xyz);
                half3 bitangent = cross(baseNormal,tangent) * i.tangent.w;
                half3 detail = UnpackNormal(tex2D(_NormalMap,i.uv));
                half3 n = normalize(tangent*detail.x + bitangent*detail.y + baseNormal*detail.z);
                n *= facing > 0 ? 1 : -1;
                half3 color = tex2D(_MainTex,i.uv).rgb;
                half2 surface = tex2D(_Surface,i.uv).gb;
                half3 key = normalize(half3(-.45,.65,-.84));
                half3 halfway = normalize(key + half3(0,0,-1));
                half diffuse = saturate(dot(n,key));
                half spec = pow(saturate(dot(n,halfway)),lerp(64,10,surface.x));
                half3 glint = lerp(half3(.065,.059,.055),color*.3,surface.y);
                return fixed4(color*(.76+.27*diffuse) + glint*spec*(1-.7*surface.x),1);
            }
            ENDCG
        }
    }
}
