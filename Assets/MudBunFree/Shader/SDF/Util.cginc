/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_SDF_UTIL
#define MUDBUN_SDF_UTIL

float min_comp(float3 v)
{
  return min(v.x, min(v.y, v.z));
}

float max_comp(float3 v)
{
  return max(v.x, max(v.y, v.z));
}

#endif
