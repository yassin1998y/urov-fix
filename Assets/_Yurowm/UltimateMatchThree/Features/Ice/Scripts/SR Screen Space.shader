Shader "Custom/SR Screen Space" {
	Properties {
		_Tex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_Scale("Pattern Scale", Float) = 1
		_Offset("Pattern Offset", Float) = 1
		_Parallax("Parallax", Float) = 0

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

		sampler2D _MainTex;
		sampler2D _Tex;

		half _Scale, _Offset, _Parallax;
		half4 _Color;

		struct Input {
			float2 uv_Tile : TEXCOORD0;
			float2 uv2_TileAlpha : TEXCOORD1;
			float2 uv2_Decal : TEXCOORD1;
			float4 screenPos;
			float4 color : COLOR;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 tile = tex2D(_Tile, IN.uv_Tile) * _TileColor;
			half4 mask = tile;

			if (IN.uv2_Decal.x >= 0) {
				tile.a *= tex2D(_TileAlpha, IN.uv2_TileAlpha).a;
				fixed4 decal = tex2D(_Decal, IN.uv2_Decal);
				mask.rgb = lerp(mask, decal.rgb, decal.a);
				mask.a = tile.a + (1 - tile.a) * decal.a;
			}
			mask *= _MainColor;

			float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
			screenUV.xy *= float2(1, _ScreenParams.y / _ScreenParams.x) * _Scale;
			float2 offset = float2(1, _ScreenParams.y / _ScreenParams.x) * _Scale;

			screenUV.x += (offset.x * 2 - 1) * mask.r * _Offset + cos(_Time.x) * _Parallax / 100;
			screenUV.y += (offset.y * 2 - 1) * mask.g * _Offset + sin(_Time.x) * _Parallax / 100;

			fixed4 pattern = tex2D(_Tex, screenUV);

			o.Albedo = 0;
			o.Emission = pattern.rgb * _Color.rgb * IN.color.rgb;
			o.Alpha = pattern.a * mask.a * _Color.a * IN.color.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

