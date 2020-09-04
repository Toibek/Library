using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;

using UnityEngine;

using Firebase;
using Firebase.Unity.Editor;
using Firebase.Database;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LeaderboardManager : MonoBehaviour
{
    public bool sendScore;
    public LeaderboardEntry playerEntry;
    public LeaderboardEntry closest;

    DatabaseReference userRef;

    [Header("Leaderboards")]

    public bool EnableAllTime;
    internal bool ShowAllTime;
    public Leaderboard AllTime;

    public bool EnableWeekly;
    internal bool ShowWeekly;
    public Leaderboard Weekly;


    public bool EnableDaily;
    internal bool ShowDaily;
    public Leaderboard Daily;

    [Header("Global Settings")]
    public bool ReverseScore;
    public string databaseUrl = "https://toibektest.firebaseio.com/";
    public bool IncludeProductName;
    public bool IncludeVersion;



    delegate void EmptyDelegate();


    #region LBInit && Singleton && DDOL
    public static LeaderboardManager Instance;
    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            string save = PlayerPrefs.GetString("LeaderboardSave");
            if (save != "")
            {
                string[] saveArr = save.Split(':');
                playerEntry = new LeaderboardEntry(saveArr[0], saveArr[1], float.Parse(saveArr[2]));
            }


            FirebaseApp.DefaultInstance.SetEditorDatabaseUrl(databaseUrl);

            userRef = FirebaseDatabase.DefaultInstance.GetReference(Application.productName + "/Users");

            string baseRef = "";
            if (IncludeProductName && IncludeVersion)
                baseRef = Application.productName + "/" + Application.version.ToString().Replace('.', ',') + "/";
            else if (IncludeProductName)
                baseRef = Application.productName + "/";
            else if (IncludeVersion)
                baseRef = Application.version.ToString().Replace('.', ',') + "/";

            Clean.log(baseRef);

            if (EnableAllTime)
            {
                string allTimeId = baseRef + "1,AllTime";
                AllTime.DbRef = FirebaseDatabase.DefaultInstance.GetReference(allTimeId);
                AllTime.DbRef.ValueChanged += AllTime.Fetch;
            }
            if (EnableWeekly)
            {
                #region Weekcheck
                CultureInfo myCI = new CultureInfo("en-US");
                Calendar myCal = myCI.Calendar;
                DateTime lastDay = new System.DateTime(DateTime.Now.Year, 12, 31);
                CalendarWeekRule myCWR = myCI.DateTimeFormat.CalendarWeekRule;
                DayOfWeek myFirstDOW = DayOfWeek.Monday;
                int week = myCal.GetWeekOfYear(DateTime.Now, myCWR, myFirstDOW);
                #endregion
                string weekId = baseRef + "2,Weekly/" + DateTime.Now.Year + "," + week.ToString();
                Weekly.DbRef = FirebaseDatabase.DefaultInstance.GetReference(weekId);
                Weekly.DbRef.ValueChanged += Weekly.Fetch;
            }
            if (EnableDaily)
            {
                string dayId = baseRef + "3,Daily/" + DateTime.Now.Year + "," + DateTime.Now.DayOfYear;
                Daily.DbRef = FirebaseDatabase.DefaultInstance.GetReference(dayId);
                Daily.DbRef.ValueChanged += Daily.Fetch;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    #endregion

    private void Update()
    {
        if (sendScore)
        {
            sendScore = false;
            WriteToLeaderboard();
        }
    }

    public void WriteToLeaderboard(LeaderboardEntry entry = null)
    {
        if (entry == null)
            entry = playerEntry;
        if (entry.ID == "")
            entry.ID = userRef.Push().Key;

        Dictionary<string, object> childUpdates = new Dictionary<string, object>();
        childUpdates["/Name"] = entry.Name;
        childUpdates["/Score"] = entry.Score;
        userRef.Child(entry.ID).UpdateChildrenAsync(childUpdates);

        string save = entry.ID + ":" + entry.Name + ":" + entry.Score.ToString();
        PlayerPrefs.SetString("LeaderboardSave", save);

        if (EnableAllTime && AllTime.ActiveWrite == null)
            AllTime.ActiveWrite = StartCoroutine(AllTime.Write(entry));

        if (EnableWeekly)
            Weekly.ActiveWrite = StartCoroutine(Weekly.Write(entry));

        if (EnableDaily)
            Daily.ActiveWrite = StartCoroutine(Daily.Write(entry));
    }
}
[System.Serializable]
public class LeaderboardEntry : object
{
    public string ID = default;
    public string Name = default;
    public float Score = default;
    public LeaderboardEntry(string id ,string name, float score)
    {
        ID = id;
        Name = name;
        Score = score;
    }
    public bool Compare(LeaderboardEntry other)
    {
        return Score > other.Score;
    }
}
[System.Serializable]
public class Leaderboard : object
{
    public List<LeaderboardEntry> board = new List<LeaderboardEntry>();

    public DatabaseReference DbRef;
    public DataSnapshot Snapshot;
    public bool DatabaseLoaded = false;
    public void Fetch(object sender = null, ValueChangedEventArgs args = null)
    {
        DatabaseLoaded = false;
        DbRef.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Snapshot = task.Result;
                board = new List<LeaderboardEntry>();
                foreach (var item in Snapshot.Children)
                {
                    board.Add(
                        new LeaderboardEntry(
                            item.Child("ID").Value.ToString(),
                            item.Child("Name").Value.ToString(),
                            float.Parse(item.Child("Score").Value.ToString())));
                }
                DatabaseLoaded = true;
                Clean.log("Loaded Database");
            }
            else
                Debug.LogWarning(task.Exception);
        });
    }
    public Coroutine ActiveWrite;
    public IEnumerator Write(LeaderboardEntry entry)
    {
        while (!DatabaseLoaded)
            yield return new WaitForEndOfFrame();

        bool leftScore = false;
        if (board.Count > 0)
        {
            for (int i = 0; i < board.Count; i++)
            {
                if (!leftScore)
                {
                    if (entry.Compare(board[i]) && entry.ID == board[i].ID)
                    {
                        Clean.log(entry.ID + ": Replacing my top score");
                        board[i] = entry;
                        leftScore = true;
                        continue;
                    }
                    else if (entry.Compare(board[i]))
                    {
                        Clean.log(entry.ID + ": Placing above: " + i.ToString());
                        board.Insert(i, entry);
                        leftScore = true;
                        continue;
                    }
                    else if (entry.ID == board[i].ID)
                    {
                        Clean.log(entry.ID + ": Found duplicate, getting ignored");
                        leftScore = true;
                        break;
                    }
                }
                else
                {
                    if (entry.ID == board[i].ID)
                    {
                        Clean.log(entry.ID + ": Found duplicate at lower pos");
                        board.RemoveAt(i--);
                    }
                }
            }
            if (!leftScore)
            {
                Clean.log(entry.ID + ": Placed at bottom");
                board.Add(entry);
            }
        }
        else
        {
            board.Add(entry);
            Clean.log(entry.ID + ": First entry");
        }

        //Dictionary<string, object> entUpdates = new Dictionary<string, object>();
        //for (int i = 0; i < AllTimeLeaderboard.Count; i++)
        //{
        //    entUpdates["/" + i.ToString() + "/ID"] = AllTimeLeaderboard[i].ID;
        //    entUpdates["/" + i.ToString() + "/Name"] = AllTimeLeaderboard[i].Name;
        //    entUpdates["/" + i.ToString() + "/Score"] = AllTimeLeaderboard[i].Score;
        //}
        //bool writeWait = false;
        //DbRef.UpdateChildrenAsync(entUpdates).ContinueWith(task => { writeWait = true; });


        //for (int i = AllTimeLeaderboard.Count; i < AllTimeLeaderboard.Count + 10; i++)
        //{
        //    DbRef.Child(i.ToString()).RemoveValueAsync();
        //}

        //while (!writeWait)
        //    yield return new WaitForEndOfFrame();

        ActiveWrite = null;
    }
}
[CustomEditor(typeof(LeaderboardManager))]
public class CustomLeaderboardInspector: Editor
{
    bool custom = true;
    bool settings = false;
    LeaderboardManager scr;
    Vector2 scrollPos;
    private void OnEnable()
    {
        scr = (LeaderboardManager)target;
    }
    public override void OnInspectorGUI()
    {
        custom = EditorGUILayout.ToggleLeft("Custom Inspector",custom);
        if (custom)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player entry:");
            EditorGUILayout.BeginHorizontal();
            scr.playerEntry.Name = EditorGUILayout.TextField(scr.playerEntry.Name);
            scr.playerEntry.Score = EditorGUILayout.FloatField(scr.playerEntry.Score);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Add Entry"))
                scr.WriteToLeaderboard(scr.playerEntry);

            EditorGUILayout.Space();
            scr.EnableAllTime = EditorGUILayout.Toggle("Enable AllTime Leaderboard", scr.EnableAllTime);
            scr.EnableWeekly = EditorGUILayout.Toggle("Enable Weekly Leaderboard", scr.EnableWeekly);
            scr.EnableDaily = EditorGUILayout.Toggle("Enable Daily Leaderboard", scr.EnableDaily);
            EditorGUILayout.Space();
            if (scr.EnableAllTime)
            {
                scr.ShowAllTime = EditorGUILayout.Foldout(scr.ShowAllTime, "AllTime Leaderboard");
                if (scr.ShowAllTime)
                {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Height(300));
                    for (int i = 0; i < scr.AllTime.board.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.TextField(scr.AllTime.board[i].Name);
                        EditorGUILayout.TextField(scr.AllTime.board[i].Score.ToString());
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            if (scr.EnableWeekly)
            {
                scr.ShowWeekly = EditorGUILayout.Foldout(scr.ShowWeekly, "Weekly Leaderboard");
                if (scr.ShowWeekly)
                {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Height(300));
                    for (int i = 0; i < scr.Weekly.board.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.TextField(scr.Weekly.board[i].Name);
                        EditorGUILayout.TextField(scr.Weekly.board[i].Score.ToString());
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            if (scr.EnableDaily)
            {
                scr.ShowDaily = EditorGUILayout.Foldout(scr.ShowDaily, "Daily Leaderboard");
                if (scr.ShowDaily)
                {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, true, GUILayout.Height(300));
                    for (int i = 0; i < scr.Daily.board.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.TextField(scr.Daily.board[i].Name);
                        EditorGUILayout.TextField(scr.Daily.board[i].Score.ToString());
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            EditorGUILayout.Space();
            settings = EditorGUILayout.BeginFoldoutHeaderGroup(settings,"Settings:");
            if (settings)
            {
                EditorGUILayout.HelpBox("Only change these if the leaderboards are empty",MessageType.Info);
                scr.ReverseScore = EditorGUILayout.Toggle(scr.ReverseScore,"Reverse boards");
                scr.IncludeProductName = EditorGUILayout.Toggle(scr.IncludeProductName, "Include Name");
                scr.IncludeVersion = EditorGUILayout.Toggle(scr.IncludeVersion, "Include version");
                scr.databaseUrl = EditorGUILayout.TextField("Database Url",scr.databaseUrl);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
        else
        {
            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
