using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;

namespace ExampleMod.Common.Configs
{
    [Label("DefaultValue with Reference Types")]
    public class DefaultValueConfig : ModConfig
    {
        /*
         * All but someBool are false upon opening the config
         * Despite the simpleBool and boolWrapped having [DefaultValue(true)] attribute
         * This is most likely caused by the reference types NOT using DefaultValue attribute, but a constructor instead
         * (Just like ExampleConfig suggests though)
         *
         * In addition to that, only reference types work with SeparatePage, which forces you to use those if you want separate pages,
         * which leads to default values not being assigned properly
         *
         * Plus, if you collapse all the config elements, and change the someBool, then save config, all the collapsed elements expand again,
         * which is pretty annoying if you have lists or similar in those which clutter the UI
         */

        public override ConfigScope Mode => ConfigScope.ClientSide;

        public Simple simple = new Simple();

        [SeparatePage] //Doesn't work
        [DefaultValue(true)] //Works
        [Label("default true")]
        public bool someBool;

        [SeparatePage] //works
        public Simple simplePage = new Simple();

        public BoolWrapper boolWrapper = new BoolWrapper();

        public SimpleWrapper simpleWrapper = new SimpleWrapper();
    }

    [Label("Dicts and Wrappers")]
    public class DictWrapperConfig : ModConfig
    {
        /*
         * As stated previously, SeparatePage doesn't work on dicts either, but it should since they are very clunky and take up alot of space
         * Therefore, you need to make a wrapper.
         *
         * but It will always create a default
         * "integerDictWrapper" : { }
         * inside the config file
         *
         * Adding `ReferenceLoopHandling = ReferenceLoopHandling.Serialize,` to serializerSettings(Compact) in ConfigManager fixes a previous error with how I implemented Equals for the DictWrapper,
         * not needed anymore
         */

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [SeparatePage] //Doesn't work
        public Dictionary<int, int> integerDict = new Dictionary<int, int>();

        [SeparatePage] //Works
        public DictWrapperPrimitive integerDictWrapper = new DictWrapperPrimitive();
    }

    [Label("Inheritance")]
    public class InheritanceConfig : ModConfig
    {
        /*
         * JsonIgnore on a get-property that doesn't have a label won't show up on the config UI
         *
         * When inheriting another class, all the child fields are listed above the parent ones
         * (Expected otherwise)
         *
         * #parent fields:
         * first
         * second
         * third
         *
         * #child fields:
         * fourth
         * fifth
         * first
         * second
         * third
         */

        public override ConfigScope Mode => ConfigScope.ClientSide;

        public Parent parent = new Parent();

        public Child child = new Child();
    }

    [Label("JsonIgnore and Labels")]
    public class JsonIgnoreGetConfig : ModConfig
    {
        /*
         * JsonIgnore on a get-property that doesn't have a label won't show up on the config UI (same as above)
         *
         * The int properties also update their value whenever you click with the mouse somewhere, the string ones don't
         */

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Label("TimeInt")]
        public int TimeInt => (int)Main.time;

        [JsonIgnore]
        [Label("TimeIntIgnore")]
        public int TimeIntIgnore => (int)Main.time;

        public int TimeIntNoLabel => (int)Main.time;

        //This one doesn't even show up on the config UI
        [JsonIgnore]
        public int TimeIntIgnoreNoLabel => (int)Main.time;

        [Label("TimeString")]
        public string TimeString => ((int)Main.time).ToString();

        [JsonIgnore]
        [Label("TimeStringIgnore")]
        public string TimeStringIgnore => ((int)Main.time).ToString();
    }
}