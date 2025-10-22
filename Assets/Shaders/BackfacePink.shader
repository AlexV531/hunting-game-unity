Shader "Custom/URP_BackfacePinkMaterial"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (255,0,255,255)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "BackfaceUnlit"
            Tags { "LightMode"="UniversalForward" }
            Cull Front // Only render backfaces

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                return color;
            }
            ENDHLSL
        }
    }
}
