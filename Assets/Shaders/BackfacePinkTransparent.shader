Shader "Custom/BackfacePinkTransparentURP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 0.3, 0.5, 0.5)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Cull Front // only render backfaces

        // Use URP shader library for correct transparency
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 _BaseColor;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(float4(IN.positionOS,1));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}