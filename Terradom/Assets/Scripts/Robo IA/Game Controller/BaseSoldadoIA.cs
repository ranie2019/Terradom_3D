using UnityEngine;

[DisallowMultipleComponent]
public class BaseSoldadoIA : MonoBehaviour
{
    private BaseAreaIA baseArea;
    private SoldadoSpownIA soldadoSpawn;

    [Header("Tempo entre bases")]
    [SerializeField] private float tempoBaseSoldado = 120f;

    [Header("Limite de bases")]
    [SerializeField] private int maxBaseSoldado = 10;

    private int quantidadeBaseSoldado;
    private bool setupSoldadoFeito = false;

    private void Awake()
    {
        AtualizarReferencias();
    }

    private void Update()
    {
        GarantirReferencias();

        // =====================================================
        // SETUP OBRIGATÓRIO
        // =====================================================
        if (!setupSoldadoFeito)
        {
            if (CriarBaseSoldado())
            {
                quantidadeBaseSoldado++;
                tempoBaseSoldado = 120f; // Reinicia o tempo
                setupSoldadoFeito = true;
            }
            return;
        }

        // =====================================================
        // TIMER SOLDADO (CONTAGEM REGRESSIVA NO PRÓPRIO TEMPO)
        // =====================================================
        if (tempoBaseSoldado > 0f)
        {
            tempoBaseSoldado -= Time.deltaTime;
            
            if (tempoBaseSoldado <= 0f)
            {
                tempoBaseSoldado = 0f; // Trava no 0
                
                if (quantidadeBaseSoldado < maxBaseSoldado)
                {
                    if (CriarBaseSoldado())
                    {
                        quantidadeBaseSoldado++;
                    }
                }
                
                tempoBaseSoldado = 120f; // Reinicia o tempo
            }
        }
    }

    // =========================================================
    // REFERÊNCIAS
    // =========================================================

    public void AtualizarReferencias()
    {
        if (baseArea == null)
            baseArea = FindObjectOfType<BaseAreaIA>();

        if (soldadoSpawn == null)
            soldadoSpawn = FindObjectOfType<SoldadoSpownIA>();
    }

    private void GarantirReferencias()
    {
        if (baseArea == null || soldadoSpawn == null)
            AtualizarReferencias();
    }

    // =========================================================
    // BASE SOLDADO
    // =========================================================

    public bool CriarBaseSoldado()
    {
        if (baseArea == null)
            return false;

        return baseArea.TentarCriarBasePorIndice(0);
    }

    public bool BaseSoldadoExiste()
    {
        return baseArea != null && baseArea.ExisteBasePorIndice(0);
    }

    // =========================================================
    // COLETOR
    // =========================================================
    public bool PodeCriarColetor()
    {
        return soldadoSpawn != null && soldadoSpawn.PodeCriarColetor();
    }

    public bool CriarColetor()
    {
        return soldadoSpawn != null && soldadoSpawn.TentarCriarColetor();
    }

    // =========================================================
    // SOLDADO
    // =========================================================
    public bool PodeCriarSoldado()
    {
        return soldadoSpawn != null && soldadoSpawn.PodeCriarSoldado();
    }

    public bool CriarSoldado()
    {
        return soldadoSpawn != null && soldadoSpawn.TentarCriarSoldado();
    }

    // =========================================================
    // GUERREIRO
    // =========================================================
    public bool PodeCriarGuerreiro()
    {
        return soldadoSpawn != null && soldadoSpawn.PodeCriarGuerreiro();
    }

    public bool CriarGuerreiro()
    {
        return soldadoSpawn != null && soldadoSpawn.TentarCriarGuerreiro();
    }

    // =========================================================
    // GETTERS
    // =========================================================
    public float GetTempoBaseSoldado() => tempoBaseSoldado;
    public int GetQuantidadeBaseSoldado() => quantidadeBaseSoldado;
    public int GetMaxBaseSoldado() => maxBaseSoldado;
}