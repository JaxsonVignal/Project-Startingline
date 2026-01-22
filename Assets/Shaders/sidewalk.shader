Shader "Universal Render Pipeline/CartoonySidewalk"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.7, 0.7, 0.75, 1)
        _CrackColor ("Crack Color", Color) = (0.3, 0.3, 0.35, 1)
        _CrackIntensity ("Crack Intensity", Range(0, 1)) = 0.5
        _NoiseScale ("Noise Scale", Float) = 5.0
        _CellSize ("Cell Size", Float) = 2.0
        _ShadowSteps ("Shadow Steps", Range(2, 5)) = 3
        _ShadowSoftness ("Shadow Softness", Range(0, 0.5)) = 0.05
        _SpecularSize ("Specular Size", Range(0, 1)) = 0.1
        _SpecularIntensity ("Specular Intensity", Range(0, 1)) = 0.3
        _Smoothness ("Smoothness", Range(0, 1)) = 0.2
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 200
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            
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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _CrackColor;
                float _CrackIntensity;
                float _NoiseScale;
                float _CellSize;
                float _ShadowSteps;
                float _ShadowSoftness;
                float _SpecularSize;
                float _SpecularIntensity;
                float _Smoothness;
            CBUFFER_END
            
            // Simple hash function for noise
            float hash(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }
            
            // Value noise
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
            
            // Fractal noise
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }
            
            // Sidewalk cracks pattern
            float cracks(float2 uv)
            {
                float2 cell = floor(uv * _CellSize);
                float2 localUV = frac(uv * _CellSize);
                
                // Create grid lines
                float lineWidth = 0.05;
                float gridLines = step(localUV.x, lineWidth) + step(localUV.y, lineWidth);
                
                // Add random cracks
                float crackNoise = fbm(uv * _NoiseScale);
                float randomCracks = step(0.7, crackNoise);
                
                // Combine grid and random cracks
                return saturate(gridLines + randomCracks * 0.5);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Get world position for texturing
                float2 uv = input.positionWS.xz;
                
                // Base concrete color with slight variation
                float variation = fbm(uv * 0.5) * 0.15;
                float3 baseColor = _BaseColor.rgb + variation;
                
                // Get crack pattern
                float crackMask = cracks(uv);
                
                // Mix base color with cracks
                float3 albedo = lerp(baseColor, _CrackColor.rgb, crackMask * _CrackIntensity);
                
                // Add subtle grime/dirt variation
                float grime = fbm(uv * 3.0) * 0.1;
                albedo *= (1.0 - grime * 0.3);
                
                // Lighting
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                
                // Cel-shaded diffuse
                float NdotL = dot(normalWS, mainLight.direction);
                float shadow = floor(saturate(NdotL) * _ShadowSteps) / _ShadowSteps;
                shadow = smoothstep(0.0, _ShadowSoftness, shadow);
                shadow *= mainLight.shadowAttenuation;
                
                // Specular highlight
                float3 halfVector = normalize(mainLight.direction + viewDirWS);
                float NdotH = dot(normalWS, halfVector);
                float specular = step(1.0 - _SpecularSize, pow(max(0, NdotH), 32.0)) * _SpecularIntensity;
                
                // Combine lighting
                float3 lighting = albedo * mainLight.color * shadow;
                lighting += specular * mainLight.color;
                
                // Additional lights
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    float NdotL_add = dot(normalWS, light.direction);
                    float shadow_add = floor(saturate(NdotL_add) * _ShadowSteps) / _ShadowSteps;
                    shadow_add = smoothstep(0.0, _ShadowSoftness, shadow_add);
                    lighting += albedo * light.color * shadow_add * light.distanceAttenuation;
                }
                #endif
                
                // Ambient
                float3 ambient = albedo * 0.3;
                lighting += ambient;
                
                // Apply fog
                lighting = MixFog(lighting, input.fogFactor);
                
                return half4(lighting, 1.0);
            }
            ENDHLSL
        }
        
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
                
                #if UNITY_REVERSED_Z
                output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
