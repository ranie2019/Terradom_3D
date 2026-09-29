using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach no botão PLAY do modo Robo (vs IA).
/// Carrega a cena de jogo contra a IA diretamente, sem passar pelo Photon.
/// </summary>
[DisallowMultipleComponent]
public class BotaoRobo : MonoBehaviour
{
    [Header("Cena")]
    [SerializeField] private string nomeCena = "Cena IA";

    [Header("Botão")]
    [SerializeField] private Button botao;

    private void Awake()
    {
        if (botao == null)
            botao = GetComponent<Button>();

        if (botao != null)
            botao.onClick.AddListener(IrParaCenaIA);
    }

    public void IrParaCenaIA()
    {
        Debug.Log($"[BotaoRobo] Carregando cena '{nomeCena}'...");
        SceneManager.LoadScene(nomeCena);
    }
}