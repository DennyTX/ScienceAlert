using UnityEngine;

namespace ReeperCommon
{
	internal class PlayableSound
	{
		public AudioClip clip;

		public string shortName = "";

		public float nextPlayableTime;

		internal PlayableSound(AudioClip aclip)
		{
			clip = aclip;
			nextPlayableTime = 0f;
			shortName = GetShortName(aclip.name);
		}

		public static string GetShortName(string name)
		{
			if (name.Contains("/"))
			{
				int num = name.LastIndexOf('/');
				if (num >= 0)
				{
					return name.Substring(num + 1);
				}
			}
			return name;
		}
	}
}
