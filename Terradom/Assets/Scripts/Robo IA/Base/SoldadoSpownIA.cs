using UnityEngine;

[DisallowMultipleComponent]
public class SoldadoSpownIA : MonoBehaviour, IGameOverRecoverySpawner
{
    [Header("Ponto de Spawn (OBRIGATÓRIO)")]
    [SerializeField] private Transform pontoSpawn;

    [Header("Guerreiro")]
    [SerializeField] private GameObject prefabGuerreiro;
    [SerializeField] private int   custoPedraGuerreiro   = 10;
    [SerializeField] private int   custoMadeiraGuerreiro = 10;
    [SerializeField] private int   custoMetalGuerreiro   = 10;
    [SerializeField] private float delayGuerreiro        = 1.2f;

    [Header("Coletor/Recurso")]
    [SerializeField] private GameObject prefabRecurso;
    [SerializeField] private int   custoPedraRecurso   = 10;
    [SerializeField] private int   custoMadeiraRecurso = 10;
    [SerializeField] private int   custoMetalRecurso   = 10;
    [SerializeField] private float delayRecurso        = 1.2f;

    [Header("Soldado")]
    [SerializeField] private GameObject prefabSoldado;
    [SerializeField] private int   custoPedraSoldado   = 10;
    [SerializeField] private int   custoMadeiraSoldado = 10;
    [SerializeField] private int   custoMetalSoldado   = 10;
    [SerializeField] private float delaySoldado        = 1.2f;

    [Header("Espaçamento entre spawns")]
    [SerializeField] private bool  usarEspacamentoEntreSpawns  = true;
    [SerializeField] private float distanciaEntreUnidadesSpawn = 1.2f;
    [SerializeField] private int   quantidadePosicoesPorLinha  = 4;

    [Header("Ajuste de altura")]
    [SerializeField] private float alturaOffset = 0.5f;

    [Header("Time")]
    [SerializeField] private string tagDoTime = "Vermelho";

    [Header("Debug")]
    [SerializeField] private bool mostrarLogs = false;

    // =========================================================
    // PRIVADOS
    // =========================================================

    private float proximoSpawnGuerreiroPermitido;
    private float proximoSpawnRecursoPermitido;
    private float proximoSpawnSoldadoPermitido;
    private int   contadorSpawns;

    // Controle de posição forçada — revezamento entre bases pelo RoboIA
    private Vector3    posicaoForcada;
    private Quaternion rotacaoForcada;
    private bool       usarPosicaoForcada = false;

    // BUG 4 CORRIGIDO: antes chamava GameObject.Find("Clone Unidades IA") a cada spawn.
    // Agora a pasta é cacheada na primeira busca e reutilizada.
    private Transform _pastaUnidades;

    // =========================================================
    // COOLDOWN
    // =========================================================

    public bool  GuerreiroEstaEmCooldown() => Time.time < proximoSpawnGuerreiroPermitido;
    public bool  RecursoEstaEmCooldown()   => Time.time < proximoSpawnRecursoPermitido;
    public bool  ColetorEstaEmCooldown()   => RecursoEstaEmCooldown();
    public bool  SoldadoEstaEmCooldown()   => Time.time < proximoSpawnSoldadoPermitido;

    public float TempoRestanteCooldownGuerreiro() => Mathf.Max(0f, proximoSpawnGuerreiroPermitido - Time.time);
    public float TempoRestanteCooldownRecurso()   => Mathf.Max(0f, proximoSpawnRecursoPermitido   - Time.time);
    public float TempoRestanteCooldownColetor()   => TempoRestanteCooldownRecurso();
    public float TempoRestanteCooldownSoldado()   => Mathf.Max(0f, proximoSpawnSoldadoPermitido   - Time.time);

    public bool  EstaEmCooldown() =>
        GuerreiroEstaEmCooldown() || RecursoEstaEmCooldown() || SoldadoEstaEmCooldown();

    public float TempoRestanteCooldown() =>
        Mathf.Max(TempoRestanteCooldownGuerreiro(),
        Mathf.Max(TempoRestanteCooldownRecurso(), TempoRestanteCooldownSoldado()));

    // =========================================================
    // VALIDAÇÃO
    // =========================================================

