using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;

using QuarksWorld;
using Unity.Collections;

public struct DespawningEntity : IComponentData { }

 public abstract class SystemBase : Unity.Entities.SystemBase
{
    protected SystemBase() { }

    protected SystemBase(GameWorld world)
    {
        this.world = world;
    }

    readonly protected GameWorld world;
}

 public abstract class SystemBase<T1> : SystemBase
     where T1 : MonoBehaviour
{
    EntityQuery Group;
    protected ComponentType[] ExtraComponentRequirements;
    string name;

    public SystemBase(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        var list = new List<ComponentType>(6);
        if (ExtraComponentRequirements != null)
            list.AddRange(ExtraComponentRequirements);
        list.AddRange(new ComponentType[] { typeof(T1) });
        list.Add(ComponentType.Exclude<DespawningEntity>());
        Group = GetEntityQuery(list.ToArray());
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var entityArray = Group.ToEntityArray(Allocator.Temp);
        var dataArray = Group.ToComponentArray<T1>();

        for (var i = 0; i < entityArray.Length; i++)
        {
            Update(entityArray[i], dataArray[i]);
        }

        Profiler.EndSample();
    }

    protected abstract void Update(Entity entity, T1 data);
}

 public abstract class SystemBase<T1, T2> : SystemBase
    where T1 : MonoBehaviour
    where T2 : MonoBehaviour
{
    EntityQuery Group;
    protected ComponentType[] ExtraComponentRequirements;
    string name;

    public SystemBase(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        var list = new List<ComponentType>(6);
        if (ExtraComponentRequirements != null)
            list.AddRange(ExtraComponentRequirements);
        list.AddRange(new ComponentType[] { typeof(T1), typeof(T2) });
        list.Add(ComponentType.Exclude<DespawningEntity>());
        Group = GetEntityQuery(list.ToArray());
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var entityArray = Group.ToEntityArray(Allocator.Temp);
        var dataArray1 = Group.ToComponentArray<T1>();
        var dataArray2 = Group.ToComponentArray<T2>();

        for (var i = 0; i < entityArray.Length; i++)
        {
            Update(entityArray[i], dataArray1[i], dataArray2[i]);
        }

        Profiler.EndSample();
    }

    protected abstract void Update(Entity entity, T1 data1, T2 data2);
}

 public abstract class SystemBase<T1, T2, T3> : SystemBase
    where T1 : MonoBehaviour
    where T2 : MonoBehaviour
    where T3 : MonoBehaviour
{
    EntityQuery Group;
    protected ComponentType[] ExtraComponentRequirements;
    string name;

    public SystemBase(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        var list = new List<ComponentType>(6);
        if (ExtraComponentRequirements != null)
            list.AddRange(ExtraComponentRequirements);
        list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3) });
        list.Add(ComponentType.Exclude<DespawningEntity>());
        Group = GetEntityQuery(list.ToArray());
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var entityArray = Group.ToEntityArray(Allocator.Temp);
        var dataArray1 = Group.ToComponentArray<T1>();
        var dataArray2 = Group.ToComponentArray<T2>();
        var dataArray3 = Group.ToComponentArray<T3>();

        for (var i = 0; i < entityArray.Length; i++)
        {
            Update(entityArray[i], dataArray1[i], dataArray2[i], dataArray3[i]);
        }

        Profiler.EndSample();
    }

    protected abstract void Update(Entity entity, T1 data1, T2 data2, T3 data3);
}

 public abstract class DataSystemBase<T1> : SystemBase
    where T1 : struct, IComponentData
{
    EntityQuery Group;
    protected ComponentType[] ExtraComponentRequirements;
    string name;

    public DataSystemBase(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        var list = new List<ComponentType>(6);
        if (ExtraComponentRequirements != null)
            list.AddRange(ExtraComponentRequirements);
        list.AddRange(new ComponentType[] { typeof(T1) });
        list.Add(ComponentType.Exclude<DespawningEntity>());
        Group = GetEntityQuery(list.ToArray());
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var entityArray = Group.ToEntityArray(Allocator.Temp);
        var dataArray = Group.ToComponentDataArray<T1>(Allocator.Temp);

        for (var i = 0; i < entityArray.Length; i++)
        {
            Update(entityArray[i], dataArray[i]);
        }

        Profiler.EndSample();
    }

    protected abstract void Update(Entity entity, T1 data);
}

 public abstract class DataSystemBase<T1, T2> : SystemBase
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
{
    EntityQuery Group;
    protected ComponentType[] ExtraComponentRequirements;
    private string name;

    public DataSystemBase(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        name = GetType().Name;
        base.OnCreate();
        var list = new List<ComponentType>(6);
        if (ExtraComponentRequirements != null)
            list.AddRange(ExtraComponentRequirements);
        list.AddRange(new ComponentType[] { typeof(T1), typeof(T2) });
        list.Add(ComponentType.Exclude<DespawningEntity>());
        Group = GetEntityQuery(list.ToArray());
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var entityArray = Group.ToEntityArray(Allocator.Temp);
        var dataArray1 = Group.ToComponentDataArray<T1>(Allocator.Temp);
        var dataArray2 = Group.ToComponentDataArray<T2>(Allocator.Temp);

        for (var i = 0; i < entityArray.Length; i++)
        {
            Update(entityArray[i], dataArray1[i], dataArray2[i]);
        }

        Profiler.EndSample();
    }

    protected abstract void Update(Entity entity, T1 data1, T2 data2);
}

 public abstract class DataSystemBase<T1, T2, T3> : SystemBase
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
    where T3 : struct, IComponentData
{
    EntityQuery Group;
    protected ComponentType[] ExtraComponentRequirements;
    string name;

    public DataSystemBase(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        var list = new List<ComponentType>(6);
        if (ExtraComponentRequirements != null)
            list.AddRange(ExtraComponentRequirements);
        list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3) });
        list.Add(ComponentType.Exclude<DespawningEntity>());
        Group = GetEntityQuery(list.ToArray());
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var entityArray = Group.ToEntityArray(Allocator.Temp);
        var dataArray1 = Group.ToComponentDataArray<T1>(Allocator.Temp);
        var dataArray2 = Group.ToComponentDataArray<T2>(Allocator.Temp);
        var dataArray3 = Group.ToComponentDataArray<T3>(Allocator.Temp);

        for (var i = 0; i < entityArray.Length; i++)
        {
            Update(entityArray[i], dataArray1[i], dataArray2[i], dataArray3[i]);
        }

        Profiler.EndSample();
    }

    protected abstract void Update(Entity entity, T1 data1, T2 data2, T3 data3);
}

 public abstract class DataSystemBase<T1, T2, T3, T4> : SystemBase
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
    where T3 : struct, IComponentData
    where T4 : struct, IComponentData
{
    EntityQuery Group;
    protected ComponentType[] ExtraComponentRequirements;
    string name;

    public DataSystemBase(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        var list = new List<ComponentType>(6);
        if (ExtraComponentRequirements != null)
            list.AddRange(ExtraComponentRequirements);
        list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) });
        list.Add(ComponentType.Exclude<DespawningEntity>());
        Group = GetEntityQuery(list.ToArray());
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var entityArray = Group.ToEntityArray(Allocator.Temp);
        var dataArray1 = Group.ToComponentDataArray<T1>(Allocator.Temp);
        var dataArray2 = Group.ToComponentDataArray<T2>(Allocator.Temp);
        var dataArray3 = Group.ToComponentDataArray<T3>(Allocator.Temp);
        var dataArray4 = Group.ToComponentDataArray<T4>(Allocator.Temp);

        for (var i = 0; i < entityArray.Length; i++)
        {
            Update(entityArray[i], dataArray1[i], dataArray2[i], dataArray3[i], dataArray4[i]);
        }

        Profiler.EndSample();
    }

    protected abstract void Update(Entity entity, T1 data1, T2 data2, T3 data3, T4 data4);
}

 public abstract class DataSystemBase<T1, T2, T3, T4, T5> : SystemBase
    where T1 : struct, IComponentData
    where T2 : struct, IComponentData
    where T3 : struct, IComponentData
    where T4 : struct, IComponentData
    where T5 : struct, IComponentData
{
    EntityQuery Group;
    protected ComponentType[] ExtraComponentRequirements;
    string name;

    public DataSystemBase(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        var list = new List<ComponentType>(6);
        if (ExtraComponentRequirements != null)
            list.AddRange(ExtraComponentRequirements);
        list.AddRange(new ComponentType[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) });
        list.Add(ComponentType.Exclude<DespawningEntity>());
        Group = GetEntityQuery(list.ToArray());
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var entityArray = Group.ToEntityArray(Allocator.Temp);
        var dataArray1 = Group.ToComponentDataArray<T1>(Allocator.Temp);
        var dataArray2 = Group.ToComponentDataArray<T2>(Allocator.Temp);
        var dataArray3 = Group.ToComponentDataArray<T3>(Allocator.Temp);
        var dataArray4 = Group.ToComponentDataArray<T4>(Allocator.Temp);
        var dataArray5 = Group.ToComponentDataArray<T5>(Allocator.Temp);

        for (var i = 0; i < entityArray.Length; i++)
        {
            Update(entityArray[i], dataArray1[i], dataArray2[i], dataArray3[i], dataArray4[i], dataArray5[i]);
        }

        Profiler.EndSample();
    }

    protected abstract void Update(Entity entity, T1 data1, T2 data2, T3 data3, T4 data4, T5 data5);
}

 
 [AlwaysUpdateSystem]
