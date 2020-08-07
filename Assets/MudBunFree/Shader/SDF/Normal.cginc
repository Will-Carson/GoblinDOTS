/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_SDF_NORMAL
#define MUDBUN_SDF_NORMAL

// http://iquilezles.org/www/articles/normalsSDF/normalsSDF.htm

// central differences
#define sdf_normal_diff(p, sdf, iBrushMask, h)                                                               \
  normalize_safe                                                                                             \
  (                                                                                                          \
    float3                                                                                                   \
    (                                                                                                        \
      sdf((p), float3(   (h), 0.0f, 0.0f), (iBrushMask)) - sdf((p), float3((-h), 0.0f, 0.0f), (iBrushMask)), \
      sdf((p), float3(0.0f,    (h), 0.0f), (iBrushMask)) - sdf((p), float3(0.0f, (-h), 0.0f), (iBrushMask)), \
      sdf((p), float3(0.0f, 0.0f,    (h)), (iBrushMask)) - sdf((p), float3(0.0f, 0.0f, (-h)), (iBrushMask))  \
    ),                                                                                                       \
    float3(0.0f, 0.0f, 0.0f)                                                                                 \
  )

// tetrahedron technique
#define sdf_normal_tetra(p, sdf, iBrushMask, h)                                       \
  normalize_safe                                                                      \
  (                                                                                   \
      float3( 1.0f, -1.0f, -1.0f) * sdf((p), float3( (h), -(h), -(h)), (iBrushMask))  \
    + float3(-1.0f, -1.0f,  1.0f) * sdf((p), float3(-(h), -(h),  (h)), (iBrushMask))  \
    + float3(-1.0f,  1.0f, -1.0f) * sdf((p), float3(-(h),  (h), -(h)), (iBrushMask))  \
    + float3( 1.0f,  1.0f,  1.0f) * sdf((p), float3( (h),  (h),  (h)), (iBrushMask)), \
    float3(0.0f, 0.0f, 0.0f)                                                          \
  )

// use tetrahedron technique as default
#define sdf_normal(p, sdf, iBrushMask, h) sdf_normal_tetra(p, sdf, iBrushMask, h)

// macro that generates less inline code
#define SDF_NORMAL(normal, p, sdf, iBrushMask, h)                               \
  {                                                                             \
    float3 aSign[4] =                                                           \
    {                                                                           \
      float3( 1.0f, -1.0f, -1.0f),                                              \
      float3(-1.0f, -1.0f,  1.0f),                                              \
      float3(-1.0f,  1.0f, -1.0f),                                              \
      float3( 1.0f,  1.0f,  1.0f),                                              \
    };                                                                          \
    float3 aDelta[4] =                                                          \
    {                                                                           \
      float3( (h), -(h), -(h)),                                                 \
      float3(-(h), -(h),  (h)),                                                 \
      float3(-(h),  (h), -(h)),                                                 \
      float3( (h),  (h),  (h)),                                                 \
    };                                                                          \
    float3 s = 0.0f;                                                            \
    for (int iDelta = 0; iDelta < 4; ++iDelta)                                  \
      s += aSign[iDelta] * sdf((p), aDelta[iDelta], (iBrushMask));              \
    normal = normalize_safe(s, float3(0.0f, 0.0f, 0.0f));                       \
  }

#endif
