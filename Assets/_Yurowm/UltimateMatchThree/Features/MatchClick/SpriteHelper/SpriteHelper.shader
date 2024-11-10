// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Sprites/Diffuse Helper"
{
    Properties {
		[PerRendererData] _MainTex("Mask", 2D) = "white" {}
	    _Tex ("Sprite Texture", 2D) = "black" {}
		_Color("Tint", Color) = (1,1,1,1)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader {
        Tags {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One
		ColorMask[_ColorMask]

		Stencil {
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		CGPROGRAM
		#pragma surface surf Lambert alpha vertex:vert 
        #pragma multi_compile UNITY_UI_ALPHACLIP

		sampler2D _MainTex;
		sampler2D _Tex;

		uniform half2 _Size = half2(1, 1);

		half4 _Color;

		struct Input {
			float2 uv_MainTex;
			float2 helper;
			float4 color : COLOR;
		};

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.helper = v.vertex.xy / _Size + 0.5;
		}

		void surf(Input IN, inout SurfaceOutput o) {
			fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
			fixed4 helper = tex2D(_Tex, IN.helper) * _Color;

			o.Albedo = 0;
			o.Emission = tex.rgb * IN.color.rgb;
			o.Emission = lerp(o.Emission, helper.rgb, helper.a);

			o.Alpha = tex.a * IN.color.a;
		}
		ENDCG
        
    }
}