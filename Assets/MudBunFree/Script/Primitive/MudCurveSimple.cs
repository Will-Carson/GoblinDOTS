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
  public class MudCurveSimple : MudSolid
  {
    [Header("Noise")]

    [SerializeField] private bool m_enableNoise = false;
    [SerializeField] private float m_noiseOffset = 0.0f;
    [SerializeField] private Vector2 m_noiseBaseOctaveSize = 0.5f * Vector2.one;
    [SerializeField] [Range(0.0f, 1.0f)] private float m_noiseThreshold = 0.5f;
    [SerializeField] [Range(1, 3)] private int m_noiseNumOctaves = 2;
    [SerializeField] private float m_noiseOctaveOffsetFactor = 0.5f;
    public bool EnableNoise { get => m_enableNoise; set { m_enableNoise = value; MarkDirty(); } }
    public float NoiseOffset { get => m_noiseOffset; set { m_noiseOffset = value; MarkDirty(); } }
    public Vector2 NoiseBaseOctaveSize { get => m_noiseBaseOctaveSize; set { m_noiseBaseOctaveSize = value; MarkDirty(); } }
    public float NoiseThreshold { get => m_noiseThreshold; set { m_noiseThreshold = value; MarkDirty(); } }
    public int NoiseNumOctaves { get => m_noiseNumOctaves; set { m_noiseNumOctaves = value; MarkDirty(); } }
    public float NoiseOctaveOffsetFactor { get => m_noiseOctaveOffsetFactor; set { m_noiseOctaveOffsetFactor = value; MarkDirty(); } }

    [Header("Shape")]

    public Transform PointA;
    public Transform ControlPoint;
    public Transform PointB;

    [SerializeField] private float m_radiusA = 0.2f;
    public float RadiusA { get => m_radiusA; set { m_radiusA = value; MarkDirty(); } }

    [SerializeField] private float m_radiusB = 0.2f;
    public float RadiusB { get => m_radiusB; set { m_radiusB = value; MarkDirty(); } }

    public override Aabb Bounds
    {
      get
      {
        if (PointA == null || PointB == null || ControlPoint == null)
          return Aabb.Empty;

        Vector3 a = PointCs(PointA.position);
        Vector3 b = PointCs(PointB.position);
        Vector3 c = PointCs(ControlPoint.position);

        Vector3 r = Mathf.Max(m_radiusA, m_radiusB) * Vector3.one;
        Aabb bounds = Aabb.Empty;
        bounds.Include(new Aabb(a - r, a + r));
        bounds.Include(new Aabb(b - r, b + r));
        bounds.Include(new Aabb(c - r, c + r));

        return bounds;
      }
    }

    public override void SanitizeParameters()
    {
      base.SanitizeParameters();

      Validate.NonNegative(ref m_noiseBaseOctaveSize);

      Validate.NonNegative(ref m_radiusA);
      Validate.NonNegative(ref m_radiusB);
    }

    private Transform[] m_aPoint = new Transform [] { null, null, null };
    private void Update()
    {
      m_aPoint[0] = PointA;
      m_aPoint[1] = PointB;
      m_aPoint[2] = ControlPoint;
      foreach (var p in m_aPoint)
      {
        if (p == null)
          return;

        if (!p.hasChanged)
          continue;

        MarkDirty();
        p.hasChanged = false;
        break;
      }
    }

    public override int FillComputeData(SdfBrush [] aBrush, int iStart, List<Transform> aBone)
    {
      if (PointA == null || PointB == null || ControlPoint == null)
        return 0;
      
      Vector3 a = PointCs(PointA.position);
      Vector3 b = PointCs(PointB.position);
      Vector3 c = PointCs(ControlPoint.position);

      int iBrush = iStart;
      SdfBrush brush = SdfBrush.New;

      brush.Type = (int) SdfBrush.TypeEnum.NoiseCurveSimple;
      brush.Data0 = new Vector4(a.x, a.y, a.z, m_radiusA);
      brush.Data1 = new Vector4(b.x, b.y, b.z, m_radiusB);
      brush.Data2 = new Vector4(c.x, c.y, c.z, m_enableNoise ? 1.0f : 0.0f);

      if (aBone != null)
      {
        brush.BoneIndex = aBone.Count;
        aBone.Add(PointA);
        aBone.Add(PointB);
        aBone.Add(ControlPoint);
      }

      aBrush[iBrush++] = brush;

      if (m_enableNoise)
      {
        brush.Type = (int) SdfBrush.TypeEnum.Nop;
        brush.Data0 = new Vector4(m_noiseBaseOctaveSize.x, m_noiseBaseOctaveSize.y, m_noiseBaseOctaveSize.y, m_noiseThreshold);
        brush.Data1 = new Vector4(m_noiseOffset, 0.0f, 0.0f, m_noiseNumOctaves);
        brush.Data2 = new Vector4(m_noiseOctaveOffsetFactor, 0.0f, 0.0f, 0.0f);
        aBrush[iBrush++] = brush;
      }

      return iBrush - iStart;
    }

    protected override void DrawOutlineGizmos()
    {
      base.DrawOutlineGizmos();

      if (PointA != null)
      {
        GizmosUtil.DrawSphere(PointA.position, m_radiusA, Vector3.one, PointA.rotation);
      }

      if (PointB != null)
      {
        GizmosUtil.DrawSphere(PointB.position, m_radiusB, Vector3.one, PointB.rotation);
      }

      if (ControlPoint != null)
      {
        float da = (ControlPoint.position - PointA.position).magnitude;
        float db = (ControlPoint.position - PointB.position).magnitude;
        float r = Mathf.Lerp(m_radiusA, m_radiusB, da / (da + db));
        GizmosUtil.DrawSphere(ControlPoint.position, r, Vector3.one, ControlPoint.rotation);
      }

      if (PointA != null && PointB != null && ControlPoint != null)
      {
        GizmosUtil.DrawBezierQuad(PointA.position, PointB.position, ControlPoint.position);
      }
    }
  }
}

