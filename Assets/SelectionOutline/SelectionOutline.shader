Shader "Unlit/SelectionOutline"
{
    Properties {
        _SelectionColor("Selection Color", Color) = (1, 1, 1, 1)
        _Thickness("Thickness", Float) = 0.5
    }
    SubShader {
        // Queue를 Overlay급으로 높여서 가장 나중에 그려지게 합니다.
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+500" }
        
        Pass {
            Name "SelectionPass"
            Cull Front     // 뒷면만 부풀려서 외곽선 형태 생성
            ZWrite Off     // 깊이 기록 안 함 (다른 물체 안 가림)
            ZTest Always   // ★ 핵심: 앞에 뭐가 있든 무조건 뚫고 그려짐

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            float _Thickness;
            float4 _SelectionColor;

            Varyings vert(Attributes input) {
                Varyings output;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(input.normalOS);
                
                // 월드 좌표 기준 확장 (오브젝트 스케일 무시하고 일정하게 확장)
                posWS += normWS * _Thickness; 
                output.positionCS = TransformWorldToHClip(posWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target { return _SelectionColor; }
            ENDHLSL
        }
    }
}
