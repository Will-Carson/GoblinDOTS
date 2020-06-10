﻿using Unity.Burst;
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
    public NativeMultiHashMap<int, int> PointOccupants = new NativeMultiHashMap<int, int>(G.occupantsPerPoint, Allocator.Persistent);

    public NativeArray<DataStage> DatasStage = new NativeArray<DataStage>(G.numberOfStages, Allocator.Persistent);
    public NativeMultiHashMap<int, int> StagePoints = new NativeMultiHashMap<int, int>(G.pointsPerStage, Allocator.Persistent);
    public NativeMultiHashMap<int, int> StageOccupants = new NativeMultiHashMap<int, int>(G.occupantsPerStage, Allocator.Persistent);

    public NativeArray<DataSite> SiteDatas = new NativeArray<DataSite>(G.numberOfSites, Allocator.Persistent);
    public NativeMultiHashMap<int, int> SiteStages = new NativeMultiHashMap<int, int>(G.stagesPerSite, Allocator.Persistent);
    public NativeMultiHashMap<int, int> SiteOccupants = new NativeMultiHashMap<int, int>(G.occupantsPerSite, Allocator.Persistent);

    public NativeHashMap<int, DataLocation> CharacterLocations = new NativeHashMap<int, DataLocation>(G.maxTotalPopulation, Allocator.Persistent);

    // Events:
    public NativeList<EventMoveRequest> EventsMoveRequest = new NativeList<EventMoveRequest>(G.maxTotalPopulation, Allocator.Persistent);

    [BurstCompile]
    struct ProcessLocationEventsJob : IJob
    {
        [ReadOnly] public NativeArray<DataPoint> pointDatas;
        [ReadOnly] public NativeMultiHashMap<int, int> pointOccupants;
        [ReadOnly] public NativeArray<DataStage> stageDatas;
        [ReadOnly] public NativeMultiHashMap<int, int> stageOccupants;
        [ReadOnly] public NativeArray<DataSite> siteDatas;

        public NativeHashMap<int, DataLocation> characterLocations;
        public NativeList<EventMoveRequest> eventsMoveRequest;

        public void Execute()
        {
            for (int i = 0; i < eventsMoveRequest.Length; i++)
            {
                var e = eventsMoveRequest[i];
                var pointData = pointDatas[e.location.pointId];
                var stageData = stageDatas[e.location.stageId];
                var po = pointOccupants.CountValuesForKey(e.location.pointId);
                var so = stageOccupants.CountValuesForKey(e.location.stageId);

                // Check if there's room at the new location.
                if (po < pointData.maxOccupants && so < stageData.maxOccupants)
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

            eventsMoveRequest.Clear();
        }
    }

    protected override void OnUpdate()
    {
        var job = new ProcessLocationEventsJob()
        {
            pointDatas = PointDatas,
            pointOccupants = PointOccupants,
            stageDatas = DatasStage,
            stageOccupants = StageOccupants,
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
        PointOccupants.Dispose();
        DatasStage.Dispose();
        StageOccupants.Dispose();
        StagePoints.Dispose();
        SiteDatas.Dispose();
        SiteOccupants.Dispose();
        SiteStages.Dispose();
        CharacterLocations.Dispose();
        EventsMoveRequest.Dispose();
    }
}