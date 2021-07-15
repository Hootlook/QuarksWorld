using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld.Old
{
    public class ReplicatedEntityModuleClient
    {
        [ConfigVar(Name = "replicatedentity.showclientinfo", DefaultValue = "0", Description = "Show replicated system info")]
        public static ConfigVar showInfo;

        public ReplicatedEntityModuleClient()
        {
            this.entityCollection = new ReplicatedEntityCollection();
        }

        public void Shutdown() { }

        public void ProcessEntitySpawn(int servertick, int id, ushort assetId)
        {
            if (showInfo.IntValue > 0)
                GameDebug.Log("ProcessEntitySpawns. Server tick:" + servertick + " id:" + id + " typeid:" + assetId);

            entityCollection.Register(id, NetworkIdentity.spawned[(uint)id].gameObject);
        }

        public void ProcessEntityUpdate(int serverTick, int id, NetworkReader reader)
        {
            if (showInfo.IntValue > 1)
                GameDebug.Log("ApplyEntitySnapshot. ServerTick:" + serverTick + " entityId:" + id);

            entityCollection.ProcessEntityUpdate(serverTick, id, reader);
        }

        public void ProcessEntityDespawns(int serverTime, List<int> despawns)
        {
            if (showInfo.IntValue > 0)
                GameDebug.Log("ProcessEntityDespawns. Server tick:" + serverTime + " ids:" + string.Join(",", despawns));

            foreach (var id in despawns)
            {
                entityCollection.Unregister(id);
            }
        }

        public void Rollback() => entityCollection.Rollback();

        public void Interpolate(GameTime time) => entityCollection.Interpolate(time);

        readonly ReplicatedEntityCollection entityCollection;
    }
}
