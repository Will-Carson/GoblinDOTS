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
  public class MudModifier : MudBrush
  {
    public enum OperatorEnum
    {
      Modify = 100, 
    }

    public override bool IsPredecessorModifier => true;

    public virtual float MaxModification => 0.0f;

    public override void FillBrushData(ref SdfBrush brush)
    {
      base.FillBrushData(ref brush);

      brush.Operator = (int) OperatorEnum.Modify;
      brush.Blend = MaxModification;
    }
  }
}

