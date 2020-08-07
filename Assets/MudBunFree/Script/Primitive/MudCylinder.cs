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
  public class MudCylinder : MudSolid
  {
    [SerializeField] private float m_radius = 0.5f;
    public float Radius { get => m_radius; set { m_radius = value; MarkDirty(); } }

    [SerializeField] private float m_topRadiusOffset = 0.0f;
    public float TopRadiusOffset { get => m_topRadiusOffset; set { m_topRadiusOffset = value; MarkDirty(); } }

    [SerializeField] private float m_round = 0.05f;
    public float Round { get => m_round; set { m_round = value; MarkDirty(); } }

    public override Aabb Bounds
    {
      get
      {
        Vector3 posCs = PointCs(transform.position);
        Vector3 size = VectorUtil.Abs(transform.localScale);
        float maxRadius = m_radius + Mathf.Max(0.0f, m_topRadiusOffset);
        Vector3 r = new Vector3(maxRadius, 0.5f * size.y, maxRadius);
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

      Validate.NonNegative(ref m_radius);
      Validate.NonNegative(ref m_round);
    }

    public override int FillComputeData(SdfBrush [] aBrush, int iStart, List<Transform> aBone)
    {
      SdfBrush brush = SdfBrush.New;
      brush.Type = (int) SdfBrush.TypeEnum.Cylinder;
      brush.Radius = m_radius;
      brush.Data0.x = m_round;
      brush.Data0.y = m_topRadiusOffset;

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

      GizmosUtil.DrawCylinder(transform.position, m_radius, m_topRadiusOffset, transform.localScale.y, transform.rotation);
    }
  }
}

