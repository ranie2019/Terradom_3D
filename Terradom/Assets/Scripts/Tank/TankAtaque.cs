using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class TankAtaque : MonoBehaviour
{
    private enum EixoFrenteMira
    {
        XPositivo,
        XNegativo,
        ZPositivo,
        ZNegativo
    }

    [Header("Referencia da visao")]
    [SerializeField] private TankVisao tankVisao;

    [Header("Mira do tank")]
    [SerializeField] private Transform miraGiroY360;
    [SerializeField] private Transform miraElevacaoZ;
    [SerializeField] private Transform spawnBala;
    [SerializeField] private EixoFrenteMira eixoFrenteDaMira = EixoFrenteMira.XPositivo;

    [Header("Giro horizontal - eixo Y 360 graus")]
    [SerializeField] private float velocidadeGiroY = 180f;

    [Header("Elevacao do canhao - eixo Z limitado")]
    [SerializeField] private float anguloMinimoZ = -9f;
    [SerializeField] private float anguloMaximoZ = 45f;
    [SerializeField] private float velocidadeGiroZ = 120f;
    [SerializeField] private bool inverterElevacaoZ = false;

    [Header("Ataque")]
    [SerializeField] private bool atacarAutomaticamente = true;
    [SerializeField] private bool atacarAlvoTerrestre = true;
    [SerializeField] private bool atacarAlvoAereo = true;
    [SerializeField] private GameObject prefabBala;
    [SerializeField] private float intervaloEntreTiros = 1.2f;
    [SerializeField] private float velocidadeBala = 25f;
    [SerializeField] private float tempoVidaBala = 5f;
    [SerializeField] private float toleranciaMiraParaAtirar = 6f;
    [SerializeField] private float alturaExtraMiraAlvo = 0.2f;

    [Header("Comportamento sem alvo")]
    [SerializeField] private bool centralizarMiraSemAlvo = true;
    [SerializeField] private float velocidadeCentralizarSemAlvo = 90f;

    [Header("Debug")]
    [SerializeField] private bool desenharLinhaMiraNoEditor = true;

    private Quaternion rotacaoOriginalGiroY;
    private Quaternion rotacaoOriginalElevacaoZ;

    private Transform alvoAtual;
    private float proximoTiroEm;

    private void Awake()
    {
        if (tankVisao == null)
            tankVisao = GetComponent<TankVisao>();

        if (miraGiroY360 == null)
            miraGiroY360 = transform;

        if (miraElevacaoZ == null)
            miraElevacaoZ = miraGiroY360;

        if (spawnBala == null)
            spawnBala = miraElevacaoZ;

        rotacaoOriginalGiroY = miraGiroY360.localRotation;
        rotacaoOriginalElevacaoZ = miraElevacaoZ.localRotation;
    }

    private void Update()
    {
        AtualizarAlvoAtual();

        if (alvoAtual == null)
        {
            AtualizarMiraSemAlvo(Time.deltaTime);
            return;
        }

        Vector3 pontoMira = ObterPontoMiraDoAlvo(alvoAtual);

        GirarMiraHorizontalY(pontoMira, Time.deltaTime);
        GirarCanhaoElevacaoZ(pontoMira, Time.deltaTime);

        if (atacarAutomaticamente)
            TentarAtirar(pontoMira);
    }

    private void AtualizarAlvoAtual()
    {
        alvoAtual = null;

        if (tankVisao == null || !tankVisao.TemAlvo)
            return;

        if (tankVisao.TipoAlvoAtual == TankVisao.TipoAlvoTank.Terrestre && !atacarAlvoTerrestre)
            return;

        if (tankVisao.TipoAlvoAtual == TankVisao.TipoAlvoTank.Aereo && !atacarAlvoAereo)
            return;

        alvoAtual = tankVisao.AlvoAtual;
    }

    private void GirarMiraHorizontalY(Vector3 pontoMira, float deltaTime)
    {
        if (miraGiroY360 == null)
            return;

        Vector3 direcaoMundo = pontoMira - miraGiroY360.position;
        direcaoMundo.y = 0f;

        if (direcaoMundo.sqrMagnitude <= 0.0001f)
            return;

        Transform pai = miraGiroY360.parent;

        Vector3 direcaoLocalPai = pai != null
            ? pai.InverseTransformDirection(direcaoMundo.normalized)
            : direcaoMundo.normalized;

        direcaoLocalPai.y = 0f;
        direcaoLocalPai.Normalize();

        Vector3 frenteOriginalLocal = rotacaoOriginalGiroY * ObterEixoFrenteLocal();
        frenteOriginalLocal.y = 0f;

        if (frenteOriginalLocal.sqrMagnitude <= 0.0001f)
            frenteOriginalLocal = Vector3.right;

        frenteOriginalLocal.Normalize();

        Quaternion deltaRotacao = Quaternion.FromToRotation(frenteOriginalLocal, direcaoLocalPai);
        Quaternion rotacaoDesejada = deltaRotacao * rotacaoOriginalGiroY;

        miraGiroY360.localRotation = Quaternion.RotateTowards(
            miraGiroY360.localRotation,
            rotacaoDesejada,
            velocidadeGiroY * deltaTime
        );
    }

    private void GirarCanhaoElevacaoZ(Vector3 pontoMira, float deltaTime)
    {
        if (miraElevacaoZ == null)
            return;

        Transform referencia = miraElevacaoZ.parent != null ? miraElevacaoZ.parent : miraGiroY360;

        if (referencia == null)
            return;

        Vector3 direcaoLocal = referencia.InverseTransformDirection(pontoMira - miraElevacaoZ.position);

        if (direcaoLocal.sqrMagnitude <= 0.0001f)
            return;

        float anguloDesejado = CalcularAnguloElevacaoZ(direcaoLocal);

        if (inverterElevacaoZ)
            anguloDesejado *= -1f;

        anguloDesejado = Mathf.Clamp(anguloDesejado, anguloMinimoZ, anguloMaximoZ);

        Quaternion rotacaoDesejada = rotacaoOriginalElevacaoZ * Quaternion.AngleAxis(anguloDesejado, Vector3.forward);

        miraElevacaoZ.localRotation = Quaternion.RotateTowards(
            miraElevacaoZ.localRotation,
            rotacaoDesejada,
            velocidadeGiroZ * deltaTime
        );
    }

    private float CalcularAnguloElevacaoZ(Vector3 direcaoLocal)
    {
        switch (eixoFrenteDaMira)
        {
            case EixoFrenteMira.XNegativo:
                return Mathf.Atan2(direcaoLocal.y, -direcaoLocal.x) * Mathf.Rad2Deg;

            case EixoFrenteMira.ZPositivo:
                return Mathf.Atan2(direcaoLocal.y, direcaoLocal.z) * Mathf.Rad2Deg;

            case EixoFrenteMira.ZNegativo:
                return Mathf.Atan2(direcaoLocal.y, -direcaoLocal.z) * Mathf.Rad2Deg;

            default:
                return Mathf.Atan2(direcaoLocal.y, direcaoLocal.x) * Mathf.Rad2Deg;
        }
    }

    private void TentarAtirar(Vector3 pontoMira)
    {
        if (prefabBala == null || spawnBala == null)
            return;

        if (Time.time < proximoTiroEm)
            return;

        if (!MiraEstaAlinhada(pontoMira))
            return;

        Atirar(pontoMira);
        proximoTiroEm = Time.time + Mathf.Max(0.05f, intervaloEntreTiros);
    }

    private bool MiraEstaAlinhada(Vector3 pontoMira)
    {
        Vector3 origemTiro = spawnBala != null ? spawnBala.position : miraElevacaoZ.position;
        Vector3 direcaoParaAlvo = pontoMira - origemTiro;

        if (direcaoParaAlvo.sqrMagnitude <= 0.0001f)
            return false;

        Vector3 frenteCanhao = ObterFrenteCanhaoMundo();
        float erroAngulo = Vector3.Angle(frenteCanhao, direcaoParaAlvo.normalized);

        return erroAngulo <= toleranciaMiraParaAtirar;
    }

    private void Atirar(Vector3 pontoMira)
    {
        Vector3 origemTiro = spawnBala.position;
        Vector3 direcaoTiro = ObterFrenteCanhaoMundo();

        if ((pontoMira - origemTiro).sqrMagnitude > 0.0001f)
            direcaoTiro = (pontoMira - origemTiro).normalized;

        Quaternion rotacaoBala = Quaternion.LookRotation(direcaoTiro, Vector3.up);
        GameObject balaCriada = Instantiate(prefabBala, origemTiro, rotacaoBala);

        Rigidbody rbBala = balaCriada.GetComponent<Rigidbody>();

        if (rbBala != null)
        {
#if UNITY_6000_0_OR_NEWER
            rbBala.linearVelocity = direcaoTiro * velocidadeBala;
#else
            rbBala.velocity = direcaoTiro * velocidadeBala;
#endif
        }
        else
        {
            StartCoroutine(MoverBalaSemRigidbody(balaCriada.transform, direcaoTiro));
        }

        if (tempoVidaBala > 0f)
            Destroy(balaCriada, tempoVidaBala);
    }

    private IEnumerator MoverBalaSemRigidbody(Transform bala, Vector3 direcao)
    {
        while (bala != null)
        {
            bala.position += direcao * velocidadeBala * Time.deltaTime;
            yield return null;
        }
    }

    private Vector3 ObterPontoMiraDoAlvo(Transform alvo)
    {
        if (alvo == null)
            return transform.position;

        Collider colisor = alvo.GetComponentInChildren<Collider>();

        if (colisor != null)
            return colisor.bounds.center + Vector3.up * alturaExtraMiraAlvo;

        return alvo.position + Vector3.up * alturaExtraMiraAlvo;
    }

    private Vector3 ObterFrenteCanhaoMundo()
    {
        Transform referencia = spawnBala != null ? spawnBala : miraElevacaoZ;

        if (referencia == null)
            return transform.right;

        return referencia.TransformDirection(ObterEixoFrenteLocal()).normalized;
    }

    private Vector3 ObterEixoFrenteLocal()
    {
        switch (eixoFrenteDaMira)
        {
            case EixoFrenteMira.XNegativo:
                return Vector3.left;

            case EixoFrenteMira.ZPositivo:
                return Vector3.forward;

            case EixoFrenteMira.ZNegativo:
                return Vector3.back;

            default:
                return Vector3.right;
        }
    }

    private void AtualizarMiraSemAlvo(float deltaTime)
    {
        if (!centralizarMiraSemAlvo)
            return;

        if (miraGiroY360 != null)
        {
            miraGiroY360.localRotation = Quaternion.RotateTowards(
                miraGiroY360.localRotation,
                rotacaoOriginalGiroY,
                velocidadeCentralizarSemAlvo * deltaTime
            );
        }

        if (miraElevacaoZ != null)
        {
            miraElevacaoZ.localRotation = Quaternion.RotateTowards(
                miraElevacaoZ.localRotation,
                rotacaoOriginalElevacaoZ,
                velocidadeCentralizarSemAlvo * deltaTime
            );
        }
    }

    private void OnValidate()
    {
        velocidadeGiroY = Mathf.Max(1f, velocidadeGiroY);
        velocidadeGiroZ = Mathf.Max(1f, velocidadeGiroZ);
        intervaloEntreTiros = Mathf.Max(0.05f, intervaloEntreTiros);
        velocidadeBala = Mathf.Max(0.1f, velocidadeBala);
        tempoVidaBala = Mathf.Max(0f, tempoVidaBala);
        toleranciaMiraParaAtirar = Mathf.Clamp(toleranciaMiraParaAtirar, 0.1f, 45f);
        velocidadeCentralizarSemAlvo = Mathf.Max(1f, velocidadeCentralizarSemAlvo);

        if (anguloMinimoZ > anguloMaximoZ)
        {
            float temp = anguloMinimoZ;
            anguloMinimoZ = anguloMaximoZ;
            anguloMaximoZ = temp;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!desenharLinhaMiraNoEditor)
            return;

        Transform origem = spawnBala != null ? spawnBala : miraElevacaoZ;

        if (origem == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origem.position, origem.position + origem.TransformDirection(ObterEixoFrenteLocal()).normalized * 8f);

        if (alvoAtual != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origem.position, ObterPontoMiraDoAlvo(alvoAtual));
        }
    }
}
