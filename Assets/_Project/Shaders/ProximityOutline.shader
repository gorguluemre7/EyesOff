// Full-screen edge detection used with URP's "Full Screen Pass Renderer Feature".
// Finds edges in the camera depth and normals textures and paints them white, but only for
// surfaces closer than _OutlineFar. Because the depth texture only contains the nearest surface
// per pixel, objects hidden behind walls never get an outline.
//
// _OutlineStrength / _OutlineNear / _OutlineFar are global values set from C# (RoomLighting).
Shader "EyesOff/ProximityOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _Thickness ("Thickness (pixels)", Range(0.5, 4)) = 1.5
        _DepthThreshold ("Depth Threshold", Range(0.001, 1)) = 0.05
        _NormalThreshold ("Normal Threshold", Range(0.01, 2)) = 0.4
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ProximityOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _OutlineColor;
            float _Thickness;
            float _DepthThreshold;
            float _NormalThreshold;

            // Globals (Shader.SetGlobalFloat)
            float _OutlineStrength;
            float _OutlineNear;
            float _OutlineFar;

            float EyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // Roberts cross: compare the two diagonals around the pixel.
                float2 offset = _Thickness / _ScreenParams.xy;
                float2 uvBL = uv - offset;
                float2 uvTR = uv + offset;
                float2 uvBR = uv + float2(offset.x, -offset.y);
                float2 uvTL = uv + float2(-offset.x, offset.y);

                float dBL = EyeDepth(uvBL);
                float dTR = EyeDepth(uvTR);
                float dBR = EyeDepth(uvBR);
                float dTL = EyeDepth(uvTL);
                float nearest = min(min(dBL, dTR), min(dBR, dTL));

                // Depth edge: a jump in distance (silhouettes). Divided by distance so far surfaces
                // seen at a grazing angle don't light up everywhere.
                float depthEdge = sqrt(pow(dTR - dBL, 2) + pow(dTL - dBR, 2)) / nearest;

                // Normal edge: a change in surface direction (box corners, creases).
                float3 nBL = SampleSceneNormals(uvBL);
                float3 nTR = SampleSceneNormals(uvTR);
                float3 nBR = SampleSceneNormals(uvBR);
                float3 nTL = SampleSceneNormals(uvTL);
                float normalEdge = sqrt(dot(nTR - nBL, nTR - nBL) + dot(nTL - nBR, nTL - nBR));

                float edge = max(step(_DepthThreshold, depthEdge), step(_NormalThreshold, normalEdge));

                // Fade out between _OutlineNear and _OutlineFar (metres from the camera).
                float proximity = 1.0 - saturate((nearest - _OutlineNear) / max(_OutlineFar - _OutlineNear, 0.001));

                return lerp(sceneColor, _OutlineColor, edge * proximity * _OutlineStrength);
            }
            ENDHLSL
        }
    }
}
