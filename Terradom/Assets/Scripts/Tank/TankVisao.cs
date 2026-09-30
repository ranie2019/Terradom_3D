using System;
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

    [Header("Linha de visao")]
    [SerializeField] private bool exigirLinhaDeVisao = true;
    [SerializeField] private LayerMask camadasBloqueiamVisao = ~0;

    [Header("Debug")]
    [SerializeField] private bool desenharVisaoNoEditor = true;

    private Transform alvoAtual;
    private Transform alvoTerrestreAtual;
    private Transform alvoAereoAtual;

    private TipoAlvoTank tipoAlvoAtual = TipoAlvoTank.Nenhum;

    private float proximaBusca;
    private Collider[] bufferCollidersVisao = new Collider[64];
    private RaycastHit[] bufferRaycastVisao = new RaycastHit[32];

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
        int quantidadeColliders = BuscarCollidersNaEsfera(origem, raioVisaoTerrestre);

        Transform melhorAlvo = null;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < quantidadeColliders; i++)
        {
            Collider colisor = bufferCollidersVisao[i];

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
            if (distancia >= menorDistancia)
                continue;

            if (!TemLinhaDeVisao(colisor, alvo, origem))
                continue;

            menorDistancia = distancia;
            melhorAlvo = alvo;
        }

        return melhorAlvo;
    }

    private Transform BuscarAlvoAereoMaisProximo()
    {
        Vector3 origem = ObterOrigemVisao();
        int quantidadeColliders = BuscarCollidersNaEsfera(origem, raioVisaoAerea);

        Transform melhorAlvo = null;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < quantidadeColliders; i++)
        {
            Collider colisor = bufferCollidersVisao[i];

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
            if (distancia >= menorDistancia)
                continue;

            if (!TemLinhaDeVisao(colisor, alvo, origem))
                continue;

            menorDistancia = distancia;
            melhorAlvo = alvo;
        }

        return melhorAlvo;
    }

    private int BuscarCollidersNaEsfera(Vector3 origem, float raio)
    {
        QueryTriggerInteraction triggerMode = detectarTriggers
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        while (true)
        {
            int quantidade = Physics.OverlapSphereNonAlloc(
                origem,
                raio,
                bufferCollidersVisao,
                ~0,
                triggerMode
            );

            if (quantidade < bufferCollidersVisao.Length)
                return quantidade;

            Array.Resize(ref bufferCollidersVisao, bufferCollidersVisao.Length * 2);
        }
    }

    private bool TemLinhaDeVisao(Collider colisorAlvo, Transform alvo, Vector3 origem)
    {
        if (!exigirLinhaDeVisao || colisorAlvo == null || alvo == null)
            return true;

        Vector3 pontoAlvo = colisorAlvo.ClosestPoint(origem);
        Vector3 deslocamento = pontoAlvo - origem;
        float distancia = deslocamento.magnitude;

        if (distancia <= 0.05f)
            return true;

        float distanciaConsulta = Mathf.Max(0f, distancia - 0.02f);
        QueryTriggerInteraction triggerMode = detectarTriggers
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        while (true)
        {
            int quantidade = Physics.RaycastNonAlloc(
                origem,
                deslocamento / distancia,
                bufferRaycastVisao,
                distanciaConsulta,
                camadasBloqueiamVisao,
                triggerMode
            );

            bool encontrouBloqueio = false;
            for (int i = 0; i < quantidade; i++)
            {
                Collider atingido = bufferRaycastVisao[i].collider;
                if (atingido == null || EhDoProprioTank(atingido.transform))
                    continue;

                Transform transformAtingido = atingido.transform;
                if (transformAtingido == alvo || transformAtingido.IsChildOf(alvo))
                    continue;

                encontrouBloqueio = true;
                break;
            }

            if (quantidade < bufferRaycastVisao.Length)
                return !encontrouBloqueio;

            Array.Resize(ref bufferRaycastVisao, bufferRaycastVisao.Length * 2);
        }
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

        // A cena agrupa as bases sob um objeto pai (por exemplo, "Bases").
        // Não suba até esse agrupador: use o objeto que realmente possui a vida.
        BaseVidaIA vidaBaseIA = alvo.GetComponentInParent<BaseVidaIA>();
        if (vidaBaseIA != null)
            return vidaBaseIA.transform;

        BaseVida vidaBase = alvo.GetComponentInParent<BaseVida>();
        if (vidaBase != null)
            return vidaBase.transform;

        TankLeve tank = alvo.GetComponentInParent<TankLeve>();
        if (tank != null)
            return tank.transform;

        Vida vida = alvo.GetComponentInParent<Vida>();
        if (vida != null)
            return vida.transform;

        Transform atual = alvo;
        Transform ultimoObjetoDaEquipe = null;

        while (atual != null && atual != transform && !atual.IsChildOf(transform))
        {
            if (EhTagDeEquipe(atual.tag))
            {
                ultimoObjetoDaEquipe = atual;
                atual = atual.parent;
                continue;
            }

            // O primeiro objeto sem tag de equipe acima do alvo costuma ser
            // apenas um agrupador da cena. Mantemos o último membro da equipe.
            if (ultimoObjetoDaEquipe != null)
                break;

            atual = atual.parent;
        }

        return ultimoObjetoDaEquipe != null ? ultimoObjetoDaEquipe : alvo;
    }

    private bool EhTagDeEquipe(string tag)
    {
        return tag == "Azul" || tag == "Vermelho" || tag == "Verde";
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
