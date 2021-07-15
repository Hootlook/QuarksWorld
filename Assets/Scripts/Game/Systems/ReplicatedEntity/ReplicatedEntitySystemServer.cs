using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld.Old
{
    public class ReplicatedEntityModuleServer
    {
        [ConfigVar(Name = "sv.replicatedsysteminfo", DefaultValue = "0", Description = "Show replicated system info")]
        public static ConfigVar showInfo;

        public ReplicatedEntityModuleServer(GameWorld gameWorld)
        {
            world = gameWorld;
            world.OnSpawn += HandleSpawning;
            world.OnDespawn += HandleDespawning;
            
            entityCollection = new ReplicatedEntityCollection();
        }

        public void Shutdown()
        {
            world.OnSpawn -= HandleSpawning;
            world.OnDespawn -= HandleDespawning;
        }

        public void GenerateEntitySnapshot(int entityId, NetworkWriter writer) => entityCollection.GenerateEntitySnapshot(entityId, writer);

        public string GenerateName(int entityId) => entityCollection.GenerateName(entityId);

        void HandleSpawning(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out NetworkIdentity identity))
            {
                if (showInfo.IntValue > 0)
                    GameDebug.Log("ReplicatedEntityModuleServer. ReplicatedEntity is missing on gameObject");
                return;
            }

            NetworkServer.Spawn(gameObject);

            entityCollection.Register((int)identity.netId, gameObject);

            if (showInfo.IntValue > 0)
                GameDebug.Log("HandleReplicatedEntityDataDespawn.Initialize entity:" + gameObject + " type:" + identity.assetId + " id:" + identity.netId);
        }

        void HandleDespawning(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out NetworkIdentity identity))
            {
                if (showInfo.IntValue > 0)
                    GameDebug.Log("ReplicatedEntityModuleServer. ReplicatedEntity is missing on gameObject");
                return;
            }

            entityCollection.Unregister((int)identity.netId);
            
            NetworkServer.UnSpawn(gameObject);

            if (showInfo.IntValue > 0)
                GameDebug.Log("HandleReplicatedEntityDataDespawn.Deinitialize entity:" + gameObject + " id:" + identity.netId);
        }

        

        public ReplicatedEntityCollection entityCollection;

        private GameWorld world;
    }
}