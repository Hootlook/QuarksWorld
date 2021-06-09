using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public class PlayerModuleClient 
    {
        public PlayerModuleClient(GameWorld world)
        {
            this.world = world;
        }

        public void Shutdown()
        {
            if (localPlayer != null)
                world.RequestDespawn(localPlayer.gameObject);
        }

        public LocalPlayer RegisterLocalPlayer(int playerId)
        {
            var prefab = Resources.Load<LocalPlayer>("Prefabs/LocalPlayer");
            localPlayer = world.Spawn<LocalPlayer>(prefab.gameObject);
            localPlayer.playerId = playerId;
            localPlayer.command.lookPitch = 90;

            return localPlayer;
        }

        readonly GameWorld world;
        LocalPlayer localPlayer;
    }
}
