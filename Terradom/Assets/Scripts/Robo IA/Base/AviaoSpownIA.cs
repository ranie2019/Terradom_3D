using UnityEngine;

[DisallowMultipleComponent]
public class AviaoSpownIA : MonoBehaviour
{
    [Header("Ponto onde o avião vai nascer")]
    [SerializeField] private Transform pontoSpawn;

    [Header("Prefab Avião")]
    [SerializeField] private GameObject prefabAviao;

    [Header("Custo Avião")]
    [SerializeField] private int custoPedraAviao = 10;
    [SerializeField] private int custoMadeiraAviao = 10;
    [SerializeField] private int custoMetalAviao = 10;

    [Header("Delay entre spawns")]
    [SerializeField] private float tempoEntreSpawns = 2.5f;

    [Header("Evitar nascer um em cima do outro")]
    [SerializeField] private bool usarEspacamentoEntreSpawns = true;
    [SerializeField] private float distanciaEntreUnidadesSpawn = 3.0f;
    [SerializeField] private int quantidadePosicoesPorLinha = 2;

    [Header("Organizacao")]
    [SerializeField] private bool manterMesmoPaiDaBase = false;

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

    public bool PodeCriarAviao()
    {
        return PodeCriarUnidade(prefabAviao, custoPedraAviao, custoMadeiraAviao, custoMetalAviao);
    }

    /// <summary>
    /// Tenta criar um avião. Retorna true se conseguiu criar.
    /// </summary>
    public bool TentarCriarAviao()
    {
        if (!PodeCriarAviao())
            return false;

        CriarAviao();
        return true;
    }

    public void CriarAviao()
    {
        CriarUnidade(
            prefabAviao,
            custoPedraAviao,
            custoMadeiraAviao,
            custoMetalAviao
        );
    }

    public bool PodeCriarAeronave()
    {
        return PodeCriarAviao();
    }

    public bool TentarCriarAeronave()
    {
        return TentarCriarAviao();
    }

    public void CriarAeronave()
    {
        CriarAviao();
    }

    // =====================================================================
    // METODOS GENERICOS (COMPATIBILIDADE COM SISTEMA DE INDICES)
    // 0 = Avião
    // =====================================================================

    public bool PodeCriarPorIndice(int indice)
    {
        if (indice != 0)
            return false;

        return PodeCriarAviao();
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

        return TentarCriarAviao();
    }

    public void CriarPorIndice(int indice)
    {
        if (indice != 0)
            return;

        CriarAviao();
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
        return indice == 0 && prefabAviao != null;
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
        return prefabAviao != null ? 1 : 0;
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
                Debug.LogWarning($"[AviaoSpownIA] GameControllerRecursosIA.Instance é null! Certifique-se de que existe um GameControllerRecursosIA na cena.");
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

        GameObject novo = Instantiate(
            prefab,
            posicaoSpawn,
            rotacaoSpawn
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
                    Debug.LogWarning($"[AviaoSpownIA] Não foi possível definir a tag '{tagDoTime}'. Verifique se a tag existe no projeto.");
            }
        }

        AtivarObjetoCompleto(novo);

        if (manterMesmoPaiDaBase && transform.parent != null)
            novo.transform.SetParent(transform.parent);

        contadorSpawns++;
        proximoSpawnPermitido = Time.time + Mathf.Max(0f, tempoEntreSpawns);

        if (mostrarLogs)
            Debug.Log($"[AviaoSpownIA] Avião criado com sucesso! Total spawns: {contadorSpawns}");

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
        custoPedraAviao = Mathf.Max(0, custoPedraAviao);
        custoMadeiraAviao = Mathf.Max(0, custoMadeiraAviao);
        custoMetalAviao = Mathf.Max(0, custoMetalAviao);

        tempoEntreSpawns = Mathf.Max(0f, tempoEntreSpawns);
        distanciaEntreUnidadesSpawn = Mathf.Max(0f, distanciaEntreUnidadesSpawn);
        quantidadePosicoesPorLinha = Mathf.Max(1, quantidadePosicoesPorLinha);
    }
}