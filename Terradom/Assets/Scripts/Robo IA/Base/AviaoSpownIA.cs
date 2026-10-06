using UnityEngine;

[DisallowMultipleComponent]
public class AviaoSpownIA : MonoBehaviour
{
    [Header("Ponto onde o avião vai nascer")]
    [SerializeField] private Transform pontoSpawn;

    [Header("Prefab Avião")]
    [SerializeField] private GameObject prefabAviao;

    [Header("Custo Avião")]
    [SerializeField] private int   custoPedraAviao   = 10;
    [SerializeField] private int   custoMadeiraAviao = 10;
    [SerializeField] private int   custoMetalAviao   = 10;

    [Header("Delay entre spawns")]
    [SerializeField] private float tempoEntreSpawns = 2.5f;

    [Header("Espaçamento entre spawns")]
    [SerializeField] private bool  usarEspacamentoEntreSpawns  = true;
    [SerializeField] private float distanciaEntreUnidadesSpawn = 3.0f;
    [SerializeField] private int   quantidadePosicoesPorLinha  = 2;

    [Header("Time")]
    [SerializeField] private string tagDoTime = "Vermelho";

    [Header("Debug")]
    [SerializeField] private bool mostrarLogs = false;

    // =========================================================
    // PRIVADOS
    // =========================================================

    private float      proximoSpawnPermitido;
    private int        contadorSpawns;

    // Controle de posição forçada — revezamento entre bases pelo RoboIA
    private Vector3    posicaoForcada;
    private Quaternion rotacaoForcada;
    private bool       usarPosicaoForcada = false;

    // BUG 4 CORRIGIDO: antes SpawnarNaPasta chamava
    // GameObject.Find("Clone Unidades IA") a cada spawn.
    // Agora a pasta é cacheada na primeira busca e reutilizada.
    private Transform _pastaUnidades;

    // =========================================================
    // COOLDOWN
    // =========================================================

    public bool  EstaEmCooldown()        => Time.time < proximoSpawnPermitido;
    public float TempoRestanteCooldown() => Mathf.Max(0f, proximoSpawnPermitido - Time.time);

    // =========================================================
    // PODE CRIAR
    // =========================================================

    public bool PodeCriarAviao() =>
        PodeCriarUnidadeInterna(prefabAviao, custoPedraAviao, custoMadeiraAviao, custoMetalAviao);

    // =========================================================
    // TENTAR / CRIAR
    // =========================================================

    public bool TentarCriarAviao() { if (!PodeCriarAviao()) return false; CriarAviao(); return true; }
    public void CriarAviao()       => CriarUnidadeInterna(prefabAviao, custoPedraAviao, custoMadeiraAviao, custoMetalAviao);

    // Aliases de compatibilidade
    public bool PodeCriarAeronave()   => PodeCriarAviao();
    public bool TentarCriarAeronave() => TentarCriarAviao();
    public void CriarAeronave()       => CriarAviao();
    public bool PodeCriarVeiculo()    => PodeCriarAviao();
    public bool TentarCriarVeiculo()  => TentarCriarAviao();
    public void CriarVeiculo()        => CriarAviao();

    // =========================================================
    // GENÉRICOS POR ÍNDICE  (0 = Avião)
    // =========================================================

    public bool PodeCriarPorIndice(int i)   => i == 0 && PodeCriarAviao();
    public bool TentarCriarPorIndice(int i) { if (i != 0) return false; return TentarCriarAviao(); }
    public void CriarPorIndice(int i)       { if (i == 0) CriarAviao(); }

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

    // =========================================================
    // MÉTODOS NA BASE (RoboIA passa o Transform da base)
    // =========================================================

    public bool PodeCriarAviaoNaBase(Transform base_)
    {
        if (prefabAviao == null || base_ == null) return false;
        if (EstaEmCooldown()) return false;
        if (GameControllerRecursosIA.Instance == null)
        {
            if (mostrarLogs) Debug.LogWarning("[AviaoSpownIA] GameControllerRecursosIA.Instance é null!");
            return false;
        }
        return GameControllerRecursosIA.Instance.TemRecursos(custoPedraAviao, custoMadeiraAviao, custoMetalAviao);
    }

    public bool TentarCriarAviaoNaBase(Transform base_)
    {
        if (!PodeCriarAviaoNaBase(base_)) return false;
        CriarAviaoNaBase(base_);
        return true;
    }

    public void CriarAviaoNaBase(Transform base_)
    {
        if (!PodeCriarAviaoNaBase(base_)) return;
        if (!GameControllerRecursosIA.Instance.TentarGastarRecursos(custoPedraAviao, custoMadeiraAviao, custoMetalAviao)) return;

        GameObject novo = Instantiate(prefabAviao, base_.position, base_.rotation, ObterPastaUnidades());
        novo.SetActive(true);
        DefinirTag(novo);
        AtivarObjetoCompleto(novo);

        contadorSpawns++;
        proximoSpawnPermitido = Time.time + Mathf.Max(0f, tempoEntreSpawns);

        if (mostrarLogs) Debug.Log($"[AviaoSpownIA] Avião na base '{base_.name}' em {base_.position}. Total: {contadorSpawns}");
    }

    // =========================================================
    // FORÇAR POSIÇÃO (revezamento pelo RoboIA)
    // =========================================================

