using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Torre antiaerea: detecta avioes inimigos dentro do raio de visao e dispara
/// um missel a cada "intervaloEntreLancamentos" enquanto houver alvo.
///
/// MISSEIS: cada missel lancado e DESTRUIDO ao acertar ou ao fim do tempo de vida.
/// Quando todos os pontos ficam vazios, a torre espera "tempoRecarga" e instancia
/// NOVOS misseis a partir do prefab em cada ponto vazio.
///
/// EQUIPE: descoberta pela tag da hierarquia (Azul/Vermelho/Verde);
/// qualquer outra equipe e considerada inimiga.
/// </summary>
[DisallowMultipleComponent]
public class TorreAr : MonoBehaviour
{
    private static readonly string[] TagsDeEquipe = { "Azul", "Vermelho", "Verde" };

    // =====================================================================
    // INSPECTOR
    // =====================================================================
    [Header("Visao")]
    public float raioVisao = 80f;
    [Tooltip("Layer em que ficam os colliders dos AVIOES. Obrigatorio.")]
    [SerializeField] private LayerMask layerAviao;
    [SerializeField] private float intervaloBuscaAlvo = 0.2f;
    [Tooltip("Folga (em %) para o alvo sair do raio antes de ser perdido.")]
    [SerializeField, Range(0f, 0.5f)] private float histereseSaida = 0.1f;

    [Header("Equipe")]
    [Tooltip("Descobre a equipe da torre pela tag da hierarquia e ataca qualquer outra equipe.")]
    [SerializeField] private bool detectarEquipeAutomaticamente = true;
    [Tooltip("Usadas apenas se a equipe automatica nao for encontrada ou estiver desligada.")]
    [SerializeField] private string[] tagsInimigos = { "Vermelho", "Verde" };

    [Header("Referencias de Mira")]
    [Tooltip("Filho direto da torre. Gira somente no eixo Y (horizontal), limitado.")]
    [SerializeField] private Transform cabeca;
    [Tooltip("Filho da Cabeca. Gira somente no eixo Z (elevacao).")]
    [SerializeField] private Transform baseMissel;

    [Header("Limites de Mira")]
    [SerializeField] private float limiteYMin = -180f;
    [SerializeField] private float limiteYMax = 180f;
    [SerializeField] private float velocidadeMira = 5f;
    [Tooltip("Se ligado, so dispara quando a cabeca ja estiver apontada para o alvo.")]
    [SerializeField] private bool exigirMiraAlinhada = false;
    [SerializeField] private float toleranciaMira = 15f;

    [Header("Misseis")]
    [Tooltip("PREFAB do Missel (arraste do Project, NAO da cena). Usado para recarregar a torre.")]
    [SerializeField] private Missel prefabMissel;
    [Tooltip("Pontos onde os misseis ficam. Pontos sem missel recebem um novo a partir do prefab.")]
    [SerializeField] private Transform[] pontosMisseis;

    [Header("Cadencia de disparo")]
    [Tooltip("Tempo entre um missel e o proximo enquanto o aviao estiver no campo de visao.")]
    [SerializeField] private float intervaloEntreLancamentos = 0.5f;

    [Header("Recarga")]
    [Tooltip("Espera depois que TODOS os pontos ficarem vazios, antes de instanciar novos misseis.")]
    [SerializeField] private float tempoRecarga = 6f;

    [Header("Audio de lancamento")]
    [Tooltip("Opcional. Se vazio e houver clipe, a torre cria um AudioSource 3D automaticamente.")]
    [SerializeField] private AudioSource fonteAudio;
    [SerializeField] private AudioClip audioLancamento;
    [SerializeField, Range(0f, 1f)] private float volumeLancamento = 1f;
    [SerializeField] private float distanciaMaximaAudio = 80f;

    [Header("Prioridade de Alvo")]
    [SerializeField] private PrioridadeAlvoAr prioridade = PrioridadeAlvoAr.MaisPerto;

    [Header("Patrulha (sem alvo)")]
    [SerializeField] private float velocidadeMin = 10f;
    [SerializeField] private float velocidadeMax = 30f;
    [SerializeField] private float tempoTrocaMin = 1f;
    [SerializeField] private float tempoTrocaMax = 3f;

    // =====================================================================
    // ESTADO INTERNO
    // =====================================================================
    private Transform alvoAtual;
    private Missel[] carregados;          // missel pronto em cada ponto (null = ponto vazio)
    private int indicePontoAtual;
    private float anguloYAtual;
    private float anguloZAtual;
    private float erroMiraY;
    private float velocidadeAtual;
    private float direcaoAtual;
    private float tempoProximaTroca;

    private float proximaBusca;
    private float proximoDisparo;
    private float fimRecarga = -1f;

    private readonly Collider[] bufferColliders = new Collider[64];
    private readonly HashSet<Transform> jaAvaliados = new HashSet<Transform>();

