using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameControllerRecursos : MonoBehaviour
{
    public static GameControllerRecursos Instance;

    [Header("Recursos")]
    public int pedra = 0;
    public int madeira = 0;
    public int metal = 0;

    [Header("Custo da Base")]
    [SerializeField] private int custoPedraBase = 100;
    [SerializeField] private int custoMadeiraBase = 100;
    [SerializeField] private int custoMetalBase = 100;

    [Header("UI")]
    [SerializeField] private TMP_Text textoPedra;
    [SerializeField] private TMP_Text textoMadeira;
    [SerializeField] private TMP_Text textoMetal;

    [Header("Botão Base")]
    [SerializeField] private Button botaoBase;

    private void Awake()
    {
        Instance = this;
        AtualizarUI();
    }

    public void AdicionarRecurso(string tagDono, string tipoRecurso, int quantidade)
    {
        if (quantidade <= 0) return;
        if (tagDono != "Azul") return;

        if (tipoRecurso == "Pedra")
            pedra += quantidade;
        else if (tipoRecurso == "Arvore" || tipoRecurso == "Madeira")
            madeira += quantidade;
        else if (tipoRecurso == "Metal")
            metal += quantidade;

        AtualizarUI();
    }

    public bool TemRecursos(int custoPedra, int custoMadeira, int custoMetal)
    {
        return pedra >= custoPedra &&
               madeira >= custoMadeira &&
               metal >= custoMetal;
    }

    public bool TentarGastarRecursos(int custoPedra, int custoMadeira, int custoMetal)
    {
        if (!TemRecursos(custoPedra, custoMadeira, custoMetal))
            return false;

        pedra -= custoPedra;
        madeira -= custoMadeira;
        metal -= custoMetal;

        AtualizarUI();
        return true;
    }

    public bool PodeCriarBase()
    {
        return TemRecursos(custoPedraBase, custoMadeiraBase, custoMetalBase);
    }

    public bool TentarGastarRecursosDaBase()
    {
        return TentarGastarRecursos(custoPedraBase, custoMadeiraBase, custoMetalBase);
    }

    public void AtualizarUI()
    {
        if (textoPedra != null) textoPedra.text = pedra.ToString();
        if (textoMadeira != null) textoMadeira.text = madeira.ToString();
        if (textoMetal != null) textoMetal.text = metal.ToString();

        if (botaoBase != null)
            botaoBase.interactable = PodeCriarBase();

        BotoesProducaoUnidades.AtualizarTodos();
    }
}