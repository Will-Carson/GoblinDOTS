using Unity.Jobs;

internal interface INonScheduler
{
    JobHandle ScheduleEvent();
}