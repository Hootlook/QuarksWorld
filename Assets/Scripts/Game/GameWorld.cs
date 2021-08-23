using Object = UnityEngine.Object;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System;

namespace QuarksWorld
{
    [DisableAutoCreation]
    public class DestroyDespawning : ComponentSystem
    {
        EntityQuery Group;

        protected override void OnCreate()
        {
            base.OnCreate();
            Group = GetEntityQuery(typeof(DespawningEntity));
        }

        protected override void OnUpdate()
        {
            var entityArray = Group.ToEntityArray(Allocator.Temp);
            for (var i = 0; i < entityArray.Length; i++)
            {
                PostUpdateCommands.DestroyEntity(entityArray[i]);
            }
        }
    }

    public class GameWorld
    {
        [ConfigVar(Name = "gameobjecthierarchy", Description = "Should gameobject be organized in a gameobject hierarchy", DefaultValue = "0")]
        public static ConfigVar gameobjectHierarchy;

        public static List<GameWorld> Worlds = new List<GameWorld>();
        
        public float FrameDuration { get; set; }

        public GameTime worldTime;

        public double lastServerTick;
        public double nextTickTime = 0;

        // SceneRoot can be used to organize crated gameobject in scene view. Is null in standalone.
        public GameObject SceneRoot => sceneRoot;
        public World GetECSWorld() => ECSWorld;
        public EntityManager GetEntityManager() => ECSWorld.EntityManager;

        public GameWorld(string name = "world")
        {
            GameDebug.Log("GameWorld " + name + " initializing");

            if (gameobjectHierarchy.IntValue == 1)
            {
                sceneRoot = new GameObject(name);
                Object.DontDestroyOnLoad(sceneRoot);
            }

            ECSWorld = World.DefaultGameObjectInjectionWorld;

            worldTime.TickRate = 60;

            nextTickTime = Game.frameTime;

            Worlds.Add(this);

            destroyDespawningSystem = ECSWorld.CreateSystem<DestroyDespawning>();
        }

        public void Shutdown()
        {
            GameDebug.Log("GameWorld " + ECSWorld.Name + " shutting down");

            foreach (var entity in dynamicEntities)
            {
                if (despawnRequests.Contains(entity))
                    continue;

                if (entity == null)
                    continue;

                var gameObjectEntity = entity.GetComponent<GameObjectEntity>();
                if (gameObjectEntity != null && !ECSWorld.EntityManager.Exists(gameObjectEntity.Entity))
                    continue;

                RequestDespawn(entity);
            }
            ProcessDespawns();

            Worlds.Remove(this);

            GameObject.Destroy(sceneRoot);

            ECSWorld.DestroySystem(destroyDespawningSystem);
        }


        public T Spawn<T>(GameObject prefab) where T : Component
        {
            return Spawn<T>(prefab, Vector3.zero, Quaternion.identity);
        }

        public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            var gameObject = SpawnInternal(prefab, position, rotation, out _);
            if (gameObject == null)
                return null;

            var result = gameObject.GetComponent<T>();
            if (result == null)
            {
                GameDebug.Log(string.Format("Spawned entity '{0}' didn't have component '{1}'", prefab, typeof(T).FullName));
                return null;
            }

            return result;
        }

        public GameObject Spawn(string name, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            RegisterInternal(go, true);
            return go;
        }

