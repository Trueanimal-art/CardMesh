using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class AnswerButton : MonoBehaviour
{
    private Button _button;
    private Image _image;
    private TMP_Text _label;

    private int _index;
    private Action<int> _onClicked;

    // Можно поменять цвета по вкусу
    private readonly Color _defaultColor = Color.white;
    private readonly Color _correctColor = Color.green;
    private readonly Color _wrongColor = Color.red;
    [SerializeField] private Color _selectedColor = new Color(0.3f, 0.8f, 0.3f, 1f);

    private void Awake()
    {
        _button = GetComponent<Button>();
        _image = GetComponent<Image>();
        _label = GetComponentInChildren<TMP_Text>(true);

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(HandleClick);

        SetStateDefault();
    }

    public void Setup(string answerText, int index, Action<int> onClicked)
    {
        _index = index;
        _onClicked = onClicked;

        if (_label != null)
            _label.text = answerText;

        SetStateDefault();
    }

    private void HandleClick()
    {
        _onClicked?.Invoke(_index);
    }

    public void SetStateDefault()
    {
        if (_image != null) _image.color = _defaultColor;
        if (_button != null) _button.interactable = true;
    }

    public void SetStateCorrect()
    {
        if (_image != null) _image.color = _correctColor;
        if (_button != null) _button.interactable = false;
    }

    public void SetStateWrong()
    {
        if (_image != null) _image.color = _wrongColor;
        if (_button != null) _button.interactable = false;
    }

    public void SetStateDisabled()
    {
        if (_button != null) _button.interactable = false;
    }
    public void SetStateSelected()
    {
        if (_image != null) _image.color = _selectedColor;
        if (_button != null) _button.interactable = false;
    }
}
