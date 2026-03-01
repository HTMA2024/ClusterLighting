Shader "Recreate/ClusterLightingCube"
{
	Properties
	{
		[Enum(Diffuse,0,Specular,1,FullLighting,2)] _LightMode("Light Mode", float) = 0
		[Enum(None,0,Linear,1, Exponential,2)] _AttenMode("Light Attenuation Mode", float) = 2
		_LightFade("Light Fade", Range( 0.05, 0.95 )) = 0.8
		
        _StencilRef ("Stencil Ref", Int) = 1
		_StencilReadMask("Read Mask", Int) = 1
        [Enum(Less, 2,Equal, 3, LEqual, 4,Greater, 5,NotEqual, 6,GEqual, 7,Always, 8)]_StencilComp ("Stencil Comparison", float) = 8
        [Enum(Keep,0,Zero,1,Replace,2,IncrSat,3,DecrSat,4,IncrWrap,6,DecrWrap,7)]_StencilPass ("Stencil Pass", float) = 0
	}

	SubShader
	{
		
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "DisableBatching"="False" }

		Cull Back
		AlphaToMask Off
		ZWrite On
		ZTest LEqual
		ColorMask RGBA
		Blend One Zero
		

		CGINCLUDE
		
			float Remap(float value, float minOld, float maxOld, float minNew, float maxNew)
			{
				return minNew + (value - minOld) * (maxNew - minNew) / (maxOld - minOld);
			}
		
			float4 ComputeClipSpacePosition( float2 screenPosNorm, float deviceDepth )
			{
				float4 positionCS = float4( screenPosNorm * 2.0 - 1.0, deviceDepth, 1.0 );
			#if UNITY_UV_STARTS_AT_TOP
				positionCS.y = -positionCS.y;
			#endif
				return positionCS;
			}
		ENDCG

		
		Pass
		{
			
			Tags {  }
			ZWrite Off
			ZTest Always
			Blend One One	
			Cull Front
			
	        Stencil
	        {
	            Ref [_StencilRef]
	            Comp [_StencilComp]    // Equal
	            Pass [_StencilPass]    // Keep
	        	
				Fail Keep
				ZFail Keep
				ReadMask [_StencilReadMask]
	        }
			
			CGPROGRAM
				#define ASE_GEOMETRY
				#define ASE_FRAGMENT_NORMAL 0
				#pragma multi_compile_instancing
				#define ASE_NO_AMBIENT 1
				#define ASE_VERSION 19904

				#pragma vertex vert
				#pragma fragment frag
				#pragma target 3.5
				#pragma multi_compile_fwdadd_fullshadows
				#ifndef UNITY_PASS_FORWARDADD
					#define UNITY_PASS_FORWARDADD
				#endif
				#include "HLSLSupport.cginc"
				#if defined( ASE_GEOMETRY ) || defined( ASE_IMPOSTOR )
					#ifndef UNITY_INSTANCED_LOD_FADE
						#define UNITY_INSTANCED_LOD_FADE
					#endif
					#ifndef UNITY_INSTANCED_SH
						#define UNITY_INSTANCED_SH
					#endif
					#ifndef UNITY_INSTANCED_LIGHTMAPSTS
						#define UNITY_INSTANCED_LIGHTMAPSTS
					#endif
				#endif
				#include "UnityShaderVariables.cginc"
				#include "UnityCG.cginc"
				#include "Lighting.cginc"
				#include "UnityPBSLighting.cginc"
				#include "AutoLight.cginc"
				#include "ClusterLighting1.hlsl"

				// #include "Assets/Recreate/ResourceData/Common/Shaders/Resources/ConsoleDepth.cginc"
				#define ASE_NEEDS_FRAG_SCREEN_POSITION_NORMALIZED


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
					UNITY_LIGHTING_COORDS( 3, 4 )
					
					UNITY_VERTEX_INPUT_INSTANCE_ID
					UNITY_VERTEX_OUTPUT_STEREO
				};
			
				uniform sampler2D _CameraGBufferTexture0;
				uniform sampler2D _CameraGBufferTexture1;
				uniform sampler2D _CameraGBufferTexture2;
				uniform float _LightFade;
				uniform float _LightMode;
				uniform float _AttenMode;


				float2 TransformTriangleVertexToUV (float2 vertex)
				{
					float2 uv = (vertex + 1.0) * 0.5;
					return uv;
				}
			
				v2f VertexFunction (appdata v  ) {
					UNITY_SETUP_INSTANCE_ID(v);
					v2f o;
					UNITY_INITIALIZE_OUTPUT(v2f,o);
					UNITY_TRANSFER_INSTANCE_ID(v,o);
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

					float3 positionWS = mul( unity_ObjectToWorld, v.vertex ).xyz;
					half3 normalWS = UnityObjectToWorldNormal( v.normal );
					half3 tangentWS = UnityObjectToWorldDir( v.tangent.xyz );

				
					o.pos = UnityObjectToClipPos( v.vertex );
					o.positionWS.xyz = positionWS;
					o.normalWS = normalWS;
					o.tangentWS = half4( tangentWS, v.tangent.w );

					UNITY_TRANSFER_LIGHTING(o, v.texcoord1.xy);
					
					return o;
				}
				v2f vert ( appdata v )
				{
					return VertexFunction( v );
				}

				half4 frag ( v2f IN 
					#if defined( ASE_DEPTH_WRITE_ON )
					, out float outputDepth : SV_Depth
					#endif
					) : SV_Target
				{
					UNITY_SETUP_INSTANCE_ID(IN);

					// float3 PositionWS = IN.positionWS.xyz;
					float4 ScreenPosNorm = float4( IN.pos.xy * ( _ScreenParams.zw - 1.0 ), IN.pos.zw );
					float3 PositionWS = WorldFromDepthTexture(ScreenPosNorm);
					
					float4 lightPos = float4(0,0,0,0);
					lightPos.w = 0;
					
					half3 ViewDirWS = normalize( UnityWorldSpaceViewDir( PositionWS ) );
					float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, IN.pos.z ) * IN.pos.w;
					float4 ScreenPos = ComputeScreenPos( ClipPos );
					half3 NormalWS = IN.normalWS;
					half3 TangentWS = IN.tangentWS.xyz;
					half3 BitangentWS = cross( IN.normalWS, IN.tangentWS.xyz ) * IN.tangentWS.w * unity_WorldTransformParams.w;
					
					
					half atten;
					{
						#if defined( ASE_RECEIVE_SHADOWS )
							UNITY_LIGHT_ATTENUATION( temp, IN, PositionWS.xyz )
							atten = temp;
						#else
							atten = 1;
						#endif
					}
					float2 uv = ScreenPosNorm.xy;
				    half4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
				    half4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
				    half4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);
				    UnityStandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);

					float viewDir = ViewDirWS;
					float3 albedo = data.diffuseColor;
					float3 normal = data.normalWorld;
					float metallic = data.specularColor;
					float occlusion = data.occlusion;
					float smoothness = data.smoothness;
					float3 emission = 0;
					float alpha = 1;

					float lightMode = _LightMode;
					float attenMode = _AttenMode;
					half4 c = 0;
					// c.xyz += ApplyClusterLighting1(PositionWS, normal, ViewDirWS, albedo, metallic, smoothness, attenMode, lightMode).xyz;

					
			        half3 lightDir = normalize(lightPos.xyz - PositionWS);
			        float lightAtten = 1;
			        if (attenMode == 0)
			        {
			        }
			        else if (attenMode == 1)
			        {
			            lightAtten = LinearAttenuation(lightPos, PositionWS);
			        }
			        else if (attenMode == 2)
			        {
			            lightPos.w = 0;
			            lightAtten = LightFalloff(lightPos, PositionWS);
			        }
			        UnityLight unityLight;
			        unityLight.color = 1 * lightAtten;
			        unityLight.dir = lightDir;
			        unityLight.ndotl = 0;
			        
			        // 计算光照贡献
					// c.xyz += PointLightingStandard(s, viewDir, unityLight, lightMode);

					
					SurfaceOutputStandard o = (SurfaceOutputStandard)0;
					o.Albedo = data.diffuseColor;
					o.Normal = data.normalWorld;
					o.Metallic = data.specularColor;
					o.Occlusion = data.occlusion;
					o.Smoothness = data.smoothness;
					o.Emission = 0;
					o.Alpha = 1;
					
					if (_AttenMode == 0)
					{
					}
					else if (_AttenMode == 1)
					{
						lightPos.w = 1;
						lightAtten = LinearAttenuation(lightPos, PositionWS);
					}
					else if (_AttenMode == 2)
					{
						lightPos.w = 0;
						lightAtten = LightFalloff(lightPos, PositionWS);
					}
					
					
					lightDir = normalize(lightPos.xyz - PositionWS) ;
					
					UnityGI gi;
					UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
					gi.indirect.diffuse = 0;
					gi.indirect.specular = 0;
					gi.light.color = 1;
					gi.light.dir = lightDir;
					gi.light.color *= lightAtten; // TODO
					
					// c += PointLightingStandard(o, ViewDirWS, gi, lightMode);
					
					c.xyz += ApplyClusterLighting1(data, PositionWS, ViewDirWS,_LightFade, attenMode, lightMode).xyz;
					return c;
				}
			ENDCG
		}
	}
	
	Fallback Off
}