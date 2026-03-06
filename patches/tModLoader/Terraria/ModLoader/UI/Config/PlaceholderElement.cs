using Terraria.Localization;

namespace Terraria.ModLoader.UI.Config;

public class PlaceholderElement : ConfigElement
{
	public LocalizedText Label;
	public object[] LabelArgs;

	public LocalizedText Tooltip;
	public object[] TooltipArgs;

	public override void OnBind()
	{
		base.OnBind();

		LabelFunction = () => Label?.Format(LabelArgs ?? []);
		TooltipFunction = () => Tooltip?.Format(TooltipArgs ?? []);
	}
}