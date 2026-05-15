using UnityEngine;

/// <summary>
/// SISTEMA DE ARMAMENTO DO AVIÃO.
///
/// Fluxo:
///   1. AviaoControler ativa este componente junto com AviaoVisao (fase Patrulha).
///   2. AviaoVisao filtra por CONE FRONTAL — só reporta alvo quando o inimigo está à frente.
///   3. Assim que AviaoVisao.AlvoAtual != null, este script dispara:
///        • Míssil       — um por vez, com cooldown, em ordem de slot 0→7.
///        • Metralhadora — disparada continuamente na cadência configurada.
///   4. O míssil recebe Lancar(alvo, transformDoAviao) — tipado, sem SendMessage.
///        • Dentro do Lancar, o míssil se desparenta e vira agente autônomo.
///        • O avião de origem é passado para o míssil ignorá-lo em colisões e dano.
///
/// Componente PASSIVO — não se auto-inicializa.
/// O AviaoControler é responsável por habilitar este componente.
/// </summary>
[DisallowMultipleComponent]
public class AviaoAtaque : MonoBehaviour
{
    // =====================================================================
    // INSPECTOR — REFERÊNCIAS
    // =====================================================================

    [Header("Referência da visão")]
    [SerializeField] private AviaoVisao aviaoVisao;

    // =====================================================================
    // INSPECTOR — METRALHADORA
    // =====================================================================

    [Header("Metralhadora — 2 canos")]
    [SerializeField] private Transform  spawnMetralhadora1;
    [SerializeField] private Transform  spawnMetralhadora2;
    [SerializeField] private GameObject prefabBalaMetralhadora;
    [SerializeField] private float      intervaloMetralhadora  = 0.12f;
    [SerializeField] private int        danoMetralhadora       = 5;
    [SerializeField] private float      velocidadeMetralhadora = 400f;
    [Tooltip("Ângulo máximo em graus entre o nariz do avião e o alvo para autorizar o disparo.\nMenor = mais preciso, dispara menos. Recomendado: 5-8°.")]
    [SerializeField] private float      anguloMiraMetralhadora = 6f;

    // =====================================================================
    // INSPECTOR — MÍSSEIS
    // =====================================================================

    [Header("Mísseis — slots acoplados ao avião")]
    [Tooltip("GameObjects de mísseis já posicionados no avião (até 8 slots). " +
             "Devem ter o componente Missel.")]
    [SerializeField] private GameObject[] slotsMisseis = new GameObject[8];
    [SerializeField] private float        intervaloMissel = 2f;

    // =====================================================================
    // INSPECTOR — FILTRO MÍSSIL
    // =====================================================================

    [Header("Filtro míssil")]
    [Tooltip("Tags do PAI que confirmam que o objeto é inimigo")]
    [SerializeField] private string[] tagsInimigoPai = { "Vermelho" };
    [Tooltip("Tags do FILHO que indicam unidade pequena — só metralhadora, sem míssil")]
    [SerializeField] private string[] tagsFilhoApenasMetralhadora = { "Soldado", "Guerreiro", "Coletor" };

    // =====================================================================
    // INSPECTOR — DEBUG
    // =====================================================================

    [Header("Debug — somente leitura")]
    [SerializeField] private bool desenharGizmosNoEditor = true;
    [SerializeField] private int  misseisRestantes       = 0;

    // =====================================================================
    // PRIVADOS
    // =====================================================================

    private Transform alvoAtual;
    private float     proximoTiroMetralhadora;
    private float     proximoLancamentoMissel;
    private int       indexMetralhadoraAtual = 0;

    // =====================================================================
    // PROPRIEDADES PÚBLICAS
    // =====================================================================

    public int  GetMisseisRestantes() => misseisRestantes;
    public bool TemMisseis()          => misseisRestantes > 0;
    public bool TemAlvo()             => alvoAtual != null;

    // =====================================================================
    // AWAKE
    // =====================================================================

    private void Awake()
    {
        if (aviaoVisao == null)
            aviaoVisao = GetComponent<AviaoVisao>();

        misseisRestantes = ContarMisseisDisponiveis();
    }

    // =====================================================================
    // OnEnable — AviaoControler ativa junto com AviaoVisao (fase Patrulha)
    // =====================================================================

    private void OnEnable()
    {
        // Cooldown de segurança ao ativar — evita rajada imediata
        proximoTiroMetralhadora = Time.time + 1f;
        proximoLancamentoMissel = Time.time + 1f;
        alvoAtual               = null;

        Debug.Log($"[AviaoAtaque] {gameObject.name} — ATIVADO. Mísseis: {misseisRestantes}", this);
    }

