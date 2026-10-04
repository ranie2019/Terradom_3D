using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Torre terrestre: detecta inimigos em SOLO (ignora a layer dos avioes) e atira
/// quando a canhao estiver apontado para o alvo.
/// A equipe da torre vem da tag da hierarquia (Azul/Vermelho/Verde); outras equipes sao inimigas.
/// </summary>
[DisallowMultipleComponent]
public class Torre : MonoBehaviour
{
    private static readonly string[] TagsDeEquipe = { "Azul", "Vermelho", "Verde" };

    // =====================================================================
    // VISAO
    // =====================================================================
    [Header("Visão")]
    public float raioVisao = 10f;

    [Tooltip("Descobre a equipe da torre pela tag da hierarquia e ataca qualquer outra equipe.")]
    [SerializeField] private bool detectarEquipeAutomaticamente = true;

    [Tooltip("Tags extras de inimigos (ex.: 'Inimigo'). Se a equipe automatica nao for encontrada, vale tambem para tags de equipe.")]
    [SerializeField] private string[] tagsInimigos = { "Vermelho" };

    [Tooltip("Torre de solo: nao atira em objetos da layer de avioes.")]
    [SerializeField] private bool ignorarAvioes = true;
    [SerializeField] private string nomeLayerAviao = "Aviao";

    [Tooltip("Intervalo entre buscas de alvo.")]
    [SerializeField] private float intervaloBuscaAlvo = 0.2f;

    [Tooltip("Folga (em %) para o alvo sair do raio antes de ser perdido.")]
    [SerializeField, Range(0f, 0.5f)] private float histereseSaida = 0.1f;

    // =====================================================================
    // REFERENCIAS
    // =====================================================================
    [Header("Referências")]
    [SerializeField] private Transform baseCanhao;
    [SerializeField] private Transform pontoDisparo;

    // =====================================================================
    // MIRA
    // =====================================================================
    [Header("Mira")]
    [Tooltip("Graus por segundo.")]
    [SerializeField] private float velocidadeGiro = 360f;
    [Tooltip("So atira quando o canhao estiver a ate X graus do alvo.")]
    [SerializeField] private float toleranciaMira = 8f;

    // =====================================================================
    // DISPARO
    // =====================================================================
    [Header("Disparo")]
    [SerializeField] private GameObject prefabBala;
    [SerializeField] private float velocidadeBala = 20f;
    [SerializeField] private float tirosPorSegundo = 2f;
    [Tooltip("Destroi a bala apos X segundos (0 = desligado; a bala se encarrega).")]
    [SerializeField] private float tempoVidaBala = 0f;

    // =====================================================================
    // PRIORIDADE DE ALVO
    // =====================================================================
    [Header("Prioridade de Alvo")]
    [SerializeField] private PrioridadeAlvo prioridade = PrioridadeAlvo.MaisPerto;

    // =====================================================================
    // PATRULHA
    // =====================================================================
    [Header("Patrulha (sem alvo)")]
    [SerializeField] private float velocidadeMin = 10f;
    [SerializeField] private float velocidadeMax = 30f;
    [SerializeField] private float tempoTrocaMin = 1f;
    [SerializeField] private float tempoTrocaMax = 3f;

    // =====================================================================
    // AUDIO
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

    [Tooltip("Variação aleatória de pitch no disparo (0 = sem variação).")]
    [Range(0f, 0.3f)]
    [SerializeField] private float variacaoPitchDisparo = 0.05f;

    [Tooltip("Ate esta distancia da camera o som toca com volume cheio. " +
             "Em jogo de estrategia a camera fica longe: com o padrao do Unity (1) o som some.")]
    [SerializeField] private float distanciaMinimaAudio = 30f;
    [Tooltip("Alem desta distancia o som fica mudo.")]
    [SerializeField] private float distanciaMaximaAudio = 200f;

