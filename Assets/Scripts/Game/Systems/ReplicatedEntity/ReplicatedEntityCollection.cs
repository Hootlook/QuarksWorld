using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace QuarksWorld
{
    public class ReplicatedEntityCollection : IEntityReferenceSerializer
    {
        public struct ReplicatedData
        {
            public GameObject gameObject;
            public IReplicatedSerializer[] serializableArray;
            public IPredictedSerializer[] predictedArray;
            public IInterpolatedSerializer[] interpolatedArray;
        }

        BehaviorSerializers serializers = new BehaviorSerializers();

        List<IReplicatedSerializer> netSerializables = new List<IReplicatedSerializer>(32);
        List<IPredictedSerializer> netPredicted = new List<IPredictedSerializer>(32);
        List<IInterpolatedSerializer> netInterpolated = new List<IInterpolatedSerializer>(32);

        void FindSerializers(GameObject gameObject)
        {
            var typeArray = gameObject.GetComponents<MonoBehaviour>();

            var serializedComponentType = typeof(IReplicatedComponent);
            var predictedComponentType = typeof(IPredictedBase);
            var interpolatedComponentType = typeof(IInterpolatedBase);

            foreach (var componentType in typeArray)
            {
                var managedType = componentType.GetType().BaseType.GenericTypeArguments[0];

                if (!typeof(IComponentBase).IsAssignableFrom(managedType))
                    continue;

                if (serializedComponentType.IsAssignableFrom(managedType))
                {
                    var serializer = serializers.CreateNetSerializer(managedType, gameObject, this);
                    if (serializer != null)
                        netSerializables.Add(serializer);
                }
                else if (predictedComponentType.IsAssignableFrom(managedType))
                {
                    var interfaceTypes = managedType.GetInterfaces();
                    foreach (var it in interfaceTypes)
                    {
                        if (it.IsGenericType)
                        {
                            var serializer = serializers.CreatePredictedSerializer(managedType, gameObject, this);
                            if (serializer != null)
                                netPredicted.Add(serializer);

                            break;
                        }
                    }
                }
                else if (interpolatedComponentType.IsAssignableFrom(managedType))
                {
                    var interfaceTypes = managedType.GetInterfaces();
                    foreach (var it in interfaceTypes)
                    {
                        if (it.IsGenericType)
                        {
                            var serializer = serializers.CreateInterpolatedSerializer(managedType, gameObject, this);
                            if (serializer != null)
                                netInterpolated.Add(serializer);

                            break;
                        }
                    }
                }
            }
        }

        public void Register(int netId, GameObject gameObject)
        {
            // Grow to make sure there is room for entity            
            if (netId >= replicatedData.Count)
            {
                var count = netId - replicatedData.Count + 1;
                var emptyData = new ReplicatedData();
                for (var i = 0; i < count; i++)
                {
                    replicatedData.Add(emptyData);
                }
            }

            GameDebug.Assert(replicatedData[netId].gameObject == null, "ReplicatedData has entity set:{0}", replicatedData[netId].gameObject);

            netSerializables.Clear();
            netPredicted.Clear();
            netInterpolated.Clear();

            FindSerializers(gameObject);

            var data = new ReplicatedData
            {
                gameObject = gameObject,
                serializableArray = netSerializables.ToArray(),
                predictedArray = netPredicted.ToArray(),
                interpolatedArray = netInterpolated.ToArray(),
            };

            replicatedData[netId] = data;
        }

        public GameObject Unregister(int netId)
        {
            var gameObject = replicatedData[netId].gameObject;
            GameDebug.Assert(gameObject != null, "Unregister. ReplicatedData still has a gameObject set");

            replicatedData[netId] = new ReplicatedData();

            return gameObject;
        }

        public void ProcessEntityUpdate(int serverTick, int id, ref NetworkReader reader)
        {
            var data = replicatedData[id];

            GameDebug.Assert(data.serializableArray != null, "Failed to apply snapshot. Serializablearray is null");

            foreach (var entry in data.serializableArray)
                entry.Deserialize(ref reader, serverTick);

            foreach (var entry in data.predictedArray)
                entry.Deserialize(ref reader, serverTick);

            foreach (var entry in data.interpolatedArray)
                entry.Deserialize(ref reader, serverTick);

            replicatedData[id] = data;
        }

        public void GenerateEntitySnapshot(int id, ref NetworkWriter writer)
        {
            var data = replicatedData[id];

            GameDebug.Assert(data.serializableArray != null, "Failed to generate snapshot. Serializablearray is null");

            foreach (var entry in data.serializableArray)
                entry.Serialize(ref writer);

            foreach (var entry in data.predictedArray)
                entry.Serialize(ref writer);

            foreach (var entry in data.interpolatedArray)
                entry.Serialize(ref writer);
        }

        public void Rollback()
        {
            for (int i = 0; i < replicatedData.Count; i++)
            {
                if (replicatedData[i].gameObject == null)
                    continue;

                if (replicatedData[i].predictedArray == null)
                    continue;

                foreach (var predicted in replicatedData[i].predictedArray)
                {
                    predicted.Rollback();
                }
            }
        }

        public void Interpolate(GameTime time)
        {
            for (int i = 0; i < replicatedData.Count; i++)
            {
                if (replicatedData[i].gameObject == null)
                    continue;

                if (replicatedData[i].interpolatedArray == null)
                    continue;

                foreach (var interpolated in replicatedData[i].interpolatedArray)
                {
                    interpolated.Interpolate(time);
                }
            }
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

        List<ReplicatedData> replicatedData = new List<ReplicatedData>(512);
    }
}
