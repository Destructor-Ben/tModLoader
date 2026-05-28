using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Terraria.ModLoader.UI.Config;

public abstract class ConfigElement<T> : ConfigElement
{
	protected new T Value {
		get => (T)base.Value;
		set => base.Value = value;
	}
}

public abstract class ConfigElement : UIElement
{
	public ConfigField Field { get; private set; }
	public ModConfig Config => Field.Config;

	public object Value {
		get => Field.Value;
		set {
			Field.Value = value;
			Interface.modConfig.OnConfigModified();
		}
	}

	public const float DefaultHeight = 30;
	public const float DefaultTextScale = 0.8f;
	public const float PaddingH = 10f;
	public const float PaddingV = 8f;
	public static readonly Color ValueTextColor = Color.White * 0.75f;
	public static Asset<DynamicSpriteFont> DefaultFont => FontAssets.ItemStack;

	private Color backgroundColor = UICommon.DefaultUIBlue; // TODO inherit parent object color?

	public const int flashRate = 120;
	public bool Flashing { get; set; }

	protected Asset<Texture2D> PlayTexture { get; set; } = Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay");
	protected Asset<Texture2D> DeleteTexture { get; set; } = Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete");
	protected Asset<Texture2D> PlusTexture { get; set; } = UICommon.ButtonPlusTexture;
	//protected Texture2D UpArrowTexture { get; set; } = Texture2D.FromStream(Main.instance.GraphicsDevice, Assembly.GetExecutingAssembly().GetManifestResourceStream("Terraria.ModLoader.Config.UI.ButtonIncrement.png"));
	//protected Texture2D DownArrowTexture { get; set; } = Texture2D.FromStream(Main.instance.GraphicsDevice, Assembly.GetExecutingAssembly().GetManifestResourceStream("Terraria.ModLoader.Config.UI.ButtonDecrement.png"));
	protected Asset<Texture2D> UpDownTexture { get; set; } = UICommon.ButtonUpDownTexture;
	protected Asset<Texture2D> CollapsedTexture { get; set; } = UICommon.ButtonCollapsedTexture;
	protected Asset<Texture2D> ExpandedTexture { get; set; } = UICommon.ButtonExpandedTexture;

	// Attributes
	protected LabelKeyAttribute LabelAttribute;
	protected string Label;
	protected TooltipKeyAttribute TooltipAttribute;
	protected BackgroundColorAttribute BackgroundColorAttribute;
	protected RangeAttribute RangeAttribute;
	protected IncrementAttribute IncrementAttribute;
	protected JsonDefaultValueAttribute JsonDefaultValueAttribute;
	// Etc
	protected bool NullAllowed { get; set; }
	protected internal Func<string> LabelFunction { get; set; }
	protected Func<string> TooltipFunction { get; set; }
	protected bool ReloadRequired { get; set; }
	protected bool ShowReloadRequiredTooltip { get; set; }

	// TODO: move to config field?
	protected object OldValue { get; set; }
	protected bool ValueChanged => !ConfigManager.ObjectEquals(OldValue, Value);

	public ConfigElement()
	{
		Width.Set(0f, 1f);
		Height.Set(DefaultHeight, 0f);
	}

	/// <summary>
	/// Bind must always be called after the ctor and serves to facilitate a convenient inheritance workflow for custom ConfigElements from mods.
	/// </summary>
	// TODO: add a parent field and param here, so we can inherit values from the parent UI element, such as bg color
	public void Bind(ConfigField field)
	{
		Field = field;
		OnBind();
	}

	public virtual void OnBind()
	{
		LabelAttribute = Field.GetAttribute<LabelKeyAttribute>();
		Label = ConfigManager.GetLocalizedLabel(Field.MemberInfo);
		// Potential TODO if highly requested: Support interpolating value itself into label.
		LabelFunction = () => Label;

		TooltipAttribute = Field.GetAttribute<TooltipKeyAttribute>();
		string tooltip = ConfigManager.GetLocalizedTooltip(Field.MemberInfo);
		if (tooltip != null) {
			TooltipFunction = () => tooltip;
		}

		BackgroundColorAttribute = Field.GetAttribute<BackgroundColorAttribute>();

		if (BackgroundColorAttribute != null) {
			backgroundColor = BackgroundColorAttribute.Color;
		}

		RangeAttribute = Field.GetAttribute<RangeAttribute>();
		IncrementAttribute = Field.GetAttribute<IncrementAttribute>();
		NullAllowed = Field.GetAttribute<NullAllowedAttribute>() != null;
		JsonDefaultValueAttribute = Field.GetAttribute<JsonDefaultValueAttribute>();
		ShowReloadRequiredTooltip = Field.GetAttribute<ReloadRequiredAttribute>() != null;

		// TODO - Add line for default value?
		if (ShowReloadRequiredTooltip && Field.Parent is null) {
			// Default ModConfig.NeedsReload logic currently only checks members of the ModConfig class, this mirrors that logic.
			ReloadRequired = true;
			// We need to check against the value in the load time config, not the value at the time of binding.
			ModConfig loadTimeConfig = ConfigManager.GetLoadTimeConfig(Config.Mod, Config.Name);
			OldValue = Field.MemberInfo.GetValue(loadTimeConfig);

			TooltipFunction = () => {
				string tt = tooltip;

				if (ShowReloadRequiredTooltip) {
					tt += string.IsNullOrEmpty(tt) ? "" : "\n";
					tt += $"[c/{Color.Orange.Hex3()}:" + Language.GetTextValue("tModLoader.ModReloadRequiredMemberTooltip") + "]";
				}

				return tt;
			};
		 }
	}

