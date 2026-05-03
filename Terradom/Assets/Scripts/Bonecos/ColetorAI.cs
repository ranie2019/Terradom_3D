using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class ColetorAI : MonoBehaviour
{
    private enum TipoRecurso
    {
        Nenhum,
        Pedra,
        Arvore,
        Metal
    }

    [Header("Vida")]
    [SerializeField] private int vidaMaxima = 10;
    [SerializeField] private int vidaAtual = 10;

    [Header("Dano por contato")]
    [SerializeField] private string tagQueDaDanoNoColetor = "Inimigo";
    [SerializeField] private int danoPorContato = 1;
    [SerializeField] private float intervaloDanoContato = 0.5f;

    [Header("Movimento")]
    [SerializeField] private float velocidadeMovimento = 2.5f;
    [SerializeField] private float velocidadeRotacao = 10f;
    [SerializeField] private bool patrulharSemAlvo = true;
    [SerializeField] private float tempoTrocaDirecao = 2f;

    [Header("Desvio / anti travamento")]
    [SerializeField] private bool mudarDirecaoAoColidir = true;
    [SerializeField] private bool usarSensorAntiTravamento = true;
    [SerializeField] private LayerMask camadasObstaculo = ~0;
    [SerializeField] private bool detectarTriggersComoObstaculo = false;
    [SerializeField] private float distanciaSensorObstaculo = 1.4f;
    [SerializeField] private float raioSensorObstaculo = 0.28f;
    [SerializeField] private float alturaSensorObstaculo = 0.6f;
    [SerializeField] private float tempoManterDesvio = 1.1f;
    [SerializeField] private float intervaloMinimoEntreDesvios = 0.35f;
    [SerializeField] private float anguloMinimoDesvio = 65f;
    [SerializeField] private float anguloMaximoDesvio = 135f;
    [SerializeField] private bool ignorarChaoNaColisao = true;
    [SerializeField] private float normalMinimaParaChao = 0.55f;
    [SerializeField] private bool ignorarAlvoQuandoBaterEmObstaculo = true;
    [SerializeField] private float tempoIgnorarAlvoAposBater = 1.2f;

    [Header("Detectar quando ficou preso")]
    [SerializeField] private bool usarDeteccaoDeTravamento = true;
    [SerializeField] private float intervaloVerificarTravado = 0.7f;
    [SerializeField] private float distanciaMinimaParaConsiderarMovimento = 0.12f;
    [SerializeField] private float tempoDesvioQuandoTravado = 1.3f;
    [SerializeField] private bool limparAlvoQuandoTravado = true;

    [Header("Visão")]
    [SerializeField] private float alcanceVisao = 12f;
    [SerializeField] private float intervaloBuscaAlvo = 0.2f;
    [SerializeField] private LayerMask camadasDetectaveis = ~0;
    [SerializeField] private bool usarCampoDeVisao = false;
    [SerializeField, Range(1f, 360f)] private float anguloCampoDeVisao = 180f;
    [SerializeField] private bool travarYNaVisao = true;

    [Header("Ação")]
    [SerializeField] private float distanciaAcao = 2f;
    [SerializeField] private float distanciaSairDaAcao = 2.5f;
    [SerializeField] private float tempoEntreAcoes = 1f;

    [Header("Tags de recurso")]
    [SerializeField] private string tagPedra = "Pedra";
    [SerializeField] private string tagArvore = "Arvore";
    [SerializeField] private string tagMetal = "Metal";

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string paramAndando = "Andando";
    [SerializeField] private string paramMinerar = "Minerar";
    [SerializeField] private string paramCorta = "Corta";
    [SerializeField] private string paramMorto = "Morto";

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private string debugAlvoAtual;
    [SerializeField] private string debugTipoRecurso;
    [SerializeField] private bool debugMinerarAtivo;
    [SerializeField] private bool debugCortaAtivo;
    [SerializeField] private bool debugDesviando;
    [SerializeField] private string debugMotivoDesvio;

    private Rigidbody rb;

    private Transform alvoAtual;
    private Collider colliderAlvoAtual;
    private TipoRecurso tipoRecursoAtual = TipoRecurso.Nenhum;

    private Transform alvoIgnoradoTemporariamente;
    private float ignorarAlvoAte;

    private Vector3 direcaoPatrulha;
    private Vector3 direcaoDesejada;
    private Vector3 direcaoDesvio;
    private Vector3 ultimaPosicaoVerificacaoTravado;

    private bool estaMovendo;
    private bool estaEmAcao;
    private bool estaMorto;

    private float proximaTrocaDirecao;
    private float proximaBuscaAlvo;
    private float proximaAcao;
    private float proximoDanoContato;
    private float manterDesvioAte;
    private float proximoDesvioPermitido;
    private float proximaVerificacaoTravado;

    private bool animAndandoExiste;
    private bool animMinerarExiste;
    private bool animCortaExiste;
    private bool animMortoExiste;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        vidaAtual = vidaMaxima;

        animAndandoExiste = TemParametro(paramAndando);
        animMinerarExiste = TemParametro(paramMinerar);
        animCortaExiste = TemParametro(paramCorta);
        animMortoExiste = TemParametro(paramMorto);

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        DefinirNovaDirecaoPatrulha();

        ultimaPosicaoVerificacaoTravado = transform.position;
        proximaVerificacaoTravado = Time.time + intervaloVerificarTravado;

        if (debugLogs)
        {
            Debug.Log($"[ColetorAI] Animator encontrado: {animator != null}");
            Debug.Log($"[ColetorAI] Param Andando existe: {animAndandoExiste}");
            Debug.Log($"[ColetorAI] Param Minerar existe: {animMinerarExiste}");
            Debug.Log($"[ColetorAI] Param Corta existe: {animCortaExiste}");
            Debug.Log($"[ColetorAI] Param Morto existe: {animMortoExiste}");
        }
    }

    private void Update()
    {
        if (estaMorto)
            return;

        AtualizarAlvo();
        ControlarEstado();
        AtualizarDebugInspector();
    }

    private void FixedUpdate()
    {
        if (estaMorto)
            return;

        AplicarMovimento();
        VerificarSeFicouTravado();
    }

    private void AtualizarAlvo()
    {
        bool alvoAindaValido = AlvoEhValido();

        if (alvoAindaValido && Time.time < proximaBuscaAlvo)
            return;

        if (!alvoAindaValido)
        {
            alvoAtual = null;
            colliderAlvoAtual = null;
            tipoRecursoAtual = TipoRecurso.Nenhum;
        }

        if (Time.time < proximaBuscaAlvo)
            return;

        proximaBuscaAlvo = Time.time + intervaloBuscaAlvo;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            alcanceVisao,
            camadasDetectaveis,
            QueryTriggerInteraction.Collide
        );

        Transform melhorTransform = null;
        Collider melhorCollider = null;
        TipoRecurso melhorTipo = TipoRecurso.Nenhum;
        float melhorDistancia = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            Transform recurso = ResolverTransformDoRecurso(hit.transform, out TipoRecurso tipoEncontrado);
            if (recurso == null || tipoEncontrado == TipoRecurso.Nenhum)
                continue;

            if (AlvoEstaIgnoradoTemporariamente(recurso))
                continue;

            Vector3 ponto = hit.ClosestPoint(transform.position);

            if (!EstaNoCampoDeVisao(ponto))
                continue;

            float distancia = DistanciaPlano(transform.position, ponto);

            if (distancia < melhorDistancia)
            {
                melhorDistancia = distancia;
                melhorTransform = recurso;
                melhorCollider = hit;
                melhorTipo = tipoEncontrado;
            }
        }

        alvoAtual = melhorTransform;
        colliderAlvoAtual = melhorCollider;
        tipoRecursoAtual = melhorTipo;
    }

    private void ControlarEstado()
    {
        if (EstaDesviando())
        {
            estaEmAcao = false;
            PararAnimacoesAcao();
            MoverNaDirecao(direcaoDesvio);
            return;
        }

        if (alvoAtual == null)
        {
            estaEmAcao = false;
            PararAnimacoesAcao();
            ExecutarPatrulha();
            return;
        }

        Vector3 pontoAlvo = GetPontoAlvoAtual();
        float distancia = DistanciaPlano(transform.position, pontoAlvo);

        if (estaEmAcao)
        {
            if (distancia > distanciaSairDaAcao)
            {
                estaEmAcao = false;
                PararAnimacoesAcao();
                IrAteAlvo();
            }
            else
            {
                PararMovimento();
                OlharPara(pontoAlvo);
                ExecutarAcao();
            }

            return;
        }

        if (distancia <= distanciaAcao)
        {
            estaEmAcao = true;
            PararMovimento();
            OlharPara(pontoAlvo);
            ExecutarAcao();
        }
        else
        {
            PararAnimacoesAcao();
            IrAteAlvo();
        }
    }

    private void ExecutarPatrulha()
    {
        if (!patrulharSemAlvo)
        {
            PararMovimento();
            return;
        }

        if (Time.time >= proximaTrocaDirecao || direcaoPatrulha == Vector3.zero)
            DefinirNovaDirecaoPatrulha();

        MoverNaDirecao(direcaoPatrulha);
    }

    private void IrAteAlvo()
    {
        if (alvoAtual == null)
        {
            PararMovimento();
            return;
        }

        Vector3 pontoAlvo = GetPontoAlvoAtual();
        Vector3 direcao = pontoAlvo - transform.position;
        direcao.y = 0f;

        if (direcao.sqrMagnitude <= 0.001f)
        {
            PararMovimento();
            return;
        }

        MoverNaDirecao(direcao.normalized);
    }

    private void ExecutarAcao()
    {
        if (alvoAtual == null)
            return;

        AtivarAnimacaoPorTipo(tipoRecursoAtual);

        if (Time.time < proximaAcao)
            return;

        proximaAcao = Time.time + tempoEntreAcoes;
    }

    private void MoverNaDirecao(Vector3 direcao)
    {
        direcao.y = 0f;

        if (direcao.sqrMagnitude <= 0.001f)
        {
            PararMovimento();
            return;
        }

        Vector3 direcaoNormalizada = direcao.normalized;

        if (usarSensorAntiTravamento && !EstaDesviando() && SensorEncontrouObstaculoNaFrente(direcaoNormalizada, out RaycastHit hit))
        {
            ForcarDesvio(hit.collider, hit.point, "Sensor frontal");
            return;
        }

        direcaoDesejada = direcaoNormalizada;
        estaMovendo = true;

        if (animator != null && animAndandoExiste)
            animator.SetBool(paramAndando, true);
    }

    private void PararMovimento()
    {
        estaMovendo = false;
        direcaoDesejada = Vector3.zero;

        if (animator != null && animAndandoExiste)
            animator.SetBool(paramAndando, false);
    }

    private void AplicarMovimento()
    {
        if (!estaMovendo)
        {
            Vector3 velocidadeAtual = ObterVelocidadeRigidbody();
            DefinirVelocidadeRigidbody(new Vector3(0f, velocidadeAtual.y, 0f));
            return;
        }

        Vector3 movimento = direcaoDesejada * velocidadeMovimento * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movimento);

        if (direcaoDesejada.sqrMagnitude > 0.001f)
        {
            Quaternion rotacaoAlvo = Quaternion.LookRotation(direcaoDesejada);
            Quaternion novaRotacao = Quaternion.Slerp(
                rb.rotation,
                rotacaoAlvo,
                velocidadeRotacao * Time.fixedDeltaTime
            );

            rb.MoveRotation(novaRotacao);
        }
    }

    private void DefinirNovaDirecaoPatrulha()
    {
        Vector2 aleatorio2D = UnityEngine.Random.insideUnitCircle.normalized;

        if (aleatorio2D == Vector2.zero)
            aleatorio2D = Vector2.right;

        direcaoPatrulha = new Vector3(aleatorio2D.x, 0f, aleatorio2D.y).normalized;
        proximaTrocaDirecao = Time.time + tempoTrocaDirecao;
    }

    private bool EstaDesviando()
    {
        return Time.time < manterDesvioAte && direcaoDesvio.sqrMagnitude > 0.001f;
    }

    private bool SensorEncontrouObstaculoNaFrente(Vector3 direcao, out RaycastHit melhorHit)
    {
        melhorHit = new RaycastHit();

        if (direcao.sqrMagnitude <= 0.001f)
            return false;

        Vector3 origem = transform.position + Vector3.up * alturaSensorObstaculo;
        QueryTriggerInteraction triggerMode = detectarTriggersComoObstaculo
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        RaycastHit[] hits = Physics.SphereCastAll(
            origem,
            Mathf.Max(0.05f, raioSensorObstaculo),
            direcao.normalized,
            Mathf.Max(0.1f, distanciaSensorObstaculo),
            camadasObstaculo,
            triggerMode
        );

        bool encontrou = false;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;

            if (col == null)
                continue;

            if (EhMeuProprioCollider(col.transform))
                continue;

            if (RecursoPodeSerIgnoradoComoObstaculo(col))
                continue;

            if (ignorarChaoNaColisao && hits[i].normal.y >= normalMinimaParaChao)
                continue;

            if (hits[i].distance < menorDistancia)
            {
                menorDistancia = hits[i].distance;
                melhorHit = hits[i];
                encontrou = true;
            }
        }

        return encontrou;
    }

    private bool RecursoPodeSerIgnoradoComoObstaculo(Collider col)
    {
        if (col == null)
            return false;

        Transform recurso = ResolverTransformDoRecurso(col.transform, out TipoRecurso tipo);
        if (recurso == null || tipo == TipoRecurso.Nenhum)
            return false;

        // Recurso não deve ser tratado como obstáculo comum,
        // senão o coletor fica desviando justamente do objeto que precisa coletar.
        return true;
    }

    private void ForcarDesvio(Collider obstaculo, Vector3 pontoContato, string motivo)
    {
        if (!mudarDirecaoAoColidir)
            return;

        if (Time.time < proximoDesvioPermitido)
            return;

        if (obstaculo != null && EhMeuProprioCollider(obstaculo.transform))
            return;

        if (obstaculo != null && TentarAssumirRecursoAtingido(obstaculo))
            return;

        Vector3 direcaoBase = direcaoDesejada.sqrMagnitude > 0.001f
            ? direcaoDesejada.normalized
            : transform.forward;

        direcaoBase.y = 0f;

        if (direcaoBase.sqrMagnitude <= 0.001f)
            direcaoBase = direcaoPatrulha.sqrMagnitude > 0.001f ? direcaoPatrulha : Vector3.forward;

        direcaoBase.Normalize();

        Vector3 direcaoAfastar = transform.position - pontoContato;
        direcaoAfastar.y = 0f;

        if (direcaoAfastar.sqrMagnitude <= 0.001f)
            direcaoAfastar = -direcaoBase;

        direcaoAfastar.Normalize();

        Vector3 melhorDirecao = EscolherMelhorDirecaoDeDesvio(direcaoBase, direcaoAfastar);

        direcaoDesvio = melhorDirecao.normalized;
        direcaoPatrulha = direcaoDesvio;

        manterDesvioAte = Time.time + tempoManterDesvio;
        proximoDesvioPermitido = Time.time + intervaloMinimoEntreDesvios;
        proximaTrocaDirecao = Time.time + tempoManterDesvio;

        estaEmAcao = false;
        PararAnimacoesAcao();

        if (ignorarAlvoQuandoBaterEmObstaculo && alvoAtual != null)
        {
            alvoIgnoradoTemporariamente = alvoAtual;
            ignorarAlvoAte = Time.time + tempoIgnorarAlvoAposBater;
        }

        debugMotivoDesvio = motivo;
    }

    private Vector3 EscolherMelhorDirecaoDeDesvio(Vector3 direcaoBase, Vector3 direcaoAfastar)
    {
        float anguloMin = Mathf.Min(anguloMinimoDesvio, anguloMaximoDesvio);
        float anguloMax = Mathf.Max(anguloMinimoDesvio, anguloMaximoDesvio);

        float anguloAleatorio = UnityEngine.Random.Range(anguloMin, anguloMax);

        Vector3 esquerda = Quaternion.Euler(0f, -anguloAleatorio, 0f) * direcaoBase;
        Vector3 direita = Quaternion.Euler(0f, anguloAleatorio, 0f) * direcaoBase;

        float espacoEsquerda = MedirEspacoLivre(esquerda);
        float espacoDireita = MedirEspacoLivre(direita);

        float alinhamentoEsquerda = Vector3.Dot(esquerda.normalized, direcaoAfastar);
        float alinhamentoDireita = Vector3.Dot(direita.normalized, direcaoAfastar);

        float pontuacaoEsquerda = espacoEsquerda + alinhamentoEsquerda;
        float pontuacaoDireita = espacoDireita + alinhamentoDireita;

        if (Mathf.Abs(pontuacaoEsquerda - pontuacaoDireita) <= 0.05f)
            return UnityEngine.Random.value < 0.5f ? esquerda : direita;

        return pontuacaoEsquerda > pontuacaoDireita ? esquerda : direita;
    }

    private float MedirEspacoLivre(Vector3 direcao)
    {
        if (direcao.sqrMagnitude <= 0.001f)
            return 0f;

        Vector3 origem = transform.position + Vector3.up * alturaSensorObstaculo;
        QueryTriggerInteraction triggerMode = detectarTriggersComoObstaculo
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        if (Physics.SphereCast(
            origem,
            Mathf.Max(0.05f, raioSensorObstaculo),
            direcao.normalized,
            out RaycastHit hit,
            Mathf.Max(0.1f, distanciaSensorObstaculo * 1.5f),
            camadasObstaculo,
            triggerMode
        ))
        {
            if (hit.collider != null && !EhMeuProprioCollider(hit.collider.transform) && !RecursoPodeSerIgnoradoComoObstaculo(hit.collider))
                return hit.distance;
        }

        return distanciaSensorObstaculo * 1.5f;
    }

    private void VerificarSeFicouTravado()
    {
        if (!usarDeteccaoDeTravamento)
            return;

        if (!estaMovendo)
        {
            ultimaPosicaoVerificacaoTravado = transform.position;
            proximaVerificacaoTravado = Time.time + intervaloVerificarTravado;
            return;
        }

        if (Time.time < proximaVerificacaoTravado)
            return;

        float distanciaMovida = DistanciaPlano(ultimaPosicaoVerificacaoTravado, transform.position);
        ultimaPosicaoVerificacaoTravado = transform.position;
        proximaVerificacaoTravado = Time.time + intervaloVerificarTravado;

        if (distanciaMovida >= distanciaMinimaParaConsiderarMovimento)
            return;

        if (Time.time < proximoDesvioPermitido)
            return;

        if (limparAlvoQuandoTravado)
        {
            alvoIgnoradoTemporariamente = alvoAtual;
            ignorarAlvoAte = Time.time + tempoIgnorarAlvoAposBater;
            alvoAtual = null;
            colliderAlvoAtual = null;
            tipoRecursoAtual = TipoRecurso.Nenhum;
        }

        Vector3 direcaoBase = direcaoDesejada.sqrMagnitude > 0.001f ? direcaoDesejada : transform.forward;
        Vector3 direcaoAfastar = -direcaoBase;

        direcaoDesvio = EscolherMelhorDirecaoDeDesvio(direcaoBase.normalized, direcaoAfastar.normalized).normalized;
        direcaoPatrulha = direcaoDesvio;
        manterDesvioAte = Time.time + tempoDesvioQuandoTravado;
        proximoDesvioPermitido = Time.time + intervaloMinimoEntreDesvios;
        proximaTrocaDirecao = Time.time + tempoDesvioQuandoTravado;

        estaEmAcao = false;
        PararAnimacoesAcao();

        debugMotivoDesvio = "Travado";
    }

    private bool TentarAssumirRecursoAtingido(Collider col)
    {
        if (col == null)
            return false;

        Transform recurso = ResolverTransformDoRecurso(col.transform, out TipoRecurso tipoEncontrado);
        if (recurso == null || tipoEncontrado == TipoRecurso.Nenhum)
            return false;

        alvoAtual = recurso;
        colliderAlvoAtual = col;
        tipoRecursoAtual = tipoEncontrado;
        alvoIgnoradoTemporariamente = null;
        ignorarAlvoAte = 0f;

        Vector3 pontoAlvo = GetPontoAlvoAtual();
        float distancia = DistanciaPlano(transform.position, pontoAlvo);

        if (distancia <= distanciaSairDaAcao)
        {
            manterDesvioAte = 0f;
            direcaoDesvio = Vector3.zero;
            return true;
        }

        return false;
    }

    private Transform ResolverTransformDoRecurso(Transform origem, out TipoRecurso tipo)
    {
        tipo = TipoRecurso.Nenhum;

        if (origem == null)
            return null;

        Transform atual = origem;

        while (atual != null)
        {
            tipo = ObterTipoRecursoPorTag(atual.tag);

            if (tipo != TipoRecurso.Nenhum)
                return atual;

            atual = atual.parent;
        }

        return null;
    }

    private TipoRecurso ObterTipoRecursoPorTag(string tagRecebida)
    {
        if (tagRecebida == tagPedra)
            return TipoRecurso.Pedra;

        if (tagRecebida == tagArvore)
            return TipoRecurso.Arvore;

        if (tagRecebida == tagMetal)
            return TipoRecurso.Metal;

        return TipoRecurso.Nenhum;
    }

    private bool AlvoEhValido()
    {
        if (alvoAtual == null)
            return false;

        if (AlvoEstaIgnoradoTemporariamente(alvoAtual))
            return false;

        if (!alvoAtual.gameObject.activeInHierarchy)
            return false;

        tipoRecursoAtual = ObterTipoRecursoPorTag(alvoAtual.tag);

        if (tipoRecursoAtual == TipoRecurso.Nenhum)
            return false;

        Vector3 pontoAlvo = GetPontoAlvoAtual();
        float distancia = DistanciaPlano(transform.position, pontoAlvo);

        if (distancia > alcanceVisao)
            return false;

        if (!EstaNoCampoDeVisao(pontoAlvo))
            return false;

        return true;
    }

    private bool AlvoEstaIgnoradoTemporariamente(Transform alvo)
    {
        if (alvo == null)
            return false;

        if (Time.time >= ignorarAlvoAte)
        {
            if (alvoIgnoradoTemporariamente != null)
                alvoIgnoradoTemporariamente = null;

            return false;
        }

        return alvoIgnoradoTemporariamente == alvo;
    }

    private Vector3 GetPontoAlvoAtual()
    {
        if (colliderAlvoAtual != null)
            return colliderAlvoAtual.ClosestPoint(transform.position);

        if (alvoAtual != null)
        {
            Collider col = alvoAtual.GetComponent<Collider>();

            if (col == null)
                col = alvoAtual.GetComponentInChildren<Collider>();

            if (col != null)
                return col.ClosestPoint(transform.position);

            return alvoAtual.position;
        }

        return transform.position;
    }

    private bool EstaNoCampoDeVisao(Vector3 alvoPos)
    {
        if (!usarCampoDeVisao || anguloCampoDeVisao >= 360f)
            return true;

        Vector3 dir = alvoPos - transform.position;

        if (travarYNaVisao)
            dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return true;

        Vector3 frente = transform.forward;
        frente.y = 0f;

        if (frente.sqrMagnitude < 0.0001f)
            frente = dir.normalized;

        float cos = Vector3.Dot(frente.normalized, dir.normalized);
        float cosLimite = Mathf.Cos(Mathf.Deg2Rad * (anguloCampoDeVisao * 0.5f));

        return cos >= cosLimite;
    }

    private float DistanciaPlano(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void AtivarAnimacaoPorTipo(TipoRecurso tipo)
    {
        if (animator == null)
            return;

        if (tipo == TipoRecurso.Pedra || tipo == TipoRecurso.Metal)
        {
            if (animMinerarExiste)
                animator.SetBool(paramMinerar, true);

            if (animCortaExiste)
                animator.SetBool(paramCorta, false);

            return;
        }

        if (tipo == TipoRecurso.Arvore)
        {
            if (animMinerarExiste)
                animator.SetBool(paramMinerar, false);

            if (animCortaExiste)
                animator.SetBool(paramCorta, true);

            return;
        }

        PararAnimacoesAcao();
    }

    private void PararAnimacoesAcao()
    {
        if (animator == null)
            return;

        if (animMinerarExiste)
            animator.SetBool(paramMinerar, false);

        if (animCortaExiste)
            animator.SetBool(paramCorta, false);
    }

    private void OlharPara(Vector3 destino)
    {
        Vector3 direcao = destino - transform.position;
        direcao.y = 0f;

        if (direcao.sqrMagnitude <= 0.001f)
            return;

        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcao.normalized);

        if (rb != null)
        {
            Quaternion novaRotacao = Quaternion.Slerp(
                rb.rotation,
                rotacaoAlvo,
                velocidadeRotacao * Time.deltaTime
            );

            rb.MoveRotation(novaRotacao);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacaoAlvo,
                velocidadeRotacao * Time.deltaTime
            );
        }
    }

    public void ReceberDano(int dano)
    {
        if (estaMorto)
            return;

        vidaAtual -= dano;

        if (vidaAtual <= 0)
            Morrer();
    }

    private void Morrer()
    {
        if (estaMorto)
            return;

        estaMorto = true;
        vidaAtual = 0;

        PararMovimento();
        PararAnimacoesAcao();

        if (animator != null && animMortoExiste)
            animator.SetBool(paramMorto, true);

        Destroy(gameObject, 0.2f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        TentarReceberDanoPorContato(collision.collider.gameObject);
        TratarColisaoParaDesvio(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        TentarReceberDanoPorContato(collision.collider.gameObject);
        TratarColisaoParaDesvio(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        TentarReceberDanoPorContato(other.gameObject);
        TratarTriggerParaDesvio(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == null)
            return;

        TentarReceberDanoPorContato(other.gameObject);
        TratarTriggerParaDesvio(other);
    }

    private void TentarReceberDanoPorContato(GameObject outroObjeto)
    {
        if (outroObjeto == null || estaMorto)
            return;

        if (Time.time < proximoDanoContato)
            return;

        if (!ObjetoOuPaisTemTag(outroObjeto.transform, tagQueDaDanoNoColetor))
            return;

        proximoDanoContato = Time.time + intervaloDanoContato;
        ReceberDano(danoPorContato);
    }

    private void TratarColisaoParaDesvio(Collision collision)
    {
        if (!mudarDirecaoAoColidir)
            return;

        if (collision == null || collision.collider == null)
            return;

        if (EhMeuProprioCollider(collision.collider.transform))
            return;

        if (DeveIgnorarColisaoComChao(collision))
            return;

        if (TentarAssumirRecursoAtingido(collision.collider))
            return;

        Vector3 pontoContato = collision.contactCount > 0
            ? collision.GetContact(0).point
            : collision.collider.ClosestPoint(transform.position);

        ForcarDesvio(collision.collider, pontoContato, "Colisao");
    }

    private void TratarTriggerParaDesvio(Collider other)
    {
        if (!mudarDirecaoAoColidir)
            return;

        if (other == null)
            return;

        if (EhMeuProprioCollider(other.transform))
            return;

        if (TentarAssumirRecursoAtingido(other))
            return;

        Vector3 pontoContato = other.ClosestPoint(transform.position);
        ForcarDesvio(other, pontoContato, "Trigger");
    }

    private bool DeveIgnorarColisaoComChao(Collision collision)
    {
        if (!ignorarChaoNaColisao)
            return false;

        if (collision == null)
            return true;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contato = collision.GetContact(i);

            if (contato.normal.y >= normalMinimaParaChao)
                return true;
        }

        return false;
    }

    private bool EhMeuProprioCollider(Transform alvo)
    {
        if (alvo == null)
            return true;

        return alvo == transform || alvo.IsChildOf(transform);
    }

    private bool ObjetoOuPaisTemTag(Transform alvo, string tagProcurada)
    {
        if (alvo == null || string.IsNullOrWhiteSpace(tagProcurada))
            return false;

        Transform atual = alvo;

        while (atual != null)
        {
            if (atual.CompareTag(tagProcurada))
                return true;

            atual = atual.parent;
        }

        return false;
    }

    private Vector3 ObterVelocidadeRigidbody()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    private void DefinirVelocidadeRigidbody(Vector3 novaVelocidade)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = novaVelocidade;
#else
        rb.velocity = novaVelocidade;
#endif
    }

    private bool TemParametro(string nome)
    {
        if (animator == null || string.IsNullOrWhiteSpace(nome))
            return false;

        foreach (AnimatorControllerParameter parametro in animator.parameters)
        {
            if (parametro.name == nome)
                return true;
        }

        return false;
    }

    private void AtualizarDebugInspector()
    {
        debugAlvoAtual = alvoAtual != null ? alvoAtual.name : "Nenhum";
        debugTipoRecurso = tipoRecursoAtual.ToString();

        if (animator != null)
        {
            debugMinerarAtivo = animMinerarExiste && animator.GetBool(paramMinerar);
            debugCortaAtivo = animCortaExiste && animator.GetBool(paramCorta);
        }

        debugDesviando = EstaDesviando();
    }

    private void OnValidate()
    {
        vidaMaxima = Mathf.Max(1, vidaMaxima);
        vidaAtual = Mathf.Clamp(vidaAtual, 0, vidaMaxima);

        danoPorContato = Mathf.Max(0, danoPorContato);
        intervaloDanoContato = Mathf.Max(0.01f, intervaloDanoContato);

        velocidadeMovimento = Mathf.Max(0f, velocidadeMovimento);
        velocidadeRotacao = Mathf.Max(0.1f, velocidadeRotacao);
        tempoTrocaDirecao = Mathf.Max(0.1f, tempoTrocaDirecao);

        distanciaSensorObstaculo = Mathf.Max(0.1f, distanciaSensorObstaculo);
        raioSensorObstaculo = Mathf.Max(0.05f, raioSensorObstaculo);
        alturaSensorObstaculo = Mathf.Max(0f, alturaSensorObstaculo);
        tempoManterDesvio = Mathf.Max(0.1f, tempoManterDesvio);
        intervaloMinimoEntreDesvios = Mathf.Max(0.05f, intervaloMinimoEntreDesvios);
        anguloMinimoDesvio = Mathf.Clamp(anguloMinimoDesvio, 5f, 175f);
        anguloMaximoDesvio = Mathf.Clamp(anguloMaximoDesvio, anguloMinimoDesvio, 180f);
        normalMinimaParaChao = Mathf.Clamp01(normalMinimaParaChao);
        tempoIgnorarAlvoAposBater = Mathf.Max(0f, tempoIgnorarAlvoAposBater);

        intervaloVerificarTravado = Mathf.Max(0.1f, intervaloVerificarTravado);
        distanciaMinimaParaConsiderarMovimento = Mathf.Max(0.01f, distanciaMinimaParaConsiderarMovimento);
        tempoDesvioQuandoTravado = Mathf.Max(0.1f, tempoDesvioQuandoTravado);

        alcanceVisao = Mathf.Max(0.1f, alcanceVisao);
        intervaloBuscaAlvo = Mathf.Max(0.02f, intervaloBuscaAlvo);
        distanciaAcao = Mathf.Max(0.1f, distanciaAcao);
        distanciaSairDaAcao = Mathf.Max(distanciaAcao, distanciaSairDaAcao);
        tempoEntreAcoes = Mathf.Max(0.01f, tempoEntreAcoes);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alcanceVisao);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanciaAcao);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaSairDaAcao);

        if (usarCampoDeVisao)
        {
            Vector3 frente = transform.forward;
            frente.y = 0f;

            if (frente.sqrMagnitude < 0.0001f)
                frente = Vector3.forward;

            Quaternion rotEsq = Quaternion.Euler(0f, -anguloCampoDeVisao * 0.5f, 0f);
            Quaternion rotDir = Quaternion.Euler(0f, anguloCampoDeVisao * 0.5f, 0f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + rotEsq * frente.normalized * alcanceVisao);
            Gizmos.DrawLine(transform.position, transform.position + rotDir * frente.normalized * alcanceVisao);
        }

        if (usarSensorAntiTravamento)
        {
            Vector3 origemSensor = transform.position + Vector3.up * alturaSensorObstaculo;
            Vector3 direcaoSensor = direcaoDesejada.sqrMagnitude > 0.001f ? direcaoDesejada : transform.forward;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(origemSensor, origemSensor + direcaoSensor.normalized * distanciaSensorObstaculo);
            Gizmos.DrawWireSphere(origemSensor + direcaoSensor.normalized * distanciaSensorObstaculo, raioSensorObstaculo);
        }

        if (EstaDesviando())
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + direcaoDesvio.normalized * 2.5f);
        }

        if (alvoAtual != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, GetPontoAlvoAtual());
        }
    }
}
