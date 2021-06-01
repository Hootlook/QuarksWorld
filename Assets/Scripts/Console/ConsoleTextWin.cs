using UnityEngine;
using System;

#if UNITY_STANDALONE_WIN

using System.IO;
using System.Runtime.InteropServices;

namespace QuarksWorld
{
    public class ConsoleTextWin : IConsoleUI
    {
        [DllImport("Kernel32.dll")]
        private static extern bool AttachConsole(uint processId);

        [DllImport("Kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("Kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("Kernel32.dll")]
        private static extern bool SetConsoleTitle(string title);

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public ConsoleTextWin(string consoleTitle, bool restoreFocus)
        {
            this.restoreFocus = restoreFocus;
            this.consoleTitle = consoleTitle;
        }

        public void Init()
        {
            if (!AttachConsole(0xffffffff))
            {
                if (restoreFocus)
                {
                    foregroundWindow = GetForegroundWindow();
                    resetWindowTime = Time.time + 1;
                }
                AllocConsole();
            }
            previousOutput = System.Console.Out;
            SetConsoleTitle(consoleTitle);
            System.Console.BackgroundColor = System.ConsoleColor.Black;
            System.Console.Clear();
            System.Console.SetOut(new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true });
            currentLine = "";
            DrawInputline();
        }

        public void Shutdown()
        {
            OutputString("Console shutdown");
            System.Console.SetOut(previousOutput);
            FreeConsole();
        }

        public void ConsoleUpdate()
        {
            if (foregroundWindow != IntPtr.Zero && Time.time > resetWindowTime)
            {
                ShowWindow(foregroundWindow, 9);
                SetForegroundWindow(foregroundWindow);
                foregroundWindow = IntPtr.Zero;
            }

            if (!System.Console.KeyAvailable)
                return;

            var keyInfo = System.Console.ReadKey();

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    Console.EnqueueCommand(currentLine);
                    currentLine = "";
                    DrawInputline();
                    break;
                case ConsoleKey.Escape:
                    currentLine = "";
                    DrawInputline();
                    break;
                case ConsoleKey.Backspace:
                    if (currentLine.Length > 0)
                        currentLine = currentLine.Substring(0, currentLine.Length - 1);
                    DrawInputline();
                    break;
                case ConsoleKey.UpArrow:
                    currentLine = Console.HistoryUp(currentLine);
                    DrawInputline();
                    break;
                case ConsoleKey.DownArrow:
                    currentLine = Console.HistoryDown();
                    DrawInputline();
                    break;
                case ConsoleKey.Tab:
                    currentLine = Console.TabComplete(currentLine);
                    DrawInputline();
                    break;
                default:
                    {
                        if (keyInfo.KeyChar != '\u0000')
                        {
                            currentLine += keyInfo.KeyChar;
                            DrawInputline();
                        }
                    }
                    break;
            }
        }

        public void ConsoleLateUpdate()
        {
        }

        public bool IsOpen()
        {
            return true;
        }

        public void OutputString(string message)
        {
            ClearInputLine();
            System.Console.WriteLine(message);
            DrawInputline();
        }

        public void SetOpen(bool open)
        {
        }

        void ClearInputLine()
        {
            System.Console.CursorLeft = 0;
            System.Console.CursorTop = System.Console.BufferHeight - 1;
            System.Console.BackgroundColor = System.ConsoleColor.Black;
            System.Console.Write(new string(' ', System.Console.BufferWidth - 1));
            System.Console.CursorLeft = 0;
        }

        void DrawInputline()
        {
            System.Console.CursorLeft = 0;
            System.Console.CursorTop = System.Console.BufferHeight - 1;
            System.Console.BackgroundColor = System.ConsoleColor.Blue;
            System.Console.Write(prompt + currentLine + new string(' ', System.Console.BufferWidth - currentLine.Length - prompt.Length - 1));
            System.Console.CursorLeft = currentLine.Length + prompt.Length;
        }

        public void SetPrompt(string prompt)
        {
            this.prompt = prompt;
        }

        string prompt = "";
        bool restoreFocus;
        string consoleTitle;
        float resetWindowTime;
        IntPtr foregroundWindow;

        string currentLine;
        TextWriter previousOutput;
    }
}
#endif