    // =====================================================================
    // UNITY
    // =====================================================================
    private void Start()
    {
        if (cabeca != null) anguloYAtual = Mathf.DeltaAngle(0f, cabeca.localEulerAngles.y);
        if (baseMissel != null) anguloZAtual = Mathf.DeltaAngle(0f, baseMissel.localEulerAngles.z);

        if (layerAviao.value == 0)
            Debug.LogWarning($"[TorreAr] '{name}': layerAviao nao configurado. A torre nao vai detectar nenhum aviao.", this);

        if (detectarEquipeAutomaticamente && string.IsNullOrEmpty(ObterEquipe(transform)))
            Debug.LogWarning($"[TorreAr] '{name}': sem tag de equipe (Azul/Vermelho/Verde) na torre ou nos pais. " +
                             "Usando a lista 'Tags Inimigos' como alvos.", this);

        if (prefabMissel == null)
            Debug.LogWarning($"[TorreAr] '{name}': 'Prefab Missel' nao atribuido. A torre NAO vai recarregar.", this);
        else if (prefabMissel.gameObject.scene.IsValid())
            Debug.LogWarning($"[TorreAr] '{name}': 'Prefab Missel' aponta para um objeto da CENA. " +
                             "Arraste o prefab da pasta Project.", this);

        PrepararAudio();
        DefinirNovaPatrulha();
        LerMisseisIniciais();
        CarregarPontosVazios(); // preenche pontos que comecaram sem missel

        proximaBusca = Time.time + Random.Range(0f, intervaloBuscaAlvo);
    }

    private void Update()
    {
        AtualizarRecarga();

        if (Time.time >= proximaBusca)
        {
            proximaBusca = Time.time + intervaloBuscaAlvo;
            ProcurarAlvo();
        }

        if (alvoAtual != null && !AlvoValido(alvoAtual))
            alvoAtual = null;

        if (alvoAtual == null)
        {
            Patrulhar();
            return;
        }

        GirarCabecaY();
        GirarBaseMisselZ();
        TentarDisparar();
    }

    // =====================================================================
    // MISSEIS: CARGA, DISPARO E RECARGA
    // =====================================================================
    private void LerMisseisIniciais()
    {
        int n = pontosMisseis != null ? pontosMisseis.Length : 0;
        carregados = new Missel[n];

        for (int i = 0; i < n; i++)
        {
            Transform ponto = pontosMisseis[i];
            if (ponto == null) continue;

            for (int c = 0; c < ponto.childCount; c++)
            {
                Missel m = ponto.GetChild(c).GetComponent<Missel>();
                if (m != null && !m.EstaLancado)
                {
                    carregados[i] = m;
                    break;
                }
            }
        }
    }

    private bool TemMisselCarregado()
    {
        if (carregados == null) return false;
        for (int i = 0; i < carregados.Length; i++)
            if (carregados[i] != null && !carregados[i].EstaLancado) return true;
        return false;
    }

    private int ProximoPontoCarregado()
    {
        if (carregados == null || carregados.Length == 0) return -1;

        for (int i = 0; i < carregados.Length; i++)
        {
            int idx = (indicePontoAtual + i) % carregados.Length;
            if (carregados[idx] != null && !carregados[idx].EstaLancado)
            {
                indicePontoAtual = (idx + 1) % carregados.Length;
                return idx;
            }
        }
        return -1;
    }

    /// <summary>Quando todos os pontos esvaziam, espera tempoRecarga e instancia novos misseis.</summary>
    private void AtualizarRecarga()
    {
        if (prefabMissel == null || carregados == null || carregados.Length == 0) return;

        if (TemMisselCarregado())
        {
            fimRecarga = -1f;
            return;
        }

        if (fimRecarga < 0f)
        {
            fimRecarga = Time.time + tempoRecarga;
            return;
        }

        if (Time.time >= fimRecarga)
        {
            CarregarPontosVazios();
            fimRecarga = -1f;
        }
    }

    private void CarregarPontosVazios()
    {
        if (prefabMissel == null || carregados == null) return;

        for (int i = 0; i < carregados.Length; i++)
        {
            Transform ponto = pontosMisseis[i];
            if (ponto == null) continue;
            if (carregados[i] != null && !carregados[i].EstaLancado) continue;

            Missel novo = Instantiate(prefabMissel, ponto.position, ponto.rotation, ponto);
            if (!novo.gameObject.activeSelf)
                novo.gameObject.SetActive(true);

            carregados[i] = novo;
        }
    }

