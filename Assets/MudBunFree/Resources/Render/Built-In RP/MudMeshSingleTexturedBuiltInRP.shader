/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

Shader "MudBun/Mud Mesh Single-Textured (Built-In RP)"
{
  Properties
  {
    _AlphaCutoutThreshold("Alpha Cutout Threshold", Range(0.0, 1.0)) = 0.5
    _Dithering("Dithering", Range(0.0, 1.0)) = 0.0

    [Toggle] _UseTex0("Use Texture", Int) = 0
      _MainTex("Albedo", 2D) = "white" {}
      [Toggle] _MainTexX("     X Axis Projection", Int) = 1
      [Toggle] _MainTexY("     Y Axis Projection", Int) = 1
      [Toggle] _MainTexZ("     Z Axis Projection", Int) = 1
  }
  SubShader
  {
    ZWrite On
    Cull Back
    Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

    CGPROGRAM

    #define MUDBUN_BUILT_IN_RP (1)
    #pragma multi_compile_instancing
    #pragma surface surf Standard vertex:vert addshadow fullforwardshadows
    #pragma target 3.5

    #include "UnityCG.cginc"

    #include "../../../Shader/Render/ShaderCommon.cginc"

    #if MUDBUN_VALID
      #include "../../../Shader/Render/MeshCommon.cginc"
    #endif

    void vert(inout Vertex i, out Input o)
    {
      UNITY_INITIALIZE_OUTPUT(Input, o);

      #if MUDBUN_VALID
        mudbun_mesh_vert(i.id, i.vertex, o.localPos, i.normal, o.localNorm, i.color, o.emission, o.metallicSmoothness, o.texWeight);
      #endif
    }

    void surf(Input i, inout SurfaceOutputStandard o)
    {
      float4 color = 1.0f;
      float4 texColor = 0.0f;
      float totalWeight = 0.0f;

      float3 triWeight = abs(i.localNorm);

      if (_UseTex0)
      {
        texColor += tex2D_triplanar(_MainTex, _MainTex_ST, triWeight, i.localPos, _MainTexX, _MainTexY, _MainTexZ);
        totalWeight += 1.0f;
      }

      if (totalWeight > 0.0f)
      {
        color = texColor / totalWeight;
      }

      applyOpaqueTransparency(i, i.color.a * _Color.a * color.a);

      o.Albedo = i.color.rgb * _Color.rgb * color.rgb;
      o.Emission = float4(i.emission.rgb, 1.0f)  * _Emission;
      o.Metallic = i.metallicSmoothness.x * _Metallic;
      o.Smoothness = i.metallicSmoothness.y * _Smoothness;
    }

    ENDCG
  }

  CustomEditor "MudBun.MudMeshSingleTexturedMaterialEditor"
}
