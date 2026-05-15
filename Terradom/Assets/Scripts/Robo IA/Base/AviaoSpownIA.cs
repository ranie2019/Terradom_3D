using UnityEngine;

[DisallowMultipleComponent]
public class AviaoSpownIA : MonoBehaviour
{
    // =====================================================================
    // INSPECTOR
    // =====================================================================

    [Header("Ponto onde o avião vai nascer")]
    [SerializeField] private Transform pontoSpawn;

    [Header("Prefab Avião")]
    [SerializeField] private GameObject prefabAviao;

    [Header("Custo Avião")]
    [SerializeField] private int custoPedraAviao   = 10;
    [SerializeField] private int custoMadeiraAviao = 10;
    [SerializeField] private int custoMetalAviao   = 10;

    [Header("Delay entre spawns")]
    [SerializeField] private float tempoEntreSpawns = 2.5f;

    [Header("Evitar nascer um em cima do outro")]
    [SerializeField] private bool  usarEspacamentoEntreSpawns  = true;
    [SerializeField] private float distanciaEntreUnidadesSpawn = 3.0f;
    [SerializeField] private int   quantidadePosicoesPorLinha  = 2;

    [Header("Time")]
    [SerializeField] private string tagDoTime = "Vermelho";

    [Header("Debug")]
    [SerializeField] private bool mostrarLogs = false;

    // =====================================================================
    // PRIVADOS
    // =====================================================================

    private float      proximoSpawnPermitido;
    private int        contadorSpawns;

    // Controle de posição forçada — usado pelo RoboIA para revezamento entre bases
    private Vector3    posicaoForcada;
    private Quaternion rotacaoForcada;
    private bool       usarPosicaoForcada = false;

    // =====================================================================
    // COOLDOWN
    // =====================================================================

    public bool  EstaEmCooldown()        => Time.time < proximoSpawnPermitido;
    public float TempoRestanteCooldown() => Mathf.Max(0f, proximoSpawnPermitido - Time.time);

    // =====================================================================
    // PODE CRIAR
    // =====================================================================

    public bool PodeCriarAviao()
    {
        return PodeCriarUnidadeInterna(prefabAviao, custoPedraAviao, custoMadeiraAviao, custoMetalAviao);
    }

    // =====================================================================
    // TENTAR CRIAR (retorna bool)
    // =====================================================================

    /// <summary>Tenta criar um avião. Retorna true se conseguiu.</summary>
    public bool TentarCriarAviao()
    {
        if (!PodeCriarAviao()) return false;
        CriarAviao();
        return true;
    }

    // =====================================================================
    // CRIAR
    // =====================================================================

    public void CriarAviao()
    {
        CriarUnidadeInterna(prefabAviao, custoPedraAviao, custoMadeiraAviao, custoMetalAviao);
    }

    // Aliases de compatibilidade
    public bool PodeCriarAeronave()   => PodeCriarAviao();
    public bool TentarCriarAeronave() => TentarCriarAviao();
    public void CriarAeronave()       => CriarAviao();

    public bool PodeCriarVeiculo()   => PodeCriarAviao();
    public bool TentarCriarVeiculo() => TentarCriarAviao();
    public void CriarVeiculo()       => CriarAviao();

    // =====================================================================
    // MÉTODOS GENÉRICOS POR ÍNDICE (0 = Avião)
    // =====================================================================

    public bool PodeCriarPorIndice(int indice)
    {
        if (indice != 0) return false;
        return PodeCriarAviao();
    }

    public bool TentarCriarPorIndice(int indice)
    {
        if (indice != 0) return false;
        return TentarCriarAviao();
    }

    public void CriarPorIndice(int indice)
    {
        if (indice != 0) return;
        CriarAviao();
    }

    // Aliases genéricos
    public bool PodeCriarUnidadePorIndice(int i)  => PodeCriarPorIndice(i);
    public bool PodeCriarPrefabPorIndice(int i)   => PodeCriarPorIndice(i);
    public bool PodeCriarObjetoPorIndice(int i)   => PodeCriarPorIndice(i);
    public bool PodeCriarUnidade(int i)           => PodeCriarPorIndice(i);
    public bool PodeCriarPrefab(int i)            => PodeCriarPorIndice(i);
    public bool PodeCriar(int i)                  => PodeCriarPorIndice(i);

    public void CriarUnidadePorIndice(int i)      => CriarPorIndice(i);
    public void CriarPrefabPorIndice(int i)       => CriarPorIndice(i);
    public void CriarObjetoPorIndice(int i)       => CriarPorIndice(i);
    public void CriarUnidade(int i)               => CriarPorIndice(i);
    public void CriarPrefab(int i)                => CriarPorIndice(i);
    public void Criar(int i)                      => CriarPorIndice(i);

