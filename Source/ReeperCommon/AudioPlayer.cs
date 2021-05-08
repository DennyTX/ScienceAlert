using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ReeperCommon
{
    internal class AudioPlayer : MonoBehaviour
    {
        private static AudioPlayer _instance;
        private readonly Dictionary<string, PlayableSound> sounds = new Dictionary<string, PlayableSound>();
        private AudioSource source;

        public static AudioPlayer Audio
        {
            get
            {
                if (_instance != null) return _instance;
                GameObject gameObject = new GameObject("Reeper.AudioPlayer", typeof(AudioSource));
                gameObject.AddComponent<AudioPlayer>().SetSource(gameObject);
                return _instance;
            }
        }

        public int Count => sounds.Count;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void SetSource(GameObject src, bool b2d = true)
        {
            source = src.GetComponent<AudioSource>() ?? src.AddComponent<AudioSource>();
        }

        public int LoadSoundsFrom(string dir, bool b2D = true)
        {
            int counter = 0;
            if (System.IO.Path.IsPathRooted(dir) && System.IO.Directory.Exists(dir))
            {
                dir = System.IO.Path.GetFullPath(dir).Replace('\\', '/');
                dir = ConfigUtil.GetRelativeToGameData(dir);
            }
            else
            {
                dir = dir.TrimStart('\\', '/');
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(System.IO.Path.GetFullPath(KSPUtil.ApplicationRootPath + "GameData"), dir)))
                {
                    string text = System.IO.Path.Combine(ConfigUtil.GetDllDirectoryPath(), dir);
                    if (!System.IO.Directory.Exists(text))
                    {
                        Log.Debug("[ScienceAlert]:AudioPlayer: Couldn't find '{0}'", dir);
                        return 0;
                    }
                    dir = ConfigUtil.GetRelativeToGameData(text).Replace('\\', '/');
                }
                else
                {
                    dir = dir.Replace('\\', '/');
                }
            }
            GameDatabase.Instance.databaseAudio.ForEach(delegate(AudioClip ac)
            {
                string text2 = ac.name;
                int num = text2.LastIndexOf('/');
                if (num >= 0)
                {
                    text2 = text2.Substring(0, num);
                }

                if (!string.Equals(text2, dir)) return;
                if (sounds.ContainsKey(ac.name))return;
                sounds.Add(ac.name, new PlayableSound(ac));
                counter++;
            });
            if (counter == 0)
            {
                Log.Warning("AudioPlayer: Didn't load any sounds from directory '{0}'", dir);
            }
            return counter;
        }

        public bool PlayThenDelay(string name, float delay = 1f)
        {
            return Play(name, 1f, delay);
        }

        public bool PlayUI(string name, float delay = 0f)
        {
            return Play(name, GameSettings.UI_VOLUME, delay);
        }

        public bool Play(string name, float volume = 1f, float delay = 0f)
        {
            PlayableSound playableSound = null;
            if (sounds.ContainsKey(name))
            {
                playableSound = sounds[name];
            }
            else
            {
                string text = sounds.Keys.ToList<string>().SingleOrDefault((string k) => string.Equals(PlayableSound.GetShortName(k), name));
                if (!string.IsNullOrEmpty(text) && sounds.ContainsKey(text))
                {
                    playableSound = sounds[text];
                }
            }
            if (playableSound == null) return false;

            if (!(Time.realtimeSinceStartup - playableSound.nextPlayableTime > 0f))  return false;

            if (source == null)
            {
                SetSource(gameObject);
            }
            try
            {
                source.PlayOneShot(playableSound.clip, Mathf.Clamp(volume, 0f, 1f));
                playableSound.nextPlayableTime = Time.realtimeSinceStartup + delay;
                bool result = true;
                return result;
            } 
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
