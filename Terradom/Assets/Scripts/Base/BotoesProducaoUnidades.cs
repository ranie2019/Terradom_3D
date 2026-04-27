using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BotoesProducaoUnidades : MonoBehaviour
{
    private static BotoesProducaoUnidades instance;

    [Header("Botões")]
    [SerializeField] private Button botaoGuerreiro;
    [SerializeField] private Button botaoRecurso;
    [SerializeField] private Button botaoSoldado;

    [Header("Objetos visuais dos botões")]
    [SerializeField] private GameObject objetoBotaoGuerreiro;
    [SerializeField] private GameObject objetoBotaoRecurso;
    [SerializeField] private GameObject objetoBotaoSoldado;

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

    public void BotaoCriarSoldado()
    {
        SoldadoSpown spown = GetSpawnSelecionado();

        if (spown == null)
            return;

        spown.CriarSoldado();
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

        if (botaoSoldado != null)
            botaoSoldado.interactable = spown.PodeCriarSoldado();
    }

    private void MostrarBotoes()
    {
        if (objetoBotaoGuerreiro != null)
            objetoBotaoGuerreiro.SetActive(true);

        if (objetoBotaoRecurso != null)
            objetoBotaoRecurso.SetActive(true);

        if (objetoBotaoSoldado != null)
            objetoBotaoSoldado.SetActive(true);
    }

    private void EsconderBotoes()
    {
        if (objetoBotaoGuerreiro != null)
            objetoBotaoGuerreiro.SetActive(false);

        if (objetoBotaoRecurso != null)
            objetoBotaoRecurso.SetActive(false);

        if (objetoBotaoSoldado != null)
            objetoBotaoSoldado.SetActive(false);

        if (botaoGuerreiro != null)
            botaoGuerreiro.interactable = false;

        if (botaoRecurso != null)
            botaoRecurso.interactable = false;

        if (botaoSoldado != null)
            botaoSoldado.interactable = false;
    }

    private SoldadoSpown GetSpawnSelecionado()
    {
        BaseSelecionavel baseSelecionada = BaseSelecionavel.BaseAtualSelecionada;

        if (baseSelecionada == null)
            return null;

        return baseSelecionada.GetSoldadoSpown();
    }
}