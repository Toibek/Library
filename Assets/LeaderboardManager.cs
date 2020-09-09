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
    public LeaderboardEntry playerEntry;

    public LeaderboardEntry playerHigh;
    public LeaderboardEntry closestOverall;
    public LeaderboardEntry closestAllTime;
    public LeaderboardEntry closestWeekly;
    public LeaderboardEntry closestDaily;

    DatabaseReference userRef;

    [Header("Leaderboards")]

    public bool EnableAllTime;
    public Leaderboard AllTime;

    public bool EnableWeekly;
    public Leaderboard Weekly;


    public bool EnableDaily;
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

            //This area serves to prepare the connections with the database and fetch from it.
            #region LBInit
            //Loads up your last sent entry
            string save = PlayerPrefs.GetString("LeaderboardSave");
            if (save != "")
            {
                string[] saveArr = save.Split(':');
                playerEntry = new LeaderboardEntry(saveArr[0], saveArr[1], float.Parse(saveArr[2]));
            }

            //setting default instance
            FirebaseApp.DefaultInstance.SetEditorDatabaseUrl(databaseUrl);
            //Get the user reference to keep everyones latest score
            userRef = FirebaseDatabase.DefaultInstance.GetReference(Application.productName + "/Users");

            //Set up the base reference based on the settings
            string baseRef = "";
            if (IncludeProductName && IncludeVersion)
                baseRef = Application.productName + "/" + Application.version.ToString().Replace('.', ',') + "/";
            else if (IncludeProductName)
                baseRef = Application.productName + "/";
            else if (IncludeVersion)
                baseRef = Application.version.ToString().Replace('.', ',') + "/";

            //Set up each leaderboards reference and set up their connections and runs the initial fetch
            if (EnableAllTime)
            {
                string allTimeId = baseRef + "1,AllTime";
                AllTime.DbRef = FirebaseDatabase.DefaultInstance.GetReference(allTimeId);
                AllTime.AddChild();
                AllTime.DbRef.ChildChanged += AllTime.ChangeChild;
                AllTime.DbRef.ChildAdded += AllTime.AddChild;
                AllTime.DbRef.ChildRemoved += AllTime.RemoveChild;
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
                Weekly.AddChild();
                Weekly.DbRef.ChildChanged += Weekly.ChangeChild;
                Weekly.DbRef.ChildAdded += Weekly.AddChild;
                Weekly.DbRef.ChildRemoved += Weekly.RemoveChild;
            }
            if (EnableDaily)
            {
                string dayId = baseRef + "3,Daily/" + DateTime.Now.Year + "," + DateTime.Now.DayOfYear;
                Daily.DbRef = FirebaseDatabase.DefaultInstance.GetReference(dayId);
                Daily.AddChild();
                Daily.DbRef.ChildChanged += Daily.ChangeChild;
                Daily.DbRef.ChildAdded += Daily.AddChild;
                Daily.DbRef.ChildRemoved += Daily.RemoveChild;
            }
            #endregion
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


    /// <summary>
    /// Finds your place in the leaderboard, sends your entry to the user database and saves your entry
    /// secures to that the entry has all the required fields filled in.
    /// </summary>
    /// <param name="entry">The entry you want to write to the leaderboard</param>
    public void WriteToLeaderboard(LeaderboardEntry entry = null)
    {
        if (entry == null)
            entry = new LeaderboardEntry(playerEntry);

        //Secures the variables on the entry
        if (entry.ID == "")
            entry.ID = userRef.Push().Key;
        //saves it as the local entry
        playerEntry = entry;
        if (playerHigh == null || entry.Compare(playerHigh))
            playerHigh = entry;
        FindClosest(entry);


        Dictionary<string, object> childUpdates = new Dictionary<string, object>();
        childUpdates["/Name"] = entry.Name;
        childUpdates["/Score"] = entry.Score;
        userRef.Child(entry.ID).UpdateChildrenAsync(childUpdates);

        string save = entry.ID + ":" + entry.Name + ":" + entry.Score.ToString();
        PlayerPrefs.SetString("LeaderboardSave", save);

        if (EnableAllTime && AllTime.ActiveWrite == null)
            AllTime.ActiveWrite = StartCoroutine(AllTime.Write(entry));
        else if (EnableAllTime)
            Debug.LogError("Failed at setting the all time leaderboard");

        if (EnableWeekly && Weekly.ActiveWrite == null)
            Weekly.ActiveWrite = StartCoroutine(Weekly.Write(entry));
        else if (EnableWeekly)
            Debug.LogError("Failed at setting the weekly leaderboard");

        if (EnableDaily && Daily.ActiveWrite == null)
            Daily.ActiveWrite = StartCoroutine(Daily.Write(entry));
        else if (EnableDaily)
            Debug.LogError("Failed at setting the daily leaderboard");
    }
    /// <summary>
    /// Finds the nearest entries in each of the leaderboards and outputs it to the three closest variables
    /// </summary>
    /// <param name="entry">The entry that you want to compare to the leaderboard</param>
    public void FindClosest(LeaderboardEntry entry = null)
    {
        if (entry == null)
            entry = playerHigh;
        for (int i = 0; i < AllTime.board.Count; i++)
        {
            if (entry.Compare(AllTime.board[i]))
            {
                if (i == 0)
                    closestAllTime = new LeaderboardEntry("NaN", "You", playerEntry.Score);
                else
                    closestAllTime = AllTime.board[Mathf.Clamp(i - 1, 0, AllTime.board.Count)];
                break;
            }
            if (i == AllTime.board.Count - 1)
                closestAllTime = AllTime.board[i];
        }
        for (int i = 0; i < Weekly.board.Count; i++)
        {
            if (entry.Compare(Weekly.board[i]))
            {
                if (i == 0)
                    closestWeekly = new LeaderboardEntry("NaN", "You", playerEntry.Score);
                else
                    closestWeekly = Weekly.board[Mathf.Clamp(i - 1, 0, Weekly.board.Count)];
                break;
            }
            if (i == Weekly.board.Count - 1)
                closestWeekly = Weekly.board[i];
        }
        for (int i = 0; i < Daily.board.Count; i++)
        {
            if (entry.Compare(Daily.board[i]))
            {
                if (i == 0)
                    closestDaily = new LeaderboardEntry("NaN", "You", playerEntry.Score);
                else
                    closestDaily = Daily.board[Mathf.Clamp(i - 1, 0, Daily.board.Count)];
                break;
            }
            if (i == Daily.board.Count - 1)
                closestDaily = Daily.board[i];
        }

        closestOverall = closestAllTime.Compare(closestWeekly) ? closestWeekly : closestAllTime;
        closestOverall = closestOverall.Compare(closestDaily) ? closestDaily : closestOverall;
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
        if (!LeaderboardManager.Instance.ReverseScore)
            return Score > other.Score;
        else
            return Score < other.Score;
    }
}
[System.Serializable]
public class Leaderboard : object
{
    public int Limit;
    public bool DatabaseLoaded = false;
    public List<LeaderboardEntry> board = new List<LeaderboardEntry>();
    internal Coroutine ActiveWrite;

