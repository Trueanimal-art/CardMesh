using SolitaireAssociations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundToggleButton : MonoBehaviour
{
    [Header("Collection Book")]
    [SerializeField] private CollectionBookPopup collectionBookPopup;
    [SerializeField] private CollectionBookManager collectionBookManager;
    [SerializeField] private TMP_Text progressBadgeText;

    private Button button;
    private Toggle legacyToggle;
    private UIJuicyButton juicyButton;
    private int lastOpenFrame = -1;

    private void Reset()
    {
        BindReferences();
        EnsureHoverFeedback();
    }

    private void Awake()
    {
        BindReferences();
        EnsureHoverFeedback();
        WireButton();
        RefreshBadge();
    }

    private void OnEnable()
    {
        BindReferences();
        EnsureHoverFeedback();
        WireButton();

        collectionBookManager ??= CollectionBookManager.GetOrCreate(this);
        collectionBookManager.StateChanged -= RefreshBadge;
        collectionBookManager.StateChanged += RefreshBadge;
        RefreshBadge();
    }

    private void OnDisable()
    {
        if (collectionBookManager != null)
        {
            collectionBookManager.StateChanged -= RefreshBadge;
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OpenCollectionBook);
        }
    }

    // Kept for the existing MainMenu scene persistent callback.
    public void ToggleSound()
    {
        OpenCollectionBook();
    }

    public void OpenCollectionBook()
    {
        if (lastOpenFrame == Time.frameCount)
        {
            return;
        }

        lastOpenFrame = Time.frameCount;
        collectionBookManager ??= CollectionBookManager.GetOrCreate(this);
        collectionBookPopup ??= CollectionBookPopup.SceneInstance;

        if (collectionBookPopup == null)
        {
            Debug.LogWarning("SoundToggleButton: CollectionBookPopup is missing from MainMenu scene.");
            return;
        }

        collectionBookPopup.Show(collectionBookManager);
    }

    private void BindReferences()
    {
        button = GetComponent<Button>();
        legacyToggle = GetComponentInChildren<Toggle>(true);
        juicyButton = GetComponent<UIJuicyButton>();
        collectionBookPopup ??= CollectionBookPopup.SceneInstance;
        collectionBookManager ??= FindFirstObjectByType<CollectionBookManager>(FindObjectsInactive.Include);

        if (progressBadgeText == null)
        {
            progressBadgeText = transform.Find("ProgressBadge/BadgeText")?.GetComponent<TMP_Text>();
        }
    }

    private void WireButton()
    {
        if (legacyToggle != null)
        {
            legacyToggle.onValueChanged.RemoveAllListeners();
            legacyToggle.interactable = false;
        }

        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(OpenCollectionBook);
        button.onClick.AddListener(OpenCollectionBook);
    }

    private void RefreshBadge()
    {
        if (progressBadgeText == null)
        {
            return;
        }

        collectionBookManager ??= CollectionBookManager.GetOrCreate(this);
        CollectionBookReward next = collectionBookManager.GetNextReward();

        if (next == null)
        {
            progressBadgeText.text = "MAX";
        }
        else
        {
            progressBadgeText.text = $"{collectionBookManager.CurrentPoints}/{next.RequiredPoints}";
        }

        RuntimeCyrillicFontProvider.Apply(progressBadgeText, progressBadgeText.text);
    }

    private void EnsureHoverFeedback()
    {
        if (juicyButton == null)
        {
            juicyButton = GetComponent<UIJuicyButton>();
        }

        if (juicyButton != null)
        {
            juicyButton.enabled = true;
        }
    }
}
