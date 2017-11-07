Shader "Custom/Additive" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Amp ("Amp", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		ZTest Always ZWrite Off Cull Off Fog { Mode Off }
		Blend One One

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float _Amp;

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			
			appdata vert(appdata IN) {
				appdata OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.uv = IN.uv;
				return OUT;
			}
			fixed4 frag(appdata IN) : COLOR {
				fixed4 c = tex2D(_MainTex, IN.uv);
				return _Amp * c;
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}
