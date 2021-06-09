using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld.Systems
{
    struct SnapshotMessage
    {
        UInt16 sequence;
        CubeState[] cubes;
    }

    struct CubeState
    {
        Vector3 position;
        Quaternion rotation;
    }

    public class SnapshotInterpolationServerSystem
    {
        public void Shutdown()
        {

        }

        public void Update()
        {
            
        }
    }
}
