using UnityEngine;

public static class InGameDateTime
{
    public static double morning = 0800;
    public static double afternoon = 1600;
    public static double night = 2400;
    public static double margin = 400;

    public static TimeSlot TimeSlot
    {
        get
        {
            if (DoubleEqual(Time.time % night, 0, margin))
            {
                return TimeSlot.night;
            }
            if (DoubleEqual(Time.time % afternoon, 0, margin))
            {
                return TimeSlot.afternoon;
            }
            if (DoubleEqual(Time.time % morning, 0, margin))
            {
                return TimeSlot.morning;
            }
            return TimeSlot.morning;
        }
    }

    public static bool DoubleEqual(double d1, double d2, double margin)
    {
        if (d1 < d2 + margin && d1 > d2 - margin) return true;
        return false;
    }
}

public enum TimeSlot
{
    morning,
    afternoon,
    night
}