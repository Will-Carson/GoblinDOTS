// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "MudBun/Mud Mesh Single-Textured (URP)"
{
	Properties
	{
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		_Color("Color", Color) = (1,1,1,1)
		_Emission("Emission", Color) = (1,1,1,1)
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_Smoothness("Smoothness", Range( 0 , 1)) = 1
		_AlphaCutoutThreshold("Alpha Cutout Threshold", Range( 0 , 1)) = 0
		_Dithering("Dithering", Range( 0 , 1)) = 1
		[Toggle]_UseTex0("Use Texture", Float) = 0
		_MainTex("Texture 0", 2D) = "white" {}
		[Toggle]_MainTexX("Project Texture 0 X", Float) = 1
		[Toggle]_MainTexY("Project Texture 0 Y", Float) = 1
		[Toggle]_MainTexZ("Project Texture 0 Z", Float) = 1

		[HideInInspector]_TransmissionShadow( "Transmission Shadow", Range( 0, 1 ) ) = 0.5
		[HideInInspector]_TransStrength( "Trans Strength", Range( 0, 50 ) ) = 1
		[HideInInspector]_TransNormal( "Trans Normal Distortion", Range( 0, 1 ) ) = 0.5
		[HideInInspector]_TransScattering( "Trans Scattering", Range( 1, 50 ) ) = 2
		[HideInInspector]_TransDirect( "Trans Direct", Range( 0, 1 ) ) = 0.9
		[HideInInspector]_TransAmbient( "Trans Ambient", Range( 0, 1 ) ) = 0.1
		[HideInInspector]_TransShadow( "Trans Shadow", Range( 0, 1 ) ) = 0.5
		[HideInInspector]_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		[HideInInspector]_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		[HideInInspector]_TessMin( "Tess Min Distance", Float ) = 10
		[HideInInspector]_TessMax( "Tess Max Distance", Float ) = 25
		[HideInInspector]_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		[HideInInspector]_TessMaxDisp( "Tess Max Displacement", Float ) = 25
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
		Cull Back
		HLSLINCLUDE
		#pragma target 4.0

		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}
		
		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }
			
			Blend One Zero , One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA
			

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _EMISSION
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_FORWARD

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			
			#if ASE_SRP_VERSION <= 70108
			#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
			#endif

			#define SHADER_GRAPH
			#include "../../../Shader/Render/ShaderCommon.cginc"
			#include "../../../Shader/Render/MeshCommon.cginc"


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord1 : TEXCOORD1;
				uint ase_vertexID : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 lightmapUVOrVertexSH : TEXCOORD0;
				half4 fogFactorAndVertexLight : TEXCOORD1;
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord : TEXCOORD2;
				#endif
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				float4 screenPos : TEXCOORD6;
				#endif
				float4 ase_texcoord7 : TEXCOORD7;
				float4 ase_texcoord8 : TEXCOORD8;
				float4 ase_texcoord9 : TEXCOORD9;
				float4 ase_texcoord10 : TEXCOORD10;
				float4 ase_texcoord11 : TEXCOORD11;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _UseTex0;
			float _MainTexX;
			float _MainTexY;
			float _MainTexZ;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _MainTex;


			float3 MudBunMeshPoint( int VertexID , out float3 PositionLs , out float3 NormalWs , out float3 NormalLs , out float4 Color , out float4 Emission , out float Metallic , out float Smoothness , out float4 TextureWeight )
			{
				float4 positionWs;
				float2 metallicSmoothness;
				mudbun_mesh_vert(VertexID, positionWs, PositionLs, NormalWs, NormalLs, Color, Emission, metallicSmoothness, TextureWeight);
				Metallic = metallicSmoothness.x;
				Smoothness = metallicSmoothness.y;
				return positionWs.xyz;
			}
			
			inline float Dither8x8Bayer( int x, int y )
			{
				const float dither[ 64 ] = {
			 1, 49, 13, 61,  4, 52, 16, 64,
			33, 17, 45, 29, 36, 20, 48, 32,
			 9, 57,  5, 53, 12, 60,  8, 56,
			41, 25, 37, 21, 44, 28, 40, 24,
			 3, 51, 15, 63,  2, 50, 14, 62,
			35, 19, 47, 31, 34, 18, 46, 30,
			11, 59,  7, 55, 10, 58,  6, 54,
			43, 27, 39, 23, 42, 26, 38, 22};
				int r = y * 8 + x;
				return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
			}
			

			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				int VertexID4_g83 = v.ase_vertexID;
				float3 PositionLs4_g83 = float3( 0,0,0 );
				float3 NormalWs4_g83 = float3( 0,0,0 );
				float3 NormalLs4_g83 = float3( 0,0,0 );
				float4 Color4_g83 = float4( 0,0,0,0 );
				float4 Emission4_g83 = float4( 0,0,0,0 );
				float Metallic4_g83 = 0.0;
				float Smoothness4_g83 = 0.0;
				float4 TextureWeight4_g83 = float4( 1,0,0,0 );
				float3 localMudBunMeshPoint4_g83 = MudBunMeshPoint( VertexID4_g83 , PositionLs4_g83 , NormalWs4_g83 , NormalLs4_g83 , Color4_g83 , Emission4_g83 , Metallic4_g83 , Smoothness4_g83 , TextureWeight4_g83 );
				
				float3 vertexToFrag213 = NormalLs4_g83;
				o.ase_texcoord7.xyz = vertexToFrag213;
				float3 vertexToFrag212 = PositionLs4_g83;
				o.ase_texcoord8.xyz = vertexToFrag212;
				float4 vertexToFrag5_g83 = Color4_g83;
				o.ase_texcoord9 = vertexToFrag5_g83;
				
				float4 vertexToFrag6_g83 = Emission4_g83;
				o.ase_texcoord10 = vertexToFrag6_g83;
				
				float vertexToFrag8_g83 = Metallic4_g83;
				o.ase_texcoord7.w = vertexToFrag8_g83;
				
				float vertexToFrag7_g83 = Smoothness4_g83;
				o.ase_texcoord8.w = vertexToFrag7_g83;
				
				float3 vertexToFrag16_g83 = localMudBunMeshPoint4_g83;
				o.ase_texcoord11.xyz = vertexToFrag16_g83;
				
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord11.w = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localMudBunMeshPoint4_g83;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = NormalWs4_g83;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float3 positionVS = TransformWorldToView( positionWS );
				float4 positionCS = TransformWorldToHClip( positionWS );

				VertexNormalInputs normalInput = GetVertexNormalInputs( v.ase_normal, v.ase_tangent );

				o.tSpace0 = float4( normalInput.normalWS, positionWS.x);
				o.tSpace1 = float4( normalInput.tangentWS, positionWS.y);
				o.tSpace2 = float4( normalInput.bitangentWS, positionWS.z);

				OUTPUT_LIGHTMAP_UV( v.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy );
				OUTPUT_SH( normalInput.normalWS.xyz, o.lightmapUVOrVertexSH.xyz );

				half3 vertexLight = VertexLighting( positionWS, normalInput.normalWS );
				#ifdef ASE_FOG
					half fogFactor = ComputeFogFactor( positionCS.z );
				#else
					half fogFactor = 0;
				#endif
				o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
				
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = positionWS;
				vertexInput.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				
				o.clipPos = positionCS;
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				o.screenPos = ComputeScreenPos(positionCS);
				#endif
				return o;
			}
			
			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord1 : TEXCOORD1;
				uint ase_vertexID : SV_VertexID;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.ase_tangent = v.ase_tangent;
				o.texcoord1 = v.texcoord1;
				o.vertex = v.vertex;
				o.ase_vertexID = v.ase_vertexID;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.ase_tangent = patch[0].ase_tangent * bary.x + patch[1].ase_tangent * bary.y + patch[2].ase_tangent * bary.z;
				o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_vertexID = patch[0].ase_vertexID * bary.x + patch[1].ase_vertexID * bary.y + patch[2].ase_vertexID * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag ( VertexOutput IN  ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				float3 WorldNormal = normalize( IN.tSpace0.xyz );
				float3 WorldTangent = IN.tSpace1.xyz;
				float3 WorldBiTangent = IN.tSpace2.xyz;
				float3 WorldPosition = float3(IN.tSpace0.w,IN.tSpace1.w,IN.tSpace2.w);
				float3 WorldViewDirection = _WorldSpaceCameraPos.xyz  - WorldPosition;
				float4 ShadowCoords = float4( 0, 0, 0, 0 );
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				float4 ScreenPos = IN.screenPos;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					ShadowCoords = IN.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
				#endif
	
				#if SHADER_HINT_NICE_QUALITY
					WorldViewDirection = SafeNormalize( WorldViewDirection );
				#endif

				float3 vertexToFrag213 = IN.ase_texcoord7.xyz;
				float3 appendResult238 = (float3(_MainTexX , _MainTexY , _MainTexZ));
				float3 temp_output_11_0_g226 = ( abs( vertexToFrag213 ) * appendResult238 );
				float3 break14_g226 = temp_output_11_0_g226;
				float3 vertexToFrag212 = IN.ase_texcoord8.xyz;
				float4 appendResult23_g226 = (float4(temp_output_11_0_g226 , 0.0));
				float4 appendResult24_g226 = (float4(temp_output_11_0_g226 , 1.0));
				float4 break10_g227 = ( ( break14_g226.x + break14_g226.y + break14_g226.z ) > 0.0 ? appendResult23_g226 : appendResult24_g226 );
				float4 color20_g226 = IsGammaSpace() ? float4(1,1,1,0) : float4(1,1,1,0);
				float4 color182 = IsGammaSpace() ? float4(1,1,1,1) : float4(1,1,1,1);
				float4 vertexToFrag5_g83 = IN.ase_texcoord9;
				float4 temp_output_25_0_g83 = ( _Color * vertexToFrag5_g83 );
				float4 temp_output_175_0 = ( ( _UseTex0 > 0.0 ? ( ( ( ( break14_g226.x > 0.0 ? tex2D( _MainTex, (vertexToFrag212).yz ) : float4( 0,0,0,0 ) ) * break10_g227.x ) + ( ( break14_g226.y > 0.0 ? tex2D( _MainTex, (vertexToFrag212).zx ) : float4( 0,0,0,0 ) ) * break10_g227.y ) + ( ( break14_g226.z > 0.0 ? tex2D( _MainTex, (vertexToFrag212).xy ) : float4( 0,0,0,0 ) ) * break10_g227.z ) + ( color20_g226 * break10_g227.w ) ) / ( break10_g227.x + break10_g227.y + break10_g227.z + break10_g227.w ) ) : color182 ) * temp_output_25_0_g83 );
				
				float4 vertexToFrag6_g83 = IN.ase_texcoord10;
				
				float vertexToFrag8_g83 = IN.ase_texcoord7.w;
				
				float vertexToFrag7_g83 = IN.ase_texcoord8.w;
				
				float temp_output_6_0_g228 = _AlphaCutoutThreshold;
				float3 vertexToFrag16_g83 = IN.ase_texcoord11.xyz;
				float4 unityObjectToClipPos17_g83 = TransformWorldToHClip(TransformObjectToWorld(vertexToFrag16_g83));
				float4 computeScreenPos18_g83 = ComputeScreenPos( unityObjectToClipPos17_g83 );
				float4 ditherCustomScreenPos1_g228 = float4( (( computeScreenPos18_g83 / (computeScreenPos18_g83).w )).xy, 0.0 , 0.0 );
				float2 clipScreen1_g228 = ditherCustomScreenPos1_g228.xy * _ScreenParams.xy;
				float dither1_g228 = Dither8x8Bayer( fmod(clipScreen1_g228.x, 8), fmod(clipScreen1_g228.y, 8) );
				float lerpResult4_g228 = lerp( temp_output_6_0_g228 , max( temp_output_6_0_g228 , ( dither1_g228 * 0.99 ) ) , _Dithering);
				
				float3 Albedo = temp_output_175_0.xyz;
				float3 Normal = float3(0, 0, 1);
				float3 Emission = ( _Emission * vertexToFrag6_g83 ).rgb;
				float3 Specular = 0.5;
				float Metallic = ( _Metallic * vertexToFrag8_g83 );
				float Smoothness = ( _Smoothness * vertexToFrag7_g83 );
				float Occlusion = 1;
				float Alpha = (temp_output_175_0).w;
				float AlphaClipThreshold = ( lerpResult4_g228 + -1E-16 );
				float3 BakedGI = 0;
				float3 RefractionColor = 1;
				float RefractionIndex = 1;
				float3 Transmission = 1;
				float3 Translucency = 1;

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				InputData inputData;
				inputData.positionWS = WorldPosition;
				inputData.viewDirectionWS = WorldViewDirection;
				inputData.shadowCoord = ShadowCoords;

				#ifdef _NORMALMAP
					inputData.normalWS = normalize(TransformTangentToWorld(Normal, half3x3( WorldTangent, WorldBiTangent, WorldNormal )));
				#else
					#if !SHADER_HINT_NICE_QUALITY
						inputData.normalWS = WorldNormal;
					#else
						inputData.normalWS = normalize( WorldNormal );
					#endif
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = IN.fogFactorAndVertexLight.x;
				#endif

				inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
				inputData.bakedGI = SAMPLE_GI( IN.lightmapUVOrVertexSH.xy, IN.lightmapUVOrVertexSH.xyz, inputData.normalWS );
				#ifdef _ASE_BAKEDGI
					inputData.bakedGI = BakedGI;
				#endif
				half4 color = UniversalFragmentPBR(
					inputData, 
					Albedo, 
					Metallic, 
					Specular, 
					Smoothness, 
					Occlusion, 
					Emission, 
					Alpha);

				#ifdef _TRANSMISSION_ASE
				{
					float shadow = _TransmissionShadow;

					Light mainLight = GetMainLight( inputData.shadowCoord );
					float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
					mainAtten = lerp( mainAtten, mainAtten * mainLight.shadowAttenuation, shadow );
					half3 mainTransmission = max(0 , -dot(inputData.normalWS, mainLight.direction)) * mainAtten * Transmission;
					color.rgb += Albedo * mainTransmission;

					#ifdef _ADDITIONAL_LIGHTS
						int transPixelLightCount = GetAdditionalLightsCount();
						for (int i = 0; i < transPixelLightCount; ++i)
						{
							Light light = GetAdditionalLight(i, inputData.positionWS);
							float3 atten = light.color * light.distanceAttenuation;
							atten = lerp( atten, atten * light.shadowAttenuation, shadow );

							half3 transmission = max(0 , -dot(inputData.normalWS, light.direction)) * atten * Transmission;
							color.rgb += Albedo * transmission;
						}
					#endif
				}
				#endif

				#ifdef _TRANSLUCENCY_ASE
				{
					float shadow = _TransShadow;
					float normal = _TransNormal;
					float scattering = _TransScattering;
					float direct = _TransDirect;
					float ambient = _TransAmbient;
					float strength = _TransStrength;

					Light mainLight = GetMainLight( inputData.shadowCoord );
					float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
					mainAtten = lerp( mainAtten, mainAtten * mainLight.shadowAttenuation, shadow );

					half3 mainLightDir = mainLight.direction + inputData.normalWS * normal;
					half mainVdotL = pow( saturate( dot( inputData.viewDirectionWS, -mainLightDir ) ), scattering );
					half3 mainTranslucency = mainAtten * ( mainVdotL * direct + inputData.bakedGI * ambient ) * Translucency;
					color.rgb += Albedo * mainTranslucency * strength;

					#ifdef _ADDITIONAL_LIGHTS
						int transPixelLightCount = GetAdditionalLightsCount();
						for (int i = 0; i < transPixelLightCount; ++i)
						{
							Light light = GetAdditionalLight(i, inputData.positionWS);
							float3 atten = light.color * light.distanceAttenuation;
							atten = lerp( atten, atten * light.shadowAttenuation, shadow );

							half3 lightDir = light.direction + inputData.normalWS * normal;
							half VdotL = pow( saturate( dot( inputData.viewDirectionWS, -lightDir ) ), scattering );
							half3 translucency = atten * ( VdotL * direct + inputData.bakedGI * ambient ) * Translucency;
							color.rgb += Albedo * translucency * strength;
						}
					#endif
				}
				#endif

				#ifdef _REFRACTION_ASE
					float4 projScreenPos = ScreenPos / ScreenPos.w;
					float3 refractionOffset = ( RefractionIndex - 1.0 ) * mul( UNITY_MATRIX_V, WorldNormal ).xyz * ( 1.0 - dot( WorldNormal, WorldViewDirection ) );
					projScreenPos.xy += refractionOffset.xy;
					float3 refraction = SHADERGRAPH_SAMPLE_SCENE_COLOR( projScreenPos ) * RefractionColor;
					color.rgb = lerp( refraction, color.rgb, color.a );
					color.a = 1;
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						color.rgb = MixFogColor(color.rgb, half3( 0, 0, 0 ), IN.fogFactorAndVertexLight.x );
					#else
						color.rgb = MixFog(color.rgb, IN.fogFactorAndVertexLight.x);
					#endif
				#endif
				
				return color;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "ShadowCaster"
			Tags { "LightMode"="ShadowCaster" }

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _EMISSION
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_SHADOWCASTER

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define SHADER_GRAPH
			#include "../../../Shader/Render/ShaderCommon.cginc"
			#include "../../../Shader/Render/MeshCommon.cginc"


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				uint ase_vertexID : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _UseTex0;
			float _MainTexX;
			float _MainTexY;
			float _MainTexZ;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _MainTex;


			float3 MudBunMeshPoint( int VertexID , out float3 PositionLs , out float3 NormalWs , out float3 NormalLs , out float4 Color , out float4 Emission , out float Metallic , out float Smoothness , out float4 TextureWeight )
			{
				float4 positionWs;
				float2 metallicSmoothness;
				mudbun_mesh_vert(VertexID, positionWs, PositionLs, NormalWs, NormalLs, Color, Emission, metallicSmoothness, TextureWeight);
				Metallic = metallicSmoothness.x;
				Smoothness = metallicSmoothness.y;
				return positionWs.xyz;
			}
			
			inline float Dither8x8Bayer( int x, int y )
			{
				const float dither[ 64 ] = {
			 1, 49, 13, 61,  4, 52, 16, 64,
			33, 17, 45, 29, 36, 20, 48, 32,
			 9, 57,  5, 53, 12, 60,  8, 56,
			41, 25, 37, 21, 44, 28, 40, 24,
			 3, 51, 15, 63,  2, 50, 14, 62,
			35, 19, 47, 31, 34, 18, 46, 30,
			11, 59,  7, 55, 10, 58,  6, 54,
			43, 27, 39, 23, 42, 26, 38, 22};
				int r = y * 8 + x;
				return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
			}
			

			float3 _LightDirection;

			VertexOutput VertexFunction( VertexInput v )
			{
				VertexOutput o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				int VertexID4_g83 = v.ase_vertexID;
				float3 PositionLs4_g83 = float3( 0,0,0 );
				float3 NormalWs4_g83 = float3( 0,0,0 );
				float3 NormalLs4_g83 = float3( 0,0,0 );
				float4 Color4_g83 = float4( 0,0,0,0 );
				float4 Emission4_g83 = float4( 0,0,0,0 );
				float Metallic4_g83 = 0.0;
				float Smoothness4_g83 = 0.0;
				float4 TextureWeight4_g83 = float4( 1,0,0,0 );
				float3 localMudBunMeshPoint4_g83 = MudBunMeshPoint( VertexID4_g83 , PositionLs4_g83 , NormalWs4_g83 , NormalLs4_g83 , Color4_g83 , Emission4_g83 , Metallic4_g83 , Smoothness4_g83 , TextureWeight4_g83 );
				
				float3 vertexToFrag213 = NormalLs4_g83;
				o.ase_texcoord2.xyz = vertexToFrag213;
				float3 vertexToFrag212 = PositionLs4_g83;
				o.ase_texcoord3.xyz = vertexToFrag212;
				float4 vertexToFrag5_g83 = Color4_g83;
				o.ase_texcoord4 = vertexToFrag5_g83;
				
				float3 vertexToFrag16_g83 = localMudBunMeshPoint4_g83;
				o.ase_texcoord5.xyz = vertexToFrag16_g83;
				
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.w = 0;
				o.ase_texcoord3.w = 0;
				o.ase_texcoord5.w = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localMudBunMeshPoint4_g83;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = NormalWs4_g83;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif
				float3 normalWS = TransformObjectToWorldDir(v.ase_normal);

				float4 clipPos = TransformWorldToHClip( ApplyShadowBias( positionWS, normalWS, _LightDirection ) );

				#if UNITY_REVERSED_Z
					clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#else
					clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				o.clipPos = clipPos;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				uint ase_vertexID : SV_VertexID;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.vertex = v.vertex;
				o.ase_vertexID = v.ase_vertexID;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_vertexID = patch[0].ase_vertexID * bary.x + patch[1].ase_vertexID * bary.y + patch[2].ase_vertexID * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );
				
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float3 vertexToFrag213 = IN.ase_texcoord2.xyz;
				float3 appendResult238 = (float3(_MainTexX , _MainTexY , _MainTexZ));
				float3 temp_output_11_0_g226 = ( abs( vertexToFrag213 ) * appendResult238 );
				float3 break14_g226 = temp_output_11_0_g226;
				float3 vertexToFrag212 = IN.ase_texcoord3.xyz;
				float4 appendResult23_g226 = (float4(temp_output_11_0_g226 , 0.0));
				float4 appendResult24_g226 = (float4(temp_output_11_0_g226 , 1.0));
				float4 break10_g227 = ( ( break14_g226.x + break14_g226.y + break14_g226.z ) > 0.0 ? appendResult23_g226 : appendResult24_g226 );
				float4 color20_g226 = IsGammaSpace() ? float4(1,1,1,0) : float4(1,1,1,0);
				float4 color182 = IsGammaSpace() ? float4(1,1,1,1) : float4(1,1,1,1);
				float4 vertexToFrag5_g83 = IN.ase_texcoord4;
				float4 temp_output_25_0_g83 = ( _Color * vertexToFrag5_g83 );
				float4 temp_output_175_0 = ( ( _UseTex0 > 0.0 ? ( ( ( ( break14_g226.x > 0.0 ? tex2D( _MainTex, (vertexToFrag212).yz ) : float4( 0,0,0,0 ) ) * break10_g227.x ) + ( ( break14_g226.y > 0.0 ? tex2D( _MainTex, (vertexToFrag212).zx ) : float4( 0,0,0,0 ) ) * break10_g227.y ) + ( ( break14_g226.z > 0.0 ? tex2D( _MainTex, (vertexToFrag212).xy ) : float4( 0,0,0,0 ) ) * break10_g227.z ) + ( color20_g226 * break10_g227.w ) ) / ( break10_g227.x + break10_g227.y + break10_g227.z + break10_g227.w ) ) : color182 ) * temp_output_25_0_g83 );
				
				float temp_output_6_0_g228 = _AlphaCutoutThreshold;
				float3 vertexToFrag16_g83 = IN.ase_texcoord5.xyz;
				float4 unityObjectToClipPos17_g83 = TransformWorldToHClip(TransformObjectToWorld(vertexToFrag16_g83));
				float4 computeScreenPos18_g83 = ComputeScreenPos( unityObjectToClipPos17_g83 );
				float4 ditherCustomScreenPos1_g228 = float4( (( computeScreenPos18_g83 / (computeScreenPos18_g83).w )).xy, 0.0 , 0.0 );
				float2 clipScreen1_g228 = ditherCustomScreenPos1_g228.xy * _ScreenParams.xy;
				float dither1_g228 = Dither8x8Bayer( fmod(clipScreen1_g228.x, 8), fmod(clipScreen1_g228.y, 8) );
				float lerpResult4_g228 = lerp( temp_output_6_0_g228 , max( temp_output_6_0_g228 , ( dither1_g228 * 0.99 ) ) , _Dithering);
				
				float Alpha = (temp_output_175_0).w;
				float AlphaClipThreshold = ( lerpResult4_g228 + -1E-16 );

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}

			ENDHLSL
		}

		
		Pass
		{
			
			Name "DepthOnly"
			Tags { "LightMode"="DepthOnly" }

			ZWrite On
			ColorMask 0

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _EMISSION
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_DEPTHONLY

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define SHADER_GRAPH
			#include "../../../Shader/Render/ShaderCommon.cginc"
			#include "../../../Shader/Render/MeshCommon.cginc"


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				uint ase_vertexID : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _UseTex0;
			float _MainTexX;
			float _MainTexY;
			float _MainTexZ;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _MainTex;


			float3 MudBunMeshPoint( int VertexID , out float3 PositionLs , out float3 NormalWs , out float3 NormalLs , out float4 Color , out float4 Emission , out float Metallic , out float Smoothness , out float4 TextureWeight )
			{
				float4 positionWs;
				float2 metallicSmoothness;
				mudbun_mesh_vert(VertexID, positionWs, PositionLs, NormalWs, NormalLs, Color, Emission, metallicSmoothness, TextureWeight);
				Metallic = metallicSmoothness.x;
				Smoothness = metallicSmoothness.y;
				return positionWs.xyz;
			}
			
			inline float Dither8x8Bayer( int x, int y )
			{
				const float dither[ 64 ] = {
			 1, 49, 13, 61,  4, 52, 16, 64,
			33, 17, 45, 29, 36, 20, 48, 32,
			 9, 57,  5, 53, 12, 60,  8, 56,
			41, 25, 37, 21, 44, 28, 40, 24,
			 3, 51, 15, 63,  2, 50, 14, 62,
			35, 19, 47, 31, 34, 18, 46, 30,
			11, 59,  7, 55, 10, 58,  6, 54,
			43, 27, 39, 23, 42, 26, 38, 22};
				int r = y * 8 + x;
				return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
			}
			

			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				int VertexID4_g83 = v.ase_vertexID;
				float3 PositionLs4_g83 = float3( 0,0,0 );
				float3 NormalWs4_g83 = float3( 0,0,0 );
				float3 NormalLs4_g83 = float3( 0,0,0 );
				float4 Color4_g83 = float4( 0,0,0,0 );
				float4 Emission4_g83 = float4( 0,0,0,0 );
				float Metallic4_g83 = 0.0;
				float Smoothness4_g83 = 0.0;
				float4 TextureWeight4_g83 = float4( 1,0,0,0 );
				float3 localMudBunMeshPoint4_g83 = MudBunMeshPoint( VertexID4_g83 , PositionLs4_g83 , NormalWs4_g83 , NormalLs4_g83 , Color4_g83 , Emission4_g83 , Metallic4_g83 , Smoothness4_g83 , TextureWeight4_g83 );
				
				float3 vertexToFrag213 = NormalLs4_g83;
				o.ase_texcoord2.xyz = vertexToFrag213;
				float3 vertexToFrag212 = PositionLs4_g83;
				o.ase_texcoord3.xyz = vertexToFrag212;
				float4 vertexToFrag5_g83 = Color4_g83;
				o.ase_texcoord4 = vertexToFrag5_g83;
				
				float3 vertexToFrag16_g83 = localMudBunMeshPoint4_g83;
				o.ase_texcoord5.xyz = vertexToFrag16_g83;
				
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.w = 0;
				o.ase_texcoord3.w = 0;
				o.ase_texcoord5.w = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localMudBunMeshPoint4_g83;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = NormalWs4_g83;
				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = positionCS;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				o.clipPos = positionCS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				uint ase_vertexID : SV_VertexID;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.vertex = v.vertex;
				o.ase_vertexID = v.ase_vertexID;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_vertexID = patch[0].ase_vertexID * bary.x + patch[1].ase_vertexID * bary.y + patch[2].ase_vertexID * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float3 vertexToFrag213 = IN.ase_texcoord2.xyz;
				float3 appendResult238 = (float3(_MainTexX , _MainTexY , _MainTexZ));
				float3 temp_output_11_0_g226 = ( abs( vertexToFrag213 ) * appendResult238 );
				float3 break14_g226 = temp_output_11_0_g226;
				float3 vertexToFrag212 = IN.ase_texcoord3.xyz;
				float4 appendResult23_g226 = (float4(temp_output_11_0_g226 , 0.0));
				float4 appendResult24_g226 = (float4(temp_output_11_0_g226 , 1.0));
				float4 break10_g227 = ( ( break14_g226.x + break14_g226.y + break14_g226.z ) > 0.0 ? appendResult23_g226 : appendResult24_g226 );
				float4 color20_g226 = IsGammaSpace() ? float4(1,1,1,0) : float4(1,1,1,0);
				float4 color182 = IsGammaSpace() ? float4(1,1,1,1) : float4(1,1,1,1);
				float4 vertexToFrag5_g83 = IN.ase_texcoord4;
				float4 temp_output_25_0_g83 = ( _Color * vertexToFrag5_g83 );
				float4 temp_output_175_0 = ( ( _UseTex0 > 0.0 ? ( ( ( ( break14_g226.x > 0.0 ? tex2D( _MainTex, (vertexToFrag212).yz ) : float4( 0,0,0,0 ) ) * break10_g227.x ) + ( ( break14_g226.y > 0.0 ? tex2D( _MainTex, (vertexToFrag212).zx ) : float4( 0,0,0,0 ) ) * break10_g227.y ) + ( ( break14_g226.z > 0.0 ? tex2D( _MainTex, (vertexToFrag212).xy ) : float4( 0,0,0,0 ) ) * break10_g227.z ) + ( color20_g226 * break10_g227.w ) ) / ( break10_g227.x + break10_g227.y + break10_g227.z + break10_g227.w ) ) : color182 ) * temp_output_25_0_g83 );
				
				float temp_output_6_0_g228 = _AlphaCutoutThreshold;
				float3 vertexToFrag16_g83 = IN.ase_texcoord5.xyz;
				float4 unityObjectToClipPos17_g83 = TransformWorldToHClip(TransformObjectToWorld(vertexToFrag16_g83));
				float4 computeScreenPos18_g83 = ComputeScreenPos( unityObjectToClipPos17_g83 );
				float4 ditherCustomScreenPos1_g228 = float4( (( computeScreenPos18_g83 / (computeScreenPos18_g83).w )).xy, 0.0 , 0.0 );
				float2 clipScreen1_g228 = ditherCustomScreenPos1_g228.xy * _ScreenParams.xy;
				float dither1_g228 = Dither8x8Bayer( fmod(clipScreen1_g228.x, 8), fmod(clipScreen1_g228.y, 8) );
				float lerpResult4_g228 = lerp( temp_output_6_0_g228 , max( temp_output_6_0_g228 , ( dither1_g228 * 0.99 ) ) , _Dithering);
				
				float Alpha = (temp_output_175_0).w;
				float AlphaClipThreshold = ( lerpResult4_g228 + -1E-16 );

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif
				return 0;
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Meta"
			Tags { "LightMode"="Meta" }

			Cull Off

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _EMISSION
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 999999

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_META

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#define SHADER_GRAPH
			#include "../../../Shader/Render/ShaderCommon.cginc"
			#include "../../../Shader/Render/MeshCommon.cginc"


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				uint ase_vertexID : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _UseTex0;
			float _MainTexX;
			float _MainTexY;
			float _MainTexZ;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _MainTex;


			float3 MudBunMeshPoint( int VertexID , out float3 PositionLs , out float3 NormalWs , out float3 NormalLs , out float4 Color , out float4 Emission , out float Metallic , out float Smoothness , out float4 TextureWeight )
			{
				float4 positionWs;
				float2 metallicSmoothness;
				mudbun_mesh_vert(VertexID, positionWs, PositionLs, NormalWs, NormalLs, Color, Emission, metallicSmoothness, TextureWeight);
				Metallic = metallicSmoothness.x;
				Smoothness = metallicSmoothness.y;
				return positionWs.xyz;
			}
			
			inline float Dither8x8Bayer( int x, int y )
			{
				const float dither[ 64 ] = {
			 1, 49, 13, 61,  4, 52, 16, 64,
			33, 17, 45, 29, 36, 20, 48, 32,
			 9, 57,  5, 53, 12, 60,  8, 56,
			41, 25, 37, 21, 44, 28, 40, 24,
			 3, 51, 15, 63,  2, 50, 14, 62,
			35, 19, 47, 31, 34, 18, 46, 30,
			11, 59,  7, 55, 10, 58,  6, 54,
			43, 27, 39, 23, 42, 26, 38, 22};
				int r = y * 8 + x;
				return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
			}
			

			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				int VertexID4_g83 = v.ase_vertexID;
				float3 PositionLs4_g83 = float3( 0,0,0 );
				float3 NormalWs4_g83 = float3( 0,0,0 );
				float3 NormalLs4_g83 = float3( 0,0,0 );
				float4 Color4_g83 = float4( 0,0,0,0 );
				float4 Emission4_g83 = float4( 0,0,0,0 );
				float Metallic4_g83 = 0.0;
				float Smoothness4_g83 = 0.0;
				float4 TextureWeight4_g83 = float4( 1,0,0,0 );
				float3 localMudBunMeshPoint4_g83 = MudBunMeshPoint( VertexID4_g83 , PositionLs4_g83 , NormalWs4_g83 , NormalLs4_g83 , Color4_g83 , Emission4_g83 , Metallic4_g83 , Smoothness4_g83 , TextureWeight4_g83 );
				
				float3 vertexToFrag213 = NormalLs4_g83;
				o.ase_texcoord2.xyz = vertexToFrag213;
				float3 vertexToFrag212 = PositionLs4_g83;
				o.ase_texcoord3.xyz = vertexToFrag212;
				float4 vertexToFrag5_g83 = Color4_g83;
				o.ase_texcoord4 = vertexToFrag5_g83;
				
				float4 vertexToFrag6_g83 = Emission4_g83;
				o.ase_texcoord5 = vertexToFrag6_g83;
				
				float3 vertexToFrag16_g83 = localMudBunMeshPoint4_g83;
				o.ase_texcoord6.xyz = vertexToFrag16_g83;
				
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.w = 0;
				o.ase_texcoord3.w = 0;
				o.ase_texcoord6.w = 0;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localMudBunMeshPoint4_g83;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = NormalWs4_g83;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				o.clipPos = MetaVertexPosition( v.vertex, v.texcoord1.xy, v.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST );
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = o.clipPos;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord2 : TEXCOORD2;
				uint ase_vertexID : SV_VertexID;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.texcoord1 = v.texcoord1;
				o.texcoord2 = v.texcoord2;
				o.vertex = v.vertex;
				o.ase_vertexID = v.ase_vertexID;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.texcoord1 = patch[0].texcoord1 * bary.x + patch[1].texcoord1 * bary.y + patch[2].texcoord1 * bary.z;
				o.texcoord2 = patch[0].texcoord2 * bary.x + patch[1].texcoord2 * bary.y + patch[2].texcoord2 * bary.z;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_vertexID = patch[0].ase_vertexID * bary.x + patch[1].ase_vertexID * bary.y + patch[2].ase_vertexID * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float3 vertexToFrag213 = IN.ase_texcoord2.xyz;
				float3 appendResult238 = (float3(_MainTexX , _MainTexY , _MainTexZ));
				float3 temp_output_11_0_g226 = ( abs( vertexToFrag213 ) * appendResult238 );
				float3 break14_g226 = temp_output_11_0_g226;
				float3 vertexToFrag212 = IN.ase_texcoord3.xyz;
				float4 appendResult23_g226 = (float4(temp_output_11_0_g226 , 0.0));
				float4 appendResult24_g226 = (float4(temp_output_11_0_g226 , 1.0));
				float4 break10_g227 = ( ( break14_g226.x + break14_g226.y + break14_g226.z ) > 0.0 ? appendResult23_g226 : appendResult24_g226 );
				float4 color20_g226 = IsGammaSpace() ? float4(1,1,1,0) : float4(1,1,1,0);
				float4 color182 = IsGammaSpace() ? float4(1,1,1,1) : float4(1,1,1,1);
				float4 vertexToFrag5_g83 = IN.ase_texcoord4;
				float4 temp_output_25_0_g83 = ( _Color * vertexToFrag5_g83 );
				float4 temp_output_175_0 = ( ( _UseTex0 > 0.0 ? ( ( ( ( break14_g226.x > 0.0 ? tex2D( _MainTex, (vertexToFrag212).yz ) : float4( 0,0,0,0 ) ) * break10_g227.x ) + ( ( break14_g226.y > 0.0 ? tex2D( _MainTex, (vertexToFrag212).zx ) : float4( 0,0,0,0 ) ) * break10_g227.y ) + ( ( break14_g226.z > 0.0 ? tex2D( _MainTex, (vertexToFrag212).xy ) : float4( 0,0,0,0 ) ) * break10_g227.z ) + ( color20_g226 * break10_g227.w ) ) / ( break10_g227.x + break10_g227.y + break10_g227.z + break10_g227.w ) ) : color182 ) * temp_output_25_0_g83 );
				
				float4 vertexToFrag6_g83 = IN.ase_texcoord5;
				
				float temp_output_6_0_g228 = _AlphaCutoutThreshold;
				float3 vertexToFrag16_g83 = IN.ase_texcoord6.xyz;
				float4 unityObjectToClipPos17_g83 = TransformWorldToHClip(TransformObjectToWorld(vertexToFrag16_g83));
				float4 computeScreenPos18_g83 = ComputeScreenPos( unityObjectToClipPos17_g83 );
				float4 ditherCustomScreenPos1_g228 = float4( (( computeScreenPos18_g83 / (computeScreenPos18_g83).w )).xy, 0.0 , 0.0 );
				float2 clipScreen1_g228 = ditherCustomScreenPos1_g228.xy * _ScreenParams.xy;
				float dither1_g228 = Dither8x8Bayer( fmod(clipScreen1_g228.x, 8), fmod(clipScreen1_g228.y, 8) );
				float lerpResult4_g228 = lerp( temp_output_6_0_g228 , max( temp_output_6_0_g228 , ( dither1_g228 * 0.99 ) ) , _Dithering);
				
				
				float3 Albedo = temp_output_175_0.xyz;
				float3 Emission = ( _Emission * vertexToFrag6_g83 ).rgb;
				float Alpha = (temp_output_175_0).w;
				float AlphaClipThreshold = ( lerpResult4_g228 + -1E-16 );

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				MetaInput metaInput = (MetaInput)0;
				metaInput.Albedo = Albedo;
				metaInput.Emission = Emission;
				
				return MetaFragment(metaInput);
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Universal2D"
			Tags { "LightMode"="Universal2D" }

			Blend One Zero , One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			HLSLPROGRAM
			#pragma multi_compile_instancing
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define ASE_ABSOLUTE_VERTEX_POS 1
			#define _EMISSION
			#define _ALPHATEST_ON 1
			#define ASE_SRP_VERSION 999999

			#pragma enable_d3d11_debug_symbols
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_2D

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			
			#define SHADER_GRAPH
			#include "../../../Shader/Render/ShaderCommon.cginc"
			#include "../../../Shader/Render/MeshCommon.cginc"


			#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				uint ase_vertexID : SV_VertexID;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 worldPos : TEXCOORD0;
				#endif
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
				float4 shadowCoord : TEXCOORD1;
				#endif
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float _UseTex0;
			float _MainTexX;
			float _MainTexY;
			float _MainTexZ;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			#ifdef TESSELLATION_ON
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END
			sampler2D _MainTex;


			float3 MudBunMeshPoint( int VertexID , out float3 PositionLs , out float3 NormalWs , out float3 NormalLs , out float4 Color , out float4 Emission , out float Metallic , out float Smoothness , out float4 TextureWeight )
			{
				float4 positionWs;
				float2 metallicSmoothness;
				mudbun_mesh_vert(VertexID, positionWs, PositionLs, NormalWs, NormalLs, Color, Emission, metallicSmoothness, TextureWeight);
				Metallic = metallicSmoothness.x;
				Smoothness = metallicSmoothness.y;
				return positionWs.xyz;
			}
			
			inline float Dither8x8Bayer( int x, int y )
			{
				const float dither[ 64 ] = {
			 1, 49, 13, 61,  4, 52, 16, 64,
			33, 17, 45, 29, 36, 20, 48, 32,
			 9, 57,  5, 53, 12, 60,  8, 56,
			41, 25, 37, 21, 44, 28, 40, 24,
			 3, 51, 15, 63,  2, 50, 14, 62,
			35, 19, 47, 31, 34, 18, 46, 30,
			11, 59,  7, 55, 10, 58,  6, 54,
			43, 27, 39, 23, 42, 26, 38, 22};
				int r = y * 8 + x;
				return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
			}
			

			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

				int VertexID4_g83 = v.ase_vertexID;
				float3 PositionLs4_g83 = float3( 0,0,0 );
				float3 NormalWs4_g83 = float3( 0,0,0 );
				float3 NormalLs4_g83 = float3( 0,0,0 );
				float4 Color4_g83 = float4( 0,0,0,0 );
				float4 Emission4_g83 = float4( 0,0,0,0 );
				float Metallic4_g83 = 0.0;
				float Smoothness4_g83 = 0.0;
				float4 TextureWeight4_g83 = float4( 1,0,0,0 );
				float3 localMudBunMeshPoint4_g83 = MudBunMeshPoint( VertexID4_g83 , PositionLs4_g83 , NormalWs4_g83 , NormalLs4_g83 , Color4_g83 , Emission4_g83 , Metallic4_g83 , Smoothness4_g83 , TextureWeight4_g83 );
				
				float3 vertexToFrag213 = NormalLs4_g83;
				o.ase_texcoord2.xyz = vertexToFrag213;
				float3 vertexToFrag212 = PositionLs4_g83;
				o.ase_texcoord3.xyz = vertexToFrag212;
				float4 vertexToFrag5_g83 = Color4_g83;
				o.ase_texcoord4 = vertexToFrag5_g83;
				
				float3 vertexToFrag16_g83 = localMudBunMeshPoint4_g83;
				o.ase_texcoord5.xyz = vertexToFrag16_g83;
				
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord2.w = 0;
				o.ase_texcoord3.w = 0;
				o.ase_texcoord5.w = 0;
				
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = localMudBunMeshPoint4_g83;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif

				v.ase_normal = NormalWs4_g83;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float4 positionCS = TransformWorldToHClip( positionWS );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				o.worldPos = positionWS;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					VertexPositionInputs vertexInput = (VertexPositionInputs)0;
					vertexInput.positionWS = positionWS;
					vertexInput.positionCS = positionCS;
					o.shadowCoord = GetShadowCoord( vertexInput );
				#endif

				o.clipPos = positionCS;
				return o;
			}

			#if defined(TESSELLATION_ON)
			struct VertexControl
			{
				float4 vertex : INTERNALTESSPOS;
				float3 ase_normal : NORMAL;
				uint ase_vertexID : SV_VertexID;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( VertexInput v )
			{
				VertexControl o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				o.vertex = v.vertex;
				o.ase_normal = v.ase_normal;
				o.vertex = v.vertex;
				o.ase_vertexID = v.ase_vertexID;
				return o;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> v)
			{
				TessellationFactors o;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(v[0].vertex, v[1].vertex, v[2].vertex, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
				return o;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
			   return patch[id];
			}

			[domain("tri")]
			VertexOutput DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				VertexInput o = (VertexInput) 0;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_normal = patch[0].ase_normal * bary.x + patch[1].ase_normal * bary.y + patch[2].ase_normal * bary.z;
				o.vertex = patch[0].vertex * bary.x + patch[1].vertex * bary.y + patch[2].vertex * bary.z;
				o.ase_vertexID = patch[0].ase_vertexID * bary.x + patch[1].ase_vertexID * bary.y + patch[2].ase_vertexID * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = o.vertex.xyz - patch[i].ase_normal * (dot(o.vertex.xyz, patch[i].ase_normal) - dot(patch[i].vertex.xyz, patch[i].ase_normal));
				float phongStrength = _TessPhongStrength;
				o.vertex.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * o.vertex.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], o);
				return VertexFunction(o);
			}
			#else
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}
			#endif

			half4 frag(VertexOutput IN  ) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

				#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
				float3 WorldPosition = IN.worldPos;
				#endif
				float4 ShadowCoords = float4( 0, 0, 0, 0 );

				#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
						ShadowCoords = IN.shadowCoord;
					#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
						ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
					#endif
				#endif

				float3 vertexToFrag213 = IN.ase_texcoord2.xyz;
				float3 appendResult238 = (float3(_MainTexX , _MainTexY , _MainTexZ));
				float3 temp_output_11_0_g226 = ( abs( vertexToFrag213 ) * appendResult238 );
				float3 break14_g226 = temp_output_11_0_g226;
				float3 vertexToFrag212 = IN.ase_texcoord3.xyz;
				float4 appendResult23_g226 = (float4(temp_output_11_0_g226 , 0.0));
				float4 appendResult24_g226 = (float4(temp_output_11_0_g226 , 1.0));
				float4 break10_g227 = ( ( break14_g226.x + break14_g226.y + break14_g226.z ) > 0.0 ? appendResult23_g226 : appendResult24_g226 );
				float4 color20_g226 = IsGammaSpace() ? float4(1,1,1,0) : float4(1,1,1,0);
				float4 color182 = IsGammaSpace() ? float4(1,1,1,1) : float4(1,1,1,1);
				float4 vertexToFrag5_g83 = IN.ase_texcoord4;
				float4 temp_output_25_0_g83 = ( _Color * vertexToFrag5_g83 );
				float4 temp_output_175_0 = ( ( _UseTex0 > 0.0 ? ( ( ( ( break14_g226.x > 0.0 ? tex2D( _MainTex, (vertexToFrag212).yz ) : float4( 0,0,0,0 ) ) * break10_g227.x ) + ( ( break14_g226.y > 0.0 ? tex2D( _MainTex, (vertexToFrag212).zx ) : float4( 0,0,0,0 ) ) * break10_g227.y ) + ( ( break14_g226.z > 0.0 ? tex2D( _MainTex, (vertexToFrag212).xy ) : float4( 0,0,0,0 ) ) * break10_g227.z ) + ( color20_g226 * break10_g227.w ) ) / ( break10_g227.x + break10_g227.y + break10_g227.z + break10_g227.w ) ) : color182 ) * temp_output_25_0_g83 );
				
				float temp_output_6_0_g228 = _AlphaCutoutThreshold;
				float3 vertexToFrag16_g83 = IN.ase_texcoord5.xyz;
				float4 unityObjectToClipPos17_g83 = TransformWorldToHClip(TransformObjectToWorld(vertexToFrag16_g83));
				float4 computeScreenPos18_g83 = ComputeScreenPos( unityObjectToClipPos17_g83 );
				float4 ditherCustomScreenPos1_g228 = float4( (( computeScreenPos18_g83 / (computeScreenPos18_g83).w )).xy, 0.0 , 0.0 );
				float2 clipScreen1_g228 = ditherCustomScreenPos1_g228.xy * _ScreenParams.xy;
				float dither1_g228 = Dither8x8Bayer( fmod(clipScreen1_g228.x, 8), fmod(clipScreen1_g228.y, 8) );
				float lerpResult4_g228 = lerp( temp_output_6_0_g228 , max( temp_output_6_0_g228 , ( dither1_g228 * 0.99 ) ) , _Dithering);
				
				
				float3 Albedo = temp_output_175_0.xyz;
				float Alpha = (temp_output_175_0).w;
				float AlphaClipThreshold = ( lerpResult4_g228 + -1E-16 );

				half4 color = half4( Albedo, Alpha );

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				return color;
			}
			ENDHLSL
		}
		
	}
	/*ase_lod*/
	CustomEditor "MudBun.MudMeshSingleTexturedMaterialEditor"
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=18100
-1816;85;1682;872;1189.685;1250.006;2.043708;True;False
Node;AmplifyShaderEditor.FunctionNode;198;-128,-192;Inherit;False;Mud Mesh;0;;83;4f444db5091a94140ab2b15b933d37b6;0;0;11;COLOR;9;FLOAT;13;COLOR;10;FLOAT;11;FLOAT;12;FLOAT4;33;FLOAT3;0;FLOAT3;32;FLOAT3;2;FLOAT3;31;FLOAT2;15
Node;AmplifyShaderEditor.RangedFloatNode;185;128,-352;Inherit;False;Property;_MainTexZ;Project Texture 0 Z;11;1;[Toggle];Create;False;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexToFragmentNode;213;384,-176;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;184;128,-432;Inherit;False;Property;_MainTexY;Project Texture 0 Y;10;1;[Toggle];Create;False;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;183;128,-512;Inherit;False;Property;_MainTexX;Project Texture 0 X;9;1;[Toggle];Create;False;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexToFragmentNode;212;384,-256;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.AbsOpNode;236;768,-608;Inherit;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DynamicAppendNode;238;450.062,-502.8722;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TexturePropertyNode;146;384,-1072;Inherit;True;Property;_MainTex;Texture 0;8;0;Create;False;0;0;False;0;False;None;None;False;white;Auto;Texture2D;-1;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SwizzleNode;227;768,-800;Inherit;False;FLOAT2;2;0;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;228;768,-704;Inherit;False;FLOAT2;0;1;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SwizzleNode;230;768,-896;Inherit;False;FLOAT2;1;2;2;3;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;237;896,-608;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;178;384,-1152;Inherit;False;Property;_UseTex0;Use Texture;7;1;[Toggle];Create;False;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;182;1136,-816;Inherit;False;Constant;_White;White;26;0;Create;True;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;232;1136,-1040;Inherit;False;Mud Triplanar Sample;-1;;226;d9088f0d6015c424b98757b174010394;0;5;3;SAMPLER2D;0,0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;11;FLOAT3;0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.Compare;231;1392,-1152;Inherit;False;2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT4;0,0,0,0;False;3;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;64;-128,128;Inherit;False;Property;_AlphaCutoutThreshold;Alpha Cutout Threshold;5;0;Fetch;False;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;65;-128,224;Inherit;False;Property;_Dithering;Dithering;6;0;Fetch;False;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;175;1664,-768;Inherit;True;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.FunctionNode;125;240,80;Inherit;False;Mud Alpha Threshold;-1;;228;926535703f4c32948ac1f55275a22bf0;0;3;8;FLOAT2;0,0;False;6;FLOAT;0;False;7;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;176;1920,-672;Inherit;False;False;False;False;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;222;2944,-960;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Meta;0;4;Meta;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;True;2;False;-1;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;219;2176,-704;Float;False;True;-1;2;MudBun.MudMeshSingleTexturedMaterialEditor;0;2;MudBun/Mud Mesh Single-Textured (URP);94348b07e5e8bab40bd6c8a1e3df54cd;True;Forward;0;1;Forward;16;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;4;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;33;Workflow;1;Surface;0;  Refraction Model;0;  Blend;0;Two Sided;1;Transmission;0;  Transmission Shadow;0.5,False,-1;Translucency;0;  Translucency Strength;1,False,-1;  Normal Distortion;0.5,False,-1;  Scattering;2,False,-1;  Direct;0.9,False,-1;  Ambient;0.1,False,-1;  Shadow;0.5,False,-1;Cast Shadows;1;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;1;Built-in Fog;1;Meta Pass;1;Override Baked GI;0;Extra Pre Pass;0;DOTS Instancing;0;Tessellation;0;  Phong;0;  Strength;0.5,False,-1;  Type;0;  Tess;16,False,-1;  Min;10,False,-1;  Max;25,False,-1;  Edge Length;16,False,-1;  Max Displacement;25,False,-1;Vertex Position,InvertActionOnDeselection;0;0;6;False;True;True;True;True;True;False;;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;218;2432,-704;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;True;0;False;-1;True;True;True;True;True;0;False;-1;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;220;2944,-960;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;221;2944,-960;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthOnly;0;3;DepthOnly;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;True;False;False;False;False;0;False;-1;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;223;2944,-960;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;1;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Universal2D;0;5;Universal2D;0;False;False;False;True;0;False;-1;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;True;1;1;False;-1;0;False;-1;1;1;False;-1;0;False;-1;False;False;False;True;True;True;True;True;0;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;1;LightMode=Universal2D;False;0;Hidden/InternalErrorShader;0;0;Standard;0;0
WireConnection;213;0;198;31
WireConnection;212;0;198;32
WireConnection;236;0;213;0
WireConnection;238;0;183;0
WireConnection;238;1;184;0
WireConnection;238;2;185;0
WireConnection;227;0;212;0
WireConnection;228;0;212;0
WireConnection;230;0;212;0
WireConnection;237;0;236;0
WireConnection;237;1;238;0
WireConnection;232;3;146;0
WireConnection;232;4;230;0
WireConnection;232;5;227;0
WireConnection;232;6;228;0
WireConnection;232;11;237;0
WireConnection;231;0;178;0
WireConnection;231;2;232;0
WireConnection;231;3;182;0
WireConnection;175;0;231;0
WireConnection;175;1;198;9
WireConnection;125;8;198;15
WireConnection;125;6;64;0
WireConnection;125;7;65;0
WireConnection;176;0;175;0
WireConnection;219;0;175;0
WireConnection;219;2;198;10
WireConnection;219;3;198;11
WireConnection;219;4;198;12
WireConnection;219;6;176;0
WireConnection;219;7;125;0
WireConnection;219;8;198;0
WireConnection;219;10;198;2
ASEEND*/
//CHKSM=9676C24E054C09C768F0DACAE7B8316269996F5C