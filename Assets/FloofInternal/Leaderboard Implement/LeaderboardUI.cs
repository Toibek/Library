﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LeaderboardUI : MonoBehaviour
{
    List<RectTransform> ClosestEntries;
    public GameObject Prefab_View;
    public GameObject Prefab_LeaderboardButton;
    public GameObject Prefab_LeaderboardEntry;
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

    RectTransform LeaderboardView;
    GameObject buttonHolder;
    List<GameObject> entries;
    float positionToSet = 0;

    Button allBut;
    Button weekBut;
    Button dayBut;

    ActiveBoard activeLeaderboard = ActiveBoard.All;

    Coroutine movingRoutine;
    bool open = false;
    LeaderboardManager LBM;
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
        LBM = LeaderboardManager.Instance;
    }
    public void ToggleLeaderboard()
    {
        if (open)
            CloseLeaderboard();
        else
            OpenLeaderboard();
    }
    public void OpenLeaderboard()
    {
        if (movingRoutine == null && !open) 
        {
            if (LBM == null)
                LBM = LeaderboardManager.Instance;

            if((LBM.EnableAllTime ? 1 : 0) + (LBM.EnableWeekly ? 1 : 0) + (LBM.EnableDaily ? 1 : 0) == 1)
            {
                if(buttonHolder == null)
                    buttonHolder = LeaderboardView.GetComponentInChildren<HorizontalLayoutGroup>().gameObject;
                buttonHolder.SetActive(false);
            }
            else
            {
                if (buttonHolder == null)
                    buttonHolder = LeaderboardView.GetComponentInChildren<HorizontalLayoutGroup>().gameObject;
                buttonHolder.SetActive(true);
            }
            for (int i = buttonHolder.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(buttonHolder.transform.GetChild(i).gameObject);
            }

            if(LBM.EnableAllTime)
            {
                Button but = Instantiate(Prefab_LeaderboardButton, buttonHolder.transform).GetComponent<Button>();
                but.GetComponentInChildren<Text>().text = AllName;
                but.onClick.AddListener(() => loadBoard(0));
                but.GetComponent<Image>().color = ActiveColor;
                allBut = but;
            }
            if(LBM.EnableWeekly)
            {

                Button but = Instantiate(Prefab_LeaderboardButton, buttonHolder.transform).GetComponent<Button>();
                but.GetComponentInChildren<Text>().text = WeekName;
                but.onClick.AddListener(() => loadBoard(1));
                weekBut = but;

                if (!LBM.EnableAllTime)
                {
                    activeLeaderboard = ActiveBoard.Week;
                    but.GetComponent<Image>().color = ActiveColor;
                }
            }
            if(LBM.EnableDaily)
            {

                Button but = Instantiate(Prefab_LeaderboardButton, buttonHolder.transform).GetComponent<Button>();
                but.GetComponentInChildren<Text>().text = DayName;
                but.onClick.AddListener(() => loadBoard(2));
                dayBut = but;

                if (!LBM.EnableAllTime && !LBM.EnableWeekly)
                {
                    activeLeaderboard = ActiveBoard.Day;
                    but.GetComponent<Image>().color = ActiveColor;
                }
            }

            Button[] allButtons = LeaderboardView.GetComponentsInChildren<Button>();
            allButtons[allButtons.Length-1].onClick.AddListener(()=> CloseLeaderboard());

            loadBoard((int)activeLeaderboard);
            movingRoutine = StartCoroutine(Opening());
        }
    }
    IEnumerator Opening()
    {
        LeaderboardView.gameObject.SetActive(true);
        for (float t = 0; t < moveTime; t += Time.deltaTime)
        {
            float x = (startpos.x - endPos.y) * (1-moveCurve.Evaluate(t / moveTime));
            float y = (startpos.y - endPos.y) * (1-moveCurve.Evaluate(t / moveTime));
            LeaderboardView.anchoredPosition = new Vector2(x,y);
            yield return new WaitForEndOfFrame();
        }
        LeaderboardView.anchoredPosition = endPos;

        open = true;
        movingRoutine = null;
    }
    public void CloseLeaderboard()
    {
        if (movingRoutine == null && open)
        {
            movingRoutine = StartCoroutine(Closing());
        }
    }
    IEnumerator Closing()
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

        open = false;
        movingRoutine = null;
    }
    public void loadBoard(int changeTo)
    {
        activeLeaderboard = (ActiveBoard)changeTo;
        Transform content = LeaderboardView.GetComponentInChildren<ContentSizeFitter>().transform;
        if (entries == null)
            entries = new List<GameObject>();

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Destroy(entries[i]);
            entries.RemoveAt(i);
        }

        Leaderboard current;

        switch (activeLeaderboard)
        {
            case ActiveBoard.All:
                if (allBut) allBut.GetComponent<Image>().color = ActiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = InactiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = InactiveColor;
                current = LBM.AllTime;
                break;
            case ActiveBoard.Week:
                if (allBut) allBut.GetComponent<Image>().color = InactiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = ActiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = InactiveColor;
                current = LBM.Weekly;
                break;
            case ActiveBoard.Day:
                if (allBut) allBut.GetComponent<Image>().color = InactiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = InactiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = ActiveColor;
                current = LBM.Daily;
                break;
            default:
                if (allBut) allBut.GetComponent<Image>().color = InactiveColor;
                if (weekBut) weekBut.GetComponent<Image>().color = InactiveColor;
                if (dayBut) dayBut.GetComponent<Image>().color = InactiveColor;
                current = LBM.AllTime;
                break;
        }


        positionToSet = 0;
        for (int i = 0; i < current.board.Count; i++)
        {
            GameObject go = Instantiate(Prefab_LeaderboardEntry, content);
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
                float height = Prefab_LeaderboardEntry.GetComponent<RectTransform>().sizeDelta.y;
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
        Invoke("setScroll", 0.1f);
    }
    void setScroll()
    {
        ScrollRect scroll = LeaderboardView.GetComponentInChildren<ScrollRect>();
        scroll.content.anchoredPosition = new Vector2(0, positionToSet);
        scroll.StopMovement();
        scroll.velocity = Vector2.zero;
    }
}
    enum ActiveBoard
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
            if (GUILayout.Button("Toggle Leaderboard"))
                scr.ToggleLeaderboard();

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
