using UnityEngine;

[DisallowMultipleComponent]
public class GameControllerRecursosIA : MonoBehaviour
{
    public static GameControllerRecursosIA Instance;

    [Header("Recursos IA")]
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
        if (quantidade <= 0)
            return;

        if (tagDono != "Vermelho")
            return;

        if (tipoRecurso == "Pedra")
            pedra += quantidade;
        else if (tipoRecurso == "Arvore" || tipoRecurso == "Madeira")
            madeira += quantidade;
        else if (tipoRecurso == "Metal")
            metal += quantidade;
    }

    // =========================================================
    // BASE SOLDADO
    // =========================================================

    public bool PodeCriarBaseSoldado()
    {
        return TemRecursos(custoPedraBaseSoldado, custoMadeiraBaseSoldado, custoMetalBaseSoldado);
    }

    public bool TentarGastarRecursosDaBaseSoldado()
    {
        return TentarGastarRecursos(custoPedraBaseSoldado, custoMadeiraBaseSoldado, custoMetalBaseSoldado);
    }

    // =========================================================
    // BASE VEICULO (TANK)
    // =========================================================

    public bool PodeCriarBaseVeiculo()
    {
        return TemRecursos(custoPedraBaseVeiculo, custoMadeiraBaseVeiculo, custoMetalBaseVeiculo);
    }

    public bool TentarGastarRecursosDaBaseVeiculo()
    {
        return TentarGastarRecursos(custoPedraBaseVeiculo, custoMadeiraBaseVeiculo, custoMetalBaseVeiculo);
    }

    // =========================================================
    // BASE AVIAO
    // =========================================================

    public bool PodeCriarBaseAviao()
    {
        return TemRecursos(custoPedraBaseAviao, custoMadeiraBaseAviao, custoMetalBaseAviao);
    }

    public bool TentarGastarRecursosDaBaseAviao()
    {
        return TentarGastarRecursos(custoPedraBaseAviao, custoMadeiraBaseAviao, custoMetalBaseAviao);
    }

    // =========================================================
    // CORE
    // =========================================================

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

        return true;
    }

    // =========================================================
    // INDICE (IA)
    // =========================================================

    public bool PodeCriarBasePorIndice(int indiceBase)
    {
        if (indiceBase == 0) return PodeCriarBaseSoldado();
        if (indiceBase == 1) return PodeCriarBaseVeiculo();
        if (indiceBase == 2) return PodeCriarBaseAviao();
        return false;
    }

    public bool TentarGastarRecursosDaBasePorIndice(int indiceBase)
    {
        if (indiceBase == 0) return TentarGastarRecursosDaBaseSoldado();
        if (indiceBase == 1) return TentarGastarRecursosDaBaseVeiculo();
        if (indiceBase == 2) return TentarGastarRecursosDaBaseAviao();
        return false;
    }
}