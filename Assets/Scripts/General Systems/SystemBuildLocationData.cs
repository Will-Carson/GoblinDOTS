//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using Unity.Mathematics;
//using Unity.Transforms;
//using static Unity.Mathematics.math;
//using DOTSNET;

//[ServerWorld]
//public class SystemBuildLocationData : SystemBase
//{
//    [AutoAssign] SystemLocationManager SLM;

//    [BurstCompile]
//    struct CacheLocationDataJob : IJob
//    {
//        [ReadOnly] public NativeHashMap<int, DataLocation> characterLocations;

//        public NativeMultiHashMap<int, int> pointOccupants;
//        public NativeMultiHashMap<int, int> stageOccupants;
//        public NativeMultiHashMap<int, int> siteOccupants;

//        public void Execute()
//        {
//            pointOccupants.Clear();
//            stageOccupants.Clear();
//            siteOccupants.Clear();
//            for (int i = 0; i < characterLocations.Count(); i++)
//            {
//                pointOccupants.Add(characterLocations[i].pointId, i);
//                stageOccupants.Add(characterLocations[i].stageId, i);
//                siteOccupants.Add(characterLocations[i].siteId, i);
//            }
//        }
//    }

//    protected override void OnUpdate()
//    {
//        var job = new CacheLocationDataJob()
//        {
//            characterLocations = SLM.CharacterLocations,
//            pointOccupants = SLM.PointOccupants,
//            stageOccupants = SLM.StageOccupants,
//            siteOccupants = SLM.SiteOccupants
//        };

//        job.Schedule();
//    }
//}