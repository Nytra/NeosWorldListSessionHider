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
        public override string Name => "WorldListSessionHider";
        public override string Author => "Nytra";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/Nytra/NeosWorldListFilter";
        public static ModConfiguration Config;

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> MOD_ENABLED = new ModConfigurationKey<bool>("MOD_ENABLED", "Enable hiding sessions:", () => false);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<string> HOST_USERIDS = new ModConfigurationKey<string>("HOST_USERIDS", "Host User IDs:", () => "");
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<string> HOST_USERNAMES = new ModConfigurationKey<string>("HOST_USERNAMES", "Host Usernames:", () => "");
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<string> SESSION_IDS = new ModConfigurationKey<string>("SESSION_IDS", "Session IDs:", () => "");
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<string> UNIVERSE_IDS = new ModConfigurationKey<string>("UNIVERSE_IDS", "Universe IDs:", () => "");
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<dummy> DUMMY_1 = new ModConfigurationKey<dummy>("DUMMY_1", "<i><color=gray>All of these can be comma-separated to store multiple values.</color></i>", () => new dummy());

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("owo.Nytra.WorldListSessionHider");
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

                SlimList<SessionInfo> sessionsList = (SlimList<SessionInfo>)AccessTools.Field(itemType, "sessions").GetValue(item);
                string itemId = (string)AccessTools.Field(itemType, "id").GetValue(item);

                foreach (SessionInfo info in sessionsList)
                {
                    string hostUserIdsRaw = Config.GetValue(HOST_USERIDS);
                    string hostUsernamesRaw = Config.GetValue(HOST_USERNAMES);
                    string sessionIdsRaw = Config.GetValue(SESSION_IDS);
                    string universeIdsRaw = Config.GetValue(UNIVERSE_IDS);

                    string[] hostUserIdParts = hostUserIdsRaw.Split(',');
                    string[] hostUsernameParts = hostUsernamesRaw.Split(',');
                    string[] sessionIdParts = sessionIdsRaw.Split(',');
                    string[] universeIdParts = universeIdsRaw.Split(',');

                    bool flag = false;
                    if (!flag && info.HasEnded)
                    {
                        flag = true;
                        Debug("Session has ended.");
                    }
                    if (hostUserIdsRaw.Length > 0 && hostUserIdParts.Contains(info.HostUserId))
                    {
                        flag = true;
                        Debug("UserID Hit!");
                    }
                    if (hostUsernamesRaw.Length > 0 && hostUsernameParts.Contains(info.HostUsername))
                    {
                        flag = true;
                        Debug("Username Hit!");
                    }
                    if (sessionIdsRaw.Length > 0 && sessionIdParts.Contains(info.SessionId))
                    {
                        flag = true;
                        Debug("SessionID Hit!");
                    }
                    if (universeIdsRaw.Length > 0 && universeIdParts.Contains(info.UniverseId))
                    {
                        flag = true;
                        Debug("UniverseID Hit!");
                    }

                    if (flag)
                    {
                        Debug("Detected! Hiding this item.");
                        Debug($"Item Id: {itemId}");
                        Debug($"Session Name: {info.Name}");
                        Debug($"Host UserID: {info.HostUserId}");
                        Debug($"Host UserName: {info.HostUsername}");
                        Debug($"SessionID: {info.SessionId}");
                        Debug($"UniverseID: {info.UniverseId}");
                        __result = false;
                        return false;
                    }
                }
                return true;
            }
        }
    }
}