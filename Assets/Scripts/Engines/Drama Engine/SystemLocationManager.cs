using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

public class SystemLocationManager : SystemBase
{
    // Accessible data:
    public NativeArray<DataPoint> PointDatas = new NativeArray<DataPoint>(G.numberOfPoints, Allocator.Persistent);
    public NativeArray<DataStage> StageDatas = new NativeArray<DataStage>(G.numberOfStages, Allocator.Persistent);
    public NativeArray<DataSite> SiteDatas = new NativeArray<DataSite>(G.numberOfSites, Allocator.Persistent);
    public NativeHashMap<int, DataLocation> CharacterLocations = new NativeHashMap<int, DataLocation>(G.maxTotalPopulation, Allocator.Persistent);

    // Events:
    public NativeList<EventMoveRequest> EventsMoveRequest = new NativeList<EventMoveRequest>(G.maxTotalPopulation, Allocator.Persistent);

    [BurstCompile]
    struct SystemLocationManagerJob : IJob
    {
        public NativeArray<DataPoint> pointDatas;
        public NativeArray<DataStage> stageDatas;
        public NativeArray<DataSite> siteDatas;
        public NativeHashMap<int, DataLocation> characterLocations;
        public NativeList<EventMoveRequest> eventsMoveRequest;

        public void Execute()
        {
            for (int i = 0; i < eventsMoveRequest.Length; i++)
            {
                var e = eventsMoveRequest[i];
                var pointData = pointDatas[e.idOfNewLocation];

                // Check if there's room at the new location.
                if (pointData.occupants.Length < pointData.maxOccupants)
                {
                    var newLocationData = new DataLocation()
                    {
                        pointId = e.idOfNewLocation,
                        stageId = pointDatas[e.idOfNewLocation].parentStage,
                        siteId = pointDatas[e.idOfNewLocation].parentSite
                    };

                    characterLocations[eventsMoveRequest[i].mover] = newLocationData;
                }
            }
        }
    }

    protected override void OnUpdate()
    {
        var job = new SystemLocationManagerJob()
        {
            pointDatas = PointDatas,
            stageDatas = StageDatas,
            siteDatas = SiteDatas,
            characterLocations = CharacterLocations,
            eventsMoveRequest = EventsMoveRequest
        };

        job.Schedule();
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