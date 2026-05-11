using UnityEngine;

/// <summary>
/// SISTEMA DE ARMAMENTO DO AVIÃO.
///
/// Fluxo simplificado e direto:
///   1. AviaoControler ativa este componente junto com AviaoVisao (fase Patrulha).
///   2. AviaoVisao já faz a filtragem por CONE FRONTAL — só reporta alvo
///      quando o inimigo está à frente do avião.
///   3. Assim que AviaoVisao.AlvoAtual != null, este script dispara:
///        • Míssil  — lançado com cooldown, enquanto houver estoque.
///        • Metralhadora — disparada continuamente na cadência configurada.
///   4. Sem cone de mira secundário — a detecção do AviaoVisao já é a mira.
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
    [SerializeField] private float      intervaloMetralhadora = 0.12f;

    // =====================================================================
    // INSPECTOR — MÍSSEIS
    // =====================================================================

    [Header("Mísseis — slots acoplados ao avião")]
    [Tooltip("Os GameObjects de mísseis já posicionados no avião (até 8 slots)")]
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
    [SerializeField] private int  misseisRestantes       = 8;

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

        Debug.Log($"[AviaoAtaque] {gameObject.name} — ATIVADO.", this);
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
        if (PodeUsarMissel(alvoAtual) && misseisRestantes > 0)
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

        Transform spawn = ObterSpawnMetralhadoraAtual();
        if (spawn == null || prefabBalaMetralhadora == null) return;

        // Dispara na direção do centro de massa do alvo
        Vector3    pontoMira = ObterPontoMira(alvoAtual);
        Vector3    direcao   = (pontoMira - spawn.position).normalized;
        Quaternion rot       = Quaternion.LookRotation(direcao, Vector3.up);

        Instantiate(prefabBalaMetralhadora, spawn.position, rot);

        indexMetralhadoraAtual  = (indexMetralhadoraAtual + 1) % 2;
        proximoTiroMetralhadora = Time.time + Mathf.Max(0.02f, intervaloMetralhadora);
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

        int slotIndex = EncontrarSlotMisselDisponivel();
        if (slotIndex < 0) return;

        GameObject missel = slotsMisseis[slotIndex];
        missel.transform.SetParent(null);
        missel.SendMessage("Lancar", alvoAtual, SendMessageOptions.DontRequireReceiver);

        slotsMisseis[slotIndex] = null;
        misseisRestantes--;

        proximoLancamentoMissel = Time.time + Mathf.Max(0.1f, intervaloMissel);

        Debug.Log($"[AviaoAtaque] {gameObject.name} — míssil lançado! Restantes: {misseisRestantes}", this);
    }

    private int EncontrarSlotMisselDisponivel()
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
        intervaloMetralhadora = Mathf.Max(0.02f, intervaloMetralhadora);
        intervaloMissel       = Mathf.Max(0.1f,  intervaloMissel);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        if (!desenharGizmosNoEditor) return;

        // Slots de mísseis
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
    }
}