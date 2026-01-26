Shader "Custom/URP/CartoonYellowRoadPaint"
{
    Properties
    {
        [Header(Base Color)]
        _BaseColor ("Base Yellow Color", Color) = (1, 0.9, 0.1, 1)
        _HighlightColor ("Highlight Color", Color) = (1, 1, 0.5, 1)
        _ShadowColor ("Shadow Color", Color) = (0.8, 0.7, 0, 1)
        
        [Header(Paint Properties)]
        _Glossiness ("Glossiness", Range(0, 1)) = 0.3
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _PaintRoughness ("Paint Roughness", Range(0, 1)) = 0.7
        
        [Header(Weathering)]
        [NoScaleOffset] _WeatheringMask ("Weathering Mask", 2D) = "white" {}
        _WeatheringAmount ("Weathering Amount", Range(0, 1)) = 0.3
        _DirtColor ("Dirt/Worn Color", Color) = (0.4, 0.35, 0.2, 1)
        
        [Header(Stripes Optional)]
        _UseStripes ("Use Stripes", Float) = 0
        _StripeColor ("Stripe Color", Color) = (0, 0, 0, 1)
        _StripeWidth ("Stripe Width", Range(0, 1)) = 0.1
        _StripeSpacing ("Stripe Spacing", Range(0.1, 2)) = 0.5
        _StripeDirection ("Stripe Direction", Vector) = (1, 0, 0, 0)
        
        [Header(Cartoon Shading)]
        _ToonRamp ("Toon Ramp Steps", Range(2, 10)) = 3
        _RimColor ("Rim Color", Color) = (1, 1, 0.8, 1)
        _RimPower ("Rim Power", Range(0.1, 8)) = 2
        _RimIntensity ("Rim Intensity", Range(0, 1)) = 0.3
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        LOD 100
        
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
                float3 viewDirWS : TEXCOORD4;
            };
            
            TEXTURE2D(_WeatheringMask);
            SAMPLER(sampler_WeatheringMask);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _HighlightColor;
                float4 _ShadowColor;
                float4 _DirtColor;
                float4 _StripeColor;
                float4 _RimColor;
                float4 _StripeDirection;
                float _Glossiness;
                float _Metallic;
                float _PaintRoughness;
                float _WeatheringAmount;
                float _UseStripes;
                float _StripeWidth;
                float _StripeSpacing;
                float _ToonRamp;
                float _RimPower;
                float _RimIntensity;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                
                return output;
            }
            
            // Toon ramp function for cartoon shading
            float ToonRamp(float value, float steps)
            {
                return floor(value * steps) / steps;
            }
            
            // Simple noise for variation
            float SimpleNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Normalize vectors
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Get main light
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = normalize(mainLight.direction);
                
                // Calculate basic lighting
                float NdotL = dot(normalWS, lightDir);
                float lightIntensity = saturate(NdotL);
                
                // Apply toon ramp
                float toonLight = ToonRamp(lightIntensity, _ToonRamp);
                
                // Shadow handling
                float shadow = mainLight.shadowAttenuation;
                toonLight *= shadow;
                
                // Base color with toon shading
                float3 baseColor = _BaseColor.rgb;
                float3 litColor = lerp(_ShadowColor.rgb, _BaseColor.rgb, toonLight);
                litColor = lerp(litColor, _HighlightColor.rgb, pow(toonLight, 2.0));
                
                // Weathering/dirt
                float weathering = SAMPLE_TEXTURE2D(_WeatheringMask, sampler_WeatheringMask, input.uv).r;
                float weatheringFactor = weathering * _WeatheringAmount;
                
                // Add some noise for paint variation
                float paintNoise = SimpleNoise(input.uv * 20.0) * 0.1;
                litColor += paintNoise * (1.0 - weatheringFactor);
                
                // Apply weathering/dirt
                litColor = lerp(litColor, _DirtColor.rgb, weatheringFactor);
                
                // Stripes (for diagonal yellow/black stripes on speedbumps)
                if (_UseStripes > 0.5)
                {
                    float2 stripeDir = normalize(_StripeDirection.xy);
                    float stripePos = dot(input.uv, stripeDir);
                    float stripe = frac(stripePos / _StripeSpacing);
                    float stripeMask = step(stripe, _StripeWidth);
                    litColor = lerp(litColor, _StripeColor.rgb, stripeMask);
                }
                
                // Rim lighting for cartoon effect
                float NdotV = 1.0 - saturate(dot(normalWS, viewDirWS));
                float rim = pow(NdotV, _RimPower) * _RimIntensity;
                litColor += _RimColor.rgb * rim;
                
                // Specular highlight (simple cartoon specular)
                float3 halfDir = normalize(lightDir + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specular = pow(NdotH, 32.0 * (1.0 - _PaintRoughness)) * _Glossiness;
                specular = step(0.5, specular); // Toon specular
                litColor += specular * mainLight.color * 0.5;
                
                // Add main light color influence
                litColor *= mainLight.color;
                
                // Apply fog
                litColor = MixFog(litColor, input.fogFactor);
                
                return half4(litColor, 1.0);
            }
            ENDHLSL
        }
        
        // Shadow casting pass
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
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                return output;
            }
            
            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
        
        // Depth only pass
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
            
            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