    internal DatabaseReference DbRef;
    string idToFind;

    /// <summary>
    /// Recieves updates from the database and sets them into the leaderboard
    /// </summary>
    /// <param name="sender">Unused but required</param>
    /// <param name="args">The changes made</param>
    public void ChangeChild(object sender = null, ChildChangedEventArgs args = null)
    {
        if (args != null)
        {
            DataSnapshot snapshot = args.Snapshot;

            int old = -1;
            idToFind = snapshot.Key;
            old = board.FindIndex(FindByID);
            if (old != -1)
                board.RemoveAt(old);

            board.Add(
                new LeaderboardEntry(
                    snapshot.Key,
                    snapshot.Child("Name").Value.ToString(),
                    float.Parse(snapshot.Child("Score").Value.ToString())
            ));

            board.Sort(SortByScore);
            DatabaseLoaded = true;
        }
    }

    /// <summary>
    /// Recieves updates from the database and sets them into the leaderboard
    /// </summary>
    /// <param name="sender">Unused but required</param>
    /// <param name="args">The changes made</param>
    public void AddChild(object sender = null, ChildChangedEventArgs args = null)
    {
        if (args != null)
        {
            DataSnapshot snapshot = args.Snapshot;

            int old = -1;
            idToFind = snapshot.Key;

            old = board.FindIndex(FindByID);
            if (old != -1)
                board.RemoveAt(old);

            board.Add(
                new LeaderboardEntry(
                    snapshot.Key,
                    snapshot.Child("Name").Value.ToString(),
                    float.Parse(snapshot.Child("Score").Value.ToString())
            ));


            board.Sort(SortByScore);
            DatabaseLoaded = true;
        }
        else
            DatabaseLoaded = true;
    }