public abstract class InitializeComponentSystem<T> : SystemBase
    where T : MonoBehaviour
{
    EndInitializationEntityCommandBufferSystem entityCommandBufferSystem;

    struct SystemState : IComponentData { }
    EntityQuery IncomingGroup;
    string name;

    public InitializeComponentSystem(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        IncomingGroup = GetEntityQuery(typeof(T), ComponentType.Exclude<SystemState>());

        entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var postUpdateCommands = entityCommandBufferSystem.CreateCommandBuffer();
        var incomingEntityArray = IncomingGroup.ToEntityArray(Allocator.Temp);
        if (incomingEntityArray.Length > 0)
        {
            var incomingComponentArray = IncomingGroup.ToComponentArray<T>();
            for (var i = 0; i < incomingComponentArray.Length; i++)
            {
                var entity = incomingEntityArray[i];
                postUpdateCommands.AddComponent(entity, new SystemState());

                Initialize(entity, incomingComponentArray[i]);
            }
        }

        Profiler.EndSample();
    }

    protected abstract void Initialize(Entity entity, T component);
}

 [AlwaysUpdateSystem]
public abstract class InitializeComponentDataSystem<T, K> : SystemBase
    where T : struct, IComponentData
    where K : struct, IComponentData
{
    EndInitializationEntityCommandBufferSystem entityCommandBufferSystem;

    EntityQuery IncomingGroup;
    string name;

    public InitializeComponentDataSystem(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        IncomingGroup = GetEntityQuery(typeof(T), ComponentType.Exclude<K>());

        entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var postUpdateCommands = entityCommandBufferSystem.CreateCommandBuffer();
        var incomingEntityArray = IncomingGroup.ToEntityArray(Allocator.Temp);
        if (incomingEntityArray.Length > 0)
        {
            var incomingComponentDataArray = IncomingGroup.ToComponentDataArray<T>(Allocator.Temp);
            for (var i = 0; i < incomingComponentDataArray.Length; i++)
            {
                var entity = incomingEntityArray[i];
                postUpdateCommands.AddComponent(entity, new K());

                Initialize(entity, incomingComponentDataArray[i]);
            }
        }

        Profiler.EndSample();
    }

    protected abstract void Initialize(Entity entity, T component);
}


 [AlwaysUpdateSystem]
