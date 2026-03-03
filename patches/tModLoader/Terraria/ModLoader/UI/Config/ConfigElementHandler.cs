using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Terraria.Localization;
using Terraria.ModLoader.Config;
using Terraria.UI;

namespace Terraria.ModLoader.UI.Config;

/// <summary>
/// Contains various utilities to deal with config elements, such as creating them, handling headers, and registering custom config elements.
/// </summary>
public static class ConfigElementHandler
{
	public static List<UIElement> GetConfigElements(ModConfig config, object obj = null)
	{
		var elements = new List<UIElement>();

		// ReSharper disable once LoopCanBePartlyConvertedToQuery
		foreach (PropertyFieldWrapper memberInfo in ConfigManager.GetFieldsAndProperties(obj ?? config)) {
			if (Attribute.IsDefined(memberInfo.MemberInfo, typeof(JsonIgnoreAttribute)) && !Attribute.IsDefined(memberInfo.MemberInfo, typeof(ShowDespiteJsonIgnoreAttribute)))
				continue;

			var field = new ConfigField(config, memberInfo);

			if (TryGetHeader(field, out var header)) {
				elements.Add(header);
			}

			elements.Add(GetConfigElement(field));
		}

		return elements;
	}

	public static ConfigElement GetConfigElement(ConfigField field)
	{
		// TODO: priorities:
		// - custom UI from the attribute
		// - custom UI from the mod registering a custom config element -> should ModConfig have a method to set allow setting this?
		// - default UI
		return TryGetDefaultConfigElement(field);
	}

	private static ConfigElement TryGetDefaultConfigElement(ConfigField field)
	{
		ConfigElement configElement = null;

		/* TODO: bring all of this back
		//public static Tuple<UIElement, UIElement> WrapIt(UIElement uiParent, ref int top, ConfigField field, ConfigField parent, int order, Type arrayType = null, int index = -1)
		int elementHeight;
		Type type = field.MemberInfo.Type;

		if (arrayType != null) {
			type = arrayType;
		}

		// TODO: Other common structs? -- Rectangle, Point
		var customUI = field.GetAttribute<CustomModConfigItemAttribute>();

		if (customUI != null) {
			Type customUIType = customUI.Type;

			if (typeof(ConfigElement).IsAssignableFrom(customUIType)) {
				ConstructorInfo ctor = customUIType.GetConstructor(Array.Empty<Type>());

				if (ctor != null) {
					object instance = ctor.Invoke(new object[0]);
					configElement = instance as UIElement;
				}
				else {
					configElement = new UIText($"{customUIType.Name} specified via CustomModConfigItem for {field.MemberInfo.Name} does not have an empty constructor.");
				}
			}
			else {
				configElement = new UIText($"{customUIType.Name} specified via CustomModConfigItem for {field.MemberInfo.Name} does not inherit from ConfigElement.");
			}
		}
		else if (type == typeof(ItemDefinition)) {
			configElement = new ItemDefinitionElement();
		}
		else if (type == typeof(ProjectileDefinition)) {
			configElement = new ProjectileDefinitionElement();
		}
		else if (type == typeof(NPCDefinition)) {
			configElement = new NPCDefinitionElement();
		}
		else if (type == typeof(PrefixDefinition)) {
			configElement = new PrefixDefinitionElement();
		}
		else if (type == typeof(BuffDefinition)) {
			configElement = new BuffDefinitionElement();
		}
		else if (type == typeof(TileDefinition)) {
			configElement = new TileDefinitionElement();
		}
		else if (type == typeof(Color)) {
			configElement = new ColorElement();
		}
		else if (type == typeof(Vector2)) {
			configElement = new Vector2Element();
		}
		else if (type == typeof(bool)) // isassignedfrom?
		{
			configElement = new BooleanElement();
		}
		else if (type == typeof(float)) {
			configElement = new FloatElement();
		}
		else if (type == typeof(byte)) {
			configElement = new ByteElement();
		}
		else if (type == typeof(uint)) {
			configElement = new UIntElement();
		}
		else if (type == typeof(int)) {
			SliderAttribute sliderAttribute = parent.GetAttribute<SliderAttribute>();

			if (sliderAttribute != null)
				configElement = new IntRangeElement();
			else
				configElement = new IntInputElement();
		}
		else if (type == typeof(string)) {
			OptionStringsAttribute ost = parent.GetAttribute<OptionStringsAttribute>();
			if (ost != null)
				configElement = new StringOptionElement();
			else
				configElement = new StringInputElement();
		}
		else if (type == typeof(long)) {
			configElement = new LongElement();
		}
		else if (type == typeof(ulong)) {
			configElement = new ULongElement();
		}
		else if (type.IsEnum) {
			if (/*list != null* false) // TODO: why the fuck wouldn't this work with lists?
				configElement = new UIText($"{field.MemberInfo.Name} not handled yet ({type.Name}).");
			else {
				SliderAttribute sliderAttribute = parent.GetAttribute<SliderAttribute>();
				bool useNewElements = (Interface.modConfig == null || Interface.modConfig.mod.TModLoaderVersion.MajorMinor() >= new Version(2025, 9)) && sliderAttribute == null;
				if (useNewElements)
					configElement = new EnumElement2();
				else
					configElement = new EnumElement();
			}
		}
		else if (type.IsArray) {
			configElement = new ArrayElement();
		}
		else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
			configElement = new ListElement();
		}
		else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>)) {
			configElement = new SetElement();
		}
		else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
			configElement = new DictionaryElement();
		}
		else if (type == typeof(object)) {
			configElement = new UIText($"{field.MemberInfo.Name} can't be of the Type Object.");
		}
		else if (type.IsClass) {
			configElement = new ObjectElement(/*, ignoreSeparatePage: ignoreSeparatePage/);
		}
		else if (type.IsValueType && !type.IsPrimitive) {
			configElement = new UIText($"{field.MemberInfo.Name} not handled yet ({type.Name}) Structs need special UI.");
			//e.Top.Pixels += 6;
			configElement.Height.Pixels += 6;
			configElement.Left.Pixels += 4;

			//object subitem = memberInfo.GetValue(item);
		}*/

		// Absolute backup
		// TODO: handle properly
		configElement ??= new PlaceholderElement(Language.GetText($"{field.MemberInfo.Name} not handled yet ({field.MemberInfo.Type.Name})"));

		configElement.Bind(field);
		return configElement;
	}

	public static bool TryGetHeader(ConfigField field, out UIHeaderElement header)
	{
		header = null;

		HeaderAttribute headerAttribute = ConfigManager.GetLocalizedHeader(field.MemberInfo.MemberInfo);
		if (headerAttribute == null) {
			return false;
		}

		// TODO: blend with the panel color that the config specifies
		// - but what if inside an element? it should inherit then
		header = new UIHeaderElement(headerAttribute.Header, UIHeaderElement.BlendColor(UICommon.DefaultUIBlue));
		return true;
	}
}