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
		public override string Version => "1.1.0";
		public override string Link => "https://github.com/Nytra/NeosWorldListSessionHider";
		public static ModConfiguration Config;

		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> MOD_ENABLED = new ModConfigurationKey<bool>("MOD_ENABLED", "Enable hiding sessions:", () => false);
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_0 = new ModConfigurationKey<dummy>("DUMMY_0", "<size=0></size>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> HOST_USERIDS = new ModConfigurationKey<string>("HOST_USERIDS", "Host User IDs:", () => "");
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> HOST_USERNAMES = new ModConfigurationKey<string>("HOST_USERNAMES", "Host Usernames:", () => "");
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<string> SESSION_IDS = new ModConfigurationKey<string>("SESSION_IDS", "Session IDs:", () => "");
		//[AutoRegisterConfigKey]
		//private static ModConfigurationKey<string> UNIVERSE_IDS = new ModConfigurationKey<string>("UNIVERSE_IDS", "Universe IDs:", () => "");
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_1 = new ModConfigurationKey<dummy>("DUMMY_1", "<i><color=gray>All of these can be comma-separated to store multiple values.</color></i>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<dummy> DUMMY_2 = new ModConfigurationKey<dummy>("DUMMY_2", "<size=0></size>", () => new dummy());
		[AutoRegisterConfigKey]
		private static ModConfigurationKey<bool> HIDE_COMPLETELY = new ModConfigurationKey<bool>("HIDE_COMPLETELY", "Hide sessions completely:", () => false);

		public override void OnEngineInit()
		{
			Harmony harmony = new Harmony("owo.Nytra.WorldListSessionHider");
			Config = GetConfiguration();
			harmony.PatchAll();
		}

		private static Type theType = typeof(WorldThumbnailItem);
		private static MethodInfo getBestSessionMethod = AccessTools.Method(theType, "GetBestSession");
		private static MethodInfo updateThumbnailMethod = AccessTools.Method(theType, "UpdateThumbnailURL");
		private static FieldInfo nameTextField = AccessTools.Field(theType, "_nameText");
		private static FieldInfo detailTextField = AccessTools.Field(theType, "_detailText");
		private static FieldInfo counterTextField = AccessTools.Field(theType, "_counterText");
		private static FieldInfo deferredThumbnailField = AccessTools.Field(theType, "_deferredThumbnailUrl");

		private static void Hide(WorldThumbnailItem worldThumbnailItem)
		{
			if (Config.GetValue(HIDE_COMPLETELY))
			{
				worldThumbnailItem.Slot.ActiveSelf = false;
			}
			else
			{
				updateThumbnailMethod.Invoke(worldThumbnailItem, new object[] { NeosAssets.Skyboxes.Thumbnails.NoThumbnail });
				var nameText = (SyncRef<Text>)nameTextField.GetValue(worldThumbnailItem);
				nameText.Target.Content.Value = "<i>[HIDDEN]</i>";
				var detailText = (SyncRef<Text>)detailTextField.GetValue(worldThumbnailItem);
				detailText.Target.Content.Value = "<i>...</i>";
				var counterText = (SyncRef<Text>)counterTextField.GetValue(worldThumbnailItem);
				counterText.Target.Content.Value = "<i>...</i>";
				deferredThumbnailField.SetValue(worldThumbnailItem, NeosAssets.Skyboxes.Thumbnails.NoThumbnail);
			}
		}

		[HarmonyPatch(typeof(WorldThumbnailItem), "UpdateInfo")]
		class WorldListSessionHiderPatch
		{
			public static void Postfix(WorldThumbnailItem __instance, FrooxEngine.Record record, IReadOnlyList<SessionInfo> sessions, IReadOnlyList<World> openedWorlds)
			{
				if (!Config.GetValue(MOD_ENABLED)) return;

				SessionInfo bestSession = (SessionInfo)getBestSessionMethod.Invoke(__instance, new object[] { sessions });

				if (bestSession == null) return;

				if (Config.GetValue(SESSION_IDS).Split(',').Contains(bestSession.SessionId) ||
					Config.GetValue(HOST_USERIDS).Split(',').Contains(bestSession.HostUserId) ||
					Config.GetValue(HOST_USERNAMES).Split(',').Contains(bestSession.HostUsername))
				{
					Debug("Hiding session: " + bestSession.Name);
					Hide(__instance);
				}
			}
		}
	}
}