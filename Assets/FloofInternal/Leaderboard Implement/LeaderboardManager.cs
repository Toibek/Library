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
    public List<LeaderboardGroup> Leaderboards;

    [Header("Settings")]
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
            //Loads up your last sent entry
            string save = PlayerPrefs.GetString("LeaderboardSave");
            if (save != "")
            {
                string[] saveArr = save.Split(':');
                playerEntry = new LeaderboardEntry(saveArr[0], saveArr[1], float.Parse(saveArr[2]));
            }

            for (int i = 0; i < Leaderboards.Count; i++)
                Leaderboards[i].Init();
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
    /// Adds a new leaderboard to the leaderboards list
    /// </summary>
    /// <param name="name"></param>
    public void AddLeaderboard(string name) 
    { 
        if (Leaderboards.Count > 0)
            Leaderboards.Add(new LeaderboardGroup(name, Leaderboards[0]));
        else
            Leaderboards.Add(new LeaderboardGroup("Main"));
    }
    /// <summary>
    /// Removes a leaderboard from the leaderboards list
    /// </summary>
    /// <param name="group"></param>
    public void RemoveLeaderboard(LeaderboardGroup group) => Leaderboards.Remove(group);
    public void WriteToLeaderboard(float score) => WriteToLeaderboard(playerEntry.Score = score);
    public void WriteToLeaderboard(string leaderboard,float score) => WriteToLeaderboard(leaderboard,playerEntry.Score = score);
    public void WriteToLeaderboard(LeaderboardGroup leaderboard, float score) => WriteToLeaderboard(leaderboard, playerEntry.Score = score);
    /// <summary>
    /// Writes to the primary leaderboard
    /// </summary>
    /// <param name="entry"></param>
    public void WriteToLeaderboard(LeaderboardEntry entry) => Leaderboards[0]?.WriteToLeaderboard(entry);
    /// <summary>
    /// Writes to the selected leaderboard, by name
    /// </summary>
    /// <param name="name"></param>
    /// <param name="entry"></param>
    public void WriteToLeaderboard(string name,LeaderboardEntry entry) => WriteToLeaderboard(FindGroupByName(name),entry);
    /// <summary>
    /// Writes to the selectedLeaderboard, by leaderboard
    /// </summary>
    /// <param name="Leaderboard"></param>
    /// <param name="entry"></param>
    public void WriteToLeaderboard(LeaderboardGroup Leaderboard, LeaderboardEntry entry) => Leaderboard.WriteToLeaderboard(entry);
    public void FindClosest(float score) => FindClosest(playerEntry.Score = score);
    public void FindClosest(string leaderboard,float score) => FindClosest(leaderboard,playerEntry.Score = score);
    public void FindClosest(LeaderboardGroup leaderboard,float score) => FindClosest(leaderboard,playerEntry.Score = score);
    /// <summary>
    /// Finds the closest entry of the primary leaderboard
    /// </summary>
    /// <param name="entry"></param>
    public void FindClosest(LeaderboardEntry entry) => Leaderboards[0]?.FindClosest(entry);
    /// <summary>
    /// Finds the closest entry of the named leaderboard
    /// </summary>
    /// <param name="name"></param>
    /// <param name="entry"></param>
    public void FindClosest(string name, LeaderboardEntry entry) => FindClosest(FindGroupByName(name), entry);
    /// <summary>
    /// Finds the closest entry in the sent leaderboard
    /// </summary>
    /// <param name="Leaderboard"></param>
    /// <param name="entry"></param>
    public void FindClosest(LeaderboardGroup Leaderboard, LeaderboardEntry entry) => Leaderboard.FindClosest(entry);
    /// <summary>
    /// Finds the leaderboard by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public LeaderboardGroup FindGroupByName(string name)
    {
        for (int i = 0; i < Leaderboards.Count; i++)
            if (Leaderboards[i].Name == name)
                return Leaderboards[i];

        Debug.LogError("Leaderboard: " + name + " not Found");
        return null;
    }
}
[System.Serializable]
public class LeaderboardEntry : object
{
    public string ID = default;
    public string Name = default;
    public float Score = default;

