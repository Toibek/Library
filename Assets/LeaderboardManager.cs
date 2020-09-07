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
                //AllTime.Fetch();
                AllTime.DbRef.ValueChanged += AllTime.Update;
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
                //Weekly.Fetch();
                Weekly.DbRef.ValueChanged += Weekly.Update;
            }
            if (EnableDaily)
            {
                string dayId = baseRef + "3,Daily/" + DateTime.Now.Year + "," + DateTime.Now.DayOfYear;
                Daily.DbRef = FirebaseDatabase.DefaultInstance.GetReference(dayId);
                //Daily.Fetch();
                Daily.DbRef.ValueChanged += Daily.Update;
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
            entry = new LeaderboardEntry(playerEntry);
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
        else if (EnableAllTime)
            Debug.LogError("Failed at setting the all time leaderboard, don't do it this often");

        if (EnableWeekly && Weekly.ActiveWrite == null)
            Weekly.ActiveWrite = StartCoroutine(Weekly.Write(entry));
        else if (EnableWeekly)
            Debug.LogError("Failed at setting the weekly leaderboard, don't do it this often");

        if (EnableDaily && Daily.ActiveWrite == null)
            Daily.ActiveWrite = StartCoroutine(Daily.Write(entry));
        else if (EnableDaily)
            Debug.LogError("Failed at setting the daily leaderboard, don't do it this often");
    }
}
[System.Serializable]
public class LeaderboardEntry : object
{
    public string ID = default;
    public string Name = default;
    public float Score = default;
    public LeaderboardEntry(LeaderboardEntry old)
    {
        ID = old.ID;
        Name = old.Name;
        Score = old.Score;
    }
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

    public int Limit;

    public DatabaseReference DbRef;
    public DataSnapshot Snapshot;
    public bool DatabaseLoaded = false;


