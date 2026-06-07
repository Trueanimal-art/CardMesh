using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultPanelController : MonoBehaviour
{
    [Header("Text blocks")]
    [SerializeField] private TMP_Text headerText;      // "ТВОЙ ОФИСНЫЙ ПСИХОТИП"
    [SerializeField] private TMP_Text titleText;       // "ОФИСНЫЙ ТРУП"
    [SerializeField] private TMP_Text descriptionText; // 2-3 строки
    [SerializeField] private TMP_Text sloganText;      // *слоган*

    [Header("Definitions")]
    [SerializeField] private List<ArchetypeDefinition> definitions = new List<ArchetypeDefinition>();

    [Header("Restart")]
    [SerializeField] private string sceneToReload = ""; // пусто = перезагрузить текущую сцену

    public void ShowResult(OfficeArchetype type)
    {
        if (headerText != null)
            headerText.text = "ТВОЙ ОФИСНЫЙ ПСИХОТИП";

        var def = FindDefinition(type);
        if (def == null)
        {
            if (titleText != null) titleText.text = type.ToString();
            if (descriptionText != null) descriptionText.text = "";
            if (sloganText != null) sloganText.text = "";
            return;
        }

        if (titleText != null) titleText.text = def.title;
        if (descriptionText != null) descriptionText.text = def.description;
        if (sloganText != null) sloganText.text = $"«{def.slogan}»";
    }

    private ArchetypeDefinition FindDefinition(OfficeArchetype type)
    {
        for (int i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] != null && definitions[i].type == type)
                return definitions[i];
        }
        return null;
    }

    // Повесь на кнопку "Пройти ещё раз"
    public void Restart()
    {
        if (!string.IsNullOrEmpty(sceneToReload))
        {
            SceneManager.LoadScene(sceneToReload);
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