    public bool PodeCriarGuerreiro() => PodeCriarUnidade(prefabGuerreiro, custoPedraGuerreiro, custoMadeiraGuerreiro, custoMetalGuerreiro, proximoSpawnGuerreiroPermitido);
    public bool PodeCriarRecurso()   => PodeCriarUnidade(prefabRecurso,   custoPedraRecurso,   custoMadeiraRecurso,   custoMetalRecurso,   proximoSpawnRecursoPermitido);
    public bool PodeCriarColetor()   => PodeCriarRecurso();
    public bool PodeCriarSoldado()   => PodeCriarUnidade(prefabSoldado,   custoPedraSoldado,   custoMadeiraSoldado,   custoMetalSoldado,   proximoSpawnSoldadoPermitido);

    public bool TemRecursosParaCriarColetor()
    {
        return tagDoTime == "Vermelho"
            && prefabRecurso != null
            && pontoSpawn != null
            && GameControllerRecursosIA.Instance != null
            && GameControllerRecursosIA.Instance.TemRecursos(
                custoPedraRecurso, custoMadeiraRecurso, custoMetalRecurso);
    }

    // =========================================================
    // TENTAR CRIAR
    // =========================================================

    public bool TentarCriarGuerreiro() { if (!PodeCriarGuerreiro()) return false; CriarGuerreiro(); return true; }
    public bool TentarCriarRecurso()   { if (!PodeCriarRecurso())   return false; CriarRecurso();   return true; }
    public bool TentarCriarColetor()   => TentarCriarRecurso();
    public bool TentarCriarSoldado()   { if (!PodeCriarSoldado())   return false; CriarSoldado();   return true; }

    // =========================================================
    // CRIAR
    // =========================================================

    public void CriarGuerreiro() => CriarUnidade(prefabGuerreiro, custoPedraGuerreiro, custoMadeiraGuerreiro, custoMetalGuerreiro, delayGuerreiro, ref proximoSpawnGuerreiroPermitido);
    public void CriarRecurso()   => CriarUnidade(prefabRecurso,   custoPedraRecurso,   custoMadeiraRecurso,   custoMetalRecurso,   delayRecurso,   ref proximoSpawnRecursoPermitido);
    public void CriarColetor()   => CriarRecurso();
    public void CriarSoldado()   => CriarUnidade(prefabSoldado,   custoPedraSoldado,   custoMadeiraSoldado,   custoMetalSoldado,   delaySoldado,   ref proximoSpawnSoldadoPermitido);

    // =========================================================
    // GENÉRICOS POR ÍNDICE  (0=Guerreiro 1=Recurso 2=Soldado)
    // =========================================================

    public void CriarPorIndice(int i)
    {
        switch (i) { case 0: CriarGuerreiro(); break; case 1: CriarRecurso(); break; case 2: CriarSoldado(); break; }
    }

    public bool TentarCriarPorIndice(int i)
    {
        switch (i) { case 0: return TentarCriarGuerreiro(); case 1: return TentarCriarRecurso(); case 2: return TentarCriarSoldado(); default: return false; }
    }

    public bool PodeCriarPorIndice(int i)
    {
        switch (i) { case 0: return PodeCriarGuerreiro(); case 1: return PodeCriarRecurso(); case 2: return PodeCriarSoldado(); default: return false; }
    }

    // Aliases de compatibilidade
    public void CriarUnidadePorIndice(int i) => CriarPorIndice(i);
    public void CriarPrefabPorIndice(int i)  => CriarPorIndice(i);
    public void CriarObjetoPorIndice(int i)  => CriarPorIndice(i);
    public void CriarUnidade(int i)          => CriarPorIndice(i);
    public void CriarPrefab(int i)           => CriarPorIndice(i);
    public void Criar(int i)                 => CriarPorIndice(i);

    public bool PodeCriarUnidadePorIndice(int i) => PodeCriarPorIndice(i);
    public bool PodeCriarPrefabPorIndice(int i)  => PodeCriarPorIndice(i);
    public bool PodeCriarObjetoPorIndice(int i)  => PodeCriarPorIndice(i);
    public bool PodeCriarUnidade(int i)          => PodeCriarPorIndice(i);
    public bool PodeCriarPrefab(int i)           => PodeCriarPorIndice(i);
    public bool PodeCriar(int i)                 => PodeCriarPorIndice(i);

