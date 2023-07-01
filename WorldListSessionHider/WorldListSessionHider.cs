using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using CloudX.Shared;
using BaseX;
using FrooxEngine.UIX;

namespace WorldListSessionHider
{
	public class WorldListSessionHider : NeosMod
	{
		public override string Name => "WorldListSessionHider";
		public override string Author => "Nytra";
		public override string Version => "1.2.0";
		public override string Link => "https://github.com/Nytra/NeosWorldListSessionHider";
		public static ModConfiguration Config;

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> MOD_ENABLED = new ModConfigurationKey<bool>("MOD_ENABLED", "Mod Enabled:", () => true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_0 = new ModConfigurationKey<dummy>("DUMMY_0", "<size=0></size>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> HOST_USERIDS = new ModConfigurationKey<string>("HOST_USERIDS", "Host User IDs to hide:", () => "");
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> HOST_USERNAMES = new ModConfigurationKey<string>("HOST_USERNAMES", "Host Usernames to hide:", () => "");
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> SESSION_IDS = new ModConfigurationKey<string>("SESSION_IDS", "Session IDs to hide:", () => "");
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_STRING_MATCHED_SESSIONS_COMPLETELY = new ModConfigurationKey<bool>("HIDE_STRING_MATCHED_SESSIONS_COMPLETELY", "Hide matching sessions completely:", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_1 = new ModConfigurationKey<dummy>("DUMMY_1", "<i><color=gray>All of these can be comma-separated to store multiple values.</color></i>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_2 = new ModConfigurationKey<dummy>("DUMMY_2", "<i><color=gray>e.g: U-Cheese,U-spaghet,U-OwO</color></i>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_3 = new ModConfigurationKey<dummy>("DUMMY_3", "<size=0></size>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_DEAD_SESSIONS = new ModConfigurationKey<bool>("HIDE_DEAD_SESSIONS", "Hide dead sessions (experimental):", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<int> DEAD_SESSION_LAST_UPDATE_MINUTES = new ModConfigurationKey<int>("DEAD_SESSION_LAST_UPDATE_MINUTES", "If session has not updated for this number of minutes, consider it possibly dead:", () => 30, valueValidator: v => v > 0);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_DEAD_SESSIONS_COMPLETELY = new ModConfigurationKey<bool>("HIDE_DEAD_SESSIONS_COMPLETELY", "Hide dead sessions completely:", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_4 = new ModConfigurationKey<dummy>("DUMMY_4", "<i><color=gray>A dead session is one that has not been updated recently and you have a pending contact request from it.</color></i>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_5 = new ModConfigurationKey<dummy>("DUMMY_5", "<i><color=gray>In rare cases this could give false-positives.</color></i>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_6 = new ModConfigurationKey<dummy>("DUMMY_6", "<size=0></size>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_EXPIRED_SESSIONS = new ModConfigurationKey<bool>("HIDE_EXPIRED_SESSIONS", "Hide expired sessions:", () => true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<int> LAST_UPDATE_MAX_DAYS = new ModConfigurationKey<int>("LAST_UPDATE_MAX_DAYS", "If session has not updated for this number of days, consider it expired:", () => 90, valueValidator: v => v > 0);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_EXPIRED_SESSIONS_COMPLETELY = new ModConfigurationKey<bool>("HIDE_EXPIRED_SESSIONS_COMPLETELY", "Hide expired sessions completely:", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_7 = new ModConfigurationKey<dummy>("DUMMY_7", "<i><color=gray>Meant to hide sessions that have not updated in a very long time.</color></i>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_ENDED_SESSIONS = new ModConfigurationKey<bool>("HIDE_ENDED_SESSIONS", "Hide ended sessions (Sessions without URLs):", () => false, internalAccessOnly: true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_ENDED_SESSIONS_COMPLETELY = new ModConfigurationKey<bool>("HIDE_ENDED_SESSIONS_COMPLETELY", "Hide ended sessions completely:", () => false, internalAccessOnly: true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> EXTRA_LOGGING = new ModConfigurationKey<bool>("EXTRA_LOGGING", "Enable extra debug logging:", () => false, internalAccessOnly: true);

		//private const string SUBSCRIBED_TAG = "WorldListSessionHider.Subscribed";

		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("owo.Nytra.WorldListSessionHider");
			Config = GetConfiguration();
			Config.OnThisConfigurationChanged += (configChangedEvent) => 
			{
				if (configChangedEvent.Key == EXTRA_LOGGING && Config.GetValue(EXTRA_LOGGING))
				{
					Debug("Logging configured strings...");
					foreach (string s in Config.GetValue(HOST_USERIDS).Split(','))
					{
						Debug($"UserID: \"{s}\"");
					}
					foreach (string s in Config.GetValue(HOST_USERNAMES).Split(','))
					{
						Debug($"Username: \"{s}\"");
					}
					foreach (string s in Config.GetValue(SESSION_IDS).Split(','))
					{
						Debug($"SessionID: \"{s}\"");
					}
					Debug("Done.");
				}
			};
			harmony.PatchAll();
		}

		private static Type theType = typeof(WorldThumbnailItem);
		private static MethodInfo getBestSessionMethod = AccessTools.Method(theType, "GetBestSession");
		private static MethodInfo updateThumbnailMethod = AccessTools.Method(theType, "UpdateThumbnailURL");
		private static FieldInfo nameTextField = AccessTools.Field(theType, "_nameText");
		private static FieldInfo detailTextField = AccessTools.Field(theType, "_detailText");
		private static FieldInfo counterTextField = AccessTools.Field(theType, "_counterText");
		private static FieldInfo deferredThumbnailField = AccessTools.Field(theType, "_deferredThumbnailUrl");
		//private static FieldInfo thumbnailTextureField = AccessTools.Field(theType, "_thumbnailTexture");

		private static bool ReceivedContactRequestInSession(SessionInfo sessionInfo)
		{
			foreach (SessionUser user in sessionInfo.SessionUsers)
			{
				Friend friend = Engine.Current.Cloud.Friends.FindFriend((Friend f) => f.FriendUserId == user.UserID && f.FriendStatus == FriendStatus.Requested);
				if (friend != null) return true;
			}
			return false;
		}

		private static void CheckSession(WorldThumbnailItem worldThumbnailItem, string debugString = "")
		{
			SessionInfo sessionInfo = Engine.Current.WorldAnnouncer.GetInfoForSessionId(worldThumbnailItem.WorldOrSessionId);

			if (sessionInfo == null)
			{
				List<SessionInfo> sessions = new List<SessionInfo>();
				Engine.Current.WorldAnnouncer.GetSessionsForWorldId(worldThumbnailItem.WorldOrSessionId, sessions);
				sessionInfo = (SessionInfo)getBestSessionMethod.Invoke(worldThumbnailItem, new object[] { sessions });
				if (sessionInfo == null) return;
			}

			if (Config.GetValue(EXTRA_LOGGING))
			{
				Debug(new string('=', 30));
				if (debugString.Length > 0) Debug(debugString);
				Debug($"Host UserID: \"{sessionInfo.HostUserId}\" Host Username: \"{sessionInfo.HostUsername}\" SessionID: \"{sessionInfo.SessionId}\"");
				foreach(string url in sessionInfo.SessionURLs)
				{
					Debug($"URL: {url}");
				}
				Debug($"LastUpdate: {sessionInfo.LastUpdate}");
			}

			// Don't hide sessions that the LocalUser is currently in
			if (Engine.Current.WorldManager.Worlds.Any((World w) => w.SessionId == sessionInfo.SessionId)) return;

			if (Config.GetValue(HIDE_ENDED_SESSIONS) && sessionInfo.HasEnded)
			{
                if (Config.GetValue(EXTRA_LOGGING)) Debug("Found ended session (Session without any URLs).");
				Debug("Hiding session: " + sessionInfo.Name);
				Hide(worldThumbnailItem, nameTextValue: "<i>[ENDED]</i>", hideCompletely: Config.GetValue(HIDE_ENDED_SESSIONS_COMPLETELY));
			}
			else if (Config.GetValue(HIDE_EXPIRED_SESSIONS) && DateTime.UtcNow.Subtract(sessionInfo.LastUpdate).TotalDays > Config.GetValue(LAST_UPDATE_MAX_DAYS))
			{
                if (Config.GetValue(EXTRA_LOGGING)) Debug("Session LastUpdate time exceeded max days.");
				Debug("Hiding session: " + sessionInfo.Name);
				Hide(worldThumbnailItem, nameTextValue: "<i>[EXPIRED]</i>", hideCompletely: Config.GetValue(HIDE_EXPIRED_SESSIONS_COMPLETELY));
			}
			else if (Config.GetValue(SESSION_IDS).Split(',').Contains(sessionInfo.SessionId) ||
					Config.GetValue(HOST_USERIDS).Split(',').Contains(sessionInfo.HostUserId) ||
					Config.GetValue(HOST_USERNAMES).Split(',').Contains(sessionInfo.HostUsername))
			{
                if (Config.GetValue(EXTRA_LOGGING)) Debug("Session string matched config.");
				Debug("Hiding session: " + sessionInfo.Name);
				Hide(worldThumbnailItem, nameTextValue: "<i>[HIDDEN]</i>", hideCompletely: Config.GetValue(HIDE_STRING_MATCHED_SESSIONS_COMPLETELY));
			}
			else if (Config.GetValue(HIDE_DEAD_SESSIONS) && DateTime.UtcNow.Subtract(sessionInfo.LastUpdate).TotalMinutes > Config.GetValue(DEAD_SESSION_LAST_UPDATE_MINUTES) && ReceivedContactRequestInSession(sessionInfo))
			{
                if (Config.GetValue(EXTRA_LOGGING)) Debug("Session LastUpdate time not recent enough AND received a contact request in the session. Session is likely dead.");
				Debug("Hiding session: " + sessionInfo.Name);
				Hide(worldThumbnailItem, nameTextValue: "<i>[DEAD]</i>", hideCompletely: Config.GetValue(HIDE_DEAD_SESSIONS_COMPLETELY));

				//Debug("Attempting to get info from API...");
				//var task = Engine.Current.Cloud.GetSession(sessionInfo.SessionId);
				//CloudResult<SessionInfo> result = await task;
				//Debug($"CloudResult: {result}");
			}
			
		}

		private static void Hide(WorldThumbnailItem worldThumbnailItem, string nameTextValue = "<i>[HIDDEN]</i>", bool hideCompletely = false)
		{
			updateThumbnailMethod.Invoke(worldThumbnailItem, new object[] { NeosAssets.Skyboxes.Thumbnails.NoThumbnail });
			var nameText = (SyncRef<Text>)nameTextField.GetValue(worldThumbnailItem);
			nameText.Target.Content.Value = nameTextValue;
			var detailText = (SyncRef<Text>)detailTextField.GetValue(worldThumbnailItem);
			detailText.Target.Content.Value = "<i>...</i>";
			var counterText = (SyncRef<Text>)counterTextField.GetValue(worldThumbnailItem);
			counterText.Target.Content.Value = "<i>...</i>";
			deferredThumbnailField.SetValue(worldThumbnailItem, NeosAssets.Skyboxes.Thumbnails.NoThumbnail);

			if (hideCompletely)
			{
				worldThumbnailItem.Slot.ActiveSelf = false;
			}
		}

		[HarmonyPatch(typeof(WorldThumbnailItem), "UpdateInfo")]
		class WorldListSessionHiderPatch
		{
			public static void Postfix(WorldThumbnailItem __instance, FrooxEngine.Record record, IReadOnlyList<SessionInfo> sessions, IReadOnlyList<World> openedWorlds)
			{
				if (!Config.GetValue(MOD_ENABLED)) return;

				//if (__instance.Slot.Tag == SUBSCRIBED_TAG)
				//{
				//	CheckSession(__instance, "From Patch1 early return");
				//	return;
				//}

				//if (Config.GetValue(EXTRA_LOGGING))
				//{
				//	Debug($"Subcribing to {__instance.Name} {__instance.ReferenceID}.");
				//}

				//__instance.Slot.Tag = SUBSCRIBED_TAG;

				//__instance.WorldOrSessionId.Changed += (iChangeable) =>
				//{
				//	CheckSession(__instance, "From Changed Event");
				//};

				CheckSession(__instance, "Called from UpdateInfo");
			}
		}

		[HarmonyPatch(typeof(WorldThumbnailItem), "OnActivated")]
		class WorldListSessionHiderPatch2
		{
			public static void Postfix(WorldThumbnailItem __instance)
			{
				if (!Config.GetValue(MOD_ENABLED)) return;

				__instance.RunSynchronously(delegate { CheckSession(__instance, "Called from OnActivated"); });
			}
		}
	}
}