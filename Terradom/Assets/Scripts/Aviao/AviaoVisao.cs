using UnityEngine;

/// <summary>
/// FASE 3 — PATRULHA E DETECÇÃO.
///
/// Detecção por CONE FRONTAL (não mais esfera 360°).
/// O inimigo precisa estar à frente do avião dentro de um ângulo e distância
/// configuráveis — assim que entrar nessa área, é registrado como alvo.
///
/// Modelo de voo realista de caça:
///   • Yaw limitado (curvas ABERTAS, raio proporcional à velocidade)
///   • Pitch suave e contínuo
///   • Banking proporcional à taxa de curva
///   • Patrulha por setores embaralhados cobrindo todo o terrain
///   • Sensor de terrain por raycast com tag "Terrain"
/// </summary>
[DisallowMultipleComponent]
public class AviaoVisao : MonoBehaviour
{
    // =====================================================================
    // ENUMS
    // =====================================================================

    public enum TipoAlvoAviao  { Nenhum, Terrestre, Aereo }
    public enum PrioridadeAlvo { AereosPrimeiro, TerrestresPrimeiro, MaisProximo }
    public enum EstadoVisao    { Patrulhando, EmAtaque }
    public enum ClasseAlvoCombate { Nenhum, Unidade, Base, Tanque, Aereo, OutroTerrestre }

    // =====================================================================
    // INSPECTOR — VOO
    // =====================================================================

    [Header("Velocidade")]
    [SerializeField] private float velocidadeCruzeiro = 120f;
    [SerializeField] private float aceleracao         = 8f;

    [Header("Curva (Yaw)")]
    [Tooltip("Taxa máxima de guinada em graus/s. Menor = curva mais aberta.")]
    [SerializeField] private float taxaYawMaxima  = 14f;
    [SerializeField] private float suavizacaoYaw  = 1.2f;

    [Header("Pitch")]
    [SerializeField] private float taxaPitchMaxima = 6f;
    [SerializeField] private float pitchMaxSubida  = 15f;
    [SerializeField] private float pitchMaxDescida = 12f;

    [Header("Banking")]
    [SerializeField] private float bankingMaximo    = 45f;
    [SerializeField] private float suavizacaoBanking = 3f;

    // =====================================================================
    // INSPECTOR — ALTITUDE
    // =====================================================================

    [Header("Altitude de Patrulha")]
    [SerializeField] private float alturaPatrulhaMin  = 60f;
    [SerializeField] private float alturaPatrulhaMax  = 130f;
    [SerializeField] private float toleranciaAltitude = 15f;

    // =====================================================================
    // INSPECTOR — SENSOR
    // =====================================================================

    [Header("Sensor de Terrain")]
    [SerializeField] private string tagTerrain        = "Terrain";
    [SerializeField] private float  comprimentoSensor = 900f;
    [SerializeField] private float  offsetSensor      = 5f;

    // =====================================================================
    // INSPECTOR — PATRULHA
    // =====================================================================

    [Header("Patrulha")]
    [SerializeField] private Terrain terrainReferencia;
    [SerializeField] private float   distanciaMinWaypoint     = 350f;
    [SerializeField] private float   distanciaMaxWaypoint     = 600f;
    [SerializeField] private float   distanciaChegadaWaypoint = 120f;
    [SerializeField] private int     numeroDeSetores          = 8;

    // =====================================================================
    // INSPECTOR — DETECÇÃO POR CONE
    // =====================================================================

    [Header("Detecção por Cone Frontal")]
    [Tooltip("Ponto de origem do cone (nariz do avião). Se vazio usa o transform raiz.")]
    [SerializeField] private Transform origemVisao;

    [Tooltip("Semiângulo do cone de detecção em graus.\n" +
             "Ex: 60° = inimigos à frente num cone de 120° total são detectados.\n" +
             "Terrestres e Aéreos usam o mesmo ângulo, mas distâncias diferentes.")]
    [SerializeField] private float anguloConeFrontal = 60f;

    [Header("Detecção Terrestre")]
    [SerializeField] private bool     usarVisaoTerrestre            = true;
    [SerializeField] private float    alcanceVisaoTerrestre         = 80f;
    [SerializeField] private string[] tagsInimigosTerrestres        = { "Azul", "Vermelho", "Verde" };
    [SerializeField] private bool     ignorarAvioesNaVisaoTerrestre = true;
    [SerializeField] private LayerMask layerAviao;

    [Header("Detecção Aérea")]
    [SerializeField] private bool      usarVisaoAerea     = true;
    [SerializeField] private float     alcanceVisaoAerea  = 200f;
    [SerializeField] private string[]  tagsInimigosAereos = { "Azul", "Vermelho", "Verde" };

    [Header("Filtro geral")]
    [SerializeField] private bool           detectarTriggers = false;
    [SerializeField] private float          intervaloBusca   = 0.12f;
    [SerializeField] private PrioridadeAlvo prioridadeAlvo   = PrioridadeAlvo.AereosPrimeiro;

    [Header("Manobra defensiva contra aviões")]
    [Tooltip("Pequena oscilação de guinada durante o combate aéreo para o avião não voar em linha reta previsível.")]
    [SerializeField] private float amplitudeEvasivaAerea = 5f;
    [SerializeField] private float frequenciaEvasivaAerea = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool  desenharGizmosNoEditor = true;

    // =====================================================================
    // ESTADO INTERNO — VOO
    // =====================================================================

    private float   velocidadeAtual     = 0f;
    private Vector3 direcaoHorizontal   = Vector3.forward;
    private float   yawRateAtual        = 0f;
    private float   pitchAtual          = 0f;
    private float   bankingAtual        = 0f;
    private float   alturaAlvoSuavizada = 0f;
    private bool    sensorVeTerrain     = true;

    // =====================================================================
    // ESTADO INTERNO — PATRULHA
    // =====================================================================

    private Vector3 wpAtual;
    private Vector3 wpProximo;
    private int     setorAtual   = 0;
    private int[]   ordemSetores;

    // =====================================================================
    // ESTADO INTERNO — DETECÇÃO
    // =====================================================================

    private Transform     alvoAtual;
    private Transform     alvoTerrestreAtual;
    private Transform     alvoAereoAtual;
    private Transform     alvoTanqueAtual;
    private Transform     alvoBaseAtual;
    private Transform     alvoUnidadeAtual;
    private ClasseAlvoCombate classeAlvoTerrestreMaisProximo = ClasseAlvoCombate.Nenhum;
    private TipoAlvoAviao tipoAlvoAtual = TipoAlvoAviao.Nenhum;
    private ClasseAlvoCombate classeAlvoAtual = ClasseAlvoCombate.Nenhum;
    private LayerMask layersTanques;
    private LayerMask layersBases;
    private LayerMask layersUnidades;
    private bool misselInimigoNaVisao;
    private float         proximaBusca;
    private EstadoVisao   estadoAtual = EstadoVisao.Patrulhando;

    // =====================================================================
    // PROPRIEDADES PÚBLICAS
    // =====================================================================

    public Transform      AlvoAtual          => alvoAtual;
    public Transform      AlvoTerrestreAtual => alvoTerrestreAtual;
    public Transform      AlvoAereoAtual     => alvoAereoAtual;
    public TipoAlvoAviao  TipoAlvoAtual      => tipoAlvoAtual;
    public ClasseAlvoCombate ClasseAlvoAtual => classeAlvoAtual;
    public bool           TemAlvo            => alvoAtual != null;
    public bool           TemAlvoTerrestre   => alvoTerrestreAtual != null;
    public bool           TemAlvoAereo       => alvoAereoAtual != null;
    public EstadoVisao    EstadoAtual        => estadoAtual;
    public bool           EmPatrulha         => estadoAtual == EstadoVisao.Patrulhando;
    public bool           EmAtaque           => estadoAtual == EstadoVisao.EmAtaque;
    public Vector3        PontoNavegacaoAtual => wpAtual;

    /// <summary>
    /// Ângulo em graus entre o nariz do avião (origemVisao.forward) e a direção ao alvo atual.
    /// Retorna 180f quando não há alvo.
    /// Usado pelo AviaoAtaque para só disparar quando o nariz estiver apontado para o inimigo.
    /// </summary>
    public float AnguloParaAlvo
    {
        get
        {
            if (alvoAtual == null) return 180f;
            Vector3 forward = origemVisao != null ? origemVisao.forward : transform.forward;
            Vector3 origem  = origemVisao != null ? origemVisao.position : transform.position;
            Vector3 dir     = (alvoAtual.position - origem).normalized;
            return Vector3.Angle(forward, dir);
        }
    }

    // =====================================================================
    // AWAKE
    // =====================================================================

    private void Awake()
    {
        if (origemVisao       == null) origemVisao       = transform;
        if (terrainReferencia == null) terrainReferencia = Terrain.activeTerrain;

        layersTanques  = CriarMascaraCamadas("Tank");
        layersBases    = CriarMascaraCamadas("BaseTank", "BaseAviao", "BaseSoldado");
        layersUnidades = CriarMascaraCamadas("Coletor", "Soldado", "Guerreiro");

        GerarOrdemDeSetores();
    }

    // =====================================================================
    // OnEnable — AviaoControler ativa na fase Patrulha
    // =====================================================================

    private void OnEnable()
    {
        direcaoHorizontal   = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        velocidadeAtual     = velocidadeCruzeiro * 0.85f;
        pitchAtual          = 0f;
        bankingAtual        = 0f;
        yawRateAtual        = 0f;
        alturaAlvoSuavizada = transform.position.y;
        proximaBusca        = 0f;
        estadoAtual         = EstadoVisao.Patrulhando;
        sensorVeTerrain     = true;
        alvoAtual           = null;
        alvoTerrestreAtual  = null;
        alvoAereoAtual      = null;
        alvoTanqueAtual     = null;
        alvoBaseAtual       = null;
        alvoUnidadeAtual    = null;
        classeAlvoTerrestreMaisProximo = ClasseAlvoCombate.Nenhum;
        tipoAlvoAtual       = TipoAlvoAviao.Nenhum;
        classeAlvoAtual     = ClasseAlvoCombate.Nenhum;
        misselInimigoNaVisao = false;

        wpAtual   = GerarWaypointNoSetor(setorAtual);
        AvançarSetor();
        wpProximo = GerarWaypointNoSetor(setorAtual);
    }

    // =====================================================================
    // UPDATE
    // =====================================================================

    private void Update()
    {
        if (Time.time >= proximaBusca)
        {
            proximaBusca = Time.time + Mathf.Max(0.02f, intervaloBusca);
            AtualizarDeteccao();
        }

        AtualizarSensor();
        AtualizarEstado();
        AplicarVoo();
    }

    // =====================================================================
    // SENSOR DE TERRAIN
    // =====================================================================

    private void AtualizarSensor()
    {
        Vector3 origem = transform.position + Vector3.up * offsetSensor;
        if (Physics.Raycast(origem, Vector3.down, out RaycastHit hit, comprimentoSensor))
            sensorVeTerrain = hit.collider.CompareTag(tagTerrain);
        else
            sensorVeTerrain = false;
    }

    // =====================================================================
    // MÁQUINA DE ESTADOS
    // =====================================================================

    private void AtualizarEstado()
    {
        switch (estadoAtual)
        {
            case EstadoVisao.Patrulhando:
                AtualizarPatrulha();
                if (TemAlvo) estadoAtual = EstadoVisao.EmAtaque;
                break;

            case EstadoVisao.EmAtaque:
                if (!TemAlvo)
                {
                    estadoAtual = EstadoVisao.Patrulhando;
                    wpAtual   = GerarWaypointNoSetor(setorAtual);
                    AvançarSetor();
                    wpProximo = GerarWaypointNoSetor(setorAtual);
                    break;
                }
                // Em ataque: voa direto para o alvo
                wpAtual = alvoAtual.position;
                break;
        }
    }

    // =====================================================================
    // PATRULHA
    // =====================================================================

    private void AtualizarPatrulha()
    {
        if (!sensorVeTerrain)
        {
            wpAtual   = GerarPontoCentro();
            wpProximo = GerarWaypointNoSetor(setorAtual);
            return;
        }

        float distXZ = DistanciaHorizontal(transform.position, wpAtual);
        if (distXZ <= distanciaChegadaWaypoint)
        {
            wpAtual = wpProximo;
            AvançarSetor();
            wpProximo = GerarWaypointNoSetor(setorAtual);
        }
    }

    // =====================================================================
    // VOO REALISTA
    // =====================================================================

    private void AplicarVoo()
    {
        float dt = Time.deltaTime;

        // 1. VELOCIDADE
        velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeCruzeiro, aceleracao * dt);

        // 2. YAW — curva aberta proporcional à velocidade
        float yawMaxEfetivo = taxaYawMaxima * (60f / Mathf.Max(velocidadeAtual, 1f));
        yawMaxEfetivo = Mathf.Clamp(yawMaxEfetivo, 3f, taxaYawMaxima);
        if (estadoAtual == EstadoVisao.EmAtaque)
            yawMaxEfetivo = Mathf.Max(yawMaxEfetivo, Mathf.Min(taxaYawMaxima, 12f));

        Vector3 paraWp      = new Vector3(wpAtual.x - transform.position.x, 0f,
                                          wpAtual.z - transform.position.z).normalized;
        float   yawAlvo     = AnguloSignado(direcaoHorizontal, paraWp);
        float desvioEvasivo = (classeAlvoAtual == ClasseAlvoCombate.Aereo || misselInimigoNaVisao)
            ? Mathf.Sin(Time.time * frequenciaEvasivaAerea) * amplitudeEvasivaAerea
            : 0f;
        float yawDesejado = Mathf.Clamp(yawAlvo + desvioEvasivo, -yawMaxEfetivo, yawMaxEfetivo);

        yawRateAtual = Mathf.Lerp(yawRateAtual, yawDesejado, suavizacaoYaw * dt);

        direcaoHorizontal = Quaternion.Euler(0f, yawRateAtual * dt, 0f) * direcaoHorizontal;
        direcaoHorizontal.Normalize();

        // 3. PITCH — altitude
        float altTerrainWp    = AlturaTerrainAbaixo(wpAtual);
        float altAlvo         = altTerrainWp
                              + Mathf.Lerp(alturaPatrulhaMin, alturaPatrulhaMax,
                                    Mathf.PerlinNoise(wpAtual.x * 0.002f, wpAtual.z * 0.002f));

        alturaAlvoSuavizada = Mathf.Lerp(alturaAlvoSuavizada, altAlvo, dt * 0.4f);

        float diferencaAlt = alturaAlvoSuavizada - transform.position.y;
        float pitchAlvo;
        if      (diferencaAlt >  toleranciaAltitude)
            pitchAlvo =  Mathf.Lerp(0f,  pitchMaxSubida,  Mathf.Clamp01( diferencaAlt / (alturaPatrulhaMax * 0.5f)));
        else if (diferencaAlt < -toleranciaAltitude)
            pitchAlvo =  Mathf.Lerp(0f, -pitchMaxDescida, Mathf.Clamp01(-diferencaAlt / (alturaPatrulhaMax * 0.5f)));
        else
            pitchAlvo = 0f;

        pitchAtual = Mathf.MoveTowards(pitchAtual, pitchAlvo, taxaPitchMaxima * dt);

        // 4. BANKING
        float bankingAlvo = -Mathf.Clamp(yawRateAtual / taxaYawMaxima, -1f, 1f) * bankingMaximo;
        bankingAtual = Mathf.Lerp(bankingAtual, bankingAlvo, suavizacaoBanking * dt);

        // 5. ROTAÇÃO FINAL
        float yawAngulo = Mathf.Atan2(direcaoHorizontal.x, direcaoHorizontal.z) * Mathf.Rad2Deg;
        Quaternion rotAlvo = Quaternion.Euler(-pitchAtual, yawAngulo, bankingAtual);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotAlvo, 6f * dt);

        // 6. POSIÇÃO — move na direção do nariz
        transform.position += transform.forward * velocidadeAtual * dt;
    }

    // =====================================================================
    // DETECÇÃO POR CONE FRONTAL
    // =====================================================================

    /// <summary>
    /// Substitui o OverlapSphere 360° por uma varredura de cone frontal.
    /// Um inimigo é detectado se:
    ///   1. Está dentro do alcance máximo (esfera de pré-filtro barato).
    ///   2. Está dentro do ângulo do cone frontal (Vector3.Angle).
    ///   3. Tem a tag correta (terrestre ou aéreo).
    /// </summary>
    private void AtualizarDeteccao()
    {
        misselInimigoNaVisao = false;
        alvoTerrestreAtual = null;
        alvoAereoAtual     = null;
        alvoTanqueAtual    = null;
        alvoBaseAtual      = null;
        alvoUnidadeAtual   = null;
        classeAlvoTerrestreMaisProximo = ClasseAlvoCombate.Nenhum;
        float menorDistanciaTerrestre = float.MaxValue;

        float alcanceMax = Mathf.Max(alcanceVisaoTerrestre, alcanceVisaoAerea);

        // Pré-filtro: todos os colliders dentro do alcance máximo
        Collider[] candidatos = Physics.OverlapSphere(
            ObterOrigemVisao(), alcanceMax, ~0,
            detectarTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);

        foreach (Collider col in candidatos)
        {
            if (col == null || EhDoProprioAviao(col.transform)) continue;

            Missel misselDetectado = col.GetComponentInParent<Missel>();
            if (misselDetectado != null && misselDetectado.EstaLancado
                && (misselDetectado.AlvoAtual == transform
                    || (misselDetectado.AlvoAtual != null && misselDetectado.AlvoAtual.IsChildOf(transform)))
                && misselDetectado.TagEquipeDona != ObterTagEquipePropria())
            {
                misselInimigoNaVisao = true;
            }

            Transform raiz      = ObterRaizDoAlvo(col.transform);
            float     distancia = Vector3.Distance(ObterOrigemVisao(), raiz.position);

            // ── Verifica se está dentro do CONE FRONTAL ───────────────────
            if (!NoCone(raiz.position)) continue;

            // ── Verifica se é inimigo terrestre ───────────────────────────
            bool camadaAerea = ObjetoOuPaisTemLayerNaMascara(col.transform, layerAviao);
            bool inimigoAereo = ObjetoOuPaisTemAlgumaTag(col.transform, tagsInimigosAereos);
            bool inimigoTerrestre = ObjetoOuPaisTemAlgumaTag(col.transform, tagsInimigosTerrestres);

            if (usarVisaoTerrestre
                && distancia <= alcanceVisaoTerrestre
                && !camadaAerea
                && inimigoTerrestre)
            {
                if (distancia < menorDistanciaTerrestre)
                {
                    menorDistanciaTerrestre = distancia;
                    alvoTerrestreAtual = raiz;
                    classeAlvoTerrestreMaisProximo = ClassificarAlvoTerrestre(col.transform);
                }

                if (ObjetoOuPaisTemLayerNaMascara(col.transform, layersTanques))
                    AtualizarAlvoMaisProximo(ref alvoTanqueAtual, raiz, distancia);
                else if (ObjetoOuPaisTemLayerNaMascara(col.transform, layersBases))
                    AtualizarAlvoMaisProximo(ref alvoBaseAtual, raiz, distancia);
                else if (ObjetoOuPaisTemLayerNaMascara(col.transform, layersUnidades))
                    AtualizarAlvoMaisProximo(ref alvoUnidadeAtual, raiz, distancia);
            }

            // ── Verifica se é inimigo aéreo ───────────────────────────────
            if (usarVisaoAerea
                && distancia <= alcanceVisaoAerea
                && camadaAerea
                && inimigoAereo)
            {
                AtualizarAlvoMaisProximo(ref alvoAereoAtual, raiz, distancia);
            }
        }

        EscolherAlvoAtual();
    }

    /// <summary>
    /// Retorna true se 'posicao' está dentro do cone frontal do avião.
    /// Cone definido por anguloConeFrontal (semiângulo) a partir de origemVisao.forward.
    /// </summary>
    private bool NoCone(Vector3 posicao)
    {
        Vector3 direcao = (posicao - ObterOrigemVisao()).normalized;
        float   angulo  = Vector3.Angle(origemVisao.forward, direcao);
        return angulo <= anguloConeFrontal;
    }

    // =====================================================================
    // SELEÇÃO DO ALVO
    // =====================================================================

    private void EscolherAlvoAtual()
    {
        Transform melhorAlvoTerrestre = alvoTanqueAtual != null ? alvoTanqueAtual
            : alvoBaseAtual != null ? alvoBaseAtual
            : alvoUnidadeAtual;
        ClasseAlvoCombate classeTerrestre = alvoTanqueAtual != null ? ClasseAlvoCombate.Tanque
            : alvoBaseAtual != null ? ClasseAlvoCombate.Base
            : alvoUnidadeAtual != null ? ClasseAlvoCombate.Unidade
            : ClasseAlvoCombate.Nenhum;

        Transform alvoTerrestreMaisProximo = ObterAlvoTerrestrePermitidoMaisProximo(
            out ClasseAlvoCombate classeTerrestreMaisProximo);

        Transform novoAlvo = null;
        ClasseAlvoCombate novaClasse = ClasseAlvoCombate.Nenhum;

        if (prioridadeAlvo == PrioridadeAlvo.MaisProximo
            && alvoAereoAtual != null && alvoTerrestreMaisProximo != null)
        {
            bool aereoMaisProximo = Vector3.Distance(ObterOrigemVisao(), alvoAereoAtual.position)
                <= Vector3.Distance(ObterOrigemVisao(), alvoTerrestreMaisProximo.position);
            novoAlvo = aereoMaisProximo ? alvoAereoAtual : alvoTerrestreMaisProximo;
            novaClasse = aereoMaisProximo ? ClasseAlvoCombate.Aereo : classeTerrestreMaisProximo;
        }
        else if (prioridadeAlvo == PrioridadeAlvo.MaisProximo && alvoTerrestreMaisProximo != null)
        {
            novoAlvo = alvoTerrestreMaisProximo;
            novaClasse = classeTerrestreMaisProximo;
        }
        else if (prioridadeAlvo == PrioridadeAlvo.TerrestresPrimeiro)
        {
            if (melhorAlvoTerrestre != null) { novoAlvo = melhorAlvoTerrestre; novaClasse = classeTerrestre; }
            else if (alvoAereoAtual != null) { novoAlvo = alvoAereoAtual; novaClasse = ClasseAlvoCombate.Aereo; }
        }
        else
        {
            if (alvoAereoAtual != null) { novoAlvo = alvoAereoAtual; novaClasse = ClasseAlvoCombate.Aereo; }
            else if (melhorAlvoTerrestre != null) { novoAlvo = melhorAlvoTerrestre; novaClasse = classeTerrestre; }
        }

        // Mantém o combate ao sair momentaneamente do cone. Um alvo de categoria
        // mais alta continua sendo perseguido; alvos prioritários novos podem
        // substituir imediatamente um alvo terrestre inferior.
        if (novoAlvo != null && alvoAtual != null && novoAlvo != alvoAtual
            && EhAlvoInimigo(alvoAtual)
            && PontuacaoPrioridade(classeAlvoAtual) < PontuacaoPrioridade(novaClasse))
            return;

        if (novoAlvo != null)
        {
            alvoAtual = novoAlvo;
            classeAlvoAtual = novaClasse;
            tipoAlvoAtual = novaClasse == ClasseAlvoCombate.Aereo
                ? TipoAlvoAviao.Aereo
                : TipoAlvoAviao.Terrestre;
            return;
        }

        if (alvoAtual != null && EhAlvoInimigo(alvoAtual))
            return;

        alvoAtual = null;
        tipoAlvoAtual = TipoAlvoAviao.Nenhum;
        classeAlvoAtual = ClasseAlvoCombate.Nenhum;
    }

    private int PontuacaoPrioridade(ClasseAlvoCombate classe)
    {
        if (prioridadeAlvo == PrioridadeAlvo.MaisProximo)
            return 0;

        if (prioridadeAlvo == PrioridadeAlvo.TerrestresPrimeiro)
        {
            switch (classe)
            {
                case ClasseAlvoCombate.Tanque: return 0;
                case ClasseAlvoCombate.Base: return 1;
                case ClasseAlvoCombate.Unidade: return 2;
                case ClasseAlvoCombate.OutroTerrestre: return 3;
                case ClasseAlvoCombate.Aereo: return 4;
                default: return int.MaxValue;
            }
        }

        switch (classe)
        {
            case ClasseAlvoCombate.Aereo: return 0;
            case ClasseAlvoCombate.Tanque: return 1;
            case ClasseAlvoCombate.Base: return 2;
            case ClasseAlvoCombate.Unidade: return 3;
            case ClasseAlvoCombate.OutroTerrestre: return 4;
            default: return int.MaxValue;
        }
    }

    private void AtualizarAlvoMaisProximo(ref Transform alvo, Transform candidato, float distancia)
    {
        if (candidato == null) return;
        if (alvo == null || distancia < Vector3.Distance(ObterOrigemVisao(), alvo.position))
            alvo = candidato;
    }

    private Transform ObterAlvoTerrestrePermitidoMaisProximo(out ClasseAlvoCombate classe)
    {
        Transform melhor = null;
        float menorDistancia = float.MaxValue;
        classe = ClasseAlvoCombate.Nenhum;

        ConsiderarAlvoTerrestre(alvoTanqueAtual, ClasseAlvoCombate.Tanque,
            ref melhor, ref classe, ref menorDistancia);
        ConsiderarAlvoTerrestre(alvoBaseAtual, ClasseAlvoCombate.Base,
            ref melhor, ref classe, ref menorDistancia);
        ConsiderarAlvoTerrestre(alvoUnidadeAtual, ClasseAlvoCombate.Unidade,
            ref melhor, ref classe, ref menorDistancia);

        return melhor;
    }

    private void ConsiderarAlvoTerrestre(Transform candidato, ClasseAlvoCombate classeCandidata,
                                        ref Transform melhor, ref ClasseAlvoCombate classeMelhor,
                                        ref float menorDistancia)
    {
        if (candidato == null) return;
        float distancia = Vector3.Distance(ObterOrigemVisao(), candidato.position);
        if (distancia >= menorDistancia) return;

        menorDistancia = distancia;
        melhor = candidato;
        classeMelhor = classeCandidata;
    }

    private ClasseAlvoCombate ClassificarAlvoTerrestre(Transform alvo)
    {
        if (ObjetoOuPaisTemLayerNaMascara(alvo, layersTanques)) return ClasseAlvoCombate.Tanque;
        if (ObjetoOuPaisTemLayerNaMascara(alvo, layersBases)) return ClasseAlvoCombate.Base;
        if (ObjetoOuPaisTemLayerNaMascara(alvo, layersUnidades)) return ClasseAlvoCombate.Unidade;
        return ClasseAlvoCombate.OutroTerrestre;
    }

    // =====================================================================
    // GERAÇÃO DE WAYPOINTS POR SETOR
    // =====================================================================

    private Vector3 GerarWaypointNoSetor(int indiceSetor)
    {
        if (terrainReferencia == null)
            return transform.position + direcaoHorizontal * distanciaMinWaypoint;

        TerrainData td   = terrainReferencia.terrainData;
        Vector3     tPos = terrainReferencia.transform.position;
        Vector3     centro = new Vector3(tPos.x + td.size.x * 0.5f, 0f, tPos.z + td.size.z * 0.5f);

        float angBase    = (360f / numeroDeSetores) * ordemSetores[indiceSetor];
        float angVariado = angBase + Random.Range(0f, 360f / numeroDeSetores);
        float rad        = angVariado * Mathf.Deg2Rad;

        float raioMax = Mathf.Min(td.size.x, td.size.z) * 0.44f;
        float raioMin = raioMax * 0.25f;
        float dist    = Random.Range(raioMin, raioMax);

        Vector3 ponto = centro + new Vector3(Mathf.Sin(rad) * dist, 0f, Mathf.Cos(rad) * dist);
        ponto.x = Mathf.Clamp(ponto.x, tPos.x + 80f, tPos.x + td.size.x - 80f);
        ponto.z = Mathf.Clamp(ponto.z, tPos.z + 80f, tPos.z + td.size.z - 80f);

        float h = terrainReferencia.SampleHeight(ponto) + tPos.y;
        ponto.y = h + Mathf.Lerp(alturaPatrulhaMin, alturaPatrulhaMax,
                      Mathf.PerlinNoise(ponto.x * 0.003f, ponto.z * 0.003f));
        return ponto;
    }

    private Vector3 GerarPontoCentro()
    {
        if (terrainReferencia == null)
            return transform.position - new Vector3(transform.forward.x, 0f, transform.forward.z).normalized
                   * distanciaMinWaypoint;

        TerrainData td   = terrainReferencia.terrainData;
        Vector3     tPos = terrainReferencia.transform.position;
        Vector3     centro = new Vector3(tPos.x + td.size.x * 0.5f, 0f, tPos.z + td.size.z * 0.5f);
        float h = terrainReferencia.SampleHeight(centro) + tPos.y;
        centro.y = h + Mathf.Lerp(alturaPatrulhaMin, alturaPatrulhaMax, 0.5f);
        return centro;
    }

    private void AvançarSetor()
    {
        setorAtual = (setorAtual + 1) % numeroDeSetores;
        if (setorAtual == 0) EmbaralharSetores();
    }

    private void GerarOrdemDeSetores()
    {
        ordemSetores = new int[numeroDeSetores];
        for (int i = 0; i < numeroDeSetores; i++) ordemSetores[i] = i;
        EmbaralharSetores();
    }

    private void EmbaralharSetores()
    {
        for (int i = ordemSetores.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (ordemSetores[i], ordemSetores[j]) = (ordemSetores[j], ordemSetores[i]);
        }
    }

    // =====================================================================
    // TERRAIN
    // =====================================================================

    private float AlturaTerrainAbaixo(Vector3 ponto)
    {
        if (terrainReferencia != null)
        {
            TerrainData td   = terrainReferencia.terrainData;
            Vector3     tPos = terrainReferencia.transform.position;
            if (ponto.x >= tPos.x && ponto.x <= tPos.x + td.size.x &&
                ponto.z >= tPos.z && ponto.z <= tPos.z + td.size.z)
                return terrainReferencia.SampleHeight(ponto) + tPos.y;
        }
        if (Physics.Raycast(new Vector3(ponto.x, ponto.y + 50f, ponto.z),
                            Vector3.down, out RaycastHit hit, 950f))
            if (hit.collider.CompareTag(tagTerrain)) return hit.point.y;
        return 0f;
    }

    // =====================================================================
    // AUXILIARES
    // =====================================================================

    private float AnguloSignado(Vector3 de, Vector3 para)
    {
        float   angulo = Vector3.Angle(de, para);
        Vector3 cruz   = Vector3.Cross(de, para);
        return cruz.y < 0f ? -angulo : angulo;
    }

    private float DistanciaHorizontal(Vector3 a, Vector3 b)
        => new Vector2(b.x - a.x, b.z - a.z).magnitude;

    private Vector3 ObterOrigemVisao()
        => origemVisao != null
            ? origemVisao.position
            : transform.position;

    private bool EhDoProprioAviao(Transform alvo)
    {
        if (alvo == null) return true;
        return alvo == transform || alvo.IsChildOf(transform);
    }

    private Transform ObterRaizDoAlvo(Transform alvo)
    {
        if (alvo == null) return null;
        Transform raizInimiga = null;
        Transform atual = alvo;
        while (atual != null && atual != transform)
        {
            if (ObjetoTemTagInimiga(atual))
                raizInimiga = atual;
            atual = atual.parent;
        }
        return raizInimiga != null ? raizInimiga : alvo;
    }

    private bool ObjetoTemTagInimiga(Transform alvo)
    {
        if (alvo == null || EhDaPropriaEquipe(alvo)) return false;
        return ObjetoTemAlgumaTag(alvo, tagsInimigosAereos)
            || ObjetoTemAlgumaTag(alvo, tagsInimigosTerrestres);
    }

    private bool ObjetoTemAlgumaTag(Transform alvo, string[] tags)
    {
        if (alvo == null || tags == null) return false;
        for (int i = 0; i < tags.Length; i++)
            if (!string.IsNullOrWhiteSpace(tags[i]) && alvo.CompareTag(tags[i]))
                return true;
        return false;
    }

    private bool EhAlvoInimigo(Transform alvo)
        => alvo != null && ObjetoTemTagInimiga(alvo);

    private string ObterTagEquipePropria()
        => ObterTagEquipe(transform);

    private bool EhDaPropriaEquipe(Transform alvo)
    {
        if (alvo == null)
            return false;

        string equipePropria = ObterTagEquipePropria();
        string equipeAlvo = ObterTagEquipe(alvo);
        return !string.IsNullOrEmpty(equipePropria)
            && !string.IsNullOrEmpty(equipeAlvo)
            && equipePropria == equipeAlvo;
    }

    private static string ObterTagEquipe(Transform origem)
    {
        Transform atual = origem;
        while (atual != null)
        {
            if (atual.CompareTag("Azul") || atual.CompareTag("Vermelho") || atual.CompareTag("Verde"))
                return atual.tag;
            atual = atual.parent;
        }
        return string.Empty;
    }

    private static LayerMask CriarMascaraCamadas(params string[] nomes)
    {
        int mascara = 0;
        for (int layer = 0; layer < 32; layer++)
        {
            string nomeLayer = LayerMask.LayerToName(layer);
            if (string.IsNullOrEmpty(nomeLayer)) continue;
            for (int i = 0; i < nomes.Length; i++)
                if (nomeLayer == nomes[i])
                {
                    mascara |= 1 << layer;
                    break;
                }
        }
        return mascara;
    }

    private bool ObjetoOuPaisTemAlgumaTag(Transform alvo, string[] tags)
    {
        if (alvo == null || tags == null || tags.Length == 0 || EhDaPropriaEquipe(alvo)) return false;
        Transform atual = alvo;
        while (atual != null)
        {
            foreach (string tag in tags)
                if (!string.IsNullOrWhiteSpace(tag) && atual.gameObject.CompareTag(tag)) return true;
            atual = atual.parent;
        }
        return false;
    }

    private bool ObjetoOuPaisTemLayerNaMascara(Transform alvo, LayerMask mascara)
    {
        if (alvo == null) return false;
        Transform atual = alvo;
        while (atual != null)
        {
            if ((mascara.value & (1 << atual.gameObject.layer)) != 0) return true;
            atual = atual.parent;
        }
        return false;
    }

    // =====================================================================
    // VALIDAÇÃO
    // =====================================================================

    private void OnValidate()
    {
        velocidadeCruzeiro       = Mathf.Max(20f,   velocidadeCruzeiro);
        taxaYawMaxima            = Mathf.Clamp(taxaYawMaxima, 3f, 30f);
        taxaPitchMaxima          = Mathf.Clamp(taxaPitchMaxima, 1f, 20f);
        pitchMaxSubida           = Mathf.Clamp(pitchMaxSubida, 5f, 45f);
        pitchMaxDescida          = Mathf.Clamp(pitchMaxDescida, 5f, 45f);
        bankingMaximo            = Mathf.Clamp(bankingMaximo, 10f, 80f);
        alturaPatrulhaMin        = Mathf.Max(20f,   alturaPatrulhaMin);
        alturaPatrulhaMax        = Mathf.Max(alturaPatrulhaMin + 20f, alturaPatrulhaMax);
        toleranciaAltitude       = Mathf.Max(5f,    toleranciaAltitude);
        comprimentoSensor        = Mathf.Max(100f,  comprimentoSensor);
        distanciaMinWaypoint     = Mathf.Max(100f,  distanciaMinWaypoint);
        distanciaMaxWaypoint     = Mathf.Max(distanciaMinWaypoint + 100f, distanciaMaxWaypoint);
        distanciaChegadaWaypoint = Mathf.Max(30f,   distanciaChegadaWaypoint);
        numeroDeSetores          = Mathf.Max(2,      numeroDeSetores);
        anguloConeFrontal        = Mathf.Clamp(anguloConeFrontal, 5f, 90f);
        alcanceVisaoTerrestre    = Mathf.Max(10f,   alcanceVisaoTerrestre);
        alcanceVisaoAerea        = Mathf.Max(10f,   alcanceVisaoAerea);
        intervaloBusca           = Mathf.Max(0.02f, intervaloBusca);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        if (!desenharGizmosNoEditor) return;

        Vector3 origem = ObterOrigemVisao();

        // Sensor de terrain
        Gizmos.color = sensorVeTerrain ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * offsetSensor,
                        transform.position + Vector3.up * offsetSensor + Vector3.down * comprimentoSensor);

        // ── CONE DE DETECÇÃO ──────────────────────────────────────────────
        // Cone terrestre (verde) e aéreo (ciano) — mesmo ângulo, distâncias diferentes
        DesenharConeDeteccao(origem, origemVisao != null ? origemVisao.forward : transform.forward,
                             anguloConeFrontal, alcanceVisaoTerrestre, new Color(0f, 1f, 0f, 0.3f));
        DesenharConeDeteccao(origem, origemVisao != null ? origemVisao.forward : transform.forward,
                             anguloConeFrontal, alcanceVisaoAerea,     new Color(0f, 1f, 1f, 0.15f));

        if (Application.isPlaying)
        {
            // Waypoints de patrulha
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(wpAtual, 5f);
            Gizmos.DrawLine(transform.position, wpAtual);

            Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.5f);
            Gizmos.DrawSphere(wpProximo, 3f);
            Gizmos.DrawLine(wpAtual, wpProximo);

            // Setores do terrain
            if (terrainReferencia != null)
            {
                TerrainData td     = terrainReferencia.terrainData;
                Vector3     tPos   = terrainReferencia.transform.position;
                Vector3     centro = new Vector3(tPos.x + td.size.x * 0.5f,
                                                 transform.position.y,
                                                 tPos.z + td.size.z * 0.5f);
                Gizmos.color = new Color(1f, 1f, 0f, 0.12f);
                for (int i = 0; i < numeroDeSetores; i++)
                {
                    float ang = (360f / numeroDeSetores) * i * Mathf.Deg2Rad;
                    Gizmos.DrawRay(centro,
                        new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang))
                        * Mathf.Min(td.size.x, td.size.z) * 0.44f);
                }
            }
        }

        // Linha até o alvo atual
        if (alvoAtual != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origem, alvoAtual.position);
            Gizmos.DrawSphere(alvoAtual.position, 1f);
        }
    }

    private void DesenharConeDeteccao(Vector3 origemPos, Vector3 forward,
                                      float semiAngulo, float comprimento, Color cor)
    {
        Gizmos.color = cor;
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        Vector3 up    = Vector3.up;

        int   passos   = 20;
        float raio     = comprimento * Mathf.Tan(semiAngulo * Mathf.Deg2Rad);
        Vector3 centro = origemPos + forward * comprimento;

        Vector3 anterior = Vector3.zero;
        for (int i = 0; i <= passos; i++)
        {
            float   ang   = (360f / passos) * i * Mathf.Deg2Rad;
            Vector3 borda = centro
                          + right * (Mathf.Cos(ang) * raio)
                          + up    * (Mathf.Sin(ang) * raio);
            if (i == 0) { anterior = borda; continue; }
            Gizmos.DrawLine(anterior, borda);
            anterior = borda;
            if (i % 5 == 0) Gizmos.DrawLine(origemPos, borda);
        }
        Gizmos.DrawLine(origemPos, centro);
    }
}
