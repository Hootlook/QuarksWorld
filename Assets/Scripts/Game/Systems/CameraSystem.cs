using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Entitas;

namespace QuarksWorld.Systems
{
    public class CameraSystem : IExecuteSystem
    {
        [ConfigVar(Name = "debug.cameramove", Description = "Show graphs of first person camera rotation", DefaultValue = "0")]
        public static ConfigVar debugCameraMove;
        [ConfigVar(Name = "debug.cameradetach", Description = "Detach player camera from player", DefaultValue = "0", Flags = Flags.Cheat)]
        public static ConfigVar debugCameraDetach;

        IGroup<GameEntity> group;

        Camera camera;

        public CameraSystem(GameWorld gameWorld)
        {
            world = gameWorld;

            cameraPrefab = (GameObject)Resources.Load("Prefabs/MainCamera");

            camera = world.Spawn<Camera>(cameraPrefab);
            camera.gameObject.SetActive(false);
            camera.name = cameraPrefab.name;

            group = Contexts.sharedInstance.game.GetGroup(GameMatcher.PlayerCameraSetting);
        }

        public void Execute()
        {
            var cameraSettings = group.GetEntities();
            for (var i = 0; i < cameraSettings.Length; i++)
            {
                var settings = cameraSettings[i].playerCameraSetting.refSetting;
                var isEnabled = settings.isEnabled;
                var isActive = camera.gameObject.activeSelf;
                
                if (!isEnabled)
                {
                    if (isActive)
                    {
                        Game.game.PopCamera(camera);
                        camera.gameObject.SetActive(false);
                    }
                    continue;
                }

                if (!isActive)
                {
                    camera.gameObject.SetActive(true);
                    Game.game.PushCamera(camera);
                }

                camera.fieldOfView = settings.fieldOfView;
                if (debugCameraDetach.IntValue == 0)
                {
                    if (settings.lockToTransform)
                    {
                        // Act as if its parented
                        camera.transform.position = settings.transform.position;
                        camera.transform.rotation = settings.rotation;
                    }
                    else
                    {
                        // Normal movement
                        camera.transform.position = settings.position;
                        camera.transform.rotation = settings.rotation;
                    }
                }
                else if (debugCameraDetach.IntValue == 1)
                {
                    // Move char but still camera
                }

                if (debugCameraDetach.ChangeCheck())
                {
                    // Block normal input
                    Game.Input.SetBlock(Game.Input.Blocker.Debug, debugCameraDetach.IntValue == 2);
                }
                if (debugCameraDetach.IntValue == 2 && !Console.IsOpen())
                {
                    var eu = camera.transform.localEulerAngles;
                    if (eu.x > 180.0f) eu.x -= 360.0f;
                    eu.x = Mathf.Clamp(eu.x, -70.0f, 70.0f);
                    float invertY = Game.configInvertY.IntValue > 0 ? 1.0f : -1.0f;
                    eu += new Vector3(-invertY * Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0);
                    camera.transform.localEulerAngles = eu;
                    detachedMoveSpeed += Input.GetAxisRaw("Mouse ScrollWheel");
                    float verticalMove = (Input.GetKey(KeyCode.R) ? 1.0f : 0.0f) + (Input.GetKey(KeyCode.F) ? -1.0f : 0.0f);
                    camera.transform.Translate(new Vector3(Input.GetAxisRaw("Horizontal"), verticalMove, Input.GetAxisRaw("Vertical")) * Time.deltaTime * detachedMoveSpeed);
                }
            }
        }

        GameObject cameraPrefab;
        GameWorld world;

        float detachedMoveSpeed = 4.0f;
    }
}
