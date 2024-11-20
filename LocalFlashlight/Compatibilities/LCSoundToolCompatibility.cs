using LCSoundTool;
using System;
using UnityEngine;

namespace LocalFlashlight.Compatibilities
{
    internal class LCSoundToolCompatibility
    {
        //folder: BepInEx/plugins/Command293-LocalFlashlight/customsounds

        public static AudioClip LoadCustomSound(string soundName)
        {
            AudioClip tempClip;

            try
            {
                Plugin.mls.LogDebug("checking for file.");
                tempClip = SoundTool.GetAudioClip("Command293-LocalFlashlight/customsounds", $"{soundName}.mp3");
                if (tempClip != null)
                {
                    return tempClip;
                }
            }
            catch (Exception)
            {
                try
                {
                    Plugin.mls.LogDebug("no mp3 file, checking wav");
                    tempClip = SoundTool.GetAudioClip("Command293-LocalFlashlight/customsounds", $"{soundName}.wav");
                    if (tempClip != null)
                    {
                        return tempClip;
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        Plugin.mls.LogDebug("no wav file, checking ogg");
                        tempClip = SoundTool.GetAudioClip("Command293-LocalFlashlight/customsounds", $"{soundName}.ogg");
                        if (tempClip != null)
                        {
                            return tempClip;
                        }
                    }
                    catch (Exception)
                    {
                        Plugin.mls.LogError($"No custom sound found. Is the sound in the \"BepInEx/plugins/Command293-LocalFlashlight/customsounds\" folder? (sound name: {soundName})");
                        return null;
                    }
                }
            }
            return tempClip;
            //if it works it works, will change it later if i ever update this
        }
    }
}
