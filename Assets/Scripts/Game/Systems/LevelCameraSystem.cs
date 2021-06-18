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

            cameraSettings = new GameObject("LevelCamera").AddComponent<CameraSettings>();

            Console.AddCommand("debug.levelcam_shots", CmdServercamShots, "Grab a screenshot from each of the levelcam");
        }

        public void Update()
        {
            if (cameraSettings == null || !cameraSettings.isEnabled)
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

            cameraSettings.rotation.eulerAngles = originalOrientation + new Vector3(
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
            nextCameraSpot = 0;
        }

        void NextCamera()
        {
            if (cameraSettings == null || cameraSpots == null || cameraSpots.Length == 0)
                return;

            var spot = cameraSpots[nextCameraSpot];

            cameraSettings.position = spot.transform.position;
            cameraSettings.rotation = spot.transform.rotation;

            originalOrientation = cameraSettings.rotation.eulerAngles;
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

        private CameraSettings cameraSettings;
        private GameObject prefab;

        private Vector3 originalOrientation;
        private CameraSpot[] cameraSpots;
        private int nextCameraSpot;
        private float nextSwitchTime;
    }
}
