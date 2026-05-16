using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameOverController : MonoBehaviour
{
    [Header("Canvas Game Over")]
    [SerializeField] private GameObject painelGameOver;

    [Header("Botões")]
    [SerializeField] private Button botaoSair;
    [SerializeField] private Button botaoMenuPrincipal;

    [Header("Configurações")]
    [SerializeField] private string nomeSceneMenu = "MenuPrincipal";
    [SerializeField] private float intervaloVerificacao = 2f;
    [SerializeField] private int limiteRecursos = 100;

    private float proximaVerificacao;
    private bool gameOverAtivado = false;

    // Layers e Tags
    private const string LAYER_BASE_SOLDADO = "BaseSoldado";
    private const string LAYER_COLETOR      = "Coletor";
    private const string TAG_COLETOR        = "Azul";

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
        // Condição 1: sem Base Soldado do jogador em cena
        if (!ExisteBaseSoldadoJogador())
        {
            Debug.Log("[GameOver] ❌ Nenhuma Base Soldado do jogador em cena.");
            return true;
        }

        // Condição 2: sem Coletor do jogador em cena
        if (!ExisteColetorJogador())
        {
            Debug.Log("[GameOver] ❌ Nenhum Coletor do jogador em cena.");
            return true;
        }

        // Condição 3: qualquer recurso abaixo do limite
        if (RecursosInsuficientes())
        {
            Debug.Log("[GameOver] ❌ Recursos abaixo do limite mínimo.");
            return true;
        }

        return false;
    }

    private bool ExisteBaseSoldadoJogador()
    {
        int layer = LayerMask.NameToLayer(LAYER_BASE_SOLDADO);
        if (layer == -1)
        {
            Debug.LogWarning("[GameOver] ⚠️ Layer 'BaseSoldado' não encontrada!");
            return true; // não penaliza se layer não existe
        }

        GameObject[] todos = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in todos)
        {
            if (obj == null) continue;
            // Layer BaseSoldado + Tag Azul = base do jogador
            if (obj.layer == layer && obj.CompareTag("Azul"))
                return true;
        }
        return false;
    }

    private bool ExisteColetorJogador()
    {
        int layer = LayerMask.NameToLayer(LAYER_COLETOR);
        if (layer == -1)
        {
            Debug.LogWarning("[GameOver] ⚠️ Layer 'Coletor' não encontrada!");
            return true;
        }

        GameObject[] todos = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in todos)
        {
            if (obj == null) continue;
            // Layer Coletor + Tag Azul = coletor do jogador
            if (obj.layer == layer && obj.CompareTag(TAG_COLETOR))
                return true;
        }
        return false;
    }

    private bool RecursosInsuficientes()
    {
        if (GameControllerRecursos.Instance == null)
        {
            Debug.LogWarning("[GameOver] ⚠️ GameControllerRecursos.Instance é null!");
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
        Debug.Log("[GameOver] 💀 GAME OVER!");

        Time.timeScale = 0f; // pausa o jogo

        if (painelGameOver != null)
            painelGameOver.SetActive(true);
        else
            Debug.LogError("[GameOver] ❌ PainelGameOver não atribuído no Inspector!");
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