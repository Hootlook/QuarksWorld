using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;


namespace QuarksWorld
{
    public class ConsoleGUI : MonoBehaviour, IConsoleUI
    {
        [ConfigVar(Name = "console.alpha", DefaultValue = "0.9", Description = "Alpha of console")]
        public static ConfigVar consoleAlpha;

        void Awake()
        {
            inputField.onEndEdit.AddListener(OnSubmit);
        }

        public void Init()
        {
            buildIdText.text = Game.game.BuildId + " (" + Application.unityVersion + ")";
        }

        public void Shutdown()
        {

        }

        public void OutputString(string s)
        {
            lines.Add(s);
            var count = Mathf.Min(100, lines.Count);
            var start = lines.Count - count;
            textArea.text = string.Join("\n", lines.GetRange(start, count).ToArray());
        }

        public bool IsOpen()
        {
            return panel.gameObject.activeSelf;
        }

        public void SetOpen(bool open)
        {
            Game.Input.SetBlock(Game.Input.Blocker.Console, open);

            panel.gameObject.SetActive(open);
            if (open)
            {
                inputField.ActivateInputField();
            }
        }

        public void ConsoleUpdate()
        {
            if (Input.GetKeyDown(toggleConsoleKey) || Input.GetKeyDown(KeyCode.Backslash))
                SetOpen(!IsOpen());

            if (!IsOpen())
                return;

            var c = textAreaBackground.color;
            c.a = Mathf.Clamp01(consoleAlpha.FloatValue);
            textAreaBackground.color = c;

            // This is to prevent clicks outside input field from removing focus
            inputField.ActivateInputField();

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (inputField.caretPosition == inputField.text.Length && inputField.text.Length > 0)
                {
                    var res = Console.TabComplete(inputField.text);
                    inputField.text = res;
                    inputField.caretPosition = res.Length;
                }
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                inputField.text = Console.HistoryUp(inputField.text);
                wantedCaretPosition = inputField.text.Length;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                inputField.text = Console.HistoryDown();
                inputField.caretPosition = inputField.text.Length;
            }
        }

        public void ConsoleLateUpdate()
        {
            // This has to happen here because keys like KeyUp will navigate the caret
            // int the UI event handling which runs between Update and LateUpdate
            if (wantedCaretPosition > -1)
            {
                inputField.caretPosition = wantedCaretPosition;
                wantedCaretPosition = -1;
            }
        }


        void OnSubmit(string value)
        {
            // Only react to this if enter was actually pressed. Submit can also happen by mouseclicks.
            if (!Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.KeypadEnter))
                return;

            inputField.text = "";
            inputField.ActivateInputField();

            Console.EnqueueCommand(value);
        }

        public void SetPrompt(string prompt)
        {
        }

        List<string> lines = new List<string>();
        int wantedCaretPosition = -1;

        [SerializeField] Transform panel;
        [SerializeField] InputField inputField;
        [SerializeField] Text textArea;
        [SerializeField] Image textAreaBackground;
        [SerializeField] KeyCode toggleConsoleKey;
        [SerializeField] Text buildIdText;
    }
}