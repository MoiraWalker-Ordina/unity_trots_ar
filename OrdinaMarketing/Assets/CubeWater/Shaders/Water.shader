Shader "CubeWater/Water"
{
    Properties
    {
        [HideInInspector]
        _Kd("water scattering kd", Vector) = (.3867, .1055, .0469, 0)
        _Attenuation("Attenuation", Vector) = (.45, .1718, .1133, 0)
        _DiffuseRadiance("Water Diffuse", color) = (.0338, .1015, .2109, 0)
        _RefractOffset("RefractOffset", float) = 0.1
        _ReflectOffset("ReflectOffset", float) = 0.1
        _FresnelFactor("Fresnel", float) = 0.02
        _SpecularFactor("specular x:intensity y:spread z:cutout", Vector) = (64, 0, 0.96, 0)
        _SpecularColor("SpecularColor", color) = (1,1,1,1)
        _WaterDepth("WaterDepth", float) = 1

        _BumpTex("Bump Texture", 2D) = "white"{}
        _BumpStrength("Bump strength", Range(0.0, 10.0)) = 1.0
        _BumpDirection("Bump direction(2 wave)", Vector) = (0.01,0.01,0.02,-0.02)
        _BumpTiling("Bump tiling", Vector) = (0.01,0.01,0.013,0.013)

        _FoamTex("Foam", 2D) = "white"{}
        _NoiseTex("Foam noise", 2D) = "white"{}
        _FoamParam("Foam x:range y:count z:speed", Vector) = (1, 3, 1, 0)
        _FoamColor("Foam color", color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderQueue" = "Transparent"}
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
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
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float4 foamUV : TEXCOORD3;
                UNITY_FOG_COORDS(4)
            };

            float4 _QA;
            float4 _A;
            float4 _S;
            float4 _Dx;
            float4 _Dz;
            float4 _L;
            float _WaterDepth;
            float4 _FoamParam;
            float3 _Kd;
            float3 _Attenuation;
            float3 _DiffuseRadiance;

            half _ReflectOffset;
            half _RefractOffset;
            half _FresnelFactor;
            half3 _SpecularFactor;
            half4 _SpecularColor;
            half _BumpStrength;
            half4 _BumpDirection;
            half4 _BumpTiling;

            sampler2D _ReflectionTex;
            sampler2D _RefractionTex;
            sampler2D _CameraDepthTexture;
            sampler2D _BumpTex;

            sampler2D _FoamTex;
            sampler2D _NoiseTex;
            half4 _NoiseTex_ST;
            half3 _FoamColor;

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float3 disPos = CalculateWavesDisplacement(worldPos, _QA, _A, _S, _Dx, _Dz, _L);
                worldPos += disPos;

                v.vertex.xyz = mul(unity_WorldToObject, float4(worldPos, 1));

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = worldPos.xzxz * _BumpTiling.xyzw + _T * _BumpDirection.xyzw;
                o.screenPos = ComputeScreenPos(o.vertex);
                o.worldPos = worldPos;

                float sinTime = sin(_Time.y);
                o.foamUV.xy = _FoamParam.w * worldPos.xz + _Time.xx * _FoamParam.z;
                o.foamUV.zw = _NoiseTex_ST.xy * worldPos.xz + _Time.xx * _NoiseTex_ST.zw;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // sample the texture
                half4 col = half4(1,1,1,1);
                half2 screenUV = i.screenPos.xy / i.screenPos.w;

                //计算水体深度
                half sceneDepth = LinearEyeDepth(tex2D(_CameraDepthTexture, screenUV).r);
                half waterDepth = LinearEyeDepth(i.vertex.z);//i.screenPos.z / i.screenPos.w
                half depth = max(0, sceneDepth - waterDepth);

                //计算世界空间法线
                half3 pixelNormal = normalize(PerPixelNormal(_BumpTex, i.uv, _BumpStrength));
                half3 binormal = 0;
                half3 tangent = 0;
                CalculateWavesBinormalTangent(i.worldPos.xyz, _QA, _A, _S, _Dx, _Dz, _L, binormal, tangent);
                half3 worldNormal = cross(tangent, binormal);

                half3x3 mat = { binormal, worldNormal, tangent };
                mat = transpose(mat);
                worldNormal = normalize(mul(mat, pixelNormal));

                half3 viewVector = normalize(_WorldSpaceCameraPos - i.worldPos);

                //out:消光+外散射；in:内散射
                half3 outScattering, inScattering;
                half cosTheta = viewVector.y;// dot(viewVector, half3(0, 1, 0));

                ComputeScattering(depth * _WaterDepth, _DiffuseRadiance, _Attenuation, _Kd* cosTheta, outScattering, inScattering);
                //折射+反射+菲涅尔
                half3 reflCol = tex2D(_ReflectionTex, screenUV + _ReflectOffset * worldNormal.xz);
                half3 refrCol = tex2D(_RefractionTex, screenUV + _RefractOffset * worldNormal.xz);
                half fresnel = FastFresnel(viewVector, worldNormal, _FresnelFactor);

                col.xyz = refrCol * outScattering + inScattering;
                col.xyz = lerp(col.xyz, reflCol, fresnel);

                //高光
                half3 h = normalize(viewVector + _WorldSpaceLightPos0.xyz);
                worldNormal = normalize(worldNormal + half3(0, _SpecularFactor.y, 0));
                half specular = pow(max(0, dot(h, worldNormal)), _SpecularFactor.x) * _SpecularColor.w;
                specular = step(_SpecularFactor.z, specular) * specular;

                col.xyz += specular * _SpecularColor.xyz;
                //
                half foamThreshold = 1 - saturate(depth / _FoamParam.x);

                if (foamThreshold > 0)
                {
                    half noise = tex2D(_NoiseTex, i.foamUV.zw).r * 0.5 + 0.5;
                    foamThreshold *= noise;
                    half foamGradient = saturate(frac(foamThreshold * _FoamParam.y - _Time.y * _FoamParam.z) * 2 - 1);
                    half2 foamDistortUV = worldNormal.xy * 0.2;
                    half3 foamColor = tex2D(_FoamTex, i.foamUV.xy + foamDistortUV).rgb;
                    foamColor = foamGradient * foamColor  * _FoamColor.rgb * foamThreshold;
                    col.rgb += foamColor;
                }

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
