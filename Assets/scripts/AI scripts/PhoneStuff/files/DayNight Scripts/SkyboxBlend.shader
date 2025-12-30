Shader "Skybox/PanoramicBlend"
{
    Properties
    {
        [NoScaleOffset] _MainTex1 ("Texture 1", 2D) = "grey" {}
        [NoScaleOffset] _MainTex2 ("Texture 2", 2D) = "grey" {}
        _Blend ("Blend", Range(0,1)) = 0.5
        
        [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
        _Rotation1 ("Rotation 1", Range(0, 360)) = 0
        _Rotation2 ("Rotation 2", Range(0, 360)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex1;
            sampler2D _MainTex2;
            half _Blend;
            half4 _MainTex1_HDR;
            half4 _MainTex2_HDR;
            half4 _Tint;
            half _Exposure;
            float _Rotation1;
            float _Rotation2;

            struct appdata_t
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            float3 RotateAroundYInDegrees (float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            float2 ToRadialCoords(float3 coords)
            {
                float3 normalizedCoords = normalize(coords);
                float latitude = acos(normalizedCoords.y);
                float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
                float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / UNITY_PI, 1.0 / UNITY_PI);
                return float2(0.5 - sphereCoords.x, 1.0 - sphereCoords.y);  // Flipped Y
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 rotated1 = RotateAroundYInDegrees(i.texcoord, _Rotation1);
                float3 rotated2 = RotateAroundYInDegrees(i.texcoord, _Rotation2);

                float2 uv1 = ToRadialCoords(rotated1);
                float2 uv2 = ToRadialCoords(rotated2);

                half4 tex1 = tex2D(_MainTex1, uv1);
                half4 tex2 = tex2D(_MainTex2, uv2);

                half3 c1 = DecodeHDR(tex1, _MainTex1_HDR);
                half3 c2 = DecodeHDR(tex2, _MainTex2_HDR);

                half3 c = lerp(c1, c2, _Blend);
                c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
                c *= _Exposure;

                return half4(c, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}