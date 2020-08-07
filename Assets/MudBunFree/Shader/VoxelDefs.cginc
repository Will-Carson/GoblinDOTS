﻿/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_VOXEL_DEFS
#define MUDBUN_VOXEL_DEFS

struct VoxelNode
{
  float3 center;
  int iParent;
  int iBrushMask;
};

RWStructuredBuffer<VoxelNode> nodePool;
uint nodePoolSize;
RWStructuredBuffer<int> aNumNodesAllocated; //(total, L0, L1, ..., voxels)
uint chunkVoxelDensity;

int currentNodeDepth;
int currentNodeBranchingFactor;
int maxNodeDepth;
float currentNodeSize;
float voxelSize;

#endif

