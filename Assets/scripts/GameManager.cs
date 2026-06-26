using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Steamworks;
using System.Linq;
using TMPro;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    private static GameManager Instance = null;
    public static GameManager instance
    {
        get
        {
            return Instance;
        }
    }

    public SteamCloudPrefs steamStorage = new SteamCloudPrefs();

    public GameObject Process, Resource;
    public List<LevelCreator> levelHolder = new List<LevelCreator>();

    public int LockedStageNum  = 2;
    public int[] finishedStage = new int[7];

    public GameObject canvas, successScreen, failScreen, StartedGamePanel, restartButton, playButton;

    public GameObject thisScreen;

    public bool isStarted;
    public bool isPaused;

    public bool isSettingOn;
    public bool isCreditPanelOn;
    public bool isClockOver;

    public int currentLevel = 0;
    public int doneProcessCount;

    public List<GameObject> tutorialObjects = new List<GameObject>();
    public GameObject tutorialGameObject;
    public Resource tutorialResource;
    public TutorialAnim tutorialAnim;
    public Coroutine gameProcessCoroutine;

    public AudioSource backgroundMusic;

    public List<AudioSource> effectSounds = new List<AudioSource>();
    public List<Color> backgroundColors = new List<Color>();

    private int rows, cols;
    private int finishedRound = 1;
    private float lineSpeed = 1.6f;

    private List<Steamworks.Data.Achievement> achievements;

    private Dictionary<int, List<Todo>> toDoDicQueue = new Dictionary<int, List<Todo>>();
    private List<Process> processes = new List<Process>();
    private List<List<ProcessColor>> saveListForRestart = new List<List<ProcessColor>>();
    public List<Resource> resources = new List<Resource>();

    void Awake()
    {
        if (Instance)
        {
            DestroyImmediate(gameObject);
            return;
        }
        if(((float)Screen.width / Screen.height).Equals((float)16/9) == false) Screen.SetResolution(1920, 1080, Screen.fullScreen);
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Start()
    {
        try
        {
            SteamClient.Init(1688690);
            achievements = SteamUserStats.Achievements.ToList();
        }
        catch (System.Exception e) { }

        steamStorage = SaveLoadFile.Load();
        

        backgroundMusic.Play();
        backgroundMusic.loop = true;
    }

    public IEnumerator DelayForLoading(int index, bool isGoingtoGame)
    {
        DOTween.KillAll();
        AsyncOperation async = SceneManager.LoadSceneAsync(index);
        yield return async;
        if(BackgroundAnim.instance != null)
        {
            BackgroundAnim.instance.resourceA.SetColor("_EmissionColor", BackgroundAnim.instance.materialColors[(Chapter.thisChapterNum) * 2]);
            BackgroundAnim.instance.resourceB.SetColor("_EmissionColor", BackgroundAnim.instance.materialColors[(Chapter.thisChapterNum) * 2 + 1]);
        }
        if (isGoingtoGame) StartGame();
    }

    public void StartGame()
    {
        Camera.main.backgroundColor = backgroundColors[currentLevel / 10];
        Chapter.thisChapterNum = currentLevel / 10;

        isStarted = false;
        Initialize();
        if (thisScreen != null) thisScreen.SetActive(false);
    }

    public void SystemFail()
    {
        if (isStarted)
        {
            effectSounds[4].Play();
            DOTween.timeScale = 1.0f;
            thisScreen = failScreen;
            thisScreen.SetActive(true);
        }
    }

    public void SystemSuccess()
    {
        if (isStarted)
        {
            effectSounds[3].Play();
            DOTween.timeScale = 1.0f;
            if(finishedStage[Chapter.thisChapterNum] < currentLevel % 10 + 1) finishedStage[Chapter.thisChapterNum] = currentLevel % 10 + 1;
            thisScreen = successScreen;
            if (currentLevel + 2 == LockedStageNum) LockedStageNum++;
            thisScreen.SetActive(true);

            if (currentLevel % 10 == 8 && currentLevel + 3 == LockedStageNum)
            {
                LockedStageNum++;
                thisScreen.transform.GetChild(thisScreen.transform.childCount - 1).gameObject.SetActive(true);
            }
            else if (currentLevel >= 10 && currentLevel % 10 == 9)
            {
                if (achievements[(currentLevel / 10) - 1].State == false) achievements[(currentLevel / 10) - 1].Trigger();
                bool isAllDone = true;
                for(int i = 0; i < finishedStage.Length; i++) if (finishedStage[i] != 9) isAllDone = false;
                if(isAllDone) achievements[achievements.Count - 1].Trigger();
            }

            SaveLoadFile.Save();
        }
    }

    public void Initialize()
    {
        if (isStarted)
        {
            DOTween.PauseAll();
            processes[0].SetCurrentProcessNull();
            StopAllCoroutines();
        }
        
        if (BackgroundAnim.instance != null)
        {
            DestroyImmediate(BackgroundAnim.instance.gameObject);
            BackgroundAnim.instance = null;
        }
        DOTween.timeScale = 1.0f;
        Time.timeScale = 1.0f;
        toDoDicQueue.Clear();

        if (isStarted == false)
        {
            GameObject processColors = GameObject.FindGameObjectWithTag("ProcessColor");
            if (processColors.transform.GetChild(0).gameObject.activeSelf == false)
            {
                for (int i = 0; i < processColors.transform.childCount; i++)
                {
                    processColors.transform.GetChild(i).gameObject.SetActive(true);
                }
            }

            tutorialResource = null;
        }

        isPaused = true;
        isSettingOn = false;
        isClockOver = false;

        if (canvas == null)
        {
            canvas = GameObject.FindGameObjectWithTag("Canvas");
            if (currentLevel <= 9)
            {
                canvas.transform.GetChild(0).gameObject.SetActive(true);
            }
            failScreen = canvas.transform.GetChild(canvas.transform.childCount - 2).gameObject;
            successScreen = canvas.transform.GetChild(canvas.transform.childCount - 3).gameObject;
            StartedGamePanel = canvas.transform.GetChild(canvas.transform.childCount - 4).gameObject;
            playButton = canvas.transform.GetChild(canvas.transform.childCount - 5).gameObject;
            restartButton = canvas.transform.GetChild(canvas.transform.childCount - 6).gameObject;
        }

        if (currentLevel == 0 && tutorialAnim == null)
        {
            canvas.transform.GetChild(canvas.transform.childCount - 1).gameObject.SetActive(true);
            tutorialAnim = canvas.transform.GetChild(canvas.transform.childCount - 1).GetComponent<TutorialAnim>();
        }

        playButton.SetActive(true);
        restartButton.SetActive(false);
        StartedGamePanel.SetActive(false);

        doneProcessCount = 0;
        finishedRound = 1;

        rows = levelHolder[currentLevel].row;
        cols = levelHolder[currentLevel].col;

        Camera.main.transform.position = new Vector3((float)(cols - 1) / 2, (float)(rows - 1) / 2, Camera.main.transform.position.z);
        if (rows <= 3) Camera.main.orthographicSize = 3;
        else if (cols == 5)
        {
            if (rows == 4) Camera.main.orthographicSize = 3.5f;
            else if (rows == 5) Camera.main.orthographicSize = 4f;
        }
        else if (cols == 7 && (rows == 4 || rows == 5)) Camera.main.orthographicSize = 4f;
        else if (cols == 9)
        {
            if (rows == 5) Camera.main.orthographicSize = 4f;
            else if(rows == 9) Camera.main.orthographicSize = 6f;
        }



        if (isStarted)
        {
            LoadStateForRestart();
            isStarted = false;
        }
        else CreateObjects();
    }

    public string GetTipString()
    {
        switch (currentLevel+1)
        {
            case 2:
                return "To finish a circle, you must get all the necessary squares of color.";
            case 3:
                return "All circles must be finished to succeed.\nIf there is no empty space in the square, it becomes a waiting state.";
            case 4:
                return "The number of lines accessible at once depends on the number of points in the square.";
            case 5:
                return "If a circle is in a waiting state,\nThe circle does not have access to the next square until it can have the square.";
            case 6:
                return "If the distance is different, then the closest is first.\nIf the distance is the same, the first connected is first.";
            case 7:
                return "If you place a pointer on a square, you can see the order of the circles approaching it,\nbut failure to get the square because of waiting to another square is not reflected in the order.";
            case 8:
                return "All circles start at the same time,\nand the first movement of all circles finish before the next movement begins.";
            default:
                return null;
        }
    }

    private void Update()
    {
        SteamClient.RunCallbacks();
        
        // 메뉴 씬 생기면 바꿔야됨
        if(SceneManager.GetActiveScene().buildIndex == SceneManager.sceneCountInBuildSettings - 1)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isPaused)
                {
                    ButtonManager.instance.NormalSpeedButtonClicked();
                    if (isStarted) isPaused = false;
                }
                else
                {
                    ButtonManager.instance.PauseButtonClicked();
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ButtonManager.instance.RestartButtonClicked();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameManager.instance.effectSounds[1].Play();
                if (isSettingOn) ButtonManager.instance.ContinueButtonClicked();
                else ButtonManager.instance.SettingButtonClicked();
            }
        }
        else
        {
            if(SceneManager.GetActiveScene().buildIndex == 1)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow)) ButtonManager.instance.StageMoveRightButtonClicked();
                else if (Input.GetKeyDown(KeyCode.LeftArrow)) ButtonManager.instance.StageMoveLeftButtonClicked();

                if (isSettingOn == false && Input.GetKeyDown(KeyCode.Return))
                {
                    GameManager.instance.effectSounds[0].Play();
                    if (Chapter.chapters[Chapter.thisChapterNum].isLocked == false)
                    {
                        StartCoroutine(GameManager.instance.DelayForLoading(Chapter.thisChapterNum + 2, false));
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                GameManager.instance.effectSounds[1].Play();
                if (SceneManager.GetActiveScene().buildIndex == 1)
                {
                    if (isSettingOn)
                    {
                        if (isCreditPanelOn) ButtonManager.instance.CreditPanelOff();
                        else ButtonManager.instance.ContinueButtonClicked();
                    }
                    else ButtonManager.instance.SettingButtonClicked();
                }
                else
                {
                    if (isSettingOn)
                    {
                        if (isCreditPanelOn) ButtonManager.instance.CreditPanelOff();
                        else ButtonManager.instance.ContinueButtonClicked();
                    }
                    else
                    {
                        StartCoroutine(DelayForLoading(1, false));
                    }
                }
            }
        }
    }

    public void DeepCopyResourceComponents()
    {
        for(int i = 0; i < resources.Count; i++)
        {
            resources[i].DeepCopy();
        }
    }

    public void LoadStateForRestart()
    {
        for (int i = 0; i < processes.Count; i++)
        {
            processes[i].LoadForRestart();
        }
        for(int i = 0; i < resources.Count; i++)
        {
            resources[i].ReloadComponents();
        }
        isPaused = true;
    }

    public bool CheckLevels(int currentLevel)
    {
        if (levelHolder.Count > currentLevel) return true;
        return false;
    }

    public bool IsScheduleDone()
    {
        bool isFinished = true;
        for (int i = 0; i < processes.Count; i++)
        {
            if (processes[i].CheckScheduleDone() == false) isFinished = false;
        }
        return isFinished;
    }

    public void CalculateSchedule()
    {
        for (int i = 0; i < processes.Count; i++)
        {
            processes[i].SetAllLineRendererfalse();
        }

        int count = 0;
        bool isNothingToDo = false;
        while (!isNothingToDo)
        {
            isNothingToDo = true;
            toDoDicQueue.Add(count, new List<Todo>());

            List<(Process, ProcessColor)> tempList = new List<(Process, ProcessColor)>();
            for (int i = 0; i < processes.Count; i++)
            {
                if (processes[i].GetProcessColorList().Count > count) tempList.Add((processes[i], processes[i].GetProcessColorList()[count]));
            }

            if (tempList.Count > 0)
            {
                tempList.Sort((a, b) =>
                {
                    int value = Vector3.SqrMagnitude(a.Item1.transform.position - a.Item2.r.transform.position).CompareTo(Vector3.SqrMagnitude(b.Item1.transform.position - b.Item2.r.transform.position));
                    if (value == 0)
                    {
                        return a.Item2.order.CompareTo(b.Item2.order);
                    }
                    return value;
                });
                for (int i = 0; i < tempList.Count; i++)
                {
                    Todo td = new Todo(tempList[i].Item1, tempList[i].Item2);
                    toDoDicQueue[count].Add(td);
                }
                isNothingToDo = false;
            }
            count++;
        }
    }

    public void OrderbyDistanceInRound(int roundNum)
    {
        toDoDicQueue[roundNum].Sort((a, b) => Vector3.SqrMagnitude(a.p.transform.position - a.pc.r.transform.position).CompareTo(Vector3.SqrMagnitude(b.p.transform.position - b.pc.r.transform.position)));
    }

    public IEnumerator ShowProcess()
    {
        bool isDone = false;
        int roundNum;
        for (roundNum = 0; roundNum < toDoDicQueue.Count; roundNum++)
        {
            if (isClockOver) break;

            List<Coroutine> timeCheckList = new List<Coroutine>();
            for (int i = 0; i < toDoDicQueue[roundNum].Count; i++)
            {
                Todo td = toDoDicQueue[roundNum][i];
                State state = td.pc.r.Connect(td.p, td.pc);
                if (state == State.FAULT)
                {
                    MoveToNextRound(td, roundNum, false);
                    continue;
                }
                timeCheckList.Add(StartCoroutine(DrawLine(td.p, td.pc.r, state)));
                if(state == State.RUNNING)
                {
                    td.pc.isSelected = false;
                    td.pc.isMoved = true;
                    td.p.RematchColorObjectsToCorrectPlace();
                }
            }

            for(int i = 0; i < timeCheckList.Count; i++)
            {
                yield return timeCheckList[i];
            }

            if (DOTween.timeScale == 3.5f) yield return new WaitForSeconds(0.3f);
            else yield return new WaitForSeconds(0.6f);

            if (RoundEnd(roundNum) == false)
            {
                yield return new WaitForSeconds(1f);
                isDone = true;
                SystemFail();
                break;
            }
            else if (doneProcessCount == processes.Count)
            {
                yield return new WaitForSeconds(.8f);
                isDone = true;
                SystemSuccess();
                break;
            }
        }

        // 라운드가 끝나서 움직이지는 않는데 끝나지 않는경우 한번 더 남은것들 체크
        if(isDone == false)
        {
            if (RoundEnd(roundNum) == false)
            {
                yield return new WaitForSeconds(1f);
                isDone = true;
                SystemFail();
            }
            else if (doneProcessCount == processes.Count)
            {
                yield return new WaitForSeconds(.8f);
                isDone = true;
                SystemSuccess();
            }
        }
    }

    bool RoundEnd(int roundNum)
    {
        bool allIsWaiting = true;
        bool isFinishedProcessthisRound = false;

        if (isClockOver) return false;

        for (int i = 0; i < processes.Count; i++)
        {
            if (processes[i].GetState() == State.RUNNING)
            {
                if (IsMatchFinished(processes[i]))
                {
                    if (processes[i].GetFixedNum() != 0 && processes[i].GetFixedNum() != finishedRound)
                    {
                        processes[i].transform.GetComponent<SpriteRenderer>().DOColor(new Color(.89f, 0.21f, 0.22f), 0.15f).SetLoops(2, LoopType.Yoyo);
                        return false;
                    }
                    processes[i].Finish();
                    isFinishedProcessthisRound = true;
                    List<ProcessColor> pcList = processes[i].GetProcessColorList();
                    for (int j = 0; j < pcList.Count; j++)
                    {
                        // 완료된거 선이랑 프로세스 없애고 흐리게 하기
                        processes[i].SetAllLineRendererfalse();
                        Color c = processes[i].GetComponent<SpriteRenderer>().color;
                        processes[i].GetComponent<SpriteRenderer>().DOFade(0.2f, 0.3f);
                        pcList[j].r.FinishComponents(roundNum, processes[i]);

                        (Process, ProcessColor) waitingProcessAndPC = pcList[j].r.GetWaitingPandPC();
                        if (waitingProcessAndPC.Item1 != null)
                        {
                            Todo td = new Todo(waitingProcessAndPC.Item1, waitingProcessAndPC.Item2);
                            MoveToNextRound(td, roundNum, true);
                        }
                    }
                }
                // sim resource에 걸려서 아직 대기중이면 running 상태이지만 종료를 위해 allisWaiting을 false로 해야한다.
                if (processes[i].isFinished() && processes[i].IsSimWaiting()) continue;
                allIsWaiting = false;
            }
        }
        if (isFinishedProcessthisRound)
        {
            if (FixedNumCheckAndFinishCount() == false) return false;
            effectSounds[7].Play();
        }
        if (CheckClockWaiting(roundNum) == false && allIsWaiting) return false;

        for (int i = 0; i < resources.Count; i++) resources[i].DoAfterRound(roundNum);
        return true;
    }

    bool IsMatchFinished(Process p)
    {
        if (p.isFinished())
        {
            if (p.IsSimWaiting() == false) return true;
            List<ProcessColor> pcList = p.GetProcessColorList();
            for (int i = 0; i < pcList.Count; i++)
                if(pcList[i].r.IsFinishable() == false) return false;
            return true;
        }
        return false;
    }

    bool CheckClockWaiting(int roundNum)
    {
        bool isSomethingInClock = false;
        for (int i = 0; i < processes.Count; i++)
        {
            if (processes[i].isClockWaiting) isSomethingInClock = true;
        }

        if (isSomethingInClock)
        {
            if (toDoDicQueue.Count - 1 <= roundNum) toDoDicQueue.Add(roundNum + 1, new List<Todo>());
            return true;
        }
        return false;
    }

    bool FixedNumCheckAndFinishCount()
    {
        bool isNotMatch = false;
        int count = 0;

        for (int i = 0; i < processes.Count; i++) if (processes[i].GetState() == State.TERMINATE) count++;
        finishedRound = count + 1;

        for(int i = 0; i < processes.Count; i++)
        {
            if(processes[i].GetState() != State.TERMINATE)
            {
                if(processes[i].GetFixedNum() != 0 && processes[i].GetFixedNum() <= finishedRound - 1)
                {
                    isNotMatch = true;
                    processes[i].transform.GetComponent<SpriteRenderer>().DOColor(new Color(.89f, 0.21f, 0.22f), 0.15f).SetLoops(2, LoopType.Yoyo);
                }
            }
        }

        return !isNotMatch;
    }

    public void MoveToNextRound(Todo td, int roundNum, bool isFirst)
    {
        if (toDoDicQueue.ContainsKey(roundNum + 1) == false)
        {
            toDoDicQueue.Add(roundNum + 1, new List<Todo>());
        }

        for (int i = toDoDicQueue[roundNum + 1].Count - 1; i >= 0; i--)
        {
            if (toDoDicQueue[roundNum + 1][i].p == td.p)
            {
                Todo temp = toDoDicQueue[roundNum + 1][i];
                toDoDicQueue[roundNum + 1].RemoveAt(i);
                MoveToNextRound(temp, roundNum + 1, false);
            }
        }
        toDoDicQueue[roundNum + 1].Insert(0, td);
        // first가 아니면 waiting때문에 기다리는게 아니라 waiting이 걸린 부분의 다음 부분이기 때문에 아직 실행도 안한것이므로 거리에 따른 순서 재정렬
        if (isFirst == false) OrderbyDistanceInRound(roundNum + 1);
    }

    IEnumerator DrawLine(Process P, Resource R, State state)
    {
        LineRenderer lr;
        Tween t;

        float distance = Vector3.Distance(P.pointer.transform.position, R.transform.position);
        float time = distance / lineSpeed;

        if (state == State.RUNNING)
        {
            ProcessColor pc = P.GetProcessColorAble(R);
            lr = pc.lr;
            lr.sortingOrder = P.lineSortingOrder++;
            pc.isCompleted = true;
            t = lr.transform.DOMove(R.transform.position, time).SetEase(Ease.OutQuad).OnUpdate(() =>
            {
                lr.SetPosition(0, P.transform.position);
                lr.SetPosition(1, lr.transform.position);
                P.pointer.transform.position = lr.transform.position;
                lr.enabled = true;
            });
            t.OnComplete(() =>
            {
                P.pointer.transform.DOMove(P.transform.position, 0.7f).SetEase(Ease.OutQuad);
            });
            yield return t.WaitForCompletion();
        }
        else if (state == State.WAITING)
        {
            Vector3 dist = R.GetPositionForWaiting(P);
            lr = P.GetProcessColorAble(R).lr;
            lr.sortingOrder = P.lineSortingOrder++;
            t = lr.transform.DOMove(dist, time).SetEase(Ease.OutQuad).OnUpdate(() =>
            {
                lr.SetPosition(0, P.transform.position);
                lr.SetPosition(1, lr.transform.position);
                P.pointer.transform.position = lr.transform.position;
                lr.enabled = true;
            });
            yield return t.WaitForCompletion();
        }
    }

    public void ShowConnectedNumber(Resource r)
    {
        for(int i = 0; i < r.connectedProcess.Count; i++)
        {
            SpriteRenderer sr = r.connectedProcess[i].Item1.insideNumObject.GetComponent<SpriteRenderer>();
            sr.size = new Vector2(sr.size.x + .5f, sr.size.y);

            TextMeshPro tp = r.connectedProcess[i].Item1.insideNumObject.transform.GetChild(0).GetComponent<TextMeshPro>();
            if (sr.size.x.Equals(1f)) tp.text = (i + 1).ToString();
            else tp.text = tp.text + " " + (i + 1).ToString();

            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.0f);
            sr.DOFade(1.0f, .2f);
            r.connectedProcess[i].Item1.insideNumObject.SetActive(true);
        }
    }

    public void HideConnectedNumber(Resource r)
    {
        for (int i = 0; i < r.connectedProcess.Count; i++)
        {
            SpriteRenderer sr = r.connectedProcess[i].Item1.insideNumObject.GetComponent<SpriteRenderer>();
            sr.size = new Vector2(.5f, sr.size.y);
            sr.DOFade(0.0f, .2f);
            r.connectedProcess[i].Item1.insideNumObject.SetActive(false);
        }
    }



    public void CreateObjects()
    {
        processes.Clear();
        resources.Clear();
        bool isTutorialOn = false;
        if (currentLevel % 10 == 0) isTutorialOn = true;
        int counter = 0;
        for (int i = 0; i < levelHolder[currentLevel].level.Count; i++)
        {
            if (levelHolder[currentLevel].level[i].nodeTypes != NodeTypes.Empty)
            {
                GameObject g = null;
                Node node = levelHolder[currentLevel].level[i];
                if (node.nodeTypes == NodeTypes.Process)
                {
                    g = Instantiate(Process, new Vector3(counter % cols, counter / cols, 0), Quaternion.identity);
                    Process p = g.GetComponent<Process>();
                    p.Initialize(node.fixedNum);
                    for (int j = 0; j < node.colors.Count; j++)
                    {
                        Transform child = p.transform.GetChild(j);
                        child.gameObject.SetActive(true);
                        child.GetComponent<SpriteRenderer>().color = node.colors[j];
                        p.AddColorObject(child.position, child, node.colors[j]);
                    }
                    processes.Add(p);
                }
                else if (node.nodeTypes == NodeTypes.Resource)
                {
                    g = Instantiate(Resource, new Vector3(counter % cols, counter / cols, 0), Quaternion.identity);
                    Resource r = g.GetComponent<Resource>();
                    List<IResourceComponent> components = new List<IResourceComponent>();
                    List<Color> colors = node.colors.ToList();

                    if (node.isSimul)
                    {
                        components.Add(new SimultaneousComponent(node.maxCount, g.transform.GetChild(4).GetChild(node.maxCount - 2)));
                    }
                    if (node.isSwitchColor)
                    {
                        components.Add(new ColorSwitchComponent(colors, g.transform.GetChild(5).GetChild(node.colors.Count - 2)));
                    }
                    if (node.isStartWithEmptyColor)
                    {
                        components.Add(new EmptyColorComponent(r));
                        r.isEmpty = true;
                    }
                    if (node.isClockOffToOn)
                    {
                        components.Add(new ClockComponent(false, node.clockNum, g.transform.GetChild(7)));
                    }
                    if (node.isClockOnToOff)
                    {
                        components.Add(new ClockComponent(true, node.clockNum, g.transform.GetChild(7)));
                    }

                    r.Initialize(node.maxCount, colors[0], components);
                    r.GetComponent<SpriteRenderer>().color = colors[0];
                    r.TypeMatchAndActive();

                    resources.Add(r);

                    if (isTutorialOn)
                    {
                        switch(currentLevel / 10)
                        {
                            case 0:
                                tutorialGameObject = Instantiate(tutorialObjects[currentLevel / 10], r.transform.position, Quaternion.identity);
                                tutorialResource = r;
                                isTutorialOn = false;
                                break;
                            case 2:
                                for(int j = 0; j < components.Count; j++)
                                {
                                    if(components[j] is SimultaneousComponent)
                                    {
                                        tutorialGameObject = Instantiate(tutorialObjects[1], r.transform.position, Quaternion.identity);
                                        isTutorialOn = false;
                                        tutorialResource = r;
                                        break;
                                    }
                                }
                                break;
                            case 3:
                                for (int j = 0; j < components.Count; j++)
                                {
                                    if (components[j] is ClockComponent)
                                    {
                                        tutorialGameObject = Instantiate(tutorialObjects[2], r.transform.position, Quaternion.identity);
                                        isTutorialOn = false;
                                        tutorialResource = r;
                                        break;
                                    }
                                }
                                break;
                            case 4:
                                for (int j = 0; j < components.Count; j++)
                                {
                                    if (components[j] is ColorSwitchComponent)
                                    {
                                        tutorialGameObject = Instantiate(tutorialObjects[3], r.transform.position, Quaternion.identity);
                                        isTutorialOn = false;
                                        tutorialResource = r;
                                        break;
                                    }
                                }
                                break;
                            case 5:
                                for (int j = 0; j < components.Count; j++)
                                {
                                    if (components[j] is EmptyColorComponent)
                                    {
                                        tutorialGameObject = Instantiate(tutorialObjects[4], r.transform.position, Quaternion.identity);
                                        isTutorialOn = false;
                                        tutorialResource = r;
                                        break;
                                    }
                                }
                                break;
                        }
                        
                        
                    }
                }
            }
            counter++;
        }
    }

    public void OnDestroy()
    {
        SaveLoadFile.Save();

        SteamClient.Shutdown();
    }

    public void OnDisable()
    {
        SteamClient.Shutdown();
    }
}

public class Todo
{
    public Process p;
    public ProcessColor pc;

    public Todo(Process p, ProcessColor pc)
    {
        this.p = p;
        this.pc = pc;
    }
}

