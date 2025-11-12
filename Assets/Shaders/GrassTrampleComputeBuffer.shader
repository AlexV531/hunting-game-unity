Shader "Custom/GrassTrampleComputeBuffer"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

        [Header(Wind)]
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindStrength ("Wind Strength", Float) = 0.1

        [Header(Trample)]
        _TrampleRadius ("Trample Radius", Float) = 10
        _TrailLifetime ("Trail Lifetime", Float) = 5.0
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float _TrampleRadius;
                float _TrailLifetime;
            CBUFFER_END

            StructuredBuffer<float4> _TrampleTrailBuffer; // xyz = position, w = creationTime
            int _TrampleTrailCount;
            float _GlobalTime;

            // Wind sway
            float3 ApplyWind(float3 positionWS, float verticalFactor)
            {
                float windPhase = (_GlobalTime * _WindSpeed) + (positionWS.x * 0.5) + (positionWS.z * 0.5);
                float wind = sin(windPhase) * _WindStrength;
                return float3(wind, 0, wind * 0.5) * verticalFactor;
            }

            // Trample bending
            float3 ApplyTrample(float3 worldPos, float3 positionOS)
            {
                float3 offset = float3(0,0,0);
                float verticalFactor = saturate(positionOS.y * 2.0);

                for (int i = 0; i < _TrampleTrailCount; i++)
                {
                    float3 trailPos = _TrampleTrailBuffer[i].xyz;
                    float age = _GlobalTime - _TrampleTrailBuffer[i].w;
                    float fade = saturate(1.0 - age / _TrailLifetime);
                    if (fade <= 0) continue;

                    float2 deltaXZ = worldPos.xz - trailPos.xz;
                    float dist = length(deltaXZ);
                    if (dist > _TrampleRadius) continue; // spatial culling

                    float influence = saturate(1.0 - dist / _TrampleRadius) * fade;
                    influence = influence * influence * influence;

                    float2 dir = normalize(deltaXZ);
                    offset.x += dir.x * influence * 0.6;
                    offset.y -= influence * 1.2;
                    offset.z += dir.y * influence * 0.6;
                }

                return offset;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // Trample
                float3 trampleOffset = ApplyTrample(positionWS, input.positionOS.xyz);
                positionWS += trampleOffset;

                // Wind
                float verticalFactor = saturate(input.positionOS.y);
                float3 windOffset = ApplyWind(positionWS, verticalFactor);
                positionWS += windOffset;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                albedo *= _Color * input.color;
                clip(albedo.a - _Cutoff);

                Light mainLight = GetMainLight();
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, mainLight.direction));

                float3 ambient = float3(0.4, 0.45, 0.5);
                float3 lighting = ambient + (mainLight.color * NdotL * 0.6);

                half3 color = albedo.rgb * lighting;
                color = MixFog(color, input.fogFactor);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
