using EasyRun.CustomAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Reflection;

namespace EasyRun.Settings
{
    public class TargetPropertiesResolver : DefaultContractResolver
    {
        private readonly JsonTargetType[] targets;

        public TargetPropertiesResolver(params JsonTargetType[] targets)
        {
            this.targets = targets;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            var target = member.GetCustomAttribute(typeof(JsonTargetAttribute)) as JsonTargetAttribute;

            if (target != null)
            {
                if (targets.Contains(target.JsonTargetType))
                {
                    property.ShouldSerialize = _ => true;
                    property.ShouldDeserialize = _ => true;
                }
                else
                {
                    property.ShouldSerialize = _ => false;
                    property.ShouldDeserialize = _ => false;
                }
            }

            return property;
        }
    }
}
