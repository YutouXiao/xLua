using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace Consolation
{
    /// <summary>
    /// A console to display Unity's debug logs in-game.
    /// </summary>
    [LuaCallCSharp]
    class MemoryUI : MonoBehaviour
    {
        /// <summary>
        /// Debug.log message
        /// </summary>
        struct Log
        {
            public string message;
            public string stackTrace;
            public LogType type;
        }

        #region Inspector Settings

        public class PreMes
        {
            public string title;
            public string message;

            public PreMes(string t, string m)
            {
                title = t;
                message = m;
            }
        }
        private List<PreMes> preMesList = new List<PreMes>();

        /// <summary>
        /// The hotkey to show and hide the console window.
        /// </summary>
        public KeyCode toggleKey = KeyCode.BackQuote;


        /// <summary>
        /// Filter string
        /// </summary>
        public string textToFilter = "";

        /// <summary>
        /// Snap string
        /// </summary>
        public string textToSnap = "";

        /// <summary>
        /// Calculate string
        /// </summary>
        public string textToCal1 = "";
        public string textToCal2 = "";

        /// <summary>
        /// Whether to open the window by shaking the device (mobile-only).
        /// </summary>
        public bool shakeToOpen = true;

        /// <summary>
        /// The (squared) acceleration above which the window should open.
        /// </summary>
        public float shakeAcceleration = 3f;

        /// <summary>
        /// Whether to only keep a certain number of logs.
        ///
        /// Setting this can be helpful if memory usage is a concern.
        /// </summary>
        public bool restrictLogCount = false;

        /// <summary>
        /// Number of logs to keep before removing old ones.
        /// </summary>
        public int maxLogs = 1000;

        #endregion

        readonly List<Log> logs = new List<Log>();
        Vector2 scrollPosition;
        bool visible;
        bool collapse;

        public TextAsset luaScript;
        public Injection[] injections;

        internal static LuaEnv luaEnv = new LuaEnv(); //all lua behaviour shared one luaenv only!
        internal static float lastGCTime = 0;
        internal const float GCInterval = 1;//1 second 

        /// <summary>
        /// Delegate a new Action for two input paragrams
        /// </summary>
        /// <param name="str1">input string1 </param>
        /// <param name="str2">input string2 </param>
        [CSharpCallLua]
        public delegate List<SnapMsg> String2Paragram(string str1, string str2);

        /// <summary>
        /// Delegate a new Action for one input paragrams
        /// </summary>
        /// <param name="str1"></param>
        /// <returns></returns>
        [CSharpCallLua]
        public delegate List<SnapMsg> stringParam(string str1);

        /// <summary>
        /// Delegate a new Action for none input paragrams
        /// </summary>
        /// <returns></returns>
        [CSharpCallLua]
        public delegate string voidParam();

        /// <summary>
        /// class to output lua memory message
        /// </summary>
        [CSharpCallLua]
        public class SnapMsg
        {
            public string name;
            public string size;
            public string type;
            public string id;
            public string info;
        }
        private List<SnapMsg> snapMsgs = new List<SnapMsg>();

        //Set Origin Actions
        private Action luaStart;
        private Action luaUpdate;
        private Action luaOnDestroy;
        private String2Paragram luaFilter;
        private stringParam luaTakeSnap;
        private String2Paragram luaCalculSnap;
        private voidParam luaMemoryTotal;

        private LuaTable scriptEnv;

        // Visual elements:

        /// <summary>
        /// Set readonly color type
        /// </summary>
        static readonly Dictionary<LogType, Color> logTypeColors = new Dictionary<LogType, Color>
        {
            { LogType.Assert, Color.white },
            { LogType.Error, Color.red },
            { LogType.Exception, Color.red },
            { LogType.Log, Color.white },
            { LogType.Warning, Color.yellow },
        };

        const string windowTitle = "Console";
        const int margin = 20;

        //Original readonly GUIContent
        static readonly GUIContent TextContent = new GUIContent("Text", "Text Input.");
        static readonly GUIContent filterContent = new GUIContent("Filter", "Filter the content.");
        static readonly GUIContent takeSnap = new GUIContent("TakeSnap", "Take the snapshot content.");
        static readonly GUIContent CalculSnap = new GUIContent("CalculSnap", "Calculate the snapshots.");
        static readonly GUIContent collapseLabel = new GUIContent("Collapse", "Hide repeated messages.");
        static readonly GUIContent clearLabel = new GUIContent("Clear", "Clear the content of the log.");

        // windowRect size 
        readonly Rect titleBarRect = new Rect(0, 0, 10000, 20);
        Rect windowRect = new Rect(margin * 2, margin * 3, Screen.width - (margin * 4), Screen.height - (margin * 6));

        void Awake()
        {
            scriptEnv = luaEnv.NewTable();

            // 为每个脚本设置一个独立的环境，可一定程度上防止脚本间全局变量、函数冲突
            LuaTable meta = luaEnv.NewTable();
            meta.Set("__index", luaEnv.Global);
            scriptEnv.SetMetaTable(meta);
            meta.Dispose();

            scriptEnv.Set("self", this);
            foreach (var injection in injections)
            {
                scriptEnv.Set(injection.name, injection.value);
            }

            luaEnv.DoString(luaScript.text, "MemoryUI", scriptEnv);

            //Register Actions
            Action luaAwake = scriptEnv.Get<Action>("awake");
            scriptEnv.Get("start", out luaStart);
            scriptEnv.Get("update", out luaUpdate);
            scriptEnv.Get("ondestroy", out luaOnDestroy);
            scriptEnv.Get("filterstr", out luaFilter);
            scriptEnv.Get("takesnap", out luaTakeSnap);
            scriptEnv.Get("calculation", out luaCalculSnap);
            scriptEnv.Get("total", out luaMemoryTotal);

            if (luaAwake != null)
            {
                luaAwake();
            }
        }

        /*  Add Debug.log to Handlelog 
        /// <summary>
        /// Get Debug.log message 
        /// </summary>
        void OnEnable()
        {
#if UNITY_5
			Application.logMessageReceived += HandleLog;
#else
            Application.RegisterLogCallback(HandleLog);
#endif
        }

        void OnDisable()
        {
#if UNITY_5
			Application.logMessageReceived -= HandleLog;
#else
            Application.RegisterLogCallback(null);
#endif
        }
        */

        void Start()
        {

        }

        void Update()
        {
            if (luaUpdate != null)
            {
                luaUpdate();
            }
            if (Time.time - LuaBehaviour.lastGCTime > GCInterval)
            {
                luaEnv.Tick();
                LuaBehaviour.lastGCTime = Time.time;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                visible = !visible;
                windowRect = new Rect(margin * 2, margin * 3, Screen.width - (margin * 4), Screen.height - (margin * 6));
            }

            if (shakeToOpen && Input.acceleration.sqrMagnitude > shakeAcceleration)
            {
                visible = true;
            }
        }

        /// <summary>
        /// OnGUIDestory
        /// </summary>
        void OnDestroy()
        {
            if (luaOnDestroy != null)
            {
                luaOnDestroy();
            }
            luaOnDestroy = null;
            luaUpdate = null;
            luaStart = null;
            scriptEnv.Dispose();
            injections = null;
        }

        /// <summary>
        /// On GUI Start 
        /// </summary>
        void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            windowRect = GUILayout.Window(1, windowRect, DrawConsoleWindow, windowTitle);
        }

        /// <summary>
        /// Displays a window that lists the recorded logs.
        /// </summary>
        /// <param name="windowID">Window ID.</param>
        void DrawConsoleWindow(int windowID)
        {
            DrawLogsList();
            DrawToolbar();

            // Allow the window to be dragged by its title bar.
            GUI.DragWindow(titleBarRect);
        }

        /// <summary>
        /// Displays a scrollable list of logs.
        /// </summary>
        void DrawLogsList()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            // Iterate through the recorded logs.
            //for (var i = 0; i < logs.Count; i++)
            //{
            //    var log = logs[i];

            //    // Combine identical messages if collapse option is chosen.
            //    if (collapse && i > 0)
            //    {
            //        var previousMessage = logs[i - 1].message;

            //        if (log.message == previousMessage)
            //        {
            //            continue;
            //        }
            //    }

            //    GUI.contentColor = logTypeColors[log.type];
            //    GUILayout.Label(log.message);
            //}

            LabelPrint();

            GUILayout.EndScrollView();

            // Ensure GUI colour is reset before drawing other components.
            GUI.contentColor = Color.white;
        }

        /// <summary>
        /// Displays options for filtering and changing the logs list.
        /// </summary>
        void DrawToolbar()
        {
            //Draw the components on the GUI
            GUILayout.BeginHorizontal();
            textToFilter = GUILayout.TextField(textToFilter, 50);
            if (GUILayout.Button(filterContent))
            {
                snapMsgs = luaFilter(textToSnap, textToFilter);
                if (collapse)
                {
                    DistinctList();
                }
            }
            if (GUILayout.Button(clearLabel))
            {
                preMesList.Clear();
                snapMsgs.Clear();
            }
            collapse = GUILayout.Toggle(collapse, collapseLabel, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("the snap name", GUILayout.Width(90));
            textToSnap = GUILayout.TextField(textToSnap, 30);
            if (GUILayout.Button(takeSnap))
            {
                AddTitle();
                snapMsgs = luaTakeSnap(textToSnap);
                if (collapse)
                {
                    DistinctList();
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("snap1", GUILayout.Width(40));
            textToCal1 = GUILayout.TextField(textToCal1, 30);
            GUILayout.Label("snap2", GUILayout.Width(40));
            textToCal2 = GUILayout.TextField(textToCal2, 30);
            if (GUILayout.Button(CalculSnap))
            {
                preMesList.Clear();
                snapMsgs = luaCalculSnap(textToCal1, textToCal2);
                if (collapse)
                {
                    DistinctList();
                }
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Records a log from the log callback.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="stackTrace">Trace of where the message came from.</param>
        /// <param name="type">Type of message (error, exception, warning, assert).</param>
        void HandleLog(string message, string stackTrace, LogType type)
        {
            logs.Add(new Log
            {
                message = message,
                stackTrace = stackTrace,
                type = type,
            });

            TrimExcessLogs();
        }

        /// <summary>
        /// Removes old logs that exceed the maximum number allowed.
        /// </summary>
        void TrimExcessLogs()
        {
            if (!restrictLogCount)
            {
                return;
            }

            var amountToRemove = Mathf.Max(logs.Count - maxLogs, 0);

            if (amountToRemove == 0)
            {
                return;
            }

            logs.RemoveRange(0, amountToRemove);
        }

        /// <summary>
        /// print UI label
        /// </summary>
        void LabelPrint()
        {
            if (preMesList != null && preMesList.Count > 0)
            {
                for (int i = 0; i < preMesList.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(preMesList[i].title, GUILayout.Width(windowRect.width * 0.4f));
                    GUILayout.Label(preMesList[i].message, GUILayout.Width(windowRect.width * 0.5f));
                    GUILayout.EndHorizontal();
                }
            }
            if (snapMsgs != null && snapMsgs.Count > 0)
            {
                float win = windowRect.width * 0.95f;
                //{0,20}:{1,10}:{2,15}:{3,25}:{4,30}
                float w1 = win * 0.2f; var w2 = win * 0.1f; var w3 = win * 0.15f; var w4 = win * 0.25f; var w5 = win * 0.3f;
                for (int i = 0; i < snapMsgs.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(snapMsgs[i].name, GUILayout.Width(w1));
                    GUILayout.Label(snapMsgs[i].size, GUILayout.Width(w2));
                    GUILayout.Label(snapMsgs[i].type, GUILayout.Width(w3));
                    GUILayout.Label(snapMsgs[i].id, GUILayout.Width(w4));
                    GUILayout.Label(snapMsgs[i].info, GUILayout.Width(w5));
                    GUILayout.EndHorizontal();
                }
            }
        }

        /// <summary>
        /// add first two lines message
        /// </summary>
        void AddTitle()
        {
            if (preMesList != null)
            {
                if (preMesList.Count == 0)
                {
                    preMesList.Add(new PreMes("snapshot key: ", textToSnap));
                    preMesList.Add(new PreMes("total memory: ", luaMemoryTotal()));
                }
                if (preMesList.Count == 2)
                {
                    preMesList[0].title = "snapshot key: ";
                    preMesList[0].message = textToSnap;

                    preMesList[1].title = "total memory: ";
                    preMesList[1].message = luaMemoryTotal();
                }
            }
        }

        /// <summary>
        /// distinct label message list 
        /// </summary>
        void DistinctList()
        {
            //List<SnapMsg> noRepeat = new List<SnapMsg>();
            //bool flag = true;
            //snapMsgs.ForEach((msg1) =>
            //{
            //    flag = true;
            //    noRepeat.ForEach((msg2) =>
            //    {
            //        if (msg1 != msg2 && msg1.name != msg2.name)
            //        {
            //            flag = false;
            //            noRepeat.Add(msg1);
            //        }
            //        else if (msg1 != msg2 && msg1.name == msg2.name)
            //        {
            //            flag = false;
            //        }
            //    });
            //    if (flag)
            //    {
            //        noRepeat.Add(msg1);
            //    }
            //});
            //snapMsgs = noRepeat;
            int checkState = 0;
            for (int i = 0; i < snapMsgs.Count; i++)
            {
                checkState = 0;
                for (int j = 0; j < snapMsgs.Count; j++)
                {
                    if (snapMsgs[i].name == snapMsgs[j].name)
                    {
                        checkState += 1;
                    }
                }
                if (checkState >= 2)
                {
                    snapMsgs.Remove(snapMsgs[i]);
                }
            }
        }
    }
}

