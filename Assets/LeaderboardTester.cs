using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardTester : MonoBehaviour
{
    [SerializeField] bool auto = default;
    [SerializeField] float time = default;
    [Space]
    [SerializeField] LeaderboardEntry entry = default;
    [SerializeField] List<LeaderboardEntry> usedEntries = new List<LeaderboardEntry>();
    // Start is called before the first frame update
    Coroutine autoroutine;
    void Start()
    {
        autoroutine = StartCoroutine(autoTest());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            sendTest();
        }
        if (auto && autoroutine == null)
            autoroutine = StartCoroutine(autoTest());
    }
    IEnumerator autoTest()
    {
        while (auto)
        {
            yield return new WaitForSeconds(time);
            sendTest();
        }
        autoroutine = null;
    }
    void sendTest()
    {
        if(Random.Range(0,2) == 0)
        {
            if (entry.Name != "")
                usedEntries.Add(entry);
            entry = new LeaderboardEntry(Random.Range(1, 1000).ToString(), Random.Range(1, 1000).ToString(), Random.Range(0, 1000));
            LeaderboardManager.Instance.WriteToLeaderboard(entry);
        }
        else
        {
            if (usedEntries.Count == 0)
            {
                if (entry.Name != "")
                    usedEntries.Add(entry);
                entry = new LeaderboardEntry(Random.Range(1, 1000).ToString(), Random.Range(1, 1000).ToString(), Random.Range(0, 1000));
                LeaderboardManager.Instance.WriteToLeaderboard(entry);
            }
            else
            {
                if (entry.Name != "")
                    usedEntries.Add(entry);

                entry = new LeaderboardEntry("new", "new", Random.Range(0, 1000));
                int toUse = Random.Range(0,usedEntries.Count-1);
                entry.ID = usedEntries[toUse].ID;
                entry.Name = usedEntries[toUse].Name;
                LeaderboardManager.Instance.WriteToLeaderboard(entry);
            }
        }
    }
}
