using UnityEngine;

[DisallowMultipleComponent]
public class BaseTankIA : MonoBehaviour
{
    private BaseAreaIA baseArea;
    private TankSpownIA tankSpawn;

    [Header("Tempo entre bases")]
    [SerializeField] private float tempoBaseTank = 120f;

    [Header("Limite de bases")]
    [SerializeField] private int maxBaseTank = 10;

    private int quantidadeBaseTank;
    private bool setupTankFeito = false;

    private void Awake()
    {
        AtualizarReferencias();
        Debug.Log("[BaseTankIA] Awake - Setup concluído. baseArea=" + (baseArea != null) + " tankSpawn=" + (tankSpawn != null));
    }

    private void Update()
    {
        GarantirReferencias();

        // =====================================================
        // SETUP OBRIGATÓRIO
        // =====================================================
        if (!setupTankFeito)
        {
            Debug.Log("[BaseTankIA] Tentando setup inicial...");
            
            if (CriarBaseTank())
            {
                quantidadeBaseTank++;
                tempoBaseTank = 120f; // Reinicia o tempo
                setupTankFeito = true;
                Debug.Log("[BaseTankIA] Setup inicial CONCLUÍDO! Base tank criada.");
            }
            else
            {
                Debug.LogWarning("[BaseTankIA] Setup inicial FALHOU! Tentando novamente no próximo frame...");
            }
            return;
        }

        // =====================================================
        // TIMER TANK (CONTAGEM REGRESSIVA NO PRÓPRIO TEMPO)
        // =====================================================
        if (tempoBaseTank > 0f)
        {
            tempoBaseTank -= Time.deltaTime;
            
            if (tempoBaseTank <= 0f)
            {
                tempoBaseTank = 0f; // Trava no 0
                
                Debug.Log("[BaseTankIA] Timer zerado! Tentando criar nova base tank...");
                
                if (quantidadeBaseTank < maxBaseTank)
                {
                    if (CriarBaseTank())
                    {
                        quantidadeBaseTank++;
                        Debug.Log("[BaseTankIA] Nova base tank criada! Total: " + quantidadeBaseTank);
                    }
                    else
                    {
                        Debug.LogError("[BaseTankIA] FALHA ao criar nova base tank!");
                    }
                }
                else
                {
                    Debug.Log("[BaseTankIA] Limite máximo atingido: " + maxBaseTank);
                }
                
                tempoBaseTank = 120f; // Reinicia o tempo
            }
        }
    }

    // =========================================================
    // REFERÊNCIAS
    // =========================================================

    public void AtualizarReferencias()
    {
        if (baseArea == null)
        {
            baseArea = FindObjectOfType<BaseAreaIA>();
            if (baseArea != null)
                Debug.Log("[BaseTankIA] BaseAreaIA encontrada!");
            else
                Debug.LogError("[BaseTankIA] BaseAreaIA NÃO encontrada na cena!");
        }

        if (tankSpawn == null)
        {
            tankSpawn = FindObjectOfType<TankSpownIA>();
            if (tankSpawn != null)
                Debug.Log("[BaseTankIA] TankSpownIA encontrada!");
            else
                Debug.LogError("[BaseTankIA] TankSpownIA NÃO encontrada na cena!");
        }
    }

    private void GarantirReferencias()
    {
        if (baseArea == null || tankSpawn == null)
        {
            Debug.LogWarning("[BaseTankIA] Referências perdidas! Tentando recuperar...");
            AtualizarReferencias();
        }
    }

    // =========================================================
    // BASE TANK
    // =========================================================

    public bool CriarBaseTank()
    {
        if (baseArea == null)
        {
            Debug.LogError("[BaseTankIA] ERRO: baseArea é null ao tentar criar base tank!");
            return false;
        }

        Debug.Log("[BaseTankIA] Chamando baseArea.TentarCriarBasePorIndice(1)...");
        bool resultado = baseArea.TentarCriarBasePorIndice(1);
        Debug.Log("[BaseTankIA] Resultado de TentarCriarBasePorIndice(1): " + resultado);
        
        return resultado;
    }

    public bool BaseTankExiste()
    {
        if (baseArea == null)
        {
            Debug.LogError("[BaseTankIA] ERRO: baseArea é null ao verificar existência!");
            return false;
        }

        bool existe = baseArea.ExisteBasePorIndice(1);
        Debug.Log("[BaseTankIA] Verificando se base tank existe: " + existe);
        return existe;
    }

    // =========================================================
    // TANK
    // =========================================================
    public bool PodeCriarTank()
    {
        if (tankSpawn == null)
        {
            Debug.LogError("[BaseTankIA] ERRO: tankSpawn é null!");
            return false;
        }

        bool pode = tankSpawn.PodeCriarTank();
        Debug.Log("[BaseTankIA] Pode criar tank? " + pode);
        return pode;
    }

    public bool CriarTank()
    {
        if (tankSpawn == null)
        {
            Debug.LogError("[BaseTankIA] ERRO: tankSpawn é null!");
            return false;
        }

        Debug.Log("[BaseTankIA] Tentando criar unidade tank...");
        bool criado = tankSpawn.TentarCriarTank();
        Debug.Log("[BaseTankIA] Unidade tank criada? " + criado);
        return criado;
    }

    // =========================================================
    // GETTERS
    // =========================================================
    public float GetTempoBaseTank() => tempoBaseTank;
    public int GetQuantidadeBaseTank() => quantidadeBaseTank;
    public int GetMaxBaseTank() => maxBaseTank;
}