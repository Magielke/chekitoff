Shader "Skybox/BlendedCubemap"
{
    Properties
    {
        _Tint ("Tint Color",Color) = (.5,.5,.5,.5)
        [Gamma] _Exposure ("Exposure", Range(0,8))=1.0
        _Rotation("Rotation", Range(0,360))=0
        [NoScaleOffset] _Tex("Cubemap A (Day)",Cube) = "grey"{}
        [NoScaleOffset] _Tex2("Cubemap B (Night)",Cube) = "grey" {}
        _Blend ("Blend",Range(0.0,1.0)) = 0.5
        
    }
    SubShader
    {
        Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
        Cull Off Zwrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vertSky
            #pragma fragment fragSky
            #pragma target 3.0
            #include "UnityCG.cginc"

            samplerCUBE _Tex;
            samplerCUBE _Tex2;
            half4 _Tex_HDR;
            half4 _Tex2_HDR;
            half4 _Tint;
            half _Exposure;
            half _Blend;
            float _Rotation;

            float3 RotateAroundYInDegrees(float3 vertex, float degrees)
            {
                float alhpa = degrees * UNITY_PI/180.0;
                float sina,cosa;
                sincos(alhpa, sina,cosa);
                float2x2 m = float2x2(cosa,-sina,sina,cosa);
                return float3(mul(m,vertex.xz),vertex.y).xzy;
            }

            struct skyInput{float4 pos: POSITION;};
            struct skyVaryings{
                float4 clipPos: SV_POSITION;
                float3 dir: TEXCOORD0;
                };
            skyVaryings vertSky(skyInput v)
            {
                skyVaryings o;
                float3 rotated = RotateAroundYInDegrees(v.pos.xyz, _Rotation);
                o.clipPos = UnityObjectToClipPos(rotated);
                o.dir = v.pos.xyz;
                return o;
                }
            half4 fragSky(skyVaryings i) :SV_Target
            {
                half4 texA = texCUBE(_Tex, i.dir);
                half4 texB = texCUBE(_Tex2, i.dir);

                half3 colA = DecodeHDR(texA, _Tex_HDR);
                half3 colB = DecodeHDR(texB,_Tex2_HDR);

                half3 c = lerp(colA,colB,_Blend);
                c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb * _Exposure;

                return half4(c,1);
            }
            ENDCG
        }
    }
    Fallback Off
}