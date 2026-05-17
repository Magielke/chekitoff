Shader "Custom/EyeParallax"
{
    Properties
    {
        _IrisTex      ("Iris Texture",    2D)    = "white" {}
        _CorneaTex    ("Cornea/Gloss",    2D)    = "white" {}
        _ParallaxDepth("Parallax Depth",  Range(0, 0.15)) = 0.05
        _IrisColor    ("Iris Color",      Color)  = (0.2, 0.4, 0.1, 1)
        _PupilSize    ("Pupil Size",      Range(0, 1))    = 0.3
        _Smoothness   ("Smoothness",      Range(0, 1))    = 0.95
        _Specular     ("Specular",        Color)  = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf StandardSpecular fullforwardshadows
        #pragma target 3.0

        sampler2D _IrisTex;
        sampler2D _CorneaTex;
        float     _ParallaxDepth;
        float4    _IrisColor;
        float     _PupilSize;
        float     _Smoothness;
        float4    _Specular;

        struct Input
        {
            float2 uv_IrisTex;
            float3 viewDir;       // tangent-space view direction
        };

        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            // --- Parallax offset ---
            // viewDir is in tangent space; xy gives the lateral angle
            float2 offset = IN.viewDir.xy * _ParallaxDepth;
            // Remap UV: center is 0.5,0.5 on your cornea mesh
            float2 irisUV = IN.uv_IrisTex + offset;

            // --- Iris & pupil ---
            float4 irisSample = tex2D(_IrisTex, irisUV);

            // Radial distance from center for procedural pupil (optional)
            float2 centeredUV = irisUV - 0.5;
            float  dist       = length(centeredUV) * 2.0;
            float  pupil      = smoothstep(_PupilSize, _PupilSize + 0.05, dist);

            float3 irisColor  = irisSample.rgb * _IrisColor.rgb * pupil;

            // --- Cornea gloss layer ---
            float4 cornea = tex2D(_CorneaTex, IN.uv_IrisTex); // no offset on gloss

            o.Albedo     = irisColor;
            o.Specular   = _Specular.rgb * cornea.r;
            o.Smoothness = _Smoothness * cornea.g;
            o.Alpha      = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
