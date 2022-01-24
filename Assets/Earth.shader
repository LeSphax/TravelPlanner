// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Shadow stuff from https://alastaira.wordpress.com/2014/12/30/adding-shadows-to-a-unity-vertexfragment-shader-in-7-easy-steps/

Shader "Custom/Earth" {
	Properties
	{
        _MainTex ("Albedo", 2D) = "white" {}
        _HeightMap ("Height Map", 2D) = "white" {}
	}
	SubShader {
		Pass {
		
			// 1.) This will be the base forward rendering pass in which ambient, vertex, and
			// main directional light will be applied. Additional lights will need additional passes
			// using the "ForwardAdd" lightmode.
			// see: http://docs.unity3d.com/Manual/SL-PassTags.html
			Tags { "LightMode" = "ForwardBase" }
		
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			// 2.) This matches the "forward base" of the LightMode tag to ensure the shader compiles
			// properly for the forward bass pass. As with the LightMode tag, for any additional lights
			// this would be changed from _fwdbase to _fwdadd.
			#pragma multi_compile_fwdbase

			// Data
			sampler2D _MainTex;
			sampler2D _HeightMap;

			static const float PI = 3.14159;

			struct appdata
			{
				float4 vertex : POSITION;
				float4 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};


			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD1;
			};


			v2f vert(appdata v) {
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = v.uv;
				
				return o;
			}

			float4 frag(v2f i) : COLOR
			{
				// Calculate latitude and longitude
				float3 p = i.uv.xyz;
				float latitude_rad = asin(p.y);
				float longitude_rad = atan2(p.x, -p.z);
				float latitudeT = latitude_rad / PI + 0.5;
				float longitudeT = 0.5 + (longitude_rad / PI) / 2;
				float2 coordinate = float2(longitudeT, latitudeT);

				// Sample textures
				float4 albedo = tex2D(_MainTex, coordinate);
				float4 height = tex2D(_HeightMap, coordinate);
				// if (height.r > 0.8) return (0, 0, 0); 
				return albedo * height.r;
			}

			ENDCG
		}
	}
	
	// 7.) To receive or cast a shadow, shaders must implement the appropriate "Shadow Collector" or "Shadow Caster" pass.
	// Although we haven't explicitly done so in this shader, if these passes are missing they will be read from a fallback
	// shader instead, so specify one here to import the collector/caster passes used in that fallback.
	Fallback "VertexLit"
}