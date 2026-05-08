using UnityEngine;

[DisallowMultipleComponent]
public class SoldadoSpownIA : MonoBehaviour
{
    [Header("Ponto de Spawn (OBRIGATÓRIO)")]
    [SerializeField] private Transform pontoSpawn;

    [Header("Guerreiro")]
    [SerializeField] private GameObject prefabGuerreiro;
    [SerializeField] private int custoPedraGuerreiro = 10;
    [SerializeField] private int custoMadeiraGuerreiro = 10;
    [SerializeField] private int custoMetalGuerreiro = 10;
    [SerializeField] private float delayGuerreiro = 1.2f;

    [Header("Coletor/Recurso")]
    [SerializeField] private GameObject prefabRecurso;
    [SerializeField] private int custoPedraRecurso = 10;
    [SerializeField] private int custoMadeiraRecurso = 10;
    [SerializeField] private int custoMetalRecurso = 10;
    [SerializeField] private float delayRecurso = 1.2f;

    [Header("Soldado")]
    [SerializeField] private GameObject prefabSoldado;
    [SerializeField] private int custoPedraSoldado = 10;
    [SerializeField] private int custoMadeiraSoldado = 10;
    [SerializeField] private int custoMetalSoldado = 10;
    [SerializeField] private float delaySoldado = 1.2f;

    [Header("Evitar nascer um em cima do outro")]
    [SerializeField] private bool usarEspacamentoEntreSpawns = true;
    [SerializeField] private float distanciaEntreUnidadesSpawn = 1.2f;
    [SerializeField] private int quantidadePosicoesPorLinha = 4;

    [Header("Ajuste de altura")]
    [SerializeField] private float alturaOffset = 0.5f;

    [Header("Time")]
    [SerializeField] private string tagDoTime = "Vermelho";

    [Header("Debug")]
    [SerializeField] private bool mostrarLogs = false;

    private float proximoSpawnGuerreiroPermitido;
    private float proximoSpawnRecursoPermitido;
    private float proximoSpawnSoldadoPermitido;

    private int contadorSpawns;

    // 🔥 CONTROLE DE POSIÇÃO FORÇADA (REVEZAMENTO ENTRE BASES)
    private Vector3 posicaoForcada;
    private Quaternion rotacaoForcada;
    private bool usarPosicaoForcada = false;

    // =====================================================================
    // COOLDOWN INDIVIDUAL
    // =====================================================================

    public bool GuerreiroEstaEmCooldown() => Time.time < proximoSpawnGuerreiroPermitido;
    public bool RecursoEstaEmCooldown() => Time.time < proximoSpawnRecursoPermitido;
    public bool ColetorEstaEmCooldown() => RecursoEstaEmCooldown();
    public bool SoldadoEstaEmCooldown() => Time.time < proximoSpawnSoldadoPermitido;

    public float TempoRestanteCooldownGuerreiro() => Mathf.Max(0f, proximoSpawnGuerreiroPermitido - Time.time);
    public float TempoRestanteCooldownRecurso() => Mathf.Max(0f, proximoSpawnRecursoPermitido - Time.time);
    public float TempoRestanteCooldownColetor() => TempoRestanteCooldownRecurso();
    public float TempoRestanteCooldownSoldado() => Mathf.Max(0f, proximoSpawnSoldadoPermitido - Time.time);

    public bool EstaEmCooldown() => GuerreiroEstaEmCooldown() || RecursoEstaEmCooldown() || SoldadoEstaEmCooldown();

    public float TempoRestanteCooldown()
    {
        float maior = TempoRestanteCooldownGuerreiro();
        maior = Mathf.Max(maior, TempoRestanteCooldownRecurso());
        maior = Mathf.Max(maior, TempoRestanteCooldownSoldado());
        return maior;
    }

    // =====================================================================
    // VALIDAÇÃO
    // =====================================================================

