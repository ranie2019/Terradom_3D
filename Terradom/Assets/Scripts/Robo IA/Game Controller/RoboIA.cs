using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RoboIA : MonoBehaviour
{
    private RoboCriar roboCriar;

    [Header("Controle Geral")]
    [SerializeField] private float intervalo = 1f;

    [Header("Base Soldado")]
    [SerializeField] private float tempoBaseSoldado = 120f;
    [SerializeField] private int   maxBaseSoldado   = 10;
    [SerializeField] private float timerBaseSoldado;

    [Header("Base Tank")]
    [SerializeField] private float tempoBaseTank = 120f;
    [SerializeField] private int   maxBaseTank   = 10;
    [SerializeField] private float timerBaseTank;

    [Header("Base Aviao")]
    [SerializeField] private float tempoBaseAviao = 150f;
    [SerializeField] private int   maxBaseAviao   = 5;
    [SerializeField] private float timerBaseAviao;

    [Header("Torre Terra")]
    [SerializeField] private float tempoTorreTerra = 90f;
    [SerializeField] private int   maxTorreTerra   = 5;
    [SerializeField] private float timerTorreTerra;

    [Header("Torre Ar")]
    [SerializeField] private float tempoTorreAr = 90f;
    [SerializeField] private int   maxTorreAr   = 5;
    [SerializeField] private float timerTorreAr;

    private float proximoUpdate;
    private int quantidadeBaseSoldado;
    private int quantidadeBaseTank;
    private int quantidadeBaseAviao;
    private int quantidadeTorreTerra;
    private int quantidadeTorreAr;

    // etapaUnidade: 0=Coletor 1=Soldado 2=Guerreiro 3=Tank 4=F-16
    private int etapaUnidade = 0;
    private const int TOTAL_ETAPAS = 5;

    private enum TipoBaseAtual { Nenhuma, Soldado, Tank, Aviao, TorreTerra, TorreAr }
    private TipoBaseAtual baseAtual      = TipoBaseAtual.Soldado;
    private bool          aguardandoBase = false;

    private int indiceBaseSoldadoAtual = 0;
    private int indiceBaseTankAtual    = 0;
    private int indiceBaseAviaoAtual   = 0;

    // =========================================================
    // PAINEL "IA DADOS" (liga/desliga pelo botão)
    // =========================================================
    [Header("Painel IA Dados")]
    [Tooltip("Botão que mostra/esconde as informações da IA. Arraste aqui (não adicione também um OnClick).")]
    [SerializeField] private Button botaoIADados;
    [SerializeField] private bool painelVisivelAoIniciar = false;

    [Header("Posição do painel (pixels)")]
    [Tooltip("Liga: o painel fica logo abaixo do botão IA Dados, alinhado à borda direita dele. " +
             "Desligado: usa a 'Margem Fixa' em relação ao canto superior direito da tela.")]
    [SerializeField] private bool posicionarAbaixoDoBotao = true;
    [Tooltip("Ajuste fino. X: positivo move para a direita, negativo para a esquerda. Y: positivo move para baixo.")]
    [SerializeField] private Vector2 deslocamentoPainel = new Vector2(0f, 8f);
    [Tooltip("Só vale com 'Posicionar Abaixo Do Botao' desligado. X: distância da borda direita. Y: distância do topo.")]
    [SerializeField] private Vector2 margemFixa = new Vector2(10f, 10f);

    private bool painelVisivel;
    private string textoPainel = string.Empty;

    // Cache do tamanho do texto (só recalcula quando o texto muda).
    private string textoMedido;
    private int fonteMedida;
    private Vector2 tamanhoTexto;
    private readonly Vector3[] cantosBotao = new Vector3[4];

    // FIX 3: GUIStyle criado uma vez para não alocar lixo toda chamada de OnGUI.
    // (OnGUI pode ser chamado várias vezes por frame — evitar new GUIStyle() ali dentro)
    private GUIStyle _debugStyle;
    private Transform raizUnidadesIA;
    private int quantidadeColetores;
    private int quantidadeSoldados;
    private int quantidadeGuerreiros;
    private int quantidadeTanques;
    private int quantidadeAvioes;

    private void Start()
    {
        if (botaoIADados != null)
            botaoIADados.onClick.AddListener(AlternarPainelDados);

        DefinirPainelVisivel(painelVisivelAoIniciar);

        roboCriar = FindFirstObjectByType<RoboCriar>();
        if (roboCriar == null)
        {
            Debug.LogError("[RoboIA] → RoboCriar não encontrado!");
            return;
        }

        timerBaseSoldado = tempoBaseSoldado;
        timerBaseTank    = tempoBaseTank;
        timerBaseAviao   = tempoBaseAviao;
        timerTorreTerra  = tempoTorreTerra;
        timerTorreAr     = tempoTorreAr;

        Debug.Log("=== ROBO IA INICIADO ===");
    }

    private void OnDestroy()
    {
        if (botaoIADados != null)
            botaoIADados.onClick.RemoveListener(AlternarPainelDados);
    }

    private void Update()
    {
        if (!RoboCriarAtivo()) return;
        if (Time.time < proximoUpdate) return;

        proximoUpdate = Time.time + intervalo;
        AtualizarBases();
        OrdenarUnidades();
        AtualizarPainel();
    }

    private bool RoboCriarAtivo() => roboCriar != null && roboCriar.isActiveAndEnabled;

    // =========================================================
    // BASES — RoboIA decide, RoboCriar executa
    // =========================================================

    private void AtualizarBases()
    {
        quantidadeBaseSoldado = roboCriar.ContarBaseSoldado();
        quantidadeBaseTank    = roboCriar.ContarBaseTank();
        quantidadeBaseAviao   = roboCriar.ContarBaseAviao();
        quantidadeTorreTerra  = roboCriar.ContarTorreTerra();
        quantidadeTorreAr     = roboCriar.ContarTorreAr();

        // Sem Base Soldado: prioriza reconstrução mas NÃO pausa ainda.
        // A pausa só ocorre se realmente faltar recurso (ver abaixo).
        if (quantidadeBaseSoldado == 0 && baseAtual != TipoBaseAtual.Soldado)
        {
            baseAtual        = TipoBaseAtual.Soldado;
            timerBaseSoldado = 0f;
        }

        if (baseAtual == TipoBaseAtual.Nenhuma)
        {
            if      (quantidadeBaseSoldado < maxBaseSoldado) baseAtual = TipoBaseAtual.Soldado;
            else if (quantidadeBaseTank    < maxBaseTank)    baseAtual = TipoBaseAtual.Tank;
            else if (quantidadeBaseAviao   < maxBaseAviao)   baseAtual = TipoBaseAtual.Aviao;
            else if (quantidadeTorreTerra  < maxTorreTerra)  baseAtual = TipoBaseAtual.TorreTerra;
            else if (quantidadeTorreAr     < maxTorreAr)     baseAtual = TipoBaseAtual.TorreAr;
        }

        if (baseAtual == TipoBaseAtual.Soldado)
        {
            if (quantidadeBaseSoldado >= maxBaseSoldado) { baseAtual = TipoBaseAtual.Tank; aguardandoBase = false; return; }
            TickTimer(ref timerBaseSoldado);
            if (timerBaseSoldado <= 0f)
            {
                bool temRec = GameControllerRecursosIA.Instance != null &&
                              GameControllerRecursosIA.Instance.PodeCriarBaseSoldado();
                aguardandoBase = !temRec;
                if (roboCriar.CriarBaseSoldado()) { timerBaseSoldado = tempoBaseSoldado; baseAtual = TipoBaseAtual.Tank; aguardandoBase = false; }
            }
        }
        else if (baseAtual == TipoBaseAtual.Tank)
        {
            if (quantidadeBaseTank >= maxBaseTank) { baseAtual = TipoBaseAtual.Aviao; aguardandoBase = false; return; }
            TickTimer(ref timerBaseTank);
            if (timerBaseTank <= 0f)
            {
                bool temRec = GameControllerRecursosIA.Instance != null &&
                              GameControllerRecursosIA.Instance.PodeCriarBaseVeiculo();
                aguardandoBase = !temRec;
                if (roboCriar.CriarBaseTank()) { timerBaseTank = tempoBaseTank; baseAtual = TipoBaseAtual.Aviao; aguardandoBase = false; }
            }
        }
        else if (baseAtual == TipoBaseAtual.Aviao)
        {
            if (quantidadeBaseAviao >= maxBaseAviao) { baseAtual = TipoBaseAtual.TorreTerra; aguardandoBase = false; return; }
            TickTimer(ref timerBaseAviao);
            if (timerBaseAviao <= 0f)
            {
                bool temRec = GameControllerRecursosIA.Instance != null &&
                              GameControllerRecursosIA.Instance.PodeCriarBaseAviao();
                aguardandoBase = !temRec;
                if (roboCriar.CriarBaseAviao()) { timerBaseAviao = tempoBaseAviao; baseAtual = TipoBaseAtual.TorreTerra; aguardandoBase = false; }
            }
        }
        else if (baseAtual == TipoBaseAtual.TorreTerra)
        {
            if (quantidadeTorreTerra >= maxTorreTerra) { baseAtual = TipoBaseAtual.TorreAr; aguardandoBase = false; return; }
            TickTimer(ref timerTorreTerra);
            if (timerTorreTerra <= 0f)
            {
                bool temRec = GameControllerRecursosIA.Instance != null &&
                              GameControllerRecursosIA.Instance.PodeCriarTorreTerra();
                aguardandoBase = !temRec;
                if (roboCriar.CriarTorreTerra()) { timerTorreTerra = tempoTorreTerra; baseAtual = TipoBaseAtual.TorreAr; aguardandoBase = false; }
            }
        }
        else if (baseAtual == TipoBaseAtual.TorreAr)
        {
            if (quantidadeTorreAr >= maxTorreAr) { baseAtual = TipoBaseAtual.Soldado; aguardandoBase = false; return; }
            TickTimer(ref timerTorreAr);
            if (timerTorreAr <= 0f)
            {
                bool temRec = GameControllerRecursosIA.Instance != null &&
                              GameControllerRecursosIA.Instance.PodeCriarTorreAr();
                aguardandoBase = !temRec;
                if (roboCriar.CriarTorreAr()) { timerTorreAr = tempoTorreAr; baseAtual = TipoBaseAtual.Soldado; aguardandoBase = false; }
            }
        }
    }

    private void TickTimer(ref float timer)
    {
        if (timer > 0f)
        {
            timer -= intervalo;
            if (timer < 0f) timer = 0f;
        }
    }

    // =========================================================
    // UNIDADES — RoboIA decide, RoboCriar executa
    // =========================================================

    private void OrdenarUnidades()
    {
        if (aguardandoBase) return;

        bool criado = false;
        Transform[] basesSoldado = roboCriar.ObterBasesSoldado();
        Transform[] basesTank    = roboCriar.ObterBasesTank();
        Transform[] basesAviao   = roboCriar.ObterBasesAviao();

        if (etapaUnidade == 3 && (basesTank.Length == 0 || !roboCriar.TankSpawnDisponivel()))
            etapaUnidade = 4;

        if (etapaUnidade == 4 && (basesAviao.Length == 0 || !roboCriar.AviaoSpawnDisponivel()))
            etapaUnidade = 0;

        switch (etapaUnidade)
        {
            case 0: case 1: case 2:
            {
                if (basesSoldado.Length == 0) break;
                int indice      = indiceBaseSoldadoAtual % basesSoldado.Length;
                Transform base_ = basesSoldado[indice];
                Transform spawn = roboCriar.EncontrarPontoSpawn(base_, "Soldado");
                if (etapaUnidade == 0 && roboCriar.PodeCriarColetorNaBase(spawn))
                    criado = roboCriar.CriarColetorNaBase(spawn);
                else if (etapaUnidade == 1 && roboCriar.PodeCriarSoldadoNaBase(spawn))
                    criado = roboCriar.CriarSoldadoNaBase(spawn);
                else if (etapaUnidade == 2 && roboCriar.PodeCriarGuerreiroNaBase(spawn))
                    criado = roboCriar.CriarGuerreiroNaBase(spawn);
                if (criado) indiceBaseSoldadoAtual++;
                break;
            }
            case 3:
            {
                if (basesTank.Length == 0) { etapaUnidade = 4; break; }
                int indice      = indiceBaseTankAtual % basesTank.Length;
                Transform base_ = basesTank[indice];
                Transform spawn = roboCriar.EncontrarPontoSpawn(base_, "Tank");
                if (roboCriar.PodeCriarTankNaBase(spawn))
                    criado = roboCriar.CriarTankNaBase(spawn);
                if (criado) indiceBaseTankAtual++;
                break;
            }
            case 4:
            {
                if (basesAviao.Length == 0) { etapaUnidade = 0; break; }
                int indice      = indiceBaseAviaoAtual % basesAviao.Length;
                Transform base_ = basesAviao[indice];
                Transform spawn = roboCriar.EncontrarPontoSpawn(base_, "Aviao");
                if (roboCriar.PodeCriarF16NaBase(spawn))
                    criado = roboCriar.CriarF16NaBase(spawn);
                if (criado) indiceBaseAviaoAtual++;
                break;
            }
        }

        if (criado) etapaUnidade = (etapaUnidade + 1) % TOTAL_ETAPAS;
    }

    // =========================================================
    // PAINEL "IA DADOS"
    // O texto é montado 1x por tick e só com o painel visível (antes era montado
    // dentro do OnGUI, que roda várias vezes por frame).
    // =========================================================

    /// <summary>Liga/desliga o painel. Use no botão "IA Dados".</summary>
    public void AlternarPainelDados()
    {
        DefinirPainelVisivel(!painelVisivel);
    }

    public void DefinirPainelVisivel(bool visivel)
    {
        painelVisivel = visivel;

        // Ao abrir, já mostra os dados atuais (sem esperar o próximo tick).
        if (painelVisivel)
            AtualizarPainel();
    }

    private void AtualizarPainel()
    {
        if (!painelVisivel) return;

        AtualizarContagemUnidades();
        textoPainel = ConstruirTextoPainel();
    }

    private void AtualizarContagemUnidades()
    {
        if (raizUnidadesIA == null)
        {
            GameObject pastaUnidades = GameObject.Find("Clone Unidades IA");
            if (pastaUnidades != null)
                raizUnidadesIA = pastaUnidades.transform;
        }

        quantidadeColetores = 0;
        quantidadeSoldados = 0;
        quantidadeGuerreiros = 0;
        quantidadeTanques = 0;
        quantidadeAvioes = 0;

        if (raizUnidadesIA == null)
            return;

        foreach (Transform unidade in raizUnidadesIA)
        {
            if (unidade == null || !unidade.gameObject.activeInHierarchy)
                continue;

            if (unidade.GetComponentInChildren<ColetorAi>(true) != null)
                quantidadeColetores++;
            else if (unidade.GetComponentInChildren<AtaqueDistancia>(true) != null)
                quantidadeSoldados++;
            else if (unidade.GetComponentInChildren<Ataque>(true) != null)
                quantidadeGuerreiros++;
            else if (unidade.GetComponentInChildren<TankLeve>(true) != null)
                quantidadeTanques++;
            else if (unidade.GetComponentInChildren<AviaoVoo>(true) != null)
                quantidadeAvioes++;
        }
    }

    private string ConstruirTextoPainel()
    {
        string etapaLabel = etapaUnidade switch
        {
            0 => "Coletor",
            1 => "Soldado",
            2 => "Guerreiro",
            3 => "Tank",
            4 => "F-16",
            _ => etapaUnidade.ToString()
        };

        string debug = $"BASE SOLDADO:  {quantidadeBaseSoldado}/{maxBaseSoldado} | Timer: {timerBaseSoldado:F0}\n";
        debug += $"BASE TANK:     {quantidadeBaseTank}/{maxBaseTank} | Timer: {timerBaseTank:F0}\n";
        debug += $"BASE AVIAO:    {quantidadeBaseAviao}/{maxBaseAviao} | Timer: {timerBaseAviao:F0}\n";
        debug += $"TORRE TERRA:   {quantidadeTorreTerra}/{maxTorreTerra} | Timer: {timerTorreTerra:F0}\n";
        debug += $"TORRE AR:      {quantidadeTorreAr}/{maxTorreAr} | Timer: {timerTorreAr:F0}\n";
        debug += $"Construindo:   {baseAtual}\n";
        debug += $"Producao:      {(aguardandoBase ? "PAUSADA" : "ATIVA")}\n";
        debug += $"Prox. Unidade: {etapaLabel}\n";
        debug += $"---\n";
        debug += "RECURSOS IA\n";
        int _pedra   = GameControllerRecursosIA.Instance != null ? GameControllerRecursosIA.Instance.pedra   : 0;
        int _madeira = GameControllerRecursosIA.Instance != null ? GameControllerRecursosIA.Instance.madeira : 0;
        int _metal   = GameControllerRecursosIA.Instance != null ? GameControllerRecursosIA.Instance.metal   : 0;
        debug += $"Pedra:   {_pedra}\n";
        debug += $"Madeira: {_madeira}\n";
        debug += $"Metal:   {_metal}\n";
        debug += $"---\n";
        debug += "UNIDADES ATUAIS\n";
        debug += $"Guerreiros: {quantidadeGuerreiros}\n";
        debug += $"Soldados:   {quantidadeSoldados}\n";
        debug += $"Coletores:  {quantidadeColetores}\n";
        debug += $"Tanques:    {quantidadeTanques}\n";
        debug += $"Avioes:     {quantidadeAvioes}";
        return debug;
    }

    private void OnGUI()
    {
        if (!painelVisivel)
            return;

        if (!RoboCriarAtivo())
        {
            GUI.Label(new Rect(10, 10, 300, 30), "[RoboIA] → RoboCriar desativado");
            return;
        }

        // GUIStyle criado só uma vez (lazy init) para não alocar a cada frame
        if (_debugStyle == null)
        {
            _debugStyle = new GUIStyle();
            _debugStyle.normal.textColor = Color.green;
        }

        _debugStyle.fontSize = Screen.width < 480 || Screen.height < 360 ? 12 : 16;

        Vector2 tamanho = ObterTamanhoDoTexto();

        float x;
        float y;
        if (posicionarAbaixoDoBotao && TentarObterRetanguloDoBotao(out Rect botao))
        {
            // Alinha a borda direita do texto com a borda direita do botão e coloca logo abaixo dele.
            x = botao.xMax - tamanho.x + deslocamentoPainel.x;
            y = botao.yMax + deslocamentoPainel.y;
        }
        else
        {
            x = Screen.width - tamanho.x - margemFixa.x;
            y = margemFixa.y;
        }

        // Mantém o painel inteiro dentro da tela.
        x = Mathf.Clamp(x, 0f, Mathf.Max(0f, Screen.width - tamanho.x));
        y = Mathf.Clamp(y, 0f, Mathf.Max(0f, Screen.height - tamanho.y));

        GUI.Label(new Rect(x, y, tamanho.x + 4f, tamanho.y + 4f), textoPainel, _debugStyle);
    }

    private Vector2 ObterTamanhoDoTexto()
    {
        if (!ReferenceEquals(textoMedido, textoPainel) || fonteMedida != _debugStyle.fontSize)
        {
            tamanhoTexto = _debugStyle.CalcSize(new GUIContent(textoPainel));
            textoMedido = textoPainel;
            fonteMedida = _debugStyle.fontSize;
        }
        return tamanhoTexto;
    }

    /// <summary>Retângulo do botão em coordenadas do OnGUI (origem no canto superior esquerdo).</summary>
    private bool TentarObterRetanguloDoBotao(out Rect retangulo)
    {
        retangulo = default;

        if (botaoIADados == null || !botaoIADados.gameObject.activeInHierarchy)
            return false;

        RectTransform rt = botaoIADados.transform as RectTransform;
        if (rt == null)
            return false;

        Canvas canvas = botaoIADados.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        rt.GetWorldCorners(cantosBotao);

        float xMin = float.MaxValue, xMax = float.MinValue;
        float yMin = float.MaxValue, yMax = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            Vector2 p = cam != null ? (Vector2)cam.WorldToScreenPoint(cantosBotao[i]) : (Vector2)cantosBotao[i];
            xMin = Mathf.Min(xMin, p.x);
            xMax = Mathf.Max(xMax, p.x);
            yMin = Mathf.Min(yMin, p.y);
            yMax = Mathf.Max(yMax, p.y);
        }

        // Tela: y cresce para cima. OnGUI: y cresce para baixo.
        retangulo = Rect.MinMaxRect(xMin, Screen.height - yMax, xMax, Screen.height - yMin);
        return true;
    }
}