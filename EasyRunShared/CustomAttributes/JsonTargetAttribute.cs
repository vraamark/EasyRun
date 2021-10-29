using System;

namespace EasyRun.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class JsonTargetAttribute : Attribute
    {
        private readonly JsonTargetType jsonTargetType;

        public JsonTargetAttribute(JsonTargetType jsonTargetType)
        {
            this.jsonTargetType = jsonTargetType;
        }

        public virtual JsonTargetType JsonTargetType
        {
            get { return jsonTargetType; }
        }
    }
}