    // =====================================================================
    // UPDATE
    // =====================================================================

    private void Update()
    {
        // Obtém o alvo direto do AviaoVisao.
        // O cone frontal já foi aplicado lá — se há alvo, está à frente do avião.
        alvoAtual = (aviaoVisao != null && aviaoVisao.TemAlvo) ? aviaoVisao.AlvoAtual : null;

        if (alvoAtual == null) return;

        // ── Míssil ────────────────────────────────────────────────────────
        if (misseisRestantes > 0 && PodeUsarMissel(alvoAtual))
            TentarLancarMissel();

        // ── Metralhadora ──────────────────────────────────────────────────
        TentarAtirarMetralhadora();
    }

    // =====================================================================
    // METRALHADORA
    // =====================================================================

    private void TentarAtirarMetralhadora()
    {
        if (Time.time < proximoTiroMetralhadora) return;

        // LOCK-ON: só dispara se o nariz do avião estiver apontado para o alvo
        // dentro da tolerância configurada. O AviaoVisao gira o avião em direção
        // ao alvo no estado EmAtaque — aguardamos esse alinhamento antes de atirar.
        if (!NaLinhaDeAtiro()) return;

        Transform spawn = ObterSpawnMetralhadoraAtual();
        if (spawn == null || prefabBalaMetralhadora == null) return;

        // spawn.rotation: projétil nasce alinhado com o cano.
        // Configurar() define a direção final, ignorando qualquer pivot incorreto no prefab.
        GameObject balaGO = Instantiate(prefabBalaMetralhadora, spawn.position, spawn.rotation);

        ProjetilDistancia projetil = balaGO.GetComponent<ProjetilDistancia>();
        if (projetil != null)
            projetil.Configurar(alvoAtual, danoMetralhadora, velocidadeMetralhadora);

        indexMetralhadoraAtual  = (indexMetralhadoraAtual + 1) % 2;
        proximoTiroMetralhadora = Time.time + Mathf.Max(0.02f, intervaloMetralhadora);
    }

    /// <summary>
    /// Retorna true se o nariz do avião está apontado para o alvo
    /// dentro de anguloMiraMetralhadora graus.
    /// Usa AnguloParaAlvo do AviaoVisao (calculado a partir da origem da visão / nariz do avião).
    /// </summary>
    private bool NaLinhaDeAtiro()
    {
        if (alvoAtual == null) return false;

        if (aviaoVisao != null)
            return aviaoVisao.AnguloParaAlvo <= anguloMiraMetralhadora;

        // Fallback caso AviaoVisao não esteja disponível
        Vector3 dir    = (alvoAtual.position - transform.position).normalized;
        float   angulo = Vector3.Angle(transform.forward, dir);
        return angulo <= anguloMiraMetralhadora;
    }

    private Transform ObterSpawnMetralhadoraAtual()
    {
        if (indexMetralhadoraAtual == 0)
            return spawnMetralhadora1 != null ? spawnMetralhadora1 : spawnMetralhadora2;
        return spawnMetralhadora2 != null ? spawnMetralhadora2 : spawnMetralhadora1;
    }

    // =====================================================================
    // MÍSSIL
    // =====================================================================

    private void TentarLancarMissel()
    {
        if (Time.time < proximoLancamentoMissel) return;

        int slotIndex = EncontrarProximoSlotDisponivel();
        if (slotIndex < 0) return;

        GameObject go = slotsMisseis[slotIndex];
        if (go == null) { slotsMisseis[slotIndex] = null; return; }

        Missel misselScript = go.GetComponent<Missel>();
        if (misselScript == null)
        {
            Debug.LogWarning($"[AviaoAtaque] Slot {slotIndex} não tem componente Missel!", this);
            slotsMisseis[slotIndex] = null;
            misseisRestantes--;
            return;
        }

        // 1. Desparenta ANTES de Lancar — a partir daqui o míssil não segue mais o avião
        go.transform.SetParent(null);

        // 2. Ativa o míssil passando o alvo E o Transform deste avião
        //    O míssil usará o Transform do avião para nunca colidir ou causar dano nele
        misselScript.Lancar(alvoAtual, transform);

        // 3. Remove o slot e contabiliza
        slotsMisseis[slotIndex] = null;
        misseisRestantes--;

        proximoLancamentoMissel = Time.time + Mathf.Max(0.1f, intervaloMissel);

        Debug.Log($"[AviaoAtaque] {gameObject.name} — míssil slot {slotIndex} lançado! " +
                  $"Restantes: {misseisRestantes}", this);
    }