    public bool PodeCriarGuerreiro() => PodeCriarUnidade(prefabGuerreiro, custoPedraGuerreiro, custoMadeiraGuerreiro, custoMetalGuerreiro, proximoSpawnGuerreiroPermitido);
    public bool PodeCriarRecurso() => PodeCriarUnidade(prefabRecurso, custoPedraRecurso, custoMadeiraRecurso, custoMetalRecurso, proximoSpawnRecursoPermitido);
    public bool PodeCriarColetor() => PodeCriarRecurso();
    public bool PodeCriarSoldado() => PodeCriarUnidade(prefabSoldado, custoPedraSoldado, custoMadeiraSoldado, custoMetalSoldado, proximoSpawnSoldadoPermitido);

    // =====================================================================
    // TENTAR CRIAR (Retorna bool)
    // =====================================================================

    public bool TentarCriarGuerreiro()
    {
        if (!PodeCriarGuerreiro()) return false;
        CriarGuerreiro();
        return true;
    }

    public bool TentarCriarRecurso()
    {
        if (!PodeCriarRecurso()) return false;
        CriarRecurso();
        return true;
    }

    public bool TentarCriarColetor() => TentarCriarRecurso();

    public bool TentarCriarSoldado()
    {
        if (!PodeCriarSoldado()) return false;
        CriarSoldado();
        return true;
    }

    // =====================================================================
    // CRIAR UNIDADES
    // =====================================================================

    public void CriarGuerreiro()
    {
        CriarUnidade(prefabGuerreiro, custoPedraGuerreiro, custoMadeiraGuerreiro, custoMetalGuerreiro, delayGuerreiro, ref proximoSpawnGuerreiroPermitido);
    }

    public void CriarRecurso()
    {
        CriarUnidade(prefabRecurso, custoPedraRecurso, custoMadeiraRecurso, custoMetalRecurso, delayRecurso, ref proximoSpawnRecursoPermitido);
    }

    public void CriarColetor() => CriarRecurso();

    public void CriarSoldado()
    {
        CriarUnidade(prefabSoldado, custoPedraSoldado, custoMadeiraSoldado, custoMetalSoldado, delaySoldado, ref proximoSpawnSoldadoPermitido);
    }

    // =====================================================================
    // MÉTODOS GENÉRICOS (0=Guerreiro, 1=Recurso, 2=Soldado)
    // =====================================================================

    public void CriarPorIndice(int indice)
    {
        switch (indice)
        {
            case 0: CriarGuerreiro(); break;
            case 1: CriarRecurso(); break;
            case 2: CriarSoldado(); break;
        }
    }

    public bool TentarCriarPorIndice(int indice)
    {
        switch (indice)
        {
            case 0: return TentarCriarGuerreiro();
            case 1: return TentarCriarRecurso();
            case 2: return TentarCriarSoldado();
            default: return false;
        }
    }

    public bool PodeCriarPorIndice(int indice)
    {
        switch (indice)
        {
            case 0: return PodeCriarGuerreiro();
            case 1: return PodeCriarRecurso();
            case 2: return PodeCriarSoldado();
            default: return false;
        }
    }

    // =====================================================================
    // COMPATIBILIDADE (Aliases)
    // =====================================================================
    public void CriarUnidadePorIndice(int i) => CriarPorIndice(i);
    public void CriarPrefabPorIndice(int i) => CriarPorIndice(i);
    public void CriarObjetoPorIndice(int i) => CriarPorIndice(i);
    public void CriarUnidade(int i) => CriarPorIndice(i);
    public void CriarPrefab(int i) => CriarPorIndice(i);
    public void Criar(int i) => CriarPorIndice(i);

    public bool PodeCriarUnidadePorIndice(int i) => PodeCriarPorIndice(i);
    public bool PodeCriarPrefabPorIndice(int i) => PodeCriarPorIndice(i);
    public bool PodeCriarObjetoPorIndice(int i) => PodeCriarPorIndice(i);
    public bool PodeCriarUnidade(int i) => PodeCriarPorIndice(i);
    public bool PodeCriarPrefab(int i) => PodeCriarPorIndice(i);
    public bool PodeCriar(int i) => PodeCriarPorIndice(i);