    public bool ExistePrefabPorIndice(int i)
    {
        switch (i) { case 0: return prefabGuerreiro != null; case 1: return prefabRecurso != null; case 2: return prefabSoldado != null; default: return false; }
    }

    public bool TemPrefabPorIndice(int i)    => ExistePrefabPorIndice(i);
    public bool ExisteUnidadePorIndice(int i) => ExistePrefabPorIndice(i);
    public bool TemUnidadePorIndice(int i)   => ExistePrefabPorIndice(i);
    public bool IndiceExiste(int i)          => ExistePrefabPorIndice(i);
    public bool ExisteIndice(int i)          => ExistePrefabPorIndice(i);

    public int GetQuantidadePrefabs()  => 3;
    public int QuantidadePrefabs()     => 3;
    public int GetQuantidadeUnidades() => 3;
    public int QuantidadeUnidades()    => 3;
    public int GetTotalPrefabs()       => 3;
    public int TotalPrefabs()          => 3;

    // =========================================================
    // MÉTODOS NA BASE (RoboIA passa o Transform da base)
    // =========================================================

    public bool PodeCriarColetorNaBase(Transform base_)  => PodeCriarNaBase(prefabRecurso,   custoPedraRecurso,   custoMadeiraRecurso,   custoMetalRecurso,   proximoSpawnRecursoPermitido,   base_);
    public bool PodeCriarSoldadoNaBase(Transform base_)  => PodeCriarNaBase(prefabSoldado,   custoPedraSoldado,   custoMadeiraSoldado,   custoMetalSoldado,   proximoSpawnSoldadoPermitido,   base_);
    public bool PodeCriarGuerreiroNaBase(Transform base_) => PodeCriarNaBase(prefabGuerreiro, custoPedraGuerreiro, custoMadeiraGuerreiro, custoMetalGuerreiro, proximoSpawnGuerreiroPermitido, base_);

    public bool TentarCriarColetorNaBase(Transform base_)
    {
        if (!PodeCriarColetorNaBase(base_)) return false;
        CriarNaBase(prefabRecurso, custoPedraRecurso, custoMadeiraRecurso, custoMetalRecurso, delayRecurso, ref proximoSpawnRecursoPermitido, base_);
        return true;
    }

    public bool TentarCriarSoldadoNaBase(Transform base_)
    {
        if (!PodeCriarSoldadoNaBase(base_)) return false;
        CriarNaBase(prefabSoldado, custoPedraSoldado, custoMadeiraSoldado, custoMetalSoldado, delaySoldado, ref proximoSpawnSoldadoPermitido, base_);
        return true;
    }

    public bool TentarCriarGuerreiroNaBase(Transform base_)
    {
        if (!PodeCriarGuerreiroNaBase(base_)) return false;
        CriarNaBase(prefabGuerreiro, custoPedraGuerreiro, custoMadeiraGuerreiro, custoMetalGuerreiro, delayGuerreiro, ref proximoSpawnGuerreiroPermitido, base_);
        return true;
    }

    // =========================================================
    // FORÇAR POSIÇÃO DE SPAWN (revezamento pelo RoboIA)
    // =========================================================

    public void ForcarPosicaoSpawn(Vector3 posicao, Quaternion rotacao)
    {
        posicaoForcada     = posicao;
        rotacaoForcada     = rotacao;
        usarPosicaoForcada = true;
    }

    public void DefinirPontoSpawn(Vector3 posicao)
    {
        if (pontoSpawn == null) pontoSpawn = new GameObject("PontoSpawnTemp_Soldado").transform;
        pontoSpawn.position = posicao;
    }

    public void DefinirPontoSpawn(Vector3 posicao, Quaternion rotacao)
    {
        if (pontoSpawn == null) pontoSpawn = new GameObject("PontoSpawnTemp_Soldado").transform;
        pontoSpawn.position = posicao;
        pontoSpawn.rotation = rotacao;
    }

    // =========================================================
    // LÓGICA INTERNA
    // =========================================================