    /// <summary>
    /// Creates a new entry that's a copy of a old one
    /// </summary>
    /// <param name="old"></param>
    public LeaderboardEntry(LeaderboardEntry old)
    {
        ID = old.ID;
        Name = old.Name;
        Score = old.Score;
    }
    /// <summary>
    /// Creates a new entry with full control of the values
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <param name="score"></param>
    public LeaderboardEntry(string id ,string name, float score)
    {
        ID = id;
        Name = name;
        Score = score;
    }
    /// <summary>
    /// Compare function so that you can easily compare one entry to the other by score
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
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
    ///Creates a new leaderboard with a set limit
    /// </summary>
    /// <param name="Limit"></param>
    public Leaderboard(int Limit) => this.Limit = Limit;
    /// <summary>
    /// Creates a new leaderboard with default settings
    /// </summary>
    public Leaderboard() { }
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

[System.Serializable]
public class LeaderboardGroup : object
{
    public string Name = "New";

    public bool EnableAllTime;
    public Leaderboard AllTime;

    public bool EnableWeekly;
    public Leaderboard Weekly;


    public bool EnableDaily;
    public Leaderboard Daily;

    public LeaderboardEntry closestOverall;
    public LeaderboardEntry closestAllTime;
    public LeaderboardEntry closestWeekly;
    public LeaderboardEntry closestDaily;

    internal Vector2 allScrollPos;
    internal Vector2 WeekScrollPos;
    internal Vector2 dayScrollPos;

    internal bool Show;
    internal bool ShowAllTime;
    internal bool ShowWeekly;
    internal bool ShowDaily;

