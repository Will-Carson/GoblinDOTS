// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Mud Standard Mesh (Built-In RP)"
{
	Properties
	{
		_AlphaCutoutThreshold("Alpha Cutout Threshold", Range( 0 , 1)) = 0
		_Dithering("Dithering", Range( 0 , 1)) = 1
		[HideInInspector] _tex3coord( "", 2D ) = "white" {}
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.5
		#pragma exclude_renderers d3d9 gles xbox360 psp2 n3ds wiiu 
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 vertexColor : COLOR;
			float3 vertexToFrag20_g1;
			float3 uv_tex3coord;
			float2 uv2_texcoord2;
		};

		uniform float _AlphaCutoutThreshold;
		uniform float _Dithering;


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


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_vertex3Pos = v.vertex.xyz;
			v.vertex.xyz = ase_vertex3Pos;
			float3 ase_vertexNormal = v.normal.xyz;
			v.normal = ase_vertexNormal;
			o.vertexToFrag20_g1 = ase_vertex3Pos;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float temp_output_6_0_g2 = _AlphaCutoutThreshold;
			float4 unityObjectToClipPos19_g1 = UnityObjectToClipPos( i.vertexToFrag20_g1 );
			float4 computeScreenPos18_g1 = ComputeScreenPos( unityObjectToClipPos19_g1 );
			float4 ditherCustomScreenPos1_g2 = float4( (( computeScreenPos18_g1 / (computeScreenPos18_g1).w )).xy, 0.0 , 0.0 );
			float2 clipScreen1_g2 = ditherCustomScreenPos1_g2.xy * _ScreenParams.xy;
			float dither1_g2 = Dither8x8Bayer( fmod(clipScreen1_g2.x, 8), fmod(clipScreen1_g2.y, 8) );
			float lerpResult4_g2 = lerp( temp_output_6_0_g2 , max( temp_output_6_0_g2 , ( dither1_g2 * 0.99 ) ) , _Dithering);
			clip( i.vertexColor.a - ( lerpResult4_g2 + -1E-16 ));
			o.Albedo = (i.vertexColor).rgb;
			o.Emission = i.uv_tex3coord;
			o.Metallic = i.uv2_texcoord2.x;
			o.Smoothness = i.uv2_texcoord2.y;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
}
/*ASEBEGIN
Version=18100
-1862;140;1682;872;1006.769;340.0937;1.375106;True;False
Node;AmplifyShaderEditor.RangedFloatNode;2;-256,256;Inherit;False;Property;_Dithering;Dithering;1;0;Create;True;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;3;-256,160;Inherit;False;Property;_AlphaCutoutThreshold;Alpha Cutout Threshold;0;0;Create;True;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;8;-256,-128;Inherit;False;Mud Generated Standard Mesh;-1;;1;7b8a07bde06607c4284a51a0d43ac96d;0;0;8;FLOAT3;0;FLOAT;3;FLOAT3;2;FLOAT;4;FLOAT;5;FLOAT3;6;FLOAT3;1;FLOAT2;7
Node;AmplifyShaderEditor.FunctionNode;4;80,176;Inherit;False;Mud Alpha Threshold;-1;;2;926535703f4c32948ac1f55275a22bf0;0;3;8;FLOAT2;0,0;False;6;FLOAT;0;False;7;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClipNode;6;464,32;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;720,-128;Float;False;True;-1;3;;0;0;Standard;Mud Standard Mesh (Built-In RP);False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;8;d3d11_9x;d3d11;glcore;gles3;metal;vulkan;xboxone;ps4;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Absolute;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;True;3;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;4;8;8;7
WireConnection;4;6;3;0
WireConnection;4;7;2;0
WireConnection;6;0;8;0
WireConnection;6;1;8;3
WireConnection;6;2;4;0
WireConnection;0;0;6;0
WireConnection;0;2;8;2
WireConnection;0;3;8;4
WireConnection;0;4;8;5
WireConnection;0;11;8;6
WireConnection;0;12;8;1
ASEEND*/
//CHKSM=D448E61F05458A4C428926CC52B88E75670C64F9