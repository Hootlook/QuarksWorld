using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    [DisallowMultipleComponent]
    public class PlayerCamera : MonoBehaviour
    {
        public PlayerCameraSettings cameraSettings;

        public static List<PlayerCamera> List = new List<PlayerCamera>();

        void Awake() => List.Add(this);
        
        void OnDestroy() => List.Remove(this);
    }
}
