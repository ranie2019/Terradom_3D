using UnityEngine;

[DisallowMultipleComponent]
public class TankSpownIA : MonoBehaviour
{
    [Header("Ponto onde o tank vai nascer")]
    [SerializeField] private Transform pontoSpawn;

    [Header("Prefab Tank Leve")]
    [SerializeField] private GameObject prefabTankLeve;

    [Header("Custo Tank Leve")]
    [SerializeField] private int custoPedraTankLeve = 10;
    [SerializeField] private int custoMadeiraTankLeve = 10;
    [SerializeField] private int custoMetalTankLeve = 10;

    [Header("Delay entre spawns")]
    [SerializeField] private float tempoEntreSpawns = 1.8f;

    [Header("Evitar nascer um em cima do outro")]
    [SerializeField] private bool usarEspacamentoEntreSpawns = true;
    [SerializeField] private float distanciaEntreUnidadesSpawn = 2.2f;
    [SerializeField] private int quantidadePosicoesPorLinha = 3;

    [Header("Time")]
    [SerializeField] private string tagDoTime = "Vermelho";

    [Header("Debug")]
    [SerializeField] private bool mostrarLogs = false;

    private float proximoSpawnPermitido;
    private int contadorSpawns;

    public bool EstaEmCooldown()
    {
        return Time.time < proximoSpawnPermitido;
    }

    public float TempoRestanteCooldown()
    {
        return Mathf.Max(0f, proximoSpawnPermitido - Time.time);
    }

    public bool PodeCriarTank()
    {
        return PodeCriarUnidade(prefabTankLeve, custoPedraTankLeve, custoMadeiraTankLeve, custoMetalTankLeve);
    }

    /// <summary>
    /// Tenta criar um tank. Retorna true se conseguiu criar.
    /// </summary>
    public bool TentarCriarTank()
    {
        if (!PodeCriarTank())
            return false;

        CriarTank();
        return true;
    }

    public void CriarTank()
    {
        CriarUnidade(
            prefabTankLeve,
            custoPedraTankLeve,
            custoMadeiraTankLeve,
            custoMetalTankLeve
        );
    }

    public bool PodeCriarVeiculo()
    {
        return PodeCriarTank();
    }

    public bool TentarCriarVeiculo()
    {
        return TentarCriarTank();
    }

    public void CriarVeiculo()
    {
        CriarTank();
    }

    // =====================================================================
    // METODOS GENERICOS (COMPATIBILIDADE COM SISTEMA DE INDICES)
    // 0 = Tank Leve
    // =====================================================================

    public bool PodeCriarPorIndice(int indice)
    {
        if (indice != 0)
            return false;

        return PodeCriarTank();
    }

    public bool PodeCriarUnidadePorIndice(int indice)
    {
        return PodeCriarPorIndice(indice);
    }

    public bool PodeCriarPrefabPorIndice(int indice)
    {
        return PodeCriarPorIndice(indice);
    }

    public bool PodeCriarObjetoPorIndice(int indice)
    {
        return PodeCriarPorIndice(indice);
    }

    public bool PodeCriarUnidade(int indice)
    {
        return PodeCriarPorIndice(indice);
    }

    public bool PodeCriarPrefab(int indice)
    {
        return PodeCriarPorIndice(indice);
    }

    public bool PodeCriar(int indice)
    {
        return PodeCriarPorIndice(indice);
    }

    /// <summary>
    /// Tenta criar por índice. Retorna true se conseguiu.
    /// </summary>
    public bool TentarCriarPorIndice(int indice)
    {
        if (indice != 0)
            return false;

        return TentarCriarTank();
    }

    public void CriarPorIndice(int indice)
    {
        if (indice != 0)
            return;

        CriarTank();
    }

    public void CriarUnidadePorIndice(int indice)
    {
        CriarPorIndice(indice);
    }

    public void CriarPrefabPorIndice(int indice)
    {
        CriarPorIndice(indice);
    }

    public void CriarObjetoPorIndice(int indice)
    {
        CriarPorIndice(indice);
    }

    public void CriarUnidade(int indice)
    {
        CriarPorIndice(indice);
    }

    public void CriarPrefab(int indice)
    {
        CriarPorIndice(indice);
    }

    public void Criar(int indice)
    {
        CriarPorIndice(indice);
    }

    public bool ExistePrefabPorIndice(int indice)
    {
        return indice == 0 && prefabTankLeve != null;
    }

    public bool TemPrefabPorIndice(int indice)
    {
        return ExistePrefabPorIndice(indice);
    }

    public bool ExisteUnidadePorIndice(int indice)
    {
        return ExistePrefabPorIndice(indice);
    }

    public bool TemUnidadePorIndice(int indice)
    {
        return ExistePrefabPorIndice(indice);
    }

    public bool IndiceExiste(int indice)
    {
        return ExistePrefabPorIndice(indice);
    }

    public bool ExisteIndice(int indice)
    {
        return ExistePrefabPorIndice(indice);
    }

    public int GetQuantidadePrefabs()
    {
        return prefabTankLeve != null ? 1 : 0;
    }

