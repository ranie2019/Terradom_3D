using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class ColetorAI : MonoBehaviour
{
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
    [SerializeField] [Range(1f, 360f)] private float anguloCampoDeVisao = 180f;
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
    [SerializeField] private string paramCortar = "Cortar";
    [SerializeField] private string paramMorto = "Morto";

    private Rigidbody rb;

    private Transform alvoAtual;
    private Collider colliderAlvoAtual;

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
    private bool animCortarExiste;
    private bool animMortoExiste;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        vidaAtual = vidaMaxima;

        animAndandoExiste = TemParametro(paramAndando);
        animMinerarExiste = TemParametro(paramMinerar);
        animCortarExiste = TemParametro(paramCortar);
        animMortoExiste = TemParametro(paramMorto);

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        DefinirNovaDirecaoPatrulha();
    }

    private void Update()
    {
        if (estaMorto)
            return;

        AtualizarAlvo();
        ControlarEstado();
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
        float melhorDistancia = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            Transform recurso = ResolverTransformDoRecurso(hit.transform);
            if (recurso == null)
                continue;

            if (!EstaNoCampoDeVisao(hit.bounds.center))
                continue;

            float distancia = DistanciaPlano(transform.position, hit.bounds.center);

            if (distancia < melhorDistancia)
            {
                melhorDistancia = distancia;
                melhorTransform = recurso;
                melhorCollider = hit;
            }
        }

        if (melhorTransform != alvoAtual)
        {
            alvoAtual = melhorTransform;
            colliderAlvoAtual = melhorCollider;
        }
        else
        {
            colliderAlvoAtual = melhorCollider;
        }
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

        AtivarAnimacaoPorTag(alvoAtual.tag);

        if (Time.time < proximaAcao)
            return;

        proximaAcao = Time.time + tempoEntreAcoes;

        // Aqui depois você pode ligar:
        // Pedra, Árvore, inventário, dano no recurso, etc.
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

    private Transform ResolverTransformDoRecurso(Transform origem)
    {
        if (origem == null)
            return null;

        Transform atual = origem;

        while (atual != null)
        {
            if (EhTagDeRecurso(atual.tag))
                return atual;

            atual = atual.parent;
        }

        return null;
    }

    private bool EhTagDeRecurso(string tagRecebida)
    {
        return tagRecebida == tagPedra || tagRecebida == tagArvore;
    }

    private bool AlvoEhValido()
    {
        if (alvoAtual == null)
            return false;

        if (!alvoAtual.gameObject.activeInHierarchy)
            return false;

        if (!EhTagDeRecurso(alvoAtual.tag))
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
            return colliderAlvoAtual.bounds.center;

        if (alvoAtual != null)
            return alvoAtual.position;

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

    private void AtivarAnimacaoPorTag(string tagDoAlvo)
    {
        if (animator == null)
            return;

        PararAnimacoesAcao();

        if (tagDoAlvo == tagPedra)
        {
            if (animMinerarExiste)
                animator.SetBool(paramMinerar, true);
        }
        else if (tagDoAlvo == tagArvore)
        {
            if (animCortarExiste)
                animator.SetBool(paramCortar, true);
        }
    }

    private void PararAnimacoesAcao()
    {
        if (animator == null)
            return;

        if (animMinerarExiste)
            animator.SetBool(paramMinerar, false);

        if (animCortarExiste)
            animator.SetBool(paramCortar, false);
    }

    private void OlharPara(Vector3 destino)
    {
        Vector3 direcao = destino - transform.position;
        direcao.y = 0f;

        if (direcao.sqrMagnitude <= 0.001f)
            return;

        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcao.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotacaoAlvo,
            velocidadeRotacao * Time.deltaTime
        );
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