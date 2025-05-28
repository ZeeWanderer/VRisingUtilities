using Stunlock.Core;
using System;
using Il2CppInterop.Runtime;
using System.Runtime.InteropServices;
using Unity.Entities;
using ProjectM;
using System.Collections.Generic;
using System.Linq;

namespace VRisingUtilities;

internal static class Extensions
{
    static EntityManager EntityManager { get; } = Core.EntityManager;
    static PrefabCollectionSystem PrefabCollectionSystem { get; } = Core.PrefabCollectionSystem;

    public static string LookupName(this PrefabGUID prefabGuid)
    {
        return (PrefabCollectionSystem._PrefabLookupMap.TryGetName(prefabGuid, out var name)
            ? name + " " + prefabGuid : "GUID Not Found").ToString();
    }

    public static unsafe T Read<T>(this Entity entity) where T : struct
    {
        // Get the ComponentType for T
        var ct = new ComponentType(Il2CppType.Of<T>());

        // Get a pointer to the raw component data
        void* rawPointer = EntityManager.GetComponentDataRawRO(entity, ct.TypeIndex);

        // Marshal the raw data to a T struct
        T componentData = Marshal.PtrToStructure<T>(new IntPtr(rawPointer));

        return componentData;
    }

    public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : struct
    {
        return Core.Server.EntityManager.GetBuffer<T>(entity);
    }

    public static bool Has<T>(this Entity entity)
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        return EntityManager.HasComponent(entity, ct);
    }

    public static List<Il2CppSystem.Type> GetComponentTypes(this Entity entity)
    {
        var types = new List<Il2CppSystem.Type>();
        var componentTypes = EntityManager.GetComponentTypes(entity);
        
        foreach (var type in componentTypes)
        {
            types.Add(TypeManager.GetType(type.TypeIndex));
        }
        
        return types;
    }

    public static List<string> GetComponentTypeStrings(this Entity entity)
    {
        var types = entity.GetComponentTypes().Select(t => t.ToString()).ToList();
        return types;
    }
}