    string idToFind;
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
                            item.Key,
                            item.Child("Name").Value.ToString(),
                            float.Parse(item.Child("Score").Value.ToString())));
                }
                board.Sort(SortByScore);
                DatabaseLoaded = true;
                Clean.log("Loaded Database");
            }
            else
                Debug.LogWarning(task.Exception);
        });
    }
    public void Update(object sender = null, ValueChangedEventArgs args = null)
    {
        if (args != null)
        {
        DataSnapshot snapshot = args.Snapshot;
            foreach (var item in snapshot.Children)
            {
                int old = -1;
                idToFind = item.Key;
                old = board.FindIndex(FindByID);
                if (old != -1)
                    board.RemoveAt(old);

                board.Add(
                    new LeaderboardEntry(
                        item.Key,
                        item.Child("Name").Value.ToString(),
                        float.Parse(item.Child("Score").Value.ToString())
                ));
            }
            board.Sort(SortByScore);
            DatabaseLoaded = true;
            Clean.log("Loaded Database");
        }
    }
    public Coroutine ActiveWrite;
    public IEnumerator Write(LeaderboardEntry entry)
    {
        while (!DatabaseLoaded)
            yield return new WaitForEndOfFrame();

        DatabaseLoaded = false;
        idToFind = entry.ID;
        int early = -1;
        early = board.FindIndex(FindByID);
        if (early != -1)
        {
            if (board[early].Compare(entry))
            {
                Debug.Log("Lower than old score");
                DatabaseLoaded = true;
            }
            else
            {
                Debug.Log("Replacing old score");
                board.RemoveAt(early);
                board.Add(entry);
            }
        }
        else
        {
            Debug.Log("Setting new score");

            if (board.Count < Limit || Limit == 0)
                board.Add(entry);
            else
                DatabaseLoaded = true;
        }
        board.Sort(SortByScore);
        #region oldversion
        //bool leftScore = false;
        //if (board.Count > 0)
        //{
        //    for (int i = 0; i < board.Count; i++)
        //    {
        //        if (!leftScore)
        //        {
        //            if (entry.Compare(board[i]) && entry.ID == board[i].ID)
        //            {
        //                Clean.log(entry.Name + ": Replacing my top score");
        //                board[i] = entry;
        //                leftScore = true;
        //                continue;
        //            }
        //            else if (entry.Compare(board[i]))
        //            {
        //                Clean.log(entry.Name + ": Placing above: " + i.ToString());
        //                board.Insert(i, entry);
        //                leftScore = true;
        //                continue;
        //            }
        //            else if (entry.ID == board[i].ID)
        //            {
        //                Clean.log(entry.Name + ": Found duplicate, getting ignored");
        //                DatabaseLoaded = true;
        //                leftScore = true;
        //                break;
        //            }
        //        }
        //        else
        //        {
        //            if (entry.ID == board[i].ID)
        //            {
        //                Clean.log(entry.Name + ": Found duplicate at lower pos");
        //                DatabaseLoaded = false;
        //                board.RemoveAt(i--);
        //            }
        //        }
        //    }
        //    if (!leftScore)
        //    {
        //        Clean.log(entry.Name + ": Placed at bottom");
        //        board.Add(entry);
        //    }
        //}
        //else
        //{
        //    board.Add(entry);
        //    Clean.log(entry.Name + ": First entry");
        //}
        #endregion

        Dictionary<string, object> entUpdates = new Dictionary<string, object>();
        for (int i = 0; i < board.Count; i++)
        {
            entUpdates["/" + board[i].ID + "/Name"] = board[i].Name;
            entUpdates["/" + board[i].ID + "/Score"] = board[i].Score;
        }
        bool writeWait = false;
        DbRef.UpdateChildrenAsync(entUpdates).ContinueWith(task => { writeWait = true; });


        for (int i = board.Count; i < board.Count + 10; i++)
        {
            DbRef.Child(i.ToString()).RemoveValueAsync();
        }

        while (!writeWait)
            yield return new WaitForEndOfFrame();

        ActiveWrite = null;
    }
    bool FindByID(LeaderboardEntry p1)
    {
        return p1.ID == idToFind;
    }
    public int SortByScore(LeaderboardEntry p1, LeaderboardEntry p2)
    {
        if (LeaderboardManager.Instance.ReverseScore)
            return p1.Score.CompareTo(p2.Score);
        else
            return -p1.Score.CompareTo(p2.Score);
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(LeaderboardManager))]
public class CustomLeaderboardInspector: Editor
{
    bool custom = true;
    bool settings = false;
    LeaderboardManager scr;
    Vector2 allScrollPos;
    Vector2 WeekScrollPos;
    Vector2 dayScrollPos;
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
            if (!Application.isPlaying)
            {
                scr.EnableAllTime = EditorGUILayout.Toggle("Enable AllTime Leaderboard", scr.EnableAllTime);
                if (scr.EnableAllTime)
                {
                    EditorGUI.indentLevel = 1;
                    scr.AllTime.Limit = EditorGUILayout.IntField("Alltime limit", scr.AllTime.Limit);
                    EditorGUI.indentLevel = 0;
                }
                scr.EnableWeekly = EditorGUILayout.Toggle("Enable Weekly Leaderboard", scr.EnableWeekly);
                if (scr.EnableWeekly)
                {
                    EditorGUI.indentLevel = 1;
                    scr.Weekly.Limit = EditorGUILayout.IntField("Weekly limit", scr.Weekly.Limit);
                    EditorGUI.indentLevel = 0;
                }
                scr.EnableDaily = EditorGUILayout.Toggle("Enable Daily Leaderboard", scr.EnableDaily);
                if (scr.EnableDaily)
                {
                    EditorGUI.indentLevel = 1;
                    scr.Daily.Limit = EditorGUILayout.IntField("Daily limit", scr.Daily.Limit);
                    EditorGUI.indentLevel = 0;
                }

                EditorGUILayout.Space();

                settings = EditorGUILayout.BeginFoldoutHeaderGroup(settings, "Settings:");
                if (settings)
                {
                    EditorGUILayout.HelpBox("Only change these if the leaderboards are empty", MessageType.Info);
                    scr.ReverseScore = EditorGUILayout.Toggle("Reverse boards", scr.ReverseScore);
                    scr.IncludeProductName = EditorGUILayout.Toggle("Include Name", scr.IncludeProductName);
                    scr.IncludeVersion = EditorGUILayout.Toggle("Include version", scr.IncludeVersion);
                    scr.databaseUrl = EditorGUILayout.TextField("Database Url", scr.databaseUrl);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            else
            {

                if (scr.EnableAllTime)
                {
                    scr.ShowAllTime = EditorGUILayout.Foldout(scr.ShowAllTime, "AllTime Leaderboard");
                    if (scr.ShowAllTime)
                    {
                        EditorGUI.indentLevel = 1;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("nr.", GUILayout.Width(30));
                        EditorGUILayout.LabelField("Name:", GUILayout.Width(150));
                        EditorGUILayout.LabelField("Score:", GUILayout.Width(150));
                        EditorGUILayout.EndHorizontal();

                        allScrollPos = EditorGUILayout.BeginScrollView(allScrollPos, false, false, GUILayout.Height(300));
                        for (int i = 0; i < scr.AllTime.board.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(i.ToString() + ".", GUILayout.Width(30));
                            EditorGUILayout.LabelField(scr.AllTime.board[i].Name, GUILayout.Width(150));
                            EditorGUILayout.LabelField(scr.AllTime.board[i].Score.ToString(), GUILayout.Width(150));
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndScrollView();
                        EditorGUI.indentLevel = 0;
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                if (scr.EnableWeekly)
                {
                    scr.ShowWeekly = EditorGUILayout.Foldout(scr.ShowWeekly, "Weekly Leaderboard");
                    if (scr.ShowWeekly)
                    {
                        EditorGUI.indentLevel = 1;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("nr.", GUILayout.Width(30));
                        EditorGUILayout.LabelField("Name:", GUILayout.Width(150));
                        EditorGUILayout.LabelField("Score:", GUILayout.Width(150));
                        EditorGUILayout.EndHorizontal();

                        WeekScrollPos = EditorGUILayout.BeginScrollView(WeekScrollPos, false, false, GUILayout.Height(300));
                        for (int i = 0; i < scr.Weekly.board.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(i.ToString() + ".", GUILayout.Width(30));
                            EditorGUILayout.LabelField(scr.Weekly.board[i].Name, GUILayout.Width(150));
                            EditorGUILayout.LabelField(scr.Weekly.board[i].Score.ToString(), GUILayout.Width(150));
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndScrollView();
                        EditorGUI.indentLevel = 0;
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                if (scr.EnableDaily)
                {
                    scr.ShowDaily = EditorGUILayout.Foldout(scr.ShowDaily, "Daily Leaderboard");
                    if (scr.ShowDaily)
                    {
                        EditorGUI.indentLevel = 1;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("nr.", GUILayout.Width(30));
                        EditorGUILayout.LabelField("Name:", GUILayout.Width(150));
                        EditorGUILayout.LabelField("Score:", GUILayout.Width(150));
                        EditorGUILayout.EndHorizontal();

                        dayScrollPos = EditorGUILayout.BeginScrollView(dayScrollPos, false, false, GUILayout.Height(300));
                        for (int i = 0; i < scr.Daily.board.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(i.ToString() + ".", GUILayout.Width(30));
                            EditorGUILayout.LabelField(scr.Daily.board[i].Name, GUILayout.Width(150));
                            EditorGUILayout.LabelField(scr.Daily.board[i].Score.ToString(), GUILayout.Width(150));
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.EndScrollView();
                        EditorGUI.indentLevel = 0;
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
            }
        }
        else
        {
            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
        if (GUI.changed)
            EditorUtility.SetDirty(scr);
    }
}
#endif
