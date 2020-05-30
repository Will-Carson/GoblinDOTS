using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;
using DOTSNET;

public class LocationManagerSystem : SystemBase
{
    // Accessible data:
    public NativeArray<PointData> PointDatas = new NativeArray<PointData>();
    public NativeArray<StageData> StageDatas = new NativeArray<StageData>();
    public NativeArray<SiteData> SiteDatas = new NativeArray<SiteData>();
    public NativeHashMap<int, LocationData> CharacterLocations = new NativeHashMap<int, LocationData>();

    // Events:
    public NativeList<EventMoveRequest> EventsMoveRequest = new NativeList<EventMoveRequest>();

    [BurstCompile]
    struct LocationManagerSystemJob : IJob
    {
        public NativeArray<PointData> pointDatas;
        public NativeArray<StageData> stageDatas;
        public NativeArray<SiteData> siteDatas;
        public NativeHashMap<int, LocationData> characterLocations;
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
                    var newLocationData = new LocationData()
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
        var job = new LocationManagerSystemJob()
        {
            pointDatas = PointDatas,
            stageDatas = StageDatas,
            siteDatas = SiteDatas,
            characterLocations = CharacterLocations,
            eventsMoveRequest = EventsMoveRequest
        };

        job.Schedule();
    }
}