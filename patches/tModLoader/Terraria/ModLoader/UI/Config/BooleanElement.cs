using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Terraria.ModLoader.UI.Config;

internal class BooleanElement : ConfigElement<bool>
{
	private Asset<Texture2D> toggleTexture;

	public override void OnBind()
	{
		base.OnBind();

		// TODO: possibly get a new texture
		toggleTexture = Main.Assets.Request<Texture2D>("Images/UI/Settings_Toggle");
		OnLeftClick += (_, _) => Value = !Value;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		CalculatedStyle dimensions = GetDimensions();
		var sourceRectangle = new Rectangle(Value ? ((toggleTexture.Width() - 2) / 2 + 2) : 0, 0, (toggleTexture.Width() - 2) / 2, toggleTexture.Height());
		var drawPosition = new Vector2(dimensions.X + dimensions.Width - sourceRectangle.Width - 10f, dimensions.Y + 8f);
		spriteBatch.Draw(toggleTexture.Value, drawPosition, sourceRectangle, Color.White, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);

		string text = Value ? Lang.menu[126].Value : Lang.menu[124].Value;
		var textScale = new Vector2(DefaultTextScale);
		Vector2 textSize = ChatManager.GetStringSize(DefaultFont.Value, text, textScale);
		Vector2 valuePos = dimensions.Position() + (dimensions.Width - textSize.X - 30) * Vector2.UnitX + 8f * Vector2.UnitY;
		ChatManager.DrawColorCodedStringWithShadow(
			spriteBatch,
			DefaultFont.Value,
			text,
			valuePos,
			ValueTextColor,
			rotation: 0f,
			origin: Vector2.Zero,
			textScale
		);
	}
}