Shader "Custom/GrassTrampleLit_ShadowedURP"
{
    Properties
    {
        _MainTex("Grass Texture", 2D) = "white" {}
        _Color("Tint Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        _WindSpeed("Wind Speed", Float) = 1.0
        _WindStrength("Wind Strength", Float) = 0.1

        _TrampleRadius("Trample Radius", Float) = 1.5
        _BendStiffness("Bend Stiffness", Range(0,1)) = 0.5
        _TrailLifetime("Trail Lifetime", Float) = 180
        _FadeTime("Fade Time", Float) = 5
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        Cull Off
        ZWrite On

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Cutoff;
                float _WindSpeed;
                float _WindStrength;
                float _TrampleRadius;
                float _BendStiffness;
                float _TrailLifetime;
                float _FadeTime;
            CBUFFER_END

            StructuredBuffer<float4> _TrampleTrailBuffer;
            int _TrampleTrailCount;
            float _GlobalTime;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 color : COLOR;
                float fogFactor : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // -----------------------
            // Vertex Deformation
            // -----------------------
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

                    float influence = pow(saturate(1.0 - dist / _TrampleRadius), 3);

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
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                // Compute shadow coordinates correctly for URP
                float4 shadowCoord = TransformWorldToShadowCoord(posWS);
                output.shadowCoord = shadowCoord;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                albedo *= _Color * input.color;
                clip(albedo.a - _Cutoff);

                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 ambient = float3(0.4,0.45,0.5);

                // Sample main light shadow
                float shadow = GetMainLightShadow(input.shadowCoord);

                float3 lighting = ambient + mainLight.color * NdotL * shadow;
                float3 color = albedo.rgb * lighting;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }

            ENDHLSL
        }

        // -----------------------
        // Shadow Caster Pass
        // -----------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _Cutoff;
            CBUFFER_END

            StructuredBuffer<float4> _TrampleTrailBuffer;
            int _TrampleTrailCount;
            float _GlobalTime;
            float _TrampleRadius;
            float _TrailLifetime;
            float _FadeTime;

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

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

                    float influence = pow(saturate(1.0 - dist / _TrampleRadius), 3);

                    float2 dir = deltaXZ;
                    if (length(dir) > 0.001)
                        dir = normalize(dir);

                    offset.x += dir.x * influence * 0.6;
                    offset.y -= influence * 1.2;
                    offset.z += dir.y * influence * 0.6;
                }

                return offset;
            }

            Varyings vertShadow(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                posWS += ApplyTrampleShadow(posWS);

                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = input.uv;
                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
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
