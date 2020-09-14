using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LeaderboardUI : MonoBehaviour
{
    List<RectTransform> ClosestEntries;
    public GameObject Prefab_Select;
    public GameObject Prefab_View;
    public GameObject Prefab_Button;
    public GameObject Prefab_Entry;
    [Header("View")]
    public Vector2 startpos = new Vector2(250,0);
    public Vector2 endPos = Vector2.zero;
    public float moveTime = 1f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0,0,1,1);
    [Header("Buttons")]
    public string AllName = "All time";
    public string WeekName = "Weekly";
    public string DayName = "Daily";
    public Color ActiveColor = Color.gray;
    public Color InactiveColor = Color.white;
    [Header("Entries")]
    public outputForm OutputFormat = outputForm.Score;
    public Color GoldColor = Color.yellow;
    public Color SilverColor = Color.gray;
    public Color BronzeColor = Color.red;
    public Color BasicColor = Color.clear;
    
    LeaderboardManager LBM;
    LeaderboardGroup OpenGroup;

    RectTransform SelectView;
    RectTransform LeaderboardView;

    GameObject viewButtonHolder;
    List<GameObject> entries;
    float positionToSet = 0;

    Button allBut;
    Button weekBut;
    Button dayBut;

    ActiveBoard activeLeaderboard = ActiveBoard.All;

    Coroutine movingRoutine;
    private void Start()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null || !canvas.GetComponent<Canvas>())
        {
            canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
        }
        LeaderboardView = Instantiate(Prefab_View,canvas.transform).GetComponent<RectTransform>();
        LeaderboardView.gameObject.SetActive(false);

        SelectView = Instantiate(Prefab_Select, canvas.transform).GetComponent<RectTransform>();
        SelectView.gameObject.SetActive(false);

        LBM = LeaderboardManager.Instance;
    }
    public void OpenSelect()
    {
        if (movingRoutine == null)
        {
            LoadSelect();
            if (LeaderboardView.gameObject.activeInHierarchy)
                movingRoutine = StartCoroutine(Replacing(LeaderboardView,SelectView));
            else
                movingRoutine = StartCoroutine(Opening(SelectView));
        }
    }
    public void OpenLeaderboard()
    {
        if (LBM.Leaderboards.Count == 1)
            OpenLeaderboard(LBM.Leaderboards[0]);
        else
            OpenSelect();
    }
    public void OpenLeaderboard(string name) => OpenLeaderboard(LBM.FindGroupByName(name));
    public void OpenLeaderboard(LeaderboardGroup Board)
    {
        if (movingRoutine == null)
        {
            LoadLeaderboard(Board);
            if (SelectView.gameObject.activeInHierarchy)
                movingRoutine = StartCoroutine(Replacing(SelectView, LeaderboardView));
            else
                movingRoutine = StartCoroutine(Opening(LeaderboardView));
        }
    }
    
    
    public void CloseLeaderboard()
    {
        if (movingRoutine == null)
        {
            movingRoutine = StartCoroutine(ClosingAll());
        }
    }

    IEnumerator Opening(RectTransform rect)
    {
        rect.gameObject.SetActive(true);
        for (float t = 0; t < moveTime; t += Time.deltaTime)
        {
            float x = (startpos.x - endPos.y) * (1 - moveCurve.Evaluate(t / moveTime));
            float y = (startpos.y - endPos.y) * (1 - moveCurve.Evaluate(t / moveTime));
            rect.anchoredPosition = new Vector2(x, y);
            yield return new WaitForEndOfFrame();
        }
        rect.anchoredPosition = endPos;
        movingRoutine = null;
    }
    IEnumerator Closing(RectTransform rect)
    {
        for (float t = 0; t < moveTime; t += Time.deltaTime)
        {
            float x = (startpos.x - endPos.y) * moveCurve.Evaluate(t / moveTime);
            float y = (startpos.y - endPos.y) * moveCurve.Evaluate(t / moveTime);
            rect.anchoredPosition = new Vector2(x, y);
            yield return new WaitForEndOfFrame();
        }
        rect.anchoredPosition = startpos;
        rect.gameObject.SetActive(false);
        movingRoutine = null;
    }
    IEnumerator Replacing(RectTransform from,RectTransform to)
    {
        to.gameObject.SetActive(true);
        for (float t = 0; t < moveTime; t += Time.deltaTime)
        {
            float x = (startpos.x - endPos.y) * moveCurve.Evaluate(t / moveTime);
            float y = (startpos.y - endPos.y) * moveCurve.Evaluate(t / moveTime);
            from.anchoredPosition = new Vector2(x, y);

            float x2 = (-startpos.x - endPos.y) * (1 - moveCurve.Evaluate(t / moveTime));
            float y2 = (-startpos.y - endPos.y) * (1 - moveCurve.Evaluate(t / moveTime));
            to.anchoredPosition = new Vector2(x2, y2);

            yield return new WaitForEndOfFrame();
        }
        from.anchoredPosition = startpos;
        to.anchoredPosition = endPos;
        from.gameObject.SetActive(false);

        movingRoutine = null;
    }
    IEnumerator ClosingAll()
    {
        if (SelectView.gameObject.activeInHierarchy)
        {
            for (float t = 0; t < moveTime; t += Time.deltaTime)
            {
                float x = (startpos.x - endPos.y) * moveCurve.Evaluate(t / moveTime);
                float y = (startpos.y - endPos.y) * moveCurve.Evaluate(t / moveTime);
                SelectView.anchoredPosition = new Vector2(x, y);
                yield return new WaitForEndOfFrame();
            }
            SelectView.anchoredPosition = startpos;
            SelectView.gameObject.SetActive(false);
        }
        if (LeaderboardView.gameObject.activeInHierarchy)
        {
            for (float t = 0; t < moveTime; t += Time.deltaTime)
            {
                float x = (startpos.x - endPos.y) * moveCurve.Evaluate(t / moveTime);
                float y = (startpos.y - endPos.y) * moveCurve.Evaluate(t / moveTime);
                LeaderboardView.anchoredPosition = new Vector2(x, y);
                yield return new WaitForEndOfFrame();
            }
            LeaderboardView.anchoredPosition = startpos;
            LeaderboardView.gameObject.SetActive(false);
        }
        movingRoutine = null;
    }
    void LoadLeaderboard(LeaderboardGroup Board)
    {
        OpenGroup = Board;
        if ((Board.EnableAllTime ? 1 : 0) + (Board.EnableWeekly ? 1 : 0) + (Board.EnableDaily ? 1 : 0) == 1)
        {
            if (viewButtonHolder == null)
                viewButtonHolder = LeaderboardView.GetComponentInChildren<HorizontalLayoutGroup>().gameObject;
            viewButtonHolder.SetActive(false);
        }
        else
        {
            if (viewButtonHolder == null)
                viewButtonHolder = LeaderboardView.GetComponentInChildren<HorizontalLayoutGroup>().gameObject;
            viewButtonHolder.SetActive(true);
        }
        for (int i = viewButtonHolder.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(viewButtonHolder.transform.GetChild(i).gameObject);
        }

        if (Board.EnableAllTime)
        {
            Button but = Instantiate(Prefab_Button, viewButtonHolder.transform).GetComponent<Button>();
            but.GetComponentInChildren<Text>().text = AllName;
            but.onClick.AddListener(() => ChangeBoard(0));
            but.GetComponent<Image>().color = ActiveColor;
            allBut = but;
        }
        if (Board.EnableWeekly)
        {

            Button but = Instantiate(Prefab_Button, viewButtonHolder.transform).GetComponent<Button>();
            but.GetComponentInChildren<Text>().text = WeekName;
            but.onClick.AddListener(() => ChangeBoard(1));
            weekBut = but;

            if (!Board.EnableAllTime)
            {
                activeLeaderboard = ActiveBoard.Week;
                but.GetComponent<Image>().color = ActiveColor;
            }
        }
        if (Board.EnableDaily)
        {

            Button but = Instantiate(Prefab_Button, viewButtonHolder.transform).GetComponent<Button>();
            but.GetComponentInChildren<Text>().text = DayName;
            but.onClick.AddListener(() => ChangeBoard(2));
            dayBut = but;

            if (!Board.EnableAllTime && !Board.EnableWeekly)
            {
                activeLeaderboard = ActiveBoard.Day;
                but.GetComponent<Image>().color = ActiveColor;
            }
        }

        Button[] allButtons = LeaderboardView.GetComponentsInChildren<Button>();
        if (LBM.Leaderboards.Count > 1)
        {
            allButtons[allButtons.Length - 1].onClick.AddListener(() => OpenSelect());
            allButtons[allButtons.Length - 1].GetComponentInChildren<Text>().text = "Back";
        }
        else
        {
            allButtons[allButtons.Length - 1].onClick.AddListener(() => CloseLeaderboard());
            allButtons[allButtons.Length - 1].GetComponentInChildren<Text>().text = "Exit";
        }


        ChangeBoard((int)activeLeaderboard);
    }
    void LoadSelect()
    {
        Transform par = SelectView.GetComponentInChildren<VerticalLayoutGroup>().transform;
        for (int i = par.childCount - 1; i >= 0; i--)
            Destroy(par.GetChild(i).gameObject);

        for (int i = 0; i < LBM.Leaderboards.Count; i++)
        {
            GameObject go = Instantiate(Prefab_Button, par);
            LeaderboardGroup lb = LBM.Leaderboards[i];
            go.GetComponentInChildren<Text>().text = lb.Name;
            go.GetComponentInChildren<Button>().onClick.AddListener(() => OpenLeaderboard(lb));
        }
        Button[] allBut = SelectView.GetComponentsInChildren<Button>(true);
        allBut[allBut.Length - 1].onClick.AddListener(() => CloseLeaderboard());
    }
    void ChangeBoard(int changeTo) => ChangeBoard((ActiveBoard)changeTo);
    void ChangeBoard(ActiveBoard changeTo)
    {
        Transform content = LeaderboardView.GetComponentInChildren<ContentSizeFitter>().transform;
        if (entries == null)
            entries = new List<GameObject>();

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Destroy(entries[i]);
            entries.RemoveAt(i);
        }

        Leaderboard current;

        switch (changeTo)
        {
            case ActiveBoard.All:
                if (allBut) allBut.GetComponent<Image>().color = ActiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = InactiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = InactiveColor;
                current = OpenGroup.AllTime;
                break;
            case ActiveBoard.Week:
                if (allBut) allBut.GetComponent<Image>().color = InactiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = ActiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = InactiveColor;
                current = OpenGroup.Weekly;
                break;
            case ActiveBoard.Day:
                if (allBut) allBut.GetComponent<Image>().color = InactiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = InactiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = ActiveColor;
                current = OpenGroup.Daily;
                break;
            default:
                if (allBut) allBut.GetComponent<Image>().color = InactiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = InactiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = InactiveColor;
                current = OpenGroup.AllTime;
                break;
        }


        positionToSet = 0;
        for (int i = 0; i < current.board.Count; i++)
        {
            GameObject go = Instantiate(Prefab_Entry, content);
            Text[] texts = go.GetComponentsInChildren<Text>();

            if (i == 0)
            {
                texts[0].color = Color.black;
                texts[0].transform.parent.GetComponent<Image>().color = GoldColor;
            }
            else if (i == 1)
            {
                texts[0].color = Color.black;
                texts[0].transform.parent.GetComponent<Image>().color = SilverColor;
            }
            else if (i == 2)
            {
                texts[0].color = Color.black;
                texts[0].transform.parent.GetComponent<Image>().color = BronzeColor;
            }
            else
            {
                texts[0].color = Color.white;
                texts[0].transform.parent.GetComponent<Image>().color = BasicColor;
            }

            if (current.board[i].ID == LBM.playerEntry.ID)
            {
                for (int k = 0; k < texts.Length; k++)
                    texts[k].color = Color.green;
                float height = Prefab_Entry.GetComponent<RectTransform>().sizeDelta.y;
                positionToSet = 5 + (i * height + 5);
            }

            texts[0].text = (i + 1).ToString();
            texts[1].text = current.board[i].Name;
            switch (OutputFormat)
            {
                case outputForm.Time:
                    float score = current.board[i].Score;
                    int m = Mathf.FloorToInt(score / 60);
                    int s = Mathf.RoundToInt(score - (m * 60));
                    texts[2].text = m.ToString() + ":" + s.ToString("d2");
                    break;
                case outputForm.Score:
                    texts[2].text = current.board[i].Score.ToString("n0");
                    break;
                default:
                    break;
            }

            entries.Add(go);

        }
        Invoke("SetScroll", 0.1f);
    }
    void SetScroll()
    {
        ScrollRect scroll = LeaderboardView.GetComponentInChildren<ScrollRect>();
        scroll.content.anchoredPosition = new Vector2(0, positionToSet);
        scroll.StopMovement();
        scroll.velocity = Vector2.zero;
    }
}
    public enum ActiveBoard
    {
        All,Week,Day
    }
    public enum outputForm
    {
        Score,Time
    }

