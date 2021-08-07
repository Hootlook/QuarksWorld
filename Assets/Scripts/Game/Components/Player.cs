using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public class Player : MonoBehaviour
    {
        float eyeHeight;
        Transform weapons;
        IMovement movement;

        void OnPickupItem(Item item)
        {
                
        }


        void SetupCollider()
        {          
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1, eyeHeight, 1);
            collider.center = Vector3.up * (eyeHeight / 2);
        }

        void SetupCamera()
        {
            var camera = gameObject.AddComponent<CameraSettings>();
            camera.lockToTransform = true;
            camera.position = transform.up * eyeHeight;
        }

        void SetupWeapons()
        {
            var weaponsRoot = new GameObject("Weapons");
            weaponsRoot.transform.SetParent(transform);
            weapons = weaponsRoot.transform;
        }

    }
}
