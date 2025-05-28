using System.Linq;
using BepInEx.Logging;
using Il2CppInterop.Runtime;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace SoulShardUtilities;

internal static class Core
{
    public static World Server { get; } = GetWorld("Server") ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");

    // V Rising systems
    public static EntityManager EntityManager { get; } = Server.EntityManager;
    public static PrefabCollectionSystem PrefabCollectionSystem { get; internal set; }

    // BepInEx services
    public static ManualLogSource Log => Plugin.LogInstance;

    public static void Initialize()
    {
        PrefabCollectionSystem = Server.GetExistingSystemManaged<PrefabCollectionSystem>();

        ModifyPedestalInventories();

        Log.LogInfo($"Core initialized");
    }

    private static void ModifyPedestalInventories()
    {
        Log.LogInfo($"Modifying soulshard pedestals");

        var entityManager = EntityManager;

        var eqb = new EntityQueryBuilder(Allocator.Temp)
            .AddAll(new(Il2CppType.Of<PrefabGUID>(), ComponentType.AccessMode.ReadOnly))
            .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab);

        var eqbi = new EntityQueryBuilder(Allocator.Temp)
            .AddAll(new(Il2CppType.Of<InventoryConnection>(), ComponentType.AccessMode.ReadOnly))
            .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab);

        var eq = Core.EntityManager.CreateEntityQuery(ref eqb);
        var eqi = Core.EntityManager.CreateEntityQuery(ref eqbi);
        eqb.Dispose();
        eqbi.Dispose();

        var entities = eq.ToEntityArray(Allocator.Temp);
        var inventoryEntities = eqi.ToEntityArray(Allocator.Temp);

        Log.LogInfo($"Found {entities.Length} enities matching query");

        foreach (var entity in entities)
        {
            
            var guid = entity.Read<PrefabGUID>();
            var name = guid.LookupName();
            if (name.Contains("TM_Castle_Container_Specialized_Soulshards_"))
            {
                var types = entity.GetComponentTypeStrings();
                foreach (var type in types)
                {
                    Log.LogInfo($"Entity {name} has component types: {string.Join(", ", types)}");
                }

                foreach (var inventoryEntity in inventoryEntities)
                {
                    var conn = inventoryEntity.Read<InventoryConnection>();
                    if (conn.InventoryOwner == entity)
                    {
                        var inventoryBuffer = entityManager.GetBuffer<InventoryBuffer>(inventoryEntity);
                        
                        int desiredSlots = 8; // TODO: get from config or command
                        if (inventoryBuffer.Length < desiredSlots)
                        {
                            inventoryBuffer.ResizeUninitialized(desiredSlots);
                            Log.LogInfo($"Resized inventory for {name} to {desiredSlots} slots.");
                        }
                    }
                }
            }
        }
    }

    static World GetWorld(string name)
    {
        foreach (var world in World.s_AllWorlds)
        {
            if (world.Name == name)
            {
                return world;
            }
        }

        return null;
    }
}