    /// <summary>
    /// Recieves updates from the database and sets them into the leaderboard
    /// </summary>
    /// <param name="sender">Unused but required</param>
    /// <param name="args">The changes made</param>
    public void RemoveChild(object sender = null, ChildChangedEventArgs args = null)
    {
        if (args != null)
        {
            DataSnapshot snapshot = args.Snapshot;

            idToFind = snapshot.Key;
            board.RemoveAt(board.FindIndex(FindByID));

            board.Sort(SortByScore);
            DatabaseLoaded = true;
        }
    }

    /// <summary>
    /// Ienumerator that compares and writes the entry to the leaderboard
    /// </summary>
    /// <param name="entry">The entry to write</param>
    /// <returns></returns>
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
                DatabaseLoaded = true;
            }
            else
            {
                board.RemoveAt(early);
                for (int i = 0; i <= early; i++)
                {
                    if (entry.Compare(board[i]))
                    {
                        board.Insert(i, entry);
                        break;
                    }
                }
            }
        }
        else
        {
            bool placed = false;
            for (int i = 0; i < board.Count; i++)
            {
                if (entry.Compare(board[i]))
                {
                    board.Insert(i, entry);
                    placed = true;
                    break;
                }
            }

            if (!placed && board.Count < Limit || Limit == 0)
                board.Add(entry);
            else if(!placed)
                DatabaseLoaded = true;
        }
        board.Sort(SortByScore);

        Dictionary<string, object> entUpdates = new Dictionary<string, object>();
        for (int i = 0; i < board.Count; i++)
        {
            entUpdates["/" + board[i].ID + "/Name"] = board[i].Name;
            entUpdates["/" + board[i].ID + "/Score"] = board[i].Score;
        }
        bool writeWait = false;
        DbRef.UpdateChildrenAsync(entUpdates).ContinueWith(task => { writeWait = true; });

        if(board.Count > Limit)
        {
            for (int i = Limit; i < board.Count; i++)
            {
                DbRef.Child(board[i].ID).RemoveValueAsync();
            }
        }

        while (!writeWait)
            yield return new WaitForEndOfFrame();

        ActiveWrite = null;
    }
    /// <summary>
    /// Find the entry in the board of the matching ID wich is the separated variable idToFind
    /// </summary>
    bool FindByID(LeaderboardEntry p1)
    {
        return p1.ID == idToFind;
    }
    /// <summary>
    /// Comparitor for the list sorting function
    /// </summary>
    public int SortByScore(LeaderboardEntry entry1, LeaderboardEntry entry2)
    {
        if (LeaderboardManager.Instance.ReverseScore)
            return entry1.Score.CompareTo(entry2.Score);
        else
            return -entry1.Score.CompareTo(entry2.Score);
    }
}
#if UNITY_EDITOR
/// <summary>
/// Custom inspector for the leaderboard manager
/// </summary>
[CustomEditor(typeof(LeaderboardManager))]
public class CustomLeaderboardInspector: Editor
{
    bool custom = true;
    bool settings = false;

