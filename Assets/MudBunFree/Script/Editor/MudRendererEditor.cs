/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

using UnityEditor;

namespace MudBun
{
  [CustomEditor(typeof(MudRenderer))]
  [CanEditMultipleObjects]
  public class MudRendererEditor : MudRendererBaseEditor
  {
    protected override void LockMesh()
    {
      base.LockMesh();

      var renderer = (MudRenderer) target;

      renderer.LockMesh(MeshGenerationAutoRigging.boolValue);

      if (MeshGenerationCreateCollider.boolValue)
      {
        renderer.AddCollider(renderer.gameObject);
      }

      if (MeshGenerationCreateNewObject.boolValue)
      {
        var clone = Instantiate(renderer.gameObject);
        clone.name = renderer.name + " (Locked Mesh Clone)";

        if (MeshGenerationAutoRigging.boolValue)
        {
          var cloneRenderer = clone.GetComponent<MudRenderer>();
          cloneRenderer.RescanBrushersImmediate();
          cloneRenderer.DestoryAllBrushesImmediate();
        }
        else
        {
          DestroyAllChildren(clone.transform);
        }

        Undo.RegisterCreatedObjectUndo(clone, clone.name);
        DestroyImmediate(clone.GetComponent<MudRenderer>());
        Selection.activeObject = clone;

        renderer.UnlockMesh();
      }
    }
  }
}

