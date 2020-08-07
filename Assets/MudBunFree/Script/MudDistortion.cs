/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

namespace MudBun
{
  public class MudDistortion : MudBrush
  {
    public enum OperatorEnum
    {
      Distort = -100, 
    }

    public override bool IsSuccessorModifier => true;

    public virtual float MaxDistortion => 0.0f;

    public override void FillBrushData(ref SdfBrush brush)
    {
      base.FillBrushData(ref brush);

      brush.Operator = (int) OperatorEnum.Distort;
      brush.Blend = MaxDistortion;
    }
  }
}

