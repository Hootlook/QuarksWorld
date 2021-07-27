using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// Make game object as single player style
// Use replication and systems to control the objects as the current game loop wants
// Allong with this use a global input system. it will prevent things like weapon behaviours to
// Be triggered as inputs are sent to server in a clientLoop context (maybe not because of prediction, find solution)
// As added security replicated variables will be overrided by the replication.

namespace QuarksWorld
{
    public class Controller : NetworkBehaviour
    {
        // [SyncVar(hook = nameof(OnReceivedPlayerState))]
        // PlayerState latestServerState;

        // const uint BUFFER_SIZE = 1024;
        // PlayerState[] serverStateHistory = new PlayerState[BUFFER_SIZE];
        // InputState[] localPredictedInputs = new InputState[BUFFER_SIZE];
        // InputState[] clientInputBuffer = new InputState[BUFFER_SIZE];

        // uint frameId;
        // float timer;
        // Rigidbody rb;

        // public struct PlayerState
        // {
        //     public uint frameId;
        //     public Vector3 position;
        //     public Vector3 velocity;
        // }

        // public struct InputState
        // {
        //     public uint frameId;
        //     public float horizontal;
        //     public float vertical;
        // }

        // void Start() => rb = GetComponent<Rigidbody>();

        // void Update()
        // {
        //     timer += Time.deltaTime;
        //     while (timer >= 1.0f / Game.serverTickRate.FloatValue)
        //     {
        //         timer -= 1.0f / Game.serverTickRate.FloatValue;

        //         if (isServer)
        //         {
        //             var index = frameId % BUFFER_SIZE;
        //             var nextClientInput = clientInputBuffer[index];

        //             if (nextClientInput.frameId == frameId)
        //             {
        //                 Move(nextClientInput);
        //             }

        //             latestServerState = new PlayerState
        //             {
        //                 frameId = frameId,
        //                 position = rb.position,
        //                 velocity = rb.velocity
        //             };
        //         }
                
        //         if (isLocalPlayer)
        //         {
        //             var newInputState = new InputState
        //             {
        //                 frameId = frameId,
        //                 horizontal = Input.GetAxisRaw("Horizontal"),
        //                 vertical = Input.GetAxisRaw("Vertical")
        //             };

        //             var index = frameId % BUFFER_SIZE;
        //             localPredictedInputs[index] = newInputState;
                    
        //             Move(newInputState);

        //             CmdSendInputs(newInputState);
        //         }

        //         frameId++;
        //     }
        // }

        // void OnReceivedPlayerState(PlayerState oldValue, PlayerState newValue)
        // {
        //     var index = frameId % BUFFER_SIZE; 
        //     var serverState = serverStateHistory[index];

        //     Vector3 positionError = rb.position - serverState.position;

        //     if (positionError.magnitude > 0)
        //     {
        //         rb.position = serverState.position;
        //         rb.velocity = serverState.velocity;

        //         Physics.SyncTransforms();

        //         uint rewindTickNumber = serverState.frameId;
        //         while (rewindTickNumber++ < frameId)
        //         {
        //             Move(localPredictedInputs[rewindTickNumber % BUFFER_SIZE]);

        //             Physics.Simulate(1.0f / Game.serverTickRate.FloatValue);
        //         }
        //     }
        // }

        // [Command(channel = Channels.Unreliable)]
        // void CmdSendInputs(InputState clientState)
        // {
        //     if (clientState.frameId > frameId)
        //     {
        //         var index = clientState.frameId % BUFFER_SIZE;
        //         clientInputBuffer[index] = clientState;
        //     }
        // }

        // void Move(InputState input)
        // {
        //     rb.AddForce(new Vector3(input.horizontal, 0, input.vertical), ForceMode.Impulse);
        //     rb.velocity = Vector3.ClampMagnitude(rb.velocity, 1);
        // }




        // public enum Button : uint
        // {
        //     None = 0,
        //     Forward = 1 << 0,
        //     Backward = 1 << 1,
        //     Left = 1 << 2,
        //     Right = 1 << 3,
        //     Jump = 1 << 4,
        //     PrimaryFire = 1 << 5,
        //     SecondaryFire = 1 << 6,
        //     Reload = 1 << 7,
        //     Use = 1 << 8,
        // }

