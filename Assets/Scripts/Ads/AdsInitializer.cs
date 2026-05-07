using UnityEngine;

public class AdsInitializer : MonoBehaviour
{
    [SerializeField] private bool logDisabledState;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (logDisabledState)
        {
            Debug.Log("AdsInitializer is disabled for the CardMesh MVP.");
        }
    }

    public void InitializeAds()
    {
        if (logDisabledState)
        {
            Debug.Log("AdsInitializer.InitializeAds skipped: Unity Ads is not used in the CardMesh MVP.");
        }
    }
}