    private bool PodeCriarUnidade(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal, float proximoSpawn)
    {
        if (prefab == null) return false;
        if (pontoSpawn == null && transform == null) return false;
        if (Time.time < proximoSpawn) return false;
        if (GameControllerRecursosIA.Instance == null)
        {
            if (mostrarLogs) Debug.LogWarning("[SoldadoSpownIA] GameControllerRecursosIA.Instance é null!");
            return false;
        }
        return GameControllerRecursosIA.Instance.TemRecursos(custoPedra, custoMadeira, custoMetal);
    }

    private bool CriarUnidade(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal, float delay, ref float proximoSpawnRef)
    {
        if (!PodeCriarUnidade(prefab, custoPedra, custoMadeira, custoMetal, proximoSpawnRef)) return false;
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
        proximoSpawnRef = Time.time + Mathf.Max(0f, delay);

        if (mostrarLogs) Debug.Log($"[SoldadoSpownIA] '{prefab.name}' criado em {posicaoSpawn} | Total: {contadorSpawns}");
        return true;
    }

    private bool PodeCriarNaBase(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal, float proximoSpawn, Transform base_)
    {
        if (prefab == null || base_ == null) return false;
        if (Time.time < proximoSpawn) return false;
        if (GameControllerRecursosIA.Instance == null)
        {
            if (mostrarLogs) Debug.LogWarning("[SoldadoSpownIA] GameControllerRecursosIA.Instance é null!");
            return false;
        }
        return GameControllerRecursosIA.Instance.TemRecursos(custoPedra, custoMadeira, custoMetal);
    }

    private void CriarNaBase(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal, float delay, ref float proximoSpawnRef, Transform base_)
    {
        if (!PodeCriarNaBase(prefab, custoPedra, custoMadeira, custoMetal, proximoSpawnRef, base_)) return;
        if (!GameControllerRecursosIA.Instance.TentarGastarRecursos(custoPedra, custoMadeira, custoMetal)) return;

        GameObject novo = Instantiate(prefab, base_.position, base_.rotation, ObterPastaUnidades());
        novo.SetActive(true);
        DefinirTag(novo);
        AtivarObjetoCompleto(novo);

        contadorSpawns++;
        proximoSpawnRef = Time.time + Mathf.Max(0f, delay);

        if (mostrarLogs) Debug.Log($"[SoldadoSpownIA] '{prefab.name}' na base {base_.name} em {base_.position}");
    }

    // BUG 4 CORRIGIDO: cache da pasta — evita GameObject.Find a cada spawn.
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
        try { obj.tag = tagDoTime; } catch { }
    }

    // BUG 5 CORRIGIDO: antes usava foreach com GetComponentsInChildren,
    // alocando arrays e enumeradores a cada spawn.
    // Agora usa indexação direta com arrays e loops for.
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
        pos.y += alturaOffset;

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
        custoPedraGuerreiro             = Mathf.Max(0,    custoPedraGuerreiro);
        custoMadeiraGuerreiro           = Mathf.Max(0,    custoMadeiraGuerreiro);
        custoMetalGuerreiro             = Mathf.Max(0,    custoMetalGuerreiro);
        delayGuerreiro                  = Mathf.Max(0f,   delayGuerreiro);
        custoPedraRecurso               = Mathf.Max(0,    custoPedraRecurso);
        custoMadeiraRecurso             = Mathf.Max(0,    custoMadeiraRecurso);
        custoMetalRecurso               = Mathf.Max(0,    custoMetalRecurso);
        delayRecurso                    = Mathf.Max(0f,   delayRecurso);
        custoPedraSoldado               = Mathf.Max(0,    custoPedraSoldado);
        custoMadeiraSoldado             = Mathf.Max(0,    custoMadeiraSoldado);
        custoMetalSoldado               = Mathf.Max(0,    custoMetalSoldado);
        delaySoldado                    = Mathf.Max(0f,   delaySoldado);
        distanciaEntreUnidadesSpawn     = Mathf.Max(0f,   distanciaEntreUnidadesSpawn);
        quantidadePosicoesPorLinha      = Mathf.Max(1,    quantidadePosicoesPorLinha);
        alturaOffset                    = Mathf.Max(0f,   alturaOffset);
    }
}