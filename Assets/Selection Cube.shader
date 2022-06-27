Shader "Maki/Selection Cube"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 localPosition : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.localPosition = v.vertex;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float offset = 0.125 / 4;
                
                if (i.localPosition.x > -0.5 + offset && i.localPosition.x < 0.5 - offset && i.localPosition.z > -0.5 + offset && i.localPosition.z < 0.5 - offset) discard;
                if (i.localPosition.x > -0.5 + offset && i.localPosition.x < 0.5 - offset && i.localPosition.y > -0.5 + offset && i.localPosition.y < 0.5 - offset) discard;
                if (i.localPosition.y > -0.5 + offset && i.localPosition.y < 0.5 - offset && i.localPosition.z > -0.5 + offset && i.localPosition.z < 0.5 - offset) discard;
                    
                fixed4 col = fixed4(0,0,0,0.2);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
