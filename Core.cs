using BepInEx.Logging;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace VRisingUtilities;

internal static class Core
{
    public static World Server { get; } = GetWorld("Server") ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");

    // V Rising systems
    public static EntityManager EntityManager { get; } = Server.EntityManager;
    public static PrefabCollectionSystem PrefabCollectionSystem { get; internal set; }
    public static ServerScriptMapper ServerScriptMapper { get; internal set; }
    public static ServerGameManager ServerGameManager => ServerScriptMapper.GetServerGameManager();

    // BepInEx services
    public static ManualLogSource Log => Plugin.LogInstance;

    static bool hasInitialized;

    public static readonly PrefabGUID ExternalInventoryPrefab = new(1183666186);

    public static void Initialize()
    {
        if (hasInitialized) return;

        PrefabCollectionSystem = Server.GetExistingSystemManaged<PrefabCollectionSystem>();
        ServerScriptMapper = Server.GetExistingSystemManaged<ServerScriptMapper>();

        ModifyPedestalInventories();

        hasInitialized = true;
    }

    private static void ModifyPedestalInventories()
    {
        Log.LogInfo($"Modifying soulshard pedestals");

        var entityManager = EntityManager;
        var serverGameManager = Core.ServerGameManager;

        var eqb = new EntityQueryBuilder(Allocator.Temp)
            .AddAll(new(Il2CppType.Of<PrefabGUID>(), ComponentType.AccessMode.ReadOnly))
            .AddAll(new(Il2CppType.Of<InventoryOwner>(), ComponentType.AccessMode.ReadOnly))
            .AddAll(new(Il2CppType.Of<AttachedBuffer>(), ComponentType.AccessMode.ReadOnly))
            .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab);

        var eq = Core.EntityManager.CreateEntityQuery(ref eqb);
        eqb.Dispose();

        var entities = eq.ToEntityArray(Allocator.Temp);

        Log.LogInfo($"Found {entities.Length} enities matching query for pedestal");

        foreach (var entity in entities)
        {
            
            var guid = entity.Read<PrefabGUID>();
            var name = guid.LookupName();
            if (name.Contains("TM_Castle_Container_Specialized_Soulshards_"))
            {
                Log.LogDebug($"Entity {name} has component types: \n{string.Join("\n\t\t", entity.GetComponentTypeStrings())}");

                if (!serverGameManager.TryGetBuffer<AttachedBuffer>(entity, out var buffer))
                    continue;

                foreach (var attachedBuffer in buffer)
                {
                    var attachedEntity = attachedBuffer.Entity;
                    if (!attachedEntity.Has<PrefabGUID>()) continue;
                    if (!attachedEntity.Read<PrefabGUID>().Equals(ExternalInventoryPrefab)) continue;

                    var inventoryBuffer = attachedEntity.ReadBuffer<InventoryBuffer>();
                    Log.LogDebug($"Found inventory for {name} with {inventoryBuffer.Length} slots.");

                    int desiredSlots = 8; // TODO: get from config or command
                    if (inventoryBuffer.Length < desiredSlots)
                    {
                        inventoryBuffer.Resize(desiredSlots, NativeArrayOptions.ClearMemory);
                        Log.LogInfo($"Resized inventory for {name} to {desiredSlots} slots.");
                    }
                }
            }
        }
    }

    static public World GetWorld(string name)
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
