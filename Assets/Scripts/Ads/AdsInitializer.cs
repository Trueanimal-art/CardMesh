using UnityEngine;

public class AdsInitializer : MonoBehaviour
{
    [SerializeField] private bool debugLogs = true;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        EnsureAdsManager();
    }

    public void InitializeAds()
    {
        EnsureAdsManager();

        if (debugLogs)
        {
            Debug.Log("[ADS] AdsInitializer completed");
        }
    }

    private static void EnsureAdsManager()
    {
        if (AdsManager.Instance != null || FindFirstObjectByType<AdsManager>() != null)
        {
            return;
        }

        new GameObject("AdsManager").AddComponent<AdsManager>();
    }
}
