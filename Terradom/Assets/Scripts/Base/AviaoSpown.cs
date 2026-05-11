using UnityEngine;

[DisallowMultipleComponent]
public class AviaoSpown : MonoBehaviour
{
    [Header("Ponto onde o avião vai nascer")]
    [SerializeField] private Transform pontoSpawn;

    [Header("Prefab Avião Leve")]
    [SerializeField] private GameObject prefabAviaoLeve;

    [Header("Custo Avião Leve")]
    [SerializeField] private int custoPedraAviaoLeve = 10;
    [SerializeField] private int custoMadeiraAviaoLeve = 10;
    [SerializeField] private int custoMetalAviaoLeve = 10;

    [Header("Delay entre spawns")]
    [SerializeField] private float tempoEntreSpawns = 1.8f;

    [Header("Evitar nascer um em cima do outro")]
    [SerializeField] private bool usarEspacamentoEntreSpawns = true;
    [SerializeField] private float distanciaEntreUnidadesSpawn = 2.2f;
    [SerializeField] private int quantidadePosicoesPorLinha = 3;

    [Header("Organizacao")]
    [SerializeField] private bool manterMesmoPaiDaBase = false;

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
        return PodeCriarUnidade(prefabAviaoLeve, custoPedraAviaoLeve, custoMadeiraAviaoLeve, custoMetalAviaoLeve);
    }

    public void CriarAviao()
    {
        CriarUnidade(
            prefabAviaoLeve,
            custoPedraAviaoLeve,
            custoMadeiraAviaoLeve,
            custoMetalAviaoLeve
        );
    }

    public bool PodeCriarVeiculo()
    {
        return PodeCriarAviao();
    }

    public void CriarVeiculo()
    {
        CriarAviao();
    }

    // =====================================================================
    // METODOS GENERICOS PARA OS BOTOES
    // 0 = Avião Leve
    // =====================================================================

    public bool PodeCriarPorIndice(int indice)
    {
        if (indice != 0)
            return false;

        return PodeCriarAviao();
    }

    public bool PodeCriarUnidadePorIndice(int indice)  => PodeCriarPorIndice(indice);
    public bool PodeCriarPrefabPorIndice(int indice)   => PodeCriarPorIndice(indice);
    public bool PodeCriarObjetoPorIndice(int indice)   => PodeCriarPorIndice(indice);
    public bool PodeCriarUnidade(int indice)           => PodeCriarPorIndice(indice);
    public bool PodeCriarPrefab(int indice)            => PodeCriarPorIndice(indice);
    public bool PodeCriar(int indice)                  => PodeCriarPorIndice(indice);

    public void CriarPorIndice(int indice)
    {
        if (indice != 0)
            return;

        CriarAviao();
    }

    public void CriarUnidadePorIndice(int indice)  => CriarPorIndice(indice);
    public void CriarPrefabPorIndice(int indice)   => CriarPorIndice(indice);
    public void CriarObjetoPorIndice(int indice)   => CriarPorIndice(indice);
    public void CriarUnidade(int indice)           => CriarPorIndice(indice);
    public void CriarPrefab(int indice)            => CriarPorIndice(indice);
    public void Criar(int indice)                  => CriarPorIndice(indice);

    public bool ExistePrefabPorIndice(int indice)  => indice == 0 && prefabAviaoLeve != null;
    public bool TemPrefabPorIndice(int indice)     => ExistePrefabPorIndice(indice);
    public bool ExisteUnidadePorIndice(int indice) => ExistePrefabPorIndice(indice);
    public bool TemUnidadePorIndice(int indice)    => ExistePrefabPorIndice(indice);
    public bool IndiceExiste(int indice)           => ExistePrefabPorIndice(indice);
    public bool ExisteIndice(int indice)           => ExistePrefabPorIndice(indice);

    public int GetQuantidadePrefabs()  => prefabAviaoLeve != null ? 1 : 0;
    public int QuantidadePrefabs()     => GetQuantidadePrefabs();
    public int GetQuantidadeUnidades() => GetQuantidadePrefabs();
    public int QuantidadeUnidades()    => GetQuantidadePrefabs();
    public int GetTotalPrefabs()       => GetQuantidadePrefabs();
    public int TotalPrefabs()          => GetQuantidadePrefabs();

    // =====================================================================
    // LOGICA INTERNA
    // =====================================================================

    private bool PodeCriarUnidade(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal)
    {
        if (prefab == null || pontoSpawn == null)
            return false;

        if (EstaEmCooldown())
            return false;

        if (GameControllerRecursos.Instance == null)
            return false;

        return GameControllerRecursos.Instance.TemRecursos(custoPedra, custoMadeira, custoMetal);
    }

    private void CriarUnidade(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal)
    {
        if (!PodeCriarUnidade(prefab, custoPedra, custoMadeira, custoMetal))
            return;

        bool gastou = GameControllerRecursos.Instance.TentarGastarRecursos(custoPedra, custoMadeira, custoMetal);

        if (!gastou)
            return;

        Vector3 posicaoSpawn = CalcularPosicaoSpawn();
        Quaternion rotacaoSpawn = pontoSpawn != null ? pontoSpawn.rotation : transform.rotation;

        GameObject novo = Instantiate(prefab, posicaoSpawn, rotacaoSpawn);

        novo.SetActive(true);
        AtivarObjetoCompleto(novo);

        if (manterMesmoPaiDaBase && transform.parent != null)
            novo.transform.SetParent(transform.parent);

        contadorSpawns++;
        proximoSpawnPermitido = Time.time + Mathf.Max(0f, tempoEntreSpawns);

        BotoesProducaoUnidades.AtualizarTodos();
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

        if (direita.sqrMagnitude < 0.001f) direita = Vector3.right;
        if (frente.sqrMagnitude < 0.001f)  frente  = Vector3.forward;

        direita.Normalize();
        frente.Normalize();

        return posicao + direita * deslocamentoLateral + frente * deslocamentoFrente;
    }

    private void AtivarObjetoCompleto(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(true);

        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
            if (t != null) t.gameObject.SetActive(true);

        foreach (MonoBehaviour s in obj.GetComponentsInChildren<MonoBehaviour>(true))
            if (s != null) s.enabled = true;

        foreach (Collider c in obj.GetComponentsInChildren<Collider>(true))
            if (c != null) c.enabled = true;

        foreach (Renderer r in obj.GetComponentsInChildren<Renderer>(true))
            if (r != null) r.enabled = true;
    }

    private void OnValidate()
    {
        custoPedraAviaoLeve    = Mathf.Max(0, custoPedraAviaoLeve);
        custoMadeiraAviaoLeve  = Mathf.Max(0, custoMadeiraAviaoLeve);
        custoMetalAviaoLeve    = Mathf.Max(0, custoMetalAviaoLeve);

        tempoEntreSpawns              = Mathf.Max(0f, tempoEntreSpawns);
        distanciaEntreUnidadesSpawn   = Mathf.Max(0f, distanciaEntreUnidadesSpawn);
        quantidadePosicoesPorLinha    = Mathf.Max(1, quantidadePosicoesPorLinha);
    }
}