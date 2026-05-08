using UnityEngine;

[DisallowMultipleComponent]
public class RoboIA : MonoBehaviour
{
    private RoboCriar roboCriar;

    [Header("Controle Geral")]
    [SerializeField] private float intervalo = 1f;

    [Header("Base Soldado")]
    [SerializeField] private float tempoBaseSoldado = 120f;
    [SerializeField] private int maxBaseSoldado = 10;
    [SerializeField] private float timerBaseSoldado;

    [Header("Base Tank")]
    [SerializeField] private float tempoBaseTank = 120f;
    [SerializeField] private int maxBaseTank = 10;
    [SerializeField] private float timerBaseTank;

    [Header("Torre Terra")]
    [SerializeField] private float tempoTorreTerra = 90f;
    [SerializeField] private int maxTorreTerra = 5;
    [SerializeField] private float timerTorreTerra;

    private float proximoUpdate;

    private int quantidadeBaseSoldado;
    private int quantidadeBaseTank;
    private int quantidadeTorreTerra;

    private int etapaUnidade = 0;

    private const int INDICE_BASE_SOLDADO = 0;
    private const int INDICE_BASE_TANK    = 1;
    private const int INDICE_TORRE_TERRA  = 3;

    private enum TipoBaseAtual { Nenhuma, Soldado, Tank, TorreTerra }
    private TipoBaseAtual baseAtual = TipoBaseAtual.Soldado;
    private bool aguardandoBase = false;

    private int indiceBaseSoldadoAtual = 0;
    private int indiceBaseTankAtual    = 0;

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
        timerTorreTerra  = tempoTorreTerra;

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
        // Lê quantidade real da cena a cada tick
        quantidadeBaseSoldado = roboCriar.ContarBaseSoldado();
        quantidadeBaseTank    = roboCriar.ContarBaseTank();
        quantidadeTorreTerra  = roboCriar.ContarTorreTerra();

        // Define próxima base se nenhuma em andamento
        if (baseAtual == TipoBaseAtual.Nenhuma)
        {
            if      (quantidadeBaseSoldado < maxBaseSoldado) baseAtual = TipoBaseAtual.Soldado;
            else if (quantidadeBaseTank    < maxBaseTank)    baseAtual = TipoBaseAtual.Tank;
            else if (quantidadeTorreTerra  < maxTorreTerra)  baseAtual = TipoBaseAtual.TorreTerra;
        }

        if (baseAtual == TipoBaseAtual.Soldado)
        {
            // Já no máximo — passa pra próxima sem pausar unidades
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
                    // Criou com sucesso — reinicia timer, avança tipo, libera unidades
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
                baseAtual      = TipoBaseAtual.TorreTerra;
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
                    baseAtual      = TipoBaseAtual.TorreTerra;
                    aguardandoBase = false;
                }
            }
        }
        else if (baseAtual == TipoBaseAtual.TorreTerra)
        {
            if (quantidadeTorreTerra >= maxTorreTerra)
            {
                baseAtual      = TipoBaseAtual.Soldado;
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
                    baseAtual       = TipoBaseAtual.Soldado;
                    aguardandoBase  = false;
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

        // Sem base tank — pula etapa tank e continua com soldado
        if (etapaUnidade == 3 && (basesTank.Length == 0 || !roboCriar.TankSpawnDisponivel()))
        {
            etapaUnidade = 0;
        }

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
                if (basesTank.Length == 0) { etapaUnidade = 0; break; }

                int indice      = indiceBaseTankAtual % basesTank.Length;
                Transform base_ = basesTank[indice];
                Transform spawn = roboCriar.EncontrarPontoSpawn(base_, "Tank");

                if (roboCriar.PodeCriarTankNaBase(spawn))
                    criado = roboCriar.CriarTankNaBase(spawn);

                if (criado) indiceBaseTankAtual++;
                break;
            }
        }

        if (criado) etapaUnidade = (etapaUnidade + 1) % 4;
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
        style.fontSize = 16;
        style.normal.textColor = Color.green;

        Transform[] basesSoldado = roboCriar.ObterBasesSoldado();
        Transform[] basesTank    = roboCriar.ObterBasesTank();

        int soldadoAtual = basesSoldado.Length > 0 ? (indiceBaseSoldadoAtual % basesSoldado.Length) + 1 : 0;
        int tankAtual    = basesTank.Length    > 0 ? (indiceBaseTankAtual    % basesTank.Length)    + 1 : 0;

        string debug = $"BASE SOLDADO:  {quantidadeBaseSoldado}/{maxBaseSoldado} | Timer: {timerBaseSoldado:F0}\n";
        debug += $"BASE TANK:     {quantidadeBaseTank}/{maxBaseTank} | Timer: {timerBaseTank:F0}\n";
        debug += $"TORRE TERRA:   {quantidadeTorreTerra}/{maxTorreTerra} | Timer: {timerTorreTerra:F0}\n";
        debug += $"Construindo:   {baseAtual}\n";
        debug += $"Produção:      {(aguardandoBase ? "PAUSADA" : "ATIVA")}\n";
        debug += $"Próx. Unidade: {etapaUnidade}\n";
        debug += $"---\n";
        debug += $"Bases Soldado: {basesSoldado.Length} | Rodízio: #{soldadoAtual}\n";
        debug += $"Bases Tank:    {basesTank.Length}    | Rodízio: #{tankAtual}";

        GUI.Label(new Rect(10, 10, 500, 280), debug, style);
    }
}