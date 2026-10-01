using UnityEngine;

/// <summary>
/// SISTEMA DE ARMAMENTO DO AVIÃO.
///
/// Fluxo:
///   1. AviaoControler ativa este componente junto com AviaoVisao (fase Patrulha).
///   2. AviaoVisao escolhe um alvo visível por categoria e mantém o combate durante as curvas.
///   3. Mísseis atacam aviões, tanques e estruturas; metralhadoras atacam apenas
///      coletores, soldados e guerreiros.
///   4. Os mísseis retornam ao suporte depois do voo e são repostos 15 s após
///      o lançamento do último míssil.
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
    [Tooltip("Tempo para repor todos os mísseis depois que o último for lançado.")]
    [SerializeField] private float        tempoRecargaMisseis = 15f;

    // =====================================================================
    // INSPECTOR — FILTRO MÍSSIL
    // =====================================================================

    [Header("Validação de alvos")]
    [Tooltip("Tags do PAI que confirmam que o objeto é inimigo")]
    [SerializeField] private string[] tagsInimigoPai = { "Azul", "Vermelho", "Verde" };
    [Tooltip("Máscara preenchida pelas layers Coletor, Soldado e Guerreiro. Mantida no Inspector para facilitar a conferência.")]
    [SerializeField] private LayerMask layersApenasMetralhadora;

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
    private Transform[] pontosMontagemMisseis;
    private Vector3[]   posicoesLocaisMisseis;
    private Quaternion[] rotacoesLocaisMisseis;
    private Vector3[]   escalasLocaisMisseis;
    private GameObject[] misseisAguardandoRecarga;
    private bool[] slotsMisseisConfigurados;
    private LayerMask camadasAlvoMissel;
    private int quantidadeMaximaMisseis;
    private bool recarregandoMisseis;
    private float instanteFimRecarga;
    private bool suportesMisseisPreparados;

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

        PrepararSuportesMisseis();
        misseisRestantes = ContarMisseisDisponiveis();
    }

    // =====================================================================
    // OnEnable — AviaoControler ativa junto com AviaoVisao (fase Patrulha)
    // =====================================================================

    private void OnEnable()
    {
        if (!suportesMisseisPreparados)
        {
            PrepararSuportesMisseis();
            misseisRestantes = ContarMisseisDisponiveis();
        }

        proximoTiroMetralhadora = Time.time + 1f;
        proximoLancamentoMissel = Time.time + 1f;
        alvoAtual               = null;
    }

    private void PrepararSuportesMisseis()
    {
        if (suportesMisseisPreparados) return;

        if (slotsMisseis == null)
            slotsMisseis = new GameObject[0];

        int quantidade = slotsMisseis.Length;
        pontosMontagemMisseis = new Transform[quantidade];
        posicoesLocaisMisseis = new Vector3[quantidade];
        rotacoesLocaisMisseis = new Quaternion[quantidade];
        escalasLocaisMisseis = new Vector3[quantidade];
        misseisAguardandoRecarga = new GameObject[quantidade];
        slotsMisseisConfigurados = new bool[quantidade];

        layersApenasMetralhadora = CriarMascaraCamadas("Coletor", "Soldado", "Guerreiro");
        camadasAlvoMissel = CriarMascaraCamadas(
            "Aviao", "Tank", "BaseTank", "BaseAviao", "BaseSoldado");

        for (int i = 0; i < quantidade; i++)
        {
            GameObject slot = slotsMisseis[i];
            if (slot == null || slot.GetComponent<Missel>() == null)
                continue;

            Transform montagem = slot.transform.parent != null ? slot.transform.parent : transform;
            pontosMontagemMisseis[i] = montagem;
            posicoesLocaisMisseis[i] = slot.transform.localPosition;
            rotacoesLocaisMisseis[i] = slot.transform.localRotation;
            escalasLocaisMisseis[i] = slot.transform.localScale;
            slotsMisseisConfigurados[i] = true;
            quantidadeMaximaMisseis++;
        }

        suportesMisseisPreparados = true;
    }

    // =====================================================================
    // UPDATE
    // =====================================================================

    private void Update()
    {
        AtualizarRecargaMisseis();

        // Obtém o alvo direto do AviaoVisao.
        // O cone frontal já foi aplicado lá — se há alvo, está à frente do avião.
        alvoAtual = (aviaoVisao != null && aviaoVisao.TemAlvo) ? aviaoVisao.AlvoAtual : null;

        if (!AlvoEhInimigo(alvoAtual)) return;

        AviaoVisao.ClasseAlvoCombate classe = ObterClasseAlvoAtual(alvoAtual);
        if (classe == AviaoVisao.ClasseAlvoCombate.Unidade)
        {
            TentarAtirarMetralhadora();
            return;
        }

        if (misseisRestantes > 0 && PodeUsarMissel(alvoAtual, classe))
            TentarLancarMissel();
    }

    // =====================================================================
    // METRALHADORA
    // =====================================================================

    private void TentarAtirarMetralhadora()
    {
        if (Time.time < proximoTiroMetralhadora) return;
        if (!NaLinhaDeAtiro()) return;

        Transform spawn = ObterSpawnMetralhadoraAtual();
        if (spawn == null || prefabBalaMetralhadora == null) return;

        // Mira diretamente no centro do alvo — simula a passagem rasante do avião
        Vector3 pontoAlvo   = ObterPontoMira(alvoAtual);
        Vector3 direcaoMira = (pontoAlvo - spawn.position).normalized;

        // Se a direção for zero (spawn colapsado), usa o forward do spawn
        if (direcaoMira.sqrMagnitude < 0.001f)
            direcaoMira = spawn.forward;

        Quaternion rotacaoMira = Quaternion.LookRotation(direcaoMira);
        GameObject balaGO      = Instantiate(prefabBalaMetralhadora, spawn.position, rotacaoMira);

        ProjetilDistancia projetil = balaGO.GetComponent<ProjetilDistancia>();
        if (projetil != null)
            projetil.Configurar(
                alvoAtual,
                danoMetralhadora,
                velocidadeMetralhadora,
                transform,
                pontoAlvo,
                layersApenasMetralhadora);

        indexMetralhadoraAtual  = (indexMetralhadoraAtual + 1) % 2;
        proximoTiroMetralhadora = Time.time + Mathf.Max(0.02f, intervaloMetralhadora);
    }

    /// <summary>
    /// Autoriza o disparo apenas quando o nariz do avião aponta para o alvo
    /// dentro de anguloMiraMetralhadora graus.
    /// Isso simula o alinhamento real do avião antes de disparar a metralhadora.
    /// </summary>
    private bool NaLinhaDeAtiro()
    {
        if (alvoAtual == null) return false;

        // Usa o ângulo calculado pelo AviaoVisao (nariz do avião → alvo)
        if (aviaoVisao != null)
            return aviaoVisao.AnguloParaAlvo <= anguloMiraMetralhadora;

        // Fallback: calcula direto no transform deste avião
        Vector3 direcaoAlvo = (ObterPontoMira(alvoAtual) - transform.position).normalized;
        float   angulo      = Vector3.Angle(transform.forward, direcaoAlvo);
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
        if (slotIndex < 0)
        {
            misseisRestantes = ContarMisseisDisponiveis();
            IniciarRecargaSeEsgotado();
            return;
        }

        GameObject go = slotsMisseis[slotIndex];
        if (go == null)
        {
            slotsMisseis[slotIndex] = null;
            misseisRestantes = ContarMisseisDisponiveis();
            IniciarRecargaSeEsgotado();
            return;
        }

        Missel misselScript = go.GetComponent<Missel>();
        if (misselScript == null)
        {
            Debug.LogWarning($"[AviaoAtaque] Slot {slotIndex} não tem componente Missel!", this);
            slotsMisseis[slotIndex] = null;
            misseisRestantes = ContarMisseisDisponiveis();
            IniciarRecargaSeEsgotado();
            return;
        }

        // 1. Desparenta ANTES de Lancar — a partir daqui o míssil não segue mais o avião
        go.transform.SetParent(null);

        // O míssil ignora aliados, aceita apenas classes de alvo permitidas e
        // retorna ao suporte correto para a recarga depois de explodir/expirar.
        misselScript.LancarParaAviao(
            alvoAtual,
            this,
            slotIndex,
            pontosMontagemMisseis[slotIndex],
            camadasAlvoMissel);

        // 3. Remove o slot e contabiliza
        slotsMisseis[slotIndex] = null;
        misseisRestantes = ContarMisseisDisponiveis();

        proximoLancamentoMissel = Time.time + Mathf.Max(0.1f, intervaloMissel);
        IniciarRecargaSeEsgotado();
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

    // =====================================================================
    // FILTRO DE MÍSSIL
    // =====================================================================

    /// <summary>
    /// Mísseis ficam reservados para aviões, tanques e estruturas inimigas.
    /// </summary>
    private bool PodeUsarMissel(Transform alvo, AviaoVisao.ClasseAlvoCombate classe)
    {
        return AlvoEhInimigo(alvo)
            && (classe == AviaoVisao.ClasseAlvoCombate.Aereo
                || classe == AviaoVisao.ClasseAlvoCombate.Tanque
                || classe == AviaoVisao.ClasseAlvoCombate.Base);
    }

    private bool AlvoEhInimigo(Transform alvo)
    {
        if (alvo == null)
            return false;

        string equipePropria = ObterTagEquipe(transform);
        string equipeAlvo = ObterTagEquipe(alvo);

        // A lista do Inspector pode ficar desatualizada; a equipe nunca pode
        // atacar um objeto da própria equipe, mesmo que a tag esteja liberada.
        if (!string.IsNullOrEmpty(equipePropria)
            && !string.IsNullOrEmpty(equipeAlvo)
            && equipePropria == equipeAlvo)
            return false;

        return ObjetoOuAncestralTemTag(alvo, tagsInimigoPai);
    }

    private static string ObterTagEquipe(Transform origem)
    {
        Transform atual = origem;
        while (atual != null)
        {
            if (atual.CompareTag("Azul") || atual.CompareTag("Vermelho") || atual.CompareTag("Verde"))
                return atual.tag;
            atual = atual.parent;
        }
        return string.Empty;
    }

    private AviaoVisao.ClasseAlvoCombate ObterClasseAlvoAtual(Transform alvo)
    {
        if (aviaoVisao != null && aviaoVisao.AlvoAtual == alvo)
            return aviaoVisao.ClasseAlvoAtual;

        if (alvo == null) return AviaoVisao.ClasseAlvoCombate.Nenhum;
        if (AlvoEstaEmLayer(alvo, "Aviao")) return AviaoVisao.ClasseAlvoCombate.Aereo;
        if (AlvoEstaEmLayer(alvo, "Tank")) return AviaoVisao.ClasseAlvoCombate.Tanque;
        if (AlvoEstaEmLayer(alvo, "BaseTank", "BaseAviao", "BaseSoldado"))
            return AviaoVisao.ClasseAlvoCombate.Base;
        if (AlvoEstaEmLayer(alvo, "Coletor", "Soldado", "Guerreiro"))
            return AviaoVisao.ClasseAlvoCombate.Unidade;
        return AviaoVisao.ClasseAlvoCombate.OutroTerrestre;
    }

    private void IniciarRecargaSeEsgotado()
    {
        if (misseisRestantes > 0 || recarregandoMisseis || quantidadeMaximaMisseis <= 0)
            return;

        recarregandoMisseis = true;
        instanteFimRecarga = Time.time + Mathf.Max(0.1f, tempoRecargaMisseis);
    }

    private void AtualizarRecargaMisseis()
    {
        IniciarRecargaSeEsgotado();
        if (!recarregandoMisseis || Time.time < instanteFimRecarga)
            return;

        for (int i = 0; i < slotsMisseis.Length; i++)
        {
            if (!slotsMisseisConfigurados[i] || slotsMisseis[i] != null)
                continue;

            GameObject misselGuardado = misseisAguardandoRecarga[i];
            Transform montagem = pontosMontagemMisseis[i];
            if (misselGuardado == null || montagem == null)
            {
                // Um míssil ainda está em voo ou o suporte foi removido.
                instanteFimRecarga = Time.time + 0.25f;
                return;
            }

            Missel componenteMissel = misselGuardado.GetComponent<Missel>();
            if (componenteMissel == null)
            {
                instanteFimRecarga = Time.time + 0.25f;
                return;
            }

            componenteMissel.ResetarParaPool(montagem);
            RestaurarTransformLocal(misselGuardado.transform, i);
            misselGuardado.SetActive(true);
            slotsMisseis[i] = misselGuardado;
            misseisAguardandoRecarga[i] = null;
        }

        misseisRestantes = ContarMisseisDisponiveis();
        if (misseisRestantes > 0)
        {
            recarregandoMisseis = false;
            proximoLancamentoMissel = Time.time + 0.5f;
        }
        else
        {
            instanteFimRecarga = Time.time + 0.25f;
        }
    }

    /// <summary>Chamado pelo míssil ao terminar o voo, para guardá-lo até a recarga.</summary>
    public void NotificarMisselDevolvido(Missel missel, int slotIndex)
    {
        if (missel == null || slotIndex < 0 || slotIndex >= misseisAguardandoRecarga.Length)
            return;

        GameObject objeto = missel.gameObject;
        RestaurarTransformLocal(objeto.transform, slotIndex);
        objeto.SetActive(false);
        misseisAguardandoRecarga[slotIndex] = objeto;
    }

    private void RestaurarTransformLocal(Transform alvo, int slotIndex)
    {
        if (alvo == null || slotIndex < 0 || slotIndex >= pontosMontagemMisseis.Length)
            return;

        Transform montagem = pontosMontagemMisseis[slotIndex];
        if (montagem == null) return;

        alvo.SetParent(montagem, false);
        alvo.localPosition = posicoesLocaisMisseis[slotIndex];
        alvo.localRotation = rotacoesLocaisMisseis[slotIndex];
        alvo.localScale = escalasLocaisMisseis[slotIndex];
    }

    private int ContarMisseisDisponiveis()
    {
        if (slotsMisseis == null) return 0;
        int count = 0;
        for (int i = 0; i < slotsMisseis.Length; i++)
            if (slotsMisseis[i] != null && slotsMisseis[i].GetComponent<Missel>() != null)
                count++;
        return count;
    }

    private bool AlvoEstaEmLayer(Transform alvo, params string[] nomes)
    {
        if (alvo == null) return false;

        Transform atual = alvo;
        while (atual != null)
        {
            string nomeLayer = LayerMask.LayerToName(atual.gameObject.layer);
            for (int i = 0; i < nomes.Length; i++)
                if (nomeLayer == nomes[i]) return true;
            atual = atual.parent;
        }
        return false;
    }

    private static LayerMask CriarMascaraCamadas(params string[] nomes)
    {
        int mascara = 0;
        for (int layer = 0; layer < 32; layer++)
        {
            string nomeLayer = LayerMask.LayerToName(layer);
            for (int i = 0; i < nomes.Length; i++)
                if (!string.IsNullOrEmpty(nomeLayer) && nomeLayer == nomes[i])
                {
                    mascara |= 1 << layer;
                    break;
                }
        }
        return mascara;
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
        tempoRecargaMisseis    = Mathf.Max(0.1f,  tempoRecargaMisseis);
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
