using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SaveLoadFile
{
    private const string FILENAME = "/deadlock.sav";

    public static void Save()
    {
        SteamCloudPrefs steamCloudPrefs = new SteamCloudPrefs();
        BinaryFormatter bf = new BinaryFormatter();
        FileStream stream = new FileStream(Application.persistentDataPath + FILENAME, FileMode.Create);

        steamCloudPrefs.lockedStageNum = GameManager.instance.LockedStageNum;
        steamCloudPrefs.finishedStage = GameManager.instance.finishedStage;

        steamCloudPrefs.fullscreenIsOn = SettingManager.fullscreenIsOn;
        steamCloudPrefs.resolutionIndex = SettingManager.resolutionIndex;
        steamCloudPrefs.BGMValue = SettingManager.BGMValue;
        steamCloudPrefs.EffectValue = SettingManager.EffectValue;
        steamCloudPrefs.BGMisOn = SettingManager.BGMisOn;
        steamCloudPrefs.EffectisOn = SettingManager.EffectisOn;

        bf.Serialize(stream, steamCloudPrefs);
        stream.Close();
    }

    public static SteamCloudPrefs Load()
    {
        if (File.Exists(Application.persistentDataPath + FILENAME))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream stream = new FileStream(Application.persistentDataPath + FILENAME, FileMode.Open);

            SteamCloudPrefs data = bf.Deserialize(stream) as SteamCloudPrefs;

            GameManager.instance.LockedStageNum = data.lockedStageNum;
            if (data.finishedStage.Length == 5) GameManager.instance.finishedStage = new int[7];
            else GameManager.instance.finishedStage = data.finishedStage;

            SettingManager.fullscreenIsOn = data.fullscreenIsOn;
            SettingManager.resolutionIndex = data.resolutionIndex;
            SettingManager.BGMValue = data.BGMValue;
            SettingManager.EffectValue = data.EffectValue;
            SettingManager.BGMisOn = data.BGMisOn;
            SettingManager.EffectisOn = data.EffectisOn;

            switch (SettingManager.resolutionIndex)
            {
                case 0:
                    Screen.SetResolution(1280, 720, SettingManager.fullscreenIsOn);
                    break;
                case 1:
                    Screen.SetResolution(1920, 1080, SettingManager.fullscreenIsOn);
                    break;
                case 2:
                    Screen.SetResolution(2560, 1440, SettingManager.fullscreenIsOn);
                    break;
                case 3:
                    Screen.SetResolution(3840, 2160, SettingManager.fullscreenIsOn);
                    break;
            }

            GameManager.instance.backgroundMusic.volume = SettingManager.BGMValue;
            if (SettingManager.BGMisOn == false) GameManager.instance.backgroundMusic.volume = 0;

            for (int i = 0; i < GameManager.instance.effectSounds.Count; i++)
            {
                if (SettingManager.EffectisOn == false) GameManager.instance.effectSounds[i].volume = 0;
                else GameManager.instance.effectSounds[i].volume = SettingManager.EffectValue;
            }

            stream.Close();

            return data;
        }
        else return null;
    }
}

[Serializable]
public class SteamCloudPrefs
{
    public int lockedStageNum;
    public int[] finishedStage;
    
    //Setting
    public bool fullscreenIsOn;
    public int resolutionIndex;
    public float BGMValue;
    public float EffectValue;
    public bool BGMisOn;
    public bool EffectisOn;
}