    public int QuantidadePrefabs()
    {
        return GetQuantidadePrefabs();
    }

    public int GetQuantidadeUnidades()
    {
        return GetQuantidadePrefabs();
    }

    public int QuantidadeUnidades()
    {
        return GetQuantidadePrefabs();
    }

    public int GetTotalPrefabs()
    {
        return GetQuantidadePrefabs();
    }

    public int TotalPrefabs()
    {
        return GetQuantidadePrefabs();
    }

    // =====================================================================
    // LOGICA INTERNA (USA GameControllerRecursosIA)
    // =====================================================================

    private bool PodeCriarUnidade(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal)
    {
        if (prefab == null || pontoSpawn == null)
            return false;

        if (EstaEmCooldown())
            return false;

        if (GameControllerRecursosIA.Instance == null)
        {
            if (mostrarLogs)
                Debug.LogWarning($"[TankSpownIA] GameControllerRecursosIA.Instance é null! Certifique-se de que existe um GameControllerRecursosIA na cena.");
            return false;
        }

        return GameControllerRecursosIA.Instance.TemRecursos(
            custoPedra,
            custoMadeira,
            custoMetal
        );
    }

    private bool CriarUnidade(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal)
    {
        if (!PodeCriarUnidade(prefab, custoPedra, custoMadeira, custoMetal))
            return false;

        bool gastou = GameControllerRecursosIA.Instance.TentarGastarRecursos(
            custoPedra,
            custoMadeira,
            custoMetal
        );

        if (!gastou)
            return false;

        Vector3 posicaoSpawn = CalcularPosicaoSpawn();
        Quaternion rotacaoSpawn = pontoSpawn != null ? pontoSpawn.rotation : transform.rotation;

        GameObject obj = GameObject.Find("Clone IA");

        if (obj == null)
        {
            obj = new GameObject("Clone IA");
        }

        Transform pastaClones = obj.transform;

        GameObject novo = Instantiate(
            prefab,
            posicaoSpawn,
            rotacaoSpawn,
            pastaClones
        );

        novo.SetActive(true);

        // Define a tag do time da IA
        if (!string.IsNullOrWhiteSpace(tagDoTime))
        {
            try
            {
                novo.tag = tagDoTime;
            }
            catch
            {
                if (mostrarLogs)
                    Debug.LogWarning($"[TankSpownIA] Não foi possível definir a tag '{tagDoTime}'. Verifique se a tag existe no projeto.");
            }
        }

        AtivarObjetoCompleto(novo);

        contadorSpawns++;
        proximoSpawnPermitido = Time.time + Mathf.Max(0f, tempoEntreSpawns);

        if (mostrarLogs)
            Debug.Log($"[TankSpownIA] Tank criado com sucesso! Total spawns: {contadorSpawns}");

        return true;
    }

    private Vector3 CalcularPosicaoSpawn()
    {
        Transform origem = pontoSpawn != null ? pontoSpawn : transform;
        Vector3 posicao = origem.position;

        if (!usarEspacamentoEntreSpawns)
            return posicao;

        float distancia = Mathf.Max(0f, distanciaEntreUnidadesSpawn);

        if (distancia <= 0f)
            return posicao;

        int quantidadeLinha = Mathf.Max(1, quantidadePosicoesPorLinha);
        int coluna = contadorSpawns % quantidadeLinha;
        int linha = contadorSpawns / quantidadeLinha;

        float deslocamentoLateral = (coluna - (quantidadeLinha - 1) * 0.5f) * distancia;
        float deslocamentoFrente = linha * distancia;

        Vector3 direita = origem.right;
        Vector3 frente = origem.forward;

        direita.y = 0f;
        frente.y = 0f;

        if (direita.sqrMagnitude < 0.001f)
            direita = Vector3.right;

        if (frente.sqrMagnitude < 0.001f)
            frente = Vector3.forward;

        direita.Normalize();
        frente.Normalize();

        return posicao + direita * deslocamentoLateral + frente * deslocamentoFrente;
    }

    private void AtivarObjetoCompleto(GameObject obj)
    {
        if (obj == null)
            return;

        obj.SetActive(true);

        Transform[] filhos = obj.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < filhos.Length; i++)
        {
            if (filhos[i] != null)
                filhos[i].gameObject.SetActive(true);
        }

        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null)
                scripts[i].enabled = true;
        }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = true;
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = true;
        }
    }

    private void OnValidate()
    {
        custoPedraTankLeve = Mathf.Max(0, custoPedraTankLeve);
        custoMadeiraTankLeve = Mathf.Max(0, custoMadeiraTankLeve);
        custoMetalTankLeve = Mathf.Max(0, custoMetalTankLeve);

        tempoEntreSpawns = Mathf.Max(0f, tempoEntreSpawns);
        distanciaEntreUnidadesSpawn = Mathf.Max(0f, distanciaEntreUnidadesSpawn);
        quantidadePosicoesPorLinha = Mathf.Max(1, quantidadePosicoesPorLinha);
    }
}