using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class Torre : MonoBehaviour
{
    [Header("Visão")]
    public float raioVisao = 10f;
    [SerializeField] private string[] tagsInimigos;

    // FIX: adicionado intervalo de busca de alvo.
    // Antes: Physics.OverlapSphere rodava todo frame (Update).
    // Com 10 torres = 10 OverlapSpheres/frame. Agora roda a cada 0.2s por padrão.
    [SerializeField] private float intervaloBuscaAlvo = 0.2f;

    [Header("Referências")]
    [SerializeField] private Transform baseCanhao;
    [SerializeField] private Transform pontoDisparo;

    [Header("Disparo")]
    [SerializeField] private GameObject prefabBala;
    [SerializeField] private float velocidadeBala  = 20f;
    [SerializeField] private float tirosPorSegundo = 2f;

    [Header("Prioridade de Alvo")]
    [SerializeField] private PrioridadeAlvo prioridade = PrioridadeAlvo.MaisPerto;

    [Header("Patrulha (sem alvo)")]
    [SerializeField] private float velocidadeMin = 10f;
    [SerializeField] private float velocidadeMax = 30f;
    [SerializeField] private float tempoTrocaMin =  1f;
    [SerializeField] private float tempoTrocaMax =  3f;

    private Transform _alvoAtual;
    private float     _tempoProximoTiro;
    private float     _velocidadeAtual;
    private float     _direcaoAtual;
    private float     _tempoProximaTroca;

    // FIX: timer de busca de alvo
    private float _proximaBusca;

    // Reutilizado a cada busca para evitar alocação de HashSet novo
    private readonly HashSet<Transform> _jaAvaliados = new HashSet<Transform>();

    void Start()
    {
        DefinirNovaPatrulha();
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
                if (vida == null) return Mathf.Infinity;
                return prioridade == PrioridadeAlvo.MenorVida ? vida.VidaAtual : -vida.VidaAtual;

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

    void Mirar()
    {
        if (baseCanhao == null || _alvoAtual == null) return;
        Vector3 direcao = _alvoAtual.position - baseCanhao.position;
        direcao.y = 0f;
        if (direcao == Vector3.zero) return;
        float angulo = Mathf.Atan2(direcao.x, direcao.z) * Mathf.Rad2Deg;
        baseCanhao.localRotation = Quaternion.Euler(0f, angulo, 0f);
    }

    void Atirar()
    {
        if (prefabBala == null || pontoDisparo == null) return;
        if (Time.time >= _tempoProximoTiro)
        {
            _tempoProximoTiro = Time.time + (1f / tirosPorSegundo);
            GameObject bala = Instantiate(prefabBala, pontoDisparo.position, pontoDisparo.rotation);
            Rigidbody rb = bala.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = pontoDisparo.forward * velocidadeBala;
        }
    }

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

    private void OnValidate()
    {
        raioVisao         = Mathf.Max(0.5f, raioVisao);
        intervaloBuscaAlvo = Mathf.Max(0.05f, intervaloBuscaAlvo);
        tirosPorSegundo   = Mathf.Max(0.1f, tirosPorSegundo);
        velocidadeBala    = Mathf.Max(0f,   velocidadeBala);
        velocidadeMin     = Mathf.Max(0f,   velocidadeMin);
        velocidadeMax     = Mathf.Max(velocidadeMin, velocidadeMax);
        tempoTrocaMin     = Mathf.Max(0.1f, tempoTrocaMin);
        tempoTrocaMax     = Mathf.Max(tempoTrocaMin, tempoTrocaMax);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raioVisao);
    }
}

public enum PrioridadeAlvo { MaisPerto, MaisLonge, MenorVida, MaiorVida }
public interface IVida { float VidaAtual { get; } }