using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class ProjetilDistancia : MonoBehaviour
{
    [Header("Configuracao")]
    [SerializeField] private int dano = 1;
    [SerializeField] private float velocidade = 20f;
    [SerializeField] private float tempoDeVida = 3f;
    [SerializeField] private bool mirarComAlturaDoAlvo = false;

    [Header("Alvo")]
    [SerializeField] private string[] tagsQueRecebemDano = { "Vermelho" };

    [Header("Deteccao de impacto")]
    [SerializeField] private LayerMask camadasDeImpacto = ~0;
    [SerializeField] private bool detectarTriggers = true;
    [SerializeField] private float raioDeteccaoImpacto = 0.12f;
    [SerializeField] private float margemDeteccaoImpacto = 0.05f;
    [SerializeField] private bool usarRigidbodySeExistir = true;
    [SerializeField] private bool configurarRigidbodyAutomaticamente = true;
    [SerializeField] private bool destruirMesmoSemDano = true;

    [Header("Dano em BaseVida")]
    [SerializeField] private bool aplicarDanoEmBaseVida = true;
    [SerializeField] private bool baseVidaIgnoraTagDoAlvo = true;
    [SerializeField] private bool forcarDanoNaBaseVidaSemValidarTagDoAtacante = false;

    [Header("Dano generico")]
    [SerializeField] private bool aplicarDanoEmVida = true;
    [SerializeField] private bool aplicarDanoPorMetodos = true;

    private Transform alvo;
    private Rigidbody rb;
    private Vector3 direcaoInicial;
    private bool jaColidiu;
    private bool direcaoDefinida;

    public void Configurar(Transform novoAlvo, int novoDano, float novaVelocidade)
    {
        alvo = novoAlvo;
        dano = novoDano;
        velocidade = novaVelocidade;

        DefinirDirecaoInicial();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        AplicarConfiguracaoRigidbody();
    }

    private void Start()
    {
        if (!direcaoDefinida)
            direcaoInicial = transform.forward.normalized;

        if (direcaoInicial.sqrMagnitude < 0.001f)
            direcaoInicial = transform.forward.normalized;

        Destroy(gameObject, tempoDeVida);
    }

    private void FixedUpdate()
    {
        if (jaColidiu)
            return;

        MoverComDeteccaoContinua(Time.fixedDeltaTime);
    }

    private void DefinirDirecaoInicial()
    {
        if (alvo != null)
        {
            Vector3 destino = alvo.position;

            if (!mirarComAlturaDoAlvo)
                destino.y = transform.position.y;

            direcaoInicial = destino - transform.position;

            if (direcaoInicial.sqrMagnitude < 0.001f)
                direcaoInicial = transform.forward;
        }
        else
        {
            direcaoInicial = transform.forward;
        }

        direcaoInicial.Normalize();

        if (direcaoInicial.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direcaoInicial, Vector3.up);

        direcaoDefinida = true;
    }

    private void AplicarConfiguracaoRigidbody()
    {
        if (!configurarRigidbodyAutomaticamente || rb == null)
            return;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void MoverComDeteccaoContinua(float deltaTime)
    {
        Vector3 direcao = direcaoInicial.normalized;

        if (direcao.sqrMagnitude < 0.001f)
            direcao = transform.forward.normalized;

        float distanciaMovimento = Mathf.Max(0f, velocidade * deltaTime);

        if (distanciaMovimento <= 0f)
            return;

        Vector3 origem = transform.position;

        if (TentarDetectarImpacto(origem, direcao, distanciaMovimento, out RaycastHit hit))
        {
            Vector3 posicaoImpacto = hit.point;

            if (posicaoImpacto == Vector3.zero)
                posicaoImpacto = origem + direcao * hit.distance;

            transform.position = posicaoImpacto;
            ColidiuComCollider(hit.collider);
            return;
        }

        Vector3 novaPosicao = origem + direcao * distanciaMovimento;

        if (usarRigidbodySeExistir && rb != null)
            rb.MovePosition(novaPosicao);
        else
            transform.position = novaPosicao;
    }

    private bool TentarDetectarImpacto(Vector3 origem, Vector3 direcao, float distanciaMovimento, out RaycastHit melhorHit)
    {
        melhorHit = new RaycastHit();

        QueryTriggerInteraction triggerMode = detectarTriggers
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        float distanciaTotal = distanciaMovimento + Mathf.Max(0f, margemDeteccaoImpacto);
        float raio = Mathf.Max(0.01f, raioDeteccaoImpacto);

        RaycastHit[] hits = Physics.SphereCastAll(
            origem,
            raio,
            direcao.normalized,
            distanciaTotal,
            camadasDeImpacto,
            triggerMode
        );

        bool encontrou = false;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider colisor = hits[i].collider;

            if (colisor == null)
                continue;

            if (ColisorEhDoProprioProjetil(colisor))
                continue;

            if (hits[i].distance < menorDistancia)
            {
                menorDistancia = hits[i].distance;
                melhorHit = hits[i];
                encontrou = true;
            }
        }

        return encontrou;
    }

    private bool ColisorEhDoProprioProjetil(Collider colisor)
    {
        if (colisor == null)
            return true;

        Transform alvoTransform = colisor.transform;

        if (alvoTransform == transform)
            return true;

        if (alvoTransform.IsChildOf(transform))
            return true;

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        ColidiuComCollider(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        ColidiuComCollider(other);
    }

    public void ColidiuCom(GameObject objetoAtingido)
    {
        if (objetoAtingido == null)
            return;

        ColidiuComTransform(objetoAtingido.transform);
    }

    private void ColidiuComCollider(Collider colisorAtingido)
    {
        if (colisorAtingido == null)
            return;

        if (ColisorEhDoProprioProjetil(colisorAtingido))
            return;

        ColidiuComTransform(colisorAtingido.transform);
    }

    private void ColidiuComTransform(Transform transformAtingido)
    {
        if (jaColidiu)
            return;

        jaColidiu = true;

        bool aplicouDano = TentarAplicarDano(transformAtingido);

        if (aplicouDano || destruirMesmoSemDano)
            Destroy(gameObject);
        else
            jaColidiu = false;
    }

    private bool TentarAplicarDano(Transform transformAtingido)
    {
        if (transformAtingido == null)
            return false;

        if (aplicarDanoEmBaseVida && TentarAplicarDanoEmBaseVida(transformAtingido))
            return true;

        bool alvoTemTagPermitida = ObjetoOuFamiliaTemTagPermitida(transformAtingido);

        if (!alvoTemTagPermitida)
            return false;

        if (aplicarDanoEmVida && TentarAplicarDanoEmVida(transformAtingido))
            return true;

        if (aplicarDanoPorMetodos && TentarAplicarDanoPorMetodo(transformAtingido, dano))
            return true;

        return false;
    }

    private bool TentarAplicarDanoEmBaseVida(Transform transformAtingido)
    {
        BaseVida baseVida = EncontrarComponenteNaHierarquia<BaseVida>(transformAtingido);

        if (baseVida == null)
            return false;

        if (!baseVidaIgnoraTagDoAlvo && !ObjetoOuFamiliaTemTagPermitida(transformAtingido))
            return false;

        if (forcarDanoNaBaseVidaSemValidarTagDoAtacante)
            baseVida.ReceberDano(dano);
        else
            baseVida.ReceberDano(dano, gameObject);

        return true;
    }

    private bool TentarAplicarDanoEmVida(Transform transformAtingido)
    {
        Vida vida = EncontrarComponenteNaHierarquia<Vida>(transformAtingido);

        if (vida == null)
            return false;

        vida.AplicarDano(dano);
        return true;
    }

    private T EncontrarComponenteNaHierarquia<T>(Transform origem) where T : Component
    {
        if (origem == null)
            return null;

        T componente = origem.GetComponent<T>();

        if (componente != null)
            return componente;

        componente = origem.GetComponentInParent<T>(true);

        if (componente != null)
            return componente;

        componente = origem.GetComponentInChildren<T>(true);

        if (componente != null)
            return componente;

        Transform atual = origem.parent;

        while (atual != null)
        {
            componente = atual.GetComponentInChildren<T>(true);

            if (componente != null)
                return componente;

            atual = atual.parent;
        }

        return null;
    }

    private bool ObjetoOuFamiliaTemTagPermitida(Transform origem)
    {
        if (tagsQueRecebemDano == null || tagsQueRecebemDano.Length == 0)
            return true;

        if (origem == null)
            return false;

        Transform atual = origem;

        while (atual != null)
        {
            if (TemTagPermitida(atual.gameObject))
                return true;

            atual = atual.parent;
        }

        Transform[] filhos = origem.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < filhos.Length; i++)
        {
            if (filhos[i] != null && TemTagPermitida(filhos[i].gameObject))
                return true;
        }

        return false;
    }

    private bool TemTagPermitida(GameObject obj)
    {
        if (obj == null)
            return false;

        if (tagsQueRecebemDano == null || tagsQueRecebemDano.Length == 0)
            return true;

        for (int i = 0; i < tagsQueRecebemDano.Length; i++)
        {
            string tagPermitida = tagsQueRecebemDano[i];

            if (string.IsNullOrWhiteSpace(tagPermitida))
                continue;

            if (obj.CompareTag(tagPermitida))
                return true;
        }

        return false;
    }

    private bool TentarAplicarDanoPorMetodo(Transform alvoTransform, int valorDano)
    {
        if (alvoTransform == null)
            return false;

        if (TentarInvocarMetodoDeDanoNosComponentes(alvoTransform.GetComponents<MonoBehaviour>(), valorDano))
            return true;

        if (TentarInvocarMetodoDeDanoNosComponentes(alvoTransform.GetComponentsInChildren<MonoBehaviour>(true), valorDano))
            return true;

        if (TentarInvocarMetodoDeDanoNosComponentes(alvoTransform.GetComponentsInParent<MonoBehaviour>(true), valorDano))
            return true;

        return false;
    }

    private bool TentarInvocarMetodoDeDanoNosComponentes(MonoBehaviour[] componentes, int valorDano)
    {
        if (componentes == null || componentes.Length == 0)
            return false;

        for (int i = 0; i < componentes.Length; i++)
        {
            MonoBehaviour comp = componentes[i];

            if (comp == null || comp == this)
                continue;

            if (TentarInvocarMetodo(comp, "AplicarDano", valorDano))
                return true;

            if (TentarInvocarMetodo(comp, "ReceberDano", valorDano))
                return true;

            if (TentarInvocarMetodo(comp, "TomarDano", valorDano))
                return true;
        }

        return false;
    }

    private bool TentarInvocarMetodo(MonoBehaviour componente, string nomeMetodo, int valorDano)
    {
        MethodInfo metodo = componente.GetType().GetMethod(
            nomeMetodo,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(int) },
            null
        );

        if (metodo == null)
            return false;

        metodo.Invoke(componente, new object[] { valorDano });
        return true;
    }

    private void OnValidate()
    {
        dano = Mathf.Max(0, dano);
        velocidade = Mathf.Max(0f, velocidade);
        tempoDeVida = Mathf.Max(0.05f, tempoDeVida);
        raioDeteccaoImpacto = Mathf.Max(0.01f, raioDeteccaoImpacto);
        margemDeteccaoImpacto = Mathf.Max(0f, margemDeteccaoImpacto);
    }
}