    /// <summary>
    /// Percorre os slots em ordem crescente (0→7) e retorna o próximo disponível.
    /// Garante disparo sequencial — nunca dois mísseis ao mesmo tempo.
    /// </summary>
    private int EncontrarProximoSlotDisponivel()
    {
        for (int i = 0; i < slotsMisseis.Length; i++)
            if (slotsMisseis[i] != null) return i;
        return -1;
    }

    private int ContarMisseisDisponiveis()
    {
        int count = 0;
        foreach (GameObject slot in slotsMisseis)
            if (slot != null) count++;
        return count;
    }

    // =====================================================================
    // FILTRO DE MÍSSIL
    // =====================================================================

    private bool PodeUsarMissel(Transform alvo)
    {
        if (alvo == null) return true;
        if (!ObjetoOuAncestralTemTag(alvo, tagsInimigoPai)) return true;
        return !FilhoTemTag(alvo, tagsFilhoApenasMetralhadora);
    }

    private bool ObjetoOuAncestralTemTag(Transform alvo, string[] tags)
    {
        if (alvo == null || tags == null) return false;
        Transform atual = alvo;
        while (atual != null)
        {
            foreach (string tag in tags)
                if (!string.IsNullOrWhiteSpace(tag) && atual.gameObject.CompareTag(tag))
                    return true;
            atual = atual.parent;
        }
        return false;
    }

    private bool FilhoTemTag(Transform alvo, string[] tags)
    {
        if (alvo == null || tags == null) return false;
        Transform raiz = alvo;
        while (raiz.parent != null) raiz = raiz.parent;
        foreach (Transform filho in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (filho == raiz) continue;
            foreach (string tag in tags)
                if (!string.IsNullOrWhiteSpace(tag) && filho.gameObject.CompareTag(tag))
                    return true;
        }
        return false;
    }

    // =====================================================================
    // AUXILIARES
    // =====================================================================

    private Vector3 ObterPontoMira(Transform alvo)
    {
        if (alvo == null) return transform.position + transform.forward * 10f;
        Collider col = alvo.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.center;
        return alvo.position;
    }

    // =====================================================================
    // VALIDAÇÃO
    // =====================================================================

    private void OnValidate()
    {
        intervaloMetralhadora  = Mathf.Max(0.02f, intervaloMetralhadora);
        intervaloMissel        = Mathf.Max(0.1f,  intervaloMissel);
        anguloMiraMetralhadora = Mathf.Clamp(anguloMiraMetralhadora, 1f, 45f);
        danoMetralhadora       = Mathf.Max(0, danoMetralhadora);
        velocidadeMetralhadora = Mathf.Max(1f, velocidadeMetralhadora);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        if (!desenharGizmosNoEditor) return;

        // Slots de mísseis ainda acoplados
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        foreach (GameObject slot in slotsMisseis)
            if (slot != null)
                Gizmos.DrawSphere(slot.transform.position, 0.2f);

        // Linha até o alvo
        if (alvoAtual != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, ObterPontoMira(alvoAtual));
            Gizmos.DrawSphere(ObterPontoMira(alvoAtual), 0.5f);
        }

        // Spawns da metralhadora
        if (spawnMetralhadora1 != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnMetralhadora1.position,
                            spawnMetralhadora1.position + transform.forward * 4f);
        }
        if (spawnMetralhadora2 != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnMetralhadora2.position,
                            spawnMetralhadora2.position + transform.forward * 4f);
        }

        // Cone de mira da metralhadora
        // Verde = nariz alinhado com o alvo (dentro de anguloMiraMetralhadora)
        // Vermelho = fora de mira — avião ainda está girando para alinhar
        bool alinhado = Application.isPlaying && aviaoVisao != null
            && aviaoVisao.AnguloParaAlvo <= anguloMiraMetralhadora;
        Gizmos.color = alinhado ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0.2f, 0f, 0.15f);

        float   comp  = 35f;
        float   raio  = comp * Mathf.Tan(anguloMiraMetralhadora * Mathf.Deg2Rad);
        Vector3 ponta = transform.position + transform.forward * comp;
        Vector3 r     = transform.right * raio;
        Vector3 u     = transform.up   * raio;

        Gizmos.DrawLine(transform.position, ponta + r + u);
        Gizmos.DrawLine(transform.position, ponta - r + u);
        Gizmos.DrawLine(transform.position, ponta + r - u);
        Gizmos.DrawLine(transform.position, ponta - r - u);
        Gizmos.DrawLine(ponta + r + u, ponta - r + u);
        Gizmos.DrawLine(ponta - r + u, ponta - r - u);
        Gizmos.DrawLine(ponta - r - u, ponta + r - u);
        Gizmos.DrawLine(ponta + r - u, ponta + r + u);
    }
}