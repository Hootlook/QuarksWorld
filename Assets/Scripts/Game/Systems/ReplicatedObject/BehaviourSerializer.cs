using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace QuarksWorld.Experimental
{
    public struct SerializeContext
    {
        public int tick;
        public GameObject gameObject;
    }
    
    public interface IReplicates<T> : IReplicatedComponent where T : MonoBehaviour { }

    public interface IReplicatedComponent
    {
        void Serialize(ref SerializeContext context, NetworkWriter writer);
        void Deserialize(ref SerializeContext context, NetworkReader reader);
    }

    public interface IReplicatedSerializerFactory
    {
        IReplicatedSerializer CreateSerializer(GameObject gameObject);
    }

    public interface IReplicatedSerializer
    {
        void Serialize(NetworkWriter writer);
        void Deserialize(NetworkReader reader, int tick);
    }

    public class ReplicatedSerializerFactory<TData> : IReplicatedSerializerFactory where TData : struct, IReplicatedComponent
    {
        public IReplicatedSerializer CreateSerializer(GameObject gameObject) => new ReplicatedSerializer<TData>(gameObject);
    }

    public class ReplicatedSerializer<TData> : IReplicatedSerializer where TData : struct, IReplicatedComponent
    {
        SerializeContext context;

        TData state;

        public ReplicatedSerializer(GameObject gameObject)
        {
            context.gameObject = gameObject;
        }

        public void Serialize(NetworkWriter writer)
        {
            state.Serialize(ref context, writer);
        }

        public void Deserialize(NetworkReader reader, int tick)
        {
            context.tick = tick;
            state.Deserialize(ref context, reader);
        }
    }
    
    public class BehaviourSerializer
    {
        Dictionary<Type, IReplicatedSerializerFactory> netSerializerFactories = new Dictionary<Type, IReplicatedSerializerFactory>();

        public BehaviourSerializer() => CreateSerializerFactories();

        public IReplicatedSerializer CreateNetSerializer(Type type, GameObject gameObject)
        {
            if (netSerializerFactories.TryGetValue(type, out IReplicatedSerializerFactory factory))
            {
                return factory.CreateSerializer(gameObject);
            }
            GameDebug.LogError("Failed to find INetSerializer for type:" + type.Name);
            return null;
        }

        void CreateSerializerFactories()
        {
            var replicatedType = typeof(IReplicatedComponent);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsValueType)
                        continue;

                    if (replicatedType.IsAssignableFrom(type))
                    {
                        var method = type.GetMethod("CreateSerializerFactory");
                        if (method == null)
                        {
                            GameDebug.LogError("Replicated component " + type + " has no CreateSerializerFactory");
                            continue;
                        }

                        if (method.ReturnType != typeof(IReplicatedSerializerFactory))
                        {
                            GameDebug.LogError("Replicated component " + type + " CreateSerializerFactory does not have return type IReplicatedBehaviorFactory");
                            continue;
                        }

                        var behaviour = type.GetInterfaces().Last().GenericTypeArguments.First();

                        var result = method.Invoke(null, new object[] { });
                        netSerializerFactories.Add(behaviour, (IReplicatedSerializerFactory)result);
                    }
                }
            }
        }
    }
}
