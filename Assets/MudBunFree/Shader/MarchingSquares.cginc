/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_MARCHING_SQUARES
#define MUDBUN_MARCHING_SQUARES

#include "BrushFuncs.cginc"
#include "SDF/SDF.cginc"

StructuredBuffer<int> triTable;
StructuredBuffer<int> edgeTable;
StructuredBuffer<int> vertTable;

static const float3 vertPosLs[8] = 
{
  float3(-0.5f, -0.5f, -0.5f), 
  float3( 0.5f, -0.5f, -0.5f), 
  float3( 0.5f, -0.5f,  0.5f), 
  float3(-0.5f, -0.5f,  0.5f), 
  float3(-0.5f,  0.5f, -0.5f), 
  float3( 0.5f,  0.5f, -0.5f), 
  float3( 0.5f,  0.5f,  0.5f), 
  float3(-0.5f,  0.5f,  0.5f), 
};

// cubeMat = whole-cube properties (for flat normal mode only)
// tStmtPre = statements pre-processing "iTri" for new triangle
// vStmt = statements processing "iVert", "aVertPos", "aVertNorm", and "aVertaMat" for new triangle
// tStmtPost = statements post-processing "iTri" a new triangle
#define MARCHING_CUBES(                                                        \
  center, size, sdf, iBrushMask, smoothNormal, cubeMat,                        \
  tStmtPre, vStmt, tStmtPost                                                   \
)                                                                              \
{                                                                              \
  int cubeIndex = 0;                                                           \
  float d[8];                                                                  \
  SdfBrushMaterial aMat[8];                                                    \
  cubeMat = init_brush_material();                                             \
  {                                                                            \
    for (int iVert = 0; iVert < 8; ++iVert)                                    \
    {                                                                          \
      float3 vertPos = center + size * vertPosLs[iVert];                       \
      d[iVert] = sdf(vertPos, iBrushMask, aMat[iVert]);                        \
      cubeIndex |= (int(step(0.0f, -d[iVert])) << iVert);                      \
    }                                                                          \
                                                                               \
    if (!smoothNormal)                                                         \
      cubeMat = aMat[0];                                                       \
  }                                                                            \
                                                                               \
  int iTriListBase = cubeIndex * 16;                                           \
  for (int iTri = 0; iTri < 5; ++iTri)                                         \
  {                                                                            \
    int iTriBase = iTri * 3;                                                   \
    int aiEdge[3];                                                             \
    aiEdge[0] = triTable[iTriListBase + iTriBase];                             \
    if (aiEdge[0] < 0)                                                         \
      break;                                                                   \
                                                                               \
    aiEdge[1] = triTable[iTriListBase + iTriBase + 1];                         \
    aiEdge[2] = triTable[iTriListBase + iTriBase + 2];                         \
                                                                               \
    tStmtPre                                                                   \
                                                                               \
    float3 aVertPos[3];                                                        \
    float3 aVertNorm[3];                                                       \
    float3 goodNorm = float3(0.0f, 0.0f, 0.0f);                                \
    SdfBrushMaterial aVertMat[3];                                              \
    for (int jVert = 0; jVert < 3; ++jVert)                                    \
    {                                                                          \
      int iEdgeVert0 = vertTable[aiEdge[jVert] * 2];                           \
      int iEdgeVert1 = vertTable[aiEdge[jVert] * 2 + 1];                       \
      float3 p0Ls = vertPosLs[iEdgeVert0];                                     \
      float3 p1Ls = vertPosLs[iEdgeVert1];                                     \
      float t = -d[iEdgeVert0] / (d[iEdgeVert1] - d[iEdgeVert0]);              \
      aVertPos[jVert] = center + size * lerp(p0Ls, p1Ls, t);                   \
      if (smoothNormal)                                                        \
      {                                                                        \
        aVertMat[jVert] = lerp(aMat[iEdgeVert0], aMat[iEdgeVert1], t);         \
        SDF_NORMAL(aVertNorm[jVert], aVertPos[jVert], sdf, iBrushMask, 1e-4f); \
        if (dot(aVertNorm[jVert], aVertNorm[jVert]) > kEpsilon)                \
          goodNorm = aVertNorm[jVert];                                         \
      }                                                                        \
    }                                                                          \
                                                                               \
    if (smoothNormal)                                                          \
    {                                                                          \
      for (int kVert = 0; kVert < 3; ++kVert)                                  \
      {                                                                        \
        if (dot(aVertNorm[kVert], aVertNorm[kVert]) <= kEpsilon)               \
          aVertNorm[kVert] = goodNorm;                                         \
      }                                                                        \
    }                                                                          \
    else                                                                       \
    {                                                                          \
      float3 flatNorm =                                                        \
        normalize                                                              \
        (                                                                      \
          cross(aVertPos[1] - aVertPos[0], aVertPos[2] - aVertPos[0])          \
        );                                                                     \
      for (int kVert = 0; kVert < 3; ++kVert)                                  \
        aVertNorm[kVert] = flatNorm;                                           \
    }                                                                          \
                                                                               \
    for (int iVert = 0; iVert < 3; ++iVert)                                    \
    {                                                                          \
      vStmt                                                                    \
    }                                                                          \
                                                                               \
    tStmtPost                                                                  \
  }                                                                            \
}

#endif

