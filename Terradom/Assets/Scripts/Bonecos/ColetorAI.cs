using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class ColetorAI : MonoBehaviour
{
    private enum TipoRecurso
    {
        Nenhum,
        Pedra,
        Arvore
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

    private Rigidbody rb;

    private Transform alvoAtual;
    private Collider colliderAlvoAtual;
    private TipoRecurso tipoRecursoAtual = TipoRecurso.Nenhum;

    private Vector3 direcaoPatrulha;
    private Vector3 direcaoDesejada;

    private bool estaMovendo;
    private bool estaEmAcao;
    private bool estaMorto;

    private float proximaTrocaDirecao;
    private float proximaBuscaAlvo;
    private float proximaAcao;
    private float proximoDanoContato;

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

        // Aqui depois você pode adicionar:
        // - diminuir vida da pedra/árvore
        // - adicionar madeira/pedra no inventário
        // - mandar recurso para a base
    }

    private void MoverNaDirecao(Vector3 direcao)
    {
        direcao.y = 0f;

        if (direcao.sqrMagnitude <= 0.001f)
        {
            PararMovimento();
            return;
        }

        direcaoDesejada = direcao.normalized;
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
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
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
        Vector2 aleatorio2D = Random.insideUnitCircle.normalized;

        if (aleatorio2D == Vector2.zero)
            aleatorio2D = Vector2.right;

        direcaoPatrulha = new Vector3(aleatorio2D.x, 0f, aleatorio2D.y).normalized;
        proximaTrocaDirecao = Time.time + tempoTrocaDirecao;
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

        return TipoRecurso.Nenhum;
    }

    private bool AlvoEhValido()
    {
        if (alvoAtual == null)
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

        if (tipo == TipoRecurso.Pedra)
        {
            if (animMinerarExiste)
                animator.SetBool(paramMinerar, true);

            SetCorta(false);
            return;
        }

        if (tipo == TipoRecurso.Arvore)
        {
            if (animMinerarExiste)
                animator.SetBool(paramMinerar, false);

            SetCorta(true);
            return;
        }

        PararAnimacoesAcao();
    }

    private void SetCorta(bool ativo)
    {
        if (animCortaExiste)
            animator.SetBool(paramCorta, ativo);
    }

    private void PararAnimacoesAcao()
    {
        if (animator == null)
            return;

        if (animMinerarExiste)
            animator.SetBool(paramMinerar, false);

        SetCorta(false);
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
        if (collision == null || collision.gameObject == null)
            return;

        TentarReceberDanoPorContato(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.gameObject == null)
            return;

        TentarReceberDanoPorContato(other.gameObject);
    }

    private void TentarReceberDanoPorContato(GameObject outroObjeto)
    {
        if (outroObjeto == null || estaMorto)
            return;

        if (Time.time < proximoDanoContato)
            return;

        if (!outroObjeto.CompareTag(tagQueDaDanoNoColetor))
            return;

        proximoDanoContato = Time.time + intervaloDanoContato;
        ReceberDano(danoPorContato);
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

        if (alvoAtual != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, GetPontoAlvoAtual());
        }
    }
}