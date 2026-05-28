Shader "Custom/Wall_WorldUV" {
    Properties {
        _MainTex ("Wallpaper Texture", 2D) = "white" {}
        _TileSize ("Tile Size", Float) = 1.0
        _Brightness ("Brightness", Range(0, 2)) = 1.0
        _RotationAngle ("Rotation Angle", Range(0, 360)) = 0
        [Toggle] _FlipX ("Flip X", Float) = 0
        [Toggle] _FlipY ("Flip Y", Float) = 0
        [Toggle] _FlipZ ("Flip Z", Float) = 0
        [KeywordEnum(XY, ZY, XZ)] _UVAxis ("UV Axis", Float) = 0
    }

    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _TileSize;
                float _Brightness;
                float _RotationAngle;
                float _FlipX;
                float _FlipY;
                float _FlipZ;
                float _UVAxis;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.positionWS);
                return OUT;
            }

            float2 RotateUV(float2 uv, float angle) {
                float rad = angle * (3.14159265 / 180.0);
                float cosA = cos(rad);
                float sinA = sin(rad);
                float2 center = float2(0.5, 0.5);
                uv -= center;
                uv = float2(uv.x * cosA - uv.y * sinA,
                            uv.x * sinA + uv.y * cosA);
                uv += center;
                return uv;
            }

            half4 frag(Varyings IN) : SV_Target {
                float2 uv;

                if (_UVAxis < 0.5)
                    uv = IN.positionWS.xy / _TileSize;       // XY (앞면 벽)
                else if (_UVAxis < 1.5)
                    uv = IN.positionWS.zy / _TileSize;       // ZY (옆면 벽)
                else
                    uv = IN.positionWS.xz / _TileSize;       // XZ (바닥)

                if (_FlipX > 0.5) uv.x = -uv.x;
                if (_FlipY > 0.5) uv.y = -uv.y;
                if (_FlipZ > 0.5) uv.x = -uv.x;             // Z반전은 U축 반전

                uv = RotateUV(uv, _RotationAngle);

                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Brightness;

                Light mainLight = GetMainLight(IN.shadowCoord);
                half3 lighting = mainLight.color * mainLight.distanceAttenuation
                               * mainLight.shadowAttenuation
                               * saturate(dot(IN.normalWS, mainLight.direction));

                half3 ambient = SampleSH(IN.normalWS);
                half3 finalColor = texColor.rgb * (lighting + ambient);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _TileSize;
                float _Brightness;
                float _RotationAngle;
                float _FlipX;
                float _FlipY;
                float _FlipZ;
                float _UVAxis;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;
            float _ShadowBias;
            float _ShadowNormalBias;

            Varyings vert(Attributes IN) {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                positionWS += normalWS * _ShadowNormalBias;
                positionWS += _LightDirection * _ShadowBias;

                OUT.positionCS = TransformWorldToHClip(positionWS);

                #if UNITY_REVERSED_Z
                    OUT.positionCS.z = min(OUT.positionCS.z, OUT.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    OUT.positionCS.z = max(OUT.positionCS.z, OUT.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                return 0;
            }
            ENDHLSL
        }
    }
}