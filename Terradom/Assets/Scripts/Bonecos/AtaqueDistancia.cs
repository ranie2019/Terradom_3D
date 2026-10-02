using UnityEngine;

[DisallowMultipleComponent]
public class AtaqueDistancia : MonoBehaviour
{
    [Header("Ataque à distância")]
    [SerializeField] private float alcanceAtaque    = 15f;
    [SerializeField] private int   dano             = 1;
    [SerializeField] private float cooldownAtaque   = 1.2f;
    [SerializeField] private float duracaoAnimAtaque = 0.6f;
    [SerializeField] private float momentoDoDisparo = 0.25f;

    [Header("Disparo")]
    [SerializeField] private GameObject prefabBala;
    [SerializeField] private Transform  pontoDisparo;
    [SerializeField] private float      velocidadeBala = 20f;

    [Header("Audio de disparo")]
    [SerializeField] private AudioSource audioSourceDisparo;
    [SerializeField] private AudioClip audioClipDisparo;
    [SerializeField, Min(1f)] private float distanciaMaximaAudioDisparo = 70f;

    [Header("Audio de caminhada")]
    [SerializeField] private AudioSource audioSourceCaminhada;
    [SerializeField] private AudioClip audioClipCaminhada;
    [SerializeField, Min(1f)] private float distanciaMaximaAudioCaminhada = 25f;
    [SerializeField, Min(0f)] private float velocidadeMinimaAudioCaminhada = 0.05f;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string   parametroAtaque = "Atirar";

    private Transform alvoAtual;
    private bool      estaAtacando;
    private bool      disparoFeito;
    private float     fimAtaque;
    private float     momentoDisparoAtual;
    private float     proximoAtaque;
    private bool      animTemAtaque;

    // FIX: Visao era buscada via GetComponent<Visao>() em Update toda vez que
    // não havia alvo — chamada cara executada continuamente. Agora é cacheada
    // em Awake e reutilizada.
    private Visao _visaoCache;
    private Movimentacao movimentacao;
    private Vector3 ultimaPosicaoAudio;
    private float distanciaPercorridaAudio;
    private float tempoAmostraAudio;
    private const float intervaloAmostraAudio = 0.12f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;

        animTemAtaque = TemParametro(parametroAtaque);

        // FIX: cache do componente Visao
        _visaoCache = GetComponent<Visao>();
        movimentacao = GetComponent<Movimentacao>();
        ultimaPosicaoAudio = transform.position;
        PrepararAudio();
    }

    private void Update()
    {
        BuscarAlvoSeNecessario();
        AtualizarAtaque();
        AtualizarAnimacao();
        AtualizarAudioCaminhada();
    }

    private void OnDisable()
    {
        if (audioSourceCaminhada != null && audioSourceCaminhada.isPlaying)
            audioSourceCaminhada.Stop();
    }

    public void DefinirAlvo(Transform alvo)
    {
        alvoAtual = alvo;
    }

    public void LimparAlvo()
    {
        alvoAtual    = null;
        estaAtacando = false;
        disparoFeito = false;
    }

    public bool EstaAtacandoAgora() => estaAtacando;
    public float GetAlcanceAtaque() => alcanceAtaque;

    private void BuscarAlvoSeNecessario()
    {
        if (alvoAtual != null && alvoAtual.gameObject.activeInHierarchy) return;

        // FIX: usa o cache em vez de GetComponent<Visao>() a cada frame
        if (_visaoCache != null)
            alvoAtual = _visaoCache.GetAlvoAtual();
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

        if (Time.time < proximoAtaque) return;
        IniciarAtaque();
    }

    private void IniciarAtaque()
    {
        estaAtacando        = true;
        disparoFeito        = false;
        proximoAtaque       = Time.time + cooldownAtaque;
        fimAtaque           = Time.time + duracaoAnimAtaque;
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
        if (prefabBala == null) return;

        Vector3 origem = pontoDisparo != null
            ? pontoDisparo.position
            : transform.position + transform.forward * 1.2f + Vector3.up * 1f;

        Vector3 destino = alvoAtual.position;
        destino.y = origem.y;

        Vector3 direcao = destino - origem;
        if (direcao.sqrMagnitude < 0.001f)
            direcao = transform.forward;

        Quaternion rotacao = Quaternion.LookRotation(direcao.normalized, Vector3.up);
        GameObject bala    = Instantiate(prefabBala, origem, rotacao);
        bala.SetActive(true);
        TocarAudioDisparo();

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

    private void PrepararAudio()
    {
        if (audioSourceDisparo == null)
        {
            AudioSource existente = GetComponent<AudioSource>();
            if (existente != audioSourceCaminhada)
                audioSourceDisparo = existente;
        }

        if (audioSourceDisparo == null)
            audioSourceDisparo = gameObject.AddComponent<AudioSource>();

        if (audioSourceCaminhada == null || audioSourceCaminhada == audioSourceDisparo)
            audioSourceCaminhada = gameObject.AddComponent<AudioSource>();

        ConfigurarAudio3D(audioSourceDisparo, distanciaMaximaAudioDisparo);
        audioSourceDisparo.loop = false;
        audioSourceDisparo.resource = audioClipDisparo;

        ConfigurarAudio3D(audioSourceCaminhada, distanciaMaximaAudioCaminhada);
        audioSourceCaminhada.loop = true;
        audioSourceCaminhada.resource = audioClipCaminhada;
    }

    private void ConfigurarAudio3D(AudioSource fonte, float distanciaMaxima)
    {
        if (fonte == null)
            return;

        fonte.playOnAwake = false;
        fonte.spatialBlend = 1f;
        fonte.rolloffMode = AudioRolloffMode.Linear;
        fonte.maxDistance = Mathf.Max(fonte.minDistance + 0.1f, distanciaMaxima);
        fonte.dopplerLevel = 0f;
    }

    private void TocarAudioDisparo()
    {
        if (audioSourceDisparo == null || !audioSourceDisparo.isActiveAndEnabled || audioClipDisparo == null)
            return;

        audioSourceDisparo.PlayOneShot(audioClipDisparo);
    }

    private void AtualizarAudioCaminhada()
    {
        Vector3 posicaoAtual = transform.position;
        Vector3 deslocamento = posicaoAtual - ultimaPosicaoAudio;
        deslocamento.y = 0f;
        distanciaPercorridaAudio += deslocamento.magnitude;
        ultimaPosicaoAudio = posicaoAtual;
        tempoAmostraAudio += Time.deltaTime;

        if (tempoAmostraAudio < intervaloAmostraAudio)
            return;

        float velocidadeReal = distanciaPercorridaAudio / Mathf.Max(tempoAmostraAudio, 0.0001f);
        bool estaAndando = (movimentacao == null || movimentacao.EstaAndando())
            && velocidadeReal >= velocidadeMinimaAudioCaminhada;

        if (audioSourceCaminhada != null)
        {
            if (estaAndando && audioClipCaminhada != null)
            {
                if (!audioSourceCaminhada.isPlaying)
                    audioSourceCaminhada.Play();
            }
            else if (audioSourceCaminhada.isPlaying)
            {
                audioSourceCaminhada.Stop();
            }
        }

        distanciaPercorridaAudio = 0f;
        tempoAmostraAudio = 0f;
    }

    private void OlharParaAlvo()
    {
        if (alvoAtual == null) return;
        Vector3 direcao = alvoAtual.position - transform.position;
        direcao.y = 0f;
        if (direcao.sqrMagnitude < 0.001f) return;
        Quaternion rotacaoAlvo = Quaternion.LookRotation(direcao.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            rotacaoAlvo,
            720f * Time.deltaTime
        );
    }

    private void AtualizarAnimacao()
    {
        if (animator == null || !animTemAtaque) return;
        animator.SetBool(parametroAtaque, estaAtacando);
    }

    private bool AlvoValido(Transform alvo)
        => alvo != null && alvo.gameObject.activeInHierarchy;

    private bool TemParametro(string nome)
    {
        if (animator == null || string.IsNullOrWhiteSpace(nome)) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == nome) return true;
        return false;
    }

    private float DistanciaXZ(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}