    public void ForcarPosicaoSpawn(Vector3 posicao, Quaternion rotacao)
    {
        posicaoForcada     = posicao;
        rotacaoForcada     = rotacao;
        usarPosicaoForcada = true;
    }

    public void DefinirPontoSpawn(Vector3 posicao)
    {
        if (pontoSpawn == null) pontoSpawn = new GameObject("PontoSpawnTemp_Aviao").transform;
        pontoSpawn.position = posicao;
    }

    public void DefinirPontoSpawn(Vector3 posicao, Quaternion rotacao)
    {
        if (pontoSpawn == null) pontoSpawn = new GameObject("PontoSpawnTemp_Aviao").transform;
        pontoSpawn.position = posicao;
        pontoSpawn.rotation = rotacao;
    }

    // =========================================================
    // LÓGICA INTERNA
    // =========================================================

    private bool PodeCriarUnidadeInterna(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal)
    {
        if (prefab == null || pontoSpawn == null) return false;
        if (EstaEmCooldown()) return false;
        if (GameControllerRecursosIA.Instance == null)
        {
            if (mostrarLogs) Debug.LogWarning("[AviaoSpownIA] GameControllerRecursosIA.Instance é null!");
            return false;
        }
        return GameControllerRecursosIA.Instance.TemRecursos(custoPedra, custoMadeira, custoMetal);
    }

    private bool CriarUnidadeInterna(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal)
    {
        if (!PodeCriarUnidadeInterna(prefab, custoPedra, custoMadeira, custoMetal)) return false;
        if (!GameControllerRecursosIA.Instance.TentarGastarRecursos(custoPedra, custoMadeira, custoMetal)) return false;

        Vector3    posicaoSpawn;
        Quaternion rotacaoSpawn;

        if (usarPosicaoForcada)
        {
            posicaoSpawn       = posicaoForcada;
            rotacaoSpawn       = rotacaoForcada;
            usarPosicaoForcada = false;
        }
        else
        {
            posicaoSpawn = CalcularPosicaoSpawn();
            rotacaoSpawn = pontoSpawn != null ? pontoSpawn.rotation : transform.rotation;
        }

        GameObject novo = Instantiate(prefab, posicaoSpawn, rotacaoSpawn, ObterPastaUnidades());
        novo.SetActive(true);
        DefinirTag(novo);
        AtivarObjetoCompleto(novo);

        contadorSpawns++;
        proximoSpawnPermitido = Time.time + Mathf.Max(0f, tempoEntreSpawns);

        if (mostrarLogs) Debug.Log($"[AviaoSpownIA] Avião criado em {posicaoSpawn}. Total: {contadorSpawns}");
        return true;
    }

    // BUG 4 CORRIGIDO: cache da pasta para evitar GameObject.Find a cada spawn.
    private Transform ObterPastaUnidades()
    {
        if (_pastaUnidades != null) return _pastaUnidades;
        GameObject obj = GameObject.Find("Clone Unidades IA");
        if (obj == null) obj = new GameObject("Clone Unidades IA");
        _pastaUnidades = obj.transform;
        return _pastaUnidades;
    }

    private void DefinirTag(GameObject obj)
    {
        if (obj == null || string.IsNullOrWhiteSpace(tagDoTime)) return;
        try { obj.tag = tagDoTime; }
        catch { if (mostrarLogs) Debug.LogWarning($"[AviaoSpownIA] Tag '{tagDoTime}' não existe no projeto."); }
    }

    // BUG 5 CORRIGIDO: antes usava foreach com GetComponentsInChildren,
    // alocando arrays e enumeradores a cada spawn.
    // Agora usa loops for com indexação direta.
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

    private Vector3 CalcularPosicaoSpawn()
    {
        Transform origem = pontoSpawn != null ? pontoSpawn : transform;
        Vector3   pos    = origem.position;

        if (!usarEspacamentoEntreSpawns) return pos;

        float distancia = Mathf.Max(0f, distanciaEntreUnidadesSpawn);
        if (distancia <= 0f) return pos;

        int   qtdLinha = Mathf.Max(1, quantidadePosicoesPorLinha);
        int   coluna   = contadorSpawns % qtdLinha;
        int   linha    = contadorSpawns / qtdLinha;
        float lateral  = (coluna - (qtdLinha - 1) * 0.5f) * distancia;
        float frente   = linha * distancia;

        Vector3 dir = origem.right;   dir.y = 0f; if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;   dir.Normalize();
        Vector3 fwd = origem.forward; fwd.y = 0f; if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward; fwd.Normalize();

        return pos + dir * lateral + fwd * frente;
    }

    // =========================================================
    // VALIDAÇÃO
    // =========================================================

    private void OnValidate()
    {
        custoPedraAviao             = Mathf.Max(0,    custoPedraAviao);
        custoMadeiraAviao           = Mathf.Max(0,    custoMadeiraAviao);
        custoMetalAviao             = Mathf.Max(0,    custoMetalAviao);
        tempoEntreSpawns            = Mathf.Max(0f,   tempoEntreSpawns);
        distanciaEntreUnidadesSpawn = Mathf.Max(0f,   distanciaEntreUnidadesSpawn);
        quantidadePosicoesPorLinha  = Mathf.Max(1,    quantidadePosicoesPorLinha);
    }
}