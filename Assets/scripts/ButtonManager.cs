using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    private ScrollMenu scrollMenu;
    private GameObject settingMenu;

    private Text textForPointer;

    private static ButtonManager Instance = null;
    public static ButtonManager instance
    {
        get
        {
            return Instance;
        }
    }

    void Awake()
    {
        if (Instance)
        {
            DestroyImmediate(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PointEntered(Text text)
    {
        textForPointer = text;
        text.color = Color.black;
    }

    public void PointerExited(Text text)
    {
        textForPointer = text;
        text.color = Color.white;
    }

    public void PointerClicked()
    {
        if (textForPointer != null) textForPointer.color = Color.white;
        GameManager.instance.effectSounds[1].Play();
    }

    public void StartButtonClicked()
    {
        GameManager.instance.effectSounds[0].Play();
        GameManager.instance.StartCoroutine(GameManager.instance.DelayForLoading(1, false));
    }

    public void RestartButtonClicked()
    {
        Time.timeScale = 1.0f;
        DOTween.timeScale = 1.0f;
        GameManager.instance.effectSounds[1].Play();
        if (GameManager.instance.isStarted)
        {
            if (GameManager.instance.thisScreen != null && GameManager.instance.thisScreen.activeSelf) GameManager.instance.thisScreen.SetActive(false);
            GameManager.instance.Initialize();
        }
        else GameManager.instance.StartCoroutine(GameManager.instance.DelayForLoading(SceneManager.sceneCountInBuildSettings - 1, true));
    }

    public void NextLevelButtonClicked()
    {
        GameManager.instance.effectSounds[1].Play();
        if (GameManager.instance.CheckLevels(GameManager.instance.currentLevel + 1))
        {
            GameManager.instance.currentLevel++;
            if (GameManager.instance.currentLevel == GameManager.instance.LockedStageNum) GameManager.instance.LockedStageNum++;
            GameManager.instance.StartCoroutine(GameManager.instance.DelayForLoading(SceneManager.sceneCountInBuildSettings - 1, true));
        }
    }

    public void NormalSpeedButtonClicked()
    {
        GameManager.instance.effectSounds[0].Play();
        if (GameManager.instance.isStarted == false)
        {
            if (GameManager.instance.IsScheduleDone())
            {
                if (GameManager.instance.currentLevel == 0) GameManager.instance.tutorialAnim.gameObject.SetActive(false);
                GameManager.instance.isStarted = true;
                ColorInBar.Vacate();
                GameManager.instance.CalculateSchedule();
                GameManager.instance.DeepCopyResourceComponents();

                GameManager.instance.StartedGamePanel.SetActive(true);

                GameManager.instance.gameProcessCoroutine = GameManager.instance.StartCoroutine(GameManager.instance.ShowProcess());
            }
        }
        if (Time.timeScale == 0) Time.timeScale = 1;
        DOTween.timeScale = 1.0f;
    }

    public void FastSpeedButtonClicked()
    {
        GameManager.instance.effectSounds[0].Play();
        if (Time.timeScale == 0) Time.timeScale = 1;
        DOTween.timeScale = 3.5f;
    }

    public void PauseButtonClicked()
    {
        GameManager.instance.effectSounds[0].Play();
        GameManager.instance.isPaused = true;
        Time.timeScale = 0;
    }

    public void CloseButtonClicked()
    {
        GameManager.instance.effectSounds[1].Play();
        GameManager.instance.thisScreen.SetActive(false);
        GameManager.instance.StartedGamePanel.SetActive(false);
        GameManager.instance.playButton.SetActive(false);
        GameManager.instance.restartButton.SetActive(true);
    }

    public void StageMoveLeftButtonClicked()
    {
        PointerClicked();
        if (instance.scrollMenu == null) instance.scrollMenu = GameObject.FindGameObjectWithTag("ScrollMenu").GetComponent<ScrollMenu>();
        if (ScrollMenu.scroll_pos - ScrollMenu.distance > instance.scrollMenu.pos[0] - (ScrollMenu.distance / 2))
        {
            ScrollMenu.scroll_pos -= ScrollMenu.distance;
        }
    }

    public void StageMoveRightButtonClicked()
    {
        PointerClicked();
        if (instance.scrollMenu == null) instance.scrollMenu = GameObject.FindGameObjectWithTag("ScrollMenu").GetComponent<ScrollMenu>();
        if (ScrollMenu.scroll_pos + ScrollMenu.distance < instance.scrollMenu.pos[instance.scrollMenu.pos.Count - 1] + (ScrollMenu.distance / 2))
        {
            ScrollMenu.scroll_pos += ScrollMenu.distance;
        }
    }

    public void StageButtonClicked()
    {
        GameManager.instance.effectSounds[0].Play();
        GameManager.instance.currentLevel = (SceneManager.GetActiveScene().buildIndex - 2) * 10 + int.Parse(EventSystem.current.currentSelectedGameObject.transform.GetChild(0).GetComponent<Text>().text) - 1;
        GameManager.instance.StartCoroutine(GameManager.instance.DelayForLoading(SceneManager.sceneCountInBuildSettings - 1, true));
    }

    public void SettingButtonClicked()
    {
        instance.settingMenu = GameObject.FindGameObjectWithTag("Setting");

        instance.settingMenu.transform.GetChild(0).gameObject.SetActive(true);
        if (SceneManager.GetActiveScene().buildIndex == SceneManager.sceneCountInBuildSettings - 1)
        {
            instance.settingMenu.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>().text = Chapter.thisChapterNum.ToString() + " - " + (GameManager.instance.currentLevel % 10 + 1).ToString();
        }
        GameManager.instance.isSettingOn = true;
    }

    public void ContinueButtonClicked()
    {
        if (instance.settingMenu == null) return;
        instance.settingMenu.transform.GetChild(0).gameObject.SetActive(false);
        GameManager.instance.isSettingOn = false;
    }

    public void GotoChapterScene()
    {
        GameManager.instance.effectSounds[1].Play();
        SceneManager.LoadScene(1);
    }

    public void BGMOnOff(Toggle toggle)
    {
        if (toggle.isOn) GameManager.instance.backgroundMusic.Play();
        else GameManager.instance.backgroundMusic.Pause();
        SettingManager.BGMisOn = toggle.isOn;
    }

    public void EffectVolumeOnOff(Toggle toggle)
    {
        List<AudioSource> effectSounds = GameManager.instance.effectSounds;
        if(toggle.isOn)
        {
            for(int i = 0; i < effectSounds.Count; i++)
            {
                effectSounds[i].volume = SettingManager.EffectValue;
            }
        }
        else
        {
            for(int i = 0; i < effectSounds.Count; i++)
            {
                effectSounds[i].volume = 0;
            }
        }
        SettingManager.EffectisOn = toggle.isOn;
    }

    public void CreditPanelOn()
    {
        GameManager.instance.isCreditPanelOn = true;
        instance.settingMenu.transform.GetChild(0).GetComponent<SettingManager>().creditPanel.SetActive(true);
    }

    public void CreditPanelOff()
    {
        GameManager.instance.isCreditPanelOn = false;
        instance.settingMenu.transform.GetChild(0).GetComponent<SettingManager>().creditPanel.SetActive(false);
    }

    public void ChapterButtonClicked()
    {
        Chapter thisChapter = EventSystem.current.currentSelectedGameObject.GetComponent<Chapter>();
        if (thisChapter.chapterNum != Chapter.thisChapterNum) return;
        GameManager.instance.effectSounds[0].Play();
        if (thisChapter.isLocked == false)
        {
            GameManager.instance.StartCoroutine(GameManager.instance.DelayForLoading(thisChapter.chapterNum + 2, false));
        }
    }

    public void ExitToMenu()
    {
        GameManager.instance.effectSounds[1].Play();
        DOTween.KillAll();
        StopAllCoroutines();
        GameManager.instance.isStarted = false;
        GameManager.instance.isSettingOn = false;
        Chapter.thisChapterNum = GameManager.instance.currentLevel / 10;
        ScrollMenu.scroll_pos = ScrollMenu.distance * Chapter.thisChapterNum;
        GameManager.instance.StartCoroutine(GameManager.instance.DelayForLoading(Chapter.thisChapterNum + 2, false));
    }

    public void ExitToDesktop()
    {
        DOTween.KillAll();
        Application.Quit();
    }
}
