using UnityEngine;

[DisallowMultipleComponent]
public class SoldadoSpown : MonoBehaviour
{
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

    [Header("Ponto onde a unidade vai nascer")]
    [SerializeField] private Transform pontoSpawn;

    [Header("Evitar nascer um em cima do outro")]
    [SerializeField] private bool usarEspacamentoEntreSpawns = true;
    [SerializeField] private float distanciaEntreUnidadesSpawn = 1.2f;
    [SerializeField] private int quantidadePosicoesPorLinha = 4;

    [Header("Organizacao")]
    [SerializeField] private bool manterMesmoPaiDaBase = false;

    private float proximoSpawnGuerreiroPermitido;
    private float proximoSpawnRecursoPermitido;
    private float proximoSpawnSoldadoPermitido;

    private int contadorSpawns;

    // =====================================================================
    // COOLDOWN INDIVIDUAL
    // =====================================================================

    public bool GuerreiroEstaEmCooldown()
    {
        return Time.time < proximoSpawnGuerreiroPermitido;
    }

    public bool RecursoEstaEmCooldown()
    {
        return Time.time < proximoSpawnRecursoPermitido;
    }

    public bool ColetorEstaEmCooldown()
    {
        return RecursoEstaEmCooldown();
    }

    public bool SoldadoEstaEmCooldown()
    {
        return Time.time < proximoSpawnSoldadoPermitido;
    }

    public float TempoRestanteCooldownGuerreiro()
    {
        return Mathf.Max(0f, proximoSpawnGuerreiroPermitido - Time.time);
    }

    public float TempoRestanteCooldownRecurso()
    {
        return Mathf.Max(0f, proximoSpawnRecursoPermitido - Time.time);
    }

    public float TempoRestanteCooldownColetor()
    {
        return TempoRestanteCooldownRecurso();
    }

    public float TempoRestanteCooldownSoldado()
    {
        return Mathf.Max(0f, proximoSpawnSoldadoPermitido - Time.time);
    }

    // Compatibilidade com versoes antigas: retorna true se qualquer unidade estiver em cooldown.
    public bool EstaEmCooldown()
    {
        return GuerreiroEstaEmCooldown() || RecursoEstaEmCooldown() || SoldadoEstaEmCooldown();
    }

    public float TempoRestanteCooldown()
    {
        float maiorTempo = TempoRestanteCooldownGuerreiro();
        maiorTempo = Mathf.Max(maiorTempo, TempoRestanteCooldownRecurso());
        maiorTempo = Mathf.Max(maiorTempo, TempoRestanteCooldownSoldado());
        return maiorTempo;
    }

    public bool EstaEmCooldownPorIndice(int indice)
    {
        switch (indice)
        {
            case 0:
                return GuerreiroEstaEmCooldown();

            case 1:
                return RecursoEstaEmCooldown();

            case 2:
                return SoldadoEstaEmCooldown();

            default:
                return false;
        }
    }

    public float TempoRestanteCooldownPorIndice(int indice)
    {
        switch (indice)
        {
            case 0:
                return TempoRestanteCooldownGuerreiro();

            case 1:
                return TempoRestanteCooldownRecurso();

            case 2:
                return TempoRestanteCooldownSoldado();

            default:
                return 0f;
        }
    }

    // =====================================================================
    // VALIDACAO DOS BOTOES
    // =====================================================================

    public bool PodeCriarGuerreiro()
    {
        return PodeCriarUnidade(
            prefabGuerreiro,
            custoPedraGuerreiro,
            custoMadeiraGuerreiro,
            custoMetalGuerreiro,
            proximoSpawnGuerreiroPermitido
        );
    }

    public bool PodeCriarRecurso()
    {
        return PodeCriarUnidade(
            prefabRecurso,
            custoPedraRecurso,
            custoMadeiraRecurso,
            custoMetalRecurso,
            proximoSpawnRecursoPermitido
        );
    }

    public bool PodeCriarColetor()
    {
        return PodeCriarRecurso();
    }

    public bool PodeCriarSoldado()
    {
        return PodeCriarUnidade(
            prefabSoldado,
            custoPedraSoldado,
            custoMadeiraSoldado,
            custoMetalSoldado,
            proximoSpawnSoldadoPermitido
        );
    }

