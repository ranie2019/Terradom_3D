using UnityEngine;

[DisallowMultipleComponent]
public class TankVisao : MonoBehaviour
{
    public enum TipoAlvoTank
    {
        Nenhum,
        Terrestre,
        Aereo
    }

    public enum PrioridadeAlvo
    {
        TerrestrePrimeiro,
        AereoPrimeiro,
        MaisProximo
    }

    [Header("Origem da visao")]
    [SerializeField] private Transform origemVisao;
    [SerializeField] private float alturaOrigemVisao = 1.2f;

    [Header("Visao terrestre curta - por Tag")]
    [SerializeField] private bool usarVisaoTerrestre = true;
    [SerializeField] private float raioVisaoTerrestre = 14f;
    [SerializeField] private string[] tagsInimigosTerrestres = { "Vermelho" };
    [SerializeField] private bool ignorarLayersAereasNaVisaoTerrestre = true;

    [Header("Visao aerea longa - por Layer")]
    [SerializeField] private bool usarVisaoAerea = true;
    [SerializeField] private float raioVisaoAerea = 35f;
    [SerializeField] private LayerMask layersAvioesInimigos;
    [SerializeField] private bool aviaoTambemPrecisaTerTag = false;
    [SerializeField] private string[] tagsInimigosAereos = { "Vermelho" };

    [Header("Filtro geral")]
    [SerializeField] private bool detectarTriggers = false;
    [SerializeField] private float intervaloBusca = 0.15f;
    [SerializeField] private PrioridadeAlvo prioridadeAlvo = PrioridadeAlvo.TerrestrePrimeiro;

    [Header("Debug")]
    [SerializeField] private bool desenharVisaoNoEditor = true;

    private Transform alvoAtual;
    private Transform alvoTerrestreAtual;
    private Transform alvoAereoAtual;

    private TipoAlvoTank tipoAlvoAtual = TipoAlvoTank.Nenhum;

    private float proximaBusca;

    public Transform AlvoAtual => alvoAtual;
    public Transform AlvoTerrestreAtual => alvoTerrestreAtual;
    public Transform AlvoAereoAtual => alvoAereoAtual;

    public TipoAlvoTank TipoAlvoAtual => tipoAlvoAtual;

    public bool TemAlvo => alvoAtual != null;
    public bool TemAlvoTerrestre => alvoTerrestreAtual != null;
    public bool TemAlvoAereo => alvoAereoAtual != null;

    private void Awake()
    {
        if (origemVisao == null)
            origemVisao = transform;
    }

    private void Update()
    {
        if (Time.time < proximaBusca)
            return;

        proximaBusca = Time.time + Mathf.Max(0.02f, intervaloBusca);

        AtualizarVisao();
    }

    private void AtualizarVisao()
    {
        alvoTerrestreAtual = null;
        alvoAereoAtual = null;

        if (usarVisaoTerrestre)
            alvoTerrestreAtual = BuscarAlvoTerrestreMaisProximo();

        if (usarVisaoAerea)
            alvoAereoAtual = BuscarAlvoAereoMaisProximo();

        EscolherAlvoAtual();
    }

