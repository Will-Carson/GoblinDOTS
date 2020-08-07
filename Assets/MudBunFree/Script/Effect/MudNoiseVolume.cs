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
  public class MudNoiseVolume : MudSolid
  {
    public enum CoordinateSystemEnum
    {
      Cartesian, 
      Spherical, 
    }

    [SerializeField] private CoordinateSystemEnum m_coordinateSystem = CoordinateSystemEnum.Cartesian;
    [SerializeField] private SdfBrush.BoundaryShapeEnum m_boundaryShape = SdfBrush.BoundaryShapeEnum.Box;
    [SerializeField] private float m_boundaryBlend = 0.0f;
    [SerializeField] [ConditionalField("m_boundaryShape", SdfBrush.BoundaryShapeEnum.Sphere, SdfBrush.BoundaryShapeEnum.Cylinder, SdfBrush.BoundaryShapeEnum.Torus, SdfBrush.BoundaryShapeEnum.SolidAngle)]
    private float m_boundaryRadius = 0.4f;
    [SerializeField] [ConditionalField("m_boundaryShape", SdfBrush.BoundaryShapeEnum.SolidAngle)]
    private float m_boundaryAngle = 45.0f;
    [SerializeField] private Vector3 m_offset = Vector3.zero;
    [SerializeField] private Vector3 m_baseOctaveSize = Vector3.one;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_threshold = 0.5f;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_thresholdFade = 0.0f;
    [SerializeField] [Range(1, 3)] private int m_numOctaves = 2;
    [SerializeField] private float m_octaveOffsetFactor = 0.5f;
    [SerializeField] private bool m_lockPosition = false;
    public CoordinateSystemEnum CoordinateSystem { get => m_coordinateSystem; set { m_coordinateSystem = value; MarkDirty(); } }
    public SdfBrush.BoundaryShapeEnum BoundaryShape { get => m_boundaryShape; set { m_boundaryShape = value; MarkDirty(); } }
    public float BoundaryBlend { get => m_boundaryBlend; set { m_boundaryBlend = value; MarkDirty(); } }
    public float BoundaryRadius { get => m_boundaryRadius; set { m_boundaryRadius = value; MarkDirty(); } }
    public float BoundaryAngle { get => m_boundaryRadius; set { m_boundaryRadius = value; MarkDirty(); } }
    public Vector3 Offset { get => m_offset; set { m_offset = value; MarkDirty(); } }
    public Vector3 BaseOctaveSize { get => m_baseOctaveSize; set { m_baseOctaveSize = value; MarkDirty(); } }
    public float Threshold { get => m_threshold; set { m_threshold = value; MarkDirty(); } }
    public float ThresholdFade { get => m_thresholdFade; set { m_thresholdFade = value; MarkDirty(); } }
    public int NumOctaves { get => m_numOctaves; set { m_numOctaves = value; MarkDirty(); } }
    public float OctaveOffsetFactor { get => m_octaveOffsetFactor; set { m_octaveOffsetFactor = value; MarkDirty(); } }
    public bool LockPosition { get => m_lockPosition; set { m_lockPosition = value; MarkDirty(); } }

    public override Aabb Bounds
    {
      get
      {
        Vector3 posCs = PointCs(transform.position);
        Aabb bounds = BoundaryShapeBounds(m_boundaryShape, m_boundaryRadius);
        Vector3 r = 0.5f * Mathf.Max(m_baseOctaveSize.x, m_baseOctaveSize.y, m_baseOctaveSize.z) * Vector3.one;

        bounds.Min += posCs - r;
        bounds.Max += posCs + r;

        return bounds;
      }
    }

    public override void SanitizeParameters()
    {
      base.SanitizeParameters();

      Validate.NonNegative(ref m_boundaryBlend);
      Validate.NonNegative(ref m_baseOctaveSize);
    }

    public override int FillComputeData(SdfBrush [] aBrush, int iStart, List<Transform> aBone)
    {
      SdfBrush brush = SdfBrush.New;
      brush.Type = (int) SdfBrush.TypeEnum.UniformNoise;
      brush.Radius = m_boundaryRadius;
      brush.Flags.AssignBit(SdfBrush.FlagBit.LockNoisePosition, m_lockPosition);
      brush.Flags.AssignBit(SdfBrush.FlagBit.SphericalNoiseCoordinates, (m_coordinateSystem == CoordinateSystemEnum.Spherical));
      brush.Data0 = new Vector4(m_baseOctaveSize.x, m_baseOctaveSize.y, m_baseOctaveSize.z, m_threshold);
      brush.Data1 = new Vector4(m_offset.x, m_offset.y, m_offset.z, m_numOctaves);
      brush.Data2 = new Vector4(m_octaveOffsetFactor, m_thresholdFade, (int) m_boundaryShape, m_boundaryBlend);
      brush.Data3.x = Mathf.Sin(m_boundaryAngle * MathUtil.Deg2Rad);
      brush.Data3.y = Mathf.Cos(m_boundaryAngle * MathUtil.Deg2Rad);

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

      switch (m_boundaryShape)
      {
        case SdfBrush.BoundaryShapeEnum.Box:
          GizmosUtil.DrawBox(transform.position, transform.localScale, transform.rotation);
          break;

        case SdfBrush.BoundaryShapeEnum.Sphere:
          GizmosUtil.DrawSphere(transform.position, m_boundaryRadius, transform.localScale, transform.rotation);
          break;

        case SdfBrush.BoundaryShapeEnum.Cylinder:
          GizmosUtil.DrawCylinder(transform.position, m_boundaryRadius, 0.0f, transform.localScale.y, transform.rotation);
          break;

        case SdfBrush.BoundaryShapeEnum.Torus:
          // TODO
          break;

        case SdfBrush.BoundaryShapeEnum.SolidAngle:
          GizmosUtil.DrawSolidAngle(transform.position, m_boundaryRadius, m_boundaryAngle * MathUtil.Deg2Rad, transform.rotation);
          break;
      }
    }
  }
}

