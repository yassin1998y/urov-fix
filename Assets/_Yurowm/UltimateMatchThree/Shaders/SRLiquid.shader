Shader "Custom/SR Liquid" {
		Properties {
		_Tile ("Tile (RGBA, UV1)", 2D) = "white" {}
		_TileAlpha ("Tile Alpha (A, UV2)", 2D) = "white" {}
		_Decal ("Decal (RGBA, UV2)", 2D) = "white" {}

		
		_ScaleA("Pattern Scale A", Float) = 1
		_OffsetA("Pattern Offset A", Float) = 1
		_ParallaxA("Parallax A", Float) = 0
		_ScaleB("Pattern Scale B", Float) = 1
		_OffsetB("Pattern Offset B", Float) = 1
		_ParallaxB("Parallax B", Float) = 0
		_ScaleC("Pattern Scale C", Float) = 1
		_OffsetC("Pattern Offset C", Float) = 1
		_ParallaxC("Parallax C", Float) = 0
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

		half _ScaleA, _OffsetA, _ParallaxA;
		half _ScaleB, _OffsetB, _ParallaxB;
		half _ScaleC, _OffsetC, _ParallaxC;

		struct Input {
			float2 uv_Tile : TEXCOORD0;
			float2 uv2_TileAlpha : TEXCOORD1;
			float2 uv2_Decal : TEXCOORD1;
			float4 screenPos;
		};

		fixed4 _Color;

		fixed4 getScreenSpaceTex(half offset, half scale, half parallax, fixed2 uv, half phase) {
			fixed2 result = uv;
			result.xy *= float2(1, _ScreenParams.y / _ScreenParams.x) * scale;
			float2 o = float2(1, _ScreenParams.y / _ScreenParams.x) * scale;

			result.x += (o.x * 2 - 1) * offset + cos(_Time.x) * parallax / 100;
			result.y += (o.y * 2 - 1) * offset + sin(_Time.x) * parallax / 100;

			return tex2D(_Tile, result);
		}

		void surf (Input IN, inout SurfaceOutput o) {
			fixed2 screenUV = IN.screenPos.xy / IN.screenPos.w;
			fixed4 tile = (getScreenSpaceTex(_OffsetA, _ScaleA, _ParallaxA, screenUV, 0) +
				getScreenSpaceTex(_OffsetB, _ScaleB, _ParallaxB, screenUV, .33) +
				getScreenSpaceTex(_OffsetC, _ScaleC, _ParallaxC, screenUV, .66)) / 3;

			o.Albedo = tile.rgb;
			o.Alpha = tile.a;

			if (IN.uv2_Decal.x >= 0) {
				tile.a *= tex2D(_TileAlpha, IN.uv2_TileAlpha).a;
				fixed4 decal = tex2D(_Decal, IN.uv2_Decal);
				o.Albedo = lerp(o.Albedo, decal.rgb, decal.a);
				o.Alpha = tile.a + (1 - tile.a) * decal.a;
			}
		}
		ENDCG
	}
	FallBack "Diffuse"
}


//
//
//    Properties {
//		_MainTex("Mask", 2D) = "white" {}
//		_Liquid("Liquid Texture", 2D) = "white" {}
//		_Color("Tint", Color) = (1,1,1,1)
//		_ScaleA("Pattern Scale A", Float) = 1
//		_OffsetA("Pattern Offset A", Float) = 1
//		_ParallaxA("Parallax A", Float) = 0
//		_ScaleB("Pattern Scale B", Float) = 1
//		_OffsetB("Pattern Offset B", Float) = 1
//		_ParallaxB("Parallax B", Float) = 0
//		_ScaleC("Pattern Scale C", Float) = 1
//		_OffsetC("Pattern Offset C", Float) = 1
//		_ParallaxC("Parallax C", Float) = 0
//
//		_StencilComp("Stencil Comparison", Float) = 8
//		_Stencil("Stencil ID", Float) = 0
//		_StencilOp("Stencil Operation", Float) = 0
//		_StencilWriteMask("Stencil Write Mask", Float) = 255
//		_StencilReadMask("Stencil Read Mask", Float) = 255
//
//		_ColorMask("Color Mask", Float) = 15
//
//		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
//    }
//
//    SubShader {
//        Tags {
//            "Queue"="Transparent"
//            "IgnoreProjector"="True"
//            "RenderType"="Transparent"
//            "PreviewType"="Plane"
//            "CanUseSpriteAtlas"="True"
//        }
//
//        Cull Off
//        Lighting Off
//        ZWrite Off
//        Blend One One
//		ColorMask[_ColorMask]
//
//		Stencil {
//			Ref [_Stencil]
//			Comp [_StencilComp]
//			Pass [_StencilOp] 
//			ReadMask [_StencilReadMask]
//			WriteMask [_StencilWriteMask]
//		}
//
//		CGPROGRAM
//		#pragma surface surf Lambert alpha
//        #pragma multi_compile UNITY_UI_ALPHACLIP
//
//		sampler2D _MainTex;
//		sampler2D _Liquid;
//
//		half _ScaleA, _OffsetA, _ParallaxA;
//		half _ScaleB, _OffsetB, _ParallaxB;
//		half _ScaleC, _OffsetC, _ParallaxC;
//		half4 _Color;
//
//		struct Input {
//			float2 uv_MainTex;
//			float4 screenPos;
//		};
//
//		fixed4 getScreenSpaceTex(half offset, half scale, half parallax, fixed2 mask, fixed2 uv, half phase) {
//			fixed2 result = uv;
//			result.xy *= float2(1, _ScreenParams.y / _ScreenParams.x) * scale;
//			float2 o = float2(1, _ScreenParams.y / _ScreenParams.x) * scale;
//
//			result.x += (o.x * 2 - 1) * mask.x * offset + cos(_Time.x) * parallax / 100;
//			result.y += (o.y * 2 - 1) * mask.y * offset + sin(_Time.x) * parallax / 100;
//
//			return tex2D(_Liquid, result);
//		}
//
//		void surf(Input IN, inout SurfaceOutput o) {
//			fixed2 screenUV = IN.screenPos.xy / IN.screenPos.w;
//			fixed4 mask = tex2D(_MainTex, IN.uv_MainTex);
//
//			fixed4 pattern = (getScreenSpaceTex(_OffsetA, _ScaleA, _ParallaxA, mask.xy, screenUV, 0) +
//						getScreenSpaceTex(_OffsetB, _ScaleB, _ParallaxB, mask.xy, screenUV, .33) +
//						getScreenSpaceTex(_OffsetC, _ScaleC, _ParallaxC, mask.xy, screenUV, .66)) / 3;
//
//			o.Albedo = 0;
//			o.Emission = pattern.rgb * _Color.rgb;
//			o.Alpha = pattern.a * mask.a * _Color.a;
//		
//		}
//		ENDCG
//        
//    }
//}