    Vector2 allScrollPos;
    Vector2 WeekScrollPos;
    Vector2 dayScrollPos;

    bool ShowAllTime;
    bool ShowWeekly;
    bool ShowDaily;

    LeaderboardManager scr;

    private void OnEnable()
    {
        scr = (LeaderboardManager)target;
    }
    /// <summary>
    /// Main function of the inspector
    /// </summary>
    public override void OnInspectorGUI()
    {
        //bool to show/hide the custom bit
        custom = EditorGUILayout.ToggleLeft("Custom Inspector",custom);
        if (custom)
        {
            //The players entry
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player entry:");
            EditorGUILayout.BeginHorizontal();
            scr.playerEntry.Name = EditorGUILayout.TextField(scr.playerEntry.Name);
            scr.playerEntry.Score = EditorGUILayout.FloatField(scr.playerEntry.Score);
            EditorGUILayout.EndHorizontal();
            //Button that sends the current entry to the leaderboard
            if (GUILayout.Button("Add Entry"))
                scr.WriteToLeaderboard(scr.playerEntry);
            //Showing the highscore
            if (scr.playerHigh != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Highscore:", GUILayout.Width(125));
                EditorGUILayout.LabelField(scr.playerHigh.Name, GUILayout.Width(125));
                EditorGUILayout.LabelField(scr.playerHigh.Score.ToString(), GUILayout.Width(125));
                EditorGUILayout.EndHorizontal();
            }
            //showing the closest in alltime
            if (scr.closestOverall != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Closest Overall:", GUILayout.Width(125));
                EditorGUILayout.LabelField(scr.closestOverall.Name, GUILayout.Width(125));
                EditorGUILayout.LabelField(scr.closestOverall.Score.ToString(), GUILayout.Width(125));
                EditorGUILayout.EndHorizontal();
            }
            //showing the closest in alltime
            if (scr.closestAllTime != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Closest Alltime:", GUILayout.Width(125));
                EditorGUILayout.LabelField(scr.closestAllTime.Name, GUILayout.Width(125));
                EditorGUILayout.LabelField(scr.closestAllTime.Score.ToString(), GUILayout.Width(125));
                EditorGUILayout.EndHorizontal();
            }
            //showing the closest in weekly
            if (scr.closestWeekly != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Closest Weekly:", GUILayout.Width(125));
                EditorGUILayout.LabelField(scr.closestWeekly.Name, GUILayout.Width(125));
                EditorGUILayout.LabelField(scr.closestWeekly.Score.ToString(), GUILayout.Width(125));
                EditorGUILayout.EndHorizontal();
            }
            //showing the closest in daily
            if (scr.closestDaily != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Closest Daily:", GUILayout.Width(125));
                EditorGUILayout.LabelField(scr.closestDaily.Name, GUILayout.Width(125));
                EditorGUILayout.LabelField(scr.closestDaily.Score.ToString(), GUILayout.Width(125));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
            //Separating based on if the game is playing, to hide the settings you're not allowed to touch
            if (!Application.isPlaying)
            {
                //Toggeling the leaderboards and showing their specific settings
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
                //The settings foldout, serves to have them separated and hidden when they're not in use
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
                //checking if leaderboard is enabled
                if (scr.EnableAllTime)
                {
                    //Basic foldout to allow you to hide it
                    ShowAllTime = EditorGUILayout.Foldout(ShowAllTime, "AllTime Leaderboard");
                    if (ShowAllTime)
                    {
                        //indent the leaderboard, cause fancy
                        EditorGUI.indentLevel = 1;

                        //The signifier part,
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("nr.", GUILayout.Width(30));
                        EditorGUILayout.LabelField("Name:", GUILayout.Width(150));
                        EditorGUILayout.LabelField("Score:", GUILayout.Width(150));
                        EditorGUILayout.EndHorizontal();
                        //Scrollarea setup
                        allScrollPos = EditorGUILayout.BeginScrollView(allScrollPos, false, false, GUILayout.Height(Mathf.Clamp(21 * scr.AllTime.Limit, 0, 300)), GUILayout.Width(330));
                        //going through all leaderboard entries and displaying them
                        for (int i = 0; i < scr.AllTime.board.Count; i++)
                        {
                            //making the current players highscore green
                            if(scr.playerEntry.ID == scr.AllTime.board[i].ID)
                                GUI.contentColor = Color.green;
                            //horizontaly displaying the entry
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField((i+1).ToString() + ".", GUILayout.Width(50));
                            EditorGUILayout.LabelField(scr.AllTime.board[i].Name, GUILayout.Width(125));
                            EditorGUILayout.LabelField(scr.AllTime.board[i].Score.ToString(), GUILayout.Width(125));
                            EditorGUILayout.EndHorizontal();
                            //stop being green
                            GUI.contentColor = Color.white;
                        }
                        //scrollarea end
                        EditorGUILayout.EndScrollView();
                        //stop indent
                        EditorGUI.indentLevel = 0;
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                //same as above, not commenting the samey stuff
                if (scr.EnableWeekly)
                {
                    ShowWeekly = EditorGUILayout.Foldout(ShowWeekly, "Weekly Leaderboard");
                    if (ShowWeekly)
                    {
                        EditorGUI.indentLevel = 1;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("nr.", GUILayout.Width(50));
                        EditorGUILayout.LabelField("Name:", GUILayout.Width(125));
                        EditorGUILayout.LabelField("Score:", GUILayout.Width(125));
                        EditorGUILayout.EndHorizontal();

                        WeekScrollPos = EditorGUILayout.BeginScrollView(WeekScrollPos, false, false, GUILayout.Height(Mathf.Clamp(21 * scr.Weekly.Limit, 0, 300)), GUILayout.Width(330));
                        for (int i = 0; i < scr.Weekly.board.Count; i++)
                        {
                            if (scr.playerEntry.ID == scr.Weekly.board[i].ID)
                                GUI.contentColor = Color.green;
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField((i+1).ToString() + ".", GUILayout.Width(50));
                            EditorGUILayout.LabelField(scr.Weekly.board[i].Name, GUILayout.Width(125));
                            EditorGUILayout.LabelField(scr.Weekly.board[i].Score.ToString(), GUILayout.Width(125));
                            EditorGUILayout.EndHorizontal();
                            GUI.contentColor = Color.white;
                        }
                        EditorGUILayout.EndScrollView();
                        EditorGUI.indentLevel = 0;
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                //same as above, not commenting the samey stuff
                if (scr.EnableDaily)
                {
                    ShowDaily = EditorGUILayout.Foldout(ShowDaily, "Daily Leaderboard");
                    if (ShowDaily)
                    {
                        EditorGUI.indentLevel = 1;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("nr.", GUILayout.Width(50));
                        EditorGUILayout.LabelField("Name:", GUILayout.Width(125));
                        EditorGUILayout.LabelField("Score:", GUILayout.Width(125));
                        EditorGUILayout.EndHorizontal();
                        dayScrollPos = EditorGUILayout.BeginScrollView(dayScrollPos, false, false, GUILayout.Height(Mathf.Clamp(21*scr.Daily.Limit,0,300)),GUILayout.Width(330));
                        for (int i = 0; i < scr.Daily.board.Count; i++)
                        {
                            if (scr.playerEntry.ID == scr.Daily.board[i].ID)
                                GUI.contentColor = Color.green;
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField((i+1).ToString() + ".", GUILayout.Width(50));
                            EditorGUILayout.LabelField(scr.Daily.board[i].Name, GUILayout.Width(125));
                            EditorGUILayout.LabelField(scr.Daily.board[i].Score.ToString(), GUILayout.Width(125));
                            EditorGUILayout.EndHorizontal();
                            GUI.contentColor = Color.white;
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
            //Show the base if it's not in custom mode
            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
        //Mark any changes for saving
        if (GUI.changed)
            EditorUtility.SetDirty(scr);
    }
}
#endif