    private void TentarDisparar()
    {
        // Intervalo entre misseis: vale mesmo se o alvo sair e voltar.
        if (Time.time < proximoDisparo)
            return;

        int idx = ProximoPontoCarregado();
        if (idx < 0)
            return; // sem municao: AtualizarRecarga cuida da recarga

        if (exigirMiraAlinhada && erroMiraY > toleranciaMira)
            return;

        Missel missel = carregados[idx];
        carregados[idx] = null; // ponto fica vazio ate a recarga
        proximoDisparo = Time.time + intervaloEntreLancamentos;

        missel.transform.SetParent(null);

        // Se o missel estiver inativo, ativa antes de lancar
        // (StartCoroutine e FixedUpdate nao funcionam em objeto inativo).
        if (!missel.gameObject.activeSelf)
            missel.gameObject.SetActive(true);

        // Assinatura sem pool: o Missel se DESTROI ao acertar ou ao fim do tempo de vida.
        missel.Lancar(alvoAtual, transform);

        TocarAudioLancamento();
    }

    /// <summary>Mantido por compatibilidade com o Missel (modo pool nao e mais usado).</summary>
    public void NotificarMisselDevolvido() { }

    // =====================================================================
    // AUDIO
    // =====================================================================
    private void PrepararAudio()
    {
        if (audioLancamento == null || fonteAudio != null) return;

        fonteAudio = gameObject.AddComponent<AudioSource>();
        fonteAudio.playOnAwake  = false;
        fonteAudio.loop         = false;
        fonteAudio.spatialBlend = 1f; // 3D: some com a distancia
        fonteAudio.rolloffMode  = AudioRolloffMode.Linear;
        fonteAudio.minDistance  = 5f;
        fonteAudio.maxDistance  = distanciaMaximaAudio;
    }

    private void TocarAudioLancamento()
    {
        if (audioLancamento == null) return;
        if (fonteAudio == null) PrepararAudio();
        if (fonteAudio != null)
            fonteAudio.PlayOneShot(audioLancamento, volumeLancamento);
    }

    // =====================================================================
    // MIRA
    // =====================================================================
    private bool GiroCompleto => (limiteYMax - limiteYMin) >= 359f;

    private void GirarCabecaY()
    {
        if (cabeca == null || alvoAtual == null) return;

        Vector3 dirWorld = alvoAtual.position - cabeca.position;
        dirWorld.y = 0f;
        if (dirWorld.sqrMagnitude < 0.001f) return;

        Vector3 dirLocal = transform.InverseTransformDirection(dirWorld);
        float anguloDesejado = Mathf.Atan2(dirLocal.x, dirLocal.z) * Mathf.Rad2Deg;
        float anguloAlvo = Mathf.Clamp(anguloDesejado, limiteYMin, limiteYMax);

        float t = 1f - Mathf.Exp(-velocidadeMira * Time.deltaTime);

        // Com arco limitado nao pode usar LerpAngle: ele pode girar pelo lado proibido.
        anguloYAtual = GiroCompleto
            ? Mathf.LerpAngle(anguloYAtual, anguloAlvo, t)
            : Mathf.Lerp(anguloYAtual, anguloAlvo, t);

        erroMiraY = Mathf.Abs(Mathf.DeltaAngle(anguloYAtual, anguloDesejado));
        cabeca.localRotation = Quaternion.Euler(0f, anguloYAtual, 0f);
    }

    private void GirarBaseMisselZ()
    {
        if (baseMissel == null || alvoAtual == null) return;

        Transform referencia = cabeca != null ? cabeca : transform;
        Vector3 alvoLocal = referencia.InverseTransformPoint(alvoAtual.position);
        float distH = Mathf.Sqrt(alvoLocal.x * alvoLocal.x + alvoLocal.z * alvoLocal.z);
        float elevacao = Mathf.Atan2(alvoLocal.y, distH) * Mathf.Rad2Deg;

        float t = 1f - Mathf.Exp(-velocidadeMira * Time.deltaTime);
        anguloZAtual = Mathf.LerpAngle(anguloZAtual, -elevacao, t);
        baseMissel.localRotation = Quaternion.Euler(0f, 0f, anguloZAtual);
    }

    // =====================================================================
    // PATRULHA (sem alvo)
    // =====================================================================
    private void Patrulhar()
    {
        erroMiraY = 180f;

        if (cabeca != null)
        {
            anguloYAtual += direcaoAtual * velocidadeAtual * Time.deltaTime;
            if (anguloYAtual <= limiteYMin)
            {
                anguloYAtual = limiteYMin;
                direcaoAtual = 1f;
            }
            else if (anguloYAtual >= limiteYMax)
            {
                anguloYAtual = limiteYMax;
                direcaoAtual = -1f;
            }
            cabeca.localRotation = Quaternion.Euler(0f, anguloYAtual, 0f);
        }

        if (baseMissel != null)
        {
            float t = 1f - Mathf.Exp(-velocidadeMira * Time.deltaTime);
            anguloZAtual = Mathf.LerpAngle(anguloZAtual, 0f, t);
            baseMissel.localRotation = Quaternion.Euler(0f, 0f, anguloZAtual);
        }

        if (Time.time >= tempoProximaTroca)
            DefinirNovaPatrulha();
    }

