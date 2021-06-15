using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld.Systems
{
    public class PlayerModuleClient 
    {
        public PlayerModuleClient(GameWorld gameWorld)
        {
            world = gameWorld;

            cameraSystem = new PlayerCameraSystem(gameWorld);
        }

        public void CameraUpdate()  
        {
            cameraSystem.Update();
        }

        public void Update()
        {
            // // Find player with correct player id
            // var playerStateArray = PlayerState.List;
            // for (var playerIndex = 0; playerIndex < playerStateArray.Count; playerIndex++)
            // {
            //     if (playerStateArray[playerIndex].id == localPlayer.id)
            //     {
            //         localPlayer.playerState = playerStateArray[playerIndex];
            //         GameDebug.Log($"DONE");
            //         break;
            //     }
            // }
        }

        public void Shutdown()
        {
            if (localPlayer != null)
                world.RequestDespawn(localPlayer.gameObject);
        }

        public LocalPlayer RegisterLocalPlayer()
        {
            var prefab = Resources.Load<LocalPlayer>("Prefabs/LocalPlayer");
            localPlayer = world.Spawn<LocalPlayer>(prefab.gameObject);
            localPlayer.command.lookPitch = 90;

            return localPlayer;
        }

        public void SampleInput(bool userInputEnabled, float deltaTime, int renderTick)
        {
            SampleInput(localPlayer, userInputEnabled, deltaTime, renderTick);
        }
        public static void SampleInput(LocalPlayer localPlayer, bool userInputEnabled, float deltaTime, int renderTick)
        {
            if (userInputEnabled)
                UserCommand.AccumulateInput(ref localPlayer.command, deltaTime);

            localPlayer.command.renderTick = renderTick;
        }

        public void ResetInput(bool userInputEnabled)
        {
            // Clear keys and resample to make sure released keys gets detected.
            // Pass in 0 as deltaTime to make mouse input and view stick do nothing
            UserCommand.ClearInput(ref localPlayer.command);

            if (userInputEnabled)
                UserCommand.AccumulateInput(ref localPlayer.command, 0.0f);
        }

        public void StoreCommand(int tick)
        {
            StoreCommand(localPlayer, tick);
        }
        public static void StoreCommand(LocalPlayer localPlayer, int tick)
        {
            if (localPlayer.playerState == null)
                return;

            localPlayer.command.tick = tick;

            var lastBufferTick = localPlayer.commandBuffer.LastTick();
            if (tick != lastBufferTick && tick != lastBufferTick + 1)
            {
                localPlayer.commandBuffer.Clear();
                GameDebug.Log(string.Format("Trying to store tick:{0} but last buffer tick is:{1}. Clearing buffer", tick, lastBufferTick));
            }

            if (tick == lastBufferTick)
                localPlayer.commandBuffer.Set(ref localPlayer.command, tick);
            else
                localPlayer.commandBuffer.Add(ref localPlayer.command, tick);
        }

        public void SendCommand(int tick)
        {
            SendCommand(localPlayer, tick);
        }
        public static void SendCommand(LocalPlayer localPlayer, int tick)
        {
            if (localPlayer.playerState == null)
                return;

            var command = UserCommand.defaultCommand;
            var commandValid = localPlayer.commandBuffer.TryGetValue(tick, ref command);
            if (commandValid)
            {
                Mirror.NetworkClient.Send(command);
            }
        }

        readonly GameWorld world;
        readonly PlayerCameraSystem cameraSystem;

        LocalPlayer localPlayer;
    }
}
