using UnityEngine;
using Mirror;
using System;

namespace QuarksWorld
{
    public enum Button : uint
    {
        None = 0,
        Forward = 1 << 0,
        Backward = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
        Jump = 1 << 4,
        PrimaryFire = 1 << 5,
        SecondaryFire = 1 << 6,
        Reload = 1 << 7,
        Use = 1 << 8,
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
        public float lookYaw;
        public float lookPitch;

        public float moveYaw => (buttons.IsSet(Button.Right) ? 1 : 0) + (buttons.IsSet(Button.Left) ? 1 : 0);
        public float movePitch => (buttons.IsSet(Button.Forward) ? 1 : 0) + (buttons.IsSet(Button.Backward) ? 1 : 0);
        public Vector3 lookDir => Quaternion.Euler(new Vector3(-lookPitch, lookYaw, 0)) * Vector3.down;
        public Quaternion lookRotation => Quaternion.Euler(new Vector3(90 - lookPitch, lookYaw, 0));
        public ButtonBitField buttons;

        public static readonly UserCommand defaultCommand = new UserCommand(0);

        private UserCommand(int i)
        {
            tick = 0;
            renderTick = 0;
            lookYaw = 0;
            lookPitch = 90;
            buttons.flags = 0;
        }

        public void ClearCommand()
        {
            buttons.flags = 0;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void AccumulateInput(ref UserCommand command, float deltaTime)
        {
            Vector2 deltaMousePos = Vector2.zero;

            if (deltaTime > 0.0f)
                deltaMousePos += new Vector2(Game.Input.GetAxisRaw("Mouse X"), Game.Input.GetAxisRaw("Mouse Y"));

            command.lookYaw += deltaMousePos.x * Game.configMouseSensitivity.FloatValue;
            command.lookYaw = command.lookYaw % 360;
            while (command.lookYaw < 0.0f) command.lookYaw += 360.0f;

            command.lookPitch += deltaMousePos.y * Game.configMouseSensitivity.FloatValue;
            command.lookPitch = Mathf.Clamp(command.lookPitch, 0, 180);

            command.buttons.Or(Button.Forward, Game.Input.GetKeyDown(KeyCode.Z));
            command.buttons.Or(Button.Backward, Game.Input.GetKeyDown(KeyCode.S));
            command.buttons.Or(Button.Left, Game.Input.GetKeyDown(KeyCode.Q));
            command.buttons.Or(Button.Right, Game.Input.GetKeyDown(KeyCode.D));
            command.buttons.Or(Button.Jump, Game.Input.GetKeyDown(KeyCode.Space));
            command.buttons.Or(Button.PrimaryFire, Game.Input.GetMouseButton(0) && Game.GetMousePointerLock());
            command.buttons.Or(Button.SecondaryFire, Game.Input.GetMouseButton(1));
            command.buttons.Or(Button.Reload, Game.Input.GetKey(KeyCode.R));
            command.buttons.Or(Button.Use, Game.Input.GetKey(KeyCode.E));
        }

        public static void ClearInput(ref UserCommand command)
        {
            command.ClearCommand();
        }
    }
}
