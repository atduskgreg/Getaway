Shader "Hidden/Line Of Sight Mask"
{
	CGINCLUDE

		#include "UnityCG.cginc"
		#include "LOSInclude.cginc"

		// Samplers
		uniform sampler2D _SourceDepthTex;
		uniform samplerCUBE _SourceDepthCube;
		uniform sampler2D _CameraDepthTexture;

		// For fast world space reconstruction
		uniform float4x4 _FrustumRays;
		uniform float4x4 _FrustumOrigins;
		uniform float4x4 _SourceWorldProj;

		uniform float4 _SourceInfo; // xyz = source position, w = source far plane
		uniform float4 _ColorMask;
		uniform float4 _Settings; // x = distance fade, y = edge fade, z = min variance, w = invert mask
		uniform float4 _Flags; // x = clamp out of bound pixels, y = include / exclude out of bound pixels
		uniform float4 _MainTex_TexelSize;

		struct VertexOut
		{
			float4 pos : POSITION;
			float2 uv : TEXCOORD0;
			float4 interpolatedRay : TEXCOORD1;
			float4 interpolatedOrigin : TEXCOORD2;
		};

		// Vertex Shader
		VertexOut Vert( appdata_img v )
		{
			VertexOut o;
			int index = v.vertex.z;
			v.vertex.z = 0.0f;

			o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
			o.uv = v.texcoord.xy;

	#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0)
				o.uv.y = 1-o.uv.y;
	#endif

			o.interpolatedRay = _FrustumRays[index];
			o.interpolatedRay.w = index;

			o.interpolatedOrigin = _FrustumOrigins[index];
			o.interpolatedOrigin.w = index;

			return o;
		}

		float CalculateVisibility(float4 pixelWorldPos)
		{
			// Calculate distance to source in range[0 - far plane]
			float sourceDistance = distance(pixelWorldPos.xyz, _SourceInfo.xyz);

			// Convert world space to LOS cam depth texture UV's
			float4 sourcePos = mul(_SourceWorldProj, pixelWorldPos);
			float3 sourceNDC = sourcePos.xyz / sourcePos.w;

			// Clip pixels outside of source
			clip(max(min(sourcePos.w, 1 - abs(sourceNDC.x)), _Settings.w - 0.5));

			// Convert from NDC to UV
			float2 sourceUV = sourceNDC.xy;
			sourceUV *= 0.5f;
			sourceUV += 0.5f;

			// VSM
			float2 moments = tex2D(_SourceDepthTex, sourceUV).rg;
			float visible = ChebyshevUpperBound(moments, sourceDistance, _Settings.z);

			// Handle vertical out of bound pixels
			visible += _Flags.x * _Flags.y * (1 - step(abs(sourceNDC.y), 1.0));
			visible = saturate(visible);

			// Ignore pixels behind source
			visible *= step(-sourcePos.w, 0);

			// Calculate fading
			float edgeFade = CalculateFade(abs(sourceNDC.x), _Settings.y);
			float distanceFade = CalculateFade(sourceDistance / _SourceInfo.w, _Settings.x);

			// Apply fading
			visible *= distanceFade;
			visible *= edgeFade;

			return visible;
		}

		float4 CalculateVisibilityCube(float4 pixelWorldPos)
		{
			// Calculate distance to source in range[0-1]
			float sourceDistance = distance(pixelWorldPos.xyz, _SourceInfo.xyz) / _SourceInfo.w;

			// Clip outside of source far plance, don't clip when inverted
			clip(max(1 - sourceDistance, _Settings.w - 0.5));

			// Calculate sample direction for cube map
			float3 sampleDirection = normalize(pixelWorldPos.xyz - _SourceInfo.xyz);

			// Sample encoded depth from cube map
			float4 encodedFloat = texCUBE(_SourceDepthCube, sampleDirection);

			// Decode depth
			float2 moments;
			moments.x = DecodeFloatRG(encodedFloat.xy);
			moments.y = DecodeFloatRG(encodedFloat.zw);

			// VSM
			float minVariance = _Settings.z / (_SourceInfo.w * _SourceInfo.w);
			float visible = ChebyshevUpperBound(moments, sourceDistance, minVariance);

			// Calculate fading
			float distanceFade = CalculateFade(sourceDistance, _Settings.x);

			// Apply Fading
			visible *= distanceFade;

			return visible;
		}

		float4 GenerateMask(float visible)
		{
			// Invert visibility if needed
			if(_Settings.w > 0.0)
			{
				visible = 1 - visible;
			}

			// Apply mask color
			float4 mainColor = visible * _ColorMask;

			return mainColor;
		}

		float4 DepthToWorldPosition(float depth, VertexOut i)
		{
			float4 viewRay = depth * i.interpolatedRay;
			float4 positionWorld = i.interpolatedOrigin + viewRay;
			positionWorld.w = 1;

			return positionWorld;
		}

		float4 PerspectiveDepthToWorldPosition(VertexOut i)
		{
			float depthSample = tex2D(_CameraDepthTexture,i.uv);
			float depth = UNITY_SAMPLE_DEPTH(depthSample);
			depth = LinearEyeDepth(depth);

			return DepthToWorldPosition(depth, i);
		}

		float4 OrthoGraphicDepthToWorldPosition(VertexOut i)
		{
			float depthSample = tex2D(_CameraDepthTexture,i.uv);
			float depth = UNITY_SAMPLE_DEPTH(depthSample);
			depth = depth * (_ProjectionParams.z - _ProjectionParams.y) + _ProjectionParams.y;

			return DepthToWorldPosition(depth, i);
		}

		// Fragment Shaders for different passes
		half4 FragPerspective (VertexOut i) : COLOR
		{
			float4 positionWorld = PerspectiveDepthToWorldPosition(i);
			float visible = CalculateVisibility(positionWorld);
			return GenerateMask(visible);
		}

		half4 FragOrtho (VertexOut i) : COLOR
		{
			float4 positionWorld = OrthoGraphicDepthToWorldPosition(i);
			float visible = CalculateVisibility(positionWorld);
			return GenerateMask(visible);
		}

		half4 fragPerspectiveCube (VertexOut i) : COLOR
		{
			float4 positionWorld = PerspectiveDepthToWorldPosition(i);
			float visible = CalculateVisibilityCube(positionWorld);
			return GenerateMask(visible);
		}

		half4 FragOrthoCube (VertexOut i) : COLOR
		{
			float4 positionWorld = OrthoGraphicDepthToWorldPosition(i);
			float visible = CalculateVisibilityCube(positionWorld);
			return GenerateMask(visible);
		}

	ENDCG

	SubShader
	{
		// Pass 0: Perspective
		Pass
		{
			ZTest Always Cull Off ZWrite Off Blend One One
			Fog { Mode off }

			CGPROGRAM

			#pragma vertex Vert
			#pragma fragment FragPerspective
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma exclude_renderers flash

			ENDCG
		}

		// Pass 1: Orthographic
		Pass
		{
			ZTest Always Cull Off ZWrite Off Blend One One
			Fog { Mode off }

			CGPROGRAM

			#pragma vertex Vert
			#pragma fragment FragOrtho
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma exclude_renderers flash

			ENDCG
		}

		// Pass 2: Perspective Cube
		Pass
		{
			ZTest Always Cull Off ZWrite Off Blend One One
			Fog { Mode off }

			CGPROGRAM

			#pragma vertex Vert
			#pragma fragment fragPerspectiveCube
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma exclude_renderers flash

			ENDCG
		}

		// Pass 3: Orthographic Cube
		Pass
		{
			ZTest Always Cull Off ZWrite Off Blend One One
			Fog { Mode off }

			CGPROGRAM

			#pragma vertex Vert
			#pragma fragment FragOrthoCube
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma exclude_renderers flash

			ENDCG
		}
	}

Fallback off

}