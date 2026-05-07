using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class Torre : MonoBehaviour
{
    [Header("Visão")]
    public float raioVisao = 10f;
    [SerializeField] private string[] tagsInimigos;

    [Header("Referências")]
    [SerializeField] private Transform baseCanhao;
    [SerializeField] private Transform pontoDisparo;

    [Header("Disparo")]
    [SerializeField] private GameObject prefabBala;
    [SerializeField] private float velocidadeBala = 20f;
    [SerializeField] private float tirosPorSegundo = 2f;

    [Header("Prioridade de Alvo")]
    [SerializeField] private PrioridadeAlvo prioridade = PrioridadeAlvo.MaisPerto;

    [Header("Patrulha (sem alvo)")]
    [SerializeField] private float velocidadeMin = 10f;
    [SerializeField] private float velocidadeMax = 30f;
    [SerializeField] private float tempoTrocaMin = 1f;
    [SerializeField] private float tempoTrocaMax = 3f;

    private Transform alvoAtual;
    private float tempoProximoTiro;
    private float velocidadeAtual;
    private float direcaoAtual;
    private float tempoProximaTroca;

    // Reutilizado a cada frame pra evitar alocação
    private HashSet<Transform> _jaAvaliados = new HashSet<Transform>();

    void Start()
    {
        DefinirNovaPatrulha();
    }

    void Update()
    {
        ProcurarAlvo();

        if (alvoAtual != null)
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

        _jaAvaliados.Clear(); // limpa sem alocar novo HashSet

        Transform melhorAlvo = null;
        float melhorValor = prioridade == PrioridadeAlvo.MaisLonge
            ? Mathf.NegativeInfinity
            : Mathf.Infinity;

        foreach (Collider col in coliders)
        {
            Transform raiz = PegarTransformComTag(col.transform);
            if (raiz == null) continue;

            // Cada inimigo entra na comparação UMA VEZ só
            if (_jaAvaliados.Contains(raiz)) continue;
            _jaAvaliados.Add(raiz);

            float valor = CalcularValorPrioridade(raiz);

            bool ehMelhor = prioridade == PrioridadeAlvo.MaisLonge
                ? valor > melhorValor
                : valor < melhorValor;

            if (ehMelhor)
            {
                melhorValor = valor;
                melhorAlvo = raiz;
            }
        }

        alvoAtual = melhorAlvo; // null se ninguém no raio = volta a patrulhar
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
        Vector3 direcao = alvoAtual.position - baseCanhao.position;
        direcao.y = 0f;

        if (direcao == Vector3.zero) return;

        float angulo = Mathf.Atan2(direcao.x, direcao.z) * Mathf.Rad2Deg;
        baseCanhao.localRotation = Quaternion.Euler(0f, angulo, 0f);
    }

    void Atirar()
    {
        if (Time.time >= tempoProximoTiro)
        {
            tempoProximoTiro = Time.time + (1f / tirosPorSegundo);
            GameObject bala = Instantiate(prefabBala, pontoDisparo.position, pontoDisparo.rotation);
            Rigidbody rb = bala.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = pontoDisparo.forward * velocidadeBala;
        }
    }

    void Patrulhar()
    {
        baseCanhao.Rotate(0f, direcaoAtual * velocidadeAtual * Time.deltaTime, 0f, Space.Self);

        if (Time.time >= tempoProximaTroca)
            DefinirNovaPatrulha();
    }

    void DefinirNovaPatrulha()
    {
        velocidadeAtual = Random.Range(velocidadeMin, velocidadeMax);
        direcaoAtual = Random.value > 0.5f ? 1f : -1f;
        tempoProximaTroca = Time.time + Random.Range(tempoTrocaMin, tempoTrocaMax);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, raioVisao);
    }
}

public enum PrioridadeAlvo { MaisPerto, MaisLonge, MenorVida, MaiorVida }

public interface IVida { float VidaAtual { get; } }