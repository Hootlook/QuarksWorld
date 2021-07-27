using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public class Player : MonoBehaviour
    {
        float eyeHeight;

        void SetupCollider()
        {
            if (TryGetComponent<Collider>(out Collider oldCollider))
                Destroy(oldCollider);
            
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(1, eyeHeight, 1);
            collider.center = Vector3.up * (eyeHeight / 2);
        }

        void SetupCamera()
        {
            if (GetComponent<CameraSettings>() != null)
                return;

            var camera = gameObject.AddComponent<CameraSettings>();
            camera.lockToTransform = true;
            camera.position = transform.up * eyeHeight;
        }


    }
}