        public GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation, out Entity entity)
        {
            var go = Object.Instantiate(prefab, position, rotation);

            entity = RegisterInternal(go, true);

            return go;
        }

        public void RequestDespawn(GameObject entity)
        {
            if (despawnRequests.Contains(entity))
            {
                GameDebug.Assert(false, "Trying to request depawn of same gameobject({0}) multiple times", entity.name);
                return;
            }

            var gameObjectEntity = entity.GetComponent<GameObjectEntity>();
            if (gameObjectEntity != null)
                ECSWorld.EntityManager.AddComponent(gameObjectEntity.Entity, typeof(DespawningEntity));

            despawnRequests.Add(entity);
        }

        public void RequestDespawn(GameObject entity, EntityCommandBuffer commandBuffer)
        {
            if (despawnRequests.Contains(entity))
            {
                GameDebug.Assert(false, "Trying to request depawn of same gameobject({0}) multiple times", entity.name);
                return;
            }

            var gameObjectEntity = entity.GetComponent<GameObjectEntity>();
            if (gameObjectEntity != null)
                commandBuffer.AddComponent(gameObjectEntity.Entity, new DespawningEntity());

            despawnRequests.Add(entity);
        }

        public void RequestDespawn(Entity entity)
        {
            ECSWorld.EntityManager.AddComponent(entity, typeof(DespawningEntity));
            despawnEntityRequests.Add(entity);
        }

        public void RequestDespawn(EntityCommandBuffer commandBuffer, Entity entity)
        {
            if (despawnEntityRequests.Contains(entity))
            {
                GameDebug.Assert(false, "Trying to request depawn of same gameobject({0}) multiple times", entity);
                return;
            }

            commandBuffer.AddComponent(entity, new DespawningEntity());
            despawnEntityRequests.Add(entity);
        }

        public void ProcessDespawns()
        {
            foreach (var gameObject in despawnRequests)
            {
                dynamicEntities.Remove(gameObject);
                Object.Destroy(gameObject);
            }

            foreach (var entity in despawnEntityRequests)
            {
                ECSWorld.EntityManager.DestroyEntity(entity);
            }
            despawnEntityRequests.Clear();
            despawnRequests.Clear();

            destroyDespawningSystem.Update();
        }

        Entity RegisterInternal(GameObject gameObject, bool isDynamic)
        {
            // If gameObject has GameObjectEntity it is already registered in entitymanager. If not we register it here  
            var gameObjectEntity = gameObject.GetComponent<GameObjectEntity>();
            if (gameObjectEntity == null)
                GameObjectEntity.AddToEntityManager(ECSWorld.EntityManager, gameObject);

            if (isDynamic)
                dynamicEntities.Add(gameObject);

            return gameObjectEntity != null ? gameObjectEntity.Entity : Entity.Null;
        }

        World ECSWorld;

        GameObject sceneRoot;

        DestroyDespawning destroyDespawningSystem;

        List<GameObject> dynamicEntities = new List<GameObject>();
        List<GameObject> despawnRequests = new List<GameObject>(32);
        List<Entity> despawnEntityRequests = new List<Entity>(32);
    }
    
    public struct GameTime
    {
        /// <summary>Number of ticks per second.</summary>
        public int TickRate
        {
            get { return tickRate; }
            set
            {
                tickRate = value;
                TickInterval = 1.0f / tickRate;
            }
        }

        /// <summary>Length of each world tick at current tickrate, e.g. 0.0166s if ticking at 60fps.</summary>
        public float TickInterval { get; private set; }     // Time between ticks
        public int tick;                                    // Current tick   
        public float tickDuration;                          // Duration of current tick

        public GameTime(int tickRate)
        {
            this.tickRate = tickRate;
            this.TickInterval = 1.0f / this.tickRate;
            this.tick = 1;
            this.tickDuration = 0;
        }

        public float TickDurationAsFraction => tickDuration / TickInterval;

        public void SetTime(int tick, float tickDuration)
        {
            this.tick = tick;
            this.tickDuration = tickDuration;
        }

        public float DurationSinceTick(int tick) => (this.tick - tick) * TickInterval + tickDuration;

        public void AddDuration(float duration)
        {
            tickDuration += duration;
            int deltaTicks = Mathf.FloorToInt(tickDuration * TickRate);
            tick += deltaTicks;
            tickDuration %= TickInterval;
        }

        public static float GetDuration(GameTime start, GameTime end)
        {
            if (start.TickRate != end.TickRate)
            {
                GameDebug.LogError("Trying to compare time with different tick rates (" + start.TickRate + " and " + end.TickRate + ")");
                return 0;
            }

            float result = (end.tick - start.tick) * start.TickInterval + end.tickDuration - start.tickDuration;
            return result;
        }

        int tickRate;
    }
}
