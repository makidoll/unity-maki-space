Shader "Maki/Break Particle"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		[Toggle]
		_IsGrass ("Is Grass", Float) = 0
	}
//	SubShader
//	{
//		Tags { "RenderType"="Opaque" }
//		LOD 100
//
//		Pass
//		{
//			CGPROGRAM
//			#pragma vertex vert
//			#pragma fragment frag
//			// make fog work
//			#pragma multi_compile_fog
//			
//			#include "UnityCG.cginc"
//
//			struct appdata
//			{
//				float4 vertex : POSITION;
//				float2 uv : TEXCOORD0;
//				float4 color : COLOR;
//			};
//
//			struct v2f
//			{
//				float2 uv : TEXCOORD0;
//				UNITY_FOG_COORDS(1)
//				float4 vertex : SV_POSITION;
//				float4 color : COLOR;
//			};
//
//			sampler2D _MainTex;
//			float4 _MainTex_ST;
//
//			bool _IsGrass;
//			
//			v2f vert (appdata v)
//			{
//				v2f o;
//				o.vertex = UnityObjectToClipPos(v.vertex);
//				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
//				o.color = v.color;
//				UNITY_TRANSFER_FOG(o,o.vertex);
//				return o;
//			}
//			
//			fixed4 frag (v2f i) : SV_Target
//			{
//				// particle system will set color red randomly
//				// we'll split the texture into 4 x 4 squares
//				// and just randomly pick, assuming we're using 16 x 16 textures
//				
//				float2 uv = i.uv * 0.25;
//
//				// 0 to 15 ints
//				float randomA = floor(i.color * 16);
//				float randomB = floor(frac(sin(randomA * 529.148)) * 16);
//				uv += float2(randomA, randomB) / 16;
//
//				// sample the texture
//				fixed4 col = tex2D(_MainTex, uv);
//				
//				if (_IsGrass)
//				{
//					// randomly sampled from
//                    // https://minecraft.fandom.com/wiki/Color?file=Grasscolor.png#Grass
//                    col.rgb *= fixed3(129, 190, 92) / 255;
//				}
//				
//				// apply fog
//				UNITY_APPLY_FOG(i.fogCoord, col);
//				return col;
//			}
//			ENDCG
//		}
//	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        #include "GrassColor.cginc"
        
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _GrassColorMap;

        float _IsGrass;

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
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // particle system will set color red randomly
			// we'll split the texture into 4 x 4 squares
			// and just randomly pick, assuming we're using 16 x 16 textures
			
			float2 uv = IN.uv_MainTex * 0.25;

			// 0 to 15 ints
			float randomA = floor(IN.color * 16);
			float randomB = floor(frac(sin(randomA * 529.148)) * 16);
			uv += float2(randomA, randomB) / 16;

			// sample the texture
			fixed4 col = tex2D(_MainTex, uv);
			
			if (_IsGrass)
			{
				col.rgb *= GrassColor(IN.worldPos, _GrassColorMap);
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