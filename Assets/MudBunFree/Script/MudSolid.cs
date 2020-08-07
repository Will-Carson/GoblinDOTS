/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

using UnityEngine;

namespace MudBun
{
  [ExecuteInEditMode]
  [RequireComponent(typeof(MudMaterial))]
  public class MudSolid : MudBrush
  {
    [SerializeField] private SdfBrush.OperatorEnum m_operator = SdfBrush.OperatorEnum.Union;
    public SdfBrush.OperatorEnum Operator { get => m_operator; set { m_operator = value; MarkDirty(); } }

    [SerializeField] private float m_blend;
    public float Blend { get => m_blend; set { m_blend = value; MarkDirty(); } }

    [Tooltip("If checked, this brush will be counted as bone during auto rigging.")]
    [SerializeField] private bool m_countAsBone = true;
    public override bool CountAsBone => m_countAsBone;

    public override float BoundsPadding => m_blend;
    public override bool IsPredecessorModifier => (m_operator == SdfBrush.OperatorEnum.Intersect);

    public override bool UsesMaterial => true;
    public override int MaterialHash => GetComponent<MudMaterial>().MaterialHash;

    public override void SanitizeParameters()
    {
      base.SanitizeParameters();

      Validate.NonNegative(ref m_blend);
    }

    public override void FillBrushData(ref SdfBrush brush)
    {
      base.FillBrushData(ref brush);
      
      brush.Operator = (int) m_operator;
      brush.Blend = Blend;

      ValidateMaterial();
      var material = GetComponent<MudMaterial>();
      brush.Flags.AssignBit(SdfBrush.FlagBit.ContributeMaterial, material.ContributeMaterial);
      brush.Flags.AssignBit(SdfBrush.FlagBit.CountAsBone, CountAsBone);
    }

    public override void FillBrushMaterialData(ref SdfBrushMaterial mat)
    {
      base.FillBrushMaterialData(ref mat);

      var material = GetComponent<MudMaterial>();

      mat.Color = material.Color;
      mat.Emission = material.Emission;
      mat.MetallicSmoothnessSizeTightness.Set(material.Metallic, material.Smoothness, material.SplatSize, material.BlendTightness);
      mat.IntWeight.Set
      (
        (material.TextureIndex == 0 ? 1.0f : 0.0f), 
        (material.TextureIndex == 1 ? 1.0f : 0.0f), 
        (material.TextureIndex == 2 ? 1.0f : 0.0f), 
        (material.TextureIndex == 3 ? 1.0f : 0.0f)
      );
    }

    public override void ValidateMaterial()
    {
      var material = GetComponent<MudMaterial>();
      if (material != null)
        return;

      material = gameObject.AddComponent<MudMaterial>();
    }

    public override void OnDrawGizmos()
    {
      base.OnDrawGizmos();

      bool shouldDrawOutlines = false;
      switch (m_operator)
      {
        case SdfBrush.OperatorEnum.Union:
        {
          if (Renderer == null)
            break;

          if (Renderer.RenderMode != MudRendererBase.RenderModeEnum.Splats)
            break;

          var material = GetComponent<MudMaterial>();
          if (material == null)
            break;
          
          shouldDrawOutlines = 
            Renderer.SplatSize * material.SplatSize < 0.1f 
            || material.Color.a < 0.25f;
          break;
        }

        case SdfBrush.OperatorEnum.Subtract:
        case SdfBrush.OperatorEnum.Intersect:
        case SdfBrush.OperatorEnum.Dye:
        case SdfBrush.OperatorEnum.NoOp:
        {
          shouldDrawOutlines = true;
          break;
        }
      }

      if (shouldDrawOutlines)
      {
        DrawOutlineGizmos();
      }
    }

    protected virtual void DrawOutlineGizmos() { }
  }
}

