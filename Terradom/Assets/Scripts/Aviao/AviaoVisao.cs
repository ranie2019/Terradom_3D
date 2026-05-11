using UnityEngine;

/// <summary>
/// Sistema de visão e patrulha do avião.
/// Componente PASSIVO — não se auto-inicializa.
/// O AviaoControler é responsável por habilitar este componente
/// apenas quando o avião atingir altitude segura.
/// </summary>
[DisallowMultipleComponent]
public class AviaoVisao : MonoBehaviour
{
    // =====================================================================
    // ENUMS
    // =====================================================================

    public enum TipoAlvoAviao  { Nenhum, Terrestre, Aereo }
    public enum PrioridadeAlvo { AereosPrimeiro, TerrestresPrimeiro, MaisProximo }
    public enum EstadoAviao    { Patrulhando, Perseguindo, AtacandoMissel, AtacandoMetralhadora }

    // =====================================================================
    // INSPECTOR — VISÃO
    // =====================================================================

    [Header("Origem da visão")]
    [SerializeField] private Transform origemVisao;
    [SerializeField] private float alturaOrigemVisao = 0.5f;

    [Header("Visão terrestre — por Tag")]
    [SerializeField] private bool     usarVisaoTerrestre          = true;
    [SerializeField] private float    raioVisaoTerrestre          = 30f;
    [SerializeField] private string[] tagsInimigosTerrestres      = { "Vermelho" };
    [SerializeField] private bool     ignorarAvioesNaVisaoTerrestre = true;

    [Header("Visão aérea — Layer Aviao + Tag")]
    [SerializeField] private bool      usarVisaoAerea      = true;
    [SerializeField] private float     raioVisaoAerea      = 80f;
    [SerializeField] private LayerMask layerAviao;
    [SerializeField] private string[]  tagsInimigosAereos  = { "Vermelho" };

    [Header("Filtro geral")]
    [SerializeField] private bool          detectarTriggers = false;
    [SerializeField] private float         intervaloBusca   = 0.15f;
    [SerializeField] private PrioridadeAlvo prioridadeAlvo  = PrioridadeAlvo.AereosPrimeiro;

    // =====================================================================
    // INSPECTOR — PATRULHA
    // =====================================================================

    [Header("Patrulha Aérea")]
    [SerializeField] private bool    usarPatrulha           = true;
    [SerializeField] private Terrain terrainReferencia;
    [SerializeField] private float   distanciaMinPatrulha   = 180f;
    [SerializeField] private float   distanciaMaxPatrulha   = 350f;
    [SerializeField] private float   alturaPatrulhaMin      = 35f;
    [SerializeField] private float   alturaPatrulhaMax      = 90f;
    [SerializeField] private float   distanciaChegadaPatrulha = 50f;
    [Range(0f, 1f)]
    [SerializeField] private float   antecipacaoCurva         = 0.75f;
    [SerializeField] private float   velocidadeAjusteAltitude = 1.2f;

    [Header("Sensor de Terrain")]
    [SerializeField] private float comprimentoSensor = 800f;
    [SerializeField] private float offsetSensor      = 5f;

    // =====================================================================
    // INSPECTOR — ATAQUE
    // =====================================================================

    [Header("Ataque")]
    [SerializeField] private float alcanceMissel                   = 60f;
    [SerializeField] private float alcanceMetralhadora             = 22f;
    [SerializeField] private int   misseisAntesDeMetralhadora      = 3;
    [SerializeField] private float cooldownMissel                  = 2.5f;
    [SerializeField] private float tempoMetralhadoraAposPerderAlvo = 3f;

    [Header("Debug")]
    [SerializeField] private bool desenharVisaoNoEditor = true;

    // =====================================================================
    // ESTADO INTERNO
    // =====================================================================

    private Transform    alvoAtual;
    private Transform    alvoTerrestreAtual;
    private Transform    alvoAereoAtual;
    private TipoAlvoAviao tipoAlvoAtual = TipoAlvoAviao.Nenhum;
    private float         proximaBusca;

    private EstadoAviao estadoAtual = EstadoAviao.Patrulhando;

    private Vector3 wpAtual;
    private Vector3 wpProximo;
    private Vector3 pontoNavegacaoAtual;

