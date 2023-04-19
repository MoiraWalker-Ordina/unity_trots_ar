// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "CubeWater/CubeWaveDown"
{
	Properties
	{
		_WaterDepth("HeightAtten", float) = 1
		_Kd("water scattering kd", Vector) = (.3867, .1055, .0469, 0)
		_Attenuation("_Attenuation", Vector) = (.45, .1718, .1133, 0)
		_DiffuseRadiance("Water Diffuse", color) = (.0338, .1015, .2109, 0)
		_RefractOffset("RefractOffset", float) = 0.1
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
		Tags{ "RenderType" = "Transparent" "LightMode" = "ForwardBase" "Queue" = "Transparent"}
		LOD 100

		Pass
		{
			Stencil
			{
				Ref 2
				Comp Always
				Pass Replace
			}
			cull front
			
			
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
			//
			sampler2D _BumpTex;
			half _BumpStrength;
			half4 _BumpDirection;
			half4 _BumpTiling;
			//
			sampler2D _RefractionTex;
			samplerCUBE _Skybox;

			v2f vert(appdata v)
			{
				v2f o;
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 worldNor;

				float3 disPos = CalculateWavesDisplacement(worldPos, _QA, _A, _S, _Dx, _Dz, _L);
				v.vertex.xyz = mul(unity_WorldToObject, float4(worldPos + disPos, 1));
				o.vertex = UnityObjectToClipPos(v.vertex);
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
				worldNormal = -normalize(mul(Mat, normalize(pixelNormal)));
				float3 objNormal = normalize(mul((float3x3)unity_WorldToObject, worldNormal));
				//objNormal = normalize(mul(worldNormal, (float3x3)unity_ObjectToWorld));

				float3 objCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
				float3 viewVector = normalize(i.objPos - objCameraPos);
				float3 vect = reflect((viewVector), float3(0, -1, 0));
				float3 nNormal, fNormal;
				float3 start = i.objPos - vect * 10;
				float2 d = BoxIntersection(start, vect, _BoxSize, nNormal, fNormal);
				float3 near = start + normalize(vect) * d.x;
				float3 far = start + normalize(vect) * d.y;
				float len = length(far - near);
				if (nNormal.z == 1) 
				{
					discard;
				}
				//
				float2 projUV = i.screenPos.xy / i.screenPos.z;
				float3 reflV = refract(far - near, fNormal, 1 / 1.33f);
				float4 reflectColor = texCUBE(_Skybox, objNormal);
				//
				float2 d2 = BoxIntersection(objCameraPos, (viewVector), _BoxSize, nNormal, fNormal);
				near = objCameraPos.xyz + (viewVector) * d2.x;
				far = objCameraPos.xyz + (viewVector) * d2.y;
				float len2 = length(far - near);

				float3 outScattering;
				float3 inScattering;
				ComputeScattering((len + len2) * _WaterDepth, _DiffuseRadiance, _Attenuation, _Kd, outScattering, inScattering);
				reflectColor.xyz = reflectColor * outScattering + inScattering;

				float3 outScattering2;
				float3 inScattering2;
				ComputeScattering(len2 * _WaterDepth, _DiffuseRadiance, _Attenuation, _Kd, outScattering2, inScattering2);

				float3 refrV = refract((viewVector), objNormal, 1/1.33f);
				//float4 refractColor = texCUBE(_Skybox, refrV);
				float4 refractColor = tex2D(_RefractionTex, i.screenPos.xy / i.screenPos.z + _RefractOffset * worldNormal);

				refractColor.xyz = lerp(texCUBE(_Skybox, refrV).xyz, refractColor.xyz, refractColor.a);
				refractColor.xyz = refractColor.xyz * outScattering2 + inScattering2;

				//TIR 全反射，水的临界角是48.75°
				float totalRefr = dot(-(viewVector), objNormal);
				totalRefr = step(0.659f, totalRefr);

				col.xyz = lerp(reflectColor.xyz, refractColor.xyz, saturate(totalRefr));
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
