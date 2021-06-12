using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public class Spectator : MonoBehaviour
    {
        public float moveSpeed = 3;
        public float rotationSpeed = 3;
        float mouseY;
        float mouseX;

        void Update()
        {
            mouseX += Game.Input.GetAxisRaw("Mouse X");
            mouseY -= Game.Input.GetAxisRaw("Mouse Y");

            Vector3 direction = new Vector3(Game.Input.GetAxisRaw("Horizontal"), 0, Game.Input.GetAxisRaw("Vertical"));

            transform.Translate(direction * moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(mouseY, mouseX, 0);
        }
    }
}
