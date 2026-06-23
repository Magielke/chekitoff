TEXTURE2D(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);

void Blur_float(float2 UV, float Blur_strength, out float3 Out)
{
    float2 texelSize = Blur_strength / float2(_ScreenParams.x, _ScreenParams.y);
    float3 col = float3(0.0, 0.0, 0.0);
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2(-texelSize.x, -texelSize.y));
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2( 0.0,         -texelSize.y));
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2( texelSize.x, -texelSize.y));
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2(-texelSize.x,  0.0        ));
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2( 0.0,          0.0        ));
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2( texelSize.x,  0.0        ));
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2(-texelSize.x,  texelSize.y));
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2( 0.0,          texelSize.y));
    col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UV + float2( texelSize.x,  texelSize.y));
    Out = col / 9.0;
}