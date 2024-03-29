Shader "Maki/Block"
{
    Properties
    {
        _MainTex ("Main Tex", 2D) = "white" {}
        _GrassColorMap ("Grass Color Map", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        #include "GrassColor.cginc"

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _GrassColorMap;

        struct Input
        {
            float2 uv_MainTex;
            float4 color : COLOR;
            float3 worldPos;
        };

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 col = tex2D(_MainTex, IN.uv_MainTex);

            // must be grass
            if (IN.color.r < 0.5 && IN.color.g > 0.5 && IN.color.b < 0.5)
            {
                const float distance = sqrt(pow(col.r - col.g, 2) + pow(col.g - col.b, 2) + pow(col.b - col.r, 2));
                if (distance < 0.1)
                {
                    col.rgb *= GrassColor(IN.worldPos, _GrassColorMap);
                }
            }

            o.Albedo = col.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = 0;
            o.Smoothness = 0;
            o.Alpha = col.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}