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

    [Header("Custo Torre Terra")]  // <- novo
    [SerializeField] private int custoPedraTorreTerra = 120;
    [SerializeField] private int custoMadeiraTorreTerra = 120;
    [SerializeField] private int custoMetalTorreTerra = 120;

    [Header("Custo Torre Ar")]
    [SerializeField] private int custoPedraTorreAr  = 200;
    [SerializeField] private int custoMadeiraTorreAr = 200;
    [SerializeField] private int custoMetalTorreAr   = 200;

    [Header("UI Recursos")]
    [SerializeField] private TMP_Text textoPedra;
    [SerializeField] private TMP_Text textoMadeira;
    [SerializeField] private TMP_Text textoMetal;

    [Header("Botoes Base")]
    [SerializeField] private Button botaoBaseSoldado;
    [SerializeField] private Button botaoBaseVeiculo;
    [SerializeField] private Button botaoBaseAviao;
    [SerializeField] private Button botaoTorreTerra;  // <- novo

    [SerializeField] private Button botaoTorreAr;

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
        custoPedraBaseSoldado   = Mathf.Max(0, custoPedraBaseSoldado);
        custoMadeiraBaseSoldado = Mathf.Max(0, custoMadeiraBaseSoldado);
        custoMetalBaseSoldado   = Mathf.Max(0, custoMetalBaseSoldado);

        custoPedraBaseVeiculo   = Mathf.Max(0, custoPedraBaseVeiculo);
        custoMadeiraBaseVeiculo = Mathf.Max(0, custoMadeiraBaseVeiculo);
        custoMetalBaseVeiculo   = Mathf.Max(0, custoMetalBaseVeiculo);

        custoPedraBaseAviao   = Mathf.Max(0, custoPedraBaseAviao);
        custoMadeiraBaseAviao = Mathf.Max(0, custoMadeiraBaseAviao);
        custoMetalBaseAviao   = Mathf.Max(0, custoMetalBaseAviao);

        custoPedraTorreTerra   = Mathf.Max(0, custoPedraTorreTerra);
        custoMadeiraTorreTerra = Mathf.Max(0, custoMadeiraTorreTerra);
        custoMetalTorreTerra   = Mathf.Max(0, custoMetalTorreTerra);

        custoPedraTorreAr  = Mathf.Max(0, custoPedraTorreAr);
        custoMadeiraTorreAr = Mathf.Max(0, custoMadeiraTorreAr);
        custoMetalTorreAr   = Mathf.Max(0, custoMetalTorreAr);
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

        pedra   -= custoPedra;
        madeira -= custoMadeira;
        metal   -= custoMetal;

        AtualizarUI();
        return true;
    }

    // =====================================================================
    // BASE SOLDADO
    // =====================================================================

    public bool PodeCriarBaseSoldado()
    {
        return TemRecursos(custoPedraBaseSoldado, custoMadeiraBaseSoldado, custoMetalBaseSoldado);
    }

    public bool TentarGastarRecursosDaBaseSoldado()
    {
        return TentarGastarRecursos(custoPedraBaseSoldado, custoMadeiraBaseSoldado, custoMetalBaseSoldado);
    }

    // =====================================================================
    // BASE VEICULO
    // =====================================================================

    public bool PodeCriarBaseVeiculo()
    {
        return TemRecursos(custoPedraBaseVeiculo, custoMadeiraBaseVeiculo, custoMetalBaseVeiculo);
    }

    public bool TentarGastarRecursosDaBaseVeiculo()
    {
        return TentarGastarRecursos(custoPedraBaseVeiculo, custoMadeiraBaseVeiculo, custoMetalBaseVeiculo);
    }

    // =====================================================================
    // BASE AVIAO
    // =====================================================================

    public bool PodeCriarBaseAviao()
    {
        return TemRecursos(custoPedraBaseAviao, custoMadeiraBaseAviao, custoMetalBaseAviao);
    }

    public bool TentarGastarRecursosDaBaseAviao()
    {
        return TentarGastarRecursos(custoPedraBaseAviao, custoMadeiraBaseAviao, custoMetalBaseAviao);
    }

    // =====================================================================
    // TORRE TERRA  <- novo
    // =====================================================================

    public bool PodeCriarTorreTerra()
    {
        return TemRecursos(custoPedraTorreTerra, custoMadeiraTorreTerra, custoMetalTorreTerra);
    }

    public bool TentarGastarRecursosDaTorreTerra()
    {
        return TentarGastarRecursos(custoPedraTorreTerra, custoMadeiraTorreTerra, custoMetalTorreTerra);
    }

    public int GetCustoPedraTorreTerra()   => custoPedraTorreTerra;
    public int GetCustoMadeiraTorreTerra() => custoMadeiraTorreTerra;
    public int GetCustoMetalTorreTerra()   => custoMetalTorreTerra;

    // =====================================================================
    // TORRE AR
    // =====================================================================

    public bool PodeCriarTorreAr()
    {
        return TemRecursos(custoPedraTorreAr, custoMadeiraTorreAr, custoMetalTorreAr);
    }

    public bool TentarGastarRecursosDaTorreAr()
    {
        return TentarGastarRecursos(custoPedraTorreAr, custoMadeiraTorreAr, custoMetalTorreAr);
    }

    public int GetCustoPedraTorreAr()   => custoPedraTorreAr;
    public int GetCustoMadeiraTorreAr() => custoMadeiraTorreAr;
    public int GetCustoMetalTorreAr()   => custoMetalTorreAr;

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
    // 3 = Torre Terra
    // 4 = Torre Ar
    // =====================================================================

    public bool PodeCriarBasePorIndice(int indiceBase)
    {
        if (indiceBase == 0) return PodeCriarBaseSoldado();
        if (indiceBase == 1) return PodeCriarBaseVeiculo();
        if (indiceBase == 2) return PodeCriarBaseAviao();
        if (indiceBase == 3) return PodeCriarTorreTerra();
        if (indiceBase == 4) return PodeCriarTorreAr();
        return false;
    }

    public bool TentarGastarRecursosDaBasePorIndice(int indiceBase)
    {
        if (indiceBase == 0) return TentarGastarRecursosDaBaseSoldado();
        if (indiceBase == 1) return TentarGastarRecursosDaBaseVeiculo();
        if (indiceBase == 2) return TentarGastarRecursosDaBaseAviao();
        if (indiceBase == 3) return TentarGastarRecursosDaTorreTerra();
        if (indiceBase == 4) return TentarGastarRecursosDaTorreAr();
        return false;
    }

    public void AtualizarUI()
    {
        if (textoPedra != null)   textoPedra.text   = pedra.ToString();
        if (textoMadeira != null) textoMadeira.text = madeira.ToString();
        if (textoMetal != null)   textoMetal.text   = metal.ToString();

        AtualizarBotoesBase();
        AtualizarBotoesProducao();
    }

    private void AtualizarBotoesBase()
    {
        if (botaoBaseSoldado != null) botaoBaseSoldado.interactable = PodeCriarBaseSoldado();
        if (botaoBaseVeiculo != null) botaoBaseVeiculo.interactable = PodeCriarBaseVeiculo();
        if (botaoBaseAviao   != null) botaoBaseAviao.interactable   = PodeCriarBaseAviao();
        if (botaoTorreTerra  != null) botaoTorreTerra.interactable  = PodeCriarTorreTerra();
        if (botaoTorreAr     != null) botaoTorreAr.interactable     = PodeCriarTorreAr();
    }

    private void AtualizarBotoesProducao()
    {
        BotoesProducaoUnidades.AtualizarTodos();
    }

    public int GetCustoPedraBaseSoldado()   => custoPedraBaseSoldado;
    public int GetCustoMadeiraBaseSoldado() => custoMadeiraBaseSoldado;
    public int GetCustoMetalBaseSoldado()   => custoMetalBaseSoldado;

    public int GetCustoPedraBaseVeiculo()   => custoPedraBaseVeiculo;
    public int GetCustoMadeiraBaseVeiculo() => custoMadeiraBaseVeiculo;
    public int GetCustoMetalBaseVeiculo()   => custoMetalBaseVeiculo;

    public int GetCustoPedraBaseAviao()   => custoPedraBaseAviao;
    public int GetCustoMadeiraBaseAviao() => custoMadeiraBaseAviao;
    public int GetCustoMetalBaseAviao()   => custoMetalBaseAviao;
}