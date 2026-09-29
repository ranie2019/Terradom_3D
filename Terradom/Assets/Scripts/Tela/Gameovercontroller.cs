using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameOverController : MonoBehaviour
{
    [Header("Canvas Game Over")]
    [SerializeField] private GameObject painelGameOver;

    [Header("Botoes")]
    [SerializeField] private Button botaoSair;
    [SerializeField] private Button botaoMenuPrincipal;

    [Header("Configuracoes")]
    // FIX 1: Era "MenuPrincipal" — cena que não estava no build.
    // Trocado para "Inicio" que é o nome correto no EditorBuildSettings.
    [SerializeField] private string nomeSceneMenu = "Inicio";

    [SerializeField] private float intervaloVerificacao = 2f;
    [SerializeField] private int limiteRecursos = 100;

    private float proximaVerificacao;
    private bool gameOverAtivado = false;

    // FIX 2: Removida a dependência da layer "BaseSoldado" que estava duplicada
    // (índices 3 e 12), fazendo a base azul (layer 12) não ser encontrada.
    // Agora a busca usa tag "Azul" + componente SoldadoSpown — muito mais confiável.
    private const string TAG_JOGADOR = "Azul";

    private void Start()
    {
        if (painelGameOver != null)
            painelGameOver.SetActive(false);

        if (botaoSair != null)
            botaoSair.onClick.AddListener(Sair);

        if (botaoMenuPrincipal != null)
            botaoMenuPrincipal.onClick.AddListener(VoltarMenu);

        proximaVerificacao = Time.time + intervaloVerificacao;
    }

    private void Update()
    {
        if (gameOverAtivado) return;
        if (Time.time < proximaVerificacao) return;

        proximaVerificacao = Time.time + intervaloVerificacao;

        if (VerificarDerrota())
            AtivarGameOver();
    }

    // =========================================================
    // VERIFICAÇÃO DE DERROTA
    // =========================================================

    private bool VerificarDerrota()
    {
        if (!ExisteBaseSoldadoJogador())
        {
            Debug.Log("[GameOver] → Nenhuma Base Soldado do jogador em cena.");
            return true;
        }

        if (!ExisteColetorJogador())
        {
            Debug.Log("[GameOver] → Nenhum Coletor do jogador em cena.");
            return true;
        }

        if (RecursosInsuficientes())
        {
            Debug.Log("[GameOver] → Recursos abaixo do limite mínimo.");
            return true;
        }

        return false;
    }

    // FIX 2: Antes buscava por layer "BaseSoldado" (duplicada nos índices 3 e 12).
    // Agora busca por tag "Azul" + componente SoldadoSpown — funciona independente da layer.
    private bool ExisteBaseSoldadoJogador()
    {
        SoldadoSpown[] bases = FindObjectsByType<SoldadoSpown>(FindObjectsSortMode.None);
        foreach (SoldadoSpown base_ in bases)
        {
            if (base_ == null) continue;
            if (base_.CompareTag(TAG_JOGADOR))
                return true;
            // Sobe na hierarquia caso o SoldadoSpown esteja em filho
            if (base_.transform.root.CompareTag(TAG_JOGADOR))
                return true;
        }
        return false;
    }

    private bool ExisteColetorJogador()
    {
        Coletor[] coletores = FindObjectsByType<Coletor>(FindObjectsSortMode.None);
        foreach (Coletor coletor in coletores)
        {
            if (coletor == null) continue;
            if (coletor.CompareTag(TAG_JOGADOR))
                return true;
            if (coletor.transform.root.CompareTag(TAG_JOGADOR))
                return true;
        }
        return false;
    }

    private bool RecursosInsuficientes()
    {
        if (GameControllerRecursos.Instance == null)
        {
            Debug.LogWarning("[GameOver] → GameControllerRecursos.Instance é null!");
            return false;
        }

        int pedra   = GameControllerRecursos.Instance.pedra;
        int madeira = GameControllerRecursos.Instance.madeira;
        int metal   = GameControllerRecursos.Instance.metal;

        return pedra < limiteRecursos || madeira < limiteRecursos || metal < limiteRecursos;
    }

    // =========================================================
    // GAME OVER
    // =========================================================

    private void AtivarGameOver()
    {
        gameOverAtivado = true;
        Debug.Log("[GameOver] → GAME OVER!");
        Time.timeScale = 0f;

        if (painelGameOver != null)
            painelGameOver.SetActive(true);
        else
            Debug.LogError("[GameOver] → PainelGameOver não atribuído no Inspector!");
    }

    // =========================================================
    // BOTÕES
    // =========================================================

    private void VoltarMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeSceneMenu);
    }

    private void Sair()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}