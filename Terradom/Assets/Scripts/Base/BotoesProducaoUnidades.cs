using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BotoesProducaoUnidades : MonoBehaviour
{
    private static BotoesProducaoUnidades instance;

    [Header("Botões")]
    [SerializeField] private Button botaoGuerreiro;
    [SerializeField] private Button botaoRecurso;

    [Header("Objetos visuais dos botões")]
    [SerializeField] private GameObject objetoBotaoGuerreiro;
    [SerializeField] private GameObject objetoBotaoRecurso;

    private void Awake()
    {
        instance = this;
        EsconderBotoes();
    }

    private void Update()
    {
        AtualizarEstado();
    }

    public static void AtualizarTodos()
    {
        if (instance != null)
            instance.AtualizarEstado();
    }

    public void BotaoCriarGuerreiro()
    {
        SoldadoSpown spown = GetSpawnSelecionado();

        if (spown == null)
            return;

        spown.CriarGuerreiro();
        AtualizarEstado();
    }

    public void BotaoCriarRecurso()
    {
        SoldadoSpown spown = GetSpawnSelecionado();

        if (spown == null)
            return;

        spown.CriarRecurso();
        AtualizarEstado();
    }

    private void AtualizarEstado()
    {
        SoldadoSpown spown = GetSpawnSelecionado();

        if (spown == null)
        {
            EsconderBotoes();
            return;
        }

        MostrarBotoes();

        if (botaoGuerreiro != null)
            botaoGuerreiro.interactable = spown.PodeCriarGuerreiro();

        if (botaoRecurso != null)
            botaoRecurso.interactable = spown.PodeCriarRecurso();
    }

    private void MostrarBotoes()
    {
        if (objetoBotaoGuerreiro != null)
            objetoBotaoGuerreiro.SetActive(true);

        if (objetoBotaoRecurso != null)
            objetoBotaoRecurso.SetActive(true);
    }

    private void EsconderBotoes()
    {
        if (objetoBotaoGuerreiro != null)
            objetoBotaoGuerreiro.SetActive(false);

        if (objetoBotaoRecurso != null)
            objetoBotaoRecurso.SetActive(false);

        if (botaoGuerreiro != null)
            botaoGuerreiro.interactable = false;

        if (botaoRecurso != null)
            botaoRecurso.interactable = false;
    }

    private SoldadoSpown GetSpawnSelecionado()
    {
        BaseSelecionavel baseSelecionada = BaseSelecionavel.BaseAtualSelecionada;

        if (baseSelecionada == null)
            return null;

        return baseSelecionada.GetSoldadoSpown();
    }
}