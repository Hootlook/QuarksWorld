using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    #region ComponentInterfaces

    public interface IComponentBase { }

    public interface IPredictedBase : IComponentBase { }

    public interface IInterpolatedBase : IComponentBase { }

    public interface IReplicatedComponent : IComponentBase
    {
        // Interface for components that are replicated to all clients

        void Serialize(ref SerializeContext context, NetworkWriter writer);
        void Deserialize(ref SerializeContext context, NetworkReader reader);
    }

    public interface IPredictedComponent<T> : IPredictedBase
    {
        // Interface for components that are replicated only to predicting clients

        void Serialize(ref SerializeContext context, NetworkWriter writer);
        void Deserialize(ref SerializeContext context, NetworkReader reader);
    }

    public interface IInterpolatedComponent<T> : IInterpolatedBase
    {
        // Interface for components that are replicated to all non-predicting clients

        void Serialize(ref SerializeContext context, NetworkWriter writer);
        void Deserialize(ref SerializeContext context, NetworkReader reader);
        void Interpolate(ref SerializeContext context, ref T first, ref T last, float t);
    }

    #endregion

    #region SerializerFactoryInterfaces

    public interface IReplicatedBehaviorFactory
    {
        IReplicatedSerializer CreateSerializer(GameObject gameObject, IEntityReferenceSerializer refSerializer);
    }

    public interface IPredictedBehaviorFactory
    {
        IPredictedSerializer CreateSerializer(GameObject gameObject, IEntityReferenceSerializer refSerializer);
    }

    public interface IInterpolatedBehaviorFactory
    {
        IInterpolatedSerializer CreateSerializer(GameObject gameObject, IEntityReferenceSerializer refSerializer);
    }

    #endregion

    #region SerializerInterfaces

    public interface IReplicatedSerializer
    {
        void Serialize(NetworkWriter writer);
        void Deserialize(NetworkReader reader, int tick);
    }

    public interface IPredictedSerializer
    {
        void Serialize(NetworkWriter writer);
        void Deserialize(NetworkReader reader, int tick);
        void Rollback();
    }

    public interface IInterpolatedSerializer
    {
        void Serialize(NetworkWriter writer);
        void Deserialize(NetworkReader reader, int tick);
        void Interpolate(GameTime time);
    }

    #endregion

    public interface IEntityReferenceSerializer { }

    public struct SerializeContext
    {
        public int tick;
        public GameObject gameObject;
        public IEntityReferenceSerializer refSerializer;
    }

    public class ReplicatedSerializerFactory<T> : IReplicatedBehaviorFactory where T : struct, IReplicatedComponent
    {
        public IReplicatedSerializer CreateSerializer(GameObject gameObject, IEntityReferenceSerializer refSerializer)
        {
            return new ReplicatedSerializer<T>(gameObject, refSerializer);
        }
    }

    public class PredictedSerializerFactory<T> : IPredictedBehaviorFactory where T : struct, IPredictedComponent<T>
    {
        public IPredictedSerializer CreateSerializer(GameObject gameObject, IEntityReferenceSerializer refSerializer)
        {
            return new PredictedSerializer<T>(gameObject, refSerializer);
        }
    }

    public class InterpolatedSerializerFactory<T> : IInterpolatedBehaviorFactory where T : struct, IInterpolatedComponent<T>
    {
        public IInterpolatedSerializer CreateSerializer(GameObject gameObject, IEntityReferenceSerializer refSerializer)
        {
            return new InterpolatedSerializer<T>(gameObject, refSerializer);
        }
    }

    public class ReplicatedSerializer<TData> : IReplicatedSerializer where TData : struct, IReplicatedComponent
    {
        protected SerializeContext context;

        public TData state;

        public ReplicatedSerializer(GameObject gameObject, IEntityReferenceSerializer refSerializer)
        {
            context.gameObject = gameObject;
            context.refSerializer = refSerializer;
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

    public class PredictedSerializer<TData> : IPredictedSerializer where TData : struct, IPredictedComponent<TData>
    {
        SerializeContext context;

        public TData state;
        public TData lastServerState;

        public PredictedSerializer(GameObject gameObject, IEntityReferenceSerializer refSerializer)
        {
            context.gameObject = gameObject;
            context.refSerializer = refSerializer;
        }

        public void Serialize(NetworkWriter writer)
        {
            state.Serialize(ref context, writer);
        }

        public void Deserialize(NetworkReader reader, int tick)
        {
            context.tick = tick;
            lastServerState.Deserialize(ref context, reader);
        }

        public void Rollback()
        {
            state = lastServerState;
        }
    }

    public class InterpolatedSerializer<TData> : IInterpolatedSerializer where TData : struct, IInterpolatedComponent<TData>
    {
        SerializeContext context;

        public TData state;

        StateBuffer<TData> stateHistory = new StateBuffer<TData>(32);

        public InterpolatedSerializer(GameObject gameObject, IEntityReferenceSerializer refSerializer)
        {
            context.gameObject = gameObject;
            context.refSerializer = refSerializer;
        }

        public void Serialize(NetworkWriter writer)
        {
            state.Serialize(ref context, writer);
        }

        public void Deserialize(NetworkReader reader, int tick)
        {
            context.tick = tick;
            var state = new TData();
            state.Deserialize(ref context, reader);
            stateHistory.Add(tick, state);
        }

        public void Interpolate(GameTime interpTime)
        {
            TData state = new TData();

            if (stateHistory.Count > 0)
            {
                int lowIndex = 0, highIndex = 0;
                float interpVal = 0;
                var interpValid = stateHistory.GetStates(interpTime.tick, interpTime.TickDurationAsFraction, ref lowIndex, ref highIndex, ref interpVal);

                if (interpValid)
                {
                    var prevState = stateHistory[lowIndex];
                    var nextState = stateHistory[highIndex];
                    state.Interpolate(ref context, ref prevState, ref nextState, interpVal);
                }
                else
                {
                    state = stateHistory.Last();
                }
            }

            this.state = state;
        }
    }

    public abstract class ReplicatedBehavior<TData> : MonoBehaviour where TData : struct, IReplicatedComponent
    {
        public TData state;
    }

    public abstract class PredictedBehavior<TData> : MonoBehaviour where TData : struct, IPredictedComponent<TData>
    {
        public TData state;
    }

    public abstract class InterpolatedBehavior<TData> : MonoBehaviour where TData : struct, IInterpolatedComponent<TData>
    {
        public TData state;
    }

    public class BehaviorSerializers
    {
        Dictionary<Type, IReplicatedBehaviorFactory> netSerializerFactories = new Dictionary<Type, IReplicatedBehaviorFactory>();

        Dictionary<Type, IPredictedBehaviorFactory> predictedSerializerFactories = new Dictionary<Type, IPredictedBehaviorFactory>();

        Dictionary<Type, IInterpolatedBehaviorFactory> interpolatedSerializerFactories = new Dictionary<Type, IInterpolatedBehaviorFactory>();

        public BehaviorSerializers() => CreateSerializerFactories();

        public IReplicatedSerializer CreateNetSerializer(Type type, GameObject gameObject, IEntityReferenceSerializer refSerializer)
        {
            if (netSerializerFactories.TryGetValue(type, out IReplicatedBehaviorFactory factory))
            {
                return factory.CreateSerializer(gameObject, refSerializer);
            }
            GameDebug.LogError("Failed to find INetSerializer for type:" + type.Name);
            return null;
        }

        public IPredictedSerializer CreatePredictedSerializer(Type type, GameObject gameObject, IEntityReferenceSerializer refSerializer)
        {
            if (predictedSerializerFactories.TryGetValue(type, out IPredictedBehaviorFactory factory))
            {
                return factory.CreateSerializer(gameObject, refSerializer);
            }
            GameDebug.LogError("Failed to find IPredictedSerializer for type:" + type.Name);
            return null;
        }

        public IInterpolatedSerializer CreateInterpolatedSerializer(Type type, GameObject gameObject, IEntityReferenceSerializer refSerializer)
        {
            if (interpolatedSerializerFactories.TryGetValue(type, out IInterpolatedBehaviorFactory factory))
            {
                return factory.CreateSerializer(gameObject, refSerializer);
            }
            GameDebug.LogError("Failed to find IInterpolatedSerializer for type:" + type.Name);
            return null;
        }

        void CreateSerializerFactories()
        {
            var replicatedType = typeof(IReplicatedComponent);
            var predictedType = typeof(IPredictedBase);
            var interpolatedType = typeof(IInterpolatedBase);

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

                        if (method.ReturnType != typeof(IReplicatedBehaviorFactory))
                        {
                            GameDebug.LogError("Replicated component " + type + " CreateSerializerFactory does not have return type IReplicatedBehaviorFactory");
                            continue;
                        }

                        var result = method.Invoke(null, new object[] { });
                        netSerializerFactories.Add(type, (IReplicatedBehaviorFactory)result);
                    }

                    if (predictedType.IsAssignableFrom(type))
                    {
                        var method = type.GetMethod("CreateSerializerFactory");
                        if (method == null)
                        {
                            GameDebug.LogError("Predicted component " + type + " has no CreateSerializerFactory");
                            continue;
                        }

                        if (method.ReturnType != typeof(IPredictedBehaviorFactory))
                        {
                            GameDebug.LogError("Replicated component " + type + " CreateSerializerFactory does not have return type IPredictedBehaviorFactory");
                            continue;
                        }

                        var result = method.Invoke(null, new object[] { });
                        predictedSerializerFactories.Add(type, (IPredictedBehaviorFactory)result);
                    }

                    if (interpolatedType.IsAssignableFrom(type))
                    {
                        var method = type.GetMethod("CreateSerializerFactory");
                        if (method == null)
                        {
                            GameDebug.LogError("Interpolated component " + type + " has no CreateSerializerFactory");
                            continue;
                        }

                        if (method.ReturnType != typeof(IInterpolatedBehaviorFactory))
                        {
                            GameDebug.LogError("Replicated component " + type + " CreateSerializerFactory does not have return type IInterpolatedBehaviorFactory");
                            continue;
                        }

                        var result = method.Invoke(null, new object[] { });
                        interpolatedSerializerFactories.Add(type, (IInterpolatedBehaviorFactory)result);
                    }
                }
            }
        }
    }
}
