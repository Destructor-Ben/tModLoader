using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.OS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Engine;
using Terraria.ModLoader.UI;
using Terraria.Social;
using Terraria.UI.Chat;
using ReLogic.Content;
using Terraria.UI.Gamepad;
using System.Linq;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.Config;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Terraria;

public partial class Main
{
	public static int soundError;
	public static int ambientError;
	public static bool mouseMiddle;
	public static bool mouseXButton1;
	public static bool mouseXButton2;
	public static bool mouseMiddleRelease;
	public static bool mouseXButton1Release;
	public static bool mouseXButton2Release;
	public static Point16 trashSlotOffset;
	public static bool hidePlayerCraftingMenu;
	public static bool showServerConsole;
	public static bool Support8K = true; // provides an option to disable 8k (but leave 4k)
	public static double desiredWorldEventsUpdateRate = 1; // dictates the speed at which world events (falling stars, fairy spawns, sandstorms, etc.) can change/happen
	/// <summary>
	/// Representation that dictates the actual amount of "world event updates" that happen in a given GAME tick. This number increases/decreases in direct tandem with
	/// <seealso cref="desiredWorldEventsUpdateRate"/>.
	/// </summary>
	public static int worldEventUpdates;
	private double _partialWorldEventUpdates = 0f;

	public static List<TitleLinkButton> tModLoaderTitleLinks = new List<TitleLinkButton>();

	private static readonly HttpClient client = new HttpClient();

	/// <summary>
	/// A color that cycles through the colors like Rainbow Brick does.
	/// </summary>
	public static Color DiscoColor => new Color(DiscoR, DiscoG, DiscoB);
	/// <summary>
	/// The typical pulsing white color used for much of the text shown in-game.
	/// </summary>
	public static Color MouseTextColorReal => new Color(mouseTextColor / 255f, mouseTextColor / 255f, mouseTextColor / 255f, mouseTextColor / 255f);
	public static bool PlayerLoaded => CurrentFrameFlags.ActivePlayersCount > 0;

	// Used to prevent situations where when going from borderless Fullscreen display to windowed display the width and height don't get resized to be able to access key window functions
	// Does not effect resizing the window manually. May not be perfect, but will at least be sufficient to provide room to manually address this on the end user side
	// Magic constant comes from default windows border settings: ~ 1377 / 1440 and 1033 / 1080.
	private static int BorderedHeight(int height, bool state) => (int)(height * (state ? 1 : 0.95625));

	// Tracks whether the Stylist has had her hairstyle list updated for the current interaction.
	private static bool hairstylesUpdatedForThisInteraction; // TML: Track whether hairstyle cache needs refreshing for Stylist UI.

	private static Player _currentPlayerOverride;

	/// <summary>
	/// A replacement for `Main.LocalPlayer` which respects whichever player is currently running hooks on the main thread.
	/// This works in the player select screen, and in multiplayer (when other players are updating)
	/// </summary>
	public static Player CurrentPlayer => _currentPlayerOverride ?? LocalPlayer;

