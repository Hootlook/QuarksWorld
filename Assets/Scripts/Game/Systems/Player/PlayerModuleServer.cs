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
            playerStateRef = resourceSystem.GetResourceRegistry<ReplicatedEntityRegistry>().entries[0].prefab;

            world = gameWorld;
            resources = resourceSystem;
        }

        public void Shutdown() { }

        public void SpawnPlayer(NetworkConnection conn)
        {
            var prefab = (GameObject)resources.GetSingleAssetResource(playerStateRef);
            var player = world.Spawn<PlayerState>(prefab);

            player.name = $"{prefab.name} [connId={conn.connectionId}]";
            player.id = conn.connectionId;
            NetworkServer.AddPlayerForConnection(conn, player.gameObject);
        }

        public void DespawnPlayer(PlayerState player)
        {
            world.RequestDespawn(player.gameObject); 
        }

        readonly GameWorld world;
        readonly BundledResourceManager resources;
        readonly WeakAssetReference playerStateRef;
    }
}
