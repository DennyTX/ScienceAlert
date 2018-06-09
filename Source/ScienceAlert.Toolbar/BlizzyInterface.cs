#if false
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ReeperCommon;
using ToolbarControl_NS;

namespace ScienceAlert.Toolbar
{
    class BlizzyInterface : MonoBehaviour, IToolbar
    {
        private const string NormalFlaskTexture = "ScienceAlert/textures/flask";
        private List<string> StarFlaskTextures = new List<string>();
        private float FrameRate = 24f;
        private int FrameCount = 100;
        private int CurrentFrame = 0;

        //IButton button;
        ToolbarControl toolbarControl;
        IEnumerator animation;

        public event ToolbarClickHandler OnClick;

        void Start()
        {
            SliceAtlasTexture();
            button = ToolbarManager.Instance.add("ScienceAlert", "PopupOpen");
            button.Text = "Science Alert";
            button.ToolTip = "Left-click to view alert experiments; Right-click for settings";
            button.TexturePath = NormalFlaskTexture;
            button.OnClick += ce => {
                OnClick(new ClickInfo { button = ce.MouseButton, used = false });
            };
            FrameRate = Settings.Instance.StarFlaskFrameRate;
        }

        private void SliceAtlasTexture()
        {
            Func<int, int, string> GetFrame = delegate (int frame, int desiredLen)
            {
                string str = frame.ToString();
                while (str.Length < desiredLen)
                    str = "0" + str;
                return str;
            };

            // load textures
            try
            {
                if (!GameDatabase.Instance.ExistsTexture(NormalFlaskTexture))
                {
                    // load normal flask texture
                    Log.Debug("Loading normal flask texture");

                    Texture2D nflask = ResourceUtil.GetEmbeddedTexture("Textures.flask", true);
                    if (nflask == null)
                    {
                        Log.Error("Failed to create normal flask texture!");
                    }
                    else
                    {
                        GameDatabase.TextureInfo ti = new GameDatabase.TextureInfo(null, nflask, false, true, true);
                        ti.name = NormalFlaskTexture;
                        GameDatabase.Instance.databaseTexture.Add(ti);
                        Log.Debug("Created normal flask texture {0}", ti.name);
                    }

                    Texture2D sheet = ResourceUtil.GetEmbeddedTexture("Textures.sheet.png");
                    if (sheet == null)
                    {
                        Log.Error("Failed to create sprite sheet texture!");
                    }
                    else
                    {
                        var rt = RenderTexture.GetTemporary(sheet.width, sheet.height);
                        var oldRt = RenderTexture.active;
                        int invertHeight = ((FrameCount - 1) / (sheet.width / 24)) * 24;

                        Graphics.Blit(sheet, rt);
                        RenderTexture.active = rt;

                        for (int i = 0; i < FrameCount; ++i)
                        {
                            StarFlaskTextures.Add(NormalFlaskTexture + GetFrame(i + 1, 4));
                            Texture2D sliced = new Texture2D(24, 24, TextureFormat.ARGB32, false);

                            sliced.ReadPixels(new Rect((i % (sheet.width / 24)) * 24, /*invertHeight -*/ (i / (sheet.width / 24)) * 24, 24, 24), 0, 0);
                            sliced.Apply();

                            GameDatabase.TextureInfo ti = new GameDatabase.TextureInfo(null, sliced, false, false, false);
                            ti.name = StarFlaskTextures.Last();

                            GameDatabase.Instance.databaseTexture.Add(ti);
                            Log.Debug("Added sheet texture {0}", ti.name);
                        }

                        RenderTexture.active = oldRt;
                        RenderTexture.ReleaseTemporary(rt);
                    }
                    Log.Debug("Finished loading sprite sheet textures.");
                }
                else
                { // textures already loaded
                    for (int i = 0; i < FrameCount; ++i)
                        StarFlaskTextures.Add(NormalFlaskTexture + GetFrame(i + 1, 4));
                }
            }
            catch (Exception e)
            {
                Log.Error("Failed to load textures: {0}", e);
            }
        }

        /// <summary>
        /// Normal cleanup
        /// </summary>
        void OnDestroy()
        {
            //Log.Verbose("Destroying BlizzyInterface");
            button.Destroy();
        }

        /// <summary>
        /// Begins playing the "star flask" animation, used when a new 
        /// experiment has become available.
        /// </summary>
        public void PlayAnimation()
        {
            if (animation == null) animation = DoAnimation();
        }

        /// <summary>
        /// Stops playing animation (but leaves the current frame state)
        /// </summary>
        public void StopAnimation()
        {
            animation = null;
        }

        /// <summary>
        /// Switch to normal flask texture
        /// </summary>
        public void SetUnlit()
        {
            animation = null;
            button.TexturePath = NormalFlaskTexture;
        }

        public void SetLit()
        {
            animation = null;
            button.TexturePath = StarFlaskTextures[0];
        }

        public IDrawable Drawable
        {
            get
            {
                return button.Drawable;
            }

            set
            {
                button.Drawable = value;
            }
        }

        public bool Important
        {
            get
            {
                return button.Important;
            }

            set
            {
                button.Important = value;
            }
        }

        public bool IsAnimating => animation != null;

        public bool IsLit => animation == null && button.TexturePath != NormalFlaskTexture;

        public bool IsNormal => !IsAnimating && !IsLit;

        void Update()
        {
            if (animation != null) animation.MoveNext();
        }