	/// <summary>
	/// Use to iterate over active players. Game logic is usually only interested in <see cref="Entity.active"/> elements, this iterator facilitates that usage and allows for simpler and more readable code.
	/// <para/> Typically used in a foreach statement:
	/// <code>foreach (var player in Main.ActivePlayers) {
	///     // Code
	/// }
	/// </code>
	/// This is equivalent to the less convenient approach:
	/// <code>
	/// for (int i = 0; i &lt; Main.maxPlayers; i++) {
	///     var player = Main.player[i];
	///     if (!player.active)
	///         continue;
	///     // Code
	/// }
	/// </code>
	/// Note that if the index of the Player in the <see cref="Main.player"/> array is needed, <see cref="Entity.whoAmI"/> can be used.
	/// </summary>
	public static ActiveEntityIterator<Player> ActivePlayers => new(player.AsSpan(0, maxPlayers));
	/// <summary>
	/// Use to iterate over active players. Game logic is usually only interested in <see cref="Entity.active"/> elements, this iterator facilitates that usage and allows for simpler and more readable code.
	/// <para/> Typically used in a foreach statement:
	/// <code>foreach (var npc in Main.ActiveNPCs) {
	///     // Code
	/// }
	/// </code>
	/// This is equivalent to the less convenient approach:
	/// <code>
	/// for (int i = 0; i &lt; Main.maxNPCs; i++) {
	///     var npc = Main.npc[i];
	///     if (!npc.active)
	///         continue;
	///     // Code
	/// }
	/// </code>
	/// Note that if the index of the NPC in the <see cref="Main.npc"/> array is needed, <see cref="Entity.whoAmI"/> can be used.
	/// </summary>
	public static ActiveEntityIterator<NPC> ActiveNPCs => new(npc.AsSpan(0, maxNPCs));
	/// <summary>
	/// Use to iterate over active projectiles. Game logic is usually only interested in <see cref="Entity.active"/> elements, this iterator facilitates that usage and allows for simpler and more readable code.
	/// <para/> Typically used in a foreach statement:
	/// <code>foreach (var projectile in Main.ActiveProjectiles) {
	///     // Code
	/// }
	/// </code>
	/// This is equivalent to the less convenient approach:
	/// <code>
	/// for (int i = 0; i &lt; Main.maxProjectiles; i++) {
	///     var projectile = Main.projectile[i];
	///     if (!projectile.active)
	///         continue;
	///     // Code
	/// }
	/// </code>
	/// Note that if the index of the Projectile in the <see cref="Main.projectile"/> array is needed, <see cref="Entity.whoAmI"/> can be used.
	/// </summary>
	public static ActiveEntityIterator<Projectile> ActiveProjectiles => new(projectile.AsSpan(0, maxProjectiles));
	/// <summary>
	/// Use to iterate over active items. Game logic is usually only interested in <see cref="Entity.active"/> elements, this iterator facilitates that usage and allows for simpler and more readable code.
	/// <para/> Typically used in a foreach statement:
	/// <code>foreach (var item in Main.ActiveItems) {
	///     // Code
	/// }
	/// </code>
	/// This is equivalent to the less convenient approach:
	/// <code>
	/// for (int i = 0; i &lt; Main.maxItems; i++) {
	///     var item = Main.item[i];
	///     if (!item.active)
	///         continue;
	///     // Code
	/// }
	/// </code>
	/// Note that if the index of the Item in the <see cref="Main.item"/> array is needed, <see cref="Entity.whoAmI"/> can <b>not</b> be used. This will be fixed in 1.4.5, but for now the for loop approach would have to be used instead.
	/// </summary>
	public static ActiveEntityIterator<Item> ActiveItems => new(item.AsSpan(0, maxItems));

	/// <summary>
	/// Checks if a tile at the given coordinates counts towards tile coloring from the Spelunker buff, and is detected by various pets.
	/// </summary>
	public static bool IsTileSpelunkable(int tileX, int tileY)
	{
		Tile tile = Main.tile[tileX, tileY];
		return IsTileSpelunkable(tileX, tileY, tile.type, tile.frameX, tile.frameY);
	}

	/// <summary>
	/// Checks if a tile at the given coordinates counts towards tile coloring from the Biome Sight buff.
	/// </summary>
	public static bool IsTileBiomeSightable(int tileX, int tileY, ref Color sightColor)
	{
		Tile tile = Main.tile[tileX, tileY];
		return IsTileBiomeSightable(tileX, tileY, tile.type, tile.frameX, tile.frameY, ref sightColor);
	}

