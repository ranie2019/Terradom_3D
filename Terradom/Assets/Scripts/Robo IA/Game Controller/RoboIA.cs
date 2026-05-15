using UnityEngine;

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

    [Header("Base Avião")]
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

    private void Start()
    {
        roboCriar = FindFirstObjectByType<RoboCriar>();

        if (roboCriar == null)
        {
            Debug.LogError("[RoboIA] ❌ RoboCriar não encontrado!");
            return;
        }

        timerBaseSoldado = tempoBaseSoldado;
        timerBaseTank    = tempoBaseTank;
        timerBaseAviao   = tempoBaseAviao;
        timerTorreTerra  = tempoTorreTerra;
        timerTorreAr     = tempoTorreAr;

        Debug.Log("=== ROBO IA INICIADO ===");
    }

    private void Update()
    {
        if (!RoboCriarAtivo()) return;
        if (Time.time < proximoUpdate) return;
        proximoUpdate = Time.time + intervalo;

        AtualizarBases();
        OrdenarUnidades();
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

        // Define próxima base se nenhuma em andamento
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
            if (quantidadeBaseSoldado >= maxBaseSoldado)
            {
                baseAtual      = TipoBaseAtual.Tank;
                aguardandoBase = false;
                return;
            }

            TickTimer(ref timerBaseSoldado);

            if (timerBaseSoldado <= 0f)
            {
                aguardandoBase = true;

                if (roboCriar.CriarBaseSoldado())
                {
                    timerBaseSoldado = tempoBaseSoldado;
                    baseAtual        = TipoBaseAtual.Tank;
                    aguardandoBase   = false;
                }
            }
        }
        else if (baseAtual == TipoBaseAtual.Tank)
        {
            if (quantidadeBaseTank >= maxBaseTank)
            {
                baseAtual      = TipoBaseAtual.Aviao;
                aguardandoBase = false;
                return;
            }

            TickTimer(ref timerBaseTank);

            if (timerBaseTank <= 0f)
            {
                aguardandoBase = true;

                if (roboCriar.CriarBaseTank())
                {
                    timerBaseTank  = tempoBaseTank;
                    baseAtual      = TipoBaseAtual.Aviao;
                    aguardandoBase = false;
                }
            }
        }
        else if (baseAtual == TipoBaseAtual.Aviao)
        {
            if (quantidadeBaseAviao >= maxBaseAviao)
            {
                baseAtual      = TipoBaseAtual.TorreTerra;
                aguardandoBase = false;
                return;
            }

            TickTimer(ref timerBaseAviao);

            if (timerBaseAviao <= 0f)
            {
                aguardandoBase = true;

                if (roboCriar.CriarBaseAviao())
                {
                    timerBaseAviao = tempoBaseAviao;
                    baseAtual      = TipoBaseAtual.TorreTerra;
                    aguardandoBase = false;
                }
            }
        }
        else if (baseAtual == TipoBaseAtual.TorreTerra)
        {
            if (quantidadeTorreTerra >= maxTorreTerra)
            {
                baseAtual      = TipoBaseAtual.TorreAr;
                aguardandoBase = false;
                return;
            }

            TickTimer(ref timerTorreTerra);

            if (timerTorreTerra <= 0f)
            {
                aguardandoBase = true;

                if (roboCriar.CriarTorreTerra())
                {
                    timerTorreTerra = tempoTorreTerra;
                    baseAtual       = TipoBaseAtual.TorreAr;
                    aguardandoBase  = false;
                }
            }
        }
        else if (baseAtual == TipoBaseAtual.TorreAr)
        {
            if (quantidadeTorreAr >= maxTorreAr)
            {
                baseAtual      = TipoBaseAtual.Soldado;
                aguardandoBase = false;
                return;
            }

            TickTimer(ref timerTorreAr);

            if (timerTorreAr <= 0f)
            {
                aguardandoBase = true;

                if (roboCriar.CriarTorreAr())
                {
                    timerTorreAr   = tempoTorreAr;
                    baseAtual      = TipoBaseAtual.Soldado;
                    aguardandoBase = false;
                }
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

        // Pula etapa Tank se não há bases tank disponíveis
        if (etapaUnidade == 3 && (basesTank.Length == 0 || !roboCriar.TankSpawnDisponivel()))
            etapaUnidade = 4;

        // Pula etapa F-16 se não há bases avião disponíveis (Mesma lógica do Tank)
        if (etapaUnidade == 4 && (basesAviao.Length == 0 || !roboCriar.AviaoSpawnDisponivel()))
            etapaUnidade = 0;

        switch (etapaUnidade)
        {
            case 0: // Coletor
            case 1: // Soldado
            case 2: // Guerreiro
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

            case 3: // Tank
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

            case 4: // F-16
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
    // DEBUG
    // =========================================================

    private void OnGUI()
    {
        if (!RoboCriarAtivo())
        {
            GUI.Label(new Rect(10, 10, 300, 30), "[RoboIA] ⏸️ RoboCriar desativado");
            return;
        }

        GUIStyle style = new GUIStyle();
        style.fontSize          = 16;
        style.normal.textColor  = Color.green;

        Transform[] basesSoldado = roboCriar.ObterBasesSoldado();
        Transform[] basesTank    = roboCriar.ObterBasesTank();
        Transform[] basesAviao   = roboCriar.ObterBasesAviao();

        int soldadoAtual = basesSoldado.Length > 0 ? (indiceBaseSoldadoAtual % basesSoldado.Length) + 1 : 0;
        int tankAtual    = basesTank.Length    > 0 ? (indiceBaseTankAtual    % basesTank.Length)    + 1 : 0;
        int aviaoAtual   = basesAviao.Length   > 0 ? (indiceBaseAviaoAtual   % basesAviao.Length)   + 1 : 0;

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
        debug += $"BASE AVIÃO:    {quantidadeBaseAviao}/{maxBaseAviao} | Timer: {timerBaseAviao:F0}\n";
        debug += $"TORRE TERRA:   {quantidadeTorreTerra}/{maxTorreTerra} | Timer: {timerTorreTerra:F0}\n";
        debug += $"TORRE AR:      {quantidadeTorreAr}/{maxTorreAr} | Timer: {timerTorreAr:F0}\n";
        debug += $"Construindo:   {baseAtual}\n";
        debug += $"Produção:      {(aguardandoBase ? "PAUSADA" : "ATIVA")}\n";
        debug += $"Próx. Unidade: {etapaLabel}\n";
        debug += $"---\n";
        debug += $"Bases Soldado: {basesSoldado.Length} | Rodízio: #{soldadoAtual}\n";
        debug += $"Bases Tank:    {basesTank.Length}    | Rodízio: #{tankAtual}\n";
        debug += $"Bases Avião:   {basesAviao.Length}   | Rodízio: #{aviaoAtual}";

        GUI.Label(new Rect(Screen.width - 270, 10, 520, 320), debug, style);
    }
}
