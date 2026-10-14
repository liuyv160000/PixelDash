using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneMgr : MonoBehaviour
{
    private static SceneMgr instance;
    public static SceneMgr Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[SceneMgr]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<SceneMgr>();
            }
            return instance;
        }
    }

    // 进度广播，Loading 场景 UI 可订阅
    public static event Action<float> OnProgress;

    [Tooltip("Loading 场景最短显示时长（秒）。防止加载太快时进度条一闪而过，留出推进动画的时间。")]
    [SerializeField] private float minLoadingTime = 0.5f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>直接异步切换场景（带判空，场景未加入 Build Settings 时会报错而非静默失败）。</summary>
    public void LoadScene(string sceneName, Action onComplete = null)
    {
        StartCoroutine(LoadDirectRoutine(sceneName, onComplete));
    }

    /// <summary>带 Loading 场景的异步加载。</summary>
    public void LoadSceneWithLoading(string targetScene, string loadingScene,
        Action<float> onProgress = null, Action onComplete = null)
    {
        StartCoroutine(LoadRoutine(targetScene, loadingScene, onProgress, onComplete));
    }

    private IEnumerator LoadDirectRoutine(string sceneName, Action onComplete)
    {
        AsyncOperation ao = SceneManager.LoadSceneAsync(sceneName);
        if (ao == null)
        {
            Debug.LogError($"[SceneMgr] 场景 '{sceneName}' 无法加载，请确认已加入 Build Settings（File → Build Settings）。");
            onComplete?.Invoke();
            yield break;
        }
        while (!ao.isDone)
            yield return null;
        onComplete?.Invoke();
    }

    private IEnumerator LoadRoutine(string targetScene, string loadingScene,
        Action<float> onProgress, Action onComplete)
    {
        // 1. 先同步切到 Loading 场景。
        //    SceneMgr 挂在 DontDestroyOnLoad 物体上，场景卸载不会中断这个协程。
        float loadingStartTime = Time.unscaledTime;
        SceneManager.LoadScene(loadingScene, LoadSceneMode.Single);
        yield return null;   // 等一帧，让 Loading 场景完成初始化

        // 2. 再在 Loading 场景里后台异步加载目标场景，暂不激活。
        AsyncOperation targetAo = SceneManager.LoadSceneAsync(targetScene);
        if (targetAo == null)
        {
            Debug.LogError($"[SceneMgr] 场景 '{targetScene}' 无法加载，请确认已加入 Build Settings（File → Build Settings）。");
            onComplete?.Invoke();
            yield break;
        }
        targetAo.allowSceneActivation = false;

        // 3. 轮询进度（allowSceneActivation=false 时 progress 最高停在 0.9）
        while (targetAo.progress < 0.9f)
        {
            float p = Mathf.Clamp01(targetAo.progress / 0.9f);
            onProgress?.Invoke(p);
            OnProgress?.Invoke(p);
            yield return null;
        }
        onProgress?.Invoke(1f);
        OnProgress?.Invoke(1f);

        // 4. 保证 Loading 场景至少显示 minLoadingTime 秒
        //    （给进度条的平滑推进留出时间，否则加载太快会一闪而过）
        while (Time.unscaledTime - loadingStartTime < minLoadingTime)
            yield return null;

        // 5. 允许激活，切到目标场景
        targetAo.allowSceneActivation = true;
        while (!targetAo.isDone)
            yield return null;

        onComplete?.Invoke();
    }
}
