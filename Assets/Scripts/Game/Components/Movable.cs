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

        public static List<Movable> List = new List<Movable>();

        public event Action<Movable> OnSpawn; 
        public event Action<Movable> OnDespawn; 

        public override void OnStartServer()
        {
            List.Add(this);
            OnSpawn?.Invoke(this);
        }

        public override void OnStartClient()
        {
            List.Add(this);
            OnSpawn?.Invoke(this);
        }

        public override void OnStopServer()
        {
            List.Remove(this);
            OnDespawn?.Invoke(this);
        }

        public override void OnStopClient()
        {
            List.Remove(this);
            OnDespawn?.Invoke(this);
        }
    }
}
