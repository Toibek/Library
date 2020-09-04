using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AdManager : MonoBehaviour, IUnityAdsListener
{
    public bool testMode = false;
    public string androidGameID = "";
    public string IOSGameId = "";
    public string videoName = "video";
    public string rewardedName = "rewardedVideo";
    public float minutesBeforeFirstVideo;
    public float minutesBetweenVideos;

    internal bool VideoAvaliable = true;

    internal bool showGameID;
    internal bool showAdNames;
    internal bool timeSettings;
    #region ADInit && Singleton && DDOL
    public static AdManager Instance;
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            Advertisement.AddListener(this);
#if UNITY_IOS
            Advertisement.Initialize(IOSGameID, TestMode);
#elif UNITY_ANDROID
            Advertisement.Initialize(androidGameID, testMode);
#endif
            DontDestroyOnLoad(gameObject);
        }
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Advertisement.RemoveListener(this);
            Instance = null;
        }
    }
    #endregion

    /// <summary>
    /// Used to check for time before first ad
    /// </summary>
    private void Start()
    {
        if(minutesBeforeFirstVideo > 0)
        {
            VideoAvaliable = false;
            Invoke("enableVideo", minutesBeforeFirstVideo * 60);
        }
    }
    /// <summary>
    /// the base delegate for completing an ad
    /// </summary>
    /// <param name="skipped">True when the ad got skipped, false if it got to complete</param>
    public delegate void Delegate_Completed(bool skipped);

    #region starting ads
    /// <summary>
    /// Starts a video ad if it's allowed
    /// </summary>
    public void PlayVideo()
    {
        if (VideoAvaliable && CompletedVideo != null)
        {
            if (Advertisement.IsReady(videoName))
                Advertisement.Show("video");

            //Disable then invoke videoavaliable, to make it so we can't run them too often
            if (minutesBetweenVideos > 0)
            {
                VideoAvaliable = false;
                Invoke("enableVideo", minutesBetweenVideos*60);
            }
        }
        else if (CompletedVideo == null)
            Debug.LogWarning("Video ad wanted to play without the delegate");
    }
    void enableVideo() => VideoAvaliable = true;
    //Delegate for completing video ads, needed to run an ad
    public Delegate_Completed CompletedVideo;
    /// <summary>
    /// Starts a rewarded ad if it's allowed
    /// </summary>
    public void PlayRewarded()
    {
        if (CompletedRewarded != null)
        {
            if (Advertisement.IsReady(rewardedName))
                Advertisement.Show(rewardedName);
        }
        else if (CompletedRewarded == null)
            Debug.LogWarning("Rewarded video ad wanted to play without setting the output");
    }
    //Delegate for completing reward ads, needed to run an ad
    public Delegate_Completed CompletedRewarded;
    #endregion

    #region IUnityAdsListener
    /// <summary>
    /// Runs when any ad is ready
    /// </summary>
    /// <param name="placementId">the ad that is ready</param>
    public void OnUnityAdsReady(string placementId)
    {

    }
    /// <summary>
    /// Runs when any ad starts
    /// </summary>
    /// <param name="placementId">the ad that started</param>
    public void OnUnityAdsDidStart(string placementId)
    {

    }
    /// <summary>
    /// Runs when any ad is finished playing
    /// </summary>
    /// <param name="placementId">the ad that finished</param>
    /// <param name="showResult">wether or not it got skipped</param>
    public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    {
        //Serves to make sure it runs the right delegate
        if(placementId == videoName)
        {
            if (showResult == ShowResult.Finished)
                CompletedVideo?.Invoke(false);
            else
                CompletedVideo?.Invoke(true);
            CompletedVideo = null;
        }
        else if(placementId == rewardedName)
        {
            if (showResult == ShowResult.Finished)
                CompletedRewarded?.Invoke(false);
            else
                CompletedRewarded?.Invoke(true);
            CompletedRewarded = null;
        }
    }
    /// <summary>
    /// Runs when any ad encounters and error, just logging the error for now
    /// </summary>
    /// <param name="message">The error message?</param>
    public void OnUnityAdsDidError(string message)
    {
        Debug.LogError(message);
    }
    #endregion
}
#region CustomInspector
#if UNITY_EDITOR
[CustomEditor(typeof(AdManager))]
[CanEditMultipleObjects]
public class AdManagerInspector : Editor
{
    AdManager mng;
    private void OnEnable()
    {
        mng = (AdManager)target;
    }
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Remember to also check the Ads option in Services", MessageType.Info);

        mng.testMode = EditorGUILayout.Toggle("Test mode:", mng.testMode);
        EditorGUILayout.Space(1);

        mng.showGameID = EditorGUILayout.Foldout(mng.showGameID, "Game ID:");
        if (mng.androidGameID == "" || mng.IOSGameId == "")
            mng.showGameID = true;
        if (mng.showGameID)
        {
            if (mng.androidGameID == "" || mng.IOSGameId == "")
                EditorGUILayout.HelpBox("These needs to be filled out for the ads to work, Ids are in the ad dashboard:", MessageType.Error);
            else
                EditorGUILayout.HelpBox("If the ads aren't working please double check these, Ids are in the ad dashboard:", MessageType.Info);

            if (GUILayout.Button("Dashboard"))
                Application.OpenURL("https://dashboard.unity3d.com/monetization");


            mng.androidGameID = EditorGUILayout.TextField("Android", mng.androidGameID);
            mng.IOSGameId = EditorGUILayout.TextField("IOS", mng.IOSGameId);
        }

        mng.showAdNames = EditorGUILayout.Foldout(mng.showAdNames, "Ad names:");
        if (mng.showAdNames)
        {
            EditorGUILayout.HelpBox("Only change theese if the ad names in the dashboard has been changed", MessageType.Info);
            mng.videoName = EditorGUILayout.TextField("Video:", mng.videoName);
            mng.rewardedName = EditorGUILayout.TextField("Rewarded:", mng.rewardedName);
        }
        mng.timeSettings = EditorGUILayout.Foldout(mng.timeSettings, "Time settings:");
        if (mng.timeSettings)
        {
            mng.minutesBeforeFirstVideo = EditorGUILayout.FloatField("Minutes before first video ad", mng.minutesBeforeFirstVideo);
            mng.minutesBetweenVideos = EditorGUILayout.FloatField("Minutes between each video ad", mng.minutesBetweenVideos);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(this);
            EditorUtility.SetDirty(target);
        }

    }
}
#endif
#endregion