    private void DefinirNovaPatrulha()
    {
        velocidadeAtual = Random.Range(velocidadeMin, velocidadeMax);
        direcaoAtual = Random.value > 0.5f ? 1f : -1f;
        tempoProximaTroca = Time.time + Random.Range(tempoTrocaMin, tempoTrocaMax);
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
        int total = Physics.OverlapSphereNonAlloc(transform.position, raioVisao, bufferColliders, layerAviao);
        jaAvaliados.Clear();

        string minhaEquipe = detectarEquipeAutomaticamente ? ObterEquipe(transform) : null;

        Transform melhorAlvo = null;
        bool maiorEMelhor = prioridade == PrioridadeAlvoAr.MaisLonge;
        float melhorValor = maiorEMelhor ? float.NegativeInfinity : float.PositiveInfinity;

        for (int i = 0; i < total; i++)
        {
            Collider col = bufferColliders[i];
            if (col == null) continue;

            Transform raiz = PegarRaizInimiga(col.transform, minhaEquipe);
            if (raiz == null || !jaAvaliados.Add(raiz)) continue;

            float valor = CalcularValorPrioridade(raiz);
            bool ehMelhor = maiorEMelhor ? valor > melhorValor : valor < melhorValor;

            if (ehMelhor)
            {
                melhorValor = valor;
                melhorAlvo = raiz;
            }
        }

        alvoAtual = melhorAlvo;
    }

    private float CalcularValorPrioridade(Transform alvo)
    {
        switch (prioridade)
        {
            case PrioridadeAlvoAr.MenorVida:
            case PrioridadeAlvoAr.MaiorVida:
                IVidaAr vida = alvo.GetComponentInChildren<IVidaAr>();
                if (vida == null) vida = alvo.GetComponentInParent<IVidaAr>();

                // Sem IVidaAr: valor alto e finito para ainda poder ser escolhido.
                if (vida == null) return 1e9f;
                return prioridade == PrioridadeAlvoAr.MenorVida ? vida.VidaAtual : -vida.VidaAtual;

            default: // MaisPerto / MaisLonge
                return (transform.position - alvo.position).sqrMagnitude;
        }
    }

    /// <summary>
    /// Sobe a hierarquia ate achar a tag de equipe. Retorna o transform dono da tag
    /// somente se ele for INIMIGO desta torre.
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

                return TagEstaNaListaDeInimigos(equipe) ? atual : null;
            }
            atual = atual.parent;
        }
        return null;
    }

    private bool TagEstaNaListaDeInimigos(string tag)
    {
        if (tagsInimigos == null) return false;
        for (int i = 0; i < tagsInimigos.Length; i++)
            if (tagsInimigos[i] == tag) return true;
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
    // VALIDACAO
    // =====================================================================
    private void OnValidate()
    {
        raioVisao = Mathf.Max(1f, raioVisao);
        intervaloBuscaAlvo = Mathf.Max(0.05f, intervaloBuscaAlvo);
        velocidadeMira = Mathf.Max(0.1f, velocidadeMira);
        toleranciaMira = Mathf.Max(1f, toleranciaMira);
        intervaloEntreLancamentos = Mathf.Max(0.05f, intervaloEntreLancamentos);
        tempoRecarga = Mathf.Max(0.5f, tempoRecarga);
        distanciaMaximaAudio = Mathf.Max(1f, distanciaMaximaAudio);
        limiteYMax = Mathf.Max(limiteYMin, limiteYMax);
        velocidadeMin = Mathf.Max(0f, velocidadeMin);
        velocidadeMax = Mathf.Max(velocidadeMin, velocidadeMax);
        tempoTrocaMin = Mathf.Max(0.1f, tempoTrocaMin);
        tempoTrocaMax = Mathf.Max(tempoTrocaMin, tempoTrocaMax);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, raioVisao);

        if (alvoAtual != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, alvoAtual.position);
            Gizmos.DrawSphere(alvoAtual.position, 0.5f);
        }

        if (cabeca != null)
        {
            Vector3 origem = cabeca.position;
            float r = raioVisao * 0.4f;
            Quaternion rotMin = transform.rotation * Quaternion.Euler(0f, limiteYMin, 0f);
            Quaternion rotMax = transform.rotation * Quaternion.Euler(0f, limiteYMax, 0f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(origem, rotMin * Vector3.forward * r);
            Gizmos.DrawRay(origem, rotMax * Vector3.forward * r);
        }
    }
}

public enum PrioridadeAlvoAr { MaisPerto, MaisLonge, MenorVida, MaiorVida }
public interface IVidaAr { float VidaAtual { get; } }