Shader "UI/Match-3/Color Progression"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

		_Color ("Main Color", Color) = (1,1,1,1)
			
		_FilledColor ("Filled Color", Color) = (1,1,1,1)
		_EmptyColor("Empty Color", Color) = (1,1,1,1)
		[MaterialToggle]  _Invert ("Invert", Int) = 0
					
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
					
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}
	
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType"="Plane"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}
		
		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
		
			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			//#pragma multi_compile __ UNITY_UI_ALPHACLIP

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float4 worldPosition : TEXCOORD2;
				fixed4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			fixed4 _Color;
			fixed4 _EmptyColor, _FilledColor;
			int _Invert;
			
			bool _UseClipRect;
			float4 _ClipRect;
				
			bool _UseAlphaClip;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.worldPosition = v.vertex;			
				o.vertex = UnityObjectToClipPos(o.worldPosition);

				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

				o.color = v.color * _Color;
				
				#ifdef UNITY_HALF_TEXEL_OFFSET
				o.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
				#endif

				return o;
			}
			
			fixed4 frag(v2f IN) : COLOR
			{
				fixed4 color = tex2D(_MainTex, IN.texcoord);
				fixed alpha = IN.color.a;
				bool filled = alpha >= 1 || alpha > color.r;
				if (_Invert == 1) filled = !filled;
				
				if (filled) {
					color.rgb = _FilledColor.rgb * IN.color.rgb;
					color.a *= _FilledColor.a;
				} else {
					color.rgb = _EmptyColor.rgb;
					color.a *= _EmptyColor.a;
				}
				
				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif
				
				return color;
			}
			ENDCG

		}
	}
}