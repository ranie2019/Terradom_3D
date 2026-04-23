using UnityEngine;

[DisallowMultipleComponent]
public class Movimentacao : MonoBehaviour
{
    [Header("Velocidades (PÚBLICAS)")]
    public float velocidadeNormal = 2.4f;
    public float velocidadePerseguicao = 4.0f;

    [Header("Movimento")]
    [SerializeField] private float velocidadeRotacao = 720f;
    [SerializeField] private bool usarRigidbody = true;
    [SerializeField] private bool travarY = true;

    [Header("Animator")]
    [SerializeField] private string parametroAndando = "Andando";
    [SerializeField] private bool desligarAnimacaoDeMovimentoDuranteAtaque = true;

    [Header("Correção visual")]
    [SerializeField] private Transform raizVisual;
    [SerializeField] private Vector3 correcaoRotacaoVisualEuler = Vector3.zero;
    [SerializeField] private bool autoEncontrarRaizVisual = true;

    [Header("Referências (auto)")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Ataque ataque;

    private Vector3 _direcaoDesejada = Vector3.zero;
    private bool _estaMovendo = false;
    private bool _modoPerseguicao = false;

    private bool _animTemAndando = false;

    private Transform _raizVisualEfetiva;
    private Quaternion _rotacaoLocalVisualInicial = Quaternion.identity;

    private static readonly int HASH_ANDANDO = Animator.StringToHash("Andando");

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!ataque) ataque = GetComponent<Ataque>();

        if (animator != null)
            animator.applyRootMotion = false;

        _animTemAndando = VerificarParametroAnimator(animator, parametroAndando, AnimatorControllerParameterType.Bool);

        if (usarRigidbody && rb)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            RigidbodyConstraints c = rb.constraints;
            c &= ~RigidbodyConstraints.FreezeRotationX;
            c &= ~RigidbodyConstraints.FreezeRotationY;
            c &= ~RigidbodyConstraints.FreezeRotationZ;
            rb.constraints = c;
        }

        ResolverRaizVisual();
    }

    private void Update()
    {
        AtualizarAnimacao();
    }

    private void LateUpdate()
    {
        AplicarCorrecaoVisual();
    }

    private void FixedUpdate()
    {
        AplicarMovimento();
    }

    // =========================================================
    // API PÚBLICA
    // =========================================================
    public void SetPerseguindo(bool perseguindo)
    {
        _modoPerseguicao = perseguindo;
    }

    public float GetVelocidadeAtualMaxima()
    {
        return _modoPerseguicao ? velocidadePerseguicao : velocidadeNormal;
    }

    public void Mover(Vector3 direcao)
    {
        if (travarY)
            direcao.y = 0f;

        if (direcao.sqrMagnitude > 1f)
            direcao.Normalize();

        _direcaoDesejada = direcao;
        _estaMovendo = _direcaoDesejada.sqrMagnitude > 0.0001f;
    }

    public void Parar()
    {
        _direcaoDesejada = Vector3.zero;
        _estaMovendo = false;

        if (usarRigidbody && rb != null)
        {
            Vector3 v = rb.linearVelocity;
            v.x = 0f;
            v.z = 0f;
            rb.linearVelocity = v;
        }
    }

    public bool EstaAndando()
    {
        return _estaMovendo;
    }

    // =========================================================
    // MOVIMENTO
    // =========================================================
    private void AplicarMovimento()
    {
        if (!_estaMovendo || _direcaoDesejada.sqrMagnitude < 0.0001f)
            return;

        Vector3 direcao = _direcaoDesejada.normalized;
        float velocidade = GetVelocidadeAtualMaxima();

        Vector3 deslocamento = direcao * velocidade * Time.fixedDeltaTime;

        Rotacionar(direcao);
        MoverFisicamente(deslocamento);
    }

    private void Rotacionar(Vector3 direcao)
    {
        direcao.y = 0f;

        if (direcao.sqrMagnitude < 0.0001f)
            return;

        Quaternion alvo = Quaternion.LookRotation(direcao.normalized, Vector3.up);

        if (usarRigidbody && rb != null && !rb.isKinematic)
        {
            Quaternion novaRot = Quaternion.RotateTowards(
                rb.rotation,
                alvo,
                velocidadeRotacao * Time.fixedDeltaTime
            );

            rb.MoveRotation(novaRot);
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                alvo,
                velocidadeRotacao * Time.fixedDeltaTime
            );
        }
    }

    private void MoverFisicamente(Vector3 deslocamento)
    {
        if (usarRigidbody && rb != null && !rb.isKinematic)
        {
            Vector3 novaPos = rb.position + deslocamento;
            if (travarY)
                novaPos.y = rb.position.y;

            rb.MovePosition(novaPos);
        }
        else
        {
            Vector3 novaPos = transform.position + deslocamento;
            if (travarY)
                novaPos.y = transform.position.y;

            transform.position = novaPos;
        }
    }

    // =========================================================
    // VISUAL
    // =========================================================
    private void ResolverRaizVisual()
    {
        _raizVisualEfetiva = null;

        if (raizVisual != null && raizVisual != transform)
        {
            _raizVisualEfetiva = raizVisual;
        }
        else if (autoEncontrarRaizVisual)
        {
            if (animator != null && animator.transform != transform)
            {
                _raizVisualEfetiva = animator.transform;
            }
            else
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform filho = transform.GetChild(i);
                    if (filho != null)
                    {
                        _raizVisualEfetiva = filho;
                        break;
                    }
                }
            }
        }

        if (_raizVisualEfetiva != null)
            _rotacaoLocalVisualInicial = _raizVisualEfetiva.localRotation;
    }

    private void AplicarCorrecaoVisual()
    {
        if (_raizVisualEfetiva == null)
            return;

        _raizVisualEfetiva.localRotation =
            _rotacaoLocalVisualInicial * Quaternion.Euler(correcaoRotacaoVisualEuler);
    }

    // =========================================================
    // ANIMAÇÃO
    // =========================================================
    private void AtualizarAnimacao()
    {
        if (!animator || !_animTemAndando)
            return;

        bool bloqueadoPorAtaque = false;
        if (desligarAnimacaoDeMovimentoDuranteAtaque && ataque != null)
            bloqueadoPorAtaque = ataque.EstaAtacandoAgora();

        bool andando = _estaMovendo && !bloqueadoPorAtaque;

        if (parametroAndando == "Andando")
            animator.SetBool(HASH_ANDANDO, andando);
        else
            animator.SetBool(parametroAndando, andando);
    }

    private bool VerificarParametroAnimator(Animator anim, string nome, AnimatorControllerParameterType tipo)
    {
        if (!anim || string.IsNullOrWhiteSpace(nome))
            return false;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == nome && param.type == tipo)
                return true;
        }

        return false;
    }
}