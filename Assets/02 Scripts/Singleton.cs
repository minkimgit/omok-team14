using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<T>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name;
                    _instance = obj.AddComponent<T>();
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        // 이미 인스턴스가 할당되어 있는데, 그게 내가 아니라면?
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[Singleton] {typeof(T).Name}의 중복 객체를 제거합니다. (ID: {gameObject.GetInstanceID()})");
            Destroy(gameObject);
            return;
        }

        // 내가 첫 번째 인스턴스라면 할당
        if (_instance == null)
        {
            _instance = this as T;
        }

        // 씬 전환 시 파괴 방지 (최상위 오브젝트여야 함)
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        
        // 이벤트 중복 등록 방지
        SceneManager.sceneLoaded -= OnSceneLoad;
        SceneManager.sceneLoaded += OnSceneLoad;
    }
    
    protected abstract void OnSceneLoad(Scene scene, LoadSceneMode mode);

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoad;
        }
    }
}