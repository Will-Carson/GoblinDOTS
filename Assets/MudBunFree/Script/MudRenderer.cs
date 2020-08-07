/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace MudBun
{
  [ExecuteInEditMode]
  public class MudRenderer : MudRendererBase
  {
    protected override void OnSharedMaterialChanged(Object material)
    {
      foreach (var renderer in s_renderers)
      {
        if (renderer.SharedMaterial == material)
          renderer.MarkNeedsCompute();

        foreach (var b in renderer.Brushes)
        {
          var m = b.GetComponent<MudMaterial>();
          if (m != null && m.SharedMaterial != null && m.SharedMaterial == material)
          {
            b.MarkDirty();
            return;
          }
        }
      }
    }

    private T AddComponentHelper<T>(GameObject go) where T : Component
    {
      var comp = go.GetComponent<T>();
      if (comp == null)
      {
        #if UNITY_EDITOR
        comp = Undo.AddComponent<T>(go);
        #else
        comp = go.AddComponent<T>();
        #endif
      }
      else
      {
        #if UNITY_EDITOR
        Undo.RecordObject(comp, comp.name);
        #endif
      }

      return comp;
    }

    private void RemoveComponentHelper<T>(GameObject go) where T : Component
    {
      var comp = go.GetComponent<T>();
      if (comp != null)
      {
        #if UNITY_EDITOR
        Undo.DestroyObjectImmediate(comp);
        #else
        Destroy(comp);
        #endif
      }
    }

    public override void AddCollider(GameObject go)
    {
      var comp = AddComponentHelper<MeshCollider>(go);
      comp.sharedMesh = GenerateMesh(GeneratedMeshType.Collider);
    }

    public override void AddLockedReplicaMesh(GameObject go, bool autoRigging)
    {
      ForceCompute();

      // TODO: auto-rigging

      var lockedMeshRenderer = AddComponentHelper<MudLockedMeshRenderer>(go);
      lockedMeshRenderer.Config(m_indirectDrawArgsBuffer, m_genPointsBuffer, this);
    }

    public override void AddLockedStandardMesh(GameObject go, bool autoRigging)
    {
      Undo.RecordObject(this, name);
      var transformStack = new Stack<Transform>();
      transformStack.Push(transform);
      while (transformStack.Count > 0)
      {
        var t = transformStack.Pop();
        if (t == null)
          continue;
        Undo.RecordObject(t, t.name);
        for (int i = 0; i < t.childCount; ++i)
          transformStack.Push(t.GetChild(i));
      }

      m_doRigging = autoRigging;
      Transform [] aBone;
      var mesh = GenerateMesh(GeneratedMeshType.Standard, go.transform, out aBone);
      m_doRigging = false;

      var meshFilter = AddComponentHelper<MeshFilter>(go);
      meshFilter.sharedMesh = mesh;

      Material material = ResourcesUtil.DefaultStandardMeshMaterial;

      var meshRenderer = AddComponentHelper<SkinnedMeshRenderer>(go);
      meshRenderer.sharedMesh = mesh;
      meshRenderer.sharedMaterial = material;
      meshRenderer.bones = aBone;
      meshRenderer.rootBone = go.transform;

      AddComponentHelper<MudStandardMeshRenderer>(go);
    }

    private LockMeshIntermediateStateEnum m_lockMeshIntermediateState = LockMeshIntermediateStateEnum.Idle;
    protected override LockMeshIntermediateStateEnum LockMeshIntermediateState => m_lockMeshIntermediateState;

    public override void LockMesh(bool autoRigging)
    {
      m_lockMeshIntermediateState = LockMeshIntermediateStateEnum.PreLock;

      #if UNITY_EDITOR
      Undo.RecordObject(this, "Lock Mesh (" + name + ")");
      #endif

      base.LockMesh(autoRigging);

      #if UNITY_EDITOR
      Undo.FlushUndoRecordObjects();
      #endif

      if (MeshGenerationCreateRenderableMesh)
      {
        AddLockedStandardMesh(gameObject, autoRigging);
      }

      DisposeGlobalResources();

      m_lockMeshIntermediateState = LockMeshIntermediateStateEnum.PostLock;
    }

    public override void UnlockMesh()
    {
      m_lockMeshIntermediateState = LockMeshIntermediateStateEnum.PreUnlock;

      #if UNITY_EDITOR
      Undo.RecordObject(this, "Unlock Mesh (" + name + ")");
      #endif

      base.UnlockMesh();

      #if UNITY_EDITOR
      Undo.FlushUndoRecordObjects();
      #endif

      RemoveComponentHelper<MeshCollider>(gameObject);
      RemoveComponentHelper<MeshFilter>(gameObject);
      RemoveComponentHelper<MeshRenderer>(gameObject);
      RemoveComponentHelper<SkinnedMeshRenderer>(gameObject);
      RemoveComponentHelper<MudLockedMeshRenderer>(gameObject);
      RemoveComponentHelper<MudStandardMeshRenderer>(gameObject);

      m_lockMeshIntermediateState = LockMeshIntermediateStateEnum.Idle;
    }

    //-------------------------------------------------------------------------

#if UNITY_EDITOR

    protected override void OnEnable()
    {
      base.OnEnable();

      RegisterEditorEvents();
    }

    protected override void OnDisable()
    {
      base.OnDisable();

      UnregisterEditorEvents(); 
    }

    private void OnHierarchyChanged()
    {
      RescanBrushes();
    }

    private void OnEditorUpdate()
    {

    }

    private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
    {
      MarkNeedsCompute();
    }

    private void OnUndoPerformed()
    {
      MarkNeedsCompute();
    }

    private void OnBeforeAssemblyReload()
    {
      DisposeGlobalResources();
      DisposeLocalResources();
    }

    private void OnAfterAssemblyReload()
    {

    }

    private void RegisterEditorEvents()
    { 
      EditorApplication.hierarchyChanged += OnHierarchyChanged;
      EditorApplication.update += OnEditorUpdate;
      UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
      Undo.undoRedoPerformed += OnUndoPerformed;
      AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
      AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
    }

    private void UnregisterEditorEvents()
    {
      EditorApplication.hierarchyChanged -= OnHierarchyChanged;
      EditorApplication.update -= OnEditorUpdate;
      UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
      Undo.undoRedoPerformed -= OnUndoPerformed;
      AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
      AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
    }

    protected override bool IsEditorBusy()
    {
      if (EditorApplication.isCompiling)
        return true;

      if (EditorApplication.isUpdating)
        return true;

      return false;
    }

    public override void ReloadShaders()
    {
      base.ReloadShaders();

      EditorApplication.QueuePlayerLoopUpdate();
      SceneView.RepaintAll();
    }

    private void OnDrawGizmos()
    {
      if (IsEditorBusy())
        return;

      Color prevColor = Gizmos.color;
      Gizmos.matrix = transform.localToWorldMatrix;

      if (DrawRawBrushBoundingVolumes)
      {
        Gizmos.color = Color.white;
        foreach (var b in Brushes)
        {
          Aabb bounds = b.Bounds;
          Gizmos.DrawWireCube(bounds.Center, bounds.Extents);
        }
      }

      if (DrawComputeBrushBoundingVolumes)
      {
        Gizmos.color = Color.yellow;
        m_aabbTree.ForEach(bounds => Gizmos.DrawWireCube(bounds.Center, bounds.Extents));
      }

      if (DrawVoxelNodes)
      {
        Gizmos.color = Color.gray;
        var aNumAllocated = new int[m_numNodesAllocatedBuffer.count];
        m_numNodesAllocatedBuffer.GetData(aNumAllocated);
        int numTotalNodes = aNumAllocated[0];
        var aNode = new VoxelNode[numTotalNodes];
        m_nodePoolBuffer.GetData(aNode);
        var aNodeSize = NodeSizes;
        int iNode = 0;
        for (int depth = 0; depth <= VoxelNodeDepth; ++depth)
        {
          int numNodesInDepth = Mathf.Min(aNumAllocated[depth + 1], aNode.Length);;

          if (DrawVoxelNodesDepth >= 0 && depth != DrawVoxelNodesDepth)
          {
            iNode += numNodesInDepth;
            continue;
          }

          float nodeSize = aNodeSize[depth];
          for (int i = 0; i < numNodesInDepth && iNode < aNode.Length; ++i)
          {
            Gizmos.DrawWireCube(aNode[iNode++].Center, DrawVoxelNodesScale * nodeSize * Vector3.one);
          }
        }
      }

      Gizmos.color = prevColor;
      Gizmos.matrix = Matrix4x4.identity;
    }

#endif
  }
}
