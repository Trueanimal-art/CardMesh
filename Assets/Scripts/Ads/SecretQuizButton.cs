using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SecretQuizButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _statusLabel;
    [SerializeField] private GameObject _secretQuizUnlockedLabel;

    [Header("Scene/Logic")]
    [Tooltip("Имя сцены секретного квиза (должно быть добавлено в Build Settings -> Scenes In Build).")]
    [SerializeField] private string _sceneNameSecret = "Game Secret";

    private void Start()
    {
        if (_button == null) _button = GetComponent<Button>();

        RefreshUI();

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClickOpenSecretQuiz);
    }

    // Этот метод у тебя дергается снаружи (когда показан ResultPanel).
    // Оставляем: возвращаем кликабельность каждый раз.
    public void OnResultsShown()
    {
        _button.interactable = true;
        RefreshUI();
    }

    private void RefreshUI()
    {
        // В режиме "каждый раз реклама" лейбл "разблокировано" не нужен.
        if (_secretQuizUnlockedLabel != null)
            _secretQuizUnlockedLabel.SetActive(false);

        if (_statusLabel != null)
            _statusLabel.text = "Открыть скрытый психотип (за рекламу)";
    }

    private void OnClickOpenSecretQuiz()
    {
        _button.interactable = false;

        // Страховка: если сцена не в Build Settings — не делаем вид что “не работает реклама”
        if (!Application.CanStreamedLevelBeLoaded(_sceneNameSecret))
        {
            Debug.LogError($"[SecretQuizButton] Scene '{_sceneNameSecret}' can't be loaded. " +
                           $"Add it to Build Settings -> Scenes In Build and check exact name.");
            _button.interactable = true;
            return;
        }

        // Идем через AdsManager (единый вход)
        if (AdsManager.Instance == null)
        {
            Debug.LogWarning("[SecretQuizButton] AdsManager.Instance == null. Can't show rewarded.");
            _button.interactable = true;
            return;
        }

        AdsManager.Instance.TryShowRewarded(
            onSuccess: () =>
            {
                _button.interactable = true;
                UnityEngine.SceneManagement.SceneManager.LoadScene(_sceneNameSecret);
            },
            onFailOrClosed: () =>
            {
                // Если закрыли/не показалось — просто возвращаем кнопку, НЕ открываем секретную сцену
                _button.interactable = true;
            }
        );
    }
}
