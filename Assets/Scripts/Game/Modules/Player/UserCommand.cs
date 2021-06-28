using UnityEngine;
using Mirror;
using System;

namespace QuarksWorld
{
    public enum Button : uint
    {
        None = 0,
        Jump = 1 << 0,
        Boost = 1 << 1,
        PrimaryFire = 1 << 2,
        SecondaryFire = 1 << 3,
        Reload = 1 << 4,
        Melee = 1 << 5,
        Use = 1 << 6,
        Ability1 = 1 << 7,
        Ability2 = 1 << 8,
        Ability3 = 1 << 9,
    }

    [Serializable]
    public struct UserCommand : NetworkMessage
    {
        public struct ButtonBitField
        {
            public uint flags;

            public bool IsSet(Button button)
            {
                return (flags & (uint)button) > 0;
            }

            public void Or(Button button, bool val)
            {
                if (val)
                    flags = flags | (uint)button;
            }

            public void Set(Button button, bool val)
            {
                if (val)
                    flags = flags | (uint)button;
                else
                {
                    flags = flags & ~(uint)button;
                }
            }
        }

        public int tick;
        public int renderTick;
        public float moveYaw;
        public float movePitch;
        public float moveMagnitude;
        public float lookYaw;
        public float lookPitch;
        
        public Vector3 lookDir => Quaternion.Euler(new Vector3(-lookPitch, lookYaw, 0)) * Vector3.down;
        public Quaternion lookRotation => Quaternion.Euler(new Vector3(90 - lookPitch, lookYaw, 0));
        public ButtonBitField buttons;

        public static readonly UserCommand defaultCommand = new UserCommand(0);

        private UserCommand(int i)
        {
            tick = 0;
            renderTick = 0;
            moveYaw = 0;
            movePitch = 0;
            moveMagnitude = 0;
            lookYaw = 0;
            lookPitch = 90;
            buttons.flags = 0;
        }

        public void ClearCommand()
        {
            buttons.flags = 0;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        static float maxMoveYaw;
        static float maxMoveMagnitude;

        public static void AccumulateInput(ref UserCommand command, float deltaTime)
        {
            // To accumulate move we store the input with max magnitude and uses that
            Vector2 moveInput = new Vector2(Game.Input.GetAxisRaw("Horizontal"), Game.Input.GetAxisRaw("Vertical"));

            // float angle = Vector2.Angle(Vector2.up, moveInput);

            // if (moveInput.x < 0)
            //     angle = 360 - angle;

            // float magnitude = Mathf.Clamp(moveInput.magnitude, 0, 1);
            // if (magnitude > maxMoveMagnitude)
            // {
            //     maxMoveYaw = angle;
            //     maxMoveMagnitude = magnitude;
            // }

            // command.moveYaw = maxMoveYaw;
            // command.moveMagnitude = maxMoveMagnitude;

            command.moveYaw = moveInput.x;
            command.movePitch = moveInput.y;
            command.moveMagnitude = maxMoveMagnitude;

            Vector2 deltaMousePos = new Vector2(0, 0);

            if (deltaTime > 0.0f)
                deltaMousePos += new Vector2(Game.Input.GetAxisRaw("Mouse X"), Game.Input.GetAxisRaw("Mouse Y"));

            command.lookYaw += deltaMousePos.x * Game.configMouseSensitivity.FloatValue;
            command.lookYaw = command.lookYaw % 360;
            while (command.lookYaw < 0.0f) command.lookYaw += 360.0f;

            command.lookPitch += deltaMousePos.y * Game.configMouseSensitivity.FloatValue;
            command.lookPitch = Mathf.Clamp(command.lookPitch, 0, 180);

            command.buttons.Or(Button.Jump,          Game.Input.GetKeyDown(KeyCode.Space));
            command.buttons.Or(Button.Boost,         Game.Input.GetKey(KeyCode.LeftControl));
            command.buttons.Or(Button.PrimaryFire,   Game.Input.GetMouseButton(0) && Game.GetMousePointerLock());
            command.buttons.Or(Button.SecondaryFire, Game.Input.GetMouseButton(1));
            command.buttons.Or(Button.Ability1,      Game.Input.GetKey(KeyCode.LeftShift));
            command.buttons.Or(Button.Ability2,      Game.Input.GetKey(KeyCode.E));
            command.buttons.Or(Button.Ability3,      Game.Input.GetKey(KeyCode.Q));
            command.buttons.Or(Button.Reload,        Game.Input.GetKey(KeyCode.R));
            command.buttons.Or(Button.Melee,         Game.Input.GetKey(KeyCode.V));
            command.buttons.Or(Button.Use,           Game.Input.GetKey(KeyCode.E));
        }

        public static void ClearInput(ref UserCommand command)
        {
            maxMoveMagnitude = 0;
            command.ClearCommand();
        }
    }
}