    DatabaseReference userRef;
    /// <summary>
    /// Initializes the leaderboards attatched so that they're ready to use
    /// </summary>
    public void Init()
    {
        //setting default instance
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl(LeaderboardManager.Instance.databaseUrl);
        //Get the user reference to keep everyones latest score
        userRef = FirebaseDatabase.DefaultInstance.GetReference(Application.productName + "/Users");

        //Set up the base reference based on the settings
        string baseRef = "";
        if (LeaderboardManager.Instance.IncludeProductName && LeaderboardManager.Instance.IncludeVersion)
            baseRef = Application.productName + "/" + Application.version.ToString().Replace('.', ',') + "/";
        else if (LeaderboardManager.Instance.IncludeProductName)
            baseRef = Application.productName + "/";
        else if (LeaderboardManager.Instance.IncludeVersion)
            baseRef = Application.version.ToString().Replace('.', ',') + "/";
        baseRef += Name + "/";

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
    }
    /// <summary>
    /// Finds your place in the leaderboard, sends your entry to the user database and saves your entry
    /// secures to that the entry has all the required fields filled in.
    /// </summary>
    /// <param name="entry">The entry you want to write to the leaderboard</param>
    public void WriteToLeaderboard(LeaderboardEntry entry = null)
    {
        if (entry == null)
            entry = new LeaderboardEntry(LeaderboardManager.Instance.playerEntry);

        //Secures the variables on the entry
        if (entry.ID == "")
            entry.ID = userRef.Push().Key;
        //saves it as the local entry
        LeaderboardManager.Instance.playerEntry = entry;
        if (LeaderboardManager.Instance.playerHigh == null || entry.Compare(LeaderboardManager.Instance.playerHigh))
            LeaderboardManager.Instance.playerHigh = entry;
        FindClosest(entry);


        Dictionary<string, object> childUpdates = new Dictionary<string, object>();
        childUpdates["/Name"] = entry.Name;
        childUpdates["/Score"] = entry.Score;
        userRef.Child(entry.ID).UpdateChildrenAsync(childUpdates);

        string save = entry.ID + ":" + entry.Name + ":" + entry.Score.ToString();
        PlayerPrefs.SetString("LeaderboardSave", save);

        if (EnableAllTime && AllTime.ActiveWrite == null)
            AllTime.ActiveWrite = LeaderboardManager.Instance.StartCoroutine(AllTime.Write(entry));
        else if (EnableAllTime)
            Debug.LogError("Failed at setting the all time leaderboard");

        if (EnableWeekly && Weekly.ActiveWrite == null)
            Weekly.ActiveWrite = LeaderboardManager.Instance.StartCoroutine(Weekly.Write(entry));
        else if (EnableWeekly)
            Debug.LogError("Failed at setting the weekly leaderboard");

        if (EnableDaily && Daily.ActiveWrite == null)
            Daily.ActiveWrite = LeaderboardManager.Instance.StartCoroutine(Daily.Write(entry));
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
            entry = LeaderboardManager.Instance.playerHigh;
        for (int i = 0; i < AllTime.board.Count; i++)
        {
            if (entry.Compare(AllTime.board[i]))
            {
                if (i == 0)
                    closestAllTime = new LeaderboardEntry("NaN", "You", LeaderboardManager.Instance.playerEntry.Score);
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
                    closestWeekly = new LeaderboardEntry("NaN", "You", LeaderboardManager.Instance.playerEntry.Score);
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
                    closestDaily = new LeaderboardEntry("NaN", "You", LeaderboardManager.Instance.playerEntry.Score);
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
    /// <summary>
    /// Used to make a copy of an old leaderboard, copies all settings and makes new boards
    /// </summary>
    /// <param name="name">the new name of the leaderboard</param>
    /// <param name="old">The old leaderboardgroup to copy</param>
    public LeaderboardGroup(string name, LeaderboardGroup old = null)
    {
        Name = name;
        if (old != null)
        {
            EnableAllTime = old.EnableAllTime;
            AllTime = new Leaderboard(old.AllTime.Limit);
            EnableWeekly = old.EnableWeekly;
            Weekly = new Leaderboard(old.Weekly.Limit);
            EnableDaily = old.EnableDaily;
            Daily = new Leaderboard(old.Daily.Limit);
        }
        else
        {
            AllTime = new Leaderboard();
            Weekly = new Leaderboard();
            Daily = new Leaderboard();
        }
    }
    /// <summary>
    /// Copies a old leaderboardgroup, without setting a name
    /// </summary>
    /// <param name="old"></param>
    public LeaderboardGroup(LeaderboardGroup old = null)
    {
        if (old != null)
        {
            EnableAllTime = old.EnableAllTime;
            AllTime = new Leaderboard(old.AllTime.Limit);
            EnableWeekly = old.EnableWeekly;
            Weekly = new Leaderboard(old.Weekly.Limit);
            EnableDaily = old.EnableDaily;
            Daily = new Leaderboard(old.Daily.Limit);
        }
        else
        {
            AllTime = new Leaderboard();
            Weekly = new Leaderboard();
            Daily = new Leaderboard();
        }
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
    string setName;
    LeaderboardManager scr;

    private void OnEnable()
    {
        scr = (LeaderboardManager)target;
    }
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
            if (GUILayout.Button("Write Entry"))
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

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            setName = EditorGUILayout.TextField(setName,GUILayout.Width(200));
            if (GUILayout.Button("Create Leaderboard"))
                scr.AddLeaderboard(setName);
            GUILayout.EndHorizontal();

            for (int i = 0; i < scr.Leaderboards.Count; i++)
            {
                LeaderboardGroup lbg = scr.Leaderboards[i];
                GUILayout.BeginHorizontal();
                lbg.Show = EditorGUILayout.BeginFoldoutHeaderGroup(lbg.Show, lbg.Name);
                if (GUILayout.Button("Remove Leaderboard", GUILayout.Width(175)))
                    scr.RemoveLeaderboard(lbg);
                GUILayout.EndHorizontal();
                if (lbg.Show)
                {
                    EditorGUI.indentLevel = 1;
                    lbg.Name = EditorGUILayout.TextField("Name:", lbg.Name);
                    EditorGUILayout.Space();
                    //showing the closest in alltime
                    if (lbg.closestOverall != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Closest Overall:", GUILayout.Width(125));
                        EditorGUILayout.LabelField(lbg.closestOverall.Name, GUILayout.Width(125));
                        EditorGUILayout.LabelField(lbg.closestOverall.Score.ToString(), GUILayout.Width(125));
                        EditorGUILayout.EndHorizontal();
                    }
                    //showing the closest in alltime
                    if (lbg.closestAllTime != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Closest Alltime:", GUILayout.Width(125));
                        EditorGUILayout.LabelField(lbg.closestAllTime.Name, GUILayout.Width(125));
                        EditorGUILayout.LabelField(lbg.closestAllTime.Score.ToString(), GUILayout.Width(125));
                        EditorGUILayout.EndHorizontal();
                    }
                    //showing the closest in weekly
                    if (lbg.closestWeekly != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Closest Weekly:", GUILayout.Width(125));
                        EditorGUILayout.LabelField(lbg.closestWeekly.Name, GUILayout.Width(125));
                        EditorGUILayout.LabelField(lbg.closestWeekly.Score.ToString(), GUILayout.Width(125));
                        EditorGUILayout.EndHorizontal();
                    }
                    //showing the closest in daily
                    if (lbg.closestDaily != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Closest Daily:", GUILayout.Width(125));
                        EditorGUILayout.LabelField(lbg.closestDaily.Name, GUILayout.Width(125));
                        EditorGUILayout.LabelField(lbg.closestDaily.Score.ToString(), GUILayout.Width(125));
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.Space();
                    //Separating based on if the game is playing, to hide the settings you're not allowed to touch
                    if (!Application.isPlaying)
                    {
                        //Toggeling the leaderboards and showing their specific settings
                        lbg.EnableAllTime = EditorGUILayout.Toggle("Enable AllTime Leaderboard", lbg.EnableAllTime);
                        if (lbg.EnableAllTime)
                        {
                            EditorGUI.indentLevel = 1;
                            lbg.AllTime.Limit = EditorGUILayout.IntField("Alltime limit", lbg.AllTime.Limit);
                            EditorGUI.indentLevel = 0;
                        }
                        lbg.EnableWeekly = EditorGUILayout.Toggle("Enable Weekly Leaderboard", lbg.EnableWeekly);
                        if (lbg.EnableWeekly)
                        {
                            EditorGUI.indentLevel = 1;
                            lbg.Weekly.Limit = EditorGUILayout.IntField("Weekly limit", lbg.Weekly.Limit);
                            EditorGUI.indentLevel = 0;
                        }
                        lbg.EnableDaily = EditorGUILayout.Toggle("Enable Daily Leaderboard", lbg.EnableDaily);
                        if (lbg.EnableDaily)
                        {
                            EditorGUI.indentLevel = 1;
                            lbg.Daily.Limit = EditorGUILayout.IntField("Daily limit", lbg.Daily.Limit);
                            EditorGUI.indentLevel = 0;
                        }

                    }
                    else
                    {
                        //checking if leaderboard is enabled
                        if (lbg.EnableAllTime)
                        {
                            //Basic foldout to allow you to hide it
                            lbg.ShowAllTime = EditorGUILayout.Foldout(lbg.ShowAllTime, "AllTime Leaderboard");
                            if (lbg.ShowAllTime)
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
                                lbg.allScrollPos = EditorGUILayout.BeginScrollView(lbg.allScrollPos, false, false, GUILayout.Height(Mathf.Clamp(21 * lbg.AllTime.Limit, 0, 300)), GUILayout.Width(330));
                                //going through all leaderboard entries and displaying them
                                for (int k = 0; k < lbg.AllTime.board.Count; k++)
                                {
                                    //making the current players highscore green
                                    if (scr.playerEntry.ID == lbg.AllTime.board[k].ID)
                                        GUI.contentColor = Color.green;
                                    //horizontaly displaying the entry
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField((k + 1).ToString() + ".", GUILayout.Width(50));
                                    EditorGUILayout.LabelField(lbg.AllTime.board[k].Name, GUILayout.Width(125));
                                    EditorGUILayout.LabelField(lbg.AllTime.board[k].Score.ToString(), GUILayout.Width(125));
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
                        if (lbg.EnableWeekly)
                        {
                            lbg.ShowWeekly = EditorGUILayout.Foldout(lbg.ShowWeekly, "Weekly Leaderboard");
                            if (lbg.ShowWeekly)
                            {
                                EditorGUI.indentLevel = 1;
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("nr.", GUILayout.Width(50));
                                EditorGUILayout.LabelField("Name:", GUILayout.Width(125));
                                EditorGUILayout.LabelField("Score:", GUILayout.Width(125));
                                EditorGUILayout.EndHorizontal();

                                lbg.WeekScrollPos = EditorGUILayout.BeginScrollView(lbg.WeekScrollPos, false, false, GUILayout.Height(Mathf.Clamp(21 * lbg.Weekly.Limit, 0, 300)), GUILayout.Width(330));
                                for (int k = 0; k < lbg.Weekly.board.Count; k++)
                                {
                                    if (scr.playerEntry.ID == lbg.Weekly.board[k].ID)
                                        GUI.contentColor = Color.green;
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField((k + 1).ToString() + ".", GUILayout.Width(50));
                                    EditorGUILayout.LabelField(lbg.Weekly.board[k].Name, GUILayout.Width(125));
                                    EditorGUILayout.LabelField(lbg.Weekly.board[k].Score.ToString(), GUILayout.Width(125));
                                    EditorGUILayout.EndHorizontal();
                                    GUI.contentColor = Color.white;
                                }
                                EditorGUILayout.EndScrollView();
                                EditorGUI.indentLevel = 0;
                            }
                            EditorGUILayout.EndFoldoutHeaderGroup();
                        }
                        //same as above, not commenting the samey stuff
                        if (lbg.EnableDaily)
                        {
                            lbg.ShowDaily = EditorGUILayout.Foldout(lbg.ShowDaily, "Daily Leaderboard");
                            if (lbg.ShowDaily)
                            {
                                EditorGUI.indentLevel = 1;
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("nr.", GUILayout.Width(50));
                                EditorGUILayout.LabelField("Name:", GUILayout.Width(125));
                                EditorGUILayout.LabelField("Score:", GUILayout.Width(125));
                                EditorGUILayout.EndHorizontal();
                                lbg.dayScrollPos = EditorGUILayout.BeginScrollView(lbg.dayScrollPos, false, false, GUILayout.Height(Mathf.Clamp(21 * lbg.Daily.Limit, 0, 300)), GUILayout.Width(330));
                                for (int k = 0; k < lbg.Daily.board.Count; k++)
                                {
                                    if (scr.playerEntry.ID == lbg.Daily.board[k].ID)
                                        GUI.contentColor = Color.green;
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField((k + 1).ToString() + ".", GUILayout.Width(50));
                                    EditorGUILayout.LabelField(lbg.Daily.board[k].Name, GUILayout.Width(125));
                                    EditorGUILayout.LabelField(lbg.Daily.board[k].Score.ToString(), GUILayout.Width(125));
                                    EditorGUILayout.EndHorizontal();
                                    GUI.contentColor = Color.white;
                                }
                                EditorGUILayout.EndScrollView();
                                EditorGUI.indentLevel = 0;
                            }
                            EditorGUILayout.EndFoldoutHeaderGroup();
                        }
                    }

                    EditorGUI.indentLevel = 0;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            if (!Application.isPlaying)
            {
                EditorGUILayout.Space();
                //The settings foldout, serves to have them separated and hidden when they're not in use
                settings = EditorGUILayout.BeginFoldoutHeaderGroup(settings, "Settings:");
                if (settings)
                {
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.HelpBox("Only change these if the leaderboards are empty", MessageType.Info);
                    scr.ReverseScore = EditorGUILayout.Toggle("Reverse boards", scr.ReverseScore);
                    scr.IncludeProductName = EditorGUILayout.Toggle("Include Name", scr.IncludeProductName);
                    scr.IncludeVersion = EditorGUILayout.Toggle("Include version", scr.IncludeVersion);
                    scr.databaseUrl = EditorGUILayout.TextField("Database Url", scr.databaseUrl);
                    EditorGUI.indentLevel = 0;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
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
