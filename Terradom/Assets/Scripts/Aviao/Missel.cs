using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Míssil teleguiado lançado pela TorreAr (ou por AviaoAtaque).
///
/// REGRA DE OURO:
///   O míssil nasce PARADO e inerte — filho do ponto de lançamento, sem comportamento ativo.
///   Somente após receber Lancar() ele vira um agente autônomo:
///     • Se desparenta do pai (SetParent null) — já feito pela torre antes de SetActive.
///     • Começa a se mover e perseguir o alvo.
///     • Inicia o contador de tempo de vida.
///     • Ativa o rastro de fumaça.
///     • Passa a ignorar colisões e dano no objeto que o disparou.
///
/// POOL:
///   Ao terminar seu ciclo (colisão ou tempo esgotado), o míssil NÃO se destrói.
///   Em vez disso, avisa a TorreAr via DevolverMisselAoPool() para ser reaproveitado
///   na próxima recarga, evitando alocações desnecessárias.
///
/// Fluxo (TorreAr):
///   1. TorreAr desparenta o míssil do ponto (SetParent null).
///   2. TorreAr ativa o GameObject → Awake() roda com parent = null.
///   3. TorreAr chama missel.Lancar(alvo, torreTranform, pontoOrigem, torre).
///   4. O míssil acelera para frente e gira suavemente em direção ao alvo.
///   5. Ao colidir ou ao expirar o tempo de vida, avisa a torre e volta ao pool.
/// </summary>
[DisallowMultipleComponent]
public class Missel : MonoBehaviour
{
    // =====================================================================
    // INSPECTOR — MOVIMENTO
    // =====================================================================

    [Header("Movimento")]
    [SerializeField] private float velocidade           = 55f;
    [SerializeField] private float aceleracao           = 20f;
    [SerializeField] private float velocidadeMaxima     = 80f;
    [SerializeField] private float taxaCurvaGrausPorSeg = 70f;

    [Header("Tempo de vida")]
    [SerializeField] private float tempoDeVida = 8f;

    // =====================================================================
    // INSPECTOR — DANO
    // =====================================================================

    [Header("Dano")]
    [SerializeField] private int   dano                 = 50;
    [SerializeField] private bool  destruirMesmoSemDano = true;

    [Header("Tags que recebem dano")]
    [SerializeField] private string[] tagsQueRecebemDano = { "Vermelho" };

    [Header("Dano em BaseVida")]
    [SerializeField] private bool aplicarDanoEmBaseVida                       = true;
    [SerializeField] private bool baseVidaIgnoraTagDoAlvo                     = true;
    [SerializeField] private bool forcarDanoNaBaseVidaSemValidarTagDoAtacante = false;

    [Header("Dano genérico")]
    [SerializeField] private bool aplicarDanoEmVida     = true;
    [SerializeField] private bool aplicarDanoPorMetodos = true;

    // =====================================================================
    // INSPECTOR — DETECÇÃO DE IMPACTO
    // =====================================================================

    [Header("Detecção de impacto")]
    [SerializeField] private LayerMask camadasDeImpacto = ~0;
    [SerializeField] private bool      detectarTriggers = false;
    [SerializeField] private float     raioDeteccao     = 0.4f;
    [SerializeField] private float     margemDeteccao   = 0.1f;

    // =====================================================================
    // INSPECTOR — EFEITO DE EXPLOSÃO
    // =====================================================================

    [Header("Efeito de explosão")]
    [SerializeField] private GameObject prefabExplosao;
    [SerializeField] private bool       efeitoSomenteNoImpacto   = false;
    [SerializeField] private bool       alinharExplosaoComNormal  = false;
    [SerializeField] private float      tempoParaDestruirExplosao = 4f;

    // =====================================================================
    // INSPECTOR — RASTRO DE FUMAÇA
    // =====================================================================

    [Header("Rastro de fumaça")]
    [Tooltip("Ponto de onde o rastro de fumaça vai nascer (filho do míssil)")]
    [SerializeField] private Transform    spawnRastro;
    [Tooltip("ParticleSystem do rastro — fica DESATIVADO até o lançamento")]
    [SerializeField] private ParticleSystem rastroFumaca;
    [Tooltip("Quanto tempo o rastro continua visível após o míssil ser desativado")]
    [SerializeField] private float tempoRastroAposDestruir = 3f;

    // =====================================================================
    // PRIVADOS
    // =====================================================================

