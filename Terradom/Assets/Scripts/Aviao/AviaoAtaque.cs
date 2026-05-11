using UnityEngine;

/// <summary>
/// Sistema de armamento do avião.
/// Componente PASSIVO — não se auto-inicializa.
/// O AviaoControler é responsável por habilitar este componente
/// apenas quando o avião atingir altitude segura.
/// </summary>
[DisallowMultipleComponent]
public class AviaoAtaque : MonoBehaviour
{
    // =====================================================================
    // INSPECTOR
    // =====================================================================

    [Header("Referência da visão")]
    [SerializeField] private AviaoVisao aviaoVisao;

    [Header("Metralhadora — 2 canos")]
    [SerializeField] private Transform  spawnMetralhadora1;
    [SerializeField] private Transform  spawnMetralhadora2;
    [SerializeField] private GameObject prefabBalaMetralhadora;
    [SerializeField] private float      intervaloMetralhadora = 0.12f;

    [Header("Mísseis — slots acoplados ao avião")]
    [SerializeField] private GameObject[] slotsMisseis = new GameObject[8];
    [SerializeField] private float        intervaloMissel = 2f;

    [Header("Ataque")]
    [SerializeField] private bool  atacarAutomaticamente    = true;
    [SerializeField] private float toleranciaMiraParaAtirar = 8f;
    [SerializeField] private float alcanceMetralhadora      = 40f;
    [SerializeField] private float alcanceMissel            = 70f;

    [Header("Filtro míssil — pai + filho")]
    [SerializeField] private string[] tagsInimigoPai               = { "Vermelho" };
    [SerializeField] private string[] tagsFilhoApenasMetralhadora   = { "Soldado", "Guerreiro", "Coletor" };

    [Header("Debug")]
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
    // AWAKE — apenas referências e contagem de mísseis
    // =====================================================================

    private void Awake()
    {
        if (aviaoVisao == null)
            aviaoVisao = GetComponent<AviaoVisao>();

        misseisRestantes = ContarMisseisDisponiveis();
    }

    // =====================================================================
    // OnEnable — chamado quando AviaoControler faz enabled = true
    // =====================================================================

    private void OnEnable()
    {
        // Cooldown inicial para estabilizar antes do primeiro disparo
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
        AtualizarAlvoAtual();

        if (alvoAtual == null || !atacarAutomaticamente) return;

        float distancia       = Vector3.Distance(transform.position, alvoAtual.position);
        bool  misselPermitido = PodeUsarMissel(alvoAtual);

        if (misselPermitido && misseisRestantes > 0 && distancia <= alcanceMissel)
            TentarLancarMissel();

        if (distancia <= alcanceMetralhadora)
            TentarAtirarMetralhadora();
    }

    // =====================================================================
    // ALVO
    // =====================================================================

    private void AtualizarAlvoAtual()
    {
        alvoAtual = null;

        if (aviaoVisao == null || !aviaoVisao.TemAlvo) return;

        alvoAtual = aviaoVisao.AlvoAtual;
    }

    // =====================================================================
    // METRALHADORA
    // =====================================================================

    private void TentarAtirarMetralhadora()
    {
        if (Time.time < proximoTiroMetralhadora) return;

        Transform spawn = ObterSpawnMetralhadoraAtual();
        if (spawn == null || prefabBalaMetralhadora == null) return;

        Vector3 pontoMira = ObterPontoMira(alvoAtual);
        if (!MiraAlinhada(spawn.position, pontoMira, toleranciaMiraParaAtirar)) return;

        Vector3    direcao = (pontoMira - spawn.position).normalized;
        Quaternion rot     = Quaternion.LookRotation(direcao, Vector3.up);

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

        Vector3 pontoMira = ObterPontoMira(alvoAtual);
        Vector3 posSlot   = slotsMisseis[slotIndex].transform.position;

        if (!MiraAlinhada(posSlot, pontoMira, toleranciaMiraParaAtirar * 3f)) return;

        GameObject missel = slotsMisseis[slotIndex];
        missel.transform.SetParent(null);
        missel.SendMessage("Lancar", alvoAtual, SendMessageOptions.DontRequireReceiver);

        slotsMisseis[slotIndex]  = null;
        misseisRestantes--;

        proximoLancamentoMissel = Time.time + Mathf.Max(0.1f, intervaloMissel);
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
    // VALIDAÇÃO — pai inimigo + filho unidade pequena
    // =====================================================================

    private bool PodeUsarMissel(Transform alvo)
    {
        if (alvo == null) return true;
        bool paiEhInimigo = ObjetoOuAncestralTemTag(alvo, tagsInimigoPai);
        if (!paiEhInimigo) return true;
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

    private bool MiraAlinhada(Vector3 origemPos, Vector3 pontoMira, float tolerancia)
    {
        Vector3 direcaoAlvo = (pontoMira - origemPos).normalized;
        return Vector3.Angle(transform.forward, direcaoAlvo) <= tolerancia;
    }

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
        intervaloMetralhadora    = Mathf.Max(0.02f, intervaloMetralhadora);
        intervaloMissel          = Mathf.Max(0.1f,  intervaloMissel);
        toleranciaMiraParaAtirar = Mathf.Clamp(toleranciaMiraParaAtirar, 1f, 45f);
        alcanceMetralhadora      = Mathf.Max(1f, alcanceMetralhadora);
        alcanceMissel            = Mathf.Max(1f, alcanceMissel);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        if (!desenharGizmosNoEditor) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alcanceMetralhadora);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, alcanceMissel);

        if (spawnMetralhadora1 != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnMetralhadora1.position,
                            spawnMetralhadora1.position + transform.forward * 3f);
        }
        if (spawnMetralhadora2 != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnMetralhadora2.position,
                            spawnMetralhadora2.position + transform.forward * 3f);
        }

        Gizmos.color = Color.red;
        foreach (GameObject slot in slotsMisseis)
            if (slot != null)
                Gizmos.DrawSphere(slot.transform.position, 0.2f);

        if (alvoAtual != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, alvoAtual.position);
        }
    }
}