    public bool ExistePrefabPorIndice(int i)      => i == 0 && prefabAviao != null;
    public bool TemPrefabPorIndice(int i)         => ExistePrefabPorIndice(i);
    public bool ExisteUnidadePorIndice(int i)     => ExistePrefabPorIndice(i);
    public bool TemUnidadePorIndice(int i)        => ExistePrefabPorIndice(i);
    public bool IndiceExiste(int i)               => ExistePrefabPorIndice(i);
    public bool ExisteIndice(int i)               => ExistePrefabPorIndice(i);

    public int GetQuantidadePrefabs()  => prefabAviao != null ? 1 : 0;
    public int QuantidadePrefabs()     => GetQuantidadePrefabs();
    public int GetQuantidadeUnidades() => GetQuantidadePrefabs();
    public int QuantidadeUnidades()    => GetQuantidadePrefabs();
    public int GetTotalPrefabs()       => GetQuantidadePrefabs();
    public int TotalPrefabs()          => GetQuantidadePrefabs();

    // =====================================================================
    // MÉTODOS COM BASE (RoboIA passa o Transform da base diretamente)
    // =====================================================================

    public bool PodeCriarAviaoNaBase(Transform baseTransform)
    {
        if (prefabAviao   == null) return false;
        if (baseTransform == null) return false;
        if (EstaEmCooldown())      return false;

        if (GameControllerRecursosIA.Instance == null)
        {
            if (mostrarLogs) Debug.LogWarning("[AviaoSpownIA] GameControllerRecursosIA.Instance é null!");
            return false;
        }

        return GameControllerRecursosIA.Instance.TemRecursos(custoPedraAviao, custoMadeiraAviao, custoMetalAviao);
    }

    /// <summary>Tenta criar o avião na base indicada. Retorna true se conseguiu.</summary>
    public bool TentarCriarAviaoNaBase(Transform baseTransform)
    {
        if (!PodeCriarAviaoNaBase(baseTransform)) return false;
        CriarAviaoNaBase(baseTransform);
        return true;
    }

    /// <summary>Cria o avião exatamente na posição e rotação da base recebida.</summary>
    public void CriarAviaoNaBase(Transform baseTransform)
    {
        if (!PodeCriarAviaoNaBase(baseTransform)) return;

        if (!GameControllerRecursosIA.Instance.TentarGastarRecursos(custoPedraAviao, custoMadeiraAviao, custoMetalAviao))
            return;

        Vector3    posicaoSpawn = baseTransform.position;
        Quaternion rotacaoSpawn = baseTransform.rotation;

        GameObject novo = SpawnarNaPasta(prefabAviao, posicaoSpawn, rotacaoSpawn);

        DefinirTagDoTime(novo);
        AtivarObjetoCompleto(novo);

        contadorSpawns++;
        proximoSpawnPermitido = Time.time + Mathf.Max(0f, tempoEntreSpawns);

        if (mostrarLogs)
            Debug.Log($"[AviaoSpownIA] Avião spawnado na base '{baseTransform.name}' em {posicaoSpawn}. Total spawns: {contadorSpawns}");
    }

    // =====================================================================
    // FORÇAR POSIÇÃO DE SPAWN (RoboIA — revezamento entre bases)
    // =====================================================================

    /// <summary>
    /// Força a próxima unidade a spawnar exatamente nesta posição/rotação.
    /// O flag usarPosicaoForcada é resetado automaticamente após o spawn.
    /// </summary>
    public void ForcarPosicaoSpawn(Vector3 posicao, Quaternion rotacao)
    {
        posicaoForcada     = posicao;
        rotacaoForcada     = rotacao;
        usarPosicaoForcada = true;
    }

    // =====================================================================
    // DEFINIR PONTO DE SPAWN (ajuste dinâmico)
    // =====================================================================

    /// <summary>Define apenas a posição do ponto de spawn.</summary>
    public void DefinirPontoSpawn(Vector3 posicao)
    {
        if (pontoSpawn == null)
        {
            GameObject obj = new GameObject("PontoSpawnTemp_Aviao");
            pontoSpawn = obj.transform;
        }
        pontoSpawn.position = posicao;
    }

    /// <summary>Define a posição e rotação do ponto de spawn.</summary>
    public void DefinirPontoSpawn(Vector3 posicao, Quaternion rotacao)
    {
        if (pontoSpawn == null)
        {
            GameObject obj = new GameObject("PontoSpawnTemp_Aviao");
            pontoSpawn = obj.transform;
        }
        pontoSpawn.position = posicao;
        pontoSpawn.rotation = rotacao;
    }

    // =====================================================================
    // LÓGICA INTERNA — USA GameControllerRecursosIA
    // =====================================================================

    private bool PodeCriarUnidadeInterna(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal)
    {
        if (prefab == null || pontoSpawn == null)
            return false;

        if (EstaEmCooldown())
            return false;

        if (GameControllerRecursosIA.Instance == null)
        {
            if (mostrarLogs)
                Debug.LogWarning("[AviaoSpownIA] GameControllerRecursosIA.Instance é null! Certifique-se de que existe um GameControllerRecursosIA na cena.");
            return false;
        }

        return GameControllerRecursosIA.Instance.TemRecursos(custoPedra, custoMadeira, custoMetal);
    }

