Shader "Custom/GrassDetailTrample"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        
        [Header(Wind)]
        _WindSpeed ("Wind Speed", Float) = 1.0
        _WindStrength ("Wind Strength", Float) = 0.1
        
        [Header(Trample Settings)]
        _TrampleRadius ("Trample Radius", Float) = 1.5
        _BendStiffness ("Bend Stiffness", Range(0,1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="TransparentCutout" 
            "Queue"="AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
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
                float _BendStiffness;
            CBUFFER_END

            float4 _TramplerPositions[20];
            float _TramplerStrengths[20];
            int _TramplerCount;
            float _RecoverySpeed;
            float _GlobalTime;

            float3 ApplyTrample(float3 positionWS, float3 positionOS)
            {
                float3 offset = float3(0, 0, 0);
                
                float verticalFactor = saturate(positionOS.y * 2.0);
                float stiffness = lerp(1.0, _BendStiffness, verticalFactor);
                
                for (int i = 0; i < _TramplerCount && i < 20; i++)
                {
                    float3 tramplerPos = _TramplerPositions[i].xyz;
                    float3 toTrampler = positionWS - tramplerPos;
                    
                    float horizontalDist = length(float2(toTrampler.x, toTrampler.z));
                    
                    float influence = saturate(1.0 - (horizontalDist / _TrampleRadius));
                    influence = influence * influence * influence;
                    
                    if (influence > 0.001)
                    {
                        float2 horizontalDir = normalize(float2(toTrampler.x, toTrampler.z));
                        
                        float strength = _TramplerStrengths[i];
                        float bendAmount = influence * strength * verticalFactor * stiffness;
                        
                        offset.x += horizontalDir.x * bendAmount * 0.6;
                        offset.y -= bendAmount * 1.2;
                        offset.z += horizontalDir.y * bendAmount * 0.6;
                    }
                }
                
                return offset;
            }

            float3 ApplyWind(float3 positionWS, float3 positionOS)
            {
                float verticalFactor = saturate(positionOS.y);
                
                float windPhase = (_GlobalTime * _WindSpeed) + (positionWS.x * 0.5) + (positionWS.z * 0.5);
                float wind = sin(windPhase) * _WindStrength;
                
                return float3(wind, 0, wind * 0.5) * verticalFactor;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                float3 trampleOffset = ApplyTrample(positionWS, input.positionOS.xyz);
                positionWS += trampleOffset;
                
                float trampleMagnitude = length(trampleOffset);
                float windInfluence = saturate(1.0 - trampleMagnitude * 2.0);
                float3 windOffset = ApplyWind(positionWS, input.positionOS.xyz) * windInfluence;
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
        
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, 0));
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
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

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
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