public abstract class DeinitializeComponentSystem<T> : SystemBase
    where T : MonoBehaviour
{
    EntityQuery OutgoingGroup;
    string name;

    public DeinitializeComponentSystem(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        OutgoingGroup = GetEntityQuery(typeof(T), typeof(DespawningEntity));
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var outgoingComponentArray = OutgoingGroup.ToComponentArray<T>();
        var outgoingEntityArray = OutgoingGroup.ToEntityArray(Allocator.Temp);
        for (var i = 0; i < outgoingComponentArray.Length; i++)
        {
            Deinitialize(outgoingEntityArray[i], outgoingComponentArray[i]);
        }

        Profiler.EndSample();
    }

    protected abstract void Deinitialize(Entity entity, T component);
}

 [AlwaysUpdateSystem]
public abstract class DeinitializeComponentDataSystem<T> : SystemBase
    where T : struct, IComponentData
{
    EntityQuery OutgoingGroup;
    string name;

    public DeinitializeComponentDataSystem(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        OutgoingGroup = GetEntityQuery(typeof(T), typeof(DespawningEntity));
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var outgoingComponentArray = OutgoingGroup.ToComponentDataArray<T>(Allocator.Temp);
        var outgoingEntityArray = OutgoingGroup.ToEntityArray(Allocator.Temp);
        for (var i = 0; i < outgoingComponentArray.Length; i++)
        {
            Deinitialize(outgoingEntityArray[i], outgoingComponentArray[i]);
        }

        Profiler.EndSample();
    }

    protected abstract void Deinitialize(Entity entity, T component);
}


 [AlwaysUpdateSystem]
public abstract class InitializeEntityQuerySystem<T, S> : SystemBase
    where T : MonoBehaviour
    where S : struct, IComponentData
{
    EndInitializationEntityCommandBufferSystem entityCommandBufferSystem;

    EntityQuery IncomingGroup;
    string name;

    public InitializeEntityQuerySystem(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        IncomingGroup = GetEntityQuery(typeof(T), ComponentType.Exclude<S>());

        entityCommandBufferSystem = World.GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        var postUpdateCommands = entityCommandBufferSystem.CreateCommandBuffer();
        var incomingEntityArray = IncomingGroup.ToEntityArray(Allocator.Temp);
        if (incomingEntityArray.Length > 0)
        {
            for (var i = 0; i < incomingEntityArray.Length; i++)
            {
                var entity = incomingEntityArray[i];
                postUpdateCommands.AddComponent(entity, new S());
            }
            Initialize(ref IncomingGroup);
        }
        Profiler.EndSample();
    }

    protected abstract void Initialize(ref EntityQuery group);
}

 [AlwaysUpdateSystem]
public abstract class DeinitializeEntityQuerySystem<T> : SystemBase
    where T : MonoBehaviour
{
    EntityQuery OutgoingGroup;
    string name;

    public DeinitializeEntityQuerySystem(GameWorld world) : base(world) { }

    protected override void OnCreate()
    {
        base.OnCreate();
        name = GetType().Name;
        OutgoingGroup = GetEntityQuery(typeof(T), typeof(DespawningEntity));
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample(name);

        if (OutgoingGroup.CalculateChunkCount() > 0)
            Deinitialize(ref OutgoingGroup);

        Profiler.EndSample();
    }

    protected abstract void Deinitialize(ref EntityQuery group);
}
