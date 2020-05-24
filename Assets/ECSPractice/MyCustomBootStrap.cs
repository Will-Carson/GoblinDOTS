using Unity.Entities;
using UnityEngine;

class MyCustomBootStrap// : ICustomBootstrap
{
    public bool Initialize(string defaultWorldName)
    {
        Debug.Log("Executing bootstrap");
        var world = new World("Custom world");
        World.DefaultGameObjectInjectionWorld = world;
        var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);

        var e = world.EntityManager.CreateEntity();

        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);
        return true;
    }
}
