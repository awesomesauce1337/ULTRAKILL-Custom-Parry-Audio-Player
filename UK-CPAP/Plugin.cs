﻿using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using BepInEx.Logging;


// frankensteining https://github.com/GBRodrickTed/AnyParry/blob/main/AnyParry/Plugin.cs and https://github.com/The-Graze/I-Wont-Sugar-Coat-It-UK/blob/master/IWontSugarCoatIt/Plugin.cs

namespace ultrakillParrySoundAndAudioReplacer
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        
 
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Lets make some noise!, {Assets.AudioPath}");

            HarmonyPatches.ApplyHarmonyPatches();
        }
    }

    [HarmonyPatch(typeof(HUDOptions))]
    [HarmonyPatch("Start", MethodType.Normal)]
    internal class HudPatch
    {
        private static void Postfix(HUDOptions __instance)
        {
            if (__instance.GetComponent<AudioReplacer>() == null)
            {
                __instance.gameObject.AddComponent<AudioReplacer>();
            }
        }
    }

    public static class SoundConverter
    {
        public static int FindBitDepth(byte[] ByteArray)
        {
            byte[] WorkingArray = new byte[2];
            for (int i = 0; i < 2; i++)
            {

                byte WorkingByte = ByteArray[i + 34];
                WorkingArray[i] = WorkingByte;
                WorkingArray[WorkingArray.Length - 1] = WorkingByte;
            }
            int CombinedBytes = BitConverter.ToInt16(WorkingArray, 0);
            return CombinedBytes;
        }
        public static float[] ConvertByteToFloat(byte[] byteArray)
        {
            const int headerSize = 44; // WAV header size
            int floatCount = (byteArray.Length - headerSize) / sizeof(short); // Assuming 16-bit PCM
            float[] floatArray = new float[floatCount];
            int BitDepth = FindBitDepth(byteArray);
            //Console.WriteLine(BitDepth);
            if (BitDepth == 16)
            {
                for (int i = 0; i < floatCount; i++)
                {
                    short sample = BitConverter.ToInt16(byteArray, headerSize + i * sizeof(short));
                    floatArray[i] = sample / 32768f; // Convert to float
                }
            }
            else if (BitDepth == 32)
            {
                for (int i = 0; i < floatCount; i++)
                {
                    int sample = BitConverter.ToInt32(byteArray, headerSize + i * sizeof(int));
                    floatArray[i] = sample / 32768f; // Convert to float
                }
            }
            
            return floatArray;
        }
        public static int FindFrequency(byte[] ByteArray)
        {
            byte[] WorkingArray = new byte[4];
            for (int i = 0; i < 3; i++)
            {
                byte WorkingByte = ByteArray[i + 24];
                WorkingArray[i] = WorkingByte;
                WorkingArray[WorkingArray.Length - 1] = WorkingByte;
            }
            int CombinedBytes = BitConverter.ToInt32(WorkingArray, 0);
            //Console.WriteLine(CombinedBytes);
            return CombinedBytes * 2; //audio plays slow, brute forcing frequency higher fixes
        }
        
    }

    public static class Assets // taken from https://stackoverflow.com/q/16078254 and https://stackoverflow.com/a/16180762
    {
        

        static string ModDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string AudioPath = Path.Combine(ModDir, "ParryNoise");
        static byte[] RawAudio = File.ReadAllBytes(Path.Combine(AudioPath, "parry.wav"));

        

        static float[] FloatArray = SoundConverter.ConvertByteToFloat(RawAudio);
        static int Frequency = SoundConverter.FindFrequency(RawAudio);

        public static AudioClip AudioParryClip = AudioClip.Create("ParryNoise", FloatArray.Length, 1, Frequency, false);

        static Assets()
        {
            AudioParryClip.SetData(FloatArray, 0);
        }
    }
    public class AudioReplacer : MonoBehaviour
    {
        GameObject AudioClipObject;
        GameObject ParryFlash;
        AudioSource AudioSource;


        internal static ManualLogSource Logger;
        private void Awake()
        {
            //Console.WriteLine("Audio making");
            if (ParryFlash == null)
            {
                
                //Console.WriteLine("Audio making 1");
                ParryFlash = transform.Find("ParryFlash").gameObject;
                AudioClipObject = new GameObject("Audio Clip");
                AudioClipObject.SetActive(true);
                //Console.WriteLine("Audio making 2");



                AudioSource = AudioClipObject.AddComponent<AudioSource>();
                AudioSource.playOnAwake = false;
                AudioSource.spatialBlend = 0f; // Set to 0 for 2D sound
                AudioSource.volume = 1f; // Set volume level as needed

                

                if (Assets.AudioParryClip != null)
                {
                    AudioSource.clip = Assets.AudioParryClip;
                    //Console.WriteLine("Audio making 2");
                    AudioClipObject.transform.SetParent(gameObject.transform);
                    //Console.WriteLine("Audio made");
                }
            }
            
        }
        private void Update()
        {
            
                if (ParryFlash != null && ParryFlash.activeSelf)
                {
                    if (AudioClipObject == null)
                    {
                        Debug.LogError("AudioClipObject is NULL!");
                        return;
                    }

                    if (AudioSource == null)
                    {
                        Debug.LogError("AudioSource is NULL!");
                        return;
                    }

                    if (!AudioSource.isPlaying) // Check if audio is not already playing to avoid overlapping
                    {
                        AudioSource.Play();
                        Debug.Log("Parry sound played");
                    }
                    else
                    {
                        Debug.Log("Audio is already playing!");
                    }
                }
        }
    }
}
    