        /// <summary>
        /// Is called by Update whenever animation exists to
        /// update animation frame.
        /// 
        /// Note: I didn't make this into an actual coroutine
        /// because StopCoroutine seems to sometimes throw
        /// exceptions
        /// </summary>
        /// <returns></returns>
        IEnumerator DoAnimation()
        {
            float elapsed = 0f;
            while (true)
            {
                while (elapsed < 1f / FrameRate)
                {
                    elapsed += Time.deltaTime;
                    yield return new WaitForSeconds(1f / FrameRate);
                }
                elapsed -= 1f / FrameRate;
                CurrentFrame = (CurrentFrame + 1) % FrameCount;
                button.TexturePath = StarFlaskTextures[CurrentFrame];
            }
        }
    }
}



//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using ReeperCommon;
//using UnityEngine;

//namespace ScienceAlert.Toolbar
//{
//    internal class BlizzyInterface : MonoBehaviour, IToolbar
//    {
//        private readonly List<string> StarFlaskTextures = new List<string>();
//        private float FrameRate = 24f;
//        private int FrameCount = 100;
//        private int CurrentFrame;
//        private IButton button;
//        private IEnumerator animation;
//        public event ToolbarClickHandler OnClick;

//        public IDrawable Drawable
//        {
//            get
//            {
//                return button.Drawable;
//            }
//            set
//            {
//                button.Drawable = value;
//            }
//        }

//        public bool Important
//        {
//            get
//            {
//                return button.Important;
//            }
//            set
//            {
//                button.Important = value;
//            }
//        }

//        public bool IsAnimating => animation != null;

//        public bool IsLit => animation == null && button.TexturePath != "ScienceAlert/textures/flask";

//        public bool IsNormal => !IsAnimating && !IsLit;

//        private void Start()
//        {
//            SliceAtlasTexture();
//            button = ToolbarManager.Instance.add("ScienceAlert", "PopupOpen");
//            button.Text = "Science Alert";
//            button.ToolTip = "Left-click to view alert experiments; Right-click for settings";
//            button.OnClick += delegate(ClickEvent ce)
//            {
//                OnClick(new ClickInfo{button = ce.MouseButton,used = false});
//            };
//            FrameRate = Settings.Instance.StarFlaskFrameRate;
//        }

//        private void SliceAtlasTexture()
//        {
//            Func<int, int, string> func = delegate(int frame, int desiredLen)
//            {
//                string text = frame.ToString();
//                while (text.Length < desiredLen)
//                {
//                    text = "0" + text;
//                }
//                return text;
//            };
//            try
//            {
//                if (!GameDatabase.Instance.ExistsTexture("ScienceAlert/textures/flask"))
//                {
//                    Texture2D embeddedTexture = ResourceUtil.GetEmbeddedTexture("Textures.flask.png", true);
//                    GameDatabase.TextureInfo textureInfo = new GameDatabase.TextureInfo(null, embeddedTexture, false, true, true);
//                    GameDatabase.Instance.databaseTexture.Add(textureInfo);
//                    Texture2D embeddedTexture2 = ResourceUtil.GetEmbeddedTexture("Textures.sheet.png");
//                    RenderTexture temporary = RenderTexture.GetTemporary(embeddedTexture2.width, embeddedTexture2.height);
//                    RenderTexture active = RenderTexture.active;
//                    Graphics.Blit(embeddedTexture2, temporary);
//                    RenderTexture.active = temporary;
//                    for (var i = 0; i < FrameCount; i++)
//                    {
//                        StarFlaskTextures.Add("ScienceAlert/textures/flask" + func(i + 1, 4));
//                        Texture2D texture2D = new Texture2D(24, 24, TextureFormat.ARGB32, false);
//                        texture2D.ReadPixels(new Rect(i % (embeddedTexture2.width / 24) * 24, i / (embeddedTexture2.width / 24) * 24, 24f, 24f), 0, 0);
//                        texture2D.Apply();
//                        GameDatabase.TextureInfo textureInfo2 =
//                            new GameDatabase.TextureInfo(null, texture2D, false, false, false)
//                            {
//                                name = StarFlaskTextures.Last()
//                            };
//                        GameDatabase.Instance.databaseTexture.Add(textureInfo2);
//                    }
//                    RenderTexture.active = active;
//                    RenderTexture.ReleaseTemporary(temporary);
//                }
//                else
//                {
//                    for (int j = 0; j < FrameCount; j++)
//                    {
//                        StarFlaskTextures.Add("ScienceAlert/textures/flask" + func(j + 1, 4));
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Log.Debug("[ScienceAlert]:Failed to load textures: {0}", ex);
//            }
//        }

//        private void OnDestroy()
//        {
//            Log.Debug("VERB ALERT:Destroying BlizzyInterface");
//            button.Destroy();
//        }

//        public void PlayAnimation()
//        {
//            if (animation == null)
//            {
//                animation = DoAnimation();
//            }
//        }

//        public void StopAnimation()
//        {
//            animation = null;
//        }

//        public void SetUnlit()
//        {
//            animation = null;
//        }

//        public void SetLit()
//        {
//            animation = null;
//            button.TexturePath = StarFlaskTextures[0];
//        }

//        private void Update()
//        {
//            if (animation != null)
//            {
//                animation.MoveNext();
//            }
//        }

//        private IEnumerator DoAnimation()
//        {
//            float num = 0f;
//            while (true)
//            {
//                if (num >= 1f / FrameRate)
//                {
//                    num -= 1f / FrameRate;
//                    CurrentFrame = (CurrentFrame + 1) % FrameCount;
//                    button.TexturePath = StarFlaskTextures[CurrentFrame];
//                }
//                else
//                {
//                    num += Time.deltaTime;
//                    yield return new WaitForSeconds(1f / FrameRate);
//                }
//            }
//        }
//    }
//}

#endif