	/// <summary>
	/// Called when the config UI refreshes.<br/><br/>
	/// Ensure the ConfigElement UI still reflects the value from GetObject(), as the config may have had changes reverted or it's default values restored.
	/// </summary>
	public virtual void RefreshUI() { }

	public override void MouseOver(UIMouseEvent evt)
	{
		base.MouseOver(evt);
		Flashing = false;
	}

	// TODO: provide basic functionality for rendering value/getting color of the value text?
	// TODO: also add hooks for drawing stuff on the right, since things like the revert and restore buttons will take up space
	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();

		DrawBackgroundPanel(spriteBatch, dimensions.ToRectangle(), GetBackgroundPanelColor());

		string label = GetLabel();
		if (!string.IsNullOrEmpty(label)) {
			var (labelTextColor, labelShadowColor) = GetLabelColor();
			DrawLabel(spriteBatch, label, labelTextColor, labelShadowColor);
		}

		if (!IsMouseHovering)
			return;

		string tooltip = GetTooltip();
		if (string.IsNullOrEmpty(tooltip))
			return;

		UIModConfig.Tooltip = tooltip;
	}

	protected virtual Color GetBackgroundPanelColor()
	{
		Color panelColor = backgroundColor;

		if (Flashing) {
			float ratio = Utils.Turn01ToCyclic010(((Interface.modConfig.UpdateCount % flashRate) / (float)flashRate)) * 0.5f + 0.5f;
			panelColor = Color.Lerp(panelColor, Color.White, MathF.Pow(ratio, 2));
		}

		if (!IsMouseHovering)
			panelColor = panelColor.MultiplyRGBA(new Color(180, 180, 180));

		return panelColor;
	}

	protected virtual (Color, Color) GetLabelColor()
	{
		return (Field.MemberInfo.CanWrite ? Color.White : Color.Gray, Color.Black);
	}

	protected virtual void DrawBackgroundPanel(SpriteBatch sb, Rectangle dims, Color color)
	{
		Texture2D texture = UICommon.ConfigPanelTexture.Value;
		int highlightSize = dims.Height / 2;

		// Left and right
		sb.Draw(texture, new Rectangle(dims.X, dims.Y + 2, 2, dims.Height - 4), new Rectangle(0, 2, 1, 1), color);
		sb.Draw(texture, new Rectangle(dims.X + dims.Width - 2, dims.Y + 2, 2, dims.Height - 4), new Rectangle(0, 2, 1, 1), color);

		// Up and down
		sb.Draw(texture, new Rectangle(dims.X + 2, dims.Y, dims.Width - 4, 2), new Rectangle(2, 0, 1, 1), color);
		sb.Draw(texture, new Rectangle(dims.X + 2, dims.Y + dims.Height - 2, dims.Width - 4, 2), new Rectangle(2, 0, 1, 1), color);

		// Inner panel
		sb.Draw(texture, new Rectangle(dims.X + 2, dims.Y + 2, dims.Width - 4, highlightSize - 2), new Rectangle(2, 2, 1, 1), color);
		sb.Draw(texture, new Rectangle(dims.X + 2, dims.Y + highlightSize, dims.Width - 4, dims.Height - highlightSize - 2), new Rectangle(2, 16, 1, 1), color);
	}

	protected virtual void DrawLabel(SpriteBatch sb, string label, Color textColor, Color shadowColor)
	{
		CalculatedStyle dimensions = GetDimensions();
		Vector2 textPos = dimensions.Position();
		// TODO: better alignment?
		textPos.X += 8f;
		textPos.Y += 8f;

		// TODO: bigger text?

		// TODO: Support chat tag hover?
		ChatManager.DrawColorCodedStringWithShadow(
			sb,
			DefaultFont.Value,
			label,
			textPos,
			textColor,
			shadowColor,
			rotation: 0f,
			origin: Vector2.Zero,
			baseScale: new Vector2(DefaultTextScale),
			maxWidth: dimensions.Width
		);
	}

	// TODO: just override these in child classes instead of using a weird function that can be set?

	protected virtual string GetLabel()
	{
		string label = LabelFunction();

		if (ReloadRequired && ValueChanged) {
			label += " - [c/FF0000:" + Language.GetTextValue("tModLoader.ModReloadRequired") + "]";
		}

		return label;
	}

	protected virtual string GetTooltip()
	{
		return TooltipFunction();
	}
}