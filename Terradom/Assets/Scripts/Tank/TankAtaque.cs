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

    // =====================================================================
    // REFER�NCIA DE VIS�O
    // =====================================================================

    [Header("Referencia da visao")]
    [SerializeField] private TankVisao tankVisao;
    private TankLeve tankLeve;

    // =====================================================================
    // MIRA TERRESTRE
    // =====================================================================

    [Header("Mira Terrestre � Giro Y e Elevacao Z")]
    [SerializeField] private Transform miraGiroY360;
    [SerializeField] private Transform miraElevacaoZ;
    [SerializeField] private Transform spawnBala;
    [SerializeField] private EixoFrenteMira eixoFrenteDaMira = EixoFrenteMira.XPositivo;

    [Header("Mira Terrestre � Velocidades")]
    [SerializeField] private float velocidadeGiroY = 180f;
    [SerializeField] private float anguloMinimoZ   = -9f;
    [SerializeField] private float anguloMaximoZ   = 45f;
    [SerializeField] private float velocidadeGiroZ = 120f;
    [SerializeField] private bool  inverterElevacaoZ = false;

    [Header("Mira Terrestre � Ataque")]
    [SerializeField] private bool       atacarAlvoTerrestre        = true;
    [SerializeField] private bool       atacarAutomaticamente      = true;
    [SerializeField] private GameObject prefabBala                 = null;
    [SerializeField] private int        danoBala                   = 5;
    [SerializeField] private float      intervaloEntreTiros        = 1.2f;
    [SerializeField] private float      velocidadeBala             = 25f;
    [SerializeField] private float      tempoVidaBala              = 5f;
    [SerializeField] private float      toleranciaMiraParaAtirar   = 6f;
    [SerializeField] private float      alturaExtraMiraAlvo        = 0.2f;

    [Header("Audio de disparo")]
    [Tooltip("AudioSource 3D do disparo. Se nao for atribuido, sera procurado neste objeto e nos filhos.")]
    [SerializeField] private AudioSource audioSourceDisparo;
    [Tooltip("Clip tocado a cada disparo. Mantido separado do AudioSource para funcionar com Audio Resource no Unity 6.")]
    [SerializeField] private AudioClip audioClipDisparo;
    [Tooltip("Distancia maxima em unidades do jogo para ouvir o disparo.")]
    [SerializeField, Min(1f)] private float distanciaMaximaAudioDisparo = 120f;

    // =====================================================================
    // MIRA ANTIA�REA
    // =====================================================================

    [Header("Mira Antiaerea � Giro Y e Elevacao Z")]
    [SerializeField] private Transform miraAereaGiroY360;
    [SerializeField] private Transform miraAereaElevacaoZ;
    [SerializeField] private Transform spawnBalaAerea;
    [SerializeField] private EixoFrenteMira eixoFrenteDaMiraAerea = EixoFrenteMira.XPositivo;

    [Header("Mira Antiaerea � Velocidades")]
    [SerializeField] private float velocidadeGiroYAerea = 270f;
    [SerializeField] private float anguloMinimoZAerea   = 10f;
    [SerializeField] private float anguloMaximoZAerea   = 85f;
    [SerializeField] private float velocidadeGiroZAerea = 200f;
    [SerializeField] private bool  inverterElevacaoZAerea = false;

    [Header("Mira Antiaerea � Ataque")]
    [SerializeField] private bool       atacarAlvoAereo                  = true;
    [SerializeField] private bool       atacarAutomaticamenteAerea        = true;
    [SerializeField] private GameObject prefabBalaAerea                   = null;
    [SerializeField] private int        danoBalaAerea                     = 5;
    [SerializeField] private float      intervaloEntreTirosAerea          = 0.4f;
    [SerializeField] private float      velocidadeBalaAerea               = 45f;
    [SerializeField] private float      tempoVidaBalaAerea                = 6f;
    [SerializeField] private float      toleranciaMiraParaAtirarAerea     = 10f;
    [SerializeField] private float      alturaExtraMiraAlvoAerea          = 0f;

    // =====================================================================
    // COMPORTAMENTO SEM ALVO
    // =====================================================================

    [Header("Comportamento sem alvo")]
    [SerializeField] private bool  centralizarMiraSemAlvo          = true;
    [SerializeField] private float velocidadeCentralizarSemAlvo    = 90f;
    [SerializeField] private bool  centralizarMiraAereaSemAlvo     = true;
    [SerializeField] private float velocidadeCentralizarAereaSemAlvo = 120f;

    // =====================================================================
    // DEBUG
    // =====================================================================

    [Header("Debug")]
    [SerializeField] private bool desenharLinhaMiraNoEditor = true;

    // =====================================================================
    // PRIVADOS
    // =====================================================================

    private Quaternion rotacaoOriginalGiroY;
    private Quaternion rotacaoOriginalElevacaoZ;

    private Quaternion rotacaoOriginalGiroYAerea;
    private Quaternion rotacaoOriginalElevacaoZAerea;

    private Transform alvoTerrestre;
    private Transform alvoAereo;

    private float proximoTiroEm;
    private float proximoTiroAereoEm;

    // =====================================================================
    // AWAKE
    // =====================================================================

    private void Awake()
    {
        if (audioSourceDisparo == null)
            audioSourceDisparo = GetComponent<AudioSource>();
        if (audioSourceDisparo == null)
            audioSourceDisparo = GetComponentInChildren<AudioSource>(true);
        ConfigurarAudioDisparo();

        if (tankVisao == null)
            tankVisao = GetComponent<TankVisao>();

        tankLeve = GetComponentInParent<TankLeve>();

        // Mira terrestre � fallbacks
        if (miraGiroY360  == null) miraGiroY360  = transform;
        if (miraElevacaoZ == null) miraElevacaoZ = miraGiroY360;
        if (spawnBala     == null) spawnBala     = miraElevacaoZ;

        rotacaoOriginalGiroY     = miraGiroY360.localRotation;
        rotacaoOriginalElevacaoZ = miraElevacaoZ.localRotation;

        // Mira antia�rea � guarda rota��es originais s� se os transforms existirem
        if (miraAereaGiroY360  != null) rotacaoOriginalGiroYAerea     = miraAereaGiroY360.localRotation;
        if (miraAereaElevacaoZ != null) rotacaoOriginalElevacaoZAerea = miraAereaElevacaoZ.localRotation;
        if (spawnBalaAerea     == null && miraAereaElevacaoZ != null)
            spawnBalaAerea = miraAereaElevacaoZ;
    }

    // =====================================================================
    // UPDATE
    // =====================================================================

    private void Update()
    {
        AtualizarAlvos();
        bool mirasCompartilhadas = MirasCompartilhamTransform();

        // MIRA TERRESTRE
        if (alvoTerrestre != null)
        {
            Vector3 pontoMira = ObterPontoMiraPrevisto(alvoTerrestre, alturaExtraMiraAlvo, velocidadeBala, spawnBala);
            GirarMiraHorizontalY(pontoMira, Time.deltaTime);
            GirarCanhaoElevacaoZ(pontoMira, Time.deltaTime);

            if (atacarAutomaticamente)
                TentarAtirar(alvoTerrestre, pontoMira);
        }
        else if (!mirasCompartilhadas || (alvoTerrestre == null && alvoAereo == null))
        {
            CentralizarMiraTerrestre(Time.deltaTime);
        }

        // MIRA ANTIA�REA
        if (alvoAereo != null && MiraAereaConfigurada())
        {
            Vector3 pontoMiraAereo = ObterPontoMiraPrevisto(alvoAereo, alturaExtraMiraAlvoAerea, velocidadeBalaAerea, spawnBalaAerea);
            GirarMiraAereaHorizontalY(pontoMiraAereo, Time.deltaTime);
            GirarMiraAereaElevacaoZ(pontoMiraAereo, Time.deltaTime);

            if (atacarAutomaticamenteAerea)
                TentarAtirarAereo(alvoAereo, pontoMiraAereo);
        }
        else if (!mirasCompartilhadas)
        {
            CentralizarMiraAerea(Time.deltaTime);
        }
    }

    // =====================================================================
    // ATUALIZAR ALVOS
    // =====================================================================

    private void AtualizarAlvos()
    {
        alvoTerrestre = null;
        alvoAereo     = null;

        if (tankVisao == null) return;
        // Uma torre fisica so pode apontar para um alvo por vez. Segue a
        // prioridade selecionada pelo TankVisao para evitar duas miras em conflito.
        if (tankVisao.TemAlvo)
        {
            if (tankVisao.TipoAlvoAtual == TankVisao.TipoAlvoTank.Terrestre && atacarAlvoTerrestre)
                alvoTerrestre = tankVisao.AlvoAtual;
            else if (tankVisao.TipoAlvoAtual == TankVisao.TipoAlvoTank.Aereo && atacarAlvoAereo)
                alvoAereo = tankVisao.AlvoAtual;
        }

        // Se o tipo prioritario nao pode ser atacado por este prefab, tenta o outro.
        if (alvoTerrestre == null && alvoAereo == null)
        {
            if (atacarAlvoTerrestre && tankVisao.TemAlvoTerrestre)
                alvoTerrestre = tankVisao.AlvoTerrestreAtual;
            else if (atacarAlvoAereo && tankVisao.TemAlvoAereo)
                alvoAereo = tankVisao.AlvoAereoAtual;
        }
    }

    private bool MiraAereaConfigurada() =>
        miraAereaGiroY360 != null && miraAereaElevacaoZ != null;

    private bool MirasCompartilhamTransform()
    {
        return (miraGiroY360 != null && miraGiroY360 == miraAereaGiroY360) ||
               (miraElevacaoZ != null && miraElevacaoZ == miraAereaElevacaoZ);
    }

    // =====================================================================
    // MIRA TERRESTRE � GIRO
    // =====================================================================

    private void GirarMiraHorizontalY(Vector3 pontoMira, float deltaTime)
    {
        if (miraGiroY360 == null) return;

        Vector3 direcaoMundo = pontoMira - miraGiroY360.position;
        direcaoMundo.y = 0f;
        if (direcaoMundo.sqrMagnitude <= 0.0001f) return;

        Transform pai = miraGiroY360.parent;
        Vector3 direcaoLocalPai = pai != null
            ? pai.InverseTransformDirection(direcaoMundo.normalized)
            : direcaoMundo.normalized;
        direcaoLocalPai.y = 0f;
        direcaoLocalPai.Normalize();

        Vector3 frenteOriginalLocal = rotacaoOriginalGiroY * ObterEixoFrenteLocal(eixoFrenteDaMira);
        frenteOriginalLocal.y = 0f;
        if (frenteOriginalLocal.sqrMagnitude <= 0.0001f) frenteOriginalLocal = Vector3.right;
        frenteOriginalLocal.Normalize();

        Quaternion rotacaoDesejada = Quaternion.FromToRotation(frenteOriginalLocal, direcaoLocalPai) * rotacaoOriginalGiroY;

        miraGiroY360.localRotation = Quaternion.RotateTowards(
            miraGiroY360.localRotation, rotacaoDesejada, velocidadeGiroY * deltaTime);
    }

    private void GirarCanhaoElevacaoZ(Vector3 pontoMira, float deltaTime)
    {
        if (miraElevacaoZ == null) return;

        // Calcula dire��o no espa�o do pai do osso de eleva��o (que j� girou no Y)
        Transform referencia = miraElevacaoZ.parent != null ? miraElevacaoZ.parent : miraGiroY360;
        if (referencia == null) return;

        Vector3 direcaoMundo = pontoMira - miraElevacaoZ.position;
        if (direcaoMundo.sqrMagnitude <= 0.0001f) return;

        // Componente vertical e horizontal (independente do eixo frente)
        float componenteVertical    = direcaoMundo.y;
        Vector3 direcaoHorizontal   = new Vector3(direcaoMundo.x, 0f, direcaoMundo.z);
        float componenteHorizontal  = direcaoHorizontal.magnitude;

        float anguloDesejado = Mathf.Atan2(componenteVertical, componenteHorizontal) * Mathf.Rad2Deg;
        if (inverterElevacaoZ) anguloDesejado *= -1f;
        anguloDesejado = Mathf.Clamp(anguloDesejado, anguloMinimoZ, anguloMaximoZ);

        Quaternion rotacaoDesejada = rotacaoOriginalElevacaoZ * Quaternion.AngleAxis(anguloDesejado, Vector3.forward);

        miraElevacaoZ.localRotation = Quaternion.RotateTowards(
            miraElevacaoZ.localRotation, rotacaoDesejada, velocidadeGiroZ * deltaTime);
    }

    // =====================================================================
    // MIRA ANTIA�REA � GIRO
    // =====================================================================

    private void GirarMiraAereaHorizontalY(Vector3 pontoMira, float deltaTime)
    {
        if (miraAereaGiroY360 == null) return;

        Vector3 direcaoMundo = pontoMira - miraAereaGiroY360.position;
        direcaoMundo.y = 0f;
        if (direcaoMundo.sqrMagnitude <= 0.0001f) return;

        Transform pai = miraAereaGiroY360.parent;
        Vector3 direcaoLocalPai = pai != null
            ? pai.InverseTransformDirection(direcaoMundo.normalized)
            : direcaoMundo.normalized;
        direcaoLocalPai.y = 0f;
        direcaoLocalPai.Normalize();

        Vector3 frenteOriginalLocal = rotacaoOriginalGiroYAerea * ObterEixoFrenteLocal(eixoFrenteDaMiraAerea);
        frenteOriginalLocal.y = 0f;
        if (frenteOriginalLocal.sqrMagnitude <= 0.0001f) frenteOriginalLocal = Vector3.right;
        frenteOriginalLocal.Normalize();

        Quaternion rotacaoDesejada = Quaternion.FromToRotation(frenteOriginalLocal, direcaoLocalPai) * rotacaoOriginalGiroYAerea;

        miraAereaGiroY360.localRotation = Quaternion.RotateTowards(
            miraAereaGiroY360.localRotation, rotacaoDesejada, velocidadeGiroYAerea * deltaTime);
    }

    private void GirarMiraAereaElevacaoZ(Vector3 pontoMira, float deltaTime)
    {
        if (miraAereaElevacaoZ == null) return;

        Vector3 direcaoMundo = pontoMira - miraAereaElevacaoZ.position;
        if (direcaoMundo.sqrMagnitude <= 0.0001f) return;

        float componenteVertical   = direcaoMundo.y;
        float componenteHorizontal = new Vector3(direcaoMundo.x, 0f, direcaoMundo.z).magnitude;

        float anguloDesejado = Mathf.Atan2(componenteVertical, componenteHorizontal) * Mathf.Rad2Deg;
        if (inverterElevacaoZAerea) anguloDesejado *= -1f;
        anguloDesejado = Mathf.Clamp(anguloDesejado, anguloMinimoZAerea, anguloMaximoZAerea);

        Quaternion rotacaoDesejada = rotacaoOriginalElevacaoZAerea * Quaternion.AngleAxis(anguloDesejado, Vector3.forward);

        miraAereaElevacaoZ.localRotation = Quaternion.RotateTowards(
            miraAereaElevacaoZ.localRotation, rotacaoDesejada, velocidadeGiroZAerea * deltaTime);
    }

    // =====================================================================
    // ATIRAR � TERRESTRE
    // =====================================================================

    private void TentarAtirar(Transform alvo, Vector3 pontoMira)
    {
        if (prefabBala == null || spawnBala == null) return;
        if (Time.time < proximoTiroEm) return;

        Vector3 frenteCanhao    = ObterFrenteCanhao(spawnBala, eixoFrenteDaMira);
        Vector3 direcaoParaAlvo = (pontoMira - spawnBala.position).normalized;

        if (direcaoParaAlvo.sqrMagnitude <= 0.0001f) return;
        if (Vector3.Angle(frenteCanhao, direcaoParaAlvo) > toleranciaMiraParaAtirar) return;

        Atirar(alvo, spawnBala, prefabBala, pontoMira, danoBala, velocidadeBala, tempoVidaBala, eixoFrenteDaMira);
        proximoTiroEm = Time.time + Mathf.Max(0.05f, intervaloEntreTiros);
    }

    // =====================================================================
    // ATIRAR � ANTIA�REA
    // =====================================================================

    private void TentarAtirarAereo(Transform alvo, Vector3 pontoMira)
    {
        if (prefabBalaAerea == null || spawnBalaAerea == null) return;
        if (Time.time < proximoTiroAereoEm) return;

        Vector3 frenteCanhao    = ObterFrenteCanhao(spawnBalaAerea, eixoFrenteDaMiraAerea);
        Vector3 direcaoParaAlvo = (pontoMira - spawnBalaAerea.position).normalized;

        if (direcaoParaAlvo.sqrMagnitude <= 0.0001f) return;
        if (Vector3.Angle(frenteCanhao, direcaoParaAlvo) > toleranciaMiraParaAtirarAerea) return;

        Atirar(alvo, spawnBalaAerea, prefabBalaAerea, pontoMira, danoBalaAerea, velocidadeBalaAerea, tempoVidaBalaAerea, eixoFrenteDaMiraAerea);
        proximoTiroAereoEm = Time.time + Mathf.Max(0.05f, intervaloEntreTirosAerea);
    }

    // =====================================================================
    // ATIRAR � GEN�RICO
    // =====================================================================

    private void Atirar(Transform alvo, Transform spawn, GameObject prefab, Vector3 pontoMira, int danoTiro, float velBala, float vidaBala, EixoFrenteMira eixo)
    {
        Vector3 origemTiro  = spawn.position;
        Vector3 direcaoTiro = ObterFrenteCanhao(spawn, eixo);

        if ((pontoMira - origemTiro).sqrMagnitude > 0.0001f)
            direcaoTiro = (pontoMira - origemTiro).normalized;

        Quaternion rotacaoBala = Quaternion.LookRotation(direcaoTiro, Vector3.up);
        GameObject balaCriada  = Instantiate(prefab, origemTiro, rotacaoBala);

        TocarAudioDisparo();

        ProjetilDistancia projetil = balaCriada.GetComponent<ProjetilDistancia>();
        if (projetil != null)
        {
            Transform donoDoDisparo = tankLeve != null ? tankLeve.transform : transform;
            projetil.Configurar(alvo, danoTiro, velBala, donoDoDisparo, pontoMira);
        }
        else
        {
            Rigidbody rb = balaCriada.GetComponent<Rigidbody>();
            if (rb != null)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = direcaoTiro * velBala;
#else
                rb.velocity = direcaoTiro * velBala;
#endif
            }
            else
            {
                StartCoroutine(MoverBalaSemRigidbody(balaCriada.transform, direcaoTiro, velBala));
            }
        }

        if (vidaBala > 0f)
            Destroy(balaCriada, vidaBala);
    }

    private void TocarAudioDisparo()
    {
        if (audioSourceDisparo == null || !audioSourceDisparo.isActiveAndEnabled)
            return;

        if (audioClipDisparo == null)
            return;

        // No Unity 6, o AudioSource pode usar Audio Resource com .clip vazio.
        // A referencia explicita do AudioClip garante o disparo pelo mesmo
        // AudioSource e preserva a atenuacao/distancia 3D configuradas nele.
        audioSourceDisparo.PlayOneShot(audioClipDisparo);
    }

    private void ConfigurarAudioDisparo()
    {
        if (audioSourceDisparo == null)
            return;

        audioSourceDisparo.playOnAwake = false;
        audioSourceDisparo.spatialBlend = 1f;
        audioSourceDisparo.rolloffMode = AudioRolloffMode.Linear;
        audioSourceDisparo.maxDistance = Mathf.Max(
            audioSourceDisparo.minDistance + 0.1f,
            distanciaMaximaAudioDisparo);
    }

    private IEnumerator MoverBalaSemRigidbody(Transform bala, Vector3 direcao, float vel)
    {
        while (bala != null)
        {
            bala.position += direcao * vel * Time.deltaTime;
            yield return null;
        }
    }

    // =====================================================================
    // CENTRALIZAR SEM ALVO
    // =====================================================================

    private void CentralizarMiraTerrestre(float deltaTime)
    {
        if (!centralizarMiraSemAlvo) return;

        if (miraGiroY360 != null)
            miraGiroY360.localRotation = Quaternion.RotateTowards(
                miraGiroY360.localRotation, rotacaoOriginalGiroY,
                velocidadeCentralizarSemAlvo * deltaTime);

        if (miraElevacaoZ != null)
            miraElevacaoZ.localRotation = Quaternion.RotateTowards(
                miraElevacaoZ.localRotation, rotacaoOriginalElevacaoZ,
                velocidadeCentralizarSemAlvo * deltaTime);
    }

    private void CentralizarMiraAerea(float deltaTime)
    {
        if (!centralizarMiraAereaSemAlvo) return;

        if (miraAereaGiroY360 != null)
            miraAereaGiroY360.localRotation = Quaternion.RotateTowards(
                miraAereaGiroY360.localRotation, rotacaoOriginalGiroYAerea,
                velocidadeCentralizarAereaSemAlvo * deltaTime);

        if (miraAereaElevacaoZ != null)
            miraAereaElevacaoZ.localRotation = Quaternion.RotateTowards(
                miraAereaElevacaoZ.localRotation, rotacaoOriginalElevacaoZAerea,
                velocidadeCentralizarAereaSemAlvo * deltaTime);
    }

    // =====================================================================
    // HELPERS
    // =====================================================================

    private Vector3 ObterPontoMira(Transform alvo, float alturaExtra)
    {
        if (alvo == null) return transform.position;

        Collider col = alvo.GetComponentInChildren<Collider>();
        return col != null
            ? col.bounds.center + Vector3.up * alturaExtra
            : alvo.position    + Vector3.up * alturaExtra;
    }

    private Vector3 ObterPontoMiraPrevisto(Transform alvo, float alturaExtra, float velocidadeProjetil, Transform pontoDisparo)
    {
        Vector3 pontoAtual = ObterPontoMira(alvo, alturaExtra);
        if (alvo == null) return pontoAtual;

        Rigidbody rbAlvo = alvo.GetComponentInParent<Rigidbody>();
        if (rbAlvo == null) rbAlvo = alvo.GetComponentInChildren<Rigidbody>();
        if (rbAlvo == null) return pontoAtual;

#if UNITY_6000_0_OR_NEWER
        Vector3 velocidadeAlvo = rbAlvo.linearVelocity;
#else
        Vector3 velocidadeAlvo = rbAlvo.velocity;
#endif
        float velocidadeSegura = Mathf.Max(0.1f, velocidadeProjetil);
        Vector3 origem = pontoDisparo != null ? pontoDisparo.position : transform.position;
        float tempoInterceptacao = Mathf.Clamp(Vector3.Distance(origem, pontoAtual) / velocidadeSegura, 0f, 1.5f);

        // Refina uma vez a estimativa, porque o ponto adiantado tambem aumenta a distancia do tiro.
        Vector3 pontoPrevisto = pontoAtual + velocidadeAlvo * tempoInterceptacao;
        tempoInterceptacao = Mathf.Clamp(Vector3.Distance(origem, pontoPrevisto) / velocidadeSegura, 0f, 1.5f);
        return pontoAtual + velocidadeAlvo * tempoInterceptacao;
    }

    private Vector3 ObterFrenteCanhao(Transform spawn, EixoFrenteMira eixo)
    {
        if (spawn == null) return transform.right;
        return spawn.TransformDirection(ObterEixoFrenteLocal(eixo)).normalized;
    }

    private Vector3 ObterEixoFrenteLocal(EixoFrenteMira eixo)
    {
        switch (eixo)
        {
            case EixoFrenteMira.XNegativo: return Vector3.left;
            case EixoFrenteMira.ZPositivo: return Vector3.forward;
            case EixoFrenteMira.ZNegativo: return Vector3.back;
            default:                       return Vector3.right;
        }
    }

    private float CalcularAnguloElevacao(Vector3 direcaoLocal, EixoFrenteMira eixo)
    {
        switch (eixo)
        {
            case EixoFrenteMira.XNegativo: return Mathf.Atan2(direcaoLocal.y, -direcaoLocal.x) * Mathf.Rad2Deg;
            case EixoFrenteMira.ZPositivo: return Mathf.Atan2(direcaoLocal.y,  direcaoLocal.z) * Mathf.Rad2Deg;
            case EixoFrenteMira.ZNegativo: return Mathf.Atan2(direcaoLocal.y, -direcaoLocal.z) * Mathf.Rad2Deg;
            default:                       return Mathf.Atan2(direcaoLocal.y,  direcaoLocal.x) * Mathf.Rad2Deg;
        }
    }

    // =====================================================================
    // VALIDA��O
    // =====================================================================

    private void OnValidate()
    {
        velocidadeGiroY                    = Mathf.Max(1f,    velocidadeGiroY);
        velocidadeGiroZ                    = Mathf.Max(1f,    velocidadeGiroZ);
        intervaloEntreTiros                = Mathf.Max(0.05f, intervaloEntreTiros);
        velocidadeBala                     = Mathf.Max(0.1f,  velocidadeBala);
        danoBala                           = Mathf.Max(0,     danoBala);
        tempoVidaBala                      = Mathf.Max(0f,    tempoVidaBala);
        toleranciaMiraParaAtirar           = Mathf.Clamp(toleranciaMiraParaAtirar, 0.1f, 45f);
        velocidadeCentralizarSemAlvo       = Mathf.Max(1f,    velocidadeCentralizarSemAlvo);

        velocidadeGiroYAerea               = Mathf.Max(1f,    velocidadeGiroYAerea);
        velocidadeGiroZAerea               = Mathf.Max(1f,    velocidadeGiroZAerea);
        intervaloEntreTirosAerea           = Mathf.Max(0.05f, intervaloEntreTirosAerea);
        velocidadeBalaAerea                = Mathf.Max(0.1f,  velocidadeBalaAerea);
        danoBalaAerea                      = Mathf.Max(0,     danoBalaAerea);
        tempoVidaBalaAerea                 = Mathf.Max(0f,    tempoVidaBalaAerea);
        toleranciaMiraParaAtirarAerea      = Mathf.Clamp(toleranciaMiraParaAtirarAerea, 0.1f, 45f);
        velocidadeCentralizarAereaSemAlvo  = Mathf.Max(1f,    velocidadeCentralizarAereaSemAlvo);

        if (anguloMinimoZ    > anguloMaximoZ)    (anguloMinimoZ,    anguloMaximoZ)    = (anguloMaximoZ,    anguloMinimoZ);
        if (anguloMinimoZAerea > anguloMaximoZAerea) (anguloMinimoZAerea, anguloMaximoZAerea) = (anguloMaximoZAerea, anguloMinimoZAerea);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        if (!desenharLinhaMiraNoEditor) return;

        // Mira terrestre � vermelho
        if (spawnBala != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(spawnBala.position,
                spawnBala.position + ObterFrenteCanhao(spawnBala, eixoFrenteDaMira) * 8f);

            if (alvoTerrestre != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(spawnBala.position, ObterPontoMira(alvoTerrestre, alturaExtraMiraAlvo));
            }
        }

        // Mira antia�rea � ciano
        if (spawnBalaAerea != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(spawnBalaAerea.position,
                spawnBalaAerea.position + ObterFrenteCanhao(spawnBalaAerea, eixoFrenteDaMiraAerea) * 8f);

            if (alvoAereo != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(spawnBalaAerea.position, ObterPontoMira(alvoAereo, alturaExtraMiraAlvoAerea));
            }
        }
    }
}
