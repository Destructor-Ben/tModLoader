using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ExampleMod.Common.Configs
{
    public class Simple
    {
        [DefaultValue(true)]
        [Label("default true")]
        public bool simpleBool;

        public override string ToString()
        {
            return $"Enabled: {simpleBool}";
        }

        public override bool Equals(object obj)
        {
            if (obj is Simple other)
                return simpleBool == other.simpleBool;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return new { simpleBool }.GetHashCode();
        }
    }

    public class SimpleWrapper : Simple { }

    public abstract class BoolWrapperAbstract
    {
        [DefaultValue(true)]
        [Label("default true")]
        public bool boolWrapped;

        public override string ToString()
        {
            return $"Enabled: {boolWrapped}";
        }

        public override bool Equals(object obj)
        {
            if (obj is BoolWrapperAbstract other)
                return boolWrapped == other.boolWrapped;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return new { boolWrapped }.GetHashCode();
        }
    }

    public class BoolWrapper : BoolWrapperAbstract { }

    public abstract class DictWrapperAbstract<T1, T2>
    {
        public Dictionary<T1, T2> Dict = new Dictionary<T1, T2>();

        public override bool Equals(object obj)
        {
            if (obj is DictWrapperAbstract<T1, T2> other)
            {
                return Dict.Equals(other.Dict);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode() => Dict.GetHashCode();
    }

    public class DictWrapperPrimitive : DictWrapperAbstract<int, int> { }

    public class Parent
    {
        public bool first;
        public bool second;
        public bool third;

        //Doesn't show up in the config UI at all if it has no label
        [JsonIgnore]
        public string FirstEnabledIgnore => first ? "Yes" : "No";

        public string FirstEnabledNoIgnore => first ? "Yes" : "No";

        public override bool Equals(object obj)
        {
            if (obj is Parent other)
                return first == other.first && second == other.second && third == other.third;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return new { first, second, third }.GetHashCode();
        }
    }

    public class Child : Parent
    {
        //Out of order (doesnt matter if you implement equals and gethashcode separately)
        public bool fourth;
        public bool fifth;
    }
}