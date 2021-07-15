using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace QuarksWorld
{
    public class ReplicatedEntityCollection
    {
        public List<ReplicatedData> replicatedData = new List<ReplicatedData>();
        
        public struct ReplicatedData
        {
            public GameObject gameObject;
            public IReplicatedSerializer[] serializableArray;
        }

        List<IReplicatedSerializer> cacheSerializables = new List<IReplicatedSerializer>(32);

        BehaviourSerializer serializers = new BehaviourSerializer();

        void FindSerializers(GameObject gameObject)
        {
            var behaviourArray = gameObject.GetComponents<MonoBehaviour>();

            foreach (var componentType in behaviourArray)
            {
                var managedType = componentType.GetType();

                var serializer = serializers.CreateNetSerializer(managedType, gameObject);
                if (serializer != null) 
                {
                    cacheSerializables.Add(serializer);
                }
            }
        }

        public void Register(int id, GameObject gameObject)
        {
            // Grow to make sure there is room for entity            
            if (id >= replicatedData.Count)
            {
                var count = id - replicatedData.Count + 1;
                var emptyData = new ReplicatedData();
                for (var i = 0; i < count; i++)
                {
                    replicatedData.Add(emptyData);
                }
            }

            cacheSerializables.Clear();

            FindSerializers(gameObject);

            var data = new ReplicatedData
            {
                gameObject = gameObject,
                serializableArray = cacheSerializables.ToArray(),
            };

            replicatedData[id] = data;
        }

        public GameObject Unregister(int id)
        {
            var gameObject = replicatedData[id].gameObject;
            GameDebug.Assert(gameObject != null, "Unregister. ReplicatedData still has a gameObject set");

            replicatedData[id] = new ReplicatedData();

            return gameObject;
        }

        public void ProcessEntityUpdate(int serverTick, int id, NetworkReader reader)
        {
            var data = replicatedData[id];

            GameDebug.Assert(data.serializableArray != null, "Failed to apply snapshot. Serializablearray is null");

            foreach (var entry in data.serializableArray)
                entry.Deserialize(reader, serverTick);

            replicatedData[id] = data;
        }

        public void GenerateEntitySnapshot(int id, NetworkWriter writer)
        {
            var data = replicatedData[id];

            GameDebug.Assert(data.serializableArray != null, "Failed to generate snapshot. Serializablearray is null");

            foreach (var entry in data.serializableArray)
                entry.Serialize(writer);
        }

        public string GenerateName(int entityId)
        {
            var data = replicatedData[entityId];

            bool first = true;
            string name = "";
            foreach (var entry in data.serializableArray)
            {
                if (!first)
                    name += "_";
                if (entry is Component)
                    name += (entry as Component).GetType();
                else
                    name += entry.GetType().ToString();
                first = false;
            }
            return name;
        }
    }
}