    private bool CriarUnidadeInterna(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal)
    {
        if (!PodeCriarUnidadeInterna(prefab, custoPedra, custoMadeira, custoMetal))
            return false;

        if (!GameControllerRecursosIA.Instance.TentarGastarRecursos(custoPedra, custoMadeira, custoMetal))
            return false;

        // Usa posição forçada (revezamento pelo RoboIA) ou calcula normalmente
        Vector3    posicaoSpawn;
        Quaternion rotacaoSpawn;

        if (usarPosicaoForcada)
        {
            posicaoSpawn       = posicaoForcada;
            rotacaoSpawn       = rotacaoForcada;
            usarPosicaoForcada = false; // reseta após o uso
        }
        else
        {
            posicaoSpawn = CalcularPosicaoSpawn();
            rotacaoSpawn = pontoSpawn != null ? pontoSpawn.rotation : transform.rotation;
        }

        GameObject novo = SpawnarNaPasta(prefab, posicaoSpawn, rotacaoSpawn);

        DefinirTagDoTime(novo);
        AtivarObjetoCompleto(novo);

        contadorSpawns++;
        proximoSpawnPermitido = Time.time + Mathf.Max(0f, tempoEntreSpawns);

        if (mostrarLogs)
            Debug.Log($"[AviaoSpownIA] Avião criado em {posicaoSpawn}! Total spawns: {contadorSpawns}");

        return true;
    }

    // =====================================================================
    // HELPERS INTERNOS
    // =====================================================================

    /// <summary>Faz Instantiate e organiza o clone na pasta "Clone Unidades IA".</summary>
    private GameObject SpawnarNaPasta(GameObject prefab, Vector3 posicao, Quaternion rotacao)
    {
        GameObject pasta = GameObject.Find("Clone Unidades IA");
        if (pasta == null)
            pasta = new GameObject("Clone Unidades IA");

        GameObject novo = Instantiate(prefab, posicao, rotacao, pasta.transform);
        novo.SetActive(true);
        return novo;
    }

    /// <summary>Tenta definir a tag do time no objeto. Ignora silenciosamente se a tag não existir.</summary>
    private void DefinirTagDoTime(GameObject obj)
    {
        if (obj == null || string.IsNullOrWhiteSpace(tagDoTime)) return;

        try   { obj.tag = tagDoTime; }
        catch { if (mostrarLogs) Debug.LogWarning($"[AviaoSpownIA] Não foi possível definir a tag '{tagDoTime}'. Verifique se a tag existe no projeto."); }
    }

    // =====================================================================
    // CALCULAR POSIÇÃO DE SPAWN
    // =====================================================================

    private Vector3 CalcularPosicaoSpawn()
    {
        Transform origem  = pontoSpawn != null ? pontoSpawn : transform;
        Vector3   posicao = origem.position;

        if (!usarEspacamentoEntreSpawns) return posicao;

        float distancia = Mathf.Max(0f, distanciaEntreUnidadesSpawn);
        if (distancia <= 0f) return posicao;

        int   quantidadeLinha     = Mathf.Max(1, quantidadePosicoesPorLinha);
        int   coluna              = contadorSpawns % quantidadeLinha;
        int   linha               = contadorSpawns / quantidadeLinha;
        float deslocamentoLateral = (coluna - (quantidadeLinha - 1) * 0.5f) * distancia;
        float deslocamentoFrente  = linha * distancia;

        Vector3 direita = origem.right;
        Vector3 frente  = origem.forward;

        direita.y = 0f;
        frente.y  = 0f;

        if (direita.sqrMagnitude < 0.001f) direita = Vector3.right;
        if (frente.sqrMagnitude  < 0.001f) frente  = Vector3.forward;

        direita.Normalize();
        frente.Normalize();

        return posicao + direita * deslocamentoLateral + frente * deslocamentoFrente;
    }

    // =====================================================================
    // ATIVAR OBJETO COMPLETO
    // =====================================================================

    private void AtivarObjetoCompleto(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(true);

        Transform[] filhos = obj.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < filhos.Length; i++)
            if (filhos[i] != null) filhos[i].gameObject.SetActive(true);

        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < scripts.Length; i++)
            if (scripts[i] != null) scripts[i].enabled = true;

        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = true;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].enabled = true;
    }

    // =====================================================================
    // VALIDAÇÃO
    // =====================================================================

    private void OnValidate()
    {
        custoPedraAviao             = Mathf.Max(0,  custoPedraAviao);
        custoMadeiraAviao           = Mathf.Max(0,  custoMadeiraAviao);
        custoMetalAviao             = Mathf.Max(0,  custoMetalAviao);
        tempoEntreSpawns            = Mathf.Max(0f, tempoEntreSpawns);
        distanciaEntreUnidadesSpawn = Mathf.Max(0f, distanciaEntreUnidadesSpawn);
        quantidadePosicoesPorLinha  = Mathf.Max(1,  quantidadePosicoesPorLinha);
    }
}