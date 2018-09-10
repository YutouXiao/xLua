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
    class TestProfiler : MonoBehaviour
    {

        public TextAsset luaScript;

        internal static LuaEnv luaEnv = new LuaEnv(); //all lua behaviour shared one luaenv only!


        //Set Origin Actions
        private Action luaTakeSnap;
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

            luaEnv.DoString(luaScript.text, "TestProfiler", scriptEnv);

            //Register Actions
            Action luaAwake = scriptEnv.Get<Action>("awake");
            scriptEnv.Get("report", out luaTakeSnap);

            if (luaAwake != null)
            {
                luaAwake();
            }
        }

        void Start()
        {
            luaTakeSnap();
        }

        void Update()
        {

        }
    }
}

