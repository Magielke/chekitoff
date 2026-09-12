TEXTURE2D(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);
TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

void Blur_float(float2 UV, float Blur_strength, float MinBlur,
                float FocusDist, float FalloffRange, out float3 Out)
{
    float rawDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, UV).r;
    float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

    float k = saturate((sceneDepth - FocusDist) / max(0.001, FalloffRange));
    float strength = lerp(MinBlur, Blur_strength, k);

    float2 texelSize = strength / float2(_ScreenParams.x, _ScreenParams.y);

    float3 col = 0;
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2(-texelSize.x, -texelSize.y)).rgb;
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2( 0.0,         -texelSize.y)).rgb;
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2( texelSize.x, -texelSize.y)).rgb;
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2(-texelSize.x,  0.0        )).rgb;
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV                                     ).rgb;
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2( texelSize.x,  0.0        )).rgb;
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2(-texelSize.x,  texelSize.y)).rgb;
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2( 0.0,          texelSize.y)).rgb;
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2( texelSize.x,  texelSize.y)).rgb;

    Out = col / 9.0;
}