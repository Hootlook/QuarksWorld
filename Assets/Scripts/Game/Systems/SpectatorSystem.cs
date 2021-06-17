using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld.Systems
{
    public class SpectatorSystem
    {
        GameObject controlledEntity;
        Spectator spectator;

        public SpectatorSystem(GameWorld world)
        {
            gameWorld = world;
        }

        public void Update()
        {
            var localPlayer = PlayerState.localPlayer;

            if (!localPlayer || localPlayer.controlledEntity == null)
                return;

            if (localPlayer.controlledEntity != controlledEntity)
            {
                controlledEntity = localPlayer.controlledEntity;
                
                if (!localPlayer.controlledEntity.TryGetComponent(out Spectator newSpectator)) 
                    return;

                spectator = newSpectator;
            }

            var command = localPlayer.command;

            spectator.rotation = Quaternion.Euler(new Vector3(90 - command.lookPitch, command.lookYaw, 0));

            var forward = spectator.rotation * Vector3.forward;
            var right = spectator.rotation * Vector3.right;
            var maxVel = 3 * gameWorld.worldTime.TickInterval;
            var moveDir = forward * Mathf.Cos(command.moveYaw * Mathf.Deg2Rad) + right * Mathf.Sin(command.moveYaw * Mathf.Deg2Rad);
            spectator.position += moveDir * maxVel * command.moveMagnitude;

            // var playerCameras = PlayerCamera.List;
            // for (int i = 0; i < playerCameras.Count; i++)
            // {
            //     var cameraSettings = playerCameras[i].cameraSettings;
            //     cameraSettings.isEnabled = true;
            //     cameraSettings.position = spectator.position;
            //     cameraSettings.rotation = spectator.rotation;
            // }
        }

        readonly GameWorld gameWorld;
    }
}
