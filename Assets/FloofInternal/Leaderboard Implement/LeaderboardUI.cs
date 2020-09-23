using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LeaderboardUI : MonoBehaviour
{
    public Style SelectedStyle;
    public List<Style> Styles;

    List<RectTransform> ClosestEntries;


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
        LeaderboardView = Instantiate(SelectedStyle.Prefab_View,canvas.transform).GetComponent<RectTransform>();
        LeaderboardView.gameObject.SetActive(false);

        SelectView = Instantiate(SelectedStyle.Prefab_Select, canvas.transform).GetComponent<RectTransform>();
        SelectView.gameObject.SetActive(false);

        LBM = LeaderboardManager.Instance;
    }
    /// <summary>
    /// Opens the leaderboard select menu
    /// </summary>
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
    /// <summary>
    /// Opens the primary leaderboard or the select menu if there's more than one leaderboard
    /// </summary>
    public void OpenLeaderboard()
    {
        if (LBM.Leaderboards.Count == 1)
            OpenLeaderboard(LBM.Leaderboards[0]);
        else
            OpenSelect();
    }
    /// <summary>
    /// Opens the named leaderboard
    /// </summary>
    /// <param name="name"></param>
    public void OpenLeaderboard(string name) => OpenLeaderboard(LBM.FindGroupByName(name));
    /// <summary>
    /// Opens the entered leaderboard
    /// </summary>
    /// <param name="Board"></param>
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
    /// <summary>
    /// Closes the open leaderboard
    /// </summary>
    public void CloseLeaderboard()
    {
        if (movingRoutine == null)
        {
            movingRoutine = StartCoroutine(ClosingAll());
        }
    }
    /// <summary>
    /// Start as a coroutine to smoothly open a rect in accordance with the selectedStyle
    /// </summary>
    IEnumerator Opening(RectTransform rect)
    {
        rect.gameObject.SetActive(true);
        for (float t = 0; t < SelectedStyle.moveTime; t += Time.deltaTime)
        {
            float x = (SelectedStyle.startpos.x - SelectedStyle.endPos.y) * (1 - SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime));
            float y = (SelectedStyle.startpos.y - SelectedStyle.endPos.y) * (1 - SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime));
            rect.anchoredPosition = new Vector2(x, y);
            yield return new WaitForEndOfFrame();
        }
        rect.anchoredPosition = SelectedStyle.endPos;
        movingRoutine = null;
    }
    /// <summary>
    /// Start as a coroutine to smoothly close a rect in accordance with the selectedStyle
    /// </summary>
    IEnumerator Closing(RectTransform rect)
    {
        for (float t = 0; t < SelectedStyle.moveTime; t += Time.deltaTime)
        {
            float x = (SelectedStyle.startpos.x - SelectedStyle.endPos.y) * SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime);
            float y = (SelectedStyle.startpos.y - SelectedStyle.endPos.y) * SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime);
            rect.anchoredPosition = new Vector2(x, y);
            yield return new WaitForEndOfFrame();
        }
        rect.anchoredPosition = SelectedStyle.startpos;
        rect.gameObject.SetActive(false);
        movingRoutine = null;
    }
    /// <summary>
    /// Start as a coroutine to smoothly replace a rect in accordance with the selectedStyle
    /// </summary>
    IEnumerator Replacing(RectTransform from,RectTransform to)
    {
        to.gameObject.SetActive(true);
        for (float t = 0; t < SelectedStyle.moveTime; t += Time.deltaTime)
        {
            float x = (SelectedStyle.startpos.x - SelectedStyle.endPos.y) * SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime);
            float y = (SelectedStyle.startpos.y - SelectedStyle.endPos.y) * SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime);
            from.anchoredPosition = new Vector2(x, y);

            float x2 = (-SelectedStyle.startpos.x - SelectedStyle.endPos.y) * (1 - SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime));
            float y2 = (-SelectedStyle.startpos.y - SelectedStyle.endPos.y) * (1 - SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime));
            to.anchoredPosition = new Vector2(x2, y2);

            yield return new WaitForEndOfFrame();
        }
        from.anchoredPosition = SelectedStyle.startpos;
        to.anchoredPosition = SelectedStyle.endPos;
        from.gameObject.SetActive(false);

        movingRoutine = null;
    }
    /// <summary>
    /// Start as a coroutine to smoothly close all rects in accordance with the selectedStyle
    /// </summary>
    IEnumerator ClosingAll()
    {
        if (SelectView.gameObject.activeInHierarchy)
        {
            for (float t = 0; t < SelectedStyle.moveTime; t += Time.deltaTime)
            {
                float x = (SelectedStyle.startpos.x - SelectedStyle.endPos.y) * SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime);
                float y = (SelectedStyle.startpos.y - SelectedStyle.endPos.y) * SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime);
                SelectView.anchoredPosition = new Vector2(x, y);
                yield return new WaitForEndOfFrame();
            }
            SelectView.anchoredPosition = SelectedStyle.startpos;
            SelectView.gameObject.SetActive(false);
        }
        if (LeaderboardView.gameObject.activeInHierarchy)
        {
            for (float t = 0; t < SelectedStyle.moveTime; t += Time.deltaTime)
            {
                float x = (SelectedStyle.startpos.x - SelectedStyle.endPos.y) * SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime);
                float y = (SelectedStyle.startpos.y - SelectedStyle.endPos.y) * SelectedStyle.moveCurve.Evaluate(t / SelectedStyle.moveTime);
                LeaderboardView.anchoredPosition = new Vector2(x, y);
                yield return new WaitForEndOfFrame();
            }
            LeaderboardView.anchoredPosition = SelectedStyle.startpos;
            LeaderboardView.gameObject.SetActive(false);
        }
        movingRoutine = null;
    }
    /// <summary>
    /// Loads the leaderboard ui to the specified board
    /// </summary>
    /// <param name="Board"></param>
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
            Button but = Instantiate(SelectedStyle.Prefab_Button, viewButtonHolder.transform).GetComponent<Button>();
            but.GetComponentInChildren<Text>().text = SelectedStyle.AllName;
            but.onClick.AddListener(() => ChangeBoard(0));
            but.GetComponent<Image>().color = SelectedStyle.ActiveColor;
            allBut = but;
        }
        if (Board.EnableWeekly)
        {

            Button but = Instantiate(SelectedStyle.Prefab_Button, viewButtonHolder.transform).GetComponent<Button>();
            but.GetComponentInChildren<Text>().text = SelectedStyle.WeekName;
            but.onClick.AddListener(() => ChangeBoard(1));
            weekBut = but;

            if (!Board.EnableAllTime)
            {
                activeLeaderboard = ActiveBoard.Week;
                but.GetComponent<Image>().color = SelectedStyle.ActiveColor;
            }
        }
        if (Board.EnableDaily)
        {

            Button but = Instantiate(SelectedStyle.Prefab_Button, viewButtonHolder.transform).GetComponent<Button>();
            but.GetComponentInChildren<Text>().text = SelectedStyle.DayName;
            but.onClick.AddListener(() => ChangeBoard(2));
            dayBut = but;

            if (!Board.EnableAllTime && !Board.EnableWeekly)
            {
                activeLeaderboard = ActiveBoard.Day;
                but.GetComponent<Image>().color = SelectedStyle.ActiveColor;
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
    /// <summary>
    /// Loads the select menu
    /// </summary>
    void LoadSelect()
    {
        Transform par = SelectView.GetComponentInChildren<VerticalLayoutGroup>().transform;
        for (int i = par.childCount - 1; i >= 0; i--)
            Destroy(par.GetChild(i).gameObject);

        for (int i = 0; i < LBM.Leaderboards.Count; i++)
        {
            GameObject go = Instantiate(SelectedStyle.Prefab_Button, par);
            LeaderboardGroup lb = LBM.Leaderboards[i];
            go.GetComponentInChildren<Text>().text = lb.Name;
            go.GetComponentInChildren<Button>().onClick.AddListener(() => OpenLeaderboard(lb));
        }
        Button[] allBut = SelectView.GetComponentsInChildren<Button>(true);
        allBut[allBut.Length - 1].onClick.AddListener(() => CloseLeaderboard());
    }
    /// <summary>
    /// Changes the open leaderboard to the specified board
    /// </summary>
    /// <param name="changeTo"></param>
    void ChangeBoard(int changeTo) => ChangeBoard((ActiveBoard)changeTo);
    /// <summary>
    /// Changes the open leaderboard to the specified board
    /// </summary>
    /// <param name="changeTo"></param>
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
                if (allBut) allBut.GetComponent<Image>().color = SelectedStyle.ActiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = SelectedStyle.InactiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = SelectedStyle.InactiveColor;
                current = OpenGroup.AllTime;
                break;
            case ActiveBoard.Week:
                if (allBut) allBut.GetComponent<Image>().color = SelectedStyle.InactiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = SelectedStyle.ActiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = SelectedStyle.InactiveColor;
                current = OpenGroup.Weekly;
                break;
            case ActiveBoard.Day:
                if (allBut) allBut.GetComponent<Image>().color = SelectedStyle.InactiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = SelectedStyle.InactiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = SelectedStyle.ActiveColor;
                current = OpenGroup.Daily;
                break;
            default:
                if (allBut) allBut.GetComponent<Image>().color = SelectedStyle.InactiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = SelectedStyle.InactiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = SelectedStyle.InactiveColor;
                current = OpenGroup.AllTime;
                break;
        }


        positionToSet = 0;
        for (int i = 0; i < current.board.Count; i++)
        {
            GameObject go = Instantiate(SelectedStyle.Prefab_Entry, content);
            Text[] texts = go.GetComponentsInChildren<Text>();

            if (i == 0)
            {
                texts[0].color = Color.black;
                texts[0].transform.parent.GetComponent<Image>().color = SelectedStyle.GoldColor;
            }
            else if (i == 1)
            {
                texts[0].color = Color.black;
                texts[0].transform.parent.GetComponent<Image>().color = SelectedStyle.SilverColor;
            }
            else if (i == 2)
            {
                texts[0].color = Color.black;
                texts[0].transform.parent.GetComponent<Image>().color = SelectedStyle.BronzeColor;
            }
            else
            {
                texts[0].color = Color.white;
                texts[0].transform.parent.GetComponent<Image>().color = SelectedStyle.BasicColor;
            }

            if (current.board[i].ID == LBM.playerEntry.ID)
            {
                for (int k = 0; k < texts.Length; k++)
                    texts[k].color = Color.green;
                float height = SelectedStyle.Prefab_Entry.GetComponent<RectTransform>().sizeDelta.y;
                positionToSet = 5 + (i * height + 5);
            }

            texts[0].text = (i + 1).ToString();
            texts[1].text = current.board[i].Name;
            switch (SelectedStyle.OutputFormat)
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
    /// <summary>
    /// Sets the scroll position to where the current users score is located
    /// </summary>
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
    All, Week, Day
}

public enum outputForm
{
    Score, Time
}

/// <summary>
/// Style containing the settings for a leaderboard
/// </summary>
[System.Serializable]
public class Style : object
{
    public string name;
    public GameObject Prefab_Select;
    public GameObject Prefab_View;
    public GameObject Prefab_Button;
    public GameObject Prefab_Entry;
    [Header("View")]
    public Vector2 startpos = new Vector2(250, 0);
    public Vector2 endPos = Vector2.zero;
    public float moveTime = 1f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
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

    internal bool ShowPrefabs;
    internal bool ShowView;
    internal bool ShowButtons;
    internal bool ShowEntries;
    internal bool ShowInInspector;
}

#if UNITY_EDITOR
[CustomEditor(typeof(LeaderboardUI))]
public class LeaderboardUIEditor : Editor
{
    LeaderboardUI scr;
    bool custom = true;
    int selectedStyle;
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
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Leaderboard"))
                scr.OpenLeaderboard();
            if (GUILayout.Button("Close Leaderboard"))
                scr.CloseLeaderboard();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            List<string> styles = new List<string>();
            for (int i = 0; i < scr.Styles.Count; i++)
                styles.Add(scr.Styles[i].name);
            selectedStyle = styles.IndexOf(scr.SelectedStyle.name);
            selectedStyle = EditorGUILayout.Popup(selectedStyle, styles.ToArray());
            scr.SelectedStyle = scr.Styles[selectedStyle];

            EditorGUILayout.Space();
            if (GUILayout.Button("Create Style"))
                scr.Styles.Add(new Style());
            for (int i = 0; i < scr.Styles.Count; i++)
            {
                Style style = scr.Styles[i];
                EditorGUILayout.BeginHorizontal();
                style.ShowInInspector = EditorGUILayout.BeginFoldoutHeaderGroup(style.ShowInInspector,style.name);
                if (GUILayout.Button("Remove Style", GUILayout.Width(175)))
                    scr.Styles.Remove(style);
                EditorGUILayout.EndHorizontal();
                if (style.ShowInInspector)
                {
                    EditorGUI.indentLevel++;
                    style.name = EditorGUILayout.TextField("Name", style.name);
                    style.ShowPrefabs = EditorGUILayout.Foldout(style.ShowPrefabs, "Prefabs");
                    if (style.ShowPrefabs)
                    {
                        style.Prefab_Select = (GameObject)EditorGUILayout.ObjectField("Select view", style.Prefab_Select, typeof(GameObject), false);
                        style.Prefab_View = (GameObject)EditorGUILayout.ObjectField("Board view", style.Prefab_View, typeof(GameObject), false);
                        style.Prefab_Button = (GameObject)EditorGUILayout.ObjectField("Button", style.Prefab_Button, typeof(GameObject), false);
                        style.Prefab_Entry = (GameObject)EditorGUILayout.ObjectField("Entry", style.Prefab_Entry, typeof(GameObject), false);
                    }

                    style.ShowView = EditorGUILayout.Foldout(style.ShowView, "View Animation");
                    EditorGUI.indentLevel++;
                    if (style.ShowView)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUIUtility.labelWidth = 40;
                        style.startpos = EditorGUILayout.Vector2Field("Start:", style.startpos);
                        style.endPos = EditorGUILayout.Vector2Field("End:", style.endPos);
                        EditorGUIUtility.labelWidth = default;
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        style.moveTime = EditorGUILayout.FloatField(style.moveTime);
                        style.moveCurve = EditorGUILayout.CurveField(style.moveCurve);
                        GUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;

                    style.ShowButtons = EditorGUILayout.Foldout(style.ShowButtons, "Button Settings");
                    EditorGUI.indentLevel++;
                    if (style.ShowButtons)
                    {
                        EditorGUILayout.LabelField("Button names", centeredText);
                        GUILayout.BeginHorizontal();
                        style.AllName = EditorGUILayout.TextField(style.AllName);
                        style.WeekName = EditorGUILayout.TextField(style.WeekName);
                        style.DayName = EditorGUILayout.TextField(style.DayName);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUIUtility.labelWidth = 80;
                        style.ActiveColor = EditorGUILayout.ColorField("Selected:", style.ActiveColor);
                        style.InactiveColor = EditorGUILayout.ColorField("Inactive:", style.InactiveColor);
                        EditorGUIUtility.labelWidth = default;
                        GUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;

                    style.ShowEntries = EditorGUILayout.Foldout(style.ShowEntries, "Entry Settings");
                    EditorGUI.indentLevel++;
                    if (style.ShowEntries)
                    {
                        style.OutputFormat = (outputForm)EditorGUILayout.EnumPopup("Score Format", style.OutputFormat);
                        EditorGUILayout.LabelField("Placement Colors", centeredText);
                        EditorGUILayout.BeginHorizontal();
                        style.GoldColor = EditorGUILayout.ColorField(style.GoldColor);
                        style.SilverColor = EditorGUILayout.ColorField(style.SilverColor);
                        style.BronzeColor = EditorGUILayout.ColorField(style.BronzeColor);
                        style.BasicColor = EditorGUILayout.ColorField(style.BasicColor);
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            if (GUI.changed)
                EditorUtility.SetDirty(scr);
        }
        else
            base.OnInspectorGUI();
    }
}
#endif

