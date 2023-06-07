Shader "Custom/WavesSurface"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Speed("Speed", Range(.01, 1.0)) = .5

        _WaveA("Wave A (dir, steepness, wavelength)", Vector) = (1,0,0.5,10)
        _WaveB("Wave B", Vector) = (0,1,0.25,20)
        _WaveC("Wave C", Vector) = (1,1,0.15,10)

        _Wall("Wall", Int) = 0

        _SizeX("Size X", Float) = 1
        _SizeZ("Size Z", Float) = 1
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard noshadow vertex:vert alpha:premul

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 vertex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float _Steepness, _Freq;
        float4 _Direction;
        float _Speed;

        float4 _WaveA, _WaveB, _WaveC;

        int _Wall;

        float _SizeX, _SizeZ;

        float _GameTime;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal)
        {
            float steepness = wave.z;
            float wavelength = wave.w;
            float k = 2 * UNITY_PI / wavelength;
            float c = sqrt(9.8 / k);
            float2 d = normalize(wave.xy);
            float f = k * (dot(d, p.xz) - c * _GameTime * _Speed);

            float sf = steepness * sin(f);
            float cf = steepness * cos(f);

            tangent +=  float3(-d.x * d.x * sf, d.x * cf, -d.x * d.y * sf);
            binormal += float3(-d.x * d.y * sf, d.y * cf, -d.y * d.y * sf);
            
            float as = sf / k;
            float ac = cf / k;

            return float3(d.x * ac, as, d.y * ac);
        }

        void vert(inout appdata_full vertexData, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            float3 gridPoint = vertexData.vertex.xyz;
            float3 tangent = float3(1, 0, 0);
            float3 binormal = float3(0, 0, 1);
            float3 p = float3(0, 0, 0);

            p += GerstnerWave(_WaveA, gridPoint, tangent, binormal);
            p += GerstnerWave(_WaveB, gridPoint, tangent, binormal);
            p += GerstnerWave(_WaveC, gridPoint, tangent, binormal);

            if (gridPoint.y < 0) p.y = 0;
            float3 normal = float3(0, 0, 0);

            if (_Wall > 0)
            {
                float3 binormalWall = binormal;
                binormalWall.y = 0;
                float3 tangentWall = tangent;
                tangentWall.y = 0;

                float3 wallTangent = (binormalWall * vertexData.normal.x + tangentWall * vertexData.normal.z);

                //// somewhat inefficient solution to have correct bottom normals
                normal = lerp(cross(float3(0, 1, 0), normalize(wallTangent)), vertexData.normal, abs(vertexData.normal.y));
            
                //normal = vertexData.normal;
                //p *= float3(0, 1, 0);
            }
            else normal = normalize(cross(binormal, tangent));

            p += gridPoint;

            vertexData.vertex.xyz = p;
            vertexData.normal = normal;
            vertexData.color = half4(normal, 1);

            o.vertex = p.xz;
            //o.uv = vertexData.uv;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex)* _Color;
            o.Albedo = c.rgb;
            //o.Albedo = half4(0, 0, 0, 1);// c.rgb;
            //o.Emission = o.Normal;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;

            //if (_Wall == 0 && (abs(IN.vertex.x) > _SizeX / 2 || abs(IN.vertex.y) > _SizeZ / 2)) clip(-1);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
