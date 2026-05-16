using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unity主线程调度器
/// 用于从其他线程调度任务到Unity主线程执行
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<Action> _executionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            var go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 将操作加入队列，在主线程执行
    /// </summary>
    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// 将协程加入队列，在主线程执行
    /// </summary>
    public void Enqueue(IEnumerator coroutine)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(() => StartCoroutine(coroutine));
        }
    }

    private void Update()
    {
        // 在主线程的Update中执行队列中的所有操作
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                try
                {
                    _executionQueue.Dequeue()?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"主线程调度器执行失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }
}
