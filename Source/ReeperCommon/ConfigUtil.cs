using System;
using System.Reflection;
using UnityEngine;

namespace ReeperCommon
{
    public static class ConfigUtil
    {
        public static T ParseEnum<T>(this ConfigNode node, string valueName, T defaultValue)
        {
            try
            {
                string value = node.GetValue(valueName);
                T result;
                if (string.IsNullOrEmpty(value))
                {
                    result = defaultValue;
                    return result;
                }
                Enum.GetValues(typeof(T));
                result = (T)Enum.Parse(typeof(T), value, true);
                return result;
            }
            catch (Exception ex)
            {
                Log.Debug("[ScienceAlert]:Settings: Failed to parse value '{0}' from ConfigNode, resulted in an exception {1}", valueName, ex);
            }
            return defaultValue;
        }

        public static string Parse(this ConfigNode node, string valueName, string defaultValue = "")
        {
            try
            {
                string result;
                if (!node.HasValue(valueName))
                {
                    result = defaultValue;
                    return result;
                }
                result = node.GetValue(valueName);
                return result;
            }
            catch (Exception ex)
            {
                Log.Debug("[ScienceAlert]:Settings: Failed to parse string value '{0}' from ConfigNode, resulted in an exception {1}", valueName, ex);
            }
            return defaultValue;
        }

        public static T Parse<T>(string value)
        {
            return Parse(value, default(T));
        }

        public static T Parse<T>(string value, T defaultValue)
        {
            try
            {
                MethodInfo method = typeof(T).GetMethod("TryParse", new[]
                {
                    typeof(string),
                    typeof(T).MakeByRefType()
                });
                if (method == null)
                {
                    Log.Debug("[ScienceAlert]:Failed to locate TryParse in {0}", typeof(T).FullName);
                }
                else
                {
                    object[] array = {
                        value,
                        default(T)
                    };
                    T result;
                    if ((bool)method.Invoke(null, array))
                    {
                        result = (T)array[1];
                        return result;
                    }
                    result = defaultValue;
                    return result;
                }
            }
            catch (Exception)
            {
                T result = defaultValue;
                return result;
            }
            return defaultValue;
        }

        public static T ParseThrowable<T>(string value)
        {
            T result;
            try
            {
                MethodInfo method = typeof(T).GetMethod("TryParse", new[]
                {
                    typeof(string),
                    typeof(T).MakeByRefType()
                });
                if (method == null)
                {
                    throw new Exception("TryParse method not found");
                }
                object[] array = {
                    value,
                    default(T)
                };
                if (!(bool)method.Invoke(null, array))
                {
                    throw new Exception("TryParse invoke reports failure");
                }
                result = (T)array[1];
            }
            catch (Exception ex)
            {
                Log.Debug("[ScienceAlert]:ConfigUtil.Parse<{0}>: Failed to parse from value '{1}': {2}", typeof(T).FullName, value, ex);
                throw;
            }
            return result;
        }

        public static T Parse<T>(this ConfigNode node, string valueName, T defaultValue)
        {
            try
            {
                T result;
                if (!node.HasValue(valueName))
                {
                    result = defaultValue;
                    return result;
                }
                string value = node.GetValue(valueName);
                result = Parse(value, defaultValue);
                return result;
            }
            catch (Exception ex)
            {
                Log.Debug("[ScienceAlert]:ConfigUtil.Parse<{0}>: Exception while parsing a value named {1}: {2}", typeof(T).FullName, valueName, ex);
            }
            return defaultValue;
        }

        public static string ReadString(this ConfigNode node, string valueName, string defaultValue = "")
        {
            if (node == null || !node.HasValue(valueName))
            {
                return defaultValue;
            }
            return node.GetValue(valueName);
        }

        public static void Set(this ConfigNode node, string valueName, string value)
        {
            if (node.HasValue(valueName))
            {
                node.SetValue(valueName, value);
                return;
            }
            node.AddValue(valueName, value);
        }

        public static void Set<T>(this ConfigNode node, string valueName, T value)
        {
            node.Set(valueName, value.ToString());
        }

        public static string GetDllDirectoryPath()
        {
            return System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static string GetRelativeToGameData(string path)
        {
            if (!path.Contains("GameData"))
            {
                Log.Debug(
                    $"GetRelativeToGameData: Given path '{path}' does not reside in GameData.  The plugin does not appear to be installed correctly.");
                throw new FormatException($"GetRelativeToGameData: path '{path}' does not contain 'GameData'");
            }
            int num = path.IndexOf("GameData");
            string text = "";
            if (path.Length > num + "GameData".Length + 1)
            {
                text = path.Substring(num + "GameData".Length + 1);
            }
            return text;
        }

        public static Rect ReadRect(this ConfigNode node, string name, Rect defaultValue = default(Rect))
        {
            if (node.HasValue(name))
            {
                try
                {
                    Vector4 vector = KSPUtil.ParseVector4(node.GetValue(name));
                    return new Rect(vector.x, vector.y, vector.z, vector.w);
                }
                catch (Exception ex)
                {
                    Log.Debug("[ScienceAlert]:ConfigUtil.ReadRect: exception while reading value '{0}': {1}", name, ex);
                }
                return defaultValue;
            }
            return defaultValue;
        }

        public static Vector4 AsVector(this Rect rect)
        {
            return new Vector4(rect.x, rect.y, rect.width, rect.height);
        }
    }
}
