using UnityEngine;

[DisallowMultipleComponent]
public class AtaqueDistancia : MonoBehaviour
{
    [Header("Ataque à distância")]
    [SerializeField] private float alcanceAtaque = 15f;
    [SerializeField] private int dano = 1;
    [SerializeField] private float cooldownAtaque = 1.2f;
    [SerializeField] private float duracaoAnimAtaque = 0.6f;
    [SerializeField] private float momentoDoDisparo = 0.25f;

    [Header("Disparo")]
    [SerializeField] private GameObject prefabBala;
    [SerializeField] private Transform pontoDisparo;
    [SerializeField] private float velocidadeBala = 20f;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string parametroAtaque = "Atirar";

    private Transform alvoAtual;
    private bool estaAtacando;
    private bool disparoFeito;

    private float fimAtaque;
    private float momentoDisparoAtual;
    private float proximoAtaque;

    private bool animTemAtaque;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        animTemAtaque = TemParametro(parametroAtaque);
    }

    private void Update()
    {
        BuscarAlvoSeNecessario();
        AtualizarAtaque();
        AtualizarAnimacao();
    }

    public void DefinirAlvo(Transform alvo)
    {
        alvoAtual = alvo;
    }

    public void LimparAlvo()
    {
        alvoAtual = null;
        estaAtacando = false;
        disparoFeito = false;
    }

    public bool EstaAtacandoAgora()
    {
        return estaAtacando;
    }

    public float GetAlcanceAtaque()
    {
        return alcanceAtaque;
    }

    private void BuscarAlvoSeNecessario()
    {
        if (alvoAtual != null && alvoAtual.gameObject.activeInHierarchy)
            return;

        Visao visao = GetComponent<Visao>();

        if (visao != null)
            alvoAtual = visao.GetAlvoAtual();
    }

    private void AtualizarAtaque()
    {
        if (!AlvoValido(alvoAtual))
        {
            estaAtacando = false;
            return;
        }

        float distancia = DistanciaXZ(transform.position, alvoAtual.position);

        if (distancia > alcanceAtaque)
        {
            estaAtacando = false;
            return;
        }

        OlharParaAlvo();

        if (estaAtacando)
        {
            ProcessarAtaqueEmAndamento();
            return;
        }

        if (Time.time < proximoAtaque)
            return;

        IniciarAtaque();
    }

    private void IniciarAtaque()
    {
        estaAtacando = true;
        disparoFeito = false;

        proximoAtaque = Time.time + cooldownAtaque;
        fimAtaque = Time.time + duracaoAnimAtaque;
        momentoDisparoAtual = Time.time + Mathf.Clamp(momentoDoDisparo, 0f, duracaoAnimAtaque);
    }

    private void ProcessarAtaqueEmAndamento()
    {
        if (!AlvoValido(alvoAtual))
        {
            estaAtacando = false;
            return;
        }

        if (!disparoFeito && Time.time >= momentoDisparoAtual)
        {
            disparoFeito = true;
            DispararBala();
        }

        if (Time.time >= fimAtaque)
            estaAtacando = false;
    }

    private void DispararBala()
    {
        if (prefabBala == null)
            return;

        Vector3 origem = pontoDisparo != null
            ? pontoDisparo.position
            : transform.position + transform.forward * 1.2f + Vector3.up * 1f;

        Vector3 destino = alvoAtual.position;
        destino.y = origem.y;

        Vector3 direcao = destino - origem;

        if (direcao.sqrMagnitude < 0.001f)
            direcao = transform.forward;

        Quaternion rotacao = Quaternion.LookRotation(direcao.normalized, Vector3.up);

        GameObject bala = Instantiate(prefabBala, origem, rotacao);
        bala.SetActive(true);

        ProjetilDistancia projetil = bala.GetComponent<ProjetilDistancia>();

        if (projetil != null)
        {
            projetil.Configurar(alvoAtual, dano, velocidadeBala);
            return;
        }

        Rigidbody rb = bala.GetComponent<Rigidbody>();

        if (rb != null)
            rb.linearVelocity = direcao.normalized * velocidadeBala;
    }

    private void OlharParaAlvo()
    {
        if (alvoAtual == null)
            return;

        Vector3 direcao = alvoAtual.position - transform.position;
        direcao.y = 0f;

        if (direcao.sqrMagnitude < 0.001f)
            return;

        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcao.normalized, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            rotacaoAlvo,
            720f * Time.deltaTime
        );
    }

    private void AtualizarAnimacao()
    {
        if (animator == null || !animTemAtaque)
            return;

        animator.SetBool(parametroAtaque, estaAtacando);
    }

    private bool AlvoValido(Transform alvo)
    {
        return alvo != null && alvo.gameObject.activeInHierarchy;
    }

    private bool TemParametro(string nome)
    {
        if (animator == null || string.IsNullOrWhiteSpace(nome))
            return false;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == nome)
                return true;
        }

        return false;
    }

    private float DistanciaXZ(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}