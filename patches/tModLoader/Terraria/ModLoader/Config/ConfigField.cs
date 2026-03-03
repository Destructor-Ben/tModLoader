using System;
using System.Collections;

namespace Terraria.ModLoader.Config;

/// <summary>
/// Used in the UI and elsewhere to refer to a specific field in a ModConfig.
/// <br/><br/>
/// Contains methods to get and set the value of a config field.
/// </summary>
public class ConfigField(ModConfig config, PropertyFieldWrapper memberInfo, ConfigField parent = null, int index = -1)
{
	// The config and the field
	public ModConfig Config { get; } = config;
	public PropertyFieldWrapper MemberInfo { get; } = memberInfo;

	// The optional "parent" field, which will be non-null if the config field is nested inside an object
	public ConfigField Parent { get; } = parent;
	// If Parent is a collection, then instead of using Field to access this data, we index into Parent with this index
	public int Index { get; } = index;

	// This allows for Value.get to propagate upwards back to the original config,
	// so if any new reference types are created and assigned to any mod config
	// fields, then config elements won't have to be refreshed since they won't
	// be referencing an old object.
	public object Value {
		get {
			// We are in the root config
			if (Parent is null)
				return MemberInfo.GetValue(Config);

			// Decide if collection or sub-field, and either access correctly
			// TODO: how does this work with non-list collections, e.g. sets/dicts?
			// - surely passing a get/set func down from whatever is creating the elements is better, but it does push the responsibility of how to get/set onto the collection elements themselves
			object parent = Parent.Value;
			if (parent is not IList collection)
				return MemberInfo.GetValue(parent);

			return collection[Index];
		}
		set {
			if (Parent is null) {
				MemberInfo.SetValue(Config, value);
				return;
			}

			object parent = Parent.Value;
			if (parent is not IList collection) {
				MemberInfo.SetValue(parent, value);
				return;
			}

			collection[Index] = value;
		}
	}

	public T GetAttribute<T>() where T : Attribute
	{
		return
			(T)Attribute.GetCustomAttribute(memberInfo.MemberInfo, typeof(T)) // On the field
		 ?? (T)Attribute.GetCustomAttribute(memberInfo.Type, typeof(T), true); // On the class
		// TODO: The intention was to prioritize the Type of the element in the array at this index. That was never hooked up it seems, and it might not make sense to apply this behavior to all config attributes at this time. Needs more thought, specifically about collections and which attributes to "inherit". For example, currently ListOfPair won't use Pair BackgroundColor
	}
}