    private Transform BuscarAlvoTerrestreMaisProximo()
    {
        Vector3 origem = ObterOrigemVisao();
        Collider[] colliders = Physics.OverlapSphere(
            origem,
            raioVisaoTerrestre,
            ~0,
            detectarTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore
        );

        Transform melhorAlvo = null;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider colisor = colliders[i];

            if (colisor == null)
                continue;

            if (EhDoProprioTank(colisor.transform))
                continue;

            if (ignorarLayersAereasNaVisaoTerrestre && ObjetoOuPaisTemLayerNaMascara(colisor.transform, layersAvioesInimigos))
                continue;

            if (!ObjetoOuPaisTemAlgumaTag(colisor.transform, tagsInimigosTerrestres))
                continue;

            Transform alvo = ObterTransformPrincipalDoAlvo(colisor.transform);
            float distancia = DistanciaHorizontal(origem, alvo.position);

            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                melhorAlvo = alvo;
            }
        }

        return melhorAlvo;
    }

    private Transform BuscarAlvoAereoMaisProximo()
    {
        Vector3 origem = ObterOrigemVisao();
        Collider[] colliders = Physics.OverlapSphere(
            origem,
            raioVisaoAerea,
            ~0,
            detectarTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore
        );

        Transform melhorAlvo = null;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider colisor = colliders[i];

            if (colisor == null)
                continue;

            if (EhDoProprioTank(colisor.transform))
                continue;

            if (!ObjetoOuPaisTemLayerNaMascara(colisor.transform, layersAvioesInimigos))
                continue;

            if (aviaoTambemPrecisaTerTag && !ObjetoOuPaisTemAlgumaTag(colisor.transform, tagsInimigosAereos))
                continue;

            Transform alvo = ObterTransformPrincipalDoAlvo(colisor.transform);
            float distancia = DistanciaHorizontal(origem, alvo.position);

            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                melhorAlvo = alvo;
            }
        }

        return melhorAlvo;
    }

    private void EscolherAlvoAtual()
    {
        alvoAtual = null;
        tipoAlvoAtual = TipoAlvoTank.Nenhum;

        if (prioridadeAlvo == PrioridadeAlvo.TerrestrePrimeiro)
        {
            if (alvoTerrestreAtual != null)
            {
                alvoAtual = alvoTerrestreAtual;
                tipoAlvoAtual = TipoAlvoTank.Terrestre;
                return;
            }

            if (alvoAereoAtual != null)
            {
                alvoAtual = alvoAereoAtual;
                tipoAlvoAtual = TipoAlvoTank.Aereo;
                return;
            }
        }

        if (prioridadeAlvo == PrioridadeAlvo.AereoPrimeiro)
        {
            if (alvoAereoAtual != null)
            {
                alvoAtual = alvoAereoAtual;
                tipoAlvoAtual = TipoAlvoTank.Aereo;
                return;
            }

            if (alvoTerrestreAtual != null)
            {
                alvoAtual = alvoTerrestreAtual;
                tipoAlvoAtual = TipoAlvoTank.Terrestre;
                return;
            }
        }

        if (prioridadeAlvo == PrioridadeAlvo.MaisProximo)
        {
            if (alvoTerrestreAtual == null && alvoAereoAtual == null)
                return;

            if (alvoTerrestreAtual != null && alvoAereoAtual == null)
            {
                alvoAtual = alvoTerrestreAtual;
                tipoAlvoAtual = TipoAlvoTank.Terrestre;
                return;
            }

            if (alvoAereoAtual != null && alvoTerrestreAtual == null)
            {
                alvoAtual = alvoAereoAtual;
                tipoAlvoAtual = TipoAlvoTank.Aereo;
                return;
            }

            float distanciaTerrestre = DistanciaHorizontal(ObterOrigemVisao(), alvoTerrestreAtual.position);
            float distanciaAerea = DistanciaHorizontal(ObterOrigemVisao(), alvoAereoAtual.position);

            if (distanciaTerrestre <= distanciaAerea)
            {
                alvoAtual = alvoTerrestreAtual;
                tipoAlvoAtual = TipoAlvoTank.Terrestre;
            }
            else
            {
                alvoAtual = alvoAereoAtual;
                tipoAlvoAtual = TipoAlvoTank.Aereo;
            }
        }
    }

    private Vector3 ObterOrigemVisao()
    {
        Transform origem = origemVisao != null ? origemVisao : transform;
        return origem.position + Vector3.up * alturaOrigemVisao;
    }

    private bool EhDoProprioTank(Transform alvo)
    {
        if (alvo == null)
            return true;

        return alvo == transform || alvo.IsChildOf(transform);
    }

    private Transform ObterTransformPrincipalDoAlvo(Transform alvo)
    {
        if (alvo == null)
            return null;

        Transform atual = alvo;

        while (atual.parent != null)
        {
            if (atual.parent == transform)
                break;

            atual = atual.parent;
        }

        return atual;
    }

    private bool ObjetoOuPaisTemAlgumaTag(Transform alvo, string[] tags)
    {
        if (alvo == null || tags == null || tags.Length == 0)
            return false;

        Transform atual = alvo;

        while (atual != null)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                string tagProcurada = tags[i];

                if (string.IsNullOrWhiteSpace(tagProcurada))
                    continue;

                if (atual.gameObject.tag == tagProcurada)
                    return true;
            }

            atual = atual.parent;
        }

        return false;
    }

    private bool ObjetoOuPaisTemLayerNaMascara(Transform alvo, LayerMask mascara)
    {
        if (alvo == null)
            return false;

        Transform atual = alvo;

        while (atual != null)
        {
            int layerObjeto = atual.gameObject.layer;
            bool layerEstaNaMascara = (mascara.value & (1 << layerObjeto)) != 0;

            if (layerEstaNaMascara)
                return true;

            atual = atual.parent;
        }

        return false;
    }

    private float DistanciaHorizontal(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnValidate()
    {
        raioVisaoTerrestre = Mathf.Max(0.1f, raioVisaoTerrestre);
        raioVisaoAerea = Mathf.Max(0.1f, raioVisaoAerea);
        alturaOrigemVisao = Mathf.Max(0f, alturaOrigemVisao);
        intervaloBusca = Mathf.Max(0.02f, intervaloBusca);
    }

    private void OnDrawGizmosSelected()
    {
        if (!desenharVisaoNoEditor)
            return;

        Vector3 origem = ObterOrigemVisao();

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origem, raioVisaoTerrestre);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origem, raioVisaoAerea);

        if (alvoTerrestreAtual != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origem, alvoTerrestreAtual.position);
            Gizmos.DrawSphere(alvoTerrestreAtual.position, 0.35f);
        }

        if (alvoAereoAtual != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(origem, alvoAereoAtual.position);
            Gizmos.DrawSphere(alvoAereoAtual.position, 0.45f);
        }

        if (alvoAtual != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origem, alvoAtual.position);
        }
    }
}