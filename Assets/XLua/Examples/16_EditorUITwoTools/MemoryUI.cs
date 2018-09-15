using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace LuaMonitor
{
    /// <summary>
    /// A console to display Unity's debug logs in-game.
    /// </summary>
    [LuaCallCSharp]
    class MemoryUI : MonoBehaviour
    {
        public static MemoryUI _instance;

        public static MemoryUI Instance
        {
            get
            {
                return _instance;
            }
        }

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

        public TextAsset luaScript;

        internal static LuaEnv luaEnv = new LuaEnv(); //all lua behaviour shared one luaenv only!

        /// <summary>
        /// Delegate a new Action for two input paragrams
        /// </summary>
        /// <param name="str1">input string1 </param>
        /// <param name="str2">input string2 </param>
        [CSharpCallLua]
        public delegate List<LuaSnapshotData> String2Paragram(string str1, string str2);

        /// <summary>
        /// Delegate a new Action for one input paragrams
        /// </summary>
        /// <param name="str1"></param>
        /// <returns></returns>
        [CSharpCallLua]
        public delegate List<LuaSnapshotData> stringParam(string str1);

        /// <summary>
        /// Delegate a new Action for none input paragrams
        /// </summary>
        /// <returns></returns>
        [CSharpCallLua]
        public delegate string voidParam();

        //Set Origin Actions
        public Action luaStart;
        public Action luaUpdate;
        public Action luaOnDestroy;
        public static String2Paragram luaFilter;
        public static stringParam luaTakeSnap;
        public static String2Paragram luaCalculSnap;
        public static voidParam luaMemoryTotal;

        private LuaTable scriptEnv;

        void Awake()
        {
            scriptEnv = luaEnv.NewTable();

            // 为每个脚本设置一个独立的环境，可一定程度上防止脚本间全局变量、函数冲突
            LuaTable meta = luaEnv.NewTable();
            meta.Set("__index", luaEnv.Global);
            scriptEnv.SetMetaTable(meta);
            meta.Dispose();

            scriptEnv.Set("self", this);
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
        
        void Start()
        {
            _instance = this;
        }

        void Update()
        {
        }

        /// <summary>
        /// OnGUIDestory
        /// </summary>
        void OnDestroy()
        { 
        }
    }
}

