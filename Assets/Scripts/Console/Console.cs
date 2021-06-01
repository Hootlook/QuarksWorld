using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public interface IConsoleUI
    {
        void Init();
        void Shutdown();
        void OutputString(string message);
        bool IsOpen();
        void SetOpen(bool open);
        void ConsoleUpdate();
        void ConsoleLateUpdate();
        void SetPrompt(string prompt);
    }

    public class ConsoleNullUI : IConsoleUI
    {
        public void ConsoleUpdate()
        {
        }

        public void ConsoleLateUpdate()
        {
        }

        public void Init()
        {
        }

        public void Shutdown()
        {
        }

        public bool IsOpen()
        {
            return false;
        }

        public void OutputString(string message)
        {
        }

        public void SetOpen(bool open)
        {
        }

        public void SetPrompt(string prompt)
        {
        }
    }

    public class Console
    {
        public delegate void MethodDelegate(string[] args);

        static IConsoleUI consoleUI;

        public static void Init(IConsoleUI consoleUI)
        {
            GameDebug.Assert(Console.consoleUI == null);

            Console.consoleUI = consoleUI;
            Console.consoleUI.Init();
            AddCommand("help", CmdHelp, "Show available commands");
            AddCommand("vars", CmdVars, "Show available variables");
            AddCommand("wait", CmdWait, "Wait for next frame or level");
            AddCommand("waitload", CmdWaitLoad, "Wait for level load");
            AddCommand("exec", CmdExec, "Executes commands from file");
            Write("Console ready");
        }

        public static void Shutdown()
        {
            consoleUI.Shutdown();
        }

        static void OutputString(string message)
        {
            if (consoleUI != null)
                consoleUI.OutputString(message);
        }

        static string lastMsg = "";
        static double timeLastMsg;
        public static void Write(string msg)
        {
            // Have to condition on cvar being null as this may run before cvar system is initialized
            if (consoleShowLastLine != null && consoleShowLastLine.IntValue > 0)
            {
                lastMsg = msg;
                timeLastMsg = Game.frameTime;
            }
            OutputString(msg);
        }

        public static void AddCommand(string name, MethodDelegate method, string description, int tag = 0)
        {
            name = name.ToLower();
            if (commands.ContainsKey(name))
            {
                OutputString("Cannot add command " + name + " twice");
                return;
            }
            commands.Add(name, new ConsoleCommand(name, method, description, tag));
        }

        public static bool RemoveCommand(string name)
        {
            return commands.Remove(name.ToLower());
        }

        public static void RemoveCommandsWithTag(int tag)
        {
            var removals = new List<string>();
            foreach (var c in commands)
            {
                if (c.Value.tag == tag)
                    removals.Add(c.Key);
            }
            foreach (var c in removals)
                RemoveCommand(c);
        }

        public static void ProcessCommandLineArguments(string[] arguments)
        {
            // Process arguments that have '+' prefix as console commands. Ignore all other arguments

            OutputString("ProcessCommandLineArguments: " + string.Join(" ", arguments));

            var commands = new List<string>();

            foreach (var argument in arguments)
            {
                var newCommandStarting = argument.StartsWith("+") || argument.StartsWith("-");

                // Skip leading arguments before we have seen '-' or '+'
                if (commands.Count == 0 && !newCommandStarting)
                    continue;

                if (newCommandStarting)
                    commands.Add(argument);
                else
                    commands[commands.Count - 1] += " " + argument;
            }

            foreach (var command in commands)
            {
                if (command.StartsWith("+"))
                    EnqueueCommandNoHistory(command.Substring(1));
            }
        }

        public static bool IsOpen()
        {
            return consoleUI.IsOpen();
        }

        public static void SetOpen(bool open)
        {
            consoleUI.SetOpen(open);
        }

        public static void SetPrompt(string prompt)
        {
            consoleUI.SetPrompt(prompt);
        }

        public static void Update()
        {
            //var lastMsgTime = Game.frameTime - timeLastMsg;
            //if (lastMsgTime < 1.0)
            //    DebugOverlay.Write(0, 0, lastMsg);

            consoleUI.ConsoleUpdate();

            while (pendingCommands.Count > 0)
            {
                if (PendingCommandsWaitForFrames > 0)
                {
                    PendingCommandsWaitForFrames--;
                    break;
                }
                if (pendingCommandsWaitForLoad)
                {
                    if (!Game.game.levelManager.IsCurrentLevelLoaded())
                        break;
                    pendingCommandsWaitForLoad = false;
                }
                // Remove before executing as we may hit an 'exec' command that wants to insert commands
                var cmd = pendingCommands[0];
                pendingCommands.RemoveAt(0);
                ExecuteCommand(cmd);
            }
        }

        public static void LateUpdate()
        {
            consoleUI.ConsoleLateUpdate();
        }

        static void SkipWhite(string input, ref int pos)
        {
            while (pos < input.Length && " \t".IndexOf(input[pos]) > -1)
            {
                pos++;
            }
        }

        static string ParseQuoted(string input, ref int pos)
        {
            pos++;
            int startPos = pos;
            while (pos < input.Length)
            {
                if (input[pos] == '"' && input[pos - 1] != '\\')
                {
                    pos++;
                    return input.Substring(startPos, pos - startPos - 1);
                }
                pos++;
            }
            return input.Substring(startPos);
        }

        static string Parse(string input, ref int pos)
        {
            int startPos = pos;
            while (pos < input.Length)
            {
                if (" \t".IndexOf(input[pos]) > -1)
                {
                    return input.Substring(startPos, pos - startPos);
                }
                pos++;
            }
            return input.Substring(startPos);
        }

        static List<string> Tokenize(string input)
        {
            var pos = 0;
            var res = new List<string>();
            var c = 0;
            while (pos < input.Length && c++ < 10000)
            {
                SkipWhite(input, ref pos);
                if (pos == input.Length)
                    break;

                if (input[pos] == '"' && (pos == 0 || input[pos - 1] != '\\'))
                {
                    res.Add(ParseQuoted(input, ref pos));
                }
                else
                    res.Add(Parse(input, ref pos));
            }
            return res;
        }

        public static void ExecuteCommand(string command)
        {
            var tokens = Tokenize(command);
            if (tokens.Count < 1)
                return;

            OutputString('>' + command);
            var commandName = tokens[0].ToLower();

            ConsoleCommand consoleCommand;
            ConfigVar configVar;

            if (commands.TryGetValue(commandName, out consoleCommand))
            {
                var arguments = tokens.GetRange(1, tokens.Count - 1).ToArray();
                consoleCommand.method(arguments);
            }
            else if (ConfigVar.ConfigVars.TryGetValue(commandName, out configVar))
            {
                if (tokens.Count == 2)
                {
                    configVar.Value = tokens[1];
                }
                else if (tokens.Count == 1)
                {
                    // Print value
                    OutputString(string.Format("{0} = {1}", configVar.name, configVar.Value));
                }
                else
                {
                    OutputString("Too many arguments");
                }
            }
            else
            {
                OutputString("Unknown command: " + tokens[0]);
            }
        }

        static void CmdHelp(string[] arguments)
        {
            OutputString("Available commands:");

            foreach (var c in commands)
                OutputString(c.Value.name + ": " + c.Value.description);
        }

        static void CmdVars(string[] arguments)
        {
            var varNames = new List<string>(ConfigVar.ConfigVars.Keys);
            varNames.Sort();
            foreach (var v in varNames)
            {
                var cv = ConfigVar.ConfigVars[v];
                OutputString(string.Format("{0} = {1}", cv.name, cv.Value));
            }
        }

        static void CmdWait(string[] arguments)
        {
            if (arguments.Length == 0)
            {
                PendingCommandsWaitForFrames = 1;
            }
            else if (arguments.Length == 1)
            {
                int f = 0;
                if (int.TryParse(arguments[0], out f))
                {
                    PendingCommandsWaitForFrames = f;
                }
            }
            else
            {
                OutputString("Usage: wait [n] \nWait for next n frames. Default is 1\n");
            }
        }

        static void CmdWaitLoad(string[] arguments)
        {
            if (arguments.Length != 0)
            {
                OutputString("Usage: waitload\nWait for level load\n");
                return;
            }
            if (!Game.game.levelManager.IsLoadingLevel())
            {
                OutputString("waitload: not loading level; ignoring\n");
                return;
            }
            pendingCommandsWaitForLoad = true;
        }

        static void CmdExec(string[] arguments)
        {
            bool silent = false;
            string filename = "";
            if (arguments.Length == 1)
            {
                filename = arguments[0];
            }
            else if (arguments.Length == 2 && arguments[0] == "-s")
            {
                silent = true;
                filename = arguments[1];
            }
            else
            {
                OutputString("Usage: exec [-s] <filename>");
                return;
            }

            try
            {
                var lines = System.IO.File.ReadAllLines(filename);
                pendingCommands.InsertRange(0, lines);
                if (pendingCommands.Count > 128)
                {
                    pendingCommands.Clear();
                    OutputString("Command overflow. Flushing pending commands!!!");
                }
            }
            catch (Exception e)
            {
                if (!silent)
                    OutputString("Exec failed: " + e.Message);
            }
        }

        public static void EnqueueCommandNoHistory(string command)
        {
            GameDebug.Log("cmd: " + command);
            pendingCommands.Add(command);
        }

        public static void EnqueueCommand(string command)
        {
            history[historyNextIndex % HISTORY_COUNT] = command;
            historyNextIndex++;
            historyIndex = historyNextIndex;

            EnqueueCommandNoHistory(command);
        }


        public static string TabComplete(string prefix)
        {
            // Look for possible tab completions
            List<string> matches = new List<string>();

            foreach (var c in commands)
            {
                var name = c.Key;
                if (!name.StartsWith(prefix, true, null))
                    continue;
                matches.Add(name);
            }

            foreach (var v in ConfigVar.ConfigVars)
            {
                var name = v.Key;
                if (!name.StartsWith(prefix, true, null))
                    continue;
                matches.Add(name);
            }

            if (matches.Count == 0)
                return prefix;

            // Look for longest common prefix
            int lcp = matches[0].Length;
            for (var i = 0; i < matches.Count - 1; i++)
            {
                lcp = Mathf.Min(lcp, CommonPrefix(matches[i], matches[i + 1]));
            }
            prefix += matches[0].Substring(prefix.Length, lcp - prefix.Length);
            if (matches.Count > 1)
            {
                // write list of possible completions
                for (var i = 0; i < matches.Count; i++)
                    Console.Write(" " + matches[i]);
            }
            else
            {
                prefix += " ";
            }
            return prefix;
        }

        public static string HistoryUp(string current)
        {
            if (historyIndex == 0 || historyNextIndex - historyIndex >= HISTORY_COUNT - 1)
                return "";

            if (historyIndex == historyNextIndex)
            {
                history[historyIndex % HISTORY_COUNT] = current;
            }

            historyIndex--;

            return history[historyIndex % HISTORY_COUNT];
        }

        public static string HistoryDown()
        {
            if (historyIndex == historyNextIndex)
                return "";

            historyIndex++;

            return history[historyIndex % HISTORY_COUNT];
        }

        // Returns length of largest common prefix of two strings
        static int CommonPrefix(string a, string b)
        {
            int minl = Mathf.Min(a.Length, b.Length);
            for (int i = 1; i <= minl; i++)
            {
                if (!a.StartsWith(b.Substring(0, i), true, null))
                    return i - 1;
            }
            return minl;
        }

        class ConsoleCommand
        {
            public string name;
            public MethodDelegate method;
            public string description;
            public int tag;

            public ConsoleCommand(string name, MethodDelegate method, string description, int tag)
            {
                this.name = name;
                this.method = method;
                this.description = description;
                this.tag = tag;
            }
        }

        [ConfigVar(Name = "config.showlastline", DefaultValue = "0", Description = "Show last logged line briefly at top of screen")]
        public static ConfigVar consoleShowLastLine;

        static List<string> pendingCommands = new List<string>();
        public static int PendingCommandsWaitForFrames = 0;
        public static bool pendingCommandsWaitForLoad = false;
        static Dictionary<string, ConsoleCommand> commands = new Dictionary<string, ConsoleCommand>();
        const int HISTORY_COUNT = 50;
        static string[] history = new string[HISTORY_COUNT];
        static int historyNextIndex = 0;
        static int historyIndex = 0;
    }
}