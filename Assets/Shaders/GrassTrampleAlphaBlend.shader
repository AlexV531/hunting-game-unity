Shader "Custom/GrassTrampleAlphaBlend"
{
    Properties
    {
        _MainTex("Grass Texture", 2D) = "white" {}
        _Color("Tint Color", Color) = (1,1,1,1)
        _AlphaMultiplier("Alpha Multiplier", Range(0,2)) = 1.0

        _WindSpeed("Wind Speed", Float) = 1.0
        _WindStrength("Wind Strength", Float) = 0.1
        
        _AmbientStrength("Ambient Strength", Range(0,1)) = 0.3
        _ShadowStrength("Shadow Strength", Range(0,1)) = 0.7
        _NormalInfluence("Normal Lighting Influence", Range(0,1)) = 0.5
        _MinLighting("Minimum Lighting", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Cull Off

        // ===== Forward Pass with Transparency =====
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual

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
                float _AlphaMultiplier;
                float _WindSpeed;
                float _WindStrength;
                float _AmbientStrength;
                float _ShadowStrength;
                float _NormalInfluence;
                float _MinLighting;
            CBUFFER_END
            
            // Global trample map - MUST be declared OUTSIDE CBuffer
            TEXTURE2D(_TrampleMap);
            SAMPLER(sampler_TrampleMap);

            // Global properties set by GrassTrampleSystem
            float4 _GridWorldMin;
            float4 _GridWorldSize;
            float _GlobalTime;

            float3 ApplyTrample(float3 worldPos, float vertexHeight)
            {
                // Convert world XZ to UV coordinates
                float2 uv = (worldPos.xz - _GridWorldMin.xz) / _GridWorldSize.xz;
                uv = saturate(uv);
                
                // Sample trample map (R = strength, GB = direction)
                float4 trampleData = SAMPLE_TEXTURE2D_LOD(_TrampleMap, sampler_TrampleMap, uv, 0);
                float strength = trampleData.r;
                float2 dirEncoded = trampleData.gb;
                float2 direction = dirEncoded * 2.0 - 1.0;
                
                // Height factor: squared for more natural bend (bottom stays fixed, top moves most)
                float heightFactor = vertexHeight * vertexHeight;
                
                // Apply offset
                float3 offset;
                offset.x = direction.x * strength * 0.6 * heightFactor;
                offset.y = -strength * 1.2 * heightFactor;
                offset.z = direction.y * strength * 0.6 * heightFactor;
                
                return offset;
            }

            float3 CalculateWindOffset(float3 worldPos, float vertexHeight)
            {
                // Use multiple octaves for smoother, less flickery motion
                float phase1 = (_GlobalTime * _WindSpeed * 0.8) + (worldPos.x + worldPos.z) * 0.3;
                float phase2 = (_GlobalTime * _WindSpeed * 0.4) + (worldPos.x * 0.7 + worldPos.z * 1.3) * 0.2;
                
                float wind = (sin(phase1) + sin(phase2) * 0.5) * _WindStrength * 0.5;
                
                float heightFactor = vertexHeight * vertexHeight;
                return float3(wind, 0, wind * 0.5) * heightFactor;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // Use UV.y as height factor (0 at bottom, 1 at top of grass blade)
                float vertexHeight = input.uv.y;

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                posWS += ApplyTrample(posWS, vertexHeight);
                posWS += CalculateWindOffset(posWS, vertexHeight);

                output.positionCS = TransformWorldToHClip(posWS);
                output.positionWS = posWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.color = input.color;
                
                // Calculate shadow coordinates
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = posWS;
                vertexInput.positionCS = output.positionCS;
                
                output.shadowCoord = GetShadowCoord(vertexInput);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                albedo *= _Color * input.color;
                
                // Distance-based alpha handling
                float distToCamera = length(input.positionWS - _WorldSpaceCameraPos);
                half alpha = albedo.a * _AlphaMultiplier;
                
                // Close range: use hard cutoff to prevent see-through
                // Far range: use soft alpha for AA
                float cutoffRange = 10.0; // Distance where we switch methods
                float blendFactor = saturate((distToCamera - 5.0) / cutoffRange);
                
                if (blendFactor < 0.5) {
                    // Close up: hard cutoff
                    clip(alpha - 0.5);
                    alpha = 1.0;
                } else {
                    // Distance: soft alpha
                    if (alpha < 0.01) discard;
                }

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
                
                return half4(color, alpha);
            }
            ENDHLSL
        }

        // ===== Depth Only Pass (for depth pre-pass if needed) =====
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
            float _AlphaMultiplier;

            float4 _GridWorldMin;
            float4 _GridWorldSize;
            float _GlobalTime;
            float _WindSpeed;
            float _WindStrength;

            float3 ApplyTrampleDepth(float3 worldPos, float vertexHeight)
            {
                float2 uv = (worldPos.xz - _GridWorldMin.xz) / _GridWorldSize.xz;
                uv = saturate(uv);
                
                float4 trampleData = SAMPLE_TEXTURE2D_LOD(_TrampleMap, sampler_TrampleMap, uv, 0);
                float strength = trampleData.r;
                float2 dirEncoded = trampleData.gb;
                float2 direction = dirEncoded * 2.0 - 1.0;
                
                float heightFactor = vertexHeight * vertexHeight;
                
                float3 offset;
                offset.x = direction.x * strength * 0.6 * heightFactor;
                offset.y = -strength * 1.2 * heightFactor;
                offset.z = direction.y * strength * 0.6 * heightFactor;
                
                return offset;
            }

            float3 CalculateWindOffsetDepth(float3 worldPos, float vertexHeight)
            {
                float phase1 = (_GlobalTime * _WindSpeed * 0.8) + (worldPos.x + worldPos.z) * 0.3;
                float phase2 = (_GlobalTime * _WindSpeed * 0.4) + (worldPos.x * 0.7 + worldPos.z * 1.3) * 0.2;
                
                float wind = (sin(phase1) + sin(phase2) * 0.5) * _WindStrength * 0.5;
                
                float heightFactor = vertexHeight * vertexHeight;
                return float3(wind, 0, wind * 0.5) * heightFactor;
            }

            Varyings vertDepth(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                float vertexHeight = input.uv.y;

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                posWS += ApplyTrampleDepth(posWS, vertexHeight);
                posWS += CalculateWindOffsetDepth(posWS, vertexHeight);

                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 fragDepth(Varyings input) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _AlphaMultiplier;
                if (alpha < 0.01) discard;
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