    public bool ExistePrefabPorIndice(int i)
    {
        switch (i)
        {
            case 0: return prefabGuerreiro != null;
            case 1: return prefabRecurso != null;
            case 2: return prefabSoldado != null;
            default: return false;
        }
    }

    public bool TemPrefabPorIndice(int i) => ExistePrefabPorIndice(i);
    public bool ExisteUnidadePorIndice(int i) => ExistePrefabPorIndice(i);
    public bool TemUnidadePorIndice(int i) => ExistePrefabPorIndice(i);
    public bool IndiceExiste(int i) => ExistePrefabPorIndice(i);
    public bool ExisteIndice(int i) => ExistePrefabPorIndice(i);
    public int GetQuantidadePrefabs() => 3;
    public int QuantidadePrefabs() => 3;
    public int GetQuantidadeUnidades() => 3;
    public int QuantidadeUnidades() => 3;
    public int GetTotalPrefabs() => 3;
    public int TotalPrefabs() => 3;

    // =====================================================================
    // CÁLCULO DE POSIÇÃO (GRID NORMAL - QUANDO NÃO TEM FORÇAÇÃO)
    // =====================================================================

    private Vector3 CalcularPosicaoSpawn()
    {
        Transform origem = pontoSpawn != null ? pontoSpawn : transform;
        Vector3 posicao = origem.position;

        posicao.y += alturaOffset;

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

        if (direita.sqrMagnitude < 0.001f) direita = Vector3.right;
        if (frente.sqrMagnitude < 0.001f) frente = Vector3.forward;

        direita.Normalize();
        frente.Normalize();

        return posicao + direita * deslocamentoLateral + frente * deslocamentoFrente;
    }

    // =====================================================================
    // LÓGICA INTERNA
    // =====================================================================

    private bool PodeCriarUnidade(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal, float proximoSpawn)
    {
        if (prefab == null) return false;
        if (pontoSpawn == null && transform == null) return false;
        if (Time.time < proximoSpawn) return false;

        if (GameControllerRecursosIA.Instance == null)
        {
            if (mostrarLogs)
                Debug.LogWarning("[SoldadoSpownIA] GameControllerRecursosIA.Instance é null!");
            return false;
        }

        return GameControllerRecursosIA.Instance.TemRecursos(custoPedra, custoMadeira, custoMetal);
    }

    private bool CriarUnidade(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal, float delay, ref float proximoSpawnRef)
    {
        if (!PodeCriarUnidade(prefab, custoPedra, custoMadeira, custoMetal, proximoSpawnRef))
            return false;

        if (!GameControllerRecursosIA.Instance.TentarGastarRecursos(custoPedra, custoMadeira, custoMetal))
            return false;

        // 🔥 USA POSIÇÃO FORÇADA (REVEZAMENTO) OU CALCULADA (NORMAL)
        Vector3 posicaoSpawn;
        Quaternion rotacaoSpawn;

        if (usarPosicaoForcada)
        {
            posicaoSpawn = posicaoForcada;
            rotacaoSpawn = rotacaoForcada;
            usarPosicaoForcada = false; // Reseta para o próximo
        }
        else
        {
            posicaoSpawn = CalcularPosicaoSpawn();
            rotacaoSpawn = pontoSpawn != null ? pontoSpawn.rotation : transform.rotation;
        }

        GameObject obj = GameObject.Find("Clone Unidades IA");
        if (obj == null) obj = new GameObject("Clone Unidades IA");

        GameObject novo = Instantiate(prefab, posicaoSpawn, rotacaoSpawn, obj.transform);
        novo.SetActive(true);

        if (!string.IsNullOrWhiteSpace(tagDoTime))
        {
            try { novo.tag = tagDoTime; }
            catch { }
        }

        AtivarObjetoCompleto(novo);

        contadorSpawns++;
        proximoSpawnRef = Time.time + Mathf.Max(0f, delay);

        if (mostrarLogs)
            Debug.Log($"[SoldadoSpownIA] '{prefab.name}' criado em {posicaoSpawn} | Total: {contadorSpawns}");

        return true;
    }