    private Transform alvo;
    private Transform origemDisparo;     // torre ou avião que disparou — nunca recebe colisão/dano
    private Transform pontoOrigem;       // ponto de lançamento para devolução ao pool
    private TorreAr   torreDonoDoPool;   // referência para devolver ao pool (null se veio de avião)

    private Rigidbody rb;
    private Collider  col;
    private bool      foiLancado;
    private bool      jaColidiu;
    private float     velocidadeAtual;

    private Vector3 posicaoImpacto;
    private Vector3 normalImpacto;
    private bool    houveImpacto;

    // Coroutine do tempo de vida (guardada para cancelar se necessário)
    private Coroutine _coroutineVida;

    // =====================================================================
    // API PÚBLICA
    // =====================================================================

    /// <summary>
    /// Usado pela TorreAr para saber se este míssil está disponível no pool.
    /// false = inerte no ponto, pronto para ser lançado.
    /// </summary>
    public bool EstaLancado => foiLancado;

    /// <summary>
    /// Assinatura original (compatibilidade com AviaoAtaque).
    /// Sem pool: o míssil se destrói ao terminar.
    /// </summary>
    public void Lancar(Transform novoAlvo, Transform transformOrigem)
    {
        Lancar(novoAlvo, transformOrigem, null, null);
    }

    /// <summary>
    /// Assinatura completa usada pela TorreAr para suporte a pool.
    /// pontoDeOrigem: ponto ao qual o míssil será devolvido após o ciclo de vida.
    /// torre: referência da TorreAr dona do pool (pode ser null).
    /// </summary>
    public void Lancar(Transform novoAlvo, Transform transformOrigem,
                       Transform pontoDeOrigem, TorreAr torre)
    {
        if (foiLancado) return;

        alvo             = novoAlvo;
        origemDisparo    = transformOrigem;
        pontoOrigem      = pontoDeOrigem;
        torreDonoDoPool  = torre;
        foiLancado       = true;

        // Ativa o collider — enquanto inerte ele fica desligado
        if (col != null) col.enabled = true;

        // Inicia o countdown de vida
        _coroutineVida = StartCoroutine(ContagemRegressiva());

        // Ativa o rastro de fumaça
        AtivarRastro();

        Debug.Log($"[Missel] {gameObject.name} — lançado contra {(alvo != null ? alvo.name : "null")}");
    }

    // =====================================================================
    // INICIALIZAÇÃO
    // =====================================================================

    private void Awake()
    {
        rb  = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        ConfigurarRigidbody();

        velocidadeAtual = velocidade;
        normalImpacto   = -transform.forward;

        foiLancado = false;
        jaColidiu  = false;

        // Nasce inerte: visível na torre, collider desligado, sem mover
        if (col != null) col.enabled = false;
        PararRastro();
    }

    // OnEnable não é mais usado para reset (o GameObject nunca é desativado no pool)
    private void OnEnable() { }

    /// <summary>
    /// Chamado por TorreAr.DevolverMisselAoPool — reseta o míssil sem desativar o GameObject.
    /// O míssil volta a ser visível no ponto de lançamento, pronto para o próximo disparo.
    /// </summary>
    public void ResetarParaPool(Transform pontoDeOrigem)
    {
        if (_coroutineVida != null)
        {
            StopCoroutine(_coroutineVida);
            _coroutineVida = null;
        }

        foiLancado      = false;
        jaColidiu       = false;
        houveImpacto    = false;
        velocidadeAtual = velocidade;
        normalImpacto   = -transform.forward;
        alvo            = null;
        origemDisparo   = null;
        pontoOrigem     = null;
        torreDonoDoPool = null;

        transform.SetParent(pontoDeOrigem);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (col != null) col.enabled = false;
        PararRastro();
    }

    private void ConfigurarRigidbody()
    {
        if (rb == null) return;
        rb.isKinematic            = true;
        rb.useGravity             = false;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    // =====================================================================
    // RASTRO DE FUMAÇA
    // =====================================================================

    private void PararRastro()
    {
        if (rastroFumaca == null) return;
        rastroFumaca.gameObject.SetActive(false);
    }

    private void AtivarRastro()
    {
        if (rastroFumaca == null)
        {
            Debug.LogWarning($"[Missel] {gameObject.name} — Rastro Fumaca nao atribuido no Inspector!", this);
            return;
        }

        if (spawnRastro != null)
        {
            rastroFumaca.transform.position = spawnRastro.position;
            rastroFumaca.transform.rotation = spawnRastro.rotation;
        }

        if (!rastroFumaca.gameObject.activeSelf)
            rastroFumaca.gameObject.SetActive(true);

        rastroFumaca.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        rastroFumaca.Play(true);
    }

    /// <summary>
    /// Para o rastro e agenda sua destruição após tempoRastroAposDestruir segundos.
    /// O rastro é desparentado para que as partículas existentes continuem visíveis.
    /// </summary>
    private void DesligarRastro()
    {
        if (rastroFumaca == null) return;

        rastroFumaca.transform.SetParent(null);
        rastroFumaca.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(rastroFumaca.gameObject, tempoRastroAposDestruir);
    }

    // =====================================================================
    // TEMPO DE VIDA
    // =====================================================================

    private IEnumerator ContagemRegressiva()
    {
        yield return new WaitForSeconds(tempoDeVida);
        if (!jaColidiu)
            EncerrarCicloDeMissil(false);
    }

    // =====================================================================
    // UPDATE — MOVIMENTO E PERSEGUIÇÃO (só após Lancar)
    // =====================================================================

    private void FixedUpdate()
    {
        if (!foiLancado) return;
        if (jaColidiu)   return;

        Acelerar();
        GirarEmDirecaoAoAlvo();
        MoverComDeteccao();
    }

    private void Acelerar()
    {
        if (velocidadeAtual >= velocidadeMaxima) return;
        velocidadeAtual = Mathf.MoveTowards(
            velocidadeAtual, velocidadeMaxima, aceleracao * Time.fixedDeltaTime);
    }

    private void GirarEmDirecaoAoAlvo()
    {
        if (alvo == null) return;

        Vector3 direcaoAlvo = (ObterPontoMira() - transform.position).normalized;
        if (direcaoAlvo.sqrMagnitude < 0.001f) return;

        Quaternion rotacaoDesejada = Quaternion.LookRotation(direcaoAlvo, Vector3.up);
        float      maxGiro         = taxaCurvaGrausPorSeg * Time.fixedDeltaTime;

        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacaoDesejada, maxGiro);
    }

    private void MoverComDeteccao()
    {
        Vector3 direcao        = transform.forward;
        float   distanciaFrame = velocidadeAtual * Time.fixedDeltaTime;
        Vector3 origem         = transform.position;

        if (TentarDetectarImpacto(origem, direcao, distanciaFrame, out RaycastHit hit))
        {
            Vector3 ponto  = hit.point != Vector3.zero ? hit.point : origem + direcao * hit.distance;
            posicaoImpacto = ponto;
            normalImpacto  = hit.normal.sqrMagnitude > 0.001f ? hit.normal : -direcao;
            houveImpacto   = true;

            transform.position = ponto;
            ColidiuComCollider(hit.collider);
            return;
        }

        Vector3 novaPosicao = origem + direcao * distanciaFrame;
        if (rb != null) rb.MovePosition(novaPosicao);
        else            transform.position = novaPosicao;
    }

    // =====================================================================
    // DETECÇÃO DE IMPACTO (SphereCast)
    // =====================================================================

    private bool TentarDetectarImpacto(Vector3 origem, Vector3 direcao, float distancia, out RaycastHit melhorHit)
    {
        melhorHit = new RaycastHit();

        QueryTriggerInteraction triggerMode = detectarTriggers
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        float raio           = Mathf.Max(0.05f, raioDeteccao);
        float distanciaTotal = distancia + Mathf.Max(0f, margemDeteccao);

        RaycastHit[] hits = Physics.SphereCastAll(
            origem, raio, direcao.normalized,
            distanciaTotal, camadasDeImpacto, triggerMode);

        bool  encontrou      = false;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null)                           continue;
            if (EhDoProprioMissel(col.transform))      continue;
            if (EhDoOrigemDisparo(col.transform))      continue;