#if UNITY_EDITOR
[CustomEditor(typeof(LeaderboardUI))]
public class LeaderboardUIEditor : Editor
{
    LeaderboardUI scr;
    bool custom = true;
    bool Prefabs = false;

    bool showView;
    bool showButtons;
    bool showEntries = true;


    private void OnEnable()
    {
        scr = (LeaderboardUI)target;
    }
    public override void OnInspectorGUI()
    {
        GUIStyle centeredText = new GUIStyle(GUI.skin.GetStyle("Label"));
        centeredText.alignment = TextAnchor.MiddleCenter;


        custom = EditorGUILayout.ToggleLeft("Custom Editor", custom);
        if (custom)
        {
            Prefabs = EditorGUILayout.Foldout(Prefabs, "Prefabs");
            if (Prefabs)
            {
                scr.Prefab_Select = (GameObject)EditorGUILayout.ObjectField("Select view",scr.Prefab_Select, typeof(GameObject), false);
                scr.Prefab_View = (GameObject)EditorGUILayout.ObjectField("Board view",scr.Prefab_View, typeof(GameObject), false);
                scr.Prefab_Button = (GameObject)EditorGUILayout.ObjectField("Button",scr.Prefab_Button, typeof(GameObject), false);
                scr.Prefab_Entry = (GameObject)EditorGUILayout.ObjectField("Entry",scr.Prefab_Entry, typeof(GameObject), false);
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Leaderboard"))
                scr.OpenLeaderboard();
            if (GUILayout.Button("Close Leaderboard"))
                scr.CloseLeaderboard();
            GUILayout.EndHorizontal();

            showView = EditorGUILayout.Foldout(showView,"View Animation");
            EditorGUI.indentLevel = 1;
            if (showView)
            {
                GUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 40;
                scr.startpos = EditorGUILayout.Vector2Field("Start:",scr.startpos);
                scr.endPos = EditorGUILayout.Vector2Field("End:", scr.endPos);
                EditorGUIUtility.labelWidth = default;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                scr.moveTime = EditorGUILayout.FloatField(scr.moveTime);
                scr.moveCurve = EditorGUILayout.CurveField(scr.moveCurve);
                GUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel = 0;

            showButtons = EditorGUILayout.Foldout(showButtons, "Button Settings");
            EditorGUI.indentLevel = 1;
            if (showButtons)
            {
                EditorGUILayout.LabelField("Button names",centeredText);
                GUILayout.BeginHorizontal();
                scr.AllName = EditorGUILayout.TextField(scr.AllName);
                scr.WeekName = EditorGUILayout.TextField(scr.WeekName);
                scr.DayName = EditorGUILayout.TextField(scr.DayName);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 80;
                scr.ActiveColor = EditorGUILayout.ColorField("Selected:",scr.ActiveColor);
                scr.InactiveColor = EditorGUILayout.ColorField("Inactive:",scr.InactiveColor);
                EditorGUIUtility.labelWidth = default;
                GUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel = 0;

            showEntries = EditorGUILayout.Foldout(showEntries, "Entry Settings");
            EditorGUI.indentLevel = 1;
            if (showEntries)
            {
                scr.OutputFormat = (outputForm)EditorGUILayout.EnumPopup("Score Format",scr.OutputFormat);
                EditorGUILayout.LabelField("Placement Colors",centeredText);
                EditorGUILayout.BeginHorizontal();
                scr.GoldColor = EditorGUILayout.ColorField(scr.GoldColor);
                scr.SilverColor = EditorGUILayout.ColorField(scr.SilverColor);
                scr.BronzeColor = EditorGUILayout.ColorField(scr.BronzeColor);
                scr.BasicColor = EditorGUILayout.ColorField(scr.BasicColor);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel = 0;
        }
        else
            base.OnInspectorGUI();
    }
}
#endif

