using System.Collections.Generic;
using UnityEngine;

public class TreeScheduler : MonoBehaviour
{
    public static float BudgetMilliseconds = 4f;

    private static TreeScheduler _instance;

    private readonly Queue<TreeGenerator> _queue = new Queue<TreeGenerator>();
    private readonly System.Diagnostics.Stopwatch _watch = new System.Diagnostics.Stopwatch();

    public static int Pending
    {
        get
        {
            if (_instance == null)
            {
                return 0;
            }

            return _instance._queue.Count;
        }
    }

    public static void Request(TreeGenerator tree)
    {
        if (_instance == null)
        {
            GameObject host = new GameObject("Tree Scheduler");
            host.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<TreeScheduler>();
        }

        _instance._queue.Enqueue(tree);
    }

    private void LateUpdate()
    {
        _watch.Restart();
        int processed = 0;

        while (_queue.Count > 0)
        {
            if (processed > 0 && _watch.Elapsed.TotalMilliseconds >= BudgetMilliseconds)
            {
                break;
            }

            TreeGenerator tree = _queue.Dequeue();
            if (tree == null)
            {
                continue;
            }

            tree.ScheduledRebuild();
            processed++;
        }

        _watch.Stop();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
