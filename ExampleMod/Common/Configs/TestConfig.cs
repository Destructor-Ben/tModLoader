using Terraria.ModLoader.Config;

namespace ExampleMod.Common.Configs;

public class TestConfig : ModConfig
{
	public override ConfigScope Mode => ConfigScope.ClientSide;
}