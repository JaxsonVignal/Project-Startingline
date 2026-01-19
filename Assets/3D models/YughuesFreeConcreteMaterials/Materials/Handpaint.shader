Shader "URP/HandPainted"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _BaseMap ("Base Map", 2D) = "white" {}
        
        [Header(Lighting)]
        _AmbientColor ("Ambient Color", Color) = (0.4, 0.4, 0.4, 1)
        _LightIntensity ("Light Intensity", Range(0, 2)) = 1.0
        _ShadowSoftness ("Shadow Softness", Range(0, 1)) = 0.5
        
        [Header(Painterly Effect)]
        _PaintStrength ("Paint Strength", Range(0, 1)) = 0.3
        _Saturation ("Saturation", Range(0, 2)) = 1.2
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        
        [Header(Fresnel)]
        _FresnelColor ("Fresnel Color", Color) = (1, 1, 1, 0.3)
        _FresnelPower ("Fresnel Power", Range(0.1, 5.0)) = 2.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
            };
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _AmbientColor;
                float _LightIntensity;
                float _ShadowSoftness;
                float _PaintStrength;
                float _Saturation;
                float _Brightness;
                half4 _FresnelColor;
                float _FresnelPower;
            CBUFFER_END
            
            // Simple noise function for painterly effect
            float hash(float2 p)
            {
                float h = dot(p, float2(127.1, 311.7));
                return frac(sin(h) * 43758.5453123);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Adjust saturation
            half3 AdjustSaturation(half3 color, float saturation)
            {
                half3 gray = dot(color, half3(0.299, 0.587, 0.114));
                return lerp(gray, color, saturation);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 albedo = baseMap * _BaseColor;
                
                // Get main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                
                // Calculate soft diffuse lighting
                float3 normalWS = normalize(input.normalWS);
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = dot(normalWS, lightDir);
                
                // Soft wrap lighting (like subsurface scattering)
                float diffuse = saturate((NdotL + _ShadowSoftness) / (1.0 + _ShadowSoftness));
                
                // Apply shadows softly
                diffuse *= lerp(0.5, 1.0, mainLight.shadowAttenuation);
                
                // Add ambient light
                half3 ambient = _AmbientColor.rgb;
                
                // Combine lighting
                half3 lighting = ambient + mainLight.color * diffuse * _LightIntensity;
                
                // Fresnel effect (rim lighting)
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(viewDirWS, normalWS)), _FresnelPower);
                half3 fresnelColor = _FresnelColor.rgb * fresnel * _FresnelColor.a;
                
                // Apply lighting to albedo
                half3 color = albedo.rgb * lighting + fresnelColor;
                
                // Add painterly variation
                float paintNoise = noise(input.uv * 50.0 + input.positionWS.xz * 2.0);
                color = lerp(color, color * (0.8 + paintNoise * 0.4), _PaintStrength);
                
                // Adjust saturation for painted look
                color = AdjustSaturation(color, _Saturation);
                
                // Apply brightness
                color *= _Brightness;
                
                return half4(color, albedo.a);
            }
            ENDHLSL
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            float3 _LightDirection;
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
