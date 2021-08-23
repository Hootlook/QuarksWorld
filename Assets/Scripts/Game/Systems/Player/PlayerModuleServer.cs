using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace QuarksWorld
{
    public struct PlayerSpawnRequest : IComponentData
    {
        public int characterType;
        public Vector3 position;
        public Quaternion rotation;
        public Entity playerEntity;

        private PlayerSpawnRequest(int characterType, Vector3 position, Quaternion rotation, Entity playerEntity)
        {
            this.characterType = characterType;
            this.position = position;
            this.rotation = rotation;
            this.playerEntity = playerEntity;
        }

        public static void Create(EntityCommandBuffer commandBuffer, int characterType, Vector3 position, Quaternion rotation, Entity playerEntity)
        {
            var data = new PlayerSpawnRequest(characterType, position, rotation, playerEntity);
            var entity = commandBuffer.CreateEntity();
            commandBuffer.AddComponent(entity, data);
        }
    }

    public struct PlayerRequestDespawn : IComponentData
    {
        public Entity playerEntity;

        public static void Create(EntityCommandBuffer commandBuffer, Entity playerEntity)
        {
            var data = new PlayerRequestDespawn()
            {
                playerEntity = playerEntity,
            };
            var entity = commandBuffer.CreateEntity();
            commandBuffer.AddComponent(entity, data);
        }
    }

    public class PlayerModuleServer
    {
        public PlayerModuleServer(GameWorld world, BundledResourceManager resourceSystem)
        {
            settings = Resources.Load<PlayerModuleSettings>("PlayerModuleSettings");
            resources = resourceSystem;

            gameWorld = world;
        }

        public void Shutdown()
        {
            Resources.UnloadAsset(settings);
        }

        public Player CreatePlayer(int playerId, string playerName)
        {
            var prefab = (GameObject)resources.GetSingleAssetResource(settings.playerStatePrefab);

            var gameObjectEntity = gameWorld.Spawn<GameObjectEntity>(prefab);
            var entityManager = gameObjectEntity.EntityManager;
            var entity = gameObjectEntity.Entity;

            var playerState = entityManager.GetComponentObject<Player>(entity);
            playerState.id = playerId;
            playerState.playerName = playerName;

            // // Mark the playerstate as 'owned' by ourselves so we can reduce amount of
            // // data replicated out from server
            // var re = entityManager.GetComponentData<ReplicatedEntityData>(entity);
            // re.predictingPlayerId = playerId;
            // entityManager.SetComponentData(entity, re);

            return playerState;
        }

        public void CleanupPlayer(Player player)
        {
            gameWorld.RequestDespawn(player.gameObject);
        }

        readonly GameWorld gameWorld;
        readonly BundledResourceManager resources;
        readonly PlayerModuleSettings settings;
    }
}
