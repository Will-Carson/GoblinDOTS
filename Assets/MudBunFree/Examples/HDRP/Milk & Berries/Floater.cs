/******************************************************************************/
/*
  Project   - Boing Kit
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

using UnityEngine;

namespace MudBun
{
  public class Floater : MonoBehaviour
  {
    [Range(0.0f, 10.0f)] public float Hover = 1.0f;
    [Range(0.0f, 10.0f)] public float Omega = 1.0f;
    private Vector3 m_hoverCenter;
    private Quaternion m_hoverRot;
    private float m_hoverPhase;


    void Start()
    {
      m_hoverCenter = transform.position;
      m_hoverRot = transform.rotation;
      m_hoverPhase = Random.value * MathUtil.TwoPi;
    }

    private void OnEnable()
    {
      Start();
    }

    void FixedUpdate()
    {
      m_hoverPhase += Omega * Time.deltaTime;
      Vector3 hoverVec = 
          0.05f * Mathf.Sin(1.37f * m_hoverPhase) * Vector3.right 
        + 0.05f * Mathf.Sin(1.93f * m_hoverPhase + 1.234f) * Vector3.forward 
        + 0.04f * Mathf.Sin(0.97f * m_hoverPhase + 4.321f) * Vector3.up;
      hoverVec *= Hover;
      Quaternion hoverQuat = Quaternion.FromToRotation(Vector3.up, hoverVec + Vector3.up);
      transform.position = m_hoverCenter + hoverVec;
      transform.rotation = m_hoverRot * hoverQuat;
    }
  }
}
