Shader "Sprites/Diffuse Screen Space"
{
    Properties {
		[PerRendererData] _MainTex("Mask", 2D) = "white" {}
	    _Tex ("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_Scale("Pattern Scale", Float) = 1
		_Offset("Pattern Offset", Float) = 1
		_Parallax("Parallax", Float) = 0

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
		#pragma surface surf Lambert alpha
        #pragma multi_compile UNITY_UI_ALPHACLIP

		sampler2D _MainTex;
		sampler2D _Tex;

		half _Scale, _Offset, _Parallax;
		half4 _Color;

		struct Input {
			float2 uv_MainTex;
			float4 screenPos;
			float4 color : COLOR;
		};

		void surf(Input IN, inout SurfaceOutput o) {
			float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
			screenUV.xy *= float2(1, _ScreenParams.y / _ScreenParams.x) * _Scale;
			float2 offset = float2(1, _ScreenParams.y / _ScreenParams.x) * _Scale;
			fixed4 mask = tex2D(_MainTex, IN.uv_MainTex);

			screenUV.x += (offset.x * 2 - 1) * mask.r * _Offset + cos(_Time.x) * _Parallax / 100;
			screenUV.y += (offset.y * 2 - 1) * mask.g * _Offset + sin(_Time.x) * _Parallax / 100;
			
			fixed4 pattern = tex2D(_Tex, screenUV);

			o.Albedo = 0;
			o.Emission = pattern.rgb * _Color.rgb * IN.color.rgb;
			o.Alpha = pattern.a * mask.a * _Color.a * IN.color.a;
		}
		ENDCG
        
    }
}