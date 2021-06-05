using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    public class PlayerModuleServer
    {
        public PlayerModuleServer(GameWorld gameWorld, BundledResourceManager resourceSystem)
        {
            playerStateRef = resourceSystem.GetResourceRegistry<NetworkedEntityRegistry>().entries[0].prefab;

            this.gameWorld = gameWorld;
            this.resourceSystem = resourceSystem;
        }

        public void Shutdown()
        {
            // Resources.UnloadAsset(prefab);
        }

        public void SpawnPlayer(NetworkConnection conn)
        {
            var prefab = (GameObject)resourceSystem.GetSingleAssetResource(playerStateRef);
            var player = gameWorld.Spawn(prefab);

            player.name = $"{prefab.name} [connId={conn.connectionId}]";
            NetworkServer.AddPlayerForConnection(conn, player);
        }

        public void DespawnPlayer(PlayerState player)
        {
            gameWorld.RequestDespawn(player.gameObject);
        }

        readonly GameWorld gameWorld;
        readonly BundledResourceManager resourceSystem;
        readonly WeakAssetReference playerStateRef;
    }
}