	public static void InfoDisplayPageHandler(int startX, ref string mouseText, out int startingDisplay, out int endingDisplay)
	{
		// Note that these are not indexes exactly, but count drawn elements (meaning active if inventory is open)
		startingDisplay = 0;
		endingDisplay = InfoDisplayLoader.InfoDisplayCount;

		if (!playerInventory)
			return;

		int activeDisplays = InfoDisplayLoader.ActiveDisplays();
		InfoDisplayLoader.InfoDisplayPage = Utils.Clamp(InfoDisplayLoader.InfoDisplayPage, 0, (activeDisplays - 1) / 12);

		if (activeDisplays > 12) {
			startingDisplay = 12 * InfoDisplayLoader.InfoDisplayPage;
			endingDisplay = Utils.Clamp(startingDisplay + 12, startingDisplay, activeDisplays);

			Texture2D buttonTexture = UICommon.InfoDisplayPageArrowTexture.Value;
			bool hovering = false;

			GetInfoAccIconPosition(11, startX, out int X, out int Y);
			Vector2 buttonPosition = new Vector2(X, Y + 20);

			if ((float)mouseX >= buttonPosition.X && (float)mouseY >= buttonPosition.Y && (float)mouseX <= buttonPosition.X + (float)buttonTexture.Width && (float)mouseY <= buttonPosition.Y + (float)buttonTexture.Height && !PlayerInput.IgnoreMouseInterface) {
				hovering = true;
				player[myPlayer].mouseInterface = true;

				if (mouseLeft && mouseLeftRelease) {
					SoundEngine.PlaySound(12);
					mouseLeftRelease = false;

					if (InfoDisplayLoader.ActivePages() != InfoDisplayLoader.InfoDisplayPage + 1)
						InfoDisplayLoader.InfoDisplayPage += 1;
					else
						InfoDisplayLoader.InfoDisplayPage = 0;
				}

				if (!Main.mouseText) {
					mouseText = (InfoDisplayLoader.ActivePages() != InfoDisplayLoader.InfoDisplayPage + 1) ? Language.GetTextValue("tModLoader.NextInfoAccPage") : Language.GetTextValue("tModLoader.FirstInfoAccPage");
					Main.mouseText = true;
				}
			}

			spriteBatch.Draw(buttonTexture, buttonPosition, new Rectangle(0, 0, buttonTexture.Width, buttonTexture.Height), Color.White, 0f, default, 1f, SpriteEffects.None, 0f);

			if (hovering)
				spriteBatch.Draw(TextureAssets.InfoIcon[13].Value, buttonPosition - Vector2.One * 2f, null, OurFavoriteColor, 0f, default, 1f, SpriteEffects.None, 0f);

			hovering = false;
			GetInfoAccIconPosition(0, startX, out X, out int _);
			buttonPosition = new Vector2(X, Y + 20);

			if ((float)mouseX >= buttonPosition.X && (float)mouseY >= buttonPosition.Y && (float)mouseX <= buttonPosition.X + (float)buttonTexture.Width && (float)mouseY <= buttonPosition.Y + (float)buttonTexture.Height && !PlayerInput.IgnoreMouseInterface) {
				hovering = true;
				player[myPlayer].mouseInterface = true;

				if (mouseLeft && mouseLeftRelease) {
					SoundEngine.PlaySound(12);
					mouseLeftRelease = false;

					if (InfoDisplayLoader.InfoDisplayPage != 0)
						InfoDisplayLoader.InfoDisplayPage -= 1;
					else
						InfoDisplayLoader.InfoDisplayPage = InfoDisplayLoader.ActivePages() - 1;
				}

				if (!Main.mouseText) {
					mouseText = (InfoDisplayLoader.InfoDisplayPage != 0) ? Language.GetTextValue("tModLoader.PreviousInfoAccPage") : Language.GetTextValue("tModLoader.LastInfoAccPage");
					Main.mouseText = true;
				}
			}

			spriteBatch.Draw(buttonTexture, buttonPosition, new Rectangle(0, 0, buttonTexture.Width, buttonTexture.Height), Color.White, 0f, default, 1f, SpriteEffects.FlipHorizontally, 0f);

			if (hovering)
				spriteBatch.Draw(TextureAssets.InfoIcon[13].Value, buttonPosition - Vector2.One * 2f, null, OurFavoriteColor, 0f, default, 1f, SpriteEffects.None, 0f);
		}
	}
	
