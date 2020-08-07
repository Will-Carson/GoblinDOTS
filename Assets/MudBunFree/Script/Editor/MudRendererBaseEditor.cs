/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

using System;
using UnityEditor;
using UnityEngine;

namespace MudBun
{
  [CustomEditor(typeof(MudRendererBase))]
  [CanEditMultipleObjects]
  public class MudRendererBaseEditor : MudEditorBase
  {
    protected SerializedProperty MaxVoxelsK;
    protected SerializedProperty MaxChunks;
    protected SerializedProperty ShowGpuMemoryUsage;
    protected SerializedProperty AutoAdjustBudgetsToHighWaterMarks;
    protected SerializedProperty AutoAdjustBudgetsToHighWaterMarksMarginPercent;

    protected SerializedProperty VoxelDensity;
    protected SerializedProperty RenderMode;
    protected SerializedProperty SplatSize;
    protected SerializedProperty SplatRotation;
    protected SerializedProperty SplatRotationNoisiness;
    protected SerializedProperty SplatCameraFacing;
    protected SerializedProperty CastShadows;
    protected SerializedProperty ReceiveShadows;
    
    protected SerializedProperty SharedMaterial;
    protected SerializedProperty MasterColor;
    protected SerializedProperty MasterEmission;
    protected SerializedProperty MasterMetallic;
    protected SerializedProperty MasterSmoothness;
    protected SerializedProperty RenderMaterialMesh;
    protected SerializedProperty RenderMaterialSplats;

    protected SerializedProperty MeshGenerationCreateNewObject;
    protected SerializedProperty MeshGenerationCreateRenderableMesh;
    protected SerializedProperty MeshGenerationCreateCollider;
    protected SerializedProperty MeshGenerationColliderVoxelDensity;
    protected SerializedProperty MeshGenerationAutoRigging;
    protected SerializedProperty MeshGenerationLockOnStart;

    protected SerializedProperty DrawRawBrushBoundingVolumes;
    protected SerializedProperty DrawComputeBrushBoundingVolumes;
    protected SerializedProperty DrawVoxelNodes;
    protected SerializedProperty DrawVoxelNodesDepth;
    protected SerializedProperty DrawVoxelNodesScale;


    public virtual void OnEnable()
    {
      var p = serializedObject.FindProperty("Params");

      MaxVoxelsK = serializedObject.FindProperty("MaxVoxelsK");
      MaxChunks = serializedObject.FindProperty("MaxChunks");
      ShowGpuMemoryUsage = serializedObject.FindProperty("ShowGpuMemoryUsage");
      AutoAdjustBudgetsToHighWaterMarks = serializedObject.FindProperty("AutoAdjustBudgetsToHighWaterMarks");
      AutoAdjustBudgetsToHighWaterMarksMarginPercent = serializedObject.FindProperty("AutoAdjustBudgetsToHighWaterMarksMarginPercent");

      VoxelDensity = serializedObject.FindProperty("VoxelDensity");
      RenderMode = serializedObject.FindProperty("RenderMode");
      SplatSize = serializedObject.FindProperty("SplatSize");
      SplatRotation = serializedObject.FindProperty("SplatRotation");
      SplatRotationNoisiness = serializedObject.FindProperty("SplatRotationNoisiness");
      SplatCameraFacing = serializedObject.FindProperty("SplatCameraFacing");
      CastShadows = serializedObject.FindProperty("CastShadows");
      ReceiveShadows = serializedObject.FindProperty("ReceiveShadows");

      SharedMaterial = serializedObject.FindProperty("SharedMaterial");
      MasterColor = serializedObject.FindProperty("m_masterColor");
      MasterEmission = serializedObject.FindProperty("m_masterEmission");
      MasterMetallic = serializedObject.FindProperty("m_masterMetallic");
      MasterSmoothness = serializedObject.FindProperty("m_masterSmoothness");
      RenderMaterialMesh = serializedObject.FindProperty("RenderMaterialMesh");
      RenderMaterialSplats = serializedObject.FindProperty("RenderMaterialSplats");

      MeshGenerationCreateNewObject = serializedObject.FindProperty("MeshGenerationCreateNewObject");
      MeshGenerationCreateRenderableMesh = serializedObject.FindProperty("MeshGenerationCreateRenderableMesh");
      MeshGenerationCreateCollider = serializedObject.FindProperty("MeshGenerationCreateCollider");
      MeshGenerationColliderVoxelDensity = serializedObject.FindProperty("MeshGenerationColliderVoxelDensity");
      MeshGenerationAutoRigging = serializedObject.FindProperty("MeshGenerationAutoRigging");
      MeshGenerationLockOnStart = serializedObject.FindProperty("MeshGenerationLockOnStart");

      DrawRawBrushBoundingVolumes = serializedObject.FindProperty("DrawRawBrushBoundingVolumes");
      DrawComputeBrushBoundingVolumes = serializedObject.FindProperty("DrawComputeBrushBoundingVolumes");
      DrawVoxelNodes = serializedObject.FindProperty("DrawVoxelNodes");
      DrawVoxelNodesDepth = serializedObject.FindProperty("DrawVoxelNodesDepth");
      DrawVoxelNodesScale = serializedObject.FindProperty("DrawVoxelNodesScale");

      EditorApplication.update += Update;
    }

