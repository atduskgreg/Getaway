Shader "Hidden/Line Of Sight Combiner"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "black" {}
	}

	SubShader
	{
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off

			Fog { Mode off }

			CGPROGRAM

			#pragma vertex vert_img
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform sampler2D _PreEffectTex;
			uniform sampler2D _MaskTex;

			fixed4 frag (v2f_img i) : COLOR
			{
				float4 postEffectColor = tex2D(_MainTex, i.uv);
				float4 preEffectColor = tex2D(_PreEffectTex, i.uv);
				float4 mask = tex2D(_MaskTex, i.uv);

				float4 finalColor = lerp(postEffectColor, preEffectColor * mask, mask.a);

				return finalColor;
			}

			ENDCG
		}
	}

	Fallback off
}