using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

namespace QuarksWorld
{
    [DisallowMultipleComponent]
    public class Spectator : MonoBehaviour
    {
        public Vector3 position;
        public Quaternion rotation;

        private void Update()
        {
            // if (hasAuthority)
            // {
            //     transform.position = position;
            //     transform.rotation = rotation;
            // }
        }
    }
}
