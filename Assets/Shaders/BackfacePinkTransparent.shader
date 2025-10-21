Shader "Custom/BackfacePinkTransparentURP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 0.3, 0.5, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Cull Front // Only render backfaces
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
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
                OUT.positionCS = TransformObjectToHClip(float4(IN.positionOS, 1));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Return solid pink, unaffected by lighting
                return _BaseColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