    private void OnDisable()
    {
      EditorApplication.update -= Update;
    }

    private static string IntCountString(long n, bool space = false, string suffix = "")
    {
      if (n < 1024)
        return n.ToString() + (space ? " " : "") + suffix;

      if (n < 1048576)
        return (n / 1024.0f).ToString("N1") + (space ? " " : "") + "K" + suffix;

      return (n / 1048576.0f).ToString("N1") + (space ? " " : "") + "M" + suffix;
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();

      var renderer = (MudRendererBase) target;

      {
        Header("MudBun Version " + MudBun.Version);

        if (MudBun.IsFreeVersion)
        {
          Text("  Free Version Limitations:");
          Text("     - Watermark.");
          Text("     - Limited voxel density & triangle count for mesh utilities.");
          Text("     - No full source code.");
          Text("     - No commercial use.");
        }

        Space();
      }

      if (!renderer.MeshLocked)
      {
        // budgets
        {
          Header("Memory Budgets");

          Property(MaxVoxelsK, 
            "Max Voxels (K)", 
                "Maximum number of voxels times 1024.\n\n" 
              + "A voxel is a minimum unit where SDFs are evaluated.\n\n"
              + "Try increasing this value if voxels appear missing.\n\n" 
              + "The higher this value, the more GPU memory is used."
          );

          Property(MaxChunks, 
            "Max Voxel Chunks", 
                "Maximum number voxel chunks.\n\n" 
              + "A voxel chunk is a block of space that can contain multiple voxels." 
              + "The larger the space spanned by solids, the more voxel chunks are needed.\n\n" 
              + "Try increasing this value if voxels appear missing.\n\n" 
              + "The higher this value, the more GPU memory is used."
          );

          Property(ShowGpuMemoryUsage, "Show/Adjust Usage");

          if (ShowGpuMemoryUsage.boolValue)
          {
            Text("   WARNING: SHOWING USAGE IMPACTS PERFORMANCE", FontStyle.BoldAndItalic);
            Text("   Memory Used / Allocated: ", FontStyle.Bold);

            long memoryAllocated = renderer.LocalResourceGpuMemoryAllocated;
            long memoryUsed = Math.Min(memoryAllocated, renderer.LocalResourceGpuMemoryUsed);
            float memoryUtilizationPercent = 100.0f * (memoryUsed / Mathf.Max(MathUtil.Epsilon, memoryAllocated));
            Text
            (
                "      GPU Memory - " 
              + IntCountString(memoryUsed, true, "B") + " / " 
              + IntCountString(memoryAllocated, true, "B") + " ("
              + memoryUtilizationPercent.ToString("N1") + "%)"
            );

            int voxelsAllocated = renderer.NumVoxelsAllocated;
            int voxelsUsed = Mathf.Min(voxelsAllocated, renderer.NumVoxelsUsed);
            float voxelUtilizationPercent = 100.0f * (voxelsUsed / Mathf.Max(MathUtil.Epsilon, voxelsAllocated));
            Text
            (
                "      Voxels - " 
              + IntCountString(voxelsUsed) + " / " 
              + IntCountString(voxelsAllocated) + " (" 
              + voxelUtilizationPercent.ToString("N1") + "%)"
            );

            int chunksAllocated = renderer.NumChunksAllocated;
            int chunksUsed = Mathf.Min(chunksAllocated, renderer.NumChunksUsed);
            float chunkUtilizationPercent = 100.0f * (chunksUsed / Mathf.Max(MathUtil.Epsilon, chunksAllocated));
            Text
            (
                "      Voxel Chunks - "
              + IntCountString(chunksUsed) + " / " 
              + IntCountString(chunksAllocated) + " ("
              + chunkUtilizationPercent.ToString("N1") + "%)"
            );

            int vertsAllocated = renderer.NumVerticesAllocated;
            int vertsGenerated = Mathf.Min(vertsAllocated, renderer.NumVerticesGenerated);
            float vertUtilizationPercent = 100.0f * (vertsGenerated / Mathf.Max(MathUtil.Epsilon, vertsAllocated));
            Text
            (
                "      Vertices - " 
              + IntCountString(vertsGenerated) + " / " 
              + IntCountString(vertsAllocated) +  " (" 
              + vertUtilizationPercent.ToString("N1") + "%)"
            );

            Property(AutoAdjustBudgetsToHighWaterMarks, "  Auto-Adjust Budgets");
            Property(AutoAdjustBudgetsToHighWaterMarksMarginPercent, "    Margin Percent");

          }

          Space();
        } // end budgets

        // render
        {
          Header("Render");

          Property(VoxelDensity, 
            "Voxel Density", 
                "Number of voxels per unit distance.\n\n" 
              + "Higher density means more pixels and more computation."
          );

          Property(RenderMode, 
            "Render Mode", 
                "Smooth Mesh - Mesh with smooth normals. More performance intensive than flat mesh and splats.\n\n" 
              + "Flat Mesh - Mesh with flat normals.\n\n" 
              + "Splats - Flat splats scattered on solid surface."
          );

          if (RenderMode.enumValueIndex == (int) MudRendererBase.RenderModeEnum.Splats)
          {
            Property(SplatSize, "Splat Size");
            Property(SplatRotation, "Max Splat Rotation");
            Property(SplatRotationNoisiness, "  Rotation Noisiness");
            Property(SplatCameraFacing, "Splat Camera Facing");
          }

          Property(CastShadows, "Cast Shadows");
          Property(ReceiveShadows, "Receive Shadows");

          Space();
        } // end: render

        // material
        {
          Header("Material");

          Property(SharedMaterial, 
            "Shared Material", 
                "External material used as the renderer's master material."
          );

          if (SharedMaterial.objectReferenceValue == null)
          {
            Property(MasterColor, 
              "Master Color", 
                  "Master color multiplier."
            );

            Property(MasterEmission, 
              "Master Emission", 
                  "Master emission multiplier. Alpha is not used."
            );

            Property(MasterMetallic, 
              "Master Metallic", 
                  "Master metallic multiplier."
            );

            Property(MasterSmoothness, 
              "Master Smoothness", 
                  "Master smoothness multiplier."
            );

            if (RenderMode.enumValueIndex == (int) MudRendererBase.RenderModeEnum.FlatMesh 
                || RenderMode.enumValueIndex == (int) MudRendererBase.RenderModeEnum.SmoothMesh)
            {
              Property(RenderMaterialMesh, "Render Material");
            }
            else if (RenderMode.enumValueIndex == (int) MudRendererBase.RenderModeEnum.Splats)
            {
              Property(RenderMaterialSplats, "Render Material");
            }

            if (GUILayout.Button(new GUIContent("Reload Shaders", "Reload shaders and GPU resources. This is sometimes necessary after editing shaders.")))
            {
              renderer.ReloadShaders();
            }

            Space();
          } // end: material
        }

        // mesh utilities
        {
          Header("Mesh Utilities");

          if (MudBun.IsFreeVersion)
          {
            Text("Free Version Limitations:");
            Text("  - Voxel density limited to " + ((int) MudRendererBase.MaxMeshGenerationVoxelDensityFreeVersion));
            Text("  - Triangle count limited to " + MudRendererBase.MaxMeshGenerationTrianglesFreeVersion);
          }
          
          Property(MeshGenerationLockOnStart, 
            "Lock On Start", 
                "Lock mesh on start in play mode. This can save file size by not saving the mesh, but the performance will take a hit on lock."
          );

          Property(MeshGenerationCreateNewObject, 
            "Create New Object", 
                "Check to create a new object when locking mesh."
          );

          Property(MeshGenerationCreateCollider, 
            "Create Collider", 
                "Check to create a collider when locking mesh."
          );
          if (MeshGenerationCreateCollider.boolValue)
          {
            Property(MeshGenerationColliderVoxelDensity, 
              "  Collider Voxel Density", 
                  "Voxel density used for creating collider."
            );
          }

          Property(MeshGenerationCreateRenderableMesh, 
            "Create Renderable Mesh", 
                "Check to create a renderable mesh when locking mesh."
          );
          if (MeshGenerationCreateRenderableMesh.boolValue)
          {
            Property(MeshGenerationAutoRigging, 
              "  Auto-Rigging", 
                  "Check to auto-rig locked mesh with brushes flagged as bones."
            );
          }

          if (GUILayout.Button("Lock Mesh"))
          {
            LockMesh();
          }
        } // mesh utilities

        // debug
        {
          Header("Debug");

          Property(DrawRawBrushBoundingVolumes, 
            "Draw Raw Brush Bounding Volumes", 
                "Draw raw bounding volumes for each brush."
          );

          Property(DrawComputeBrushBoundingVolumes, 
            "Draw Compute Bounding Volumes", 
                "Draw expanded bounding volume actually used for computation for each brush."
          );

          Property(DrawVoxelNodes, 
            "Draw Voxel Nodes", 
                "Draw hierarchical voxel nodes."
          );

          Property(DrawVoxelNodesDepth, 
            "  Node Depth", 
                "Draw voxel nodes at a specific hierarchical depth.\n\n" 
              + "-1 means drawing all depths."
          );

          Property(DrawVoxelNodesScale, "  Node Scale");
        } // end: debug
      }
      else // mesh locked?
      {
        Header("Mesh Utilities");

        if (GUILayout.Button("Unlock Mesh"))
        {
          renderer.UnlockMesh();
        }
      }

      serializedObject.ApplyModifiedProperties();
    }

    protected virtual void LockMesh() { }

    private static readonly float RepaintInterval = 0.2f;
    private float m_repaintTimer = 0.0f;
    private void Update()
    {
      m_repaintTimer += Time.deltaTime;
      if (m_repaintTimer < RepaintInterval)
        return;

      m_repaintTimer = Mathf.Repeat(m_repaintTimer, RepaintInterval);

      try
      {
        if (Selection.activeGameObject != ((MudRendererBase) target).gameObject)
          return;
      }
      catch (MissingReferenceException)
      {
        // renderer has been destroyed
        return;
      }

      UpdateGpuMemoryUsage();
    }

    private void UpdateGpuMemoryUsage()
    {
      if (!ShowGpuMemoryUsage.boolValue)
        return;
        
      Repaint();
    }

    protected void DestroyAllChildren(Transform t, bool isRoot = true)
    {
      if (t == null)
        return;

      var aChild = new Transform[t.childCount];
      for (int i = 0; i < t.childCount; ++i)
        aChild[i] = t.GetChild(i);
      foreach (var child in aChild)
        DestroyAllChildren(child, false);

      if (!isRoot)
        DestroyImmediate(t.gameObject);
    }
  }
}

