using UnityEngine;

[DisallowMultipleComponent]
public class GameControllerRecursosIA : MonoBehaviour
{
    public static GameControllerRecursosIA Instance;

    [Header("Recursos IA")]
    public int pedra = 1000;
    public int madeira = 1000;
    public int metal = 1000;

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

    [Header("Custo Torre Terra")]
    [SerializeField] private int custoPedraTorreTerra = 120;
    [SerializeField] private int custoMadeiraTorreTerra = 120;
    [SerializeField] private int custoMetalTorreTerra = 120;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // =========================================================
    // RECURSOS
    // =========================================================

    public void AdicionarRecurso(string tagDono, string tipoRecurso, int quantidade)
    {
        if (quantidade <= 0) return;
        if (tagDono != "Vermelho") return;

        if (tipoRecurso == "Pedra") pedra += quantidade;
        else if (tipoRecurso == "Arvore" || tipoRecurso == "Madeira") madeira += quantidade;
        else if (tipoRecurso == "Metal") metal += quantidade;
    }

    public bool TemRecursos(int custoPedra, int custoMadeira, int custoMetal)
    {
        return pedra >= custoPedra && madeira >= custoMadeira && metal >= custoMetal;
    }

    public bool TentarGastarRecursos(int custoPedra, int custoMadeira, int custoMetal)
    {
        if (!TemRecursos(custoPedra, custoMadeira, custoMetal)) return false;
        pedra -= custoPedra;
        madeira -= custoMadeira;
        metal -= custoMetal;
        return true;
    }

    // =========================================================
    // BASE SOLDADO
    // =========================================================

    public bool PodeCriarBaseSoldado() => TemRecursos(custoPedraBaseSoldado, custoMadeiraBaseSoldado, custoMetalBaseSoldado);
    public bool TentarGastarRecursosDaBaseSoldado() => TentarGastarRecursos(custoPedraBaseSoldado, custoMadeiraBaseSoldado, custoMetalBaseSoldado);

    // =========================================================
    // BASE VEICULO
    // =========================================================

    public bool PodeCriarBaseVeiculo() => TemRecursos(custoPedraBaseVeiculo, custoMadeiraBaseVeiculo, custoMetalBaseVeiculo);
    public bool TentarGastarRecursosDaBaseVeiculo() => TentarGastarRecursos(custoPedraBaseVeiculo, custoMadeiraBaseVeiculo, custoMetalBaseVeiculo);

    // =========================================================
    // BASE AVIAO
    // =========================================================

    public bool PodeCriarBaseAviao() => TemRecursos(custoPedraBaseAviao, custoMadeiraBaseAviao, custoMetalBaseAviao);
    public bool TentarGastarRecursosDaBaseAviao() => TentarGastarRecursos(custoPedraBaseAviao, custoMadeiraBaseAviao, custoMetalBaseAviao);

    // =========================================================
    // TORRE TERRA
    // =========================================================

    public bool PodeCriarTorreTerra() => TemRecursos(custoPedraTorreTerra, custoMadeiraTorreTerra, custoMetalTorreTerra);
    public bool TentarGastarRecursosDaTorreTerra() => TentarGastarRecursos(custoPedraTorreTerra, custoMadeiraTorreTerra, custoMetalTorreTerra);

    public int GetCustoPedraTorreTerra()   => custoPedraTorreTerra;
    public int GetCustoMadeiraTorreTerra() => custoMadeiraTorreTerra;
    public int GetCustoMetalTorreTerra()   => custoMetalTorreTerra;

    // =========================================================
    // INDICE
    // 0 = Soldado | 1 = Veiculo | 2 = Aviao | 3 = Torre Terra
    // =========================================================

    public bool PodeCriarBasePorIndice(int indice)
    {
        if (indice == 0) return PodeCriarBaseSoldado();
        if (indice == 1) return PodeCriarBaseVeiculo();
        if (indice == 2) return PodeCriarBaseAviao();
        if (indice == 3) return PodeCriarTorreTerra();
        return false;
    }

    public bool TentarGastarRecursosDaBasePorIndice(int indice)
    {
        if (indice == 0) return TentarGastarRecursosDaBaseSoldado();
        if (indice == 1) return TentarGastarRecursosDaBaseVeiculo();
        if (indice == 2) return TentarGastarRecursosDaBaseAviao();
        if (indice == 3) return TentarGastarRecursosDaTorreTerra();
        return false;
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
    }
}