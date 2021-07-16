using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace QuarksWorld.Experimental
{
    public class ReplicatedEntityModuleServer
    {
        [ConfigVar(Name = "replicatedsysteminfo", DefaultValue = "0", Description = "Show replicated system info")]
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
            if (!gameObject.TryGetComponent(out ReplicatedEntity identity))
            {
                if (showInfo.IntValue > 0)
                    GameDebug.Log("ReplicatedEntityModuleServer. ReplicatedEntity is missing on gameObject");
                return;
            }

            entityCollection.Register((int)identity.id, gameObject);

            if (showInfo.IntValue > 0)
                GameDebug.Log("HandleReplicatedEntityDataDespawn.Initialize entity:" + gameObject + " type:" + identity.assetId + " id:" + identity.id);
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


        // Snapshot Part 

        class EntityInfo
        {
            public class EntitySnapshotInfo
            {
                public uint[] data = new uint[64 * 1024];
                public int length;      // length of data in words
            }

            public EntityInfo()
            {
                // snapshots = new SequenceBuffer<EntitySnapshotInfo>(NetworkConfig.snapshotDeltaCacheSize, () => new EntitySnapshotInfo());
            }

            public void Reset()
            {
                assetId = 0;
                spawnSequence = 0;
                despawnSequence = 0;
                updateSequence = 0;
                predictingClientId = -1;
                // snapshots.Clear();
            }

            public ushort assetId;
            public int predictingClientId = -1;
            public int spawnSequence;
            public int despawnSequence;
            public int updateSequence;

            // public SequenceBuffer<EntitySnapshotInfo> snapshots;
        }

        List<EntityInfo> entities = new List<EntityInfo>();
        List<int> freeEntities = new List<int>();
        int serverSequence;

        public int RegisterEntity(int id, ushort assetId, int predictingClientId)
        {
            EntityInfo entityInfo;
            int freeCount = freeEntities.Count;

            if (id >= 0)
            {
                GameDebug.Assert(entities[id].spawnSequence == 0, "RegisterEntity: Trying to reuse an id that is used by a scene entity");
                entityInfo = entities[id];
            }
            else if (freeCount > 0)
            {
                id = freeEntities[freeCount - 1];
                freeEntities.RemoveAt(freeCount - 1);
                entityInfo = entities[id];
                entityInfo.Reset();
            }
            else
            {
                entityInfo = new EntityInfo();
                entities.Add(entityInfo);
                id = entities.Count - 1;
            }

            entityInfo.assetId = assetId;
            entityInfo.predictingClientId = predictingClientId;
            entityInfo.spawnSequence = serverSequence + 1; // Associate the spawn with the next snapshot

            return id;
        }

        public void UnregisterEntity(int id)
        {
            entities[id].despawnSequence = serverSequence + 1;
        }
    }
}
