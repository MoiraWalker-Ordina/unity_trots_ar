// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "CubeWater/CubeWaveWall"
{
	Properties
	{
		_WaterDepth("WaterDepth", float) = 1
		[HideInInspector]
		_Kd("water scattering kd", Vector) = (.3867, .1055, .0469, 0)
		_Attenuation("_Attenuation", Vector) = (.45, .1718, .1133, 0)
		_DiffuseRadiance("Water Diffuse", color) = (.0338, .1015, .2109, 0)
		_RefractOffset("RefractOffset", float) = 0.1
		_Skybox("skybox", CUBE) = "white"{}
	}
	SubShader
	{
		Tags{ "RenderType" = "Transparent" "LightMode" = "ForwardBase" "Queue" = "Transparent"}
		LOD 100
		Cull off
		Pass
		{
			Stencil
			{
				Ref 2
				Comp Greater
				Pass Replace
			}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
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
				float3 offsetPos:TEXCOORD2;
				float3 objCameraPos : TEXCOORD3;
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
			sampler2D _RefractionTex;
			samplerCUBE _Skybox;
			v2f vert (appdata v)
			{
				v2f o;
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex);

				float3 disPos = CalculateWavesDisplacement(worldPos, _QA, _A, _S, _Dx, _Dz, _L);
				v.vertex.xyz = mul(unity_WorldToObject, float4(worldPos+disPos, 1));
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex).xyw;
				o.objPos = v.vertex.xyz;
				o.offsetPos = disPos;
				o.objCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1));
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			float4 MyComputeScreenPos(float4 pos) {
				float4 o = pos * 0.5f;
				o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w;
				o.zw = pos.zw;
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float4 col = float4(0, 0, 0, 1);
				
				//float3 objCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1));
				float3 viewVector = normalize(i.objPos - i.objCameraPos);
				float3 nNormal, fNormal;
				float2 d = BoxIntersection(i.objCameraPos.xyz, (viewVector), _BoxSize, nNormal, fNormal);
				float3 near = i.objCameraPos.xyz + (viewVector) * d.x;
				float3 far = i.objCameraPos.xyz + (viewVector) * d.y;
				float len = length(far - near);

				float3 outScattering;
				float3 inScattering;
				ComputeScattering(len * _WaterDepth, _DiffuseRadiance, _Attenuation, _Kd, outScattering, inScattering);

				float3 refrV = refract((viewVector), -fNormal, 1/1.33f);
				//float2 d2 = BoxIntersection(i.objPos, refrV, _BoxSize, outNormal);

				float4 refractColor = tex2D(_RefractionTex, i.screenPos.xy / i.screenPos.z + _RefractOffset * i.offsetPos);
				float4 refractColor2 = texCUBE(_Skybox, refrV + _RefractOffset*i.offsetPos);
				//refractColor2.xyz = refractColor2.xyz * outScattering + inScattering;

				//float3 refr = lerp(refractColor2.xyz, refractColor.xyz, saturate(fNormal.z));

				refractColor.xyz = lerp(refractColor2.xyz, refractColor.xyz, refractColor.a);

				col.xyz = refractColor.xyz * outScattering + inScattering;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
