using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using CloudX.Shared;
using BaseX;
using System.Collections;

namespace ModNameGoesHere
{
    public class ModNameGoesHere : NeosMod
    {
        public override string Name => "WorldListFilter";
        public override string Author => "Nytra";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/Nytra/NeosWorldListFilter";
        public static ModConfiguration Config;

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> MOD_ENABLED = new ModConfigurationKey<bool>("MOD_ENABLED", "Mod enabled:", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<string> HOST_USERIDS = new ModConfigurationKey<string>("HOST_USERIDS", "Host User-IDs to filter (Comma separated):", () => "");
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<string> HOST_USERNAMES = new ModConfigurationKey<string>("HOST_USERNAMES", "Host Usernames to filter (Comma separated):", () => "");
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<string> SESSION_IDS = new ModConfigurationKey<string>("SESSION_IDS", "Session IDs to filter (Comma separated):", () => "");

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("owo.Nytra.WorldListFilter");
            Config = GetConfiguration();
            harmony.PatchAll();
        }

        private static Type itemType = AccessTools.TypeByName("FrooxEngine.WorldListManager+Item");

        [HarmonyPatch(typeof(WorldListManager), "Filter")]
        class ModNameGoesHerePatch
        {
            public static bool Prefix(ref bool __result, object item)
            {
                if (!Config.GetValue(MOD_ENABLED)) return true;

                object sessionsObj = AccessTools.Field(itemType, "sessions").GetValue(item);
                SlimList<SessionInfo> sessionsList = (SlimList<SessionInfo>)sessionsObj;
                foreach (SessionInfo info in sessionsList)
                {
                    Msg($"Session Name: {info.Name}");
                    Msg($"\t\tHost User-ID: {info.HostUserId}");
                    Msg($"\t\tHost UserName: {info.HostUsername}");

                    string host_ids = Config.GetValue(HOST_USERIDS);
                    string host_names = Config.GetValue(HOST_USERNAMES);
                    string session_ids = Config.GetValue(SESSION_IDS);

                    string[] idparts = host_ids.Split(',');
                    string[] nameparts = host_names.Split(',');
                    string[] sessionidparts = session_ids.Split(',');

                    bool flag = false;
                    if (idparts.Length > 0)
                    {
                        if (idparts.Contains(info.HostUserId))
                        {
                            flag = true;
                            goto Finish;
                        }
                    }
                    else
                    {
                        if (host_ids != null && host_ids.Length > 0 && host_ids == info.HostUserId)
                        {
                            flag = true;
                            goto Finish;
                        }
                    }
                    if (nameparts.Length > 0)
                    {
                        if (nameparts.Contains(info.HostUsername))
                        {
                            flag = true;
                            goto Finish;
                        }
                    }
                    else
                    {
                        if (host_names != null && host_names.Length > 0 && host_names == info.HostUsername)
                        {
                            flag = true;
                            goto Finish;
                        }
                    }
                    if (sessionidparts.Length > 0)
                    {
                        if (sessionidparts.Contains(info.SessionId))
                        {
                            flag = true;
                            goto Finish;
                        }
                    }
                    else
                    {
                        if (session_ids != null && session_ids.Length > 0 && session_ids == info.SessionId)
                        {
                            flag = true;
                            goto Finish;
                        }
                    }
                    
                    Finish:
                    if (flag)
                    {
                        Msg("\t\t\t\tDetected! Filtering this item.");
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }
    }
}