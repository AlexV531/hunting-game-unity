Shader "Hidden/GrassTrampleStamp"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _StampCenter; // xy = UV position
            float _StampRadius;  // In UV space
            float _StampStrength;
            float4 _StampDirection; // xy = encoded direction (0-1)

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Read existing value
                fixed4 existing = tex2D(_MainTex, i.uv);
                
                // Calculate distance from stamp center
                float2 delta = i.uv - _StampCenter.xy;
                float dist = length(delta);
                
                // Circular falloff
                float influence = saturate(1.0 - dist / _StampRadius);
                influence = influence * influence * influence; // Smooth cubic falloff
                
                // Create new stamp value
                float newStrength = influence * _StampStrength;
                
                // Max blend with existing (keep stronger value)
                float finalStrength = max(existing.r, newStrength);
                
                // Update direction only where we're stamping
                float2 finalDirection = existing.gb;
                if (newStrength > existing.r)
                {
                    finalDirection = _StampDirection.xy;
                }
                
                return fixed4(finalStrength, finalDirection.x, finalDirection.y, 1);
            }
            ENDCG
        }
    }
}
