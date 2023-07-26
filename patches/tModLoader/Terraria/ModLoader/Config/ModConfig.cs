using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace Terraria.ModLoader.Config;

// TODO: Enforce no statics allowed.

/// <summary>
/// ModConfig provides a way for mods to be configurable. ModConfigs can either be Client specific or Server specific.
/// When joining a MP server, Client configs are kept but Server configs are synced from the server.
/// Using serialization attributes such as [DefaultValue(5)] or [JsonIgnore] are critical for proper usage of ModConfig.
/// tModLoader also provides its own attributes such as ReloadRequiredAttribute and LabelAttribute.
/// </summary>
public abstract class ModConfig : ILocalizedModType
{
	[JsonIgnore]
	public Mod Mod { get; internal set; }

	[JsonIgnore]
	public string Name { get; internal set; }

	[JsonIgnore]
	public string FullName => $"{Mod.Name}/{Name}";

	[JsonIgnore]
	public string LocalizationCategory => "Configs";

	[JsonIgnore]
	public virtual LocalizedText DisplayName => Language.GetOrRegister(this.GetLocalizationKey(nameof(DisplayName)), () => ConfigManager.GetLegacyLabelAttribute(GetType())?.LocalizationEntry ?? Regex.Replace(Name, "([A-Z])", " $1").Trim());

	[JsonIgnore]
	public abstract ConfigScope Mode { get; }

	// TODO: Does non-autoloaded ModConfigs have a use-case?
	public virtual bool Autoload(ref string name) => Mod.ContentAutoloadingEnabled;

	/// <summary>
	/// This method is called when the ModConfig has been loaded for the first time. This happens before regular Autoloading and Mod.Load. You can use this hook to assign a static reference to this instance for easy access.
	/// tModLoader will automatically assign (and later unload) this instance to a static field named Instance in the class prior to calling this method, if it exists.
	/// </summary>
	public virtual void OnLoaded() { }

	/// <summary>
	/// This hook is called anytime new config values have been set and are ready to take effect. This will always be called right after OnLoaded and anytime new configuration values are ready to be used. The hook won't be called with values that violate NeedsReload. Use this hook to integrate with other code in your Mod to apply the effects of the configuration values. If your NeedsReload is correctly implemented, you should be able to apply the settings without error in this hook. Be aware that this hook can be called in-game and in the main menu, as well as in single player and multiplayer situations.
	/// </summary>
	public virtual void OnChanged() { }

	/// <summary>
	/// Called on the Server for ServerSide configs to determine if the changes asked for by the Client will be accepted. Useful for enforcing permissions. Called after a check for NeedsReload.
	/// </summary>
	/// <param name="pendingConfig">An instance of the ModConfig with the attempted changes</param>
	/// <param name="whoAmI">The client whoAmI</param>
	/// <param name="message">A message that will be returned to the client, set this to the reason the server rejects the changes.<br/>
	/// Make sure you set this to the localization key instead of the actual value, since the server and client could have different languages.</param>
	/// <returns>Return false to reject client changes</returns>
	public virtual bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message)
		=> true;

	// TODO: Can we get rid of Clone and just load from disk? Don't think so yet.
	/// <summary>
	/// tModLoader will call Clone on ModConfig to facilitate proper implementation of the ModConfig user interface and detecting when a reload is required. Modders need to override this method if their config contains reference types. Failure to do so will lead to bugs. See ModConfigShowcaseDataTypes.Clone for examples and explanations.
	/// </summary>
	/// <returns></returns>
	public virtual ModConfig Clone() => (ModConfig)MemberwiseClone();

	/// <summary>
	/// Whether or not a reload is required. The default implementation compares properties and fields annotated with the ReloadRequiredAttribute. Unlike the other ModConfig hooks, this method is called on a clone of the ModConfig that was saved during mod loading. The pendingConfig has values that are about to take effect. Neither of these instances necessarily match the instance used in OnLoaded.<br/>
	/// Has a max depth of 10.
	/// </summary>
	/// <param name="pendingConfig">The other instance of ModConfig to compare against, it contains the values that are pending to take effect</param>
	/// <returns></returns>
	public virtual bool NeedsReload(ModConfig pendingConfig)
	{
		return ObjectNeedsReload(this, pendingConfig);
	}

	/// <summary>
	/// Recursively checks an object to see if it has any fields that are changed and would require a reload.
	/// </summary>
	/// <param name="currentConfig"></param>
	/// <param name="pendingConfig"></param>
	/// <param name="depth"></param>
	/// <param name="checkSubField"></param>
	/// <returns></returns>
	protected static bool ObjectNeedsReload(object currentConfig, object pendingConfig, int depth = 10, Func<PropertyFieldWrapper, bool> checkSubField = default)
	{
		if (checkSubField == default)
			checkSubField = (field) => field.Type.IsClass;

		// Recursive limit check
		if (depth <= 0)
			return false;

		// Loop over every field to check if they have been changed
		foreach (var field in ConfigManager.GetFieldsAndProperties(currentConfig)) {
			// If it has a reload required attribute and the field values don't match, then return true
			bool doesntHaveJsonIgnore = ConfigManager.GetCustomAttributeFromMemberThenMemberType<JsonIgnoreAttribute>(field, currentConfig, null) == null;
			bool hasReloadRequired = ConfigManager.GetCustomAttributeFromMemberThenMemberType<ReloadRequiredAttribute>(field, currentConfig, null) != null;
			bool dontEqual = !ConfigManager.ObjectEquals(field.GetValue(currentConfig), field.GetValue(pendingConfig));
			if (doesntHaveJsonIgnore && hasReloadRequired && dontEqual)
				return true;

			// Otherwise if it's a sub config, then check that as well
			if (checkSubField(field))
				return ObjectNeedsReload(field.GetValue(currentConfig), field.GetValue(pendingConfig), depth - 1);
		}

		return false;
	}

	/// <summary>
	/// Opens this config in the config UI.<br/>
	/// Can be used to allow your own UI to add buttons to access the config.
	/// </summary>
	public void Open()
	{
		SoundEngine.PlaySound(SoundID.MenuOpen);
		Interface.modConfig.SetMod(Mod, this, true);
		if (Main.gameMenu) {
			Main.menuMode = Interface.modConfigID;
		}
		else {
			IngameFancyUI.CoverNextFrame();

			Main.playerInventory = false;
			Main.editChest = false;
			Main.npcChatText = "";
			Main.inFancyUI = true;

			Main.InGameUI.SetState(Interface.modConfig);
		}
	}
}
