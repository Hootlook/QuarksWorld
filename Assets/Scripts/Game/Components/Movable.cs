using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

namespace QuarksWorld.Components
{
    [RequireComponent(typeof(Rigidbody))]
    public class Movable : NetworkBehaviour
    {
        void Start()
        {
            if (isClient)
            {
                GetComponent<Rigidbody>().isKinematic = true;
            }
        }
    }

    public struct MovableData : IReplicates<Movable>
    {
        public Vector3 position;
        public Quaternion rotation;

        public static IReplicatedSerializerFactory CreateSerializerFactory()
        {
            return new ReplicatedSerializerFactory<MovableData>();
        }

        public void Serialize(ref SerializeContext context, NetworkWriter writer)
        {
            writer.Write(context.gameObject.transform);
            writer.Write(context.gameObject.transform);
        }

        public void Deserialize(ref SerializeContext context, NetworkReader reader)
        {
            position = reader.ReadVector3();
            rotation = reader.ReadQuaternion();
        }
    }
}
