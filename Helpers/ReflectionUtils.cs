using System;
using System.Collections.Generic;
using System.Reflection;

namespace IceyStimmy.Helpers
{
    public static class ReflectionUtils
    {
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> CachedFields = new();
                
        private static FieldInfo GetFieldInfo<T>(string fieldName)
        {
            const BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            FieldInfo fieldInfo;
            
            if (CachedFields.ContainsKey(typeof(T)))
            {
                var fields = CachedFields[typeof(T)];
                if (fields.ContainsKey(fieldName))
                    return fields[fieldName];
                
                fieldInfo = typeof(T).GetField(fieldName, bindFlags);
                fields.Add(fieldName, fieldInfo);
            }
            
            fieldInfo = typeof(T).GetField(fieldName, bindFlags);

            var a = new Dictionary<string, FieldInfo> {{ fieldName, fieldInfo }};
            CachedFields.Add(typeof(T), a);
            
            return fieldInfo;
        }
        
        public static object GetInstanceField<T>(T instance, string fieldName)
        {
            var field = GetFieldInfo<T>(fieldName);
            return field.GetValue(instance);
        }
    }
}
