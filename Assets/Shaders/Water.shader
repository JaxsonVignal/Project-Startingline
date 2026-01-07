Shader "Custom/SimpleWater"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.2, 0.5, 0.7, 0.8)
        _MainTex ("Wave Pattern", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _WaveSpeed ("Wave Speed", Range(0, 1)) = 0.1
        _WaveScale ("Wave Scale", Range(0, 10)) = 1
        _Glossiness ("Smoothness", Range(0, 1)) = 0.9
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalMap;
        fixed4 _Color;
        float _WaveSpeed;
        float _WaveScale;
        half _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Animate UVs
            float2 uv = IN.uv_MainTex * _WaveScale;
            uv.x += _Time.y * _WaveSpeed;
            
            // Sample textures
            fixed4 c = tex2D(_MainTex, uv) * _Color;
            fixed3 normal = UnpackNormal(tex2D(_NormalMap, uv));
            
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Normal = normal;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}