using System;
using UnityEngine;

namespace Loading
{
    public class Loader
    {
        public static void Load()
        {
            GameObject go = new GameObject("Load");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ShibaGTGenesisReborn.Plugin>();
        }
    }
}
