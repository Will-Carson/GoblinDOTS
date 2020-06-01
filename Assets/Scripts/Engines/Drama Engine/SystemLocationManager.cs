using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

[ServerWorld]
public class SystemLocationManager : SystemBase
{
    // Accessible data:
    public NativeArray<DataPoint> PointDatas = new NativeArray<DataPoint>(G.numberOfPoints, Allocator.Persistent);
    public NativeMultiHashMap<int, int> PointOccupants = new NativeMultiHashMap<int, int>(G.occupantsPerPoint, Allocator.Temp);

    public NativeArray<DataStage> StageDatas = new NativeArray<DataStage>(G.numberOfStages, Allocator.Persistent);
    public NativeMultiHashMap<int, int> StagePoints = new NativeMultiHashMap<int, int>(G.pointsPerStage, Allocator.Temp);
    public NativeMultiHashMap<int, int> StageOccupants = new NativeMultiHashMap<int, int>(G.occupantsPerStage, Allocator.Temp);

    public NativeArray<DataSite> SiteDatas = new NativeArray<DataSite>(G.numberOfSites, Allocator.Persistent);
    public NativeMultiHashMap<int, int> SiteStages = new NativeMultiHashMap<int, int>(G.stagesPerSite, Allocator.Temp);
    public NativeMultiHashMap<int, int> SiteOccupants = new NativeMultiHashMap<int, int>(G.occupantsPerSite, Allocator.Temp);

    public NativeHashMap<int, DataLocation> CharacterLocations = new NativeHashMap<int, DataLocation>(G.maxTotalPopulation, Allocator.Persistent);

    // Events:
    public NativeList<EventMoveRequest> EventsMoveRequest = new NativeList<EventMoveRequest>(G.maxTotalPopulation, Allocator.Persistent);

    [BurstCompile]
    struct CacheLocationDataJob : IJob
    {
        public NativeHashMap<int, DataLocation> characterLocations;

        public NativeMultiHashMap<int, int> pointOccupants;
        public NativeMultiHashMap<int, int> stageOccupants;
        public NativeMultiHashMap<int, int> siteOccupants;

        public void Execute()
        {
            pointOccupants.Clear();
            stageOccupants.Clear();
            siteOccupants.Clear();
            for (int i = 0; i < characterLocations.Count(); i++)
            {
                pointOccupants.Add(characterLocations[i].pointId, i);
                stageOccupants.Add(characterLocations[i].stageId, i);
                siteOccupants.Add(characterLocations[i].siteId, i);
            }
        }
    }

    [BurstCompile]
    struct ProcessEventsJob : IJob
    {
        public NativeArray<DataPoint> pointDatas;
        public NativeMultiHashMap<int, int> pointOccupants;
        public NativeArray<DataStage> stageDatas;
        public NativeMultiHashMap<int, int> stageOccupants;
        public NativeArray<DataSite> siteDatas;

        public NativeHashMap<int, DataLocation> characterLocations;
        public NativeList<EventMoveRequest> eventsMoveRequest;

        public void Execute()
        {
            for (int i = 0; i < eventsMoveRequest.Length; i++)
            {
                var e = eventsMoveRequest[i];
                var pointData = pointDatas[e.location.pointId];
                var stageData = stageDatas[e.location.stageId];
                var pointOccupants = this.pointOccupants.CountValuesForKey(e.location.pointId);
                var stageOccupants = this.stageOccupants.CountValuesForKey(e.location.stageId);

                // Check if there's room at the new location.
                if (pointOccupants < pointData.maxOccupants && stageOccupants < stageData.maxOccupants)
                {
                    // Add to new location
                    var newLocationData = new DataLocation()
                    {
                        pointId = e.location.pointId,
                        stageId = e.location.stageId,
                        siteId = e.location.stageId
                    };

                    characterLocations[eventsMoveRequest[i].mover] = newLocationData;
                }
            }
        }
    }

    protected override void OnUpdate()
    {
        var job1 = new CacheLocationDataJob()
        {
            characterLocations = CharacterLocations,
            pointOccupants = PointOccupants,
            stageOccupants = StageOccupants,
            siteOccupants = SiteOccupants
        };

        job1.Schedule();

        var job2 = new ProcessEventsJob()
        {
            pointDatas = PointDatas,
            stageDatas = StageDatas,
            siteDatas = SiteDatas,
            characterLocations = CharacterLocations,
            eventsMoveRequest = EventsMoveRequest
        };

        job2.Schedule();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        PointDatas.Dispose();
        StageDatas.Dispose();
        SiteDatas.Dispose();
        CharacterLocations.Dispose();
        EventsMoveRequest.Dispose();
    }
}