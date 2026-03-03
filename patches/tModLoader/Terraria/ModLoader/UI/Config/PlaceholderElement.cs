using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.Localization;

namespace Terraria.ModLoader.UI.Config;

public class PlaceholderElement(LocalizedText message, params string[] args) : ConfigElement
{
	private LocalizedText message = message;
	private string[] args = args;

	// TODO: just modify the text display function

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		// TODO: temp
		var dimensions = GetDimensions();
		spriteBatch.Draw(TextureAssets.MagicPixel.Value, dimensions.ToRectangle(), Color.Red);
	}
}