    private float alturaNavegacaoAtual;
    private bool  sobreTerrainNoFrameAnterior = true;

    private int   misseisLancados          = 0;
    private float proximoLancamento        = 0f;
    private float tempoSemAlvoMetralhadora = 0f;
    private bool  _deveLancarMissel        = false;
    private bool  _deveAtirarMetralhadora  = false;

    // =====================================================================
    // PROPRIEDADES PÚBLICAS
    // =====================================================================

    public Transform      AlvoAtual          => alvoAtual;
    public Transform      AlvoTerrestreAtual => alvoTerrestreAtual;
    public Transform      AlvoAereoAtual     => alvoAereoAtual;
    public TipoAlvoAviao  TipoAlvoAtual      => tipoAlvoAtual;
    public bool           TemAlvo            => alvoAtual != null;
    public bool           TemAlvoTerrestre   => alvoTerrestreAtual != null;
    public bool           TemAlvoAereo       => alvoAereoAtual != null;
    public Vector3        PontoPatrulhaAtual => pontoNavegacaoAtual;
    public Vector3        PontoNavegacaoAtual => pontoNavegacaoAtual;
    public EstadoAviao    EstadoAtual        => estadoAtual;
    public bool           EmPatrulha         => estadoAtual == EstadoAviao.Patrulhando;
    public bool           EmPerseguicao      => estadoAtual == EstadoAviao.Perseguindo;
    public bool           EmAtaque           => estadoAtual == EstadoAviao.AtacandoMissel
                                             || estadoAtual == EstadoAviao.AtacandoMetralhadora;
    public bool           DeveLancarMissel        => _deveLancarMissel;
    public bool           DeveAtirarMetralhadora  => _deveAtirarMetralhadora;

    // =====================================================================
    // API PÚBLICA
    // =====================================================================

    public void RegistrarMisselLancado()
    {
        misseisLancados++;
        proximoLancamento = Time.time + cooldownMissel;
        _deveLancarMissel = false;
    }

    public void RegistrarTiroMetralhadora() { }

    // =====================================================================
    // AWAKE — apenas configuração, SEM lógica de waypoints nem de fase
    // =====================================================================

    private void Awake()
    {
        if (origemVisao == null)
            origemVisao = transform;

        if (terrainReferencia == null)
            terrainReferencia = Terrain.activeTerrain;

        // Inicializa waypoints com posição atual (pode ser chão — será
        // corrigido em OnEnable quando o avião já estiver no ar).
        InicializarWaypoints();
    }

    // =====================================================================
    // OnEnable — chamado pelo Unity quando AviaoControler faz enabled = true
    // Neste ponto o avião já está no ar em altitude segura.
    // =====================================================================

    private void OnEnable()
    {
        // Reinicia timer para buscar alvo imediatamente
        proximaBusca = 0f;

        // Regera waypoints a partir da posição atual no ar
        InicializarWaypoints();

        // Zera flags de ataque para não disparar no primeiro frame
        _deveLancarMissel       = false;
        _deveAtirarMetralhadora = false;
        misseisLancados         = 0;
        proximoLancamento       = Time.time + 0.5f; // cooldown inicial

        estadoAtual = EstadoAviao.Patrulhando;

        Debug.Log($"[AviaoVisao] {gameObject.name} — ATIVADO " +
                  $"(altitude: {transform.position.y:F1} m).", this);
    }

    private void InicializarWaypoints()
    {
        wpAtual              = GerarPontoPatrulha(transform.position);
        wpProximo            = GerarPontoPatrulha(wpAtual);
        pontoNavegacaoAtual  = wpAtual;
        alturaNavegacaoAtual = transform.position.y;
    }

    // =====================================================================
    // UPDATE
    // =====================================================================

    private void Update()
    {
        if (Time.time >= proximaBusca)
        {
            proximaBusca = Time.time + Mathf.Max(0.02f, intervaloBusca);
            AtualizarVisao();
        }

        _deveLancarMissel       = false;
        _deveAtirarMetralhadora = false;

        AtualizarMaquinaEstado();
    }

    // =====================================================================
    // MÁQUINA DE ESTADOS
    // =====================================================================

