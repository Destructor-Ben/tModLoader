using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.ModLoader.UI.Config;
using Terraria.UI;
using Terraria.UI.Chat;

namespace Terraria.ModLoader.UI;

public class UIHeaderElement : UIElement
{
	private readonly string header;
	private Asset<Texture2D> texture;
	private Color dividerColor;

	public UIHeaderElement(string header, Color dividerColor)
	{
		this.header = header;
		this.dividerColor = dividerColor;

		texture = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Separator1");
		Width.Set(0f, 1f);
		Height.Set(ConfigElement.DefaultHeight, 0f);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		DynamicSpriteFont font = FontAssets.MouseText.Value;
		CalculatedStyle dimensions = GetInnerDimensions();
		Vector2 textPosition = dimensions.Position() + new Vector2(5);
		var dividerRect = new Rectangle((int)dimensions.X, (int)(dimensions.Y + dimensions.Height) - 4, (int)dimensions.Width, 4);

		spriteBatch.Draw(texture.Value, dividerRect, dividerColor);
		ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, header, textPosition, Color.White, 0f, Vector2.Zero, Vector2.One);
	}

	public static Color BlendColor(Color color)
	{
		return Color.Lerp(Color.White, color, 0.85f) * 0.9f;
	}
}