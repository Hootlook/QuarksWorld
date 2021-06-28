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
}