        // public struct InputState
        // {
        //     public int frameId;

        //     public ButtonBitField[] buttons;

        //     public struct ButtonBitField
        //     {
        //         public uint flags;

        //         public bool IsSet(Button button)
        //         {
        //             return (flags & (uint)button) > 0;
        //         }

        //         public void Or(Button button, bool val)
        //         {
        //             if (val)
        //                 flags = flags | (uint)button;
        //         }

        //         public void Set(Button button, bool val)
        //         {
        //             if (val)
        //                 flags = flags | (uint)button;
        //             else
        //             {
        //                 flags = flags & ~(uint)button;
        //             }
        //         }
        //     }
        // }

        // public struct ServerState
        // {
        //     public uint frameId;
        //     public Vector3 position;
        //     public Vector3 velocity;
        // }













        const uint BUFFER_SIZE = 1024;
        ServerState[] predictedPlayer = new ServerState[BUFFER_SIZE];
        ClientState[] predictedInputs = new ClientState[BUFFER_SIZE];

        uint frameId;
        float timer;
        Rigidbody rb;

        public struct ServerState
        {
            public uint frameId;
            public Vector3 position;
            public Vector3 velocity;
        }

        public struct ClientState
        {
            public uint frameId;
            public float horizontal;
            public float vertical;
        }

        void Awake() => rb = GetComponent<Rigidbody>();

        void Update()
        {
            if (Input.GetKey(KeyCode.Space))
                rb.AddForce(Vector3.up * 5, ForceMode.Impulse);

            timer += Time.deltaTime;
            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;

                if (hasAuthority)
                {
                    if (!NetworkClient.ready)
                        return;

                    ClientState inputs = new ClientState
                    {
                        frameId = frameId++,
                        horizontal = Input.GetAxisRaw("Horizontal"),
                        vertical = Input.GetAxisRaw("Vertical"),
                    };

                    uint bufferSlot = frameId % BUFFER_SIZE;
                    predictedInputs[bufferSlot] = inputs;
                    predictedPlayer[bufferSlot].position = rb.position;
                    predictedPlayer[bufferSlot].velocity = rb.velocity;

                    CmdSendInputs(inputs);

                    Move(inputs);
                }

                if (isServer)
                {
                    ServerState state = new ServerState
                    {
                        frameId = frameId++,
                        position = rb.position,
                        velocity = rb.velocity
                    };

                    ClientSendState(state);
                }

                Physics.Simulate(Time.fixedDeltaTime);
            }
        }

        [Command(channel = Channels.Unreliable)]
        void CmdSendInputs(ClientState clientInput)
        {
            clientInput.horizontal = Mathf.Clamp(clientInput.horizontal, -1, 1);
            clientInput.vertical = Mathf.Clamp(clientInput.vertical, -1, 1);

            Move(clientInput);
        }

        [ClientRpc(channel = Channels.Unreliable)]
        void ClientSendState(ServerState serverState)
        {
            if (hasAuthority)
            {
                uint bufferSlot = serverState.frameId % BUFFER_SIZE;
                Vector3 positionError = serverState.position - predictedPlayer[bufferSlot].position;

                if (positionError.magnitude > 0)
                {
                    rb.position = serverState.position;
                    rb.velocity = serverState.velocity;

                    Physics.SyncTransforms();

                    uint rewindTickNumber = serverState.frameId;
                    while (rewindTickNumber++ < frameId)
                    {
                        Move(predictedInputs[rewindTickNumber % BUFFER_SIZE]);

                        Physics.Simulate(Time.fixedDeltaTime);
                    }
                }
            }
            else
            {
                rb.position = serverState.position;
                rb.velocity = serverState.velocity;
            }
        }

        void Move(ClientState input)
        {
            rb.AddForce(new Vector3(input.horizontal, 0, input.vertical), ForceMode.Impulse);
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, 1);
        }    
    }
}
