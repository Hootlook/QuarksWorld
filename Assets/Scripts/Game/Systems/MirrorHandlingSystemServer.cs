using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    public class MirrorHandlingSystemServer
    {
        public MirrorHandlingSystemServer(GameWorld gameWorld)
        {
            world = gameWorld;
            world.OnSpawn += HandleObjectSettings;
        }

        public void ShutDown()
        {
            world.OnSpawn -= HandleObjectSettings;
        }

        void HandleObjectSettings(GameObject obj)
        {
            foreach (NetworkBehaviour item in obj.GetComponents<NetworkBehaviour>())
            {
                item.syncInterval = world.worldTime.TickInterval;
            }
        }


        GameWorld world;
    }
}
