using System;
using System.Collections.Generic;
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

    // Buffer COMPARTILHADO entre todos os projeteis (uso sincrono na main thread).
    // Antes cada bala alocava seu proprio array de 32 RaycastHit.
    private const int CapBufferHits = 1024;
    private static RaycastHit[] bufferHitsImpacto = new RaycastHit[32];

    // Cache da reflection dos metodos de dano, por tipo de componente.
    private static readonly string[] NomesMetodoDano = { "AplicarDano", "ReceberDano", "TomarDano" };
    private static readonly Dictionary<Type, MethodInfo[]> metodosDanoPorTipo =
        new Dictionary<Type, MethodInfo[]>();

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
    private bool encerrando;

    private Vector3 posicaoImpacto;
    private Vector3 normalImpacto;
    private bool houveImpacto;

    public string TagEquipeDona => tagEquipeDona;

    // =====================================================================
    // CONFIGURAR
    // =====================================================================

    /// <summary>
    /// Define apenas QUEM disparou (equipe, tanque dono e quem ignorar na colisao),
    /// sem mexer em direcao, dano ou velocidade. Use em quem instancia a bala sem chamar
    /// Configurar(...) completo (ex.: Torre). Sem isso a bala usa as tags do prefab e nao
    /// tem protecao contra fogo amigo.
    /// </summary>
    public void DefinirDono(Transform novoDono)
    {
        origemDisparo = novoDono;
        tankDono = novoDono != null ? novoDono.GetComponentInParent<TankLeve>() : null;
        tagEquipeDona = ObterTagEquipe(novoDono);

        AplicarTagsPelaEquipeDona();
    }

    public void Configurar(Transform novoAlvo, int novoDano, float novaVelocidade)
    {
        alvo       = novoAlvo;
        dano       = novoDano;
        velocidade = novaVelocidade;

        // Sem dono informado, infere a equipe pela tag oposta do alvo
        // (alvo "Vermelho" -> dono provavelmente "Azul"). Preserva o comportamento original.
        if (novoAlvo != null)
        {
            string tagAlvo = ObterTagEquipe(novoAlvo);
            if (tagAlvo == "Vermelho")
            {
                tagEquipeDona = "Azul";
                tagsQueRecebemDano = new[] { "Vermelho", "Verde" };
            }
            else if (tagAlvo == "Azul")
            {
                tagEquipeDona = "Vermelho";
                tagsQueRecebemDano = new[] { "Azul", "Verde" };
            }
            else if (tagAlvo == "Verde")
            {
                // Dono desconhecido: nao da para proteger aliados, mas o alvo Verde
                // precisa poder receber dano (antes ficava com a tag padrao "Vermelho").
                tagEquipeDona = string.Empty;
                tagsQueRecebemDano = new[] { "Verde" };
            }
        }

        DefinirDirecaoInicial();
    }

    public void Configurar(Transform novoAlvo, int novoDano, float novaVelocidade,
                           Transform novoDono, Vector3 pontoMira)
    {
        alvo       = novoAlvo;
        dano       = novoDano;
        velocidade = novaVelocidade;

        DefinirDono(novoDono);

        distanciaDoDisparo = novoDono != null && novoAlvo != null
            ? Vector3.Distance(novoDono.position, novoAlvo.position)
            : 0f;

        DefinirDirecaoInicial(pontoMira);
    }

    public void Configurar(Transform novoAlvo, int novoDano, float novaVelocidade,
                           Transform novoDono, Vector3 pontoMira, LayerMask novasCamadasAlvo)
    {
        Configurar(novoAlvo, novoDano, novaVelocidade, novoDono, pontoMira);
        camadasAlvoPermitidas   = novasCamadasAlvo;
        restringirDanoPorCamada = true;
    }

    private void AplicarTagsPelaEquipeDona()
    {
        if      (tagEquipeDona == "Azul")     tagsQueRecebemDano = new[] { "Vermelho", "Verde" };
        else if (tagEquipeDona == "Vermelho") tagsQueRecebemDano = new[] { "Azul",     "Verde" };
        else if (tagEquipeDona == "Verde")    tagsQueRecebemDano = new[] { "Azul",     "Vermelho" };
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

    private void OnApplicationQuit()
    {
        encerrando = true;
    }

    // =====================================================================
    // DIRECAO INICIAL
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

        rb.isKinematic            = true;
        rb.useGravity             = false;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    // =====================================================================
    // MOVIMENTO E DETECCAO
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

            // hit.point vem zerado quando o projetil comeca dentro de um collider.
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
        Vector3 dirNormalizada = direcao.normalized;

        while (true)
        {
            int quantidade = Physics.SphereCastNonAlloc(
                origem, raio, dirNormalizada,
                bufferHitsImpacto, distanciaTotal,
                camadasDeImpacto, triggerMode
            );

            bool  encontrou      = false;
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

            // Buffer nao encheu: o resultado e completo.
            if (quantidade < bufferHitsImpacto.Length)
                return encontrou;

            // Encheu e ja esta no teto: devolve o melhor encontrado
            // (antes o loop nunca terminava ao atingir o teto).
            if (bufferHitsImpacto.Length >= CapBufferHits)
                return encontrou;

            // Encheu: pode haver hits ignorados. Dobra (ate o teto) e tenta de novo.
            Array.Resize(ref bufferHitsImpacto, Mathf.Min(bufferHitsImpacto.Length * 2, CapBufferHits));
        }
    }

    // =====================================================================
    // COLISAO
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
            ContactPoint contato = collision.GetContact(0);
            posicaoImpacto = contato.point;
            normalImpacto  = contato.normal;
            houveImpacto   = true;
        }

        ColidiuComCollider(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (ColisorEhDoProprioProjetil(other) || EhAliado(other.transform)) return;

        // Guarda o ponto do impacto para o efeito (antes triggers nunca tocavam efeito).
        // bounds.ClosestPoint e seguro para qualquer collider; Collider.ClosestPoint
        // falha em MeshCollider nao convexo.
        if (!houveImpacto)
        {
            posicaoImpacto = other.bounds.ClosestPoint(transform.position);
            normalImpacto  = -direcaoInicial;
            houveImpacto   = true;
        }

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
    // EFEITO AO SER DESTRUIDO
    // =====================================================================

    private void OnDestroy()
    {
        if (!resultadoRegistrado && tankDono != null)
            RegistrarResultadoDisparo(false);

        // Nao cria objetos ao sair do Play Mode nem ao descarregar a cena
        // (evita o aviso "Some objects were not cleaned up when closing the scene").
        if (encerrando || !gameObject.scene.isLoaded)
            return;

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
    // LOGICA DE DANO
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

        // Base aliada nunca recebe dano, independente das flags.
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

        // GetComponentsInChildren ja inclui o proprio objeto.
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

            MethodInfo[] metodos = ObterMetodosDeDano(comp.GetType());
            for (int m = 0; m < metodos.Length; m++)
            {
                if (metodos[m] != null && TentarInvocarMetodo(comp, metodos[m], valorDano))
                    return true;
            }
        }

        return false;
    }

    // Reflection resolvida uma vez por tipo (antes: GetMethod x3 por componente a cada impacto).
    private static MethodInfo[] ObterMetodosDeDano(Type tipo)
    {
        if (metodosDanoPorTipo.TryGetValue(tipo, out MethodInfo[] cache))
            return cache;

        MethodInfo[] metodos = new MethodInfo[NomesMetodoDano.Length];
        for (int i = 0; i < NomesMetodoDano.Length; i++)
        {
            metodos[i] = tipo.GetMethod(
                NomesMetodoDano[i],
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(int) },
                null
            );
        }

        metodosDanoPorTipo[tipo] = metodos;
        return metodos;
    }

    private bool TentarInvocarMetodo(MonoBehaviour componente, MethodInfo metodo, int valorDano)
    {
        // Excecao no metodo do alvo nao pode impedir o Destroy do projetil.
        try
        {
            metodo.Invoke(componente, new object[] { valorDano });
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ProjetilDistancia] Falha ao invocar {metodo.Name} em {componente.GetType().Name}: {e.InnerException?.Message ?? e.Message}");
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
    // VALIDACAO
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