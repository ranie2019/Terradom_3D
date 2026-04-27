using UnityEngine;

[DisallowMultipleComponent]
public class PlayControleAI : MonoBehaviour
{
    [Header("Combate")]
    [SerializeField] private float distanciaParadaSemAtaque = 1.5f;
    [SerializeField] private float folgaParaEntrarNoAlcance = 0.1f;

    [Header("Patrulha")]
    [SerializeField] private bool patrulharSemAlvo = true;
    [SerializeField] private float tempoTrocaDirecao = 2f;

    [Header("Referências")]
    [SerializeField] private Movimentacao movimentacao;
    [SerializeField] private Visao visao;
    [SerializeField] private Ataque ataque;
    [SerializeField] private AtaqueDistancia ataqueDistancia;

    private Transform alvoAtual;
    private Vector3 direcaoPatrulha;
    private float proximaTrocaDirecao;

    private void Awake()
    {
        if (!movimentacao) movimentacao = GetComponent<Movimentacao>();
        if (!visao) visao = GetComponent<Visao>();
        if (!ataque) ataque = GetComponent<Ataque>();
        if (!ataqueDistancia) ataqueDistancia = GetComponent<AtaqueDistancia>();

        DefinirNovaDirecaoPatrulha();
    }

    private void OnDisable()
    {
        if (ataque != null)
            ataque.LimparAlvo();

        if (ataqueDistancia != null)
            ataqueDistancia.LimparAlvo();
    }

    private void Update()
    {
        AtualizarAlvo();

        if (alvoAtual != null)
        {
            if (ataque != null)
                ataque.DefinirAlvo(alvoAtual);

            if (ataqueDistancia != null)
                ataqueDistancia.DefinirAlvo(alvoAtual);

            ControlarPerseguicaoECombate();
        }
        else
        {
            if (ataque != null)
                ataque.LimparAlvo();

            if (ataqueDistancia != null)
                ataqueDistancia.LimparAlvo();

            ControlarPatrulha();
        }
    }

    private void AtualizarAlvo()
    {
        if (visao == null)
        {
            alvoAtual = null;
            return;
        }

        Transform alvoDaVisao = visao.GetAlvoAtual();

        if (alvoDaVisao == null || !alvoDaVisao.gameObject.activeInHierarchy)
        {
            alvoAtual = null;
            return;
        }

        alvoAtual = alvoDaVisao;
    }

    private void ControlarPerseguicaoECombate()
    {
        if (movimentacao == null || alvoAtual == null)
            return;

        Vector3 pontoAlvo = ObterPontoMaisProximoDoAlvo(alvoAtual, transform.position);
        float distancia = DistanciaXZ(transform.position, pontoAlvo);
        float distanciaParada = CalcularDistanciaParada();

        if (distancia > distanciaParada)
        {
            Vector3 direcao = pontoAlvo - transform.position;
            direcao.y = 0f;

            if (direcao.sqrMagnitude > 0.0001f)
            {
                movimentacao.SetPerseguindo(true);
                movimentacao.Mover(direcao.normalized);
            }
            else
            {
                movimentacao.SetPerseguindo(false);
                movimentacao.Parar();
            }
        }
        else
        {
            movimentacao.SetPerseguindo(false);
            movimentacao.Parar();
        }
    }

    private float CalcularDistanciaParada()
    {
        float alcance = distanciaParadaSemAtaque;

        if (ataqueDistancia != null)
            alcance = ataqueDistancia.GetAlcanceAtaque();
        else if (ataque != null)
            alcance = ataque.GetAlcanceAtaque();

        float distancia = alcance - folgaParaEntrarNoAlcance;

        if (distancia < 0.1f)
            distancia = 0.1f;

        return distancia;
    }

    private void ControlarPatrulha()
    {
        if (movimentacao == null)
            return;

        movimentacao.SetPerseguindo(false);

        if (!patrulharSemAlvo)
        {
            movimentacao.Parar();
            return;
        }

        if (Time.time >= proximaTrocaDirecao || direcaoPatrulha == Vector3.zero)
            DefinirNovaDirecaoPatrulha();

        movimentacao.Mover(direcaoPatrulha);
    }

    private void DefinirNovaDirecaoPatrulha()
    {
        Vector2 aleatorio2D = Random.insideUnitCircle.normalized;

        if (aleatorio2D == Vector2.zero)
            aleatorio2D = Vector2.right;

        direcaoPatrulha = new Vector3(aleatorio2D.x, 0f, aleatorio2D.y).normalized;
        proximaTrocaDirecao = Time.time + tempoTrocaDirecao;
    }

    private Vector3 ObterPontoMaisProximoDoAlvo(Transform alvo, Vector3 origem)
    {
        if (alvo == null)
            return origem;

        Collider melhorCollider = ObterColliderDoAlvo(alvo);

        if (melhorCollider != null)
            return melhorCollider.ClosestPoint(origem);

        return alvo.position;
    }

    private Collider ObterColliderDoAlvo(Transform alvo)
    {
        if (alvo == null)
            return null;

        Collider col = alvo.GetComponent<Collider>();

        if (col != null)
            return col;

        return alvo.GetComponentInChildren<Collider>();
    }

    private float DistanciaXZ(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}