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
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace MudBun
{
  [ExecuteInEditMode]
  public class MudCurveFull : MudSolid
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

    [Serializable]
    public class Point
    {
      public Transform Position;
      public float Radius;

      public Point(Transform position = null, float radius = 0.2f)
      {
        Position = position;
        Radius = radius;
      }

      public Point(GameObject go, float radius = 0.2f)
      {
        Position = go?.transform;
        Radius = radius;
      }
    }

    [Header("Shape")]

    [SerializeField] [Range(1, 16)] private int m_precision = 8;
    public int Precision { get => m_precision; set { m_precision = value; MarkDirty(); } }

    public Transform HeadControlPoint;
    public Transform TailControlPoint;
    [SerializeField] private List<Point> m_points = new List<Point>();
    public ICollection<Point> Points
    {
      get => m_points;
      set
      {
        m_points.Clear();
        foreach (var p in value)
          m_points.Add(p);
        
        MarkDirty();
      }
    }

    public MudCurveFull()
    {
      m_points.Add(new Point());
    }

    public override Aabb Bounds
    {
      get
      {
        Aabb bounds = Aabb.Empty;

        foreach (var p in m_points)
        {
          if (p == null || p.Position == null)
            continue;

          Vector3 posCs = PointCs(p.Position.position);
          Vector3 r = 1.5f * p.Radius * Vector3.one;
          bounds.Include(new Aabb(posCs - r, posCs + r));
        }

        return bounds;
      }
    }

    public override void SanitizeParameters()
    {
      base.SanitizeParameters();

      Validate.NonNegative(ref m_noiseBaseOctaveSize);

      if (m_points != null)
      {
        foreach(var p in m_points)
        {
          if (p == null || p.Position == null)
            continue;

          Validate.NonNegative(ref p.Radius);
        }
      }
    }

    private void Update()
    {
      foreach (var p in m_points)
      {
        if (p == null || p.Position == null)
          continue;

        if (!p.Position.hasChanged)
          continue;

        MarkDirty();
        p.Position.hasChanged = false;
        break;
      }

      if (HeadControlPoint != null && HeadControlPoint.hasChanged)
      {
        MarkDirty();
        HeadControlPoint.hasChanged = false;
      }

      if (TailControlPoint != null && TailControlPoint.hasChanged)
      {
        MarkDirty();
        TailControlPoint.hasChanged = false;
      }
    }

    public override int FillComputeData(SdfBrush [] aBrush, int iStart, List<Transform> aBone)
    {
      if (m_points == null || m_points.Count == 0)
        return 0;

      if (m_points.Any(p => p == null || p.Position == null))
        return 0;

      SdfBrush brush = SdfBrush.New;
      brush.Type = (int) SdfBrush.TypeEnum.NoiseCurveFull;

      if (m_points.Count == 1)
      {
        return 0;
      }

      int iBrush = iStart;

      brush.Data0.x = m_points.Count + 2;
      brush.Data0.y = Precision;
      brush.Data0.z = m_enableNoise ? 1.0f : 0.0f;

      if (aBone != null)
      {
        brush.BoneIndex = aBone.Count;
        foreach (var p in m_points)
          aBone.Add(p.Position);
      }

      aBrush[iBrush++] = brush;
      brush.Type = (int) SdfBrush.TypeEnum.Nop;

      if (m_enableNoise)
      {
        brush.Data0 = new Vector4(m_noiseBaseOctaveSize.x, m_noiseBaseOctaveSize.y, m_noiseBaseOctaveSize.y, m_noiseThreshold);
        brush.Data1 = new Vector4(m_noiseOffset, 0.0f, 0.0f, m_noiseNumOctaves);
        brush.Data2 = new Vector4(m_noiseOctaveOffsetFactor, 0.0f, 0.0f, 0.0f);
        aBrush[iBrush++] = brush;
      }

      int iPreHead = iBrush;
      var head = m_points[0];
      var postHead = m_points[1];
      Vector3 preHeadPosBs = 
        HeadControlPoint == null 
          ? 2.0f * head.Position.position - postHead.Position.position 
          : HeadControlPoint.position;
      preHeadPosBs = PointBs(preHeadPosBs);
      brush.Data0 = new Vector4(preHeadPosBs.x, preHeadPosBs.y, preHeadPosBs.z, head.Radius);
      aBrush[iBrush++] = brush;

      for (int i = 0; i < m_points.Count; ++i)
      {
        var p = m_points[i];
        Vector3 pointPosBs = PointBs(p.Position.position);
        brush.Data0 = new Vector4(pointPosBs.x, pointPosBs.y, pointPosBs.z, p.Radius);
        aBrush[iBrush++] = brush;
      }

      int iPostTail = iBrush;
      var tail = m_points[m_points.Count - 1];
      var preTail = m_points[m_points.Count - 2];
      Vector3 postTailPosBs = 
        TailControlPoint == null 
          ? 2.0f * tail.Position.position - preTail.Position.position 
          : TailControlPoint.position;
      postTailPosBs = PointBs(postTailPosBs);
      brush.Data0 = new Vector4(postTailPosBs.x, postTailPosBs.y, postTailPosBs.z, tail.Radius);
      aBrush[iBrush++] = brush;

      if (HeadControlPoint == null)
      {
        Vector3 headControlPosBs = 
          2.0f * head.Position.position 
          - VectorUtil.CatmullRom
            (
              preHeadPosBs, 
              head.Position.position, 
              postHead.Position.position, 
              aBrush[iPreHead + 3].Data0, 
              0.75f
            );
        headControlPosBs = PointBs(headControlPosBs);
        aBrush[iPreHead].Data0 = new Vector4(headControlPosBs.x, headControlPosBs.y, headControlPosBs.z, head.Radius);
      }

      if (TailControlPoint == null)
      {
        Vector3 tailControlPosBs = 
          2.0f * tail.Position.position 
          - VectorUtil.CatmullRom
            (
              postTailPosBs, 
              tail.Position.position, 
              preTail.Position.position, 
              aBrush[iPostTail - 3].Data0, 
              0.75f
            );
        tailControlPosBs = PointBs(tailControlPosBs);
        aBrush[iPostTail].Data0 = new Vector4(tailControlPosBs.x, tailControlPosBs.y, tailControlPosBs.z, tail.Radius);
      }

      return iBrush - iStart;
    }

    protected override void DrawOutlineGizmos()
    {
      base.DrawOutlineGizmos();

      if (m_points == null)
        return;

      if (m_points.Any(p => p == null || p.Position == null))
        return;

      GizmosUtil.DrawCatmullRom
      (
        m_points.Select(p => p.Position).ToArray(), 
        m_points.Select(p => p.Radius).ToArray(), 
        HeadControlPoint, 
        TailControlPoint
      );
    }
  }
}