    // =====================================================================
    // CRIACAO DAS UNIDADES
    // =====================================================================

    public void CriarGuerreiro()
    {
        if (CriarUnidade(
            prefabGuerreiro,
            custoPedraGuerreiro,
            custoMadeiraGuerreiro,
            custoMetalGuerreiro,
            proximoSpawnGuerreiroPermitido,
            out float proximoSpawn
        ))
        {
            proximoSpawnGuerreiroPermitido = proximoSpawn;
        }
    }

    public void CriarRecurso()
    {
        if (CriarUnidade(
            prefabRecurso,
            custoPedraRecurso,
            custoMadeiraRecurso,
            custoMetalRecurso,
            proximoSpawnRecursoPermitido,
            out float proximoSpawn
        ))
        {
            proximoSpawnRecursoPermitido = proximoSpawn;
        }
    }

    public void CriarColetor()
    {
        CriarRecurso();
    }

    public void CriarSoldado()
    {
        if (CriarUnidade(
            prefabSoldado,
            custoPedraSoldado,
            custoMadeiraSoldado,
            custoMetalSoldado,
            proximoSpawnSoldadoPermitido,
            out float proximoSpawn
        ))
        {
            proximoSpawnSoldadoPermitido = proximoSpawn;
        }
    }

    // =====================================================================
    // METODOS GENERICOS PARA OS BOTOES
    // 0 = Guerreiro, 1 = Recurso/Coletor, 2 = Soldado
    // =====================================================================

    public void CriarPorIndice(int indice)
    {
        switch (indice)
        {
            case 0:
                CriarGuerreiro();
                break;

            case 1:
                CriarRecurso();
                break;

            case 2:
                CriarSoldado();
                break;
        }
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

    public bool PodeCriarPorIndice(int indice)
    {
        switch (indice)
        {
            case 0:
                return PodeCriarGuerreiro();

            case 1:
                return PodeCriarRecurso();

            case 2:
                return PodeCriarSoldado();

            default:
                return false;
        }
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

    public bool ExistePrefabPorIndice(int indice)
    {
        switch (indice)
        {
            case 0:
                return prefabGuerreiro != null;

            case 1:
                return prefabRecurso != null;

            case 2:
                return prefabSoldado != null;

            default:
                return false;
        }
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
        return 3;
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
    // LOGICA INTERNA
    // =====================================================================

    private bool PodeCriarUnidade(
        GameObject prefab,
        int custoPedra,
        int custoMadeira,
        int custoMetal,
        float proximoSpawnPermitido
    )
    {
        if (prefab == null || pontoSpawn == null)
            return false;

        if (Time.time < proximoSpawnPermitido)
            return false;

        if (GameControllerRecursos.Instance == null)
            return false;

        return GameControllerRecursos.Instance.TemRecursos(
            custoPedra,
            custoMadeira,
            custoMetal
        );
    }

    private bool CriarUnidade(
        GameObject prefab,
        int custoPedra,
        int custoMadeira,
        int custoMetal,
        float proximoSpawnPermitido,
        out float novoProximoSpawnPermitido
    )
    {
        novoProximoSpawnPermitido = proximoSpawnPermitido;

        if (!PodeCriarUnidade(prefab, custoPedra, custoMadeira, custoMetal, proximoSpawnPermitido))
            return false;

        bool gastou = GameControllerRecursos.Instance.TentarGastarRecursos(
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
        AtivarObjetoCompleto(novo);

        if (manterMesmoPaiDaBase && transform.parent != null)
            novo.transform.SetParent(transform.parent);

        contadorSpawns++;
        novoProximoSpawnPermitido = Time.time + ObterDelayDoPrefab(prefab);

        BotoesProducaoUnidades.AtualizarTodos();
        return true;
    }

    private float ObterDelayDoPrefab(GameObject prefab)
    {
        if (prefab == prefabGuerreiro)
            return Mathf.Max(0f, delayGuerreiro);

        if (prefab == prefabRecurso)
            return Mathf.Max(0f, delayRecurso);

        if (prefab == prefabSoldado)
            return Mathf.Max(0f, delaySoldado);

        return 0f;
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
    }
}
