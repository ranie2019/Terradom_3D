using UnityEngine;

[DisallowMultipleComponent]
public class Ataque : MonoBehaviour
{
    [Header("Ataque")]
    [SerializeField] private float alcanceAtaque = 1.8f;
    [SerializeField] private int dano = 1;
    [SerializeField] private float cooldownAtaque = 0.8f;
    [SerializeField] private float duracaoAnimAtaque = 0.45f;
    [SerializeField] private float momentoDoHit = 0.18f;
    [SerializeField] private bool aplicarDanoDireto = true;

    [Header("Animator")]
    [SerializeField] private string parametroAtaque = "Ataque";

    [Header("Referências")]
    [SerializeField] private Animator animator;

    private Transform _alvoAtual;
    private bool _estaAtacando;
    private bool _hitAplicado;

    private float _fimAtaque;
    private float _momentoHitTempo;
    private float _proximoAtaque;

    private bool _animTemAtaque;

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        _animTemAtaque = VerificarParametroAnimator(
            animator,
            parametroAtaque,
            AnimatorControllerParameterType.Bool
        );
    }

    private void OnDisable()
    {
        _estaAtacando = false;
        _hitAplicado = false;
        AtualizarAnimator();
    }

    private void Update()
    {
        TickAtaque();
        AtualizarAnimator();
    }

    public void DefinirAlvo(Transform alvo)
    {
        _alvoAtual = alvo;
    }

    public void LimparAlvo()
    {
        _alvoAtual = null;
        _estaAtacando = false;
        _hitAplicado = false;
    }

    public bool EstaAtacandoAgora()
    {
        return _estaAtacando;
    }

    public float GetAlcanceAtaque()
    {
        return alcanceAtaque;
    }

    private void TickAtaque()
    {
        if (!AlvoValido(_alvoAtual))
        {
            _estaAtacando = false;
            return;
        }

        if (_estaAtacando)
        {
            TickAtaqueEmAndamento();
            return;
        }

        float dist = DistanciaAteAlvo(_alvoAtual);

        if (dist > alcanceAtaque)
        {
            _estaAtacando = false;
            return;
        }

        if (Time.time < _proximoAtaque)
            return;

        IniciarAtaque();
    }

    private void IniciarAtaque()
    {
        _estaAtacando = true;
        _hitAplicado = false;

        _proximoAtaque = Time.time + cooldownAtaque;
        _fimAtaque = Time.time + Mathf.Max(0.05f, duracaoAnimAtaque);
        _momentoHitTempo = Time.time + Mathf.Clamp(momentoDoHit, 0f, duracaoAnimAtaque);
    }

    private void TickAtaqueEmAndamento()
    {
        if (!AlvoValido(_alvoAtual))
        {
            _estaAtacando = false;
            return;
        }

        if (!_hitAplicado && Time.time >= _momentoHitTempo)
        {
            _hitAplicado = true;
            AplicarHit();
        }

        if (Time.time >= _fimAtaque)
            _estaAtacando = false;
    }

    private void AplicarHit()
    {
        if (!aplicarDanoDireto || !AlvoValido(_alvoAtual))
            return;

        float dist = DistanciaAteAlvo(_alvoAtual);
        if (dist > alcanceAtaque + 0.35f)
            return;

        Vida vida = _alvoAtual.GetComponent<Vida>();
        if (vida == null)
            vida = _alvoAtual.GetComponentInChildren<Vida>();

        if (vida != null)
        {
            vida.AplicarDano(dano);
            return;
        }

        // Para funcionar também com BaseVida/Basevida sem depender do tipo exato.
        _alvoAtual.SendMessage("AplicarDano", dano, SendMessageOptions.DontRequireReceiver);
        _alvoAtual.SendMessage("ReceberDano", dano, SendMessageOptions.DontRequireReceiver);
    }

    private float DistanciaAteAlvo(Transform alvo)
    {
        if (alvo == null)
            return float.MaxValue;

        Collider col = alvo.GetComponent<Collider>();
        if (col == null)
            col = alvo.GetComponentInChildren<Collider>();

        Vector3 pontoMaisProximo = col != null
            ? col.ClosestPoint(transform.position)
            : alvo.position;

        pontoMaisProximo.y = transform.position.y;

        return Vector3.Distance(transform.position, pontoMaisProximo);
    }

    private void AtualizarAnimator()
    {
        if (!animator || !_animTemAtaque)
            return;

        animator.SetBool(parametroAtaque, _estaAtacando);
    }

    private bool AlvoValido(Transform alvo)
    {
        return alvo != null && alvo.gameObject.activeInHierarchy;
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