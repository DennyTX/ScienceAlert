using UnityEngine;

namespace ReeperCommon
{
    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Event)]
    internal class DoNotSerialize : System.Attribute
    {}

    internal interface IConfigNodeTypeFormatter
    {
        string Serialize(object obj);

        object Deserialize(object obj, string value);
    }

    internal interface IReeperSerializable
    {
        void OnSerialize(ConfigNode node);

        void OnDeserialize(ConfigNode node);
    }

    internal class ConfigNodeTypeHandler
    {
        internal class Vector2Formatter : IConfigNodeTypeFormatter
        {
            public string Serialize(object obj)
            {
                return KSPUtil.WriteVector((Vector2)obj);
            }

            public object Deserialize(object obj, string value)
            {
                Vector2 vector = (Vector2)obj;
                vector = KSPUtil.ParseVector2(value);
                return vector;
            }
        }

        private System.Collections.Generic.Dictionary<System.Type, IConfigNodeTypeFormatter> handlers = new System.Collections.Generic.Dictionary<System.Type, IConfigNodeTypeFormatter>();

        internal ConfigNodeTypeHandler()
        {
            AddFormatter(typeof(Vector2), typeof(Vector2Formatter));
        }

        internal void AddFormatter(System.Type targetType, IConfigNodeTypeFormatter impl)
        {
            if (handlers.ContainsKey(targetType))
            {
                handlers[targetType] = impl;
                return;
            }
            handlers.Add(targetType, impl);
        }

        internal void AddFormatter(System.Type targetType, System.Type formatter)
        {
            try
            {
                if (!typeof(IConfigNodeTypeFormatter).IsAssignableFrom(formatter)) return;
                IConfigNodeTypeFormatter value = (IConfigNodeTypeFormatter)System.Activator.CreateInstance(formatter);
                if (handlers.ContainsKey(targetType))
                    handlers[targetType] = value;
                else
                    handlers.Add(targetType, value);
            }
            catch (System.Exception ex)
            {
                Log.Debug("[ScienceAlert]:ConfigNodeTypeHandler.AddFormatter: Exception while attempting to add handler for type '{0}' (of type {1}): {2}", targetType.FullName, formatter.FullName, ex);
            }
        }

        internal string Serialize<T>(ref T obj)
        {
            System.Type typeFromHandle = typeof(T);
            if (handlers.ContainsKey(typeFromHandle))
            {
                IConfigNodeTypeFormatter configNodeTypeFormatter = handlers[typeFromHandle];
                return configNodeTypeFormatter.Serialize(obj);
            }
            if (typeFromHandle.IsEnum)
            {
                return obj.ToString();
            }
            return obj.ToString();
        }

        internal bool Deserialize<T>(ref T obj, string value)
        {
            if (!handlers.ContainsKey(typeof(T)))
            {
                bool result;
                if (typeof(T).IsEnum)
                {
                    try
                    {
                        obj = (T)System.Enum.Parse(typeof(T), value, true);
                        return true;
                    }
                    catch (System.Exception)
                    {
                        return false;
                    }
                }
                try
                {
                    obj = ConfigUtil.ParseThrowable<T>(value);
                    result = true;
                }
                catch (System.Exception)
                {
                    result = false;
                }
                return result;
            }

            object obj2 = handlers[typeof(T)].Deserialize(obj, value);
            if (obj2 != null)
            {
                obj = (T)obj2;
                return true;
            }
            return false;
        }
    }
}
