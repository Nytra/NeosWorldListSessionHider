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
		public override string Version => "1.2.3";
		public override string Link => "https://github.com/Nytra/NeosWorldListSessionHider";
		public static ModConfiguration Config;

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> MOD_ENABLED = new ModConfigurationKey<bool>("MOD_ENABLED", "Enable hiding sessions:", () => true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_0 = new ModConfigurationKey<dummy>("DUMMY_0", "<size=0></size>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_0_1 = new ModConfigurationKey<dummy>("DUMMY_0_1", "<color=green>[FILTERS]</color>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_FILTERED_SESSIONS = new ModConfigurationKey<bool>("HIDE_FILTERED_SESSIONS", "Use filters:", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_FILTERED_SESSIONS_COMPLETELY = new ModConfigurationKey<bool>("HIDE_FILTERED_SESSIONS_COMPLETELY", "Hide filtered sessions completely:", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> HOST_USERIDS = new ModConfigurationKey<string>("HOST_USERIDS", "Host User IDs:", () => "");
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> HOST_USERNAMES = new ModConfigurationKey<string>("HOST_USERNAMES", "Host Usernames:", () => "");
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> SESSION_IDS = new ModConfigurationKey<string>("SESSION_IDS", "Session IDs:", () => "");
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_1 = new ModConfigurationKey<dummy>("DUMMY_1", "<i><color=gray>All of these can be comma-separated to store multiple values.</color></i>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_2 = new ModConfigurationKey<dummy>("DUMMY_2", "<i><color=gray>e.g: U-Cheese,U-spaghet,U-OwO</color></i>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_3 = new ModConfigurationKey<dummy>("DUMMY_3", "<size=0></size>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_3_1 = new ModConfigurationKey<dummy>("DUMMY_3_1", "<color=green>[STUCK SESSIONS]</color>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_DEAD_SESSIONS = new ModConfigurationKey<bool>("HIDE_DEAD_SESSIONS", "Hide stuck sessions:", () => true);
		//[AutoRegisterConfigKey]
		//private static ModConfigurationKey<int> DEAD_SESSION_LAST_UPDATE_HOURS = new ModConfigurationKey<int>("DEAD_SESSION_LAST_UPDATE_HOURS", "If session has not updated for this number of hours, consider it possibly stuck:", () => 24, valueValidator: v => v > 0);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_DEAD_SESSIONS_COMPLETELY = new ModConfigurationKey<bool>("HIDE_DEAD_SESSIONS_COMPLETELY", "Hide stuck sessions completely:", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_4 = new ModConfigurationKey<dummy>("DUMMY_4", "<i><color=gray>A stuck session is one that shows up in the world list but doesn't actually exist.</color></i>", () => new dummy());
		//[AutoRegisterConfigKey]
		//private static ModConfigurationKey<dummy> DUMMY_6 = new ModConfigurationKey<dummy>("DUMMY_6", "<size=0></size>", () => new dummy());
		//[AutoRegisterConfigKey]
		//private static ModConfigurationKey<bool> HIDE_EXPIRED_SESSIONS = new ModConfigurationKey<bool>("HIDE_EXPIRED_SESSIONS", "Hide expired sessions:", () => true);
		//[AutoRegisterConfigKey]
		//private static ModConfigurationKey<int> LAST_UPDATE_MAX_DAYS = new ModConfigurationKey<int>("LAST_UPDATE_MAX_DAYS", "If session has not updated for this number of days, consider it expired:", () => 90, valueValidator: v => v > 0);
		//[AutoRegisterConfigKey]
		//private static ModConfigurationKey<bool> HIDE_EXPIRED_SESSIONS_COMPLETELY = new ModConfigurationKey<bool>("HIDE_EXPIRED_SESSIONS_COMPLETELY", "Hide expired sessions completely:", () => false);
		//[AutoRegisterConfigKey]
		//private static ModConfigurationKey<dummy> DUMMY_7 = new ModConfigurationKey<dummy>("DUMMY_7", "<i><color=gray>Meant to hide sessions that have not updated in a very long time.</color></i>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_ENDED_SESSIONS = new ModConfigurationKey<bool>("HIDE_ENDED_SESSIONS", "Hide ended sessions (Sessions without URLs):", () => false, internalAccessOnly: true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_ENDED_SESSIONS_COMPLETELY = new ModConfigurationKey<bool>("HIDE_ENDED_SESSIONS_COMPLETELY", "Hide ended sessions completely:", () => false, internalAccessOnly: true);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> EXTRA_LOGGING = new ModConfigurationKey<bool>("EXTRA_LOGGING", "Enable extra debug logging:", () => false, internalAccessOnly: true);

		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("owo.Nytra.WorldListSessionHider");
			Config = GetConfiguration();
			Config.OnThisConfigurationChanged += (configChangedEvent) => 
			{
				if (configChangedEvent.Key == EXTRA_LOGGING && Config.GetValue(EXTRA_LOGGING))
				{
					Debug("Logging configured strings...");
					foreach (string s in Config.GetValue(HOST_USERIDS)?.Split(','))
					{
						Debug($"UserID: \"{s}\"");
					}
					foreach (string s in Config.GetValue(HOST_USERNAMES)?.Split(','))
					{
						Debug($"Username: \"{s}\"");
					}
					foreach (string s in Config.GetValue(SESSION_IDS)?.Split(','))
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

		// Userspace.UserspaceWorld.LocalUser?
		// although maybe WorldThumbnailItem can exist in non-userspace worlds?
		private static FrooxEngine.User localUser = null;

		enum HideReason
		{
			Filtered,
			Stuck,
			Ended
		}

		//private static bool ReceivedContactRequestInSession(SessionInfo sessionInfo)
		//{
		//	foreach (SessionUser user in sessionInfo.SessionUsers)
		//	{
		//		Friend friend = Engine.Current.Cloud.Friends.FindFriend((Friend f) => f.FriendUserId == user.UserID && f.FriendStatus == FriendStatus.Requested);
		//		if (friend != null) return true;
		//	}
		//	return false;
		//}

		//private static void PrintSessionUsers(SessionInfo sessionInfo)
		//{
		//	foreach(SessionUser user in sessionInfo.SessionUsers)
		//	{
		//		Debug($"UserID: {user.UserID} Username: {user.Username}");
		//	}
		//}

		private static bool ShouldHideSession(SessionInfo sessionInfo, out HideReason? reason)
		{
			string sessionIds = Config.GetValue(SESSION_IDS);
			string hostUserIds = Config.GetValue(HOST_USERIDS);
			string hostUsernames = Config.GetValue(HOST_USERNAMES);

			reason = null;

			// Don't hide sessions that are currently opened worlds
			if (Engine.Current.WorldManager.Worlds.Any((World w) => w.SessionId == sessionInfo.SessionId)) return false;

			// If the session is not an opened world but the local user is in the session then it's a stuck session
			// or if session has present users but hasn't updated for a while
			if (Config.GetValue(HIDE_DEAD_SESSIONS) &&
				(sessionInfo.SessionUsers.Any((SessionUser user) => (user.UserID ?? user.Username) == (localUser.UserID ?? localUser.UserName)) ||
				(sessionInfo.SessionUsers.Any((SessionUser user) => user.IsPresent) && DateTime.UtcNow.Subtract(sessionInfo.LastUpdate).TotalMinutes >= 30)))
			{
				reason = HideReason.Stuck;
				return true;
			}
			else if (Config.GetValue(HIDE_FILTERED_SESSIONS) &&
				((!string.IsNullOrWhiteSpace(sessionIds) && sessionIds.Split(',').Contains(sessionInfo.SessionId)) ||
				((!string.IsNullOrWhiteSpace(hostUserIds) && hostUserIds.Split(',').Contains(sessionInfo.HostUserId)) ||
				((!string.IsNullOrWhiteSpace(hostUsernames) && hostUsernames.Split(',').Contains(sessionInfo.HostUsername))
				))))
			{
				reason = HideReason.Filtered;
				return true;
			}
			else if (Config.GetValue(HIDE_ENDED_SESSIONS) && sessionInfo.HasEnded)
			{
				reason = HideReason.Ended;
				return true;
			}

			return false;
		}

		// (Ignore this comment) If the number of sessions for a worldThumbnailItem keeps changing rapidly, use the first non-stuck and non-ended session as the primary one
		private static void ProcessWorldThumbnailItem(WorldThumbnailItem worldThumbnailItem, string debugString = "")
		{
			//bool hasMultipleSessions = false;

			localUser = worldThumbnailItem.LocalUser;

			List<SessionInfo> sessions = new List<SessionInfo>();
			SessionInfo sessionInfo = Engine.Current.WorldAnnouncer.GetInfoForSessionId(worldThumbnailItem.WorldOrSessionId);

			if (sessionInfo == null)
			{
				Engine.Current.WorldAnnouncer.GetSessionsForWorldId(worldThumbnailItem.WorldOrSessionId, sessions);
				sessionInfo = (SessionInfo)getBestSessionMethod.Invoke(worldThumbnailItem, new object[] { sessions });
				if (sessionInfo == null) return;
				//hasMultipleSessions = true;
			}

			if (Config.GetValue(EXTRA_LOGGING))
			{
				Debug(new string('=', 30));
				Debug("debugString: " + debugString);
				Debug($"Host UserID: \"{sessionInfo.HostUserId}\" Host Username: \"{sessionInfo.HostUsername}\" SessionID: \"{sessionInfo.SessionId}\"");
				foreach(string url in sessionInfo.SessionURLs)
				{
					Debug($"URL: {url}");
				}
				Debug($"LastUpdate: {sessionInfo.LastUpdate}");
			}

			HideReason? reason;
			if (ShouldHideSession(sessionInfo, out reason))
			{
				string nameText;
				bool configValue;
				bool flag = true;
				switch (reason)
				{
					case HideReason.Stuck:
						nameText = "<i>[STUCK]</i>";
						configValue = Config.GetValue(HIDE_DEAD_SESSIONS_COMPLETELY);
						break;
					case HideReason.Filtered:
						nameText = "<i>[HIDDEN]</i>";
						configValue = Config.GetValue(HIDE_FILTERED_SESSIONS_COMPLETELY);
						break;
					case HideReason.Ended:
						nameText = "<i>[ENDED]</i>";
						configValue = Config.GetValue(HIDE_ENDED_SESSIONS_COMPLETELY);
						break;
					default:
						nameText = "<i>[UNKNOWN]</i>";
						configValue = false;
						break;
				}
				//if (hasMultipleSessions)
				//{
				//	HideReason? reason2;
				//	if (sessions.All((SessionInfo sessionInfo2) => ShouldHideSession(sessionInfo, out reason2)))
				//	{
				//		flag = true;
				//	}
				//	else
				//	{
				//		flag = false;
				//	}
				//}
				Hide(worldThumbnailItem, nameText, configValue && flag);
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

				ProcessWorldThumbnailItem(__instance, "Called from UpdateInfo");
			}
		}

		[HarmonyPatch(typeof(WorldThumbnailItem), "OnActivated")]
		class WorldListSessionHiderPatch2
		{
			public static void Postfix(WorldThumbnailItem __instance)
			{
				if (!Config.GetValue(MOD_ENABLED)) return;

				__instance.RunSynchronously(delegate { ProcessWorldThumbnailItem(__instance, "Called from OnActivated"); });
			}
		}
	}
}