            if (hits[i].distance < menorDistancia)
            {
                menorDistancia = hits[i].distance;
                melhorHit      = hits[i];
                encontrou      = true;
            }
        }

        return encontrou;
    }

    private bool EhDoProprioMissel(Transform t)
    {
        if (t == null)              return true;
        if (t == transform)         return true;
        if (t.IsChildOf(transform)) return true;
        return false;
    }

    private bool EhDoOrigemDisparo(Transform t)
    {
        if (t == null || origemDisparo == null) return false;
        if (t == origemDisparo)                 return true;
        if (t.IsChildOf(origemDisparo))         return true;
        return false;
    }

    // =====================================================================
    // COLISÃO FÍSICA (Unity callbacks — redundância segura)
    // =====================================================================

    private void OnCollisionEnter(Collision collision)
    {
        if (!foiLancado) return;
        if (collision == null || collision.collider == null) return;
        if (EhDoOrigemDisparo(collision.collider.transform)) return;

        if (collision.contactCount > 0)
        {
            posicaoImpacto = collision.contacts[0].point;
            normalImpacto  = collision.contacts[0].normal;
            houveImpacto   = true;
        }

        ColidiuComCollider(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!foiLancado) return;
        if (other == null)                      return;
        if (EhDoOrigemDisparo(other.transform)) return;

        ColidiuComCollider(other);
    }

    // =====================================================================
    // APLICAÇÃO DE COLISÃO
    // =====================================================================

    private void ColidiuComCollider(Collider colisor)
    {
        if (colisor == null)                        return;
        if (EhDoProprioMissel(colisor.transform))   return;
        if (EhDoOrigemDisparo(colisor.transform))   return;

        ColidiuComTransform(colisor.transform);
    }

    private void ColidiuComTransform(Transform transformAtingido)
    {
        if (jaColidiu) return;

        jaColidiu = true;

        bool aplicouDano = TentarAplicarDano(transformAtingido);

        if (aplicouDano || destruirMesmoSemDano)
            EncerrarCicloDeMissil(true);
        else
            jaColidiu = false;
    }

    // =====================================================================
    // ENCERRAMENTO DO CICLO (substitui Destroy — devolve ao pool)
    // =====================================================================

    /// <summary>
    /// Chamado tanto ao colidir quanto ao expirar o tempo de vida.
    /// Se pertence a um pool (torreDonoDoPool != null), devolve o míssil ao pool.
    /// Caso contrário (lançado por avião), destrói o GameObject normalmente.
    /// </summary>
    private void EncerrarCicloDeMissil(bool houveColisao)
    {
        // Cancela a contagem regressiva se ainda estiver rodando
        if (_coroutineVida != null)
        {
            StopCoroutine(_coroutineVida);
            _coroutineVida = null;
        }

        // Efeito de explosão
        if (foiLancado)
        {
            bool deveExplodir = !efeitoSomenteNoImpacto || houveColisao;
            if (deveExplodir) SpawnarExplosao();
        }

        // Desliga o rastro (desparenta — partículas existentes continuam visíveis)
        DesligarRastro();

        if (torreDonoDoPool != null && pontoOrigem != null)
        {
            // POOL: reseta e volta para o ponto — míssil continua visível na torre
            ResetarParaPool(pontoOrigem);
            torreDonoDoPool.NotificarMisselDevolvido();
        }
        else
        {
            // SEM POOL (ex.: avião): destrói normalmente
            Destroy(gameObject);
        }
    }

    // =====================================================================
    // DANO
    // =====================================================================

    private bool TentarAplicarDano(Transform transformAtingido)
    {
        if (transformAtingido == null) return false;

        if (EhDoOrigemDisparo(transformAtingido)) return false;

        if (aplicarDanoEmBaseVida && TentarDanoBaseVida(transformAtingido))
            return true;

        if (!ObjetoOuFamiliaTemTagPermitida(transformAtingido))
            return false;

        if (aplicarDanoEmVida && TentarDanoVida(transformAtingido))
            return true;

        if (aplicarDanoPorMetodos && TentarDanoPorMetodo(transformAtingido))
            return true;

        return false;
    }

    private bool TentarDanoBaseVida(Transform t)
    {
        BaseVida bv = BuscarBaseVidaSemSubirHierarquia(t);
        if (bv == null) return false;

        if (!baseVidaIgnoraTagDoAlvo && !ObjetoOuFamiliaTemTagPermitida(t))
            return false;

        if (forcarDanoNaBaseVidaSemValidarTagDoAtacante)
            bv.ReceberDano(dano);
        else
            bv.ReceberDano(dano, gameObject);

        return true;
    }

    private BaseVida BuscarBaseVidaSemSubirHierarquia(Transform t)
    {
        if (t == null) return null;

        BaseVida bv = t.GetComponent<BaseVida>();
        if (bv != null) return bv;

        bv = t.GetComponentInChildren<BaseVida>(true);
        return bv;
    }

    private bool TentarDanoVida(Transform t)
    {
        Vida v = t.GetComponent<Vida>();
        if (v == null) v = t.GetComponentInChildren<Vida>(true);
        if (v == null) return false;
        v.AplicarDano(dano);
        return true;
    }

    private bool TentarDanoPorMetodo(Transform t)
    {
        if (t == null) return false;

        if (InvocarDanoNosComponentes(t.GetComponents<MonoBehaviour>()))               return true;
        if (InvocarDanoNosComponentes(t.GetComponentsInChildren<MonoBehaviour>(true))) return true;

        Transform pai = t.parent;
        while (pai != null)
        {
            if (!EhDoOrigemDisparo(pai))
                if (InvocarDanoNosComponentes(pai.GetComponents<MonoBehaviour>())) return true;
            pai = pai.parent;
        }

        return false;
    }

    private bool InvocarDanoNosComponentes(MonoBehaviour[] lista)
    {
        if (lista == null) return false;

        for (int i = 0; i < lista.Length; i++)
        {
            MonoBehaviour c = lista[i];
            if (c == null || c == this) continue;

            if (InvocarMetodoDano(c, "AplicarDano")) return true;
            if (InvocarMetodoDano(c, "ReceberDano")) return true;
            if (InvocarMetodoDano(c, "TomarDano"))   return true;
        }

        return false;
    }

    private bool InvocarMetodoDano(MonoBehaviour componente, string nomeMetodo)
    {
        MethodInfo m = componente.GetType().GetMethod(
            nomeMetodo,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(int) }, null);

        if (m == null) return false;
        m.Invoke(componente, new object[] { dano });
        return true;
    }

    // =====================================================================
    // TAGS
    // =====================================================================

    private bool ObjetoOuFamiliaTemTagPermitida(Transform origem)
    {
        if (tagsQueRecebemDano == null || tagsQueRecebemDano.Length == 0) return true;
        if (origem == null) return false;

        Transform atual = origem;
        while (atual != null)
        {
            if (TemTagPermitida(atual.gameObject)) return true;
            atual = atual.parent;
        }

        Transform[] filhos = origem.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < filhos.Length; i++)
            if (filhos[i] != null && TemTagPermitida(filhos[i].gameObject)) return true;

        return false;
    }

    private bool TemTagPermitida(GameObject obj)
    {
        if (obj == null) return false;
        if (tagsQueRecebemDano == null || tagsQueRecebemDano.Length == 0) return true;

        for (int i = 0; i < tagsQueRecebemDano.Length; i++)
        {
            string tag = tagsQueRecebemDano[i];
            if (!string.IsNullOrWhiteSpace(tag) && obj.CompareTag(tag)) return true;
        }

        return false;
    }

    // =====================================================================
    // EXPLOSÃO
    // =====================================================================

    private void SpawnarExplosao()
    {
        if (prefabExplosao == null) return;

        Vector3    pos = houveImpacto ? posicaoImpacto : transform.position;
        Quaternion rot = Quaternion.identity;

        if (alinharExplosaoComNormal && normalImpacto.sqrMagnitude > 0.001f)
            rot = Quaternion.LookRotation(normalImpacto, Vector3.up);

        GameObject efeito = Instantiate(prefabExplosao, pos, rot);
        if (tempoParaDestruirExplosao > 0f)
            Destroy(efeito, tempoParaDestruirExplosao);
    }

    // =====================================================================
    // AUXILIARES
    // =====================================================================

    private Vector3 ObterPontoMira()
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
        dano                      = Mathf.Max(0, dano);
        velocidade                = Mathf.Max(1f, velocidade);
        aceleracao                = Mathf.Max(0f, aceleracao);
        velocidadeMaxima          = Mathf.Max(velocidade, velocidadeMaxima);
        taxaCurvaGrausPorSeg      = Mathf.Clamp(taxaCurvaGrausPorSeg, 5f, 360f);
        tempoDeVida               = Mathf.Max(0.5f, tempoDeVida);
        raioDeteccao              = Mathf.Max(0.05f, raioDeteccao);
        margemDeteccao            = Mathf.Max(0f, margemDeteccao);
        tempoParaDestruirExplosao = Mathf.Max(0f, tempoParaDestruirExplosao);
        tempoRastroAposDestruir   = Mathf.Max(0f, tempoRastroAposDestruir);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = foiLancado ? Color.red : Color.gray;
        Gizmos.DrawWireSphere(transform.position, raioDeteccao);

        if (alvo != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, alvo.position);
            Gizmos.DrawSphere(alvo.position, 0.5f);
        }
    }
}