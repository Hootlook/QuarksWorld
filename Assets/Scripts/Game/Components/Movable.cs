using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

namespace QuarksWorld.Components
{
    [RequireComponent(typeof(Rigidbody))]
    public class Movable : MonoBehaviour
    {
        void Start()
        {
            if (Game.GetGameLoop<ServerGameLoop>() == null)
            {
                GetComponent<Rigidbody>().isKinematic = true;
            }
        }
    }

    public struct MovableData : IReplicates<Movable>
    {
        public Vector3 position;
        public Quaternion rotation;

        public void Serialize(ref SerializeContext context, NetworkWriter writer)
        {
            writer.Write(context.gameObject.transform.position);
            writer.Write(context.gameObject.transform.rotation);
        }

        public void Deserialize(ref SerializeContext context, NetworkReader reader)
        {
            position = reader.ReadVector3();
            rotation = reader.ReadQuaternion();
        }
    }
}
