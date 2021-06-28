using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    public class Controller : NetworkBehaviour
    {
        // [SyncVar(hook = nameof(OnReceivedServerState))] 
        // ServerState latestServerState;
        // ServerState lastServerState;
        
        // const uint BUFFER_SIZE = 1024;
        // ServerState[] serverStateHistory = new ServerState[BUFFER_SIZE];
        // ClientState[] localPredictedInputs = new ClientState[BUFFER_SIZE];
        // ClientState[] clientInputBuffer = new ClientState[BUFFER_SIZE];

        // uint frameId;
        // float timer;
        // Rigidbody rb;

        // public struct ServerState
        // {
        //     public uint frameId;
        //     public Vector3 position;
        //     public Vector3 velocity;
        // }

        // public struct ClientState
        // {
        //     public uint frameId;
        //     public float horizontal;
        //     public float vertical;
        // }

        // void Start() => rb = GetComponent<Rigidbody>();

        // void OnReceivedServerState(ServerState oldState, ServerState newState)
        // {
        //     uint index = frameId % BUFFER_SIZE;
        //     serverStateHistory[index].frameId = newState.frameId;
        //     serverStateHistory[index].position = newState.position;
        //     serverStateHistory[index].velocity = newState.velocity;

        //     lastServerState = oldState;
        // }

        // void FixedUpdate()
        // {
        //     if (isLocalPlayer)
        //     {
        //         var index = frameId % BUFFER_SIZE;
        //         var serverState = serverStateHistory[index];

        //         Vector3 positionError = rb.position - serverState.position;

        //         if (positionError.magnitude > 0)
        //         {
        //             rb.position = serverState.position;
        //             rb.velocity = serverState.velocity;

        //             Physics.SyncTransforms();

        //             uint rewindTickNumber = serverState.frameId;
        //             while (rewindTickNumber++ < frameId)
        //             {
        //                 Move(localPredictedInputs[rewindTickNumber % BUFFER_SIZE]);

        //                 Physics.Simulate(Time.fixedDeltaTime);
        //             }
        //         }

        //     }   

        //     if (isServer)
        //     {
        //         var index = frameId % BUFFER_SIZE;
        //         var nextClientInput = clientInputBuffer[index];

        //         if (nextClientInput.frameId == frameId)
        //         {
        //             Move(nextClientInput);
        //         }

        //         latestServerState = new ServerState
        //         {
        //             frameId = frameId,
        //             position = rb.position,
        //             velocity = rb.velocity
        //         };

        //         frameId++;
        //     } 
        // }

        // [Command]
        // void CmdSendInputs(ClientState clientState)
        // {
        //     if (clientState.frameId > frameId)
        //     {
        //         var index = clientState.frameId % BUFFER_SIZE;
        //         clientInputBuffer[index] = clientState;
        //     }
        // }

        // void Move(ClientState input)
        // {
        //     rb.AddForce(new Vector3(input.horizontal, 0, input.vertical), ForceMode.Impulse);
        //     rb.velocity = Vector3.ClampMagnitude(rb.velocity, 1);
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
