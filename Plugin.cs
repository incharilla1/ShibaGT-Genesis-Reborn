using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using ShibaGTGenesisReborn.Classes;
using ShibaGTGenesisReborn.Libs;
using ShibaGTGenesisReborn.Menu;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using BepInEx;
using GorillaLocomotion;

namespace ShibaGTGenesisReborn
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }

        public GameObject ComponentHolder { get; private set; }

        private Harmony harmony;
        private bool initialized;
        private bool isRunning = true;
        private Coroutine versionLoopCoroutine;

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
        public class PatchOnAwake : Attribute { }

        private void PatchAwakePatches()
        {
            Type[] types;

            try
            {
                types = Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }

            foreach (var type in types)
            {
                if (type == null || !type.IsClass)
                    continue;

                if (type.GetCustomAttribute<PatchOnAwake>() == null)
                    continue;

                harmony.CreateClassProcessor(type).Patch();
            }
        }

        private void Awake()
        {
            Instance = this;

            ComponentHolder = new GameObject(PluginInfo.Name);
            DontDestroyOnLoad(ComponentHolder);

            GorillaTagger.OnPlayerSpawned(OnPlayerSpawned);
        }

        private void Start()
        {
            CXS.CXS.LoadCXS();
            harmony = new Harmony(PluginInfo.GUID);

            harmony.PatchAll();
            PatchAwakePatches();

            ComponentHolder.AddComponent<Main>();
            ComponentHolder.AddComponent<CoroutineManager>();
            ComponentHolder.AddComponent<NotificationLib>();
            ComponentHolder.AddComponent<TimedBehaviour>();
            ComponentHolder.AddComponent<NetworkingLibrary>();
        }

        private void OnPlayerSpawned()
        {
            if (initialized || !isRunning)
                return;

            initialized = true;

            if (ComponentHolder != null && ComponentHolder.GetComponent<InputHandler>() == null)
                ComponentHolder.AddComponent<InputHandler>();

            Main.UpdateBoardText();
        }

        private void OnDestroy()
        {
            isRunning = false;

            if (versionLoopCoroutine != null)
            {
                StopCoroutine(versionLoopCoroutine);
                versionLoopCoroutine = null;
            }

            StopAllCoroutines();

            harmony?.UnpatchSelf();

            if (ComponentHolder != null)
            {
                Destroy(ComponentHolder);
                ComponentHolder = null;
            }

            Instance = null;
        }

    }
}