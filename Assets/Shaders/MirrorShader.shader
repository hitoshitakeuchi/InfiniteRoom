// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/MirrorShader"
{
	Properties
	{
	_MainTex("Base (RGB)", 2D) = "white" {}
	[HideInInspector] _ReflectionTex("", 2D) = "white" {}
	_Alpha("Alpha", Range(0, 1)) = 1
	}
		SubShader
	{
		ZWrite Off
		Tags{ "RenderType" = "Transparent" }
		LOD 100
		Pass{

		Blend SrcAlpha OneMinusSrcAlpha


		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
		struct v2f
	{
		float2 uv : TEXCOORD0;
		float4 refl : TEXCOORD1;
		float4 pos : SV_POSITION;
	};
	float4 _MainTex_ST;
	float _Alpha;
	v2f vert(float4 pos : POSITION, float2 uv : TEXCOORD0)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(pos);
		o.uv = TRANSFORM_TEX(uv, _MainTex);
		o.refl = ComputeScreenPos(o.pos);
		return o;
	}
	sampler2D _MainTex;
	sampler2D _ReflectionTex;
	fixed4 frag(v2f i) : SV_Target
	{
		float2 uvMirrored = float2(1 - i.uv.x, i.uv.y);
		fixed4 tex = tex2D(_MainTex, uvMirrored);
		fixed4 refl = tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(i.refl));
		return tex * refl *_Alpha;
	}
		ENDCG
	}
	}
}