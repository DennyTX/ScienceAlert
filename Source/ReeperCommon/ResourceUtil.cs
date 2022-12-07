using System.IO;
using UnityEngine;
using ToolbarControl_NS;

namespace ReeperCommon
{
    public static class ResourceUtil
    {
        public static bool SaveToDisk(this Texture2D texture, string pathInGameData)
        {
            System.Collections.Generic.List<TextureFormat> list = new System.Collections.Generic.List<TextureFormat>
            {
                TextureFormat.Alpha8,
                TextureFormat.RGB24,
                TextureFormat.RGBA32,
                TextureFormat.ARGB32
            };
            if (!list.Contains(texture.format))
            {
                return texture.CreateReadable().SaveToDisk(pathInGameData);
            }
            if (pathInGameData.StartsWith("/"))
            {
                pathInGameData = pathInGameData.Substring(1);
            }
            pathInGameData = "/GameData/" + pathInGameData;
            if (!pathInGameData.EndsWith(".png"))
            {
                pathInGameData += ".png";
            }
            bool result;
            try
            {
                FileStream output = new FileStream(KSPUtil.ApplicationRootPath + pathInGameData, FileMode.OpenOrCreate, FileAccess.Write);
                BinaryWriter binaryWriter = new BinaryWriter(output);
                binaryWriter.Write(texture.EncodeToPNG());
                result = true;
            }
            catch (System.Exception)
            {
                result = false;
            }
            return result;
        }

        public static Texture2D as2D(this Texture tex)
        {
            return tex as Texture2D;
        }

        public static Texture2D LoadImage(string textureName, bool relativeToGameData = true)
        {
            if (relativeToGameData)
                textureName = KSPUtil.ApplicationRootPath + "GameData/ScienceAlert/PluginData/Textures/" + textureName;
            var texture2D = new Texture2D(2, 2);
            if (!ToolbarControl.LoadImageFromFile(ref texture2D, textureName))
                Log.Debug("[ScienceAlert]:Failed to find texture '{0}'", textureName);
            return texture2D;
        }

        public static void FlipTexture(Texture2D tex, bool horizontal, bool vertical)
        {
            Color32[] pixels = tex.GetPixels32();
            Color32[] array = new Color32[pixels.Length];
            for (int i = 0; i < tex.height; i++)
            {
                for (int j = 0; j < tex.width; j++)
                {
                    int num = (vertical ? tex.height - i - 1 : i) * tex.width + (horizontal ? tex.width - j - 1 : j);
                    array[i * tex.width + j] = pixels[num];
                }
            }
            tex.SetPixels32(array);
            tex.Apply();
        }

        public static Texture2D CreateReadable(this Texture2D original)
        {
            if (original.width == 0 || original.height == 0)
            {
                throw new System.Exception("CreateReadable: Original has zero width or height or both");
            }
            Texture2D texture2D = new Texture2D(original.width, original.height);
            RenderTexture temporary = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 1);
            Graphics.Blit(original, temporary);
            RenderTexture.active = temporary;
            texture2D.ReadPixels(new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), 0, 0);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(temporary);
            return texture2D;
        }

        public static Texture2D Cutout(this Texture2D source, Rect src, bool rectIsInUV = false)
        {
            Rect src2 = new Rect(src);
            if (rectIsInUV)
            {
                src2.x *= (float)source.width;
                src2.width *= (float)source.width;
                src2.y *= (float)source.height;
                src2.height *= (float)source.height;
            }
            return Cutout_Internal(source, src2);
        }

        public static Texture2D Cutout(this Renderer renderer, Rect uv)
        {
            return ((Texture2D)renderer.sharedMaterial.mainTexture).Cutout(uv, true);
        }

        private static Texture2D Cutout_Internal(Texture2D source, Rect src, bool secondAttempt = false)
        {
            Texture2D texture2D = new Texture2D(Mathf.FloorToInt(src.width), Mathf.FloorToInt(src.height), TextureFormat.ARGB32, false);
            Texture2D result;
            try
            {
                Color[] pixels = source.GetPixels(Mathf.FloorToInt(src.x), Mathf.FloorToInt(src.y), Mathf.FloorToInt(src.width), Mathf.FloorToInt(src.height));
                texture2D.SetPixels(pixels);
                texture2D.Apply();
                result = texture2D;
            }
            catch (System.Exception)
            {
                result = secondAttempt ? null : Cutout_Internal(source.CreateReadable(), src, true);
            }
            return result;
        }

        public static void GenerateRandom(this Texture2D tex)
        {
            Color32[] pixels = tex.GetPixels32();
            for (int i = 0; i < tex.height; i++)
            {
                for (int j = 0; j < tex.width; j++)
                {
                    pixels[i * tex.width + j] = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
        }

        public static Texture2D GenerateRandom(int w, int h)
        {
            Texture2D texture2D = new Texture2D(w, h, TextureFormat.ARGB32, false);
            texture2D.GenerateRandom();
            return texture2D;
        }
    }
}
