Shader "Custom/SR" {
	Properties {
		_MainColor ("Color (RGBA)", Color) = (1,1,1,1)
		_Tile ("Tile (RGBA, UV1)", 2D) = "white" {}
		_TileColor ("Tile Color (RGBA, UV1)", Color) = (1,1,1,1)
		_TileAlpha ("Tile Alpha (A, UV2)", 2D) = "white" {}
		_Decal ("Decal (RGBA, UV2)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf NoLighting alpha

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) {
			fixed4 c;
			c.rgb = s.Albedo;
			c.a = s.Alpha;
			return c;
		}

		sampler2D _Tile;
		sampler2D _Decal;
		sampler2D _TileAlpha;
		half4 _TileColor;
		half4 _MainColor;

		struct Input {
			float2 uv_Tile : TEXCOORD0;
			float2 uv2_TileAlpha : TEXCOORD1;
			float2 uv2_Decal : TEXCOORD1;
		};

		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 tile = tex2D(_Tile, IN.uv_Tile) * _TileColor;
			o.Albedo = tile.rgb;
			o.Alpha = tile.a;

			if (IN.uv2_Decal.x >= 0) {
				tile.a *= tex2D(_TileAlpha, IN.uv2_TileAlpha).a;
				fixed4 decal = tex2D(_Decal, IN.uv2_Decal);
				o.Albedo = lerp(o.Albedo, decal.rgb, decal.a);
				o.Alpha = tile.a + (1 - tile.a) * decal.a;
			}
			o.Albedo *= _MainColor.rgb;
			o.Alpha *= _MainColor.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