    // =====================================================================
    // PRIVADOS
    // =====================================================================
    private Transform _alvoAtual;
    private float _tempoProximoTiro;
    private float _velocidadeAtual;
    private float _direcaoAtual;
    private float _tempoProximaTroca;
    private float _proximaBusca;
    private float _erroMira = 180f;
    private int _mascaraBusca = ~0;
    private bool _encerrando;

    private readonly Collider[] _buffer = new Collider[128];
    private readonly HashSet<Transform> _jaAvaliados = new HashSet<Transform>();

    // =====================================================================
    // UNITY
    // =====================================================================
    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        ConfigurarAudio3D(audioSource, distanciaMinimaAudio, distanciaMaximaAudio);
    }

    private void Start()
    {
        _mascaraBusca = ~0;
        if (ignorarAvioes)
        {
            int layerAviao = LayerMask.NameToLayer(nomeLayerAviao);
            if (layerAviao >= 0)
                _mascaraBusca &= ~(1 << layerAviao);
        }

        if (detectarEquipeAutomaticamente && string.IsNullOrEmpty(ObterEquipe(transform)))
            Debug.LogWarning($"[Torre] '{name}': sem tag de equipe (Azul/Vermelho/Verde) na torre ou nos pais. " +
                             "Usando a lista 'Tags Inimigos'.", this);

        if (prefabBala == null || pontoDisparo == null)
            Debug.LogWarning($"[Torre] '{name}': 'Prefab Bala' ou 'Ponto Disparo' nao atribuido. A torre nao vai atirar.", this);

        DefinirNovaPatrulha();
        _proximaBusca = Time.time + Random.Range(0f, intervaloBuscaAlvo);
    }

    private void Update()
    {
        if (Time.time >= _proximaBusca)
        {
            _proximaBusca = Time.time + intervaloBuscaAlvo;
            ProcurarAlvo();
        }

        if (_alvoAtual != null && !AlvoValido(_alvoAtual))
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

    private void OnApplicationQuit()
    {
        _encerrando = true;
    }

    private void OnDestroy()
    {
        // Nao toca ao sair do jogo nem ao descarregar a cena (troca de cena / Game Over).
        if (_encerrando || !Application.isPlaying || !gameObject.scene.isLoaded)
            return;

        // O AudioSource da torre morre junto com ela, entao o som toca em um
        // objeto temporario (com as mesmas distancias 3D configuradas acima).
        if (clipDestruicao != null)
            TocarClipTemporario(transform.position, clipDestruicao, volumeDestruicao,
                                distanciaMinimaAudio, distanciaMaximaAudio);
    }

    // =====================================================================
    // DETECCAO DE ALVO
    // =====================================================================
    private bool AlvoValido(Transform alvo)
    {
        if (alvo == null || !alvo.gameObject.activeInHierarchy)
            return false;

        float limite = raioVisao * (1f + histereseSaida);
        return (alvo.position - transform.position).sqrMagnitude <= limite * limite;
    }

    private void ProcurarAlvo()
    {
        int total = Physics.OverlapSphereNonAlloc(transform.position, raioVisao, _buffer, _mascaraBusca);
        _jaAvaliados.Clear();

        string minhaEquipe = detectarEquipeAutomaticamente ? ObterEquipe(transform) : null;

        Transform melhorAlvo = null;
        bool maiorEMelhor = prioridade == PrioridadeAlvo.MaisLonge;
        float melhorValor = maiorEMelhor ? float.NegativeInfinity : float.PositiveInfinity;

        for (int i = 0; i < total; i++)
        {
            Collider col = _buffer[i];
            if (col == null) continue;

            Transform raiz = PegarRaizInimiga(col.transform, minhaEquipe);
            if (raiz == null) continue;

            // A torre nunca se auto-seleciona.
            if (raiz == transform || raiz.IsChildOf(transform)) continue;

            if (!_jaAvaliados.Add(raiz)) continue;

            float valor = CalcularValorPrioridade(raiz);
            bool ehMelhor = maiorEMelhor ? valor > melhorValor : valor < melhorValor;

            if (ehMelhor)
            {
                melhorValor = valor;
                melhorAlvo = raiz;
            }
        }

        _alvoAtual = melhorAlvo;
    }

    private float CalcularValorPrioridade(Transform alvo)
    {
        switch (prioridade)
        {
            case PrioridadeAlvo.MenorVida:
            case PrioridadeAlvo.MaiorVida:
                IVida vida = alvo.GetComponentInChildren<IVida>();
                if (vida == null) vida = alvo.GetComponentInParent<IVida>();

                // Sem IVida: pior valor FINITO, para ainda poder ser escolhido se for o unico alvo.
                if (vida == null)
                    return prioridade == PrioridadeAlvo.MenorVida ? 1e9f : -1e9f;

                return prioridade == PrioridadeAlvo.MenorVida ? vida.VidaAtual : -vida.VidaAtual;

            default: // MaisPerto / MaisLonge
                return (transform.position - alvo.position).sqrMagnitude;
        }
    }

    /// <summary>
    /// Sobe a hierarquia. Retorna o transform inimigo (tag de equipe diferente da minha,
    /// ou tag extra da lista). Para na primeira tag de equipe encontrada.
    /// </summary>
    private Transform PegarRaizInimiga(Transform origem, string minhaEquipe)
    {
        Transform atual = origem;
        while (atual != null)
        {
            if (EhTagDeEquipe(atual))
            {
                string equipe = atual.tag;

                if (!string.IsNullOrEmpty(minhaEquipe))
                    return equipe != minhaEquipe ? atual : null;

                return TagNaLista(atual) ? atual : null;
            }

            // Tags que nao sao de equipe (ex.: "Inimigo") entram pela lista.
            if (TagNaLista(atual))
                return atual;

            atual = atual.parent;
        }
        return null;
    }

    private bool TagNaLista(Transform t)
    {
        if (tagsInimigos == null) return false;
        for (int i = 0; i < tagsInimigos.Length; i++)
        {
            string tag = tagsInimigos[i];
            if (string.IsNullOrWhiteSpace(tag)) continue;
            if (t.CompareTag(tag)) return true;
        }
        return false;
    }

    private static bool EhTagDeEquipe(Transform t)
    {
        for (int i = 0; i < TagsDeEquipe.Length; i++)
            if (t.CompareTag(TagsDeEquipe[i])) return true;
        return false;
    }

    private static string ObterEquipe(Transform origem)
    {
        Transform atual = origem;
        while (atual != null)
        {
            if (EhTagDeEquipe(atual)) return atual.tag;
            atual = atual.parent;
        }
        return null;
    }

    // =====================================================================
    // MIRA
    // =====================================================================
    private void Mirar()
    {
        if (baseCanhao == null || _alvoAtual == null) return;

        Vector3 dirMundo = _alvoAtual.position - baseCanhao.position;
        dirMundo.y = 0f;
        if (dirMundo.sqrMagnitude < 0.0001f) return;

        // O angulo tem que ser calculado no espaco LOCAL do pai: a torre pode ter
        // sido posicionada com qualquer rotacao (antes usava o angulo do mundo).
        Transform pai = baseCanhao.parent;
        Vector3 dirLocal = pai != null ? pai.InverseTransformDirection(dirMundo) : dirMundo;

        float angulo = Mathf.Atan2(dirLocal.x, dirLocal.z) * Mathf.Rad2Deg;
        Quaternion rotAlvo = Quaternion.Euler(0f, angulo, 0f);

        baseCanhao.localRotation = Quaternion.RotateTowards(
            baseCanhao.localRotation, rotAlvo, velocidadeGiro * Time.deltaTime);

        _erroMira = Quaternion.Angle(baseCanhao.localRotation, rotAlvo);
    }

    // =====================================================================
    // DISPARO
    // =====================================================================
    private void Atirar()
    {
        if (prefabBala == null || pontoDisparo == null) return;
        if (Time.time < _tempoProximoTiro) return;
        if (_erroMira > toleranciaMira) return; // ainda girando para o alvo

        _tempoProximoTiro = Time.time + (1f / tirosPorSegundo);

        GameObject bala = Instantiate(prefabBala, pontoDisparo.position, pontoDisparo.rotation);

        Rigidbody rb = bala.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
            rb.linearVelocity = pontoDisparo.forward * velocidadeBala;

        if (tempoVidaBala > 0f)
            Destroy(bala, tempoVidaBala);

        TocarSomDisparo();
    }

    // =====================================================================
    // PATRULHA
    // =====================================================================
    private void Patrulhar()
    {
        _erroMira = 180f;

        if (baseCanhao == null) return;
        baseCanhao.Rotate(0f, _direcaoAtual * _velocidadeAtual * Time.deltaTime, 0f, Space.Self);
        if (Time.time >= _tempoProximaTroca)
            DefinirNovaPatrulha();
    }

    private void DefinirNovaPatrulha()
    {
        _velocidadeAtual = Random.Range(velocidadeMin, velocidadeMax);
        _direcaoAtual = Random.value > 0.5f ? 1f : -1f;
        _tempoProximaTroca = Time.time + Random.Range(tempoTrocaMin, tempoTrocaMax);
    }

    // =====================================================================
    // AUDIO
    // =====================================================================
    private void TocarSomDisparo()
    {
        if (audioSource == null || clipDisparo == null) return;

        audioSource.pitch = 1f + Random.Range(-variacaoPitchDisparo, variacaoPitchDisparo);
        audioSource.PlayOneShot(clipDisparo, volumeDisparo);
    }

    private static void ConfigurarAudio3D(AudioSource fonte, float distMin, float distMax)
    {
        fonte.playOnAwake = false;
        fonte.loop = false;
        fonte.spatialBlend = 1f;                    // 3D
        fonte.rolloffMode = AudioRolloffMode.Linear; // volume cheio ate distMin, mudo em distMax
        fonte.minDistance = distMin;
        fonte.maxDistance = distMax;
    }

    private static void TocarClipTemporario(Vector3 posicao, AudioClip clip, float volume, float distMin, float distMax)
    {
        GameObject go = new GameObject("SomTorre_Temp");
        go.transform.position = posicao;

        AudioSource fonte = go.AddComponent<AudioSource>();
        ConfigurarAudio3D(fonte, distMin, distMax);
        fonte.clip = clip;
        fonte.volume = volume;
        fonte.Play();

        Destroy(go, clip.length + 0.1f);
    }

    // =====================================================================
    // VALIDACAO
    // =====================================================================
    private void OnValidate()
    {
        raioVisao = Mathf.Max(0.5f, raioVisao);
        intervaloBuscaAlvo = Mathf.Max(0.05f, intervaloBuscaAlvo);
        velocidadeGiro = Mathf.Max(1f, velocidadeGiro);
        toleranciaMira = Mathf.Max(1f, toleranciaMira);
        tirosPorSegundo = Mathf.Max(0.1f, tirosPorSegundo);
        velocidadeBala = Mathf.Max(0f, velocidadeBala);
        tempoVidaBala = Mathf.Max(0f, tempoVidaBala);
        velocidadeMin = Mathf.Max(0f, velocidadeMin);
        velocidadeMax = Mathf.Max(velocidadeMin, velocidadeMax);
        tempoTrocaMin = Mathf.Max(0.1f, tempoTrocaMin);
        tempoTrocaMax = Mathf.Max(tempoTrocaMin, tempoTrocaMax);
        distanciaMinimaAudio = Mathf.Max(0.1f, distanciaMinimaAudio);
        distanciaMaximaAudio = Mathf.Max(distanciaMinimaAudio + 1f, distanciaMaximaAudio);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================
    private void OnDrawGizmosSelected()
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