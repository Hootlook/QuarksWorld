using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld.Systems
{
    public class LevelCameraSystem
    {
        public void Shutdown() => Cleanup();
        public void OnBeforeLevelUnload() => Cleanup();

        public LevelCameraSystem()
        {
            cameraSpots = Object.FindObjectsOfType<CameraSpot>();
            
            prefab = (GameObject)Resources.Load("Prefabs/LevelCamera");

            var cameraObj = Object.Instantiate(prefab);
            cameraObj.name = prefab.name;

            camera = cameraObj.GetComponent<Camera>();

            if (camera)
                Game.game.PushCamera(camera);

            Console.AddCommand("debug.servercam_shots", CmdServercamShots, "Grab a screenshot from each of the servercams");
        }

        public void Update()
        {
            if (camera == null || !camera.enabled)
                return;

            var t = Time.realtimeSinceStartup;

            if (nextCapture > -1)
            {
                nextCaptureTick--;
                if (nextCaptureTick == 5)
                    NextCamera();
                if (nextCaptureTick <= 0)
                {
                    Console.EnqueueCommand("screenshot");
                    nextCapture--;
                    nextCaptureTick = 10;
                }
                return;
            }

            camera.transform.localEulerAngles = originalOrientation + new Vector3(
                    Mathf.Sin(t * IdleCycle.x) * IdleLevel.x,
                    Mathf.Sin(t * IdleCycle.y) * IdleLevel.y,
                    Mathf.Sin(t * IdleCycle.z) * IdleLevel.z
                    );

            if (t > nextSwitchTime)
                NextCamera();

            if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
                NextCamera();
        }

        void Cleanup()
        {
            cameraSpots = null;
            camera = null;
            nextCameraSpot = 0;
        }

        void NextCamera()
        {
            if (camera == null || cameraSpots == null || cameraSpots.Length == 0)
                return;

            var spot = cameraSpots[nextCameraSpot];

            camera.transform.position = spot.transform.position;
            camera.transform.rotation = spot.transform.rotation;

            originalOrientation = camera.transform.localEulerAngles;
            nextSwitchTime = Time.realtimeSinceStartup + 5.0f;
            nextCameraSpot = (nextCameraSpot + 1) % cameraSpots.Length;
        }

        void CmdServercamShots(string[] args)
        {
            if (nextCapture > -1)
            {
                Console.Write("Already capturing!");
                return;
            }
            if (cameraSpots == null || cameraSpots.Length == 0)
            {
                Console.Write("No server cam spots!");
                return;
            }
            Console.SetOpen(false);
            nextCapture = cameraSpots.Length - 1;
            nextCaptureTick = 10;
            nextCameraSpot = 0;
        }

        int nextCapture = -1;
        int nextCaptureTick = 0;

        // Genuine QuakeWorld Idle Cam Constants!!!
        private static readonly Vector3 IdleCycle = new Vector3(1.0f, 2.0f, 0.5f);
        private static readonly Vector3 IdleLevel = new Vector3(0.1f, 0.3f, 0.3f);

        private Camera camera;
        private GameObject prefab;

        private Vector3 originalOrientation;
        private CameraSpot[] cameraSpots;
        private int nextCameraSpot;
        private float nextSwitchTime;
    }
}
