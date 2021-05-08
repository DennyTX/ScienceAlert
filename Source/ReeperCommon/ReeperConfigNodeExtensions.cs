using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ReeperCommon
{
    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
    internal class Subsection : System.Attribute
    {
        private string sectionName = "Subsection";

        public string Section => sectionName;

        public Subsection(string name)
        {
            sectionName = name;
            if (string.IsNullOrEmpty(name))
            {
                sectionName = "Subsection";
            }
        }
    }

    public static class ReeperConfigNodeExtensions
    {
        internal static ConfigNode CreateConfigFromObjectEx(this object obj, ConfigNodeTypeHandler typeFormatter = null)
        {
            ConfigNode result;
            try
            {
                ConfigNode configNode = new ConfigNode(obj.GetType().Name);
                typeFormatter = typeFormatter ?? new ConfigNodeTypeHandler();
                FieldInfo[] objectFields = GetObjectFields(obj);
                FieldInfo[] array = objectFields;
                for (int i = 0; i < array.Length; i++)
                {
                    FieldInfo fieldInfo = array[i];
                    object[] customAttributes = fieldInfo.GetCustomAttributes(false);
                    object value = fieldInfo.GetValue(obj);
                    if (value != null)
                    {
                        if (typeof(ConfigNode).IsAssignableFrom(fieldInfo.FieldType))
                        {
                            ConfigNode configNode2 = new ConfigNode(fieldInfo.Name);
                            ConfigNode configNode3 = ((ConfigNode)Convert.ChangeType(value, typeof(ConfigNode))).CreateCopy();
                            if (string.IsNullOrEmpty(configNode3.name))
                                configNode3.name = "ConfigNode";
                            configNode2.ClearData();
                            Subsection subsection = customAttributes.SingleOrDefault(attr => attr is Subsection) as Subsection;
                            if (subsection == null)
                                configNode2.AddNode(configNode3);
                            else
                                configNode2.AddNode(subsection.Section).AddNode(configNode3);
                            configNode.AddNode(configNode2);
                        }
                        else
                        {
                            MethodInfo method = typeFormatter.GetType().GetMethod("Serialize", BindingFlags.Instance | BindingFlags.NonPublic);
                            if (method == null)
                            {
                                Log.Debug("[ScienceAlert]:CreateConfigFromObjectEx: Serialize method not found");
                            }
                            MethodInfo methodInfo = method.MakeGenericMethod(fieldInfo.FieldType);
                            string value2 = methodInfo.Invoke(typeFormatter, new[]
                            {
                                value
                            }) as string;
                            if (string.IsNullOrEmpty(value2))
                            {
                                Log.Warning("ConfigUtil.CreateConfigFromObjectEx: null or empty return value for serialized type {0}", fieldInfo.FieldType.Name);
                            }
                            WriteValue(configNode, fieldInfo.Name, value2, customAttributes);
                        }
                    }
                    else
                    {
                        Log.Warning("Could not get value for " + fieldInfo.Name);
                    }
                }
                PropertyInfo[] objectProperties = GetObjectProperties(obj);
                PropertyInfo[] array2 = objectProperties;
                for (int j = 0; j < array2.Length; j++)
                {
                    PropertyInfo propertyInfo = array2[j];
                    object obj2 = propertyInfo.GetGetMethod(true).Invoke(obj, null);
                    object[] customAttributes2 = propertyInfo.GetCustomAttributes(true);
                    MethodInfo method2 = typeFormatter.GetType().GetMethod("Serialize", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (method2 == null)
                    {
                        Log.Debug("[ScienceAlert]:CreateConfigFromObjectEx: Serialize method not found");
                    }
                    else
                    {
                        MethodInfo methodInfo2 = method2.MakeGenericMethod(propertyInfo.PropertyType);
                        string value3 = methodInfo2.Invoke(typeFormatter, new[]
                        {
                            obj2
                        }) as string;
                        if (string.IsNullOrEmpty(value3))
                        {
                            Log.Warning("ConfigUtil.CreateConfigFromObjectEx: null or empty return value for serialized type {0}", propertyInfo.PropertyType.Name);
                        }
                        WriteValue(configNode, propertyInfo.Name, value3, customAttributes2);
                    }
                }
                if (obj is IReeperSerializable)
                {
                    ((IReeperSerializable)obj).OnSerialize(configNode);
                }
                result = configNode;
            }
            catch (Exception ex)
            {
                Log.Debug("[ScienceAlert]:ConfigUtil.CreateConfigFromObjectEx: Exception {0}", ex);
                result = null;
            }
            return result;
        }

        internal static bool CreateObjectFromConfigEx(this ConfigNode node, object obj, ConfigNodeTypeHandler typeFormatter = null)
        {
            bool flag = true;
            typeFormatter = typeFormatter ?? new ConfigNodeTypeHandler();
            FieldInfo[] objectFields = GetObjectFields(obj);
            PropertyInfo[] objectProperties = GetObjectProperties(obj);
            Log.Debug("ALERT:CreateObjectFromConfig: Found {0} fields and {1} properties", objectFields.Length, objectProperties.Length);
            FieldInfo[] array = objectFields;
            for (int i = 0; i < array.Length; i++)
            {
                FieldInfo fieldInfo = array[i];
                try
                {
                    object[] customAttributes = fieldInfo.GetCustomAttributes(true);
                    if (typeof(ConfigNode).IsAssignableFrom(fieldInfo.FieldType))
                    {
                        if (node.HasNode(fieldInfo.Name))
                        {
                            Convert.ChangeType(fieldInfo.GetValue(obj) ?? new ConfigNode(), typeof(ConfigNode));
                            ConfigNode node2 = node.GetNode(fieldInfo.Name);
                            Subsection subsection = customAttributes.SingleOrDefault(attr => attr is Subsection) as Subsection;
                            if (subsection != null)
                            {
                                if (node2.HasNode(subsection.Section))
                                {
                                    node2 = node2.GetNode(subsection.Section);
                                }
                            }
                            if (node2.CountNodes == 1)
                            {
                                ConfigNode value = node2.nodes[0];
                                fieldInfo.SetValue(obj, value);
                            }
                        }
                    }
                    else
                    {
                        string text = ReadValue(node, fieldInfo.Name, fieldInfo.GetCustomAttributes(true));
                        if (!string.IsNullOrEmpty(text))
                        {
                            MethodInfo method = typeFormatter.GetType().GetMethod("Deserialize", BindingFlags.Instance | BindingFlags.NonPublic);
                            MethodInfo methodInfo = method.MakeGenericMethod(fieldInfo.FieldType);
                            if (!(bool)methodInfo.Invoke(typeFormatter, new[]
                            {
                                fieldInfo.GetValue(obj),
                                text
                            }))
                            {
                                flag = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("[ScienceAlert]:Exception while deserializing field '{0}': {1}", fieldInfo.Name, ex);
                    flag = false;
                }
            }
            PropertyInfo[] array2 = objectProperties;
            for (int j = 0; j < array2.Length; j++)
            {
                PropertyInfo propertyInfo = array2[j];
                try
                {
                    string text2 = ReadValue(node, propertyInfo.Name, propertyInfo.GetCustomAttributes(true));
                    if (!string.IsNullOrEmpty(text2))
                    {
                        MethodInfo method2 = typeFormatter.GetType().GetMethod("Deserialize", BindingFlags.Instance | BindingFlags.NonPublic);
                        MethodInfo methodInfo2 = method2.MakeGenericMethod(propertyInfo.PropertyType);
                        object obj2 = Convert.ChangeType(propertyInfo.GetGetMethod(true).Invoke(obj, null), propertyInfo.PropertyType);
                        object[] array3 = { obj2, text2 };
                        if (!(bool)methodInfo2.Invoke(typeFormatter, array3))
                            flag = false;
                        else
                            propertyInfo.SetValue(obj, array3[0], BindingFlags.Instance | BindingFlags.SetProperty, null, null, null);
                    }
                }
                catch (Exception ex2)
                {
                    Log.Debug("[ScienceAlert]:Exception while deserializing property '{0}': {1}", propertyInfo.Name, ex2);
                    flag = false;
                }
            }
            if (obj is IReeperSerializable)
                ((IReeperSerializable)obj).OnDeserialize(node);
            return flag && objectFields.Count() > 0 || obj is IReeperSerializable;
        }

        private static FieldInfo[] GetObjectFields(object obj)
        {
            return (from fi in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
            where !fi.GetCustomAttributes(false).Any(attr => attr is CompilerGeneratedAttribute || attr is NonSerializedAttribute || attr is DoNotSerialize)
            select fi).ToArray();
        }

        private static PropertyInfo[] GetObjectProperties(object obj)
        {
            return obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Where(delegate(PropertyInfo pi)
            {
                if (pi.GetGetMethod(true) != null && pi.GetSetMethod(true) != null)
                    return !pi.GetCustomAttributes(true).Any(attr => attr is DoNotSerialize || attr is NonSerializedAttribute);
                return false;
            }).ToArray();
        }

        private static void WriteValue(ConfigNode node, string valueName, string value, object[] attrs)
        {
            if (attrs == null) attrs = new object[0];
            Subsection subsection = attrs.SingleOrDefault(attr => attr is Subsection) as Subsection;
            if (subsection != null)
            {
                if (node.HasNode(subsection.Section))
                    node = node.GetNode(subsection.Section);
                else
                    node = node.AddNode(subsection.Section);
            }
            attrs.ToList().ForEach(delegate{});
            node.AddValue(valueName, value);
        }

        private static string ReadValue(ConfigNode node, string valueName, object[] attrs)
        {
            if (attrs == null)
                attrs = new object[0];
            Subsection subsection = attrs.SingleOrDefault(attr => attr is Subsection) as Subsection;
            if (subsection != null)
            {
                if (node.HasNode(subsection.Section))
                    node = node.GetNode(subsection.Section);
            }
            return node.ReadString(valueName);
        }
    }
}
