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

using UnityEngine;

namespace MudBun
{
  public class MudBox : MudSolid
  {
    [SerializeField] private float m_round = 0.05f;
    public float Round { get => m_round; set { m_round = value; MarkDirty(); } }

    [Range(-1.0f, 1.0f)] public float PivotShift = 0.0f;

    public override Aabb Bounds
    {
      get
      {
        Vector3 r = 0.5f * VectorUtil.Abs(transform.localScale);
        Vector3 posCs = PointCs(transform.position - transform.up * PivotShift * r.y);
        Aabb bounds = new Aabb(-r, r);
        bounds.Rotate(RotationCs(transform.rotation));
        Vector3 round = m_round * Vector3.one;
        bounds.Min += posCs - round;
        bounds.Max += posCs + round;
        return bounds;
      }
    }

    public override void SanitizeParameters()
    {
      base.SanitizeParameters();

      Validate.NonNegative(ref m_round);
    }

    public override int FillComputeData(SdfBrush [] aBrush, int iStart, List<Transform> aBone)
    {
      SdfBrush brush = SdfBrush.New;
      brush.Type = (int) SdfBrush.TypeEnum.Box;
      brush.Radius = m_round;
      brush.Data0.x = PivotShift;

      if (aBone != null)
      {
        brush.BoneIndex = aBone.Count;
        aBone.Add(gameObject.transform);
      }

      aBrush[iStart] = brush;

      return 1;
    }

    protected override void DrawOutlineGizmos()
    {
      base.DrawOutlineGizmos();

      GizmosUtil.DrawBox(transform.position, transform.localScale, transform.rotation);
    }
  }
}