	public static void BuilderTogglePageHandler(int startY, int activeToggles, out bool moveDownForButton, out int startIndex, out int endIndex) {
		startIndex = 0;
		endIndex = activeToggles;
		moveDownForButton = false;
		string text = "";

		if (activeToggles > 12) {
			startIndex = 12 * BuilderToggleLoader.BuilderTogglePage;

			if (activeToggles - startIndex < 12)
				endIndex = activeToggles;
			else if (startIndex == 0)
				endIndex = startIndex + 12;
			else
				endIndex = startIndex + 11;

			Texture2D buttonTexture = UICommon.InfoDisplayPageArrowTexture.Value;
			bool hover = false;
			Vector2 buttonPosition = new Vector2(3, startY - 6f);

			if (BuilderToggleLoader.BuilderTogglePage != 0) {
				moveDownForButton = true;

				spriteBatch.Draw(buttonTexture, buttonPosition + new Vector2(0, 13f), new Rectangle(0, 0, buttonTexture.Width, buttonTexture.Height), Color.White, -(float)Math.PI / 2, default, 1f, SpriteEffects.None, 0f);
				if ((float)mouseX >= buttonPosition.X && (float)mouseY >= buttonPosition.Y && (float)mouseX <= buttonPosition.X + (float)buttonTexture.Width && (float)mouseY <= buttonPosition.Y + (float)buttonTexture.Height && !PlayerInput.IgnoreMouseInterface) {
					hover = true;

					player[myPlayer].mouseInterface = true;
					text = Language.GetTextValue("tModLoader.PreviousInfoAccPage");
					mouseText = true;

					if (mouseLeft && mouseLeftRelease) {
						SoundEngine.PlaySound(SoundID.MenuTick);
						mouseLeftRelease = false;

						if (BuilderToggleLoader.BuilderTogglePage > 0)
							BuilderToggleLoader.BuilderTogglePage--;
					}

					spriteBatch.Draw(TextureAssets.InfoIcon[13].Value, buttonPosition + new Vector2(0, 17) - Vector2.One * 2f, null, OurFavoriteColor, -(float)Math.PI / 2, default, 1f, SpriteEffects.None, 0f);
				}
			}

			buttonPosition = new Vector2(3, startY + ((endIndex - startIndex) + (BuilderToggleLoader.BuilderTogglePage != 0).ToInt()) * 24f - 6f);

			if (BuilderToggleLoader.BuilderTogglePage != activeToggles / 12) {
				spriteBatch.Draw(buttonTexture, buttonPosition + new Vector2(0, 12f), new Rectangle(0, 0, buttonTexture.Width, buttonTexture.Height), Color.White, (float)Math.PI / 2, new Vector2(buttonTexture.Width, buttonTexture.Height), 1f, SpriteEffects.None, 0f);

				if ((float)mouseX >= buttonPosition.X && (float)mouseY >= buttonPosition.Y && (float)mouseX <= buttonPosition.X + (float)buttonTexture.Width && (float)mouseY <= buttonPosition.Y + (float)buttonTexture.Height && !PlayerInput.IgnoreMouseInterface) {
					hover = true;

					player[myPlayer].mouseInterface = true;
					text = Language.GetTextValue("tModLoader.NextInfoAccPage");
					mouseText = true;

					if (mouseLeft && mouseLeftRelease) {
						SoundEngine.PlaySound(SoundID.MenuTick);
						mouseLeftRelease = false;

						if (BuilderToggleLoader.BuilderTogglePage < activeToggles / 12)
							BuilderToggleLoader.BuilderTogglePage++;
					}

					spriteBatch.Draw(TextureAssets.InfoIcon[13].Value, buttonPosition + new Vector2(4, 12) - Vector2.One * 2f, null, OurFavoriteColor, (float)Math.PI / 2, new Vector2(buttonTexture.Width, buttonTexture.Height), 1f, SpriteEffects.None, 0f);
				}
			}

			if (mouseText && hover) {
				float colorByte = (float)mouseTextColor / 255f;
				Color textColor = new Color(colorByte, colorByte, colorByte);

				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, text, new Vector2(mouseX + 14, mouseY + 14), textColor, 0f, Vector2.Zero, Vector2.One);
				mouseText = false;
			}
		}
	}

	private void DrawBuilderAccToggles_Inner(Vector2 start)
	{
		Player player = Main.player[myPlayer];
		int[] builderAccStatus = player.builderAccStatus;
		List<BuilderToggle> activeToggles = BuilderToggleLoader.ActiveBuilderTogglesList();
		bool shiftHotbarLock = activeToggles.Count / 12 != BuilderToggleLoader.BuilderTogglePage || activeToggles.Count % 12 >= 10;
		Vector2 startPosition = start - new Vector2(0, shiftHotbarLock ? 42 : 21);
		string text = "";

		BuilderTogglePageHandler((int)startPosition.Y, activeToggles.Count, out bool moveDownForButton, out int startIndex, out int endIndex);
		for (int i = startIndex; i < endIndex; i++) {
			BuilderToggle builderToggle = activeToggles[i];

			Texture2D texture = ModContent.Request<Texture2D>(builderToggle.Texture).Value;
			Rectangle rectangle = new Rectangle(0, 0, texture.Width, texture.Height);
			Color color = builderToggle.DisplayColorTexture_Obsolete();

			Vector2 position = startPosition + new Vector2(0, moveDownForButton ? 24 : 0) + new Vector2(0, (i % 12) * 24);
			text = builderToggle.DisplayValue();
			int numberOfStates = builderToggle.NumberOfStates;
			int toggleType = builderToggle.Type;

			/*
			BuilderToggleLoader.ModifyNumberOfStates(builderToggle, ref numberOfStates);
			BuilderToggleLoader.ModifyDisplayValue(builderToggle, ref text);
			*/

			bool hover = Utils.CenteredRectangle(position, new Vector2(14f)).Contains(MouseScreen.ToPoint()) && !PlayerInput.IgnoreMouseInterface;
			bool leftClick = hover && mouseLeft && mouseLeftRelease;
			bool rightClick = hover && mouseRight && mouseRightRelease;

			/*
			BuilderToggleLoader.ModifyDisplayColor(builderToggle, ref color);
			BuilderToggleLoader.ModifyDisplayTexture(builderToggle, ref texture, ref rectangle);
			*/

			// Save the original position for hover texture drawing so it won't be confusing
			Vector2 hoverDrawPosition = position;
			float scale = 1f;
			SpriteEffects spriteEffects = SpriteEffects.None;

			BuilderToggleDrawParams drawParams = new() {
				Texture = texture,
				Position = position,
				Frame = rectangle,
				Color = color,
				Scale = scale,
				SpriteEffects = spriteEffects
			};
			if (builderToggle.Draw(spriteBatch, ref drawParams)) {
				spriteBatch.Draw(drawParams.Texture, drawParams.Position, drawParams.Frame, drawParams.Color, 0f, drawParams.Frame.Size() / 2f, drawParams.Scale, drawParams.SpriteEffects, 0f);
			}

			if (hover) {
				player.mouseInterface = true;
				mouseText = true;

				Texture2D iconHover = ModContent.Request<Texture2D>(builderToggle.HoverTexture).Value;
				Rectangle hoverRectangle = new Rectangle(0, 0, iconHover.Width, iconHover.Height);
				Color hoverColor = OurFavoriteColor;
				float hoverScale = 1f;
				SpriteEffects hoverSpriteEffects = SpriteEffects.None;
				drawParams = new() {
					Texture = iconHover,
					Position = hoverDrawPosition,
					Frame = hoverRectangle,
					Color = hoverColor,
					Scale = hoverScale,
					SpriteEffects = hoverSpriteEffects
				};
				if (builderToggle.DrawHover(spriteBatch, ref drawParams)) {
					spriteBatch.Draw(drawParams.Texture, drawParams.Position, drawParams.Frame, drawParams.Color, 0f, drawParams.Frame.Size() / 2f, drawParams.Scale, drawParams.SpriteEffects, 0f);
				}
			}

			if (leftClick) {
				SoundStyle? sound = SoundID.MenuTick;
				if (builderToggle.OnLeftClick(ref sound)) {
					builderAccStatus[toggleType] = (builderAccStatus[toggleType] + 1) % numberOfStates;
					SoundEngine.PlaySound(sound);
				}
				mouseLeftRelease = false;
			}

			if (rightClick) {
				builderToggle.OnRightClick();
				mouseRightRelease = false;
			}


			UILinkPointNavigator.SetPosition(6000 + i % 12, position + rectangle.Size() * 0.15f);

			if (mouseText && hover && HoverItem.type <= 0) {
				float colorByte = (float)mouseTextColor / 255f;
				Color textColor = new Color(colorByte, colorByte, colorByte);

				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, text, new Vector2(mouseX + 14, mouseY + 14), textColor, 0f, Vector2.Zero, Vector2.One);
				mouseText = false;
			}
		}

		UILinkPointNavigator.Shortcuts.BUILDERACCCOUNT = (endIndex - startIndex);
	}

	//Mirrors code used in UpdateTime
	/// <summary>
	/// Syncs rain state if <see cref="StartRain"/> or <see cref="StopRain"/> were called in the same tick and caused a change to <seealso cref="maxRaining"/>.
	/// <br>Can be called on any side, but only the server will actually sync it.</br>
	/// </summary>
	public static void SyncRain()
	{
		if (maxRaining != oldMaxRaining) {
			if (netMode == 2)
				NetMessage.SendData(7);

			oldMaxRaining = maxRaining;
		}
	}

	public ref struct CurrentPlayerOverride
	{
		private Player _prevPlayer;

		public CurrentPlayerOverride(Player player)
		{
			_prevPlayer = _currentPlayerOverride;
			_currentPlayerOverride = player;
		}

		public void Dispose()
		{
			_currentPlayerOverride = _prevPlayer;
		}
	}

	internal void InitTMLContentManager()
	{
		if (dedServ) {
			return;
		}

		string vanillaContentFolder;
		if (SocialAPI.Mode == SocialMode.Steam) {
			vanillaContentFolder = Path.Combine(Steam.GetSteamTerrariaInstallDir(), "Content");
		}
		else if (InstallVerifier.DistributionPlatform == DistributionPlatform.GoG) {
			vanillaContentFolder = Path.Combine(Path.GetDirectoryName(InstallVerifier.vanillaExePath), "Content");
			Logging.tML.Info("Content folder of Terraria GOG Install Location assumed to be: " + Path.GetFullPath(vanillaContentFolder));
		}
		// Explicitly path if we are family shared using the old logic from prior to #4018; Temporary Hotfix - Solxan
		// Maybe replace with a call to get InstallDir from TerrariaSteamClient? Or change Steam.GetInstallDir to be 'FamilyShare' safe?
		// Also left as a generic fallback 
		else /*if (Social.Steam.SteamedWraps.FamilyShared)*/ {
			vanillaContentFolder = Platform.IsOSX ? "../Terraria/Terraria.app/Contents/Resources/Content" : "../Terraria/Content"; // Side-by-Side Manual Install

			if (!Directory.Exists(vanillaContentFolder)) {
				vanillaContentFolder = Platform.IsOSX ? "../Terraria.app/Contents/Resources/Content" : "../Content"; // Nested Manual Install
			}
		}
		

		if (!Directory.Exists(vanillaContentFolder)) {
			ErrorReporting.FatalExit(Language.GetTextValue("tModLoader.ContentFolderNotFound"));
		}

		// Canary file for legacy Terraria branches.
		if (!File.Exists(Path.Combine(vanillaContentFolder, "Images", "Projectile_112.xnb"))) {
			Utils.OpenToURL("https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Usage-FAQ#terraria-is-out-of-date-or-terraria-is-on-a-legacy-version");
			ErrorReporting.FatalExit(Language.GetTextValue("tModLoader.TerrariaLegacyBranchMessage"));
		}

		// Canary file, ensures that Terraria has updated to at least the version this tModLoader was built for. Alternate check to BuildID check in TerrariaSteamClient for non-Steam launches 
		if (!File.Exists(Path.Combine(vanillaContentFolder, "Images", "Projectile_981.xnb"))) {
			Utils.OpenToURL("https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Usage-FAQ#terraria-is-out-of-date-or-terraria-is-on-a-legacy-version");
			ErrorReporting.FatalExit(Language.GetTextValue("tModLoader.TerrariaOutOfDateMessage"));
		}


		TMLContentManager localOverrideContentManager = null;
		if (Directory.Exists(Path.Combine("Content", "Images")))
			localOverrideContentManager = new TMLContentManager(Content.ServiceProvider, "Content", null);

		base.Content = new TMLContentManager(Content.ServiceProvider, vanillaContentFolder, localOverrideContentManager);
	}
	
	private static void DrawtModLoaderSocialMediaButtons(Color menuColor, float upBump)
	{
		List<TitleLinkButton> titleLinks = tModLoaderTitleLinks;
		Vector2 anchorPosition = new Vector2(18f, (float)(screenHeight - 26) - upBump);
		for (int i = 0; i < titleLinks.Count; i++) {
			titleLinks[i].Draw(spriteBatch, anchorPosition);
			anchorPosition.X += 30f;
		}
	}

	private static void DrawtModLoaderMenuText(Color menuColor, ref float upBump)
	{
		DrawMenuText(ModLoader.ModLoader.versionedName, 0, 0);
		
		/*

		string supportMessage = Language.GetTextValue("tModLoader.PatreonSupport");
		string patreonShortURL = @"patreon.com/tModLoader";
		bool showPatreon = SocialAPI.Mode != SocialMode.Steam;

		// Show number of mods - 1 such as to show number of enabled mods that are not tModLoader itself
		string modsMessage = Language.GetTextValue("tModLoader.MenuModsEnabled", Math.Max(0, ModLoader.ModLoader.Mods.Length - 1));
		
		//TODO: FUTURE
		//if (ModLoader.Core.GOGModUpdateChecker.ModUpdatesAvailable > 0) {
		//	modsMessage += " " + Language.GetTextValue("tModLoader.MenuModUpdatesAvailable", ModLoader.Core.GOGModUpdateChecker.ModUpdatesAvailable);
		//}

		/*
		string text = versionNumber;
		*
		string text = ModLoader.ModLoader.versionedName + (showPatreon ? Environment.NewLine + supportMessage : "") + (menuMode == 0 ? Environment.NewLine : "") + Environment.NewLine + "Terraria " + versionNumber;
		Vector2 origin = FontAssets.MouseText.Value.MeasureString(text);
		origin.X *= 0.5f;
		origin.Y *= 0.5f;
		for (int i = 0; i < 5; i++) {
			Microsoft.Xna.Framework.Color color = Microsoft.Xna.Framework.Color.Black;
			if (i == 4) {
				color = menuColor;
				color.R = (byte)((255 + color.R) / 2);
				color.G = (byte)((255 + color.R) / 2);
				color.B = (byte)((255 + color.R) / 2);
			}

			color.A = (byte)((float)(int)color.A * 0.3f);
			int num = 0;
			int num2 = 0;
			if (i == 0)
				num = -2;

			if (i == 1)
				num = 2;

			if (i == 2)
				num2 = -2;

			if (i == 3)
				num2 = 2;

			spriteBatch.DrawString(FontAssets.MouseText.Value, text, new Vector2(origin.X + (float)num + 10f, (float)screenHeight - origin.Y + (float)num2 - 2f - upBump), color, 0f, origin, 1f, SpriteEffects.None, 0f);

			// Developer mode button.
			if (menuMode == 0 /*&& !ModCompile.DeveloperMode/) {
				string developerModeText = Language.GetTextValue("tModLoader.SwitchVersionInfoButton");

				// measure and draw text from bottom right
				var textSize = FontAssets.MouseText.Value.MeasureString(developerModeText);
				var pos = new Vector2(screenWidth - 10f + num, screenHeight - 2f + num2);
				var d_color = color;

				float scale = 1.2f;

				// final draw
				if (i == 4) {
					var rect = new Rectangle((int)(pos.X - textSize.X * scale), (int)(pos.Y - textSize.Y * scale), (int)(textSize.X * scale), (int)(textSize.Y * scale));
					bool mouseover = rect.Contains(mouseX, mouseY);
					d_color = mouseover ? highVersionColor : d_color;

					if (mouseover && mouseLeftRelease && mouseLeft) {
						SoundEngine.PlaySound(SoundID.MenuOpen);
						Utils.OpenToURL("https://github.com/tModLoader/tModLoader/wiki/tModLoader-guide-for-players#beta-branches");
					}
				}

				spriteBatch.DrawString(FontAssets.MouseText.Value, developerModeText, pos, d_color, 0f, textSize, 1.2f, SpriteEffects.None, 0f);
			}

			// TML Patreon.
			if (showPatreon) {
				var font = FontAssets.MouseText.Value;
				var patreonOrigin = font.MeasureString(supportMessage);
				Vector2 urlSize = font.MeasureString(patreonShortURL);

				spriteBatch.DrawString(font, patreonShortURL, new Vector2(patreonOrigin.X + num + 10f, screenHeight - patreonOrigin.Y + num2 - 2f - (int)upBump), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

				if (i == 4 && mouseLeftRelease && mouseLeft && new Microsoft.Xna.Framework.Rectangle((int)patreonOrigin.X + 10, screenHeight - (int)urlSize.Y - 2 - (int)upBump, (int)urlSize.X, (int)patreonOrigin.Y).Contains(new Microsoft.Xna.Framework.Point(mouseX, mouseY)) && hasFocus) {
					SoundEngine.PlaySound(SoundID.MenuOpen);
					Utils.OpenToURL("https://www.patreon.com/tModLoader");
				}
			}

			// ModPack
			if (ModOrganizer.ModPackActive != null) {
				var font = FontAssets.MouseText.Value;
				string modpackText = Language.GetTextValue("tModLoader.CurrentModPack", Path.GetFileNameWithoutExtension(ModOrganizer.ModPackActive));
				var packOrigin = font.MeasureString(modpackText);

				spriteBatch.DrawString(font, modpackText, new Vector2(packOrigin.X + num + 10f, screenHeight - packOrigin.Y + num2 - 2f - (int)upBump), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			}
		}

		// End of DrawVersionNumber
		HandleNews(menuColor);

		//*/

		// Copied from Main.DrawVersionNumber
		void DrawMenuText(string text, int left, int top, Color? textColorOverride = null, Color? textShadowColorOverride = null)
		{
			Vector2 origin = FontAssets.MouseText.Value.MeasureString(text);
			origin.X *= 0.5f;
			origin.Y *= 0.5f;
			for (int i = 0; i < 5; i++) {
				Microsoft.Xna.Framework.Color color = Microsoft.Xna.Framework.Color.Black;

				if (textShadowColorOverride is not null)
					color = textShadowColorOverride.Value;

				if (i == 4) {
					color = menuColor;
					color.R = (byte)((255 + color.R) / 2);
					color.G = (byte)((255 + color.G) / 2);
					color.B = (byte)((255 + color.B) / 2);

					if (textColorOverride is not null)
						color = textColorOverride.Value;
				}

				color.A = (byte)((float)(int)color.A * 0.3f);

				int num = 0;
				int num2 = 0;
				if (i == 0)
					num = -2;

				if (i == 1)
					num = 2;

				if (i == 2)
					num2 = -2;

				if (i == 3)
					num2 = 2;

				spriteBatch.DrawString(FontAssets.MouseText.Value, text, new Vector2(origin.X + num + left, origin.Y + num2 + top), color, 0f, origin, 1f, SpriteEffects.None, 0f);
			}
		}
	}

	/// <summary>
	/// Wait for an action to be performed on the main thread.
	/// </summary>
	/// <param name="action"></param>
	public static Task RunOnMainThread(Action action)
	{
		var tcs = new TaskCompletionSource();

		QueueMainThreadAction(() => {
			action();
			tcs.SetResult();
		});

		return tcs.Task;
	}

	/// <summary>
	/// Wait for an action to be performed on the main thread.
	/// </summary>
	/// <param name="func"></param>
	public static Task<T> RunOnMainThread<T>(Func<T> func)
	{
		var tcs = new TaskCompletionSource<T>();
		QueueMainThreadAction(() => tcs.SetResult(func()));
		return tcs.Task;
	}

	private static PosixSignalRegistration SIGINTHandler;
	private static PosixSignalRegistration SIGTERMHandler;
	public static void AddSignalTraps()
	{
		static void Handle(PosixSignalContext ctx) {
			ctx.Cancel = true;
			Logging.tML.Info($"Signal {ctx.Signal}, Closing Server...");
			Netplay.Disconnect = true;
		}

		SIGINTHandler = PosixSignalRegistration.Create(PosixSignal.SIGINT, Handle);
		SIGTERMHandler = PosixSignalRegistration.Create(PosixSignal.SIGTERM, Handle);
	}

	private static string newsText = "???";
	private static string newsURL = null;
	private static bool newsChecked = false;
	private static bool newsIsNew = false;
	private static void HandleNews(Color menuColor)
	{
		if (menuMode == 0) {
			if (!newsChecked) {
				newsText = Language.GetTextValue("tModLoader.LatestNewsChecking");
				newsChecked = true;
				// Download latest news, save to config.json.
				// https://partner.steamgames.com/doc/webapi/ISteamNews
				client.GetStringAsync("https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=1281930&count=1").ContinueWith(response => {
					if (!response.IsCompletedSuccessfully || response.Exception != null) {
						newsText = Language.GetTextValue("tModLoader.LatestNewsOffline");
						return;
					}
					JObject o = JObject.Parse(response.Result);
					newsText = (string)o["appnews"]["newsitems"][0]["title"]; // No way to access specific language results in API.
					newsURL = (string)o["appnews"]["newsitems"][0]["url"];
					int newsTimestamp = (int)o["appnews"]["newsitems"][0]["date"];
					if (newsTimestamp != ModLoader.ModLoader.LatestNewsTimestamp) {
						// Latest timestamp should usually be newer, unless a news entry is deleted for some reason?
						newsIsNew = true;
						ModLoader.ModLoader.LatestNewsTimestamp = newsTimestamp;
						Main.SaveSettings();
					}
				});
			}
			else {
				string latestNewsText = Language.GetTextValue("tModLoader.LatestNews", newsText);
				var newsScale = 1.2f;
				if (newsIsNew) {
					menuColor = Main.DiscoColor;
				}
				var newsScales = new Vector2(newsScale);
				var newsPosition = new Vector2(screenWidth - 10f, screenHeight - 38f);
				var newsSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, latestNewsText, newsScales);
				var newsRect = new Rectangle((int)(newsPosition.X - newsSize.X), (int)(newsPosition.Y - newsSize.Y), (int)newsSize.X, (int)newsSize.Y);
				bool newsMouseOver = newsRect.Contains(mouseX, mouseY);
				var newsColor = newsMouseOver && newsURL != null ? highVersionColor : menuColor;
				ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, latestNewsText, newsPosition - newsSize, newsColor, 0f, Vector2.Zero, newsScales);

				if (newsMouseOver && mouseLeftRelease && mouseLeft && hasFocus && newsURL != null) {
					SoundEngine.PlaySound(SoundID.MenuOpen);
					Utils.OpenToURL(newsURL);
					newsIsNew = false;
				}
			}
		}
	}

	private static void PrepareLoadedModsAndConfigsForSingleplayer()
	{
		// If any loaded mod or config differs from normal, intercept SinglePlayer menu with a prompt
		// TODO: Should this remember the mod loadout before joining server? Right now it only cares about version downgrades. MP mods will still load.
		bool needsReload = false;
		List<ReloadRequiredExplanation> reloadRequiredExplanationEntries = new();
		var normalModsToLoad = ModOrganizer.RecheckVersionsToLoad();
		foreach (var loadedMod in ModLoader.ModLoader.Mods) {
			if (loadedMod is ModLoaderMod || loadedMod.File == null)
				continue;
			var normalMod = normalModsToLoad.First(mod => mod.Name == loadedMod.Name); // If this throws, we have a big issue.
			if (normalMod.modFile.path != loadedMod.File.path) {
				reloadRequiredExplanationEntries.Add(new ReloadRequiredExplanation(1, normalMod.Name, normalMod, Language.GetTextValue("tModLoader.ReloadRequiredExplanationSwitchVersion", "FFFACD", normalMod.Version, loadedMod.Version)));
				needsReload = true;
			}
		}

		if(ConfigManager.AnyModNeedsReloadCheckOnly(out List<Mod> modsWithChangedConfigs)) {
			needsReload = true;
			foreach (var mod in modsWithChangedConfigs) {
				var localMod = normalModsToLoad.First(localMod => localMod.Name == mod.Name);
				reloadRequiredExplanationEntries.Add(new ReloadRequiredExplanation(5, mod.Name, localMod, Language.GetTextValue("tModLoader.ReloadRequiredExplanationConfigChangedRestore", "DDA0DD")));
			}
		}

		// If reload is required, show message. Back action should leave current ModConfig instances unchanged 
		if (needsReload) {
			string continueButtonText = Language.GetTextValue("tModLoader.ReloadRequiredReloadAndContinue");
			Interface.serverModsDifferMessage.Show(Language.GetTextValue("tModLoader.ReloadRequiredSinglePlayerMessage", continueButtonText),
				gotoMenu: 0, // back to main menu
				continueButtonText: continueButtonText,
				continueButtonAction: () => {
					ModLoader.ModLoader.OnSuccessfulLoad += () => { Main.menuMode = 1; };
					ModLoader.ModLoader.Reload();
				},
				backButtonText: Language.GetTextValue("tModLoader.ModConfigBack"),
				backButtonAction: () => {
					// Do nothing, logic will to back to main menu
				},
				reloadRequiredExplanationEntries: reloadRequiredExplanationEntries
			);
		}
		else {
			// Otherwise, config changes take effect immediately and the player select menu will be shown
			ConfigManager.LoadAll(); // Makes sure MP configs are cleared.
			ConfigManager.OnChangedAll();
		}
	}
}
