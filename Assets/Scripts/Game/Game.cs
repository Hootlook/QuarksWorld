using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace QuarksWorld
{
    [DefaultExecutionOrder(-1000)]
    public class Game : MonoBehaviour
    {
        public static Game game;

        public static readonly string UserConfigFilename = "user.cfg";
        public static readonly string BootConfigFilename = "boot.cfg";

        public string BuildId { get; } = "NoBuild";

        public static double frameTime;
        public LevelManager levelManager;

        // Vars owned by server and replicated to clients
        [ConfigVar(Name = "sv.tickrate", DefaultValue = "60", Description = "Tickrate for server", Flags = ConfigVar.Flags.ServerInfo)]
        public static ConfigVar serverTickRate;

        [ConfigVar(Name = "sv.cheat", DefaultValue = "0", Description = "Enable cheats for self and clients", Flags = ConfigVar.Flags.ServerInfo)]
        public static ConfigVar allowCheats;

        [ConfigVar(Name = "cfg.inverty", DefaultValue = "0", Description = "Invert y mouse axis", Flags = ConfigVar.Flags.Save)]
        public static ConfigVar configInvertY;

        [ConfigVar(Name = "cfg.mousesensitivity", DefaultValue = "1.5", Description = "Mouse sensitivity", Flags = ConfigVar.Flags.Save)]
        public static ConfigVar configMouseSensitivity;

        [ConfigVar(Name = "cfg.fov", DefaultValue = "60", Description = "Field of view", Flags = ConfigVar.Flags.Save)]
        public static ConfigVar configFov;

        void Awake()
        {
            GameDebug.Assert(game == null);
            DontDestroyOnLoad(gameObject);
            game = this;

            stopwatchFrequency = Stopwatch.Frequency;
            clock = new Stopwatch();
            clock.Start();

            Application.targetFrameRate = 60;

            var args = new List<string>(Environment.GetCommandLineArgs());
            isHeadless = args.Contains("-batchmode");

            if (isHeadless)
            {
#if UNITY_STANDALONE_WIN
                var title = $"{Application.productName} [{Process.GetCurrentProcess().Id}]";

                var consoleUI = new ConsoleTextWin(title, false);
#else
                UnityEngine.Debug.Log("WARNING: starting without a console");
                var consoleUI = new ConsoleNullUI();
#endif
                Console.Init(consoleUI);
            }
            else
            {
                var consolePrefab = Resources.Load<ConsoleGUI>("Prefabs/ConsoleGUI");
                var consoleUI = Instantiate(consolePrefab);
                consoleUI.name = consolePrefab.name;
                DontDestroyOnLoad(consoleUI);
                Console.Init(consoleUI);
            }

            if (args.Contains("-logfile"))
            {
                var engineLogFileLocation = ".";
                var logfileArgIdx = args.IndexOf("-logfile");
                if (logfileArgIdx >= 0 && args.Count >= logfileArgIdx)
                    engineLogFileLocation = System.IO.Path.GetDirectoryName(args[logfileArgIdx + 1]);

                var logName = isHeadless ? "game_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") : "game";
                GameDebug.Init(engineLogFileLocation, logName);
            }
            else
                GameDebug.Init();


            ConfigVar.Init();
            GameDebug.Log("ConfigVars initialized");

            if (isHeadless)
            {
                Application.targetFrameRate = serverTickRate.IntValue;
                QualitySettings.vSyncCount = 0; // Needed to make targetFramerate work; even in headless mode
            }

            // Out of the box game behaviour is driven by boot.cfg unless you ask it not to
            //if (!args.Contains("-noboot"))
            //    Console.EnqueueCommandNoHistory("exec -s " + BootConfigFilename);

            GameDebug.Log("BuildID: " + BuildId);

            levelManager = new LevelManager();
            levelManager.Init();
            GameDebug.Log("LevelManager initialized");

            Mirror.Transport.activeTransport = GetComponent<Mirror.Transport>();
            GameDebug.Log($"Transport initialized ({Mirror.Transport.activeTransport})");

            // Game loops
            Console.AddCommand("serve", CmdServe, "Start server listening");
            Console.AddCommand("client", CmdClient, "client: Enter client mode.");
            Console.AddCommand("boot", CmdBoot, "Go back to boot loop");
            Console.AddCommand("connect", CmdConnect, "connect <ip>: Connect to server on ip (default: localhost)");

            Console.AddCommand("load", CmdLoad, "Load map at boot level");

            Console.ProcessCommandLineArguments(args.ToArray());

            PushCamera(bootCamera);
        }

        bool errorState;
        void Update()
        {
            frameTime = (double)clock.ElapsedTicks / stopwatchFrequency;

            // Switch game loop if needed
            if (requestedGameLoopTypes.Count > 0)
            {
                // Multiple running gameloops only allowed in editor
#if !UNITY_EDITOR
            ShutdownGameLoops();
#endif
                bool initSucceeded = false;
                for (int i = 0; i < requestedGameLoopTypes.Count; i++)
                {
                    try
                    {
                        IGameLoop gameLoop = (IGameLoop)Activator.CreateInstance(requestedGameLoopTypes[i]);
                        initSucceeded = gameLoop.Init(requestedGameLoopArguments[i]);
                        if (!initSucceeded)
                            break;

                        gameLoops.Add(gameLoop);
                    }
                    catch (Exception e)
                    {
                        GameDebug.Log(string.Format("Game loop initialization threw exception : ({0})\n{1}", e.Message, e.StackTrace));
                    }
                }


                if (!initSucceeded)
                {
                    ShutdownGameLoops();

                    GameDebug.Log("Game loop initialization failed ... reverting to boot loop");
                }

                requestedGameLoopTypes.Clear();
                requestedGameLoopArguments.Clear();
            }

            try
            {
                if (!errorState)
                {
                    foreach (var gameLoop in gameLoops)
                        gameLoop.Update();

                    levelManager.Update();
                }
            }
            catch (Exception e)
            {
                HandleGameloopException(e);
                throw;
            }

            Console.Update();
        }

        void FixedUpdate()
        {
            foreach (var gameLoop in gameLoops)
            {
                gameLoop.FixedUpdate();
            }
        }

        void LateUpdate()
        {
            try
            {
                if (!errorState)
                {
                    foreach (var gameLoop in gameLoops)
                    {
                        gameLoop.LateUpdate();
                    }
                    Console.LateUpdate();
                }
            }
            catch (Exception e)
            {
                HandleGameloopException(e);
                throw;
            }
        }

        void OnDestroy()
        {
            GameDebug.Shutdown();
            Console.Shutdown();
        }

        void OnApplicationQuit()
        {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        GameDebug.Log("See you soon...");
        System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
            ShutdownGameLoops();
        }

        public static T GetGameLoop<T>() where T : class
        {
            if (game == null)
                return null;

            foreach (IGameLoop gameLoop in game.gameLoops)
                if (gameLoop is T result)
                    return result;

            return null;
        }
        public void RequestGameLoop(Type type, string[] args)
        {
            GameDebug.Assert(typeof(IGameLoop).IsAssignableFrom(type));

            requestedGameLoopTypes.Add(type);
            requestedGameLoopArguments.Add(args);

            GameDebug.Log("Game loop " + type + " requested");
        }
        void HandleGameloopException(Exception e)
        {
            GameDebug.Log("EXCEPTION " + e.Message + "\n" + e.StackTrace);
            Console.SetOpen(true);
            errorState = true;
        }
        void ShutdownGameLoops()
        {
            foreach (var gameLoop in gameLoops)
                gameLoop.Shutdown();
            gameLoops.Clear();
        }

        public static bool IsHeadless()
        {
            return game.isHeadless;
        }
        
        public static bool GetMousePointerLock()
        {
            return Cursor.lockState == CursorLockMode.Locked;
        }

        public void LoadLevel(string levelname)
        {
            if (!Game.game.levelManager.CanLoadLevel(levelname))
            {
                GameDebug.Log("ERROR : Cannot load level : " + levelname);
                return;
            }

            ShutdownGameLoops();

            Game.game.levelManager.LoadLevel(levelname);
        }


        #region Camera Managing

        public Camera TopCamera()
        {
            var c = cameraStack.Count;
            return c == 0 ? null : cameraStack[c - 1];
        }

        public void PushCamera(Camera cam)
        {
            if (cameraStack.Count > 0)
                SetCameraEnabled(cameraStack[cameraStack.Count - 1], false);
            cameraStack.Add(cam);
            SetCameraEnabled(cam, true);
        }

        public void PopCamera(Camera cam)
        {
            GameDebug.Assert(cameraStack.Count > 1, "Tried to pop last camera off stack !");
            GameDebug.Assert(cam == cameraStack[cameraStack.Count - 1]);
            if (cam != null)
                SetCameraEnabled(cam, false);
            cameraStack.RemoveAt(cameraStack.Count - 1);
            SetCameraEnabled(cameraStack[cameraStack.Count - 1], true);
        }

        void SetCameraEnabled(Camera cam, bool enabled)
        {
            //if (enabled)
            //    RenderSettings.UpdateCameraSettings(cam);

            cam.enabled = enabled;
            var audioListener = cam.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = enabled;
                //if (SoundSystem != null)
                //    SoundSystem.SetCurrentListener(enabled ? audioListener : null);
            }
        }

        #endregion

        #region Commands

        void CmdConnect(string[] args)
        {
            // Special hack to allow "connect a.b.c.d" as shorthand
            if (gameLoops.Count == 0)
            {
                RequestGameLoop(typeof(ClientGameLoop), args);
                Console.PendingCommandsWaitForFrames = 1;
                return;
            }

            ClientGameLoop clientGameLoop = GetGameLoop<ClientGameLoop>();

            if (clientGameLoop != null)
                clientGameLoop.CmdConnect(args);
            else
                GameDebug.Log("Cannot connect from current gamemode");
        }

        void CmdServe(string[] args)
        {
            RequestGameLoop(typeof(ServerGameLoop), args);
            Console.PendingCommandsWaitForFrames = 1;
        }

        void CmdBoot(string[] args)
        {
            //clientFrontend.ShowMenu(ClientFrontend.MenuShowing.None);
            levelManager.UnloadLevel();
            ShutdownGameLoops();
            Console.PendingCommandsWaitForFrames = 1;
            Console.SetOpen(true);
        }

        void CmdClient(string[] args)
        {
            RequestGameLoop(typeof(ClientGameLoop), args);
            Console.PendingCommandsWaitForFrames = 1;
        }

        void CmdLoad(string[] args)
        {
            LoadLevel(args[0]);
            Console.SetOpen(false);
        }

        #endregion

        #region Utils

        public interface IGameLoop
        {
            bool Init(string[] args);
            void Shutdown();
            void Update();
            void FixedUpdate();
            void LateUpdate();
        }

        public static class Input
        {
            [Flags]
            public enum Blocker
            {
                None = 0,
                Console = 1,
                Chat = 2,
                Debug = 4,
            }
            static Blocker blocks;

            public static void SetBlock(Blocker b, bool value)
            {
                if (value)
                    blocks |= b;
                else
                    blocks &= ~b;
            }
            public static float GetAxisRaw(string axis) => blocks != Blocker.None ? 0.0f : UnityEngine.Input.GetAxisRaw(axis);
            public static bool GetKey(KeyCode key) => blocks == Blocker.None && UnityEngine.Input.GetKey(key);
            public static bool GetKeyDown(KeyCode key) => blocks == Blocker.None && UnityEngine.Input.GetKeyDown(key);
            public static bool GetMouseButton(int button) => blocks == Blocker.None && UnityEngine.Input.GetMouseButton(button);
            public static bool GetKeyUp(KeyCode key) => blocks == Blocker.None && UnityEngine.Input.GetKeyUp(key);
        }

        #endregion

        List<IGameLoop> gameLoops = new List<IGameLoop>();
        List<Type> requestedGameLoopTypes = new List<Type>();
        List<string[]> requestedGameLoopArguments = new List<string[]>();

        List<Camera> cameraStack = new List<Camera>();
        public Camera bootCamera;

        public bool isHeadless;
        long stopwatchFrequency;
        Stopwatch clock;
    }
}