    private void AtivarObjetoCompleto(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(true);

        foreach (var t in obj.GetComponentsInChildren<Transform>(true))
            t.gameObject.SetActive(true);

        foreach (var m in obj.GetComponentsInChildren<MonoBehaviour>(true))
            m.enabled = true;

        foreach (var c in obj.GetComponentsInChildren<Collider>(true))
            c.enabled = true;

        foreach (var r in obj.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
    }

    // =====================================================================
    // DEFINIÇÃO DE PONTO DE SPAWN
    // =====================================================================

    /// <summary>
    /// Define apenas a posição do ponto de spawn.
    /// </summary>
    public void DefinirPontoSpawn(Vector3 posicao)
    {
        if (pontoSpawn == null)
        {
            GameObject novo = new GameObject("PontoSpawnTemp_Soldado");
            pontoSpawn = novo.transform;
        }
        pontoSpawn.position = posicao;
    }

    /// <summary>
    /// Define a posição e rotação do ponto de spawn.
    /// </summary>
    public void DefinirPontoSpawn(Vector3 posicao, Quaternion rotacao)
    {
        if (pontoSpawn == null)
        {
            GameObject novo = new GameObject("PontoSpawnTemp_Soldado");
            pontoSpawn = novo.transform;
        }
        pontoSpawn.position = posicao;
        pontoSpawn.rotation = rotacao;
    }

    // =====================================================================
    // 🔥 FORÇA SPAWN EXATO (USADO PELO ROBOIA PARA REVEZAMENTO)
    // =====================================================================

    /// <summary>
    /// Força a próxima unidade a spawnar exatamente nesta posição/rotação.
    /// Chamado pelo RoboIA para fazer o revezamento entre bases.
    /// </summary>
    public void ForcarPosicaoSpawn(Vector3 posicao, Quaternion rotacao)
    {
        posicaoForcada = posicao;
        rotacaoForcada = rotacao;
        usarPosicaoForcada = true;
    }

    private void OnValidate()
    {
        custoPedraGuerreiro = Mathf.Max(0, custoPedraGuerreiro);
        custoMadeiraGuerreiro = Mathf.Max(0, custoMadeiraGuerreiro);
        custoMetalGuerreiro = Mathf.Max(0, custoMetalGuerreiro);
        delayGuerreiro = Mathf.Max(0f, delayGuerreiro);

        custoPedraRecurso = Mathf.Max(0, custoPedraRecurso);
        custoMadeiraRecurso = Mathf.Max(0, custoMadeiraRecurso);
        custoMetalRecurso = Mathf.Max(0, custoMetalRecurso);
        delayRecurso = Mathf.Max(0f, delayRecurso);

        custoPedraSoldado = Mathf.Max(0, custoPedraSoldado);
        custoMadeiraSoldado = Mathf.Max(0, custoMadeiraSoldado);
        custoMetalSoldado = Mathf.Max(0, custoMetalSoldado);
        delaySoldado = Mathf.Max(0f, delaySoldado);

        distanciaEntreUnidadesSpawn = Mathf.Max(0f, distanciaEntreUnidadesSpawn);
        quantidadePosicoesPorLinha = Mathf.Max(1, quantidadePosicoesPorLinha);
        alturaOffset = Mathf.Max(0f, alturaOffset);
    }

    // =====================================================================
    // 🔥 MÉTODOS QUE RECEBEM A BASE (ROBOIA PASSA A BASE)
    // =====================================================================

    public bool PodeCriarColetorNaBase(Transform baseTransform)
    {
        return PodeCriarNaBase(prefabRecurso, custoPedraRecurso, custoMadeiraRecurso, custoMetalRecurso, proximoSpawnRecursoPermitido, baseTransform);
    }

    public bool TentarCriarColetorNaBase(Transform baseTransform)
    {
        if (!PodeCriarColetorNaBase(baseTransform)) return false;
        CriarNaBase(prefabRecurso, custoPedraRecurso, custoMadeiraRecurso, custoMetalRecurso, delayRecurso, ref proximoSpawnRecursoPermitido, baseTransform);
        return true;
    }

    public bool PodeCriarSoldadoNaBase(Transform baseTransform)
    {
        return PodeCriarNaBase(prefabSoldado, custoPedraSoldado, custoMadeiraSoldado, custoMetalSoldado, proximoSpawnSoldadoPermitido, baseTransform);
    }

    public bool TentarCriarSoldadoNaBase(Transform baseTransform)
    {
        if (!PodeCriarSoldadoNaBase(baseTransform)) return false;
        CriarNaBase(prefabSoldado, custoPedraSoldado, custoMadeiraSoldado, custoMetalSoldado, delaySoldado, ref proximoSpawnSoldadoPermitido, baseTransform);
        return true;
    }

    public bool PodeCriarGuerreiroNaBase(Transform baseTransform)
    {
        return PodeCriarNaBase(prefabGuerreiro, custoPedraGuerreiro, custoMadeiraGuerreiro, custoMetalGuerreiro, proximoSpawnGuerreiroPermitido, baseTransform);
    }

    public bool TentarCriarGuerreiroNaBase(Transform baseTransform)
    {
        if (!PodeCriarGuerreiroNaBase(baseTransform)) return false;
        CriarNaBase(prefabGuerreiro, custoPedraGuerreiro, custoMadeiraGuerreiro, custoMetalGuerreiro, delayGuerreiro, ref proximoSpawnGuerreiroPermitido, baseTransform);
        return true;
    }

    private bool PodeCriarNaBase(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal, float proximoSpawn, Transform baseTransform)
    {
        if (prefab == null) return false;
        if (baseTransform == null) return false;
        if (Time.time < proximoSpawn) return false;

        if (GameControllerRecursosIA.Instance == null)
        {
            if (mostrarLogs) Debug.LogWarning("[SoldadoSpownIA] GameControllerRecursosIA.Instance é null!");
            return false;
        }

        return GameControllerRecursosIA.Instance.TemRecursos(custoPedra, custoMadeira, custoMetal);
    }

    private void CriarNaBase(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal, float delay, ref float proximoSpawnRef, Transform baseTransform)
    {
        if (!PodeCriarNaBase(prefab, custoPedra, custoMadeira, custoMetal, proximoSpawnRef, baseTransform))
            return;

        if (!GameControllerRecursosIA.Instance.TentarGastarRecursos(custoPedra, custoMadeira, custoMetal))
            return;

        // 🔥 SPAWN EXATAMENTE NA POSIÇÃO DA BASE RECEBIDA
        Vector3 posicaoSpawn = baseTransform.position;
        Quaternion rotacaoSpawn = baseTransform.rotation;

        GameObject obj = GameObject.Find("Clone Unidades IA");
        if (obj == null) obj = new GameObject("Clone Unidades IA");

        GameObject novo = Instantiate(prefab, posicaoSpawn, rotacaoSpawn, obj.transform);
        novo.SetActive(true);

        if (!string.IsNullOrWhiteSpace(tagDoTime))
        {
            try { novo.tag = tagDoTime; }
            catch { }
        }

        AtivarObjetoCompleto(novo);

        contadorSpawns++;
        proximoSpawnRef = Time.time + Mathf.Max(0f, delay);

        if (mostrarLogs)
            Debug.Log($"[SoldadoSpownIA] '{prefab.name}' spawnado na base {baseTransform.name} em {posicaoSpawn}");
    }
}