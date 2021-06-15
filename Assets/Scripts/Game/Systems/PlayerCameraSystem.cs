using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld.Systems
{
    public class PlayerCameraSystem
    {
        [ConfigVar(Name = "debug.cameramove", Description = "Show graphs of first person camera rotation", DefaultValue = "0")]
        public static ConfigVar debugCameraMove;
        [ConfigVar(Name = "debug.cameradetach", Description = "Detach player camera from player", DefaultValue = "0")]
        public static ConfigVar debugCameraDetach;

        public PlayerCameraSystem(GameWorld gameWorld)
        {
            world = gameWorld;

            cameraPrefab = Resources.Load<PlayerCamera>("Prefabs/PlayerCamera");

            var camera = world.Spawn<PlayerCamera>(cameraPrefab.gameObject);
            camera.cameraSettings = PlayerState.localPlayerState.GetComponent<PlayerCameraSettings>();
            camera.gameObject.SetActive(false);
            camera.name = cameraPrefab.name;
        }

        public void Update()
        {
            var playerCameras = PlayerCamera.List;
            for (var i = 0; i < playerCameras.Count; i++)
            {
                var camera = playerCameras[i].GetComponent<Camera>();
                var playerCamera = playerCameras[i];
                var settings = playerCamera.cameraSettings;
                var enabled = settings.isEnabled;
                var isActive = camera.gameObject.activeSelf;
                
                if (!enabled)
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
                    // Normal movement
                    camera.transform.position = settings.position;
                    camera.transform.rotation = settings.rotation;
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
                    eu += new Vector3(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0);
                    float invertY = Game.configInvertY.IntValue > 0 ? 1.0f : -1.0f;
                    eu += Time.deltaTime * (new Vector3(-invertY * Input.GetAxisRaw("RightStickY"), Input.GetAxisRaw("RightStickX"), 0));
                    camera.transform.localEulerAngles = eu;
                    detachedMoveSpeed += Input.GetAxisRaw("Mouse ScrollWheel");
                    float verticalMove = (Input.GetKey(KeyCode.R) ? 1.0f : 0.0f) + (Input.GetKey(KeyCode.F) ? -1.0f : 0.0f);
                    verticalMove += Input.GetAxisRaw("Trigger");
                    camera.transform.Translate(new Vector3(Input.GetAxisRaw("Horizontal"), verticalMove, Input.GetAxisRaw("Vertical")) * Time.deltaTime * detachedMoveSpeed);
                }
            }
        }

        PlayerCamera cameraPrefab;
        GameWorld world;

        float detachedMoveSpeed = 4.0f;
    }
}
