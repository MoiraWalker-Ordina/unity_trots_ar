// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "CubeWater/CubeWave"
{
	Properties
	{
		_WaterDepth("WaterDepth", float) = 1
		[HideInInspector]
		_Kd("water scattering kd", Vector) = (.3867, .1055, .0469, 0)
		_Attenuation("_Attenuation", Vector) = (.45, .1718, .1133, 0)
		_DiffuseRadiance("Water Diffuse", color) = (.0338, .1015, .2109, 0)
		_RefractOffset("RefractOffset", float) = 0.1
		_ReflectOffset("ReflectOffset", float) = 0.1
		_FresnelFactor("FresnelFactor", float) = 0.5
		_SpecularFactor("specular x:intensity y:spread z:cutout", Vector) = (64, 0, 0.96, 0)
		_SpecularColor("_SpecularColor", color) = (1,1,1,1)
		//
		_BumpTex("Bump Texture", 2D) = "white"{}
		_BumpStrength("Bump strength", Range(0.0, 10.0)) = 1.0
		_BumpDirection("Bump direction(2 wave)", Vector) = (0.01,0.01,0.02,-0.02)
		_BumpTiling("Bump tiling", Vector) = (0.01,0.01,0.013,0.013)
			//
		_Skybox("Skybox", CUBE) = "white"{}
	}
	SubShader
	{
		Tags{ "RenderType" = "Transparent" "LightMode" = "ForwardBase" "Queue"="Transparent"}
		LOD 100
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			#include "CubeWaterTool.cginc"
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float3 screenPos:TEXCOORD0;
				float3 objPos:TEXCOORD1;
				float3 worldPos:TEXCOORD2;
				float4 bumpCoords:TEXCOORD3;
				UNITY_FOG_COORDS(4)
				float4 vertex : SV_POSITION;
			};

			float4 _QA;
			float4 _A;
			float4 _S;
			float4 _Dx;
			float4 _Dz;
			float4 _L;
			float _WaterDepth;
			float3 _BoxSize;
			float3 _Kd;
			float3 _Attenuation;
			float3 _DiffuseRadiance;
			float _RefractOffset;
			float _FresnelFactor;
			float _ReflectOffset;
			float3 _SpecularFactor;
			float4 _SpecularColor;
			//
			sampler2D _BumpTex;
			half _BumpStrength;
			half4 _BumpDirection;
			half4 _BumpTiling;
			//
			sampler2D _RefractionTex;
			sampler2D _ReflectionTex;
			samplerCUBE _Skybox;
		
			v2f vert(appdata v)
			{
				v2f o;
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 worldNor;
			
				float3 disPos = CalculateWavesDisplacement(worldPos, _QA, _A, _S, _Dx, _Dz, _L);
				v.vertex.xyz = mul(unity_WorldToObject, float4(worldPos + disPos, 1));
				o.vertex = mul(UNITY_MATRIX_VP, float4(worldPos + disPos, 1));//UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex).xyw;
				o.objPos = v.vertex.xyz;
				o.worldPos = worldPos + disPos;
				o.bumpCoords.xyzw = (o.worldPos.xzxz * _BumpTiling.xyzw + _Time.yyyy * _BumpDirection.xyzw);
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				// sample the texture
				float4 col = float4(0, 0, 0, 1);
				half3 pixelNormal = normalize(PerPixelNormal(_BumpTex, i.bumpCoords, _BumpStrength));
				float3 binormal = float3(0, 0, 0);
				float3 tangent = float3(0, 0, 0);
				CalculateWavesBinormalTangent(i.worldPos.xyz, _QA, _A, _S, _Dx, _Dz, _L, binormal, tangent);
				float3 worldNormal = normalize(cross(tangent, binormal));
				float3x3 Mat = { binormal, worldNormal, tangent };//from world coord to tangent coord
				Mat = transpose(Mat);
				worldNormal = normalize(mul(Mat, normalize(pixelNormal)));
				float3 objNormal = mul((float3x3)unity_WorldToObject, normalize(worldNormal));
		
				float3 objCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1));
				float3 viewVector = normalize(i.objPos - objCameraPos);
				float3 nNormal, fNormal;
				float2 d = BoxIntersection(objCameraPos.xyz, (viewVector), _BoxSize, nNormal, fNormal);
				float3 near = objCameraPos.xyz + (viewVector) * d.x;
				float3 far = objCameraPos.xyz + (viewVector) * d.y;
				float len = length(far - near);

				float3 outScattering;
				float3 inScattering;
				ComputeScattering(len * _WaterDepth, _DiffuseRadiance, _Attenuation, _Kd, outScattering, inScattering);

				float2 projUV = i.screenPos.xy / i.screenPos.z;
				//float3 refrV = refract(viewVector, objNormal, 1.33f);
				float3 refrV = refract((viewVector), fNormal, 1 / 1.33f);

				float4 refractColorRefr = tex2D(_RefractionTex, projUV + worldNormal *_RefractOffset);
				float4 refractColorRefl = texCUBE(_Skybox, refrV + worldNormal*_RefractOffset);
				float3 refractColor = lerp(refractColorRefl.xyz, refractColorRefr.xyz, refractColorRefr.a);

				col.xyz = refractColor.xyz * outScattering + inScattering;

				/*float fresnel = (1.0f - dot(-normalize(viewVector), objNormal));
				fresnel = _FresnelFactor + (1 - _FresnelFactor)*fresnel;*/
				float fresnel = FastFresnel((-viewVector), objNormal, _FresnelFactor);

				float4 reflectColor = tex2D(_ReflectionTex, projUV + worldNormal.xz*_ReflectOffset);
				col.xyz = lerp(col.xyz, reflectColor.xyz, fresnel);

				worldNormal = normalize(worldNormal + half3(0, _SpecularFactor.y, 0));
				objNormal = mul((float3x3)unity_WorldToObject, normalize(worldNormal));
				float3 halfVector = normalize(mul((float3x3)unity_WorldToObject, normalize(_WorldSpaceLightPos0.xyz))) - normalize(viewVector);
				float spec = pow(max(0, dot(objNormal, normalize(halfVector))), _SpecularFactor.x) * _SpecularColor.w;
				spec = step(_SpecularFactor.z, spec);
				col.xyz += _SpecularColor.xyz * spec;
				UNITY_APPLY_FOG(i.fogCoord, col);

				return col;
			}
			ENDCG
		}

	}
}
