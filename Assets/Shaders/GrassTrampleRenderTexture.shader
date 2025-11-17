Shader "Custom/GrassTrampleRenderTexture"
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
        
        [Header(Shadow Bias Fix)]
        _CustomShadowBias("Shadow Depth Bias", Range(0,10)) = 1.0
        _CustomShadowNormalBias("Shadow Normal Bias", Range(0,10)) = 1.5
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
                float4 shadowCoord : TEXCOORD4;
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
                float _CustomShadowBias;
                float _CustomShadowNormalBias;
            CBUFFER_END
            
            // Global trample map - MUST be declared OUTSIDE CBuffer
            TEXTURE2D(_TrampleMap);
            SAMPLER(sampler_TrampleMap);

            // Global properties set by GrassTrampleSystem
            float4 _GridWorldMin;
            float4 _GridWorldSize;
            float _GlobalTime;

            float3 ApplyTrample(float3 worldPos)
            {
                // Convert world XZ to UV coordinates
                float2 uv = (worldPos.xz - _GridWorldMin.xz) / _GridWorldSize.xz;
                
                // Clamp UV to valid range
                uv = saturate(uv);
                
                // Sample trample map (R = strength, GB = direction)
                // Use LOD 0 to ensure we sample the texture properly
                float4 trampleData = SAMPLE_TEXTURE2D_LOD(_TrampleMap, sampler_TrampleMap, uv, 0);
                float strength = trampleData.r;
                float2 dirEncoded = trampleData.gb;
                
                // Decode direction from 0-1 to -1 to 1
                float2 direction = dirEncoded * 2.0 - 1.0;
                
                // Apply offset based on vertex height (only affect top of grass)
                float heightFactor = saturate(worldPos.y * 0.1); // Adjust multiplier as needed
                
                // Apply offset
                float3 offset;
                offset.x = direction.x * strength * 0.6 * heightFactor;
                offset.y = -strength * 1.2 * heightFactor;
                offset.z = direction.y * strength * 0.6 * heightFactor;
                
                return offset;
            }

            float3 ApplyWind(float3 worldPos)
            {
                float phase = (_GlobalTime * _WindSpeed) + (worldPos.x + worldPos.z) * 0.5;
                float wind = sin(phase) * _WindStrength;
                return float3(wind, 0, wind * 0.5);
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
                
                // Calculate shadow coordinates with custom bias
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = posWS;
                vertexInput.positionCS = output.positionCS;
                
                float3 normalWS = output.normalWS;
                float4 shadowCoord = GetShadowCoord(vertexInput);
                
                // Apply custom bias to reduce shadow acne
                shadowCoord.xyz += normalWS * _CustomShadowNormalBias * 0.001;
                shadowCoord.z -= _CustomShadowBias * 0.0001;
                
                output.shadowCoord = shadowCoord;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                albedo *= _Color * input.color;
                clip(albedo.a - _Cutoff);

                // DEBUG: Visualize trample map sampling
                float2 trampleUV = (input.positionWS.xz - _GridWorldMin.xz) / _GridWorldSize.xz;
                float4 trampleDebug = SAMPLE_TEXTURE2D_LOD(_TrampleMap, sampler_TrampleMap, trampleUV, 0);

                // Get main light with shadow attenuation
                float4 shadowCoord = input.shadowCoord;
                Light mainLight = GetMainLight(shadowCoord);
                
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                
                // Soften the normal-based lighting with MinLighting floor
                NdotL = lerp(_MinLighting, 1.0, NdotL);
                
                // Control how much the normal affects lighting
                float normalLighting = lerp(1.0, NdotL, _NormalInfluence);

                // Apply shadow strength
                float shadow = mainLight.shadowAttenuation;
                shadow = lerp(1.0 - _ShadowStrength, 1.0, shadow);
                
                // Ambient and direct lighting
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
            
            TEXTURE2D(_TrampleMap);
            SAMPLER(sampler_TrampleMap);
            
            float4 _MainTex_ST;
            float _Cutoff;
            float _CustomShadowBias;
            float _CustomShadowNormalBias;
            
            // Global properties
            float4 _GridWorldMin;
            float4 _GridWorldSize;
            float _GlobalTime;
            float _WindSpeed;
            float _WindStrength;

            float3 ApplyTrampleShadow(float3 worldPos)
            {
                float2 uv = (worldPos.xz - _GridWorldMin.xz) / _GridWorldSize.xz;
                uv = saturate(uv);
                
                float4 trampleData = SAMPLE_TEXTURE2D_LOD(_TrampleMap, sampler_TrampleMap, uv, 0);
                float strength = trampleData.r;
                float2 dirEncoded = trampleData.gb;
                float2 direction = dirEncoded * 2.0 - 1.0;
                
                float3 offset;
                offset.x = direction.x * strength * 0.6;
                offset.y = -strength * 1.2;
                offset.z = direction.y * strength * 0.6;
                
                return offset;
            }

            float3 ApplyWindShadow(float3 worldPos)
            {
                float phase = (_GlobalTime * _WindSpeed) + (worldPos.x + worldPos.z) * 0.5;
                float wind = sin(phase) * _WindStrength;
                return float3(wind, 0, wind * 0.5);
            }

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                posWS += ApplyTrampleShadow(posWS);
                posWS += ApplyWindShadow(posWS);
                
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Apply custom shadow bias
                float3 lightDir = _MainLightPosition.xyz;
                posWS = ApplyShadowBias(posWS, normalWS, lightDir);
                
                // Add additional custom bias
                posWS += normalWS * _CustomShadowNormalBias * 0.001;
                
                float4 positionCS = TransformWorldToHClip(posWS);
                
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
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ===== Depth Only Pass =====
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
            
            TEXTURE2D(_TrampleMap);
            SAMPLER(sampler_TrampleMap);
            
            float4 _MainTex_ST;
            float _Cutoff;

            float4 _GridWorldMin;
            float4 _GridWorldSize;
            float _GlobalTime;
            float _WindSpeed;
            float _WindStrength;

            float3 ApplyTrampleDepth(float3 worldPos)
            {
                float2 uv = (worldPos.xz - _GridWorldMin.xz) / _GridWorldSize.xz;
                uv = saturate(uv);
                
                float4 trampleData = SAMPLE_TEXTURE2D_LOD(_TrampleMap, sampler_TrampleMap, uv, 0);
                float strength = trampleData.r;
                float2 dirEncoded = trampleData.gb;
                float2 direction = dirEncoded * 2.0 - 1.0;
                
                float3 offset;
                offset.x = direction.x * strength * 0.6;
                offset.y = -strength * 1.2;
                offset.z = direction.y * strength * 0.6;
                
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
