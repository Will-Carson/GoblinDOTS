using DOTSNET;
using Unity.Entities;

// Define world i.e., [ServerWorld, ClientWorld]
// Define relationships i.e., [UpdateBefore(typeof(BuildPhysicsWorld))]
// Define group name i.e., public class ApplyPhysicsGroup : ComponentSystemGroup {}

[ServerWorld]
public class ExecuteDrama : ComponentSystemGroup { }


///
/// ProcessLocationEventsJob needs to go after CacheLocationDataJob
/// ManageFactionAndFactionMembers needs to go after ProcessDeedOrRumorEventJob
/// SystemRunTask needs to go after... SystemRunTask?
/// 
///

public class JobScheduler<PR, PE, QR, TR> : SystemBase
    where PR : unmanaged, IPlayRequirement
    where PE : unmanaged, IPlayExecution
    where QR : unmanaged, IQuestRequirements
    where TR : unmanaged, ITaskRequirement
{
    [AutoAssign] SystemCheckQuest SQT;
    [AutoAssign] SystemFindValidDialogue SFVD;
    [AutoAssign] SystemFindValidPlay<PR, PE> SFVP;
    [AutoAssign] SystemFindValidQuest<QR> SFVQ;
    [AutoAssign] SystemFindValidTask<TR> SFVT;
    [AutoAssign] SystemLocationManager SLM;
    [AutoAssign] SystemRunDialogue SRD;
    [AutoAssign] SystemRunPlay<PE> SRP;
    [AutoAssign] SystemRunTask SRT;
    [AutoAssign] SystemWorldStateEvaluation SWSE;
    [AutoAssign] SystemManageFactionAndFactionMembers SMFAFM;
    [AutoAssign] SystemProcessDeedOrRumorEvent SPDORE;
    [AutoAssign] SystemBuildLocationData SBLD;
    
    protected override void OnUpdate()
    {
        
    }
}