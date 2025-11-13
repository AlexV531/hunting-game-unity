Shader "Custom/GrassTrampleComputeBuffer_ShadowReceiving"
{
    Properties
    {
        _MainTex("Grass Texture", 2D) = "white" {}
        _Color("Tint Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        _WindSpeed("Wind Speed", Float) = 1.0
        _WindStrength("Wind Strength", Float) = 0.1
        
        _AmbientStrength("Ambient Strength", Range(0,1)) = 0.3
        _ShadowStrength("Shadow Strength", Range(0,1)) = 0.7
        _NormalInfluence("Normal Lighting Influence", Range(0,1)) = 0.5
        _MinLighting("Minimum Lighting", Range(0,1)) = 0.3

        _TrampleRadius("Trample Radius", Float) = 1.5
        _BendStiffness("Bend Stiffness", Range(0,1)) = 0.5
        _TrailLifetime("Trail Lifetime", Float) = 180
        _FadeTime("Fade Time", Float) = 5
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Cull Off

        // ===== Forward Pass with Shadow Receiving =====
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            
            // Add shadow receiving support
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float4 color : COLOR;
                float4 shadowCoord : TEXCOORD4; // Shadow coordinates
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Cutoff;
                float _WindSpeed;
                float _WindStrength;
                float _AmbientStrength;
                float _ShadowStrength;
                float _NormalInfluence;
                float _MinLighting;
                float _TrampleRadius;
                float _BendStiffness;
                float _TrailLifetime;
                float _FadeTime;
            CBUFFER_END

            StructuredBuffer<float4> _TrampleTrailBuffer;
            int _TrampleTrailCount;
            float _GlobalTime;

            float3 ApplyTrample(float3 worldPos)
            {
                float3 offset = float3(0,0,0);

                for (int i = 0; i < _TrampleTrailCount; i++)
                {
                    float4 trailData = _TrampleTrailBuffer[i];
                    float3 trailPos = trailData.xyz;
                    float age = _GlobalTime - trailData.w;

                    if (age > _TrailLifetime) continue;

                    float fade = 1.0;
                    if (age > (_TrailLifetime - _FadeTime))
                        fade = saturate(1.0 - (age - (_TrailLifetime - _FadeTime)) / _FadeTime);

                    if (fade <= 0.0) continue;

                    float2 deltaXZ = worldPos.xz - trailPos.xz;
                    float dist = length(deltaXZ);
                    if (dist > _TrampleRadius) continue;

                    float influence = saturate(1.0 - dist / _TrampleRadius) * fade;
                    influence = influence * influence * influence;

                    float2 dir = deltaXZ;
                    if (length(dir) > 0.001)
                        dir = normalize(dir);

                    offset.x += dir.x * influence * 0.6;
                    offset.y -= influence * 1.2;
                    offset.z += dir.y * influence * 0.6;
                }

                return offset;
            }

            float3 ApplyWind(float3 worldPos)
            {
                float phase = (_GlobalTime * _WindSpeed) + (worldPos.x + worldPos.z) * 0.5;
                float wind = sin(phase) * _WindStrength;
                return float3(wind,0,wind*0.5);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                posWS += ApplyTrample(posWS);
                posWS += ApplyWind(posWS);

                output.positionCS = TransformWorldToHClip(posWS);
                output.positionWS = posWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.color = input.color;
                
                // Calculate shadow coordinates
                output.shadowCoord = TransformWorldToShadowCoord(posWS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                albedo *= _Color * input.color;
                clip(albedo.a - _Cutoff);

                // Get main light with shadow attenuation
                float4 shadowCoord = input.shadowCoord;
                Light mainLight = GetMainLight(shadowCoord);
                
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                
                // Soften the normal-based lighting with MinLighting floor
                // This prevents grass facing away from light from going too dark
                NdotL = lerp(_MinLighting, 1.0, NdotL);
                
                // Control how much the normal affects lighting
                float normalLighting = lerp(1.0, NdotL, _NormalInfluence);

                // Apply shadow strength - lerp between shadowed and lit
                float shadow = mainLight.shadowAttenuation;
                shadow = lerp(1.0 - _ShadowStrength, 1.0, shadow);
                
                // Ambient uses the AmbientStrength parameter
                float3 ambient = float3(0.4, 0.45, 0.5) * _AmbientStrength;
                float3 directLighting = mainLight.color * normalLighting * shadow;
                float3 lighting = ambient + directLighting;

                half3 color = albedo.rgb * lighting;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ===== Shadow Caster Pass =====
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            float4 _MainTex_ST;
            float _Cutoff;
            
            StructuredBuffer<float4> _TrampleTrailBuffer;
            int _TrampleTrailCount;
            float _GlobalTime;
            float _TrampleRadius;
            float _TrailLifetime;
            float _FadeTime;
            float _WindSpeed;
            float _WindStrength;

            float3 ApplyTrampleShadow(float3 worldPos)
            {
                float3 offset = float3(0,0,0);

                for (int i = 0; i < _TrampleTrailCount; i++)
                {
                    float4 trailData = _TrampleTrailBuffer[i];
                    float3 trailPos = trailData.xyz;
                    float age = _GlobalTime - trailData.w;

                    if (age > _TrailLifetime) continue;

                    float fade = 1.0;
                    if (age > (_TrailLifetime - _FadeTime))
                        fade = saturate(1.0 - (age - (_TrailLifetime - _FadeTime)) / _FadeTime);

                    if (fade <= 0.0) continue;

                    float2 deltaXZ = worldPos.xz - trailPos.xz;
                    float dist = length(deltaXZ);
                    if (dist > _TrampleRadius) continue;

                    float influence = saturate(1.0 - dist / _TrampleRadius) * fade;
                    influence = influence * influence * influence;

                    float2 dir = deltaXZ;
                    if (length(dir) > 0.001)
                        dir = normalize(dir);

                    offset.x += dir.x * influence * 0.6;
                    offset.y -= influence * 1.2;
                    offset.z += dir.y * influence * 0.6;
                }

                return offset;
            }

            float3 ApplyWindShadow(float3 worldPos)
            {
                float phase = (_GlobalTime * _WindSpeed) + (worldPos.x + worldPos.z) * 0.5;
                float wind = sin(phase) * _WindStrength;
                return float3(wind, 0, wind * 0.5);
            }

            float3 GetShadowPositionHClip(Attributes input)
            {
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                posWS += ApplyTrampleShadow(posWS);
                posWS += ApplyWindShadow(posWS);
                
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _MainLightPosition.xyz));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return positionCS;
            }

            Varyings vertShadow(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                output.positionCS = GetShadowPositionHClip(input);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                // Sample texture and apply alpha cutoff for correct shadow shape
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ===== Depth Only Pass (for depth prepass) =====
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float _Cutoff;

            StructuredBuffer<float4> _TrampleTrailBuffer;
            int _TrampleTrailCount;
            float _GlobalTime;
            float _TrampleRadius;
            float _TrailLifetime;
            float _FadeTime;
            float _WindSpeed;
            float _WindStrength;

            float3 ApplyTrampleDepth(float3 worldPos)
            {
                float3 offset = float3(0,0,0);

                for (int i = 0; i < _TrampleTrailCount; i++)
                {
                    float4 trailData = _TrampleTrailBuffer[i];
                    float3 trailPos = trailData.xyz;
                    float age = _GlobalTime - trailData.w;

                    if (age > _TrailLifetime) continue;

                    float fade = 1.0;
                    if (age > (_TrailLifetime - _FadeTime))
                        fade = saturate(1.0 - (age - (_TrailLifetime - _FadeTime)) / _FadeTime);

                    if (fade <= 0.0) continue;

                    float2 deltaXZ = worldPos.xz - trailPos.xz;
                    float dist = length(deltaXZ);
                    if (dist > _TrampleRadius) continue;

                    float influence = saturate(1.0 - dist / _TrampleRadius) * fade;
                    influence = influence * influence * influence;

                    float2 dir = deltaXZ;
                    if (length(dir) > 0.001)
                        dir = normalize(dir);

                    offset.x += dir.x * influence * 0.6;
                    offset.y -= influence * 1.2;
                    offset.z += dir.y * influence * 0.6;
                }

                return offset;
            }

            float3 ApplyWindDepth(float3 worldPos)
            {
                float phase = (_GlobalTime * _WindSpeed) + (worldPos.x + worldPos.z) * 0.5;
                float wind = sin(phase) * _WindStrength;
                return float3(wind, 0, wind * 0.5);
            }

            Varyings vertDepth(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                posWS += ApplyTrampleDepth(posWS);
                posWS += ApplyWindDepth(posWS);

                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 fragDepth(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}