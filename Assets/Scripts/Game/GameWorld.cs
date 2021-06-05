using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;
using System;

namespace QuarksWorld
{
    public class GameWorld
    {
        [ConfigVar(Name = "gameobjecthierarchy", Description = "Should gameobject be organized in a gameobject hierarchy", DefaultValue = "0")]
        public static ConfigVar gameobjectHierarchy;

        public static List<GameWorld> Worlds = new List<GameWorld>();
        
        public float FrameDuration { get; set; }

        // public GameTime worldTime;

        public int lastServerTick;

        public double nextTickTime = 0;

        public event Action<GameObject> OnSpawn;
        public event Action<GameObject> OnDespawn;

        public List<GameObject> GameObjects
        {
            get { return gameObjects; }
        }

        // SceneRoot can be used to organize crated gameobject in scene view. Is null in standalone.
        public GameObject SceneRoot => sceneRoot;

        public GameWorld(string name = "world")
        {
            GameDebug.Log("GameWorld " + name + " initializing");

            if (gameobjectHierarchy.IntValue == 1)
            {
                sceneRoot = new GameObject(name);
                Object.DontDestroyOnLoad(sceneRoot);
            }

            // worldTime.TickRate = 60;

            nextTickTime = Game.frameTime;

            Worlds.Add(this);
        }

        public void Shutdown()
        {
            foreach (var entity in gameObjects)
            {
                if (despawnRequests.Contains(entity))
                    continue;         

                RequestDespawn(entity);
            }

            ProcessDespawns();

            Worlds.Remove(this);

            Object.Destroy(sceneRoot);
        }

        public T Spawn<T>(GameObject prefab) where T : Component
        {
            return Spawn<T>(prefab, Vector3.zero, Quaternion.identity);
        }

        public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            var gameObject = SpawnInternal(prefab, position, rotation);
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

        public GameObject Spawn(GameObject prefab)
        {
            return SpawnInternal(prefab, Vector3.zero, Quaternion.identity);
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return SpawnInternal(prefab, position, rotation);
        }

        public GameObject Spawn(string name, params Type[] components)
        {
            var go = new GameObject(name, components);

            RegisterInternal(go);

            return go;
        }

        public GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var go = Object.Instantiate(prefab, position, rotation);

            RegisterInternal(go);

            return go;
        }

        public void RequestDespawn(GameObject entity)
        {
            if (despawnRequests.Contains(entity))
            {
                GameDebug.Assert(false, "Trying to request depawn of same gameobject({0}) multiple times", entity.name);
                return;
            }

            despawnRequests.Add(entity);
        }

        public void ProcessDespawns()
        {
            foreach (var gameObject in despawnRequests)
            {
                OnDespawn?.Invoke(gameObject);
                gameObjects.Remove(gameObject);
                Object.Destroy(gameObject);
            }

            despawnRequests.Clear();
        }

        void RegisterInternal(GameObject gameObject)
        {
            gameObjects.Add(gameObject);
            OnSpawn?.Invoke(gameObject);
        }

        GameObject sceneRoot;

        List<GameObject> gameObjects = new List<GameObject>();
        List<GameObject> despawnRequests = new List<GameObject>(32);
    }
}
