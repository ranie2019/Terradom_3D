using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class TorreAr : MonoBehaviour
{
    // =====================================================================
    // INSPECTOR
    // =====================================================================
    [Header("Visão")]
    public float raioVisao = 80f;
    [SerializeField] private LayerMask layerAviao;
    [SerializeField] private string[] tagsInimigos = { "Vermelho" };

    // FIX: intervalo de busca de alvo.
    // Antes: Physics.OverlapSphere rodava todo frame (Update).
    // Com 10 torres = 10 OverlapSpheres/frame. Agora roda a cada 0.2s por padrão.
    [SerializeField] private float intervaloBuscaAlvo = 0.2f;

    [Header("Referências de Mira")]
    [Tooltip("Filho direto da torre. Gira somente no eixo Y (horizontal), limitado.")]
    [SerializeField] private Transform cabeca;
    [Tooltip("Filho da Cabeca. Gira somente no eixo Z (elevação). NÃO gira Y independente.")]
    [SerializeField] private Transform baseMissel;

    [Header("Limites de Mira")]
    [SerializeField] private float limiteYMin     = -180f;
    [SerializeField] private float limiteYMax     =  180f;
    [SerializeField] private float velocidadeMira =    5f;

    [Header("Pontos de Míssel")]
    [Tooltip("Cada ponto deve ter o prefab do Míssel como filho INATIVO.")]
    [SerializeField] private Transform[] pontosMisseis;

    [Header("Cadência de disparo")]
    [SerializeField] private float intervaloEntreLancamentos = 0.5f;

    [Header("Recarga")]
    [Tooltip("Tempo para recarregar depois que TODOS os pontos ficarem vazios.")]
    [SerializeField] private float tempoRecarga = 6f;

    [Header("Prioridade de Alvo")]
    [SerializeField] private PrioridadeAlvoAr prioridade = PrioridadeAlvoAr.MaisPerto;

    [Header("Patrulha (sem alvo)")]
    [SerializeField] private float velocidadeMin = 10f;
    [SerializeField] private float velocidadeMax = 30f;
    [SerializeField] private float tempoTrocaMin =  1f;
    [SerializeField] private float tempoTrocaMax =  3f;

    // =====================================================================
    // ESTADO INTERNO
    // =====================================================================
    private Transform alvoAtual;
    private int       indicePontoAtual;
    private bool      recarregando;
    private bool      disparando;
    private float     anguloYAtual;
    private float     anguloZAtual;
    private float     velocidadeAtual;
    private float     direcaoAtual;
    private float     tempoProximaTroca;

    // FIX: timer de busca de alvo
    private float _proximaBusca;

    private readonly HashSet<Transform> _jaAvaliados = new HashSet<Transform>();

    // Pool: guarda os mísseis originais (filhos dos pontos) para reutilizá-los após recarga
    private Dictionary<Transform, List<Missel>> _pool = new Dictionary<Transform, List<Missel>>();

    // =====================================================================
    // UNITY
    // =====================================================================
    void Start()
    {
        if (cabeca     != null) anguloYAtual = cabeca.localEulerAngles.y;
        if (baseMissel != null) anguloZAtual = baseMissel.localEulerAngles.z;
        DefinirNovaPatrulha();
        ConstruirPool();
        _proximaBusca = Time.time; // primeira busca imediata
    }

    void Update()
    {
        // FIX: só busca alvo quando o timer vencer — não mais todo frame
        if (Time.time >= _proximaBusca)
        {
            _proximaBusca = Time.time + intervaloBuscaAlvo;
            ProcurarAlvo();
        }

        // Valida se alvo ainda existe (pode ter sido destruído entre buscas)
        if (alvoAtual != null && !alvoAtual.gameObject.activeInHierarchy)
            alvoAtual = null;

        if (alvoAtual != null)
        {
            GirarCabecaY();
            GirarBaseMisselZ();
            if (!disparando && !recarregando && TemMisselDisponivel())
                StartCoroutine(RotinaDeLancamento());
        }
        else
        {
            Patrulhar();
        }
    }

    // =====================================================================
    // POOL DE MÍSSEIS
    // =====================================================================
    void ConstruirPool()
    {
        if (pontosMisseis == null) return;
        foreach (Transform ponto in pontosMisseis)
        {
            if (ponto == null) continue;
            var lista = new List<Missel>();
            for (int i = 0; i < ponto.childCount; i++)
            {
                Missel m = ponto.GetChild(i).GetComponent<Missel>();
                if (m != null)
                    lista.Add(m);
            }
            _pool[ponto] = lista;
        }
    }

    Missel PegarMisselDoPool(Transform ponto)
    {
        if (ponto == null) return null;
        if (!_pool.TryGetValue(ponto, out var lista)) return null;
        foreach (Missel m in lista)
            if (m != null && !m.EstaLancado) return m;
        return null;
    }

    public void NotificarMisselDevolvido()
    {
        // Reservado para extensões futuras (ex.: atualizar UI de munição)
    }

    // =====================================================================
    // MIRA
    // =====================================================================
    void GirarCabecaY()
    {
        if (cabeca == null || alvoAtual == null) return;
        Vector3 dirWorld = alvoAtual.position - cabeca.position;
        dirWorld.y = 0f;
        if (dirWorld.sqrMagnitude < 0.001f) return;
        Vector3 dirLocal  = transform.InverseTransformDirection(dirWorld);
        float   anguloAlvo = Mathf.Atan2(dirLocal.x, dirLocal.z) * Mathf.Rad2Deg;
        anguloAlvo  = Mathf.Clamp(anguloAlvo, limiteYMin, limiteYMax);
        anguloYAtual = Mathf.LerpAngle(anguloYAtual, anguloAlvo, Time.deltaTime * velocidadeMira);
        cabeca.localRotation = Quaternion.Euler(0f, anguloYAtual, 0f);
    }

    void GirarBaseMisselZ()
    {
        if (baseMissel == null || alvoAtual == null) return;
        Vector3 alvoLocalDaCabeca = cabeca.InverseTransformPoint(alvoAtual.position);
        float   distH             = Mathf.Sqrt(alvoLocalDaCabeca.x * alvoLocalDaCabeca.x +
                                               alvoLocalDaCabeca.z * alvoLocalDaCabeca.z);
        float elevacao    = Mathf.Atan2(alvoLocalDaCabeca.y, distH) * Mathf.Rad2Deg;
        float anguloZAlvo = -elevacao;
        anguloZAtual = Mathf.LerpAngle(anguloZAtual, anguloZAlvo, Time.deltaTime * velocidadeMira);
        baseMissel.localRotation = Quaternion.Euler(0f, 0f, anguloZAtual);
    }

    // =====================================================================
    // PATRULHA (sem alvo)
    // =====================================================================
    void Patrulhar()
    {
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
            anguloZAtual = Mathf.LerpAngle(anguloZAtual, 0f, Time.deltaTime * velocidadeMira);
            baseMissel.localRotation = Quaternion.Euler(0f, 0f, anguloZAtual);
        }
        if (Time.time >= tempoProximaTroca)
            DefinirNovaPatrulha();
    }

    void DefinirNovaPatrulha()
    {
        velocidadeAtual   = Random.Range(velocidadeMin, velocidadeMax);
        direcaoAtual      = Random.value > 0.5f ? 1f : -1f;
        tempoProximaTroca = Time.time + Random.Range(tempoTrocaMin, tempoTrocaMax);
    }

    // =====================================================================
    // DETECÇÃO DE ALVO
    // =====================================================================
    void ProcurarAlvo()
    {
        Collider[] coliders = Physics.OverlapSphere(transform.position, raioVisao, layerAviao);
        _jaAvaliados.Clear();

        Transform melhorAlvo  = null;
        float     melhorValor = prioridade == PrioridadeAlvoAr.MaisLonge
            ? Mathf.NegativeInfinity
            : Mathf.Infinity;

        foreach (Collider col in coliders)
        {
            Transform raiz = PegarTransformComTag(col.transform);
            if (raiz == null) continue;
            if (_jaAvaliados.Contains(raiz)) continue;
            _jaAvaliados.Add(raiz);

            float valor    = CalcularValorPrioridade(raiz);
            bool  ehMelhor = prioridade == PrioridadeAlvoAr.MaisLonge
                ? valor > melhorValor
                : valor < melhorValor;

            if (ehMelhor)
            {
                melhorValor = valor;
                melhorAlvo  = raiz;
            }
        }
        alvoAtual = melhorAlvo;
    }

    float CalcularValorPrioridade(Transform alvo)
    {
        switch (prioridade)
        {
            case PrioridadeAlvoAr.MaisPerto:
            case PrioridadeAlvoAr.MaisLonge:
                return Vector3.Distance(transform.position, alvo.position);
            case PrioridadeAlvoAr.MenorVida:
            case PrioridadeAlvoAr.MaiorVida:
                IVidaAr vida = alvo.GetComponentInChildren<IVidaAr>()
                            ?? alvo.GetComponentInParent<IVidaAr>();
                if (vida == null) return Mathf.Infinity;
                return prioridade == PrioridadeAlvoAr.MenorVida ? vida.VidaAtual : -vida.VidaAtual;
            default:
                return Vector3.Distance(transform.position, alvo.position);
        }
    }

    Transform PegarTransformComTag(Transform origem)
    {
        Transform atual = origem;
        while (atual != null)
        {
            foreach (string tag in tagsInimigos)
                if (atual.CompareTag(tag)) return atual;
            atual = atual.parent;
        }
        return null;
    }

    // =====================================================================
    // DISPARO
    // =====================================================================
    bool TemMisselDisponivel()
    {
        if (pontosMisseis == null || pontosMisseis.Length == 0) return false;
        foreach (Transform ponto in pontosMisseis)
            if (PegarMisselDoPool(ponto) != null) return true;
        return false;
    }

    IEnumerator RotinaDeLancamento()
    {
        if (pontosMisseis == null || pontosMisseis.Length == 0)
        {
            disparando = false;
            yield break;
        }

        disparando = true;
        int tentativas = 0;

        while (TemMisselDisponivel() && alvoAtual != null)
        {
            Transform ponto  = pontosMisseis[indicePontoAtual];
            Missel    missel = PegarMisselDoPool(ponto);

            if (missel != null)
            {
                LancarMissel(missel, ponto);
                tentativas       = 0;
                indicePontoAtual = (indicePontoAtual + 1) % pontosMisseis.Length;
                yield return new WaitForSeconds(intervaloEntreLancamentos);
            }
            else
            {
                tentativas++;
                if (tentativas >= pontosMisseis.Length) break;
                indicePontoAtual = (indicePontoAtual + 1) % pontosMisseis.Length;
            }
        }

        disparando = false;
        if (!TemMisselDisponivel())
            StartCoroutine(Recarregar());
    }

    void LancarMissel(Missel missel, Transform pontoOrigem)
    {
        missel.transform.SetParent(null);
        missel.Lancar(alvoAtual, transform, pontoOrigem, this);
    }

    IEnumerator Recarregar()
    {
        recarregando = true;
        yield return new WaitForSeconds(tempoRecarga);
        indicePontoAtual = 0;
        recarregando     = false;
    }

    // =====================================================================
    // VALIDAÇÃO
    // =====================================================================
    private void OnValidate()
    {
        raioVisao              = Mathf.Max(1f,   raioVisao);
        intervaloBuscaAlvo     = Mathf.Max(0.05f, intervaloBuscaAlvo);
        velocidadeMira         = Mathf.Max(0.1f,  velocidadeMira);
        intervaloEntreLancamentos = Mathf.Max(0.05f, intervaloEntreLancamentos);
        tempoRecarga           = Mathf.Max(0.5f,  tempoRecarga);
        velocidadeMin          = Mathf.Max(0f,    velocidadeMin);
        velocidadeMax          = Mathf.Max(velocidadeMin, velocidadeMax);
        tempoTrocaMin          = Mathf.Max(0.1f,  tempoTrocaMin);
        tempoTrocaMax          = Mathf.Max(tempoTrocaMin, tempoTrocaMax);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================
    void OnDrawGizmosSelected()
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
            Vector3    origem = cabeca.position;
            float      r      = raioVisao * 0.4f;
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