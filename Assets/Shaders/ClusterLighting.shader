Shader "Unlit/ClusterLighting"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "ClusterLighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                half3 normal : NORMAL;
                half4 tangent : TANGENT;
                float4 texcoord : TEXCOORD0;
                float4 texcoord1 : TEXCOORD1;
                float4 texcoord2 : TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 positionWS : TEXCOORD0; // xyz = positionWS, w = fogCoord
                half3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2; // holds terrainUV ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Properties
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float _Smoothness;
            CBUFFER_END

            uniform sampler2D _CameraGBufferTexture0;
            uniform sampler2D _CameraGBufferTexture1;
            uniform sampler2D _CameraGBufferTexture2;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            float4 ComputeClipSpacePosition(float2 screenPosNorm, float deviceDepth)
            {
                float4 positionCS = float4(screenPosNorm * 2.0 - 1.0, deviceDepth, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                positionCS.y = -positionCS.y;
                #endif
                return positionCS;
            }

            float2 UnStereo(float2 UV)
            {
                #if UNITY_SINGLE_PASS_STEREO
					float4 scaleOffset = unity_StereoScaleOffset[ unity_StereoEyeIndex ];
					UV.xy = (UV.xy - scaleOffset.zw) / scaleOffset.xy;
                #endif
                return UV;
            }

            float3 InvertDepthDir72_g6(float3 In)
            {
                float3 result = In;
                #if !defined(ASE_SRP_VERSION) || ASE_SRP_VERSION <= 70301
                result *= float3(1, 1, -1);
                #endif
                return result;
            }

            float3 WorldFromDepthTexture(float4 ScreenPosNorm)
            {
                float2 UV22_g7 = ScreenPosNorm.xy;
                float2 localUnStereo22_g7 = UnStereo(UV22_g7);
                float2 break64_g6 = localUnStereo22_g7;
                float depth01_69_g6 = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, ScreenPosNorm.xy);
                #ifdef UNITY_REVERSED_Z
                float staticSwitch38_g6 = (1.0 - depth01_69_g6);
                #else
					float staticSwitch38_g6 = depth01_69_g6;
                #endif
                float3 appendResult39_g6 = (float3(break64_g6.x, break64_g6.y, staticSwitch38_g6));
                float4 appendResult42_g6 = (float4((appendResult39_g6 * 2.0 + -1.0), 1.0));
                float4 temp_output_43_0_g6 = mul(unity_CameraInvProjection, appendResult42_g6);
                float3 temp_output_46_0_g6 = ((temp_output_43_0_g6).xyz / (temp_output_43_0_g6).w);
                float3 In72_g6 = temp_output_46_0_g6;
                float3 localInvertDepthDir72_g6 = InvertDepthDir72_g6(In72_g6);
                float4 appendResult49_g6 = (float4(localInvertDepthDir72_g6, 1.0));
                float4 WorldPos181 = mul(unity_CameraToWorld, appendResult49_g6);

                return WorldPos181;
            }

            v2f VertexFunction(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


                #ifdef ASE_ABSOLUTE_VERTEX_POS
						float3 defaultVertexValue = v.vertex.xyz;
                #else
                float3 defaultVertexValue = float3(0, 0, 0);
                #endif
                float3 vertexValue = defaultVertexValue;
                #ifdef ASE_ABSOLUTE_VERTEX_POS
						v.vertex.xyz = vertexValue;
                #else
                v.vertex.xyz += vertexValue;
                #endif
                v.vertex.w = 1;
                v.normal = v.normal;
                v.tangent = v.tangent;

                float3 positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                half3 normalWS = UnityObjectToWorldNormal(v.normal);
                half3 tangentWS = UnityObjectToWorldDir(v.tangent.xyz);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.positionWS.xyz = positionWS;
                o.normalWS = normalWS;
                o.tangentWS = half4(tangentWS, v.tangent.w);

                return o;
            }

            v2f vert(appdata v)
            {
                return VertexFunction(v);
            }

            half4 frag(v2f IN) : SV_Target
            {
                // float3 PositionWS = IN.positionWS.xyz;
                float4 ScreenPosNorm = float4(IN.pos.xy * (_ScreenParams.zw - 1.0), IN.pos.zw);
                float3 PositionWS = WorldFromDepthTexture(ScreenPosNorm);

                float3 finalColor;

                half3 ViewDirWS = normalize(UnityWorldSpaceViewDir(PositionWS) - PositionWS);
                float4 ClipPos = ComputeClipSpacePosition(ScreenPosNorm.xy, IN.pos.z) * IN.pos.w;
                float4 ScreenPos = ComputeScreenPos(ClipPos);
                half3 NormalWS = IN.normalWS;
                half3 TangentWS = IN.tangentWS.xyz;
                half3 BitangentWS = cross(IN.normalWS, IN.tangentWS.xyz) * IN.tangentWS.w * unity_WorldTransformParams.w;

                float2 uv = ScreenPosNorm.xy;
                half4 gbuffer0 = tex2D(_CameraGBufferTexture0, uv);
                half4 gbuffer1 = tex2D(_CameraGBufferTexture1, uv);
                half4 gbuffer2 = tex2D(_CameraGBufferTexture2, uv);


                #if defined(_DEBUGVISUALIZATION_LIGHTCOUNT)
                    // Debug: Visualize light count
                    finalColor = VisualizeClusterLightCount(PositionWS);
                #elif defined(_DEBUGVISUALIZATION_GRID)
                    // Debug: Visualize grid cells
                    finalColor = VisualizeClusterGrid(PositionWS, gbuffer0.rgb);
                #else
                // Normal cluster lighting
                finalColor = ApplyClusterLighting(
                    PositionWS,
                    NormalWS,
                    ViewDirWS,
                    gbuffer0,
                    _Smoothness
                );
                #endif


                return float4(finalColor, 1);
            }
            ENDCG
        }
    }
}