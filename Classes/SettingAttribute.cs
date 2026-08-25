using System;

namespace ShibaGTGenesisReborn.Classes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SettingAttribute : Attribute
    {
        public string Key { get; }

        public SettingAttribute(string key = null)
        {
            Key = key;
        }
    }
}
