using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public class CameraSettings : MonoBehaviour
    {
        public bool isEnabled = true;
        public bool lockToTransform;
        public Vector3 position;
        public Quaternion rotation;
        public float fieldOfView = 60;

        GameEntity entityRef;

        private void Awake()
        {
            entityRef = Contexts.sharedInstance.game.CreateEntity();
            entityRef.AddPlayerCameraSetting(this);
        }

        private void OnDestroy() => entityRef.Destroy();

    }
}
