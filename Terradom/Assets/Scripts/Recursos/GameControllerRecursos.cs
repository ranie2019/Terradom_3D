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

    [Header("Custo Base Soldado")]
    [SerializeField] private int custoPedraBaseSoldado = 100;
    [SerializeField] private int custoMadeiraBaseSoldado = 100;
    [SerializeField] private int custoMetalBaseSoldado = 100;

    [Header("Custo Base Veiculo")]
    [SerializeField] private int custoPedraBaseVeiculo = 150;
    [SerializeField] private int custoMadeiraBaseVeiculo = 150;
    [SerializeField] private int custoMetalBaseVeiculo = 150;

    [Header("Custo Base Aviao")]
    [SerializeField] private int custoPedraBaseAviao = 200;
    [SerializeField] private int custoMadeiraBaseAviao = 200;
    [SerializeField] private int custoMetalBaseAviao = 200;

    [Header("UI Recursos")]
    [SerializeField] private TMP_Text textoPedra;
    [SerializeField] private TMP_Text textoMadeira;
    [SerializeField] private TMP_Text textoMetal;

    [Header("Botoes Base")]
    [SerializeField] private Button botaoBaseSoldado;
    [SerializeField] private Button botaoBaseVeiculo;
    [SerializeField] private Button botaoBaseAviao;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AtualizarUI();
    }

    private void OnValidate()
    {
        custoPedraBaseSoldado = Mathf.Max(0, custoPedraBaseSoldado);
        custoMadeiraBaseSoldado = Mathf.Max(0, custoMadeiraBaseSoldado);
        custoMetalBaseSoldado = Mathf.Max(0, custoMetalBaseSoldado);

        custoPedraBaseVeiculo = Mathf.Max(0, custoPedraBaseVeiculo);
        custoMadeiraBaseVeiculo = Mathf.Max(0, custoMadeiraBaseVeiculo);
        custoMetalBaseVeiculo = Mathf.Max(0, custoMetalBaseVeiculo);

        custoPedraBaseAviao = Mathf.Max(0, custoPedraBaseAviao);
        custoMadeiraBaseAviao = Mathf.Max(0, custoMadeiraBaseAviao);
        custoMetalBaseAviao = Mathf.Max(0, custoMetalBaseAviao);
    }

    public void AdicionarRecurso(string tagDono, string tipoRecurso, int quantidade)
    {
        if (quantidade <= 0)
            return;

        if (tagDono != "Azul")
            return;

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

    // =====================================================================
    // BASE SOLDADO
    // =====================================================================

    public bool PodeCriarBaseSoldado()
    {
        return TemRecursos(
            custoPedraBaseSoldado,
            custoMadeiraBaseSoldado,
            custoMetalBaseSoldado
        );
    }

    public bool TentarGastarRecursosDaBaseSoldado()
    {
        return TentarGastarRecursos(
            custoPedraBaseSoldado,
            custoMadeiraBaseSoldado,
            custoMetalBaseSoldado
        );
    }

    // =====================================================================
    // BASE VEICULO
    // =====================================================================

    public bool PodeCriarBaseVeiculo()
    {
        return TemRecursos(
            custoPedraBaseVeiculo,
            custoMadeiraBaseVeiculo,
            custoMetalBaseVeiculo
        );
    }

    public bool TentarGastarRecursosDaBaseVeiculo()
    {
        return TentarGastarRecursos(
            custoPedraBaseVeiculo,
            custoMadeiraBaseVeiculo,
            custoMetalBaseVeiculo
        );
    }

    // =====================================================================
    // BASE AVIAO
    // =====================================================================

    public bool PodeCriarBaseAviao()
    {
        return TemRecursos(
            custoPedraBaseAviao,
            custoMadeiraBaseAviao,
            custoMetalBaseAviao
        );
    }

    public bool TentarGastarRecursosDaBaseAviao()
    {
        return TentarGastarRecursos(
            custoPedraBaseAviao,
            custoMadeiraBaseAviao,
            custoMetalBaseAviao
        );
    }

    // =====================================================================
    // COMPATIBILIDADE COM CODIGOS ANTIGOS
    // =====================================================================

    public bool PodeCriarBase()
    {
        return PodeCriarBaseSoldado();
    }

    public bool TentarGastarRecursosDaBase()
    {
        return TentarGastarRecursosDaBaseSoldado();
    }

    // =====================================================================
    // BASE POR INDICE
    // 0 = Base Soldado
    // 1 = Base Veiculo
    // 2 = Base Aviao
    // =====================================================================

    public bool PodeCriarBasePorIndice(int indiceBase)
    {
        if (indiceBase == 0)
            return PodeCriarBaseSoldado();

        if (indiceBase == 1)
            return PodeCriarBaseVeiculo();

        if (indiceBase == 2)
            return PodeCriarBaseAviao();

        return false;
    }

    public bool TentarGastarRecursosDaBasePorIndice(int indiceBase)
    {
        if (indiceBase == 0)
            return TentarGastarRecursosDaBaseSoldado();

        if (indiceBase == 1)
            return TentarGastarRecursosDaBaseVeiculo();

        if (indiceBase == 2)
            return TentarGastarRecursosDaBaseAviao();

        return false;
    }

    public void AtualizarUI()
    {
        if (textoPedra != null)
            textoPedra.text = pedra.ToString();

        if (textoMadeira != null)
            textoMadeira.text = madeira.ToString();

        if (textoMetal != null)
            textoMetal.text = metal.ToString();

        AtualizarBotoesBase();
        AtualizarBotoesProducao();
    }

    private void AtualizarBotoesBase()
    {
        if (botaoBaseSoldado != null)
            botaoBaseSoldado.interactable = PodeCriarBaseSoldado();

        if (botaoBaseVeiculo != null)
            botaoBaseVeiculo.interactable = PodeCriarBaseVeiculo();

        if (botaoBaseAviao != null)
            botaoBaseAviao.interactable = PodeCriarBaseAviao();
    }

    private void AtualizarBotoesProducao()
    {
        BotoesProducaoUnidades.AtualizarTodos();
    }

    public int GetCustoPedraBaseSoldado()
    {
        return custoPedraBaseSoldado;
    }

    public int GetCustoMadeiraBaseSoldado()
    {
        return custoMadeiraBaseSoldado;
    }

    public int GetCustoMetalBaseSoldado()
    {
        return custoMetalBaseSoldado;
    }

    public int GetCustoPedraBaseVeiculo()
    {
        return custoPedraBaseVeiculo;
    }

    public int GetCustoMadeiraBaseVeiculo()
    {
        return custoMadeiraBaseVeiculo;
    }

    public int GetCustoMetalBaseVeiculo()
    {
        return custoMetalBaseVeiculo;
    }

    public int GetCustoPedraBaseAviao()
    {
        return custoPedraBaseAviao;
    }

    public int GetCustoMadeiraBaseAviao()
    {
        return custoMadeiraBaseAviao;
    }

    public int GetCustoMetalBaseAviao()
    {
        return custoMetalBaseAviao;
    }
}
