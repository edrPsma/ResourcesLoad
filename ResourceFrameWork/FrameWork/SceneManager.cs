using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using EG.Resource.Core;

namespace EG.Resource
{
    public class SceneManager : Singleton<SceneManager>
    {
        public string CurScene { get; private set; }

        //切换场景进度条
        public int LoadingProgress { get; private set; } = 0;
        //场景是否加载完
        public bool IsDone { get; set; } = true;
        //加载场景完成回调
        public Action LoadSceneOverCallBack { get; set; }
        //加载场景开始回调
        public Action LoadSceneEnterCallBack { get; set; }

        public void LoadScene(string name)
        {
            IsDone = false;
            LoadingProgress = 0;
            EmptyMono.Instance.StartCoroutine(LoadSceneAsync(name));
        }

        IEnumerator LoadSceneAsync(string name)
        {
            ClearCache();
            LoadSceneEnterCallBack?.Invoke();
            AsyncOperation unloadScene = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Loading", LoadSceneMode.Single);
            while (unloadScene != null && !unloadScene.isDone)
            {
                yield return new WaitForEndOfFrame();
            }
            LoadingProgress = 0;
            int targetProgress = 0;
            AsyncOperation asyncScene = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(name);
            if (asyncScene != null && !asyncScene.isDone)
            {
                asyncScene.allowSceneActivation = false;
                while (asyncScene.progress < 0.9f)
                {
                    targetProgress = (int)(asyncScene.progress * 100);
                    yield return new WaitForEndOfFrame();

                    //平滑过渡
                    while (LoadingProgress < targetProgress)
                    {
                        ++LoadingProgress;
                        yield return new WaitForEndOfFrame();
                    }
                }
                CurScene = name;
                //自行加载剩余10%
                targetProgress = 100;
                while (LoadingProgress < targetProgress - 2)
                {
                    ++LoadingProgress;
                    yield return new WaitForEndOfFrame();
                }
                LoadingProgress = 100;
                asyncScene.allowSceneActivation = true;
                IsDone = true;
                LoadSceneOverCallBack?.Invoke();
            }
        }

        // 跳场景清空缓存
        void ClearCache()
        {
            ObjectPoolManager.Instance.ClearCache();
            ResourcesManager.Instance.ClearCache();
        }
    }
}
