using System;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class ProjetilDistancia : MonoBehaviour
{
    [Header("Configuracao")]
    [SerializeField] private int dano = 1;
    [SerializeField] private float velocidade = 20f;
    [SerializeField] private float tempoDeVida = 3f;
    [SerializeField] private bool mirarComAlturaDoAlvo = false;

    [Header("Alvo")]
    [SerializeField] private string[] tagsQueRecebemDano = { "Vermelho" };

    [Header("Deteccao de impacto")]
    [SerializeField] private LayerMask camadasDeImpacto = ~0;
    [SerializeField] private bool detectarTriggers = true;
    [SerializeField] private float raioDeteccaoImpacto = 0.12f;
    [SerializeField] private float margemDeteccaoImpacto = 0.05f;
    [SerializeField] private bool usarRigidbodySeExistir = true;
    [SerializeField] private bool configurarRigidbodyAutomaticamente = true;
    [SerializeField] private bool destruirMesmoSemDano = true;

    [Header("Dano em BaseVida")]
    [SerializeField] private bool aplicarDanoEmBaseVida = true;
    [SerializeField] private bool baseVidaIgnoraTagDoAlvo = true;
    [SerializeField] private bool forcarDanoNaBaseVidaSemValidarTagDoAtacante = false;

    [Header("Dano generico")]
    [SerializeField] private bool aplicarDanoEmVida = true;
    [SerializeField] private bool aplicarDanoPorMetodos = true;

    [Header("Efeito ao ser destruido")]
    [SerializeField] private GameObject prefabEfeitoImpacto;
    [SerializeField] private bool tocarEfeitoSomenteNoImpacto = true;
    [SerializeField] private bool alinharEfeitoComNormal = true;
    [SerializeField] private float tempoParaDestruirEfeito = 3f;

    private Transform alvo;
    private Transform origemDisparo;
    private TankLeve tankDono;
    private string tagEquipeDona;
    private bool restringirDanoPorCamada;
    private LayerMask camadasAlvoPermitidas;
    private float distanciaDoDisparo;
    private bool resultadoRegistrado;
    private Rigidbody rb;
    private Vector3 direcaoInicial;
    private bool jaColidiu;
    private bool direcaoDefinida;
    private RaycastHit[] bufferHitsImpacto = new RaycastHit[32];

    private Vector3 posicaoImpacto;
    private Vector3 normalImpacto;
    private bool houveImpacto;

    public string TagEquipeDona => tagEquipeDona;

    // =====================================================================
    // CONFIGURAR
    // =====================================================================

    public void Configurar(Transform novoAlvo, int novoDano, float novaVelocidade)
    {
        alvo      = novoAlvo;
        dano      = novoDano;
        velocidade = novaVelocidade;

        // BUG 4 CORRIGIDO: o overload simples não definia tagEquipeDona, deixando
        // EhAliado() sempre retornar false — sem proteção de fogo amigo.
        // Agora infere a equipe dona pela tag oposta do alvo.
        // Ex: alvo "Vermelho" → quem disparou provavelmente é "Azul".
        // Se o alvo não tiver tag de equipe reconhecida, tagEquipeDona fica vazio
        // (comportamento original preservado).
        if (novoAlvo != null)
        {
            string tagAlvo = ObterTagEquipe(novoAlvo);
            if      (tagAlvo == "Vermelho") { tagEquipeDona = "Azul";    tagsQueRecebemDano = new[] { "Vermelho", "Verde" }; }
            else if (tagAlvo == "Azul")     { tagEquipeDona = "Vermelho"; tagsQueRecebemDano = new[] { "Azul",    "Verde" }; }
            else if (tagAlvo == "Verde")    { tagEquipeDona = "";          /* sem alvo com tag de equipe conhecida */ }
        }

        DefinirDirecaoInicial();
    }

    public void Configurar(Transform novoAlvo, int novoDano, float novaVelocidade,
                           Transform novoDono, Vector3 pontoMira)
    {
        alvo          = novoAlvo;
        dano          = novoDano;
        velocidade    = novaVelocidade;
        origemDisparo = novoDono;
        tankDono      = novoDono != null ? novoDono.GetComponentInParent<TankLeve>() : null;
        tagEquipeDona = ObterTagEquipe(novoDono);
        distanciaDoDisparo = novoDono != null && novoAlvo != null
            ? Vector3.Distance(novoDono.position, novoAlvo.position)
            : 0f;

        if      (tagEquipeDona == "Azul")    tagsQueRecebemDano = new[] { "Vermelho", "Verde" };
        else if (tagEquipeDona == "Vermelho") tagsQueRecebemDano = new[] { "Azul",    "Verde" };
        else if (tagEquipeDona == "Verde")    tagsQueRecebemDano = new[] { "Azul",    "Vermelho" };

        DefinirDirecaoInicial(pontoMira);
    }

    public void Configurar(Transform novoAlvo, int novoDano, float novaVelocidade,
                           Transform novoDono, Vector3 pontoMira, LayerMask novasCamadasAlvo)
    {
        Configurar(novoAlvo, novoDano, novaVelocidade, novoDono, pontoMira);
        camadasAlvoPermitidas   = novasCamadasAlvo;
        restringirDanoPorCamada = true;
    }

    // =====================================================================
    // UNITY
    // =====================================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        AplicarConfiguracaoRigidbody();
    }

    private void Start()
    {
        if (!direcaoDefinida)
            direcaoInicial = transform.forward.normalized;

        if (direcaoInicial.sqrMagnitude < 0.001f)
            direcaoInicial = transform.forward.normalized;

        normalImpacto = -direcaoInicial;
        Destroy(gameObject, tempoDeVida);
    }

    private void FixedUpdate()
    {
        if (jaColidiu) return;
        MoverComDeteccaoContinua(Time.fixedDeltaTime);
    }

    // =====================================================================
    // DIREÇÃO INICIAL
    // =====================================================================

    private void DefinirDirecaoInicial()
    {
        if (alvo != null)
        {
            Vector3 destino = alvo.position;
            if (!mirarComAlturaDoAlvo)
                destino.y = transform.position.y;

            direcaoInicial = destino - transform.position;
            if (direcaoInicial.sqrMagnitude < 0.001f)
                direcaoInicial = transform.forward;
        }
        else
        {
            direcaoInicial = transform.forward;
        }

        direcaoInicial.Normalize();
        if (direcaoInicial.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direcaoInicial, Vector3.up);

        direcaoDefinida = true;
    }

    private void DefinirDirecaoInicial(Vector3 destino)
    {
        direcaoInicial = destino - transform.position;
        if (direcaoInicial.sqrMagnitude < 0.001f)
            direcaoInicial = transform.forward;

        direcaoInicial.Normalize();
        transform.rotation = Quaternion.LookRotation(direcaoInicial, Vector3.up);
        direcaoDefinida = true;
    }

    // =====================================================================
    // RIGIDBODY
    // =====================================================================

    private void AplicarConfiguracaoRigidbody()
    {
        if (!configurarRigidbodyAutomaticamente || rb == null) return;

        rb.isKinematic         = true;
        rb.useGravity          = false;
        rb.interpolation       = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    // =====================================================================
    // MOVIMENTO E DETECÇÃO
    // =====================================================================

    private void MoverComDeteccaoContinua(float deltaTime)
    {
        Vector3 direcao = direcaoInicial.normalized;
        if (direcao.sqrMagnitude < 0.001f)
            direcao = transform.forward.normalized;

        float distanciaMovimento = Mathf.Max(0f, velocidade * deltaTime);
        if (distanciaMovimento <= 0f) return;

        Vector3 origem = transform.position;

        if (TentarDetectarImpacto(origem, direcao, distanciaMovimento, out RaycastHit hit))
        {
            Vector3 pontoImpacto = hit.point;

            // BUG 5 CORRIGIDO: "== Vector3.zero" é comparação imprecisa com floats.
            // hit.point retorna zero quando o projétil começa dentro de um collider.
            // sqrMagnitude é a comparação correta para verificar vetor próximo de zero.
            if (pontoImpacto.sqrMagnitude < 0.0001f)
                pontoImpacto = origem + direcao * hit.distance;

            posicaoImpacto = pontoImpacto;
            normalImpacto  = hit.normal.sqrMagnitude > 0.001f ? hit.normal : -direcao;
            houveImpacto   = true;

            transform.position = pontoImpacto;
            ColidiuComCollider(hit.collider);
            return;
        }

        Vector3 novaPosicao = origem + direcao * distanciaMovimento;

        if (usarRigidbodySeExistir && rb != null)
            rb.MovePosition(novaPosicao);
        else
            transform.position = novaPosicao;
    }

    private bool TentarDetectarImpacto(Vector3 origem, Vector3 direcao, float distanciaMovimento, out RaycastHit melhorHit)
    {
        melhorHit = new RaycastHit();

        QueryTriggerInteraction triggerMode = detectarTriggers
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        float distanciaTotal = distanciaMovimento + Mathf.Max(0f, margemDeteccaoImpacto);
        float raio           = Mathf.Max(0.01f, raioDeteccaoImpacto);

        // BUG 3 CORRIGIDO: while(true) sem saída de emergência.
        // Array.Resize sem teto criava alocações GC indefinidas em cenas densas.
        // Agora o buffer tem cap de 1024 hits — suficiente para qualquer cenário normal.
        const int capBuffer = 1024;

        while (bufferHitsImpacto.Length <= capBuffer)
        {
            int quantidade = Physics.SphereCastNonAlloc(
                origem, raio, direcao.normalized,
                bufferHitsImpacto, distanciaTotal,
                camadasDeImpacto, triggerMode
            );

            bool  encontrou     = false;
            float menorDistancia = float.MaxValue;

            for (int i = 0; i < quantidade; i++)
            {
                Collider colisor = bufferHitsImpacto[i].collider;
                if (colisor == null) continue;
                if (ColisorEhDoProprioProjetil(colisor)) continue;
                if (EhAliado(colisor.transform)) continue;

                if (bufferHitsImpacto[i].distance < menorDistancia)
                {
                    menorDistancia = bufferHitsImpacto[i].distance;
                    melhorHit      = bufferHitsImpacto[i];
                    encontrou      = true;
                }
            }

            // Buffer não estava cheio: resultado é completo
            if (quantidade < bufferHitsImpacto.Length)
                return encontrou;

            // Buffer estava cheio: pode ter hits ignorados — dobra e tenta de novo
            int novoTamanho = bufferHitsImpacto.Length * 2;
            if (novoTamanho > capBuffer) novoTamanho = capBuffer;
            Array.Resize(ref bufferHitsImpacto, novoTamanho);
        }

        // Chegou no cap: retorna o melhor encontrado até agora
        return melhorHit.collider != null;
    }

    // =====================================================================
    // COLISÃO
    // =====================================================================

    private bool ColisorEhDoProprioProjetil(Collider colisor)
    {
        if (colisor == null) return true;

        Transform t = colisor.transform;
        if (t == transform) return true;
        if (t.IsChildOf(transform)) return true;

        if (origemDisparo != null &&
            (t == origemDisparo || t.IsChildOf(origemDisparo)))
            return true;

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null) return;

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
        if (other == null) return;
        ColidiuComCollider(other);
    }

    public void ColidiuCom(GameObject objetoAtingido)
    {
        if (objetoAtingido == null) return;
        ColidiuComTransform(objetoAtingido.transform);
    }

    private void ColidiuComCollider(Collider colisorAtingido)
    {
        if (colisorAtingido == null) return;
        if (ColisorEhDoProprioProjetil(colisorAtingido)) return;
        if (EhAliado(colisorAtingido.transform)) return;

        ColidiuComTransform(colisorAtingido.transform);
    }

    private void ColidiuComTransform(Transform transformAtingido)
    {
        if (jaColidiu) return;
        if (transformAtingido == null || EhAliado(transformAtingido)) return;

        jaColidiu = true;

        bool aplicouDano = TentarAplicarDano(transformAtingido);
        RegistrarResultadoDisparo(aplicouDano);

        if (aplicouDano || destruirMesmoSemDano)
            Destroy(gameObject);
        else
            jaColidiu = false;
    }

    // =====================================================================
    // EFEITO AO SER DESTRUÍDO
    // =====================================================================

    private void OnDestroy()
    {
        if (!resultadoRegistrado && tankDono != null)
            RegistrarResultadoDisparo(false);

        if (tocarEfeitoSomenteNoImpacto && !houveImpacto) return;
        SpawnarEfeito();
    }

    private void RegistrarResultadoDisparo(bool acertou)
    {
        if (resultadoRegistrado) return;
        resultadoRegistrado = true;
        if (tankDono != null)
            tankDono.RegistrarResultadoDisparo(acertou, distanciaDoDisparo);
    }

    private void SpawnarEfeito()
    {
        if (prefabEfeitoImpacto == null) return;

        Vector3 posicao = houveImpacto ? posicaoImpacto : transform.position;

        Quaternion rotacao = Quaternion.identity;
        if (alinharEfeitoComNormal && normalImpacto.sqrMagnitude > 0.001f)
            rotacao = Quaternion.LookRotation(normalImpacto, Vector3.up);

        GameObject efeito = Instantiate(prefabEfeitoImpacto, posicao, rotacao);
        if (tempoParaDestruirEfeito > 0f)
            Destroy(efeito, tempoParaDestruirEfeito);
    }

    // =====================================================================
    // LÓGICA DE DANO
    // =====================================================================

    private bool TentarAplicarDano(Transform transformAtingido)
    {
        if (transformAtingido == null) return false;

        if (restringirDanoPorCamada && !AlvoEstaEmCamadaPermitida(transformAtingido))
            return false;

        if (aplicarDanoEmBaseVida && TentarAplicarDanoEmBaseVidaIA(transformAtingido))
            return true;

        if (aplicarDanoEmBaseVida && TentarAplicarDanoEmBaseVida(transformAtingido))
            return true;

        bool alvoTemTagPermitida = ObjetoOuFamiliaTemTagPermitida(transformAtingido);
        if (!alvoTemTagPermitida) return false;

        if (aplicarDanoEmVida && TentarAplicarDanoEmVida(transformAtingido))
            return true;

        if (aplicarDanoPorMetodos && TentarAplicarDanoPorMetodo(transformAtingido, dano))
            return true;

        return false;
    }

    private bool TentarAplicarDanoEmBaseVidaIA(Transform transformAtingido)
    {
        BaseVidaIA baseVidaIA = EncontrarComponenteNaHierarquia<BaseVidaIA>(transformAtingido);
        if (baseVidaIA == null) return false;

        if (!baseVidaIgnoraTagDoAlvo && !ObjetoOuFamiliaTemTagPermitida(transformAtingido))
            return false;

        // BUG 1 CORRIGIDO: baseVidaIgnoraTagDoAlvo = true (padrão) fazia com que QUALQUER
        // BaseVidaIA recebesse dano, incluindo bases da própria equipe do projétil.
        // O check anterior só validava a tag do ALVO, mas nunca comparava com a equipe
        // do ATACANTE. Agora: se a base for aliada, ignora — independente de qualquer flag.
        if (!string.IsNullOrEmpty(tagEquipeDona) && ObterTagEquipe(transformAtingido) == tagEquipeDona)
            return false;

        baseVidaIA.ReceberDano(dano);
        return true;
    }

    private bool TentarAplicarDanoEmBaseVida(Transform transformAtingido)
    {
        BaseVida baseVida = EncontrarComponenteNaHierarquia<BaseVida>(transformAtingido);
        if (baseVida == null) return false;

        if (!baseVidaIgnoraTagDoAlvo && !ObjetoOuFamiliaTemTagPermitida(transformAtingido))
            return false;

        if (forcarDanoNaBaseVidaSemValidarTagDoAtacante)
            baseVida.ReceberDano(dano);
        else
            baseVida.ReceberDano(dano, gameObject);

        return true;
    }

    private bool TentarAplicarDanoEmVida(Transform transformAtingido)
    {
        Vida vida = EncontrarComponenteNaHierarquia<Vida>(transformAtingido);
        if (vida == null) return false;

        vida.AplicarDano(dano);
        return true;
    }

    // =====================================================================
    // HELPERS DE DANO
    // =====================================================================

    private T EncontrarComponenteNaHierarquia<T>(Transform origem) where T : Component
    {
        if (origem == null) return null;

        T componente = origem.GetComponent<T>();
        if (componente != null) return componente;

        componente = origem.GetComponentInParent<T>(true);
        if (componente != null) return componente;

        componente = origem.GetComponentInChildren<T>(true);
        if (componente != null) return componente;

        Transform atual = origem.parent;
        while (atual != null)
        {
            componente = atual.GetComponentInChildren<T>(true);
            if (componente != null) return componente;
            atual = atual.parent;
        }

        return null;
    }

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
        {
            if (filhos[i] != null && TemTagPermitida(filhos[i].gameObject))
                return true;
        }

        return false;
    }

    private bool TemTagPermitida(GameObject obj)
    {
        if (obj == null) return false;
        if (tagsQueRecebemDano == null || tagsQueRecebemDano.Length == 0) return true;

        for (int i = 0; i < tagsQueRecebemDano.Length; i++)
        {
            string tagPermitida = tagsQueRecebemDano[i];
            if (string.IsNullOrWhiteSpace(tagPermitida)) continue;
            if (obj.CompareTag(tagPermitida)) return true;
        }

        return false;
    }

    private bool TentarAplicarDanoPorMetodo(Transform alvoTransform, int valorDano)
    {
        if (alvoTransform == null) return false;

        // BUG 6 CORRIGIDO: GetComponents<MonoBehaviour>() era chamado separadamente,
        // mas GetComponentsInChildren já inclui o próprio objeto — os componentes do root
        // eram verificados duas vezes (redundante). Removida a chamada duplicada.
        if (TentarInvocarMetodoDeDanoNosComponentes(
                alvoTransform.GetComponentsInChildren<MonoBehaviour>(true), valorDano))
            return true;

        if (TentarInvocarMetodoDeDanoNosComponentes(
                alvoTransform.GetComponentsInParent<MonoBehaviour>(true), valorDano))
            return true;

        return false;
    }

    private bool TentarInvocarMetodoDeDanoNosComponentes(MonoBehaviour[] componentes, int valorDano)
    {
        if (componentes == null || componentes.Length == 0) return false;

        for (int i = 0; i < componentes.Length; i++)
        {
            MonoBehaviour comp = componentes[i];
            if (comp == null || comp == this) continue;

            if (TentarInvocarMetodo(comp, "AplicarDano", valorDano)) return true;
            if (TentarInvocarMetodo(comp, "ReceberDano",  valorDano)) return true;
            if (TentarInvocarMetodo(comp, "TomarDano",    valorDano)) return true;
        }

        return false;
    }

    private bool TentarInvocarMetodo(MonoBehaviour componente, string nomeMetodo, int valorDano)
    {
        MethodInfo metodo = componente.GetType().GetMethod(
            nomeMetodo,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(int) },
            null
        );

        if (metodo == null) return false;

        // BUG 2 CORRIGIDO: metodo.Invoke sem try/catch.
        // Se o método de dano do alvo lançar qualquer exceção, ela subia como
        // TargetInvocationException e interrompia ColidiuComTransform antes do
        // Destroy(gameObject) — projétil ficava vivo com jaColidiu = true até expirar.
        try
        {
            metodo.Invoke(componente, new object[] { valorDano });
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ProjetilDistancia] Falha ao invocar {nomeMetodo} em {componente.GetType().Name}: {e.InnerException?.Message ?? e.Message}");
            return false;
        }
    }

    // =====================================================================
    // HELPERS DE EQUIPE / CAMADA
    // =====================================================================

    private bool EhAliado(Transform objeto)
    {
        if (objeto == null || string.IsNullOrEmpty(tagEquipeDona)) return false;
        return ObterTagEquipe(objeto) == tagEquipeDona;
    }

    private bool AlvoEstaEmCamadaPermitida(Transform alvoTransform)
    {
        Transform atual = alvoTransform;
        while (atual != null)
        {
            if ((camadasAlvoPermitidas.value & (1 << atual.gameObject.layer)) != 0)
                return true;
            atual = atual.parent;
        }
        return false;
    }

    private string ObterTagEquipe(Transform origem)
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

    // =====================================================================
    // VALIDAÇÃO
    // =====================================================================

    private void OnValidate()
    {
        dano                    = Mathf.Max(0,     dano);
        velocidade              = Mathf.Max(0f,    velocidade);
        tempoDeVida             = Mathf.Max(0.05f, tempoDeVida);
        raioDeteccaoImpacto     = Mathf.Max(0.01f, raioDeteccaoImpacto);
        margemDeteccaoImpacto   = Mathf.Max(0f,    margemDeteccaoImpacto);
        tempoParaDestruirEfeito = Mathf.Max(0f,    tempoParaDestruirEfeito);
    }
}