    private void AtualizarMaquinaEstado()
    {
        switch (estadoAtual)
        {
            case EstadoAviao.Patrulhando:
                AtualizarPatrulha();
                if (TemAlvo)
                {
                    misseisLancados   = 0;
                    proximoLancamento = 0f;
                    estadoAtual       = EstadoAviao.Perseguindo;
                }
                break;

            case EstadoAviao.Perseguindo:         AtualizarPerseguicao();       break;
            case EstadoAviao.AtacandoMissel:      AtualizarAtaqueMissel();      break;
            case EstadoAviao.AtacandoMetralhadora: AtualizarAtaqueMetralhadora(); break;
        }
    }

    // =====================================================================
    // SENSOR DE TERRAIN
    // =====================================================================

    private bool SensorDetectaTerrain()
    {
        if (terrainReferencia == null) return true;

        Vector3 origem = transform.position + Vector3.up * offsetSensor;

        if (Physics.Raycast(origem, Vector3.down, out RaycastHit hit, comprimentoSensor))
        {
            Terrain t = hit.collider.GetComponent<Terrain>();
            return t != null && t == terrainReferencia;
        }

        return false;
    }

    // =====================================================================
    // PATRULHA
    // =====================================================================

    private void AtualizarPatrulha()
    {
        if (!usarPatrulha) return;

        bool sobreTerrainAgora = SensorDetectaTerrain();

        if (!sobreTerrainAgora)
        {
            if (sobreTerrainNoFrameAnterior)
            {
                wpAtual   = GerarPontoRetorno();
                wpProximo = GerarPontoPatrulha(wpAtual);
            }

            float altRetorno = AlturaTerrainAbaixo(wpAtual)
                             + Mathf.Lerp(alturaPatrulhaMin, alturaPatrulhaMax, 0.5f);

            alturaNavegacaoAtual = Mathf.Lerp(
                alturaNavegacaoAtual, altRetorno,
                Time.deltaTime * velocidadeAjusteAltitude);

            pontoNavegacaoAtual         = new Vector3(wpAtual.x, alturaNavegacaoAtual, wpAtual.z);
            sobreTerrainNoFrameAnterior = false;
            return;
        }

        sobreTerrainNoFrameAnterior = true;

        float distParaAtual = Vector3.Distance(transform.position, wpAtual);

        if (distParaAtual <= distanciaChegadaPatrulha)
        {
            wpAtual   = wpProximo;
            wpProximo = GerarPontoPatrulha(wpAtual);
        }

        float raioBlend  = distanciaChegadaPatrulha * (1f + antecipacaoCurva * 4f);
        float blendFator = Mathf.Clamp01(1f - distParaAtual / raioBlend) * antecipacaoCurva;

        Vector3 pontoXZ = Vector3.Lerp(wpAtual, wpProximo, blendFator);

        float alturaAlvo = AlturaTerrainAbaixo(pontoXZ)
                         + Mathf.Lerp(
                               alturaPatrulhaMin, alturaPatrulhaMax,
                               Mathf.PerlinNoise(pontoXZ.x * 0.003f, pontoXZ.z * 0.003f));

        alturaNavegacaoAtual = Mathf.Lerp(
            alturaNavegacaoAtual, alturaAlvo,
            Time.deltaTime * velocidadeAjusteAltitude);

        pontoNavegacaoAtual = new Vector3(pontoXZ.x, alturaNavegacaoAtual, pontoXZ.z);
    }

    // =====================================================================
    // GERAÇÃO DE WAYPOINTS
    // =====================================================================

    private Vector3 GerarPontoRetorno()
    {
        if (terrainReferencia == null)
            return transform.position - transform.forward * distanciaMinPatrulha;

        TerrainData td   = terrainReferencia.terrainData;
        Vector3     tPos = terrainReferencia.transform.position;

        Vector3 centro = new Vector3(tPos.x + td.size.x * 0.5f, 0f, tPos.z + td.size.z * 0.5f);
        Vector3 dir    = centro - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) dir = -transform.forward;
        else dir.Normalize();

        float distCentro = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z), centro);

        float dist = Mathf.Clamp(distCentro * 0.6f, distanciaMinPatrulha, distanciaMaxPatrulha);

        Vector3 lateral = Vector3.Cross(dir, Vector3.up);
        Vector3 ponto   = transform.position + dir * dist + lateral * dist * Random.Range(-0.15f, 0.15f);

        ponto.x = Mathf.Clamp(ponto.x, tPos.x + 50f, tPos.x + td.size.x - 50f);
        ponto.z = Mathf.Clamp(ponto.z, tPos.z + 50f, tPos.z + td.size.z - 50f);
        ponto.y = terrainReferencia.SampleHeight(ponto) + tPos.y
                + Random.Range(alturaPatrulhaMin, alturaPatrulhaMax);

        return ponto;
    }

    private Vector3 GerarPontoPatrulha(Vector3 origem)
    {
        if (terrainReferencia == null)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float   d   = Random.Range(distanciaMinPatrulha, distanciaMaxPatrulha);
            return origem + new Vector3(dir.x * d, 0f, dir.y * d);
        }

        TerrainData td   = terrainReferencia.terrainData;
        Vector3     tPos = terrainReferencia.transform.position;

        Vector2 randDir  = Random.insideUnitCircle.normalized;
        float   randDist = Random.Range(distanciaMinPatrulha, distanciaMaxPatrulha);
        Vector3 ponto    = origem + new Vector3(randDir.x * randDist, 0f, randDir.y * randDist);

        ponto.x = Mathf.Clamp(ponto.x, tPos.x + 50f, tPos.x + td.size.x - 50f);
        ponto.z = Mathf.Clamp(ponto.z, tPos.z + 50f, tPos.z + td.size.z - 50f);
        ponto.y = terrainReferencia.SampleHeight(ponto) + tPos.y
                + Random.Range(alturaPatrulhaMin, alturaPatrulhaMax);

        return ponto;
    }

    private float AlturaTerrainAbaixo(Vector3 ponto)
    {
        if (terrainReferencia == null) return 0f;
        return terrainReferencia.SampleHeight(ponto) + terrainReferencia.transform.position.y;
    }

    // =====================================================================
    // PERSEGUIÇÃO
    // =====================================================================

    private void AtualizarPerseguicao()
    {
        if (!TemAlvo) { estadoAtual = EstadoAviao.Patrulhando; return; }

        pontoNavegacaoAtual = alvoAtual.position;
        float dist = Vector3.Distance(transform.position, alvoAtual.position);

        if (dist <= alcanceMetralhadora)
        {
            estadoAtual              = EstadoAviao.AtacandoMetralhadora;
            tempoSemAlvoMetralhadora = 0f;
            return;
        }

        if (dist <= alcanceMissel)
        {
            estadoAtual = EstadoAviao.AtacandoMissel;
            return;
        }
    }

    // =====================================================================
    // ATAQUE — MÍSSEIS
    // =====================================================================

    private void AtualizarAtaqueMissel()
    {
        if (!TemAlvo) { estadoAtual = EstadoAviao.Patrulhando; return; }

        pontoNavegacaoAtual = alvoAtual.position;
        float dist = Vector3.Distance(transform.position, alvoAtual.position);

        if (dist > alcanceMissel * 1.35f)       { estadoAtual = EstadoAviao.Perseguindo; return; }

        if (dist <= alcanceMetralhadora)
        {
            estadoAtual              = EstadoAviao.AtacandoMetralhadora;
            tempoSemAlvoMetralhadora = 0f;
            return;
        }

        if (misseisLancados >= misseisAntesDeMetralhadora)
        {
            estadoAtual              = EstadoAviao.AtacandoMetralhadora;
            tempoSemAlvoMetralhadora = 0f;
            return;
        }

        if (Time.time >= proximoLancamento) _deveLancarMissel = true;
    }

    // =====================================================================
    // ATAQUE — METRALHADORA
    // =====================================================================

    private void AtualizarAtaqueMetralhadora()
    {
        if (!TemAlvo)
        {
            tempoSemAlvoMetralhadora += Time.deltaTime;
            if (tempoSemAlvoMetralhadora >= tempoMetralhadoraAposPerderAlvo)
            {
                estadoAtual              = EstadoAviao.Patrulhando;
                tempoSemAlvoMetralhadora = 0f;
                InicializarWaypoints();
            }
            return;
        }

        tempoSemAlvoMetralhadora = 0f;
        pontoNavegacaoAtual      = alvoAtual.position;
        _deveAtirarMetralhadora  = true;
    }

    // =====================================================================
    // VISÃO
    // =====================================================================

    private void AtualizarVisao()
    {
        alvoTerrestreAtual = null;
        alvoAereoAtual     = null;

        if (usarVisaoTerrestre) alvoTerrestreAtual = BuscarAlvoTerrestreMaisProximo();
        if (usarVisaoAerea)     alvoAereoAtual     = BuscarAlvoAereoMaisProximo();

        EscolherAlvoAtual();
    }

    private Transform BuscarAlvoTerrestreMaisProximo()
    {
        Vector3    origem    = ObterOrigemVisao();
        Collider[] colliders = Physics.OverlapSphere(
            origem, raioVisaoTerrestre, ~0,
            detectarTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);

        Transform melhorAlvo     = null;
        float     menorDistancia = float.MaxValue;

        foreach (Collider col in colliders)
        {
            if (col == null) continue;
            if (EhDoProprioAviao(col.transform)) continue;
            if (ignorarAvioesNaVisaoTerrestre
                && ObjetoOuPaisTemLayerNaMascara(col.transform, layerAviao)) continue;
            if (!ObjetoOuPaisTemAlgumaTag(col.transform, tagsInimigosTerrestres)) continue;

            Transform alvo      = ObterRaizDoAlvo(col.transform);
            float     distancia = Vector3.Distance(origem, alvo.position);
            if (distancia < menorDistancia) { menorDistancia = distancia; melhorAlvo = alvo; }
        }

        return melhorAlvo;
    }

    private Transform BuscarAlvoAereoMaisProximo()
    {
        Vector3    origem    = ObterOrigemVisao();
        Collider[] colliders = Physics.OverlapSphere(
            origem, raioVisaoAerea, layerAviao,
            detectarTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);

        Transform melhorAlvo     = null;
        float     menorDistancia = float.MaxValue;

        foreach (Collider col in colliders)
        {
            if (col == null) continue;
            if (EhDoProprioAviao(col.transform)) continue;
            if (!ObjetoOuPaisTemAlgumaTag(col.transform, tagsInimigosAereos)) continue;

            Transform alvo      = ObterRaizDoAlvo(col.transform);
            float     distancia = Vector3.Distance(origem, alvo.position);
            if (distancia < menorDistancia) { menorDistancia = distancia; melhorAlvo = alvo; }
        }

        return melhorAlvo;
    }

    private void EscolherAlvoAtual()
    {
        alvoAtual     = null;
        tipoAlvoAtual = TipoAlvoAviao.Nenhum;

        bool temT = alvoTerrestreAtual != null;
        bool temA = alvoAereoAtual     != null;

        if (!temT && !temA) return;

        switch (prioridadeAlvo)
        {
            case PrioridadeAlvo.AereosPrimeiro:
                if (temA) { alvoAtual = alvoAereoAtual;     tipoAlvoAtual = TipoAlvoAviao.Aereo;     return; }
                if (temT) { alvoAtual = alvoTerrestreAtual; tipoAlvoAtual = TipoAlvoAviao.Terrestre; return; }
                break;

            case PrioridadeAlvo.TerrestresPrimeiro:
                if (temT) { alvoAtual = alvoTerrestreAtual; tipoAlvoAtual = TipoAlvoAviao.Terrestre; return; }
                if (temA) { alvoAtual = alvoAereoAtual;     tipoAlvoAtual = TipoAlvoAviao.Aereo;     return; }
                break;

            case PrioridadeAlvo.MaisProximo:
                if (temT && !temA) { alvoAtual = alvoTerrestreAtual; tipoAlvoAtual = TipoAlvoAviao.Terrestre; return; }
                if (temA && !temT) { alvoAtual = alvoAereoAtual;     tipoAlvoAtual = TipoAlvoAviao.Aereo;     return; }

                float dT = Vector3.Distance(ObterOrigemVisao(), alvoTerrestreAtual.position);
                float dA = Vector3.Distance(ObterOrigemVisao(), alvoAereoAtual.position);

                if (dT <= dA) { alvoAtual = alvoTerrestreAtual; tipoAlvoAtual = TipoAlvoAviao.Terrestre; }
                else          { alvoAtual = alvoAereoAtual;     tipoAlvoAtual = TipoAlvoAviao.Aereo;     }
                break;
        }
    }

    // =====================================================================
    // AUXILIARES
    // =====================================================================

    private Vector3 ObterOrigemVisao()
    {
        Transform o = origemVisao != null ? origemVisao : transform;
        return o.position + Vector3.up * alturaOrigemVisao;
    }

    private bool EhDoProprioAviao(Transform alvo)
    {
        if (alvo == null) return true;
        return alvo == transform || alvo.IsChildOf(transform);
    }

    private Transform ObterRaizDoAlvo(Transform alvo)
    {
        if (alvo == null) return null;
        Transform atual = alvo;
        while (atual.parent != null)
        {
            if (atual.parent == transform) break;
            atual = atual.parent;
        }
        return atual;
    }

    private bool ObjetoOuPaisTemAlgumaTag(Transform alvo, string[] tags)
    {
        if (alvo == null || tags == null || tags.Length == 0) return false;
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
        raioVisaoTerrestre              = Mathf.Max(0.1f,  raioVisaoTerrestre);
        raioVisaoAerea                  = Mathf.Max(0.1f,  raioVisaoAerea);
        alturaOrigemVisao               = Mathf.Max(0f,    alturaOrigemVisao);
        intervaloBusca                  = Mathf.Max(0.02f, intervaloBusca);
        distanciaMinPatrulha            = Mathf.Max(20f,   distanciaMinPatrulha);
        distanciaMaxPatrulha            = Mathf.Max(distanciaMinPatrulha + 1f, distanciaMaxPatrulha);
        alturaPatrulhaMin               = Mathf.Max(5f,    alturaPatrulhaMin);
        alturaPatrulhaMax               = Mathf.Max(alturaPatrulhaMin + 1f, alturaPatrulhaMax);
        distanciaChegadaPatrulha        = Mathf.Max(5f,    distanciaChegadaPatrulha);
        comprimentoSensor               = Mathf.Max(100f,  comprimentoSensor);
        offsetSensor                    = Mathf.Max(0f,    offsetSensor);
        alcanceMissel                   = Mathf.Max(5f,    alcanceMissel);
        alcanceMetralhadora             = Mathf.Clamp(alcanceMetralhadora, 1f, alcanceMissel - 1f);
        misseisAntesDeMetralhadora      = Mathf.Max(1,     misseisAntesDeMetralhadora);
        cooldownMissel                  = Mathf.Max(0.5f,  cooldownMissel);
        tempoMetralhadoraAposPerderAlvo = Mathf.Max(0.5f,  tempoMetralhadoraAposPerderAlvo);
        velocidadeAjusteAltitude        = Mathf.Max(0.1f,  velocidadeAjusteAltitude);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        if (!desenharVisaoNoEditor) return;

        Vector3 origem = ObterOrigemVisao();

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origem, raioVisaoTerrestre);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origem, raioVisaoAerea);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(origem, alcanceMissel);

        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(origem, alcanceMetralhadora);

        Gizmos.color = sobreTerrainNoFrameAnterior ? Color.green : Color.red;
        Vector3 sensorOrigem = transform.position + Vector3.up * offsetSensor;
        Gizmos.DrawLine(sensorOrigem, sensorOrigem + Vector3.down * comprimentoSensor);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(wpAtual, 3f);
            Gizmos.DrawLine(transform.position, wpAtual);

            Gizmos.color = new Color(0.4f, 0.4f, 1f, 0.6f);
            Gizmos.DrawSphere(wpProximo, 2f);
            Gizmos.DrawLine(wpAtual, wpProximo);

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(pontoNavegacaoAtual, 1.5f);
        }

        if (alvoTerrestreAtual != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origem, alvoTerrestreAtual.position);
            Gizmos.DrawSphere(alvoTerrestreAtual.position, 0.4f);
        }

        if (alvoAereoAtual != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origem, alvoAereoAtual.position);
            Gizmos.DrawSphere(alvoAereoAtual.position, 0.6f);
        }

        if (alvoAtual != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origem, alvoAtual.position);
        }
    }
}