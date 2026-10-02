using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class Torre : MonoBehaviour
{
    // =====================================================================
    // VISÃO
    // =====================================================================
    [Header("Visão")]
    public float raioVisao = 10f;

    // BUG 1 CORRIGIDO: tagsInimigos era acessado sem null-check em PegarTransformComTag.
    // Um foreach em array null lança NullReferenceException. Valor padrão adicionado.
    [SerializeField] private string[] tagsInimigos = { "Vermelho" };

    [Tooltip("Intervalo entre buscas de alvo. Antes rodava OverlapSphere todo frame.")]
    [SerializeField] private float intervaloBuscaAlvo = 0.2f;

    // =====================================================================
    // REFERÊNCIAS
    // =====================================================================
    [Header("Referências")]
    [SerializeField] private Transform baseCanhao;
    [SerializeField] private Transform pontoDisparo;

    // =====================================================================
    // DISPARO
    // =====================================================================
    [Header("Disparo")]
    [SerializeField] private GameObject prefabBala;
    [SerializeField] private float velocidadeBala  = 20f;
    [SerializeField] private float tirosPorSegundo = 2f;

    // =====================================================================
    // PRIORIDADE DE ALVO
    // =====================================================================
    [Header("Prioridade de Alvo")]
    [SerializeField] private PrioridadeAlvo prioridade = PrioridadeAlvo.MaisPerto;

    // =====================================================================
    // PATRULHA (sem alvo)
    // =====================================================================
    [Header("Patrulha (sem alvo)")]
    [SerializeField] private float velocidadeMin = 10f;
    [SerializeField] private float velocidadeMax = 30f;
    [SerializeField] private float tempoTrocaMin =  1f;
    [SerializeField] private float tempoTrocaMax =  3f;

    // =====================================================================
    // ÁUDIO
    // =====================================================================
    [Header("Áudio")]
    [Tooltip("AudioSource da torre. Se vazio, cria automaticamente no Awake.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Som tocado a cada tiro disparado.")]
    [SerializeField] private AudioClip clipDisparo;

    [Tooltip("Som tocado quando a torre é destruída.")]
    [SerializeField] private AudioClip clipDestruicao;

    [Range(0f, 1f)]
    [SerializeField] private float volumeDisparo = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float volumeDestruicao = 1f;

    [Tooltip("Variação aleatória de pitch no disparo para soar mais natural (0 = sem variação).")]
    [Range(0f, 0.3f)]
    [SerializeField] private float variacaoPitchDisparo = 0.05f;

    // =====================================================================
    // PRIVADOS
    // =====================================================================
    private Transform _alvoAtual;
    private float     _tempoProximoTiro;
    private float     _velocidadeAtual;
    private float     _direcaoAtual;
    private float     _tempoProximaTroca;
    private float     _proximaBusca;

    private readonly HashSet<Transform> _jaAvaliados = new HashSet<Transform>();

    // =====================================================================
    // UNITY
    // =====================================================================
    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 1f; // som 3D
    }

    void Start()
    {
        DefinirNovaPatrulha();
        _proximaBusca = Time.time;
    }

    void Update()
    {
        if (Time.time >= _proximaBusca)
        {
            _proximaBusca = Time.time + intervaloBuscaAlvo;
            ProcurarAlvo();
        }

        if (_alvoAtual != null && !_alvoAtual.gameObject.activeInHierarchy)
            _alvoAtual = null;

        if (_alvoAtual != null)
        {
            Mirar();
            Atirar();
        }
        else
        {
            Patrulhar();
        }
    }

    // =====================================================================
    // DETECÇÃO DE ALVO
    // =====================================================================
    void ProcurarAlvo()
    {
        Collider[] coliders = Physics.OverlapSphere(transform.position, raioVisao);
        _jaAvaliados.Clear();

        Transform melhorAlvo  = null;
        float     melhorValor = prioridade == PrioridadeAlvo.MaisLonge
            ? Mathf.NegativeInfinity
            : Mathf.Infinity;

        foreach (Collider col in coliders)
        {
            Transform raiz = PegarTransformComTag(col.transform);
            if (raiz == null) continue;

            // BUG 4 CORRIGIDO: torre podia se auto-targetar se tivesse tag inimiga
            if (raiz == transform || raiz.IsChildOf(transform)) continue;

            if (_jaAvaliados.Contains(raiz)) continue;
            _jaAvaliados.Add(raiz);

            float valor    = CalcularValorPrioridade(raiz);
            bool  ehMelhor = prioridade == PrioridadeAlvo.MaisLonge
                ? valor > melhorValor
                : valor < melhorValor;

            if (ehMelhor)
            {
                melhorValor = valor;
                melhorAlvo  = raiz;
            }
        }

        _alvoAtual = melhorAlvo;
    }

    float CalcularValorPrioridade(Transform alvo)
    {
        switch (prioridade)
        {
            case PrioridadeAlvo.MaisPerto:
            case PrioridadeAlvo.MaisLonge:
                return Vector3.Distance(transform.position, alvo.position);

            case PrioridadeAlvo.MenorVida:
            case PrioridadeAlvo.MaiorVida:
                IVida vida = alvo.GetComponentInChildren<IVida>()
                          ?? alvo.GetComponentInParent<IVida>();

                // BUG 3 CORRIGIDO: vida == null retornava Mathf.Infinity para ambos os modos.
                // No modo MaiorVida, melhorValor começa em NegativeInfinity →
                // Infinity > NegativeInfinity = true → alvo sem vida era selecionado erroneamente.
                if (vida == null)
                    return prioridade == PrioridadeAlvo.MenorVida
                        ? Mathf.Infinity          // pior valor para MenorVida → nunca selecionado
                        : Mathf.NegativeInfinity; // pior valor para MaiorVida → nunca selecionado

                return prioridade == PrioridadeAlvo.MenorVida ? vida.VidaAtual : -vida.VidaAtual;

            default:
                return Vector3.Distance(transform.position, alvo.position);
        }
    }

    Transform PegarTransformComTag(Transform origem)
    {
        // BUG 1 CORRIGIDO: foreach em array null lança NullReferenceException
        if (tagsInimigos == null || tagsInimigos.Length == 0) return null;

        Transform atual = origem;
        while (atual != null)
        {
            foreach (string tag in tagsInimigos)
            {
                // BUG 2 CORRIGIDO: CompareTag("") lança "UnityException: Tag is empty"
                // e CompareTag com tag não registrada lança "Tag: X is not defined"
                if (!string.IsNullOrWhiteSpace(tag) && atual.CompareTag(tag))
                    return atual;
            }
            atual = atual.parent;
        }
        return null;
    }

    // =====================================================================
    // MIRA
    // =====================================================================
    void Mirar()
    {
        if (baseCanhao == null || _alvoAtual == null) return;

        Vector3 direcao = _alvoAtual.position - baseCanhao.position;
        direcao.y = 0f;

        // BUG 5 CORRIGIDO: "direcao == Vector3.zero" é comparação imprecisa com floats.
        // Dois valores de ponto flutuante muito próximos de zero podem não ser exatamente zero.
        if (direcao.sqrMagnitude < 0.0001f) return;

        float angulo = Mathf.Atan2(direcao.x, direcao.z) * Mathf.Rad2Deg;
        baseCanhao.localRotation = Quaternion.Euler(0f, angulo, 0f);
    }

    // =====================================================================
    // DISPARO
    // =====================================================================
    void Atirar()
    {
        if (prefabBala == null || pontoDisparo == null) return;
        if (Time.time < _tempoProximoTiro) return;

        _tempoProximoTiro = Time.time + (1f / tirosPorSegundo);

        GameObject bala = Instantiate(prefabBala, pontoDisparo.position, pontoDisparo.rotation);
        Rigidbody rb = bala.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = pontoDisparo.forward * velocidadeBala;

        TocarSomDisparo();
    }

    // =====================================================================
    // PATRULHA
    // =====================================================================
    void Patrulhar()
    {
        if (baseCanhao == null) return;
        baseCanhao.Rotate(0f, _direcaoAtual * _velocidadeAtual * Time.deltaTime, 0f, Space.Self);
        if (Time.time >= _tempoProximaTroca)
            DefinirNovaPatrulha();
    }

    void DefinirNovaPatrulha()
    {
        _velocidadeAtual   = Random.Range(velocidadeMin, velocidadeMax);
        _direcaoAtual      = Random.value > 0.5f ? 1f : -1f;
        _tempoProximaTroca = Time.time + Random.Range(tempoTrocaMin, tempoTrocaMax);
    }

    // =====================================================================
    // ÁUDIO
    // =====================================================================

    void TocarSomDisparo()
    {
        if (audioSource == null || clipDisparo == null) return;
        audioSource.pitch = 1f + Random.Range(-variacaoPitchDisparo, variacaoPitchDisparo);
        audioSource.PlayOneShot(clipDisparo, volumeDisparo);
    }

    void OnDestroy()
    {
        // Toca o som de destruição desacoplado do AudioSource da torre,
        // pois o GameObject já está sendo destruído — AudioSource.PlayOneShot
        // pararia junto. AudioSource.PlayClipAtPoint cria um AudioSource
        // temporário na posição e toca até o fim mesmo após a destruição.
        if (clipDestruicao != null)
            AudioSource.PlayClipAtPoint(clipDestruicao, transform.position, volumeDestruicao);
    }

    // =====================================================================
    // VALIDAÇÃO
    // =====================================================================
    private void OnValidate()
    {
        raioVisao          = Mathf.Max(0.5f, raioVisao);
        intervaloBuscaAlvo = Mathf.Max(0.05f, intervaloBuscaAlvo);
        tirosPorSegundo    = Mathf.Max(0.1f, tirosPorSegundo);
        velocidadeBala     = Mathf.Max(0f,   velocidadeBala);
        velocidadeMin      = Mathf.Max(0f,   velocidadeMin);
        velocidadeMax      = Mathf.Max(velocidadeMin, velocidadeMax);
        tempoTrocaMin      = Mathf.Max(0.1f, tempoTrocaMin);
        tempoTrocaMax      = Mathf.Max(tempoTrocaMin, tempoTrocaMax);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raioVisao);

        if (_alvoAtual != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _alvoAtual.position);
            Gizmos.DrawSphere(_alvoAtual.position, 0.3f);
        }
    }
}

public enum PrioridadeAlvo { MaisPerto, MaisLonge, MenorVida, MaiorVida }
public interface IVida { float VidaAtual { get; } }