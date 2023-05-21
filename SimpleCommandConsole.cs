using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PunIntended.Tools
{
    public static class SimpleCommandConsole
    {
        private static string _currentUserInput;
        private static Vector2 _consoleScroll;
        private static Vector2 _autoCompleteScroll;
        
        private static event Action OnPressedEnter;
        private static event Action OnPressedTab;
        private static event Action OnPressedUp;
        private static event Action OnPressedDown;

        private static readonly Dictionary<string, MethodInfo> _availableCommands = new Dictionary<string, MethodInfo>();
        private static readonly List<ConsoleLine> _history = new List<ConsoleLine>();

        private static int _autoCompleteIndex;
        private static int _lastCompletionCount;
        private static bool _isOpen;
        private static KeyCode _closeKeyCode;

        private const string _inputField = "InputField";
        private const int _maxHistory = 100;

        private static readonly Texture2D _texture = new Texture2D(1, 1);

        private static readonly Dictionary<Type, string> _readableTypeNames = new Dictionary<Type, string>
        {
            { typeof(int),      "int" },
            { typeof(float),    "float" },
            { typeof(bool),     "bool" }
        };

        public static void Open(KeyCode closeKeyCode)
        {
            if (!_isOpen)
            {
                _isOpen = true;
                _closeKeyCode = closeKeyCode;
                UnityEventManager.OnGUIUpdate += DrawGUI;
                UnityEventManager.OnGUIUpdate += ReadInputs;

                OnPressedEnter += PressedEnter;
                OnPressedTab += TryAutoComplete;

                OnPressedDown += AutoCompleteSelectionMoveDown;
                OnPressedUp += AutoCompleteSelectionMoveUp;
            }
        }

        private static void OnClose()
        {
            _isOpen = false;
            UnityEventManager.OnGUIUpdate -= DrawGUI;
            UnityEventManager.OnGUIUpdate -= ReadInputs;

            OnPressedEnter -= PressedEnter;
            OnPressedTab -= TryAutoComplete;

            OnPressedDown -= AutoCompleteSelectionMoveDown;
            OnPressedUp -= AutoCompleteSelectionMoveUp;
        }

        // we should probably optimize this in some way. looping through every method could get slow for bigger projects.
        // we also only find commands inside the same assembly as this script. perhaps try to find
        // a nice way for commands to add themselves to the dictionary, that doesnt require extra work for the user.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(
                    BindingFlags.Public | 
                    BindingFlags.NonPublic | 
                    BindingFlags.Instance | 
                    BindingFlags.Static))
                {
                    // add method to commands dictionary if it has our attribute
                    CommandAttribute command = method.GetCustomAttribute<CommandAttribute>();
                    if (command != null)
                    {
                        string key = command.Name == string.Empty ? method.Name : command.Name;
                        _availableCommands.Add(key, method);
                    }
                }
            }

            WriteLine($"type 'help' to get started...", Color.white);
        }

        private static void ReadInputs()
        {
            if (Event.current.isKey)
            {
                if (Event.current.keyCode == KeyCode.Return)
                {
                    OnPressedEnter?.Invoke();
                }

                if (Event.current.keyCode == KeyCode.Tab)
                {
                    OnPressedTab?.Invoke();
                }

                if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    OnPressedDown?.Invoke();
                }

                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    OnPressedUp?.Invoke();
                }
            }
        }

        private static void AutoCompleteSelectionMoveUp()
        {
            _autoCompleteIndex--;
            if (_autoCompleteIndex < 0)
            {
                _autoCompleteIndex = 0;
            }
        }

        private static void AutoCompleteSelectionMoveDown()
        {
            _autoCompleteIndex++;
            int maxIndex = GetAutoCompletedCommands().Count - 1;
            if (_autoCompleteIndex > maxIndex)
            {
                _autoCompleteIndex = maxIndex;
            }
        }

        private static void DrawGUI()
        {
            // check if we need to close the window
            if (Event.current.Equals(Event.KeyboardEvent(_closeKeyCode.ToString())))
            {
                OnClose();
                return;
            }

            Color defaultGUIColor = GUI.color;
            Rect windowRect = new Rect(5f, 5f, Screen.width / 2f, Screen.height / 1.5f);

            GUILayout.BeginArea(windowRect, GUI.skin.window);
            _consoleScroll = GUILayout.BeginScrollView(_consoleScroll);
            GUILayout.FlexibleSpace();

            // draw command history
            const float lineHeight = 23f;
            for (int i = 0; i < _history.Count; i++)
            {
                const float light = .5f;
                const float dark = .1f;
                const float alpha = .5f;
                Color backgroundColor = i % 2 == 0 ? new Color(light, light, light, alpha) : new Color(dark, dark, dark, alpha);
                _texture.SetPixel(0, 0, backgroundColor);
                _texture.Apply();
                GUIStyle backgroundStyle = new GUIStyle();
                backgroundStyle.border = new RectOffset(2, 2, 2, 2);
                backgroundStyle.normal.background = _texture;

                Rect rect = new Rect(windowRect.x, windowRect.y + lineHeight * i, windowRect.width, lineHeight);
                GUI.color = backgroundColor;
                GUI.Box(rect, GUIContent.none, backgroundStyle);
                GUI.color = defaultGUIColor;

                ConsoleLine command = _history[i];
                GUIStyle style = GUI.skin.label;
                style.fontStyle = FontStyle.Bold;
                style.fontSize = 14;
                style.richText = true;

                GUI.color = command.Color;
                GUI.Label(rect, command.String, GUI.skin.label);
                GUILayout.Space(lineHeight);
            }

            GUILayout.EndScrollView();

            GUI.color = defaultGUIColor;
            GUI.SetNextControlName(_inputField);
            _currentUserInput = GUILayout.TextField(_currentUserInput);
            GUI.FocusControl(_inputField);

            GUILayout.EndArea();

            // show auto complete options
            if (!string.IsNullOrWhiteSpace(_currentUserInput))
            {
                Rect autoCompleteRect = new Rect(5f, 5f + Screen.height / 1.5f, Screen.width / 2f, 200f);
                GUILayout.BeginArea(autoCompleteRect);
                _autoCompleteScroll = GUILayout.BeginScrollView(_autoCompleteScroll);

                if (_availableCommands.Values.Count != _lastCompletionCount)
                {
                    _autoCompleteIndex = 0;
                    _lastCompletionCount = _availableCommands.Values.Count;
                }

                int index = 0;
                List<string> autoCompletedCommands = GetAutoCompletedCommands();
                foreach (string command in autoCompletedCommands)
                {
                    GUI.color = _autoCompleteIndex == index ? Color.cyan : defaultGUIColor;
                    GUIStyle style = GUI.skin.button;
                    style.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(command, style))
                    {
                        _autoCompleteIndex = index;
                        PressedEnter();
                    }

                    index++;
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }

            GUI.color = defaultGUIColor;
        }

        private static List<string> GetAutoCompletedCommands()
        {
            List<string> commands = new List<string>();
            foreach (string command in _availableCommands.Keys)
            {
                string loweredCommand = new string(command.ToCharArray()).ToLower();
                string loweredUserInput = new string(_currentUserInput.ToCharArray()).ToLower();
                if (loweredCommand.Contains(loweredUserInput))
                {
                    commands.Add(command);
                }
            }

            return commands;
        }

        private static void TryAutoComplete()
        {
            // try update current input
            int index = 0;
            foreach (string command in GetAutoCompletedCommands())
            {
                if (index == _autoCompleteIndex && _currentUserInput != command)
                {
                    string[] commandNameOnly = command.Split(' ');
                    _currentUserInput = commandNameOnly[0];
                }

                index++;
            }

            // move cursor to end of line
            TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            Vector2 max = new Vector2(999, 999);
            editor.MoveCursorToPosition(max);
        }

        private static void PressedEnter()
        {
            _autoCompleteIndex = 0;
            _consoleScroll.y = Mathf.Infinity;

            if (string.IsNullOrWhiteSpace(_currentUserInput))
            {
                return;
            }

            string[] commandAndParameters = _currentUserInput.Split(' ');
            if (_availableCommands.TryGetValue(commandAndParameters[0], out MethodInfo methodInfo))
            {
                int parameterCount = commandAndParameters.Length - 1;
                object[] parameters = new object[parameterCount];
                for (int i = 0; i < parameterCount; i++)
                {
                    parameters[i] = commandAndParameters[i + 1];
                }

                if (CastParametersForCommand(methodInfo, ref parameters))
                {
                    ExecuteCommand(methodInfo, parameters);
                }
            }
            else
            {
                WriteLine($"{_currentUserInput} is not a valid command!", Color.red);
            }

            _currentUserInput = string.Empty;
        }

        private static bool CastParametersForCommand(MethodInfo methodInfo, ref object[] parameters)
        {
            int expectedParameterCount = methodInfo.GetParameters().Length;
            if ( methodInfo.GetParameters().Length != parameters.Length)
            {
                WriteLine($"{parameters.Length} parameters provided, expected {expectedParameterCount}!", Color.red);
                return false;
            }

            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            for (int i = 0; i < methodInfo.GetParameters().Length; i++)
            {
                ParameterInfo parameterInfo = parameterInfos[i];
                Type parameterType = parameterInfo.ParameterType;
                // ints
                if (parameterType == typeof(int))
                {
                    if (int.TryParse((string)parameters[i], out int integer))
                    {
                        parameters[i] = integer;
                    }
                    else
                    {
                        WriteLine($"{(string)parameters[i]} was provided as parameter, but expected an integer!", Color.red);
                        return false;
                    }
                }
                // bools
                else if (parameterType == typeof(bool))
                {
                    if ((string)parameters[i] == "true")
                    {
                        parameters[i] = true;
                    }
                    else if ((string)parameters[i] == "false")
                    {
                        parameters[i] = false;
                    }
                    else
                    {
                        WriteLine($"{(string)parameters[i]} was provided as parameter, but expected a bool!", Color.red);
                        return false;
                    }
                }
                // floats
                else if (parameterType == typeof(float))
                {
                    if (float.TryParse((string)parameters[i], out float floatingPoint))
                    {
                        parameters[i] = floatingPoint;
                    }
                    else
                    {
                        WriteLine($"{(string)parameters[i]} was provided as parameter, but expected a float!", Color.red);
                        return false;
                    }
                }
                else
                {
                    WriteLine($"parameter {(string)parameters[i]} is of an unexpected type!", Color.red);
                    return false;
                }
            }

            return true;
        }

        private static void ExecuteCommand(MethodInfo methodInfo, object[] parameters)
        {
            Type classMethodType = methodInfo.DeclaringType;
            if (classMethodType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                // find all instances of command on a monobehaviour
                List<UnityEngine.Object> classInstances = UnityEngine.Object.FindObjectsOfType(classMethodType).ToList();
                if (classInstances.Count < 1)
                {
                    string message = GetCommandAsString(methodInfo);
                    WriteLine($"no instances for {message} were found!", Color.red);
                    return;
                }

                WriteLine(_currentUserInput, Color.white);
                foreach (MonoBehaviour o in classInstances.Cast<MonoBehaviour>())
                {
                    methodInfo.Invoke(o, parameters);
                }

                return;
            }
            else if (methodInfo.IsStatic)
            {
                // call command on a static class
                WriteLine(_currentUserInput, Color.white);
                methodInfo.Invoke(null, parameters);
                return;
            }

            WriteLine($"{methodInfo.Name} is neither is neiter static or inside a monobehaviour!", Color.red);
        }

        private static string GetCommandAsString(MethodInfo methodInfo)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();
            string customName = methodInfo.GetCustomAttribute<CommandAttribute>().Name;
            string message = customName == string.Empty ? methodInfo.Name : customName;
            message += " ";
            message += "<color=#a3d3d3>";
            for (int i = 0; i < parameters.Length; i++)
            {
                message += parameters[i].Name.ToLower();
                message += "(";
                message += GetHumanReadableName(parameters[i].ParameterType);
                message += ")";
                message += " ";
            }

            message += "</color>";

            return message;
        }

        public static void WriteLine(string line, Color color)
        {
            ConsoleLine command = new ConsoleLine(line, color);
            _history.Add(command);
            if (_history.Count > _maxHistory)
            {
                _history.RemoveAt(0);
            }
        }

        public static string GetHumanReadableName(Type type)
        {
            if (_readableTypeNames.TryGetValue(type, out string name))
            {
                return name;
            }

            return type.Name;
        }

        [Command("help")]
        public static void HelpCommand()
        {
            WriteLine("------------------------------------------", Color.yellow);
            WriteLine("supported parameter types:", Color.yellow);
            WriteLine("int, float, bool", Color.white); 
            WriteLine("available commands: ", Color.yellow);
            foreach (KeyValuePair<string, MethodInfo> command in _availableCommands) 
            {
                string message = GetCommandAsString(command.Value);
                WriteLine(message, Color.white);
            }

            WriteLine("------------------------------------------", Color.yellow);
        }

        private readonly struct ConsoleLine
        {
            public readonly string String;
            public readonly Color Color;

            public ConsoleLine(string s, Color c)
            {
                String = s;
                Color = c;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; private set; }

        public CommandAttribute()
        {
            Name = string.Empty;
        }

        public CommandAttribute(string name)
        {
            Name = name;
        }
    }
}
