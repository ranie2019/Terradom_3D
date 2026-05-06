using UnityEngine;

[DisallowMultipleComponent]
public class RoboIA : MonoBehaviour
{
    private BaseAreaIA baseArea;
    private SoldadoSpownIA soldadoSpawn;
    private TankSpownIA tankSpawn;

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

    private float proximoUpdate;

    private int quantidadeBaseSoldado;
    private int quantidadeBaseTank;

    private int etapaUnidade = 0;

    private const int INDICE_BASE_SOLDADO = 0;
    private const int INDICE_BASE_TANK = 1;

    private enum TipoBaseAtual
    {
        Nenhuma,
        Soldado,
        Tank
    }

    private TipoBaseAtual baseAtual = TipoBaseAtual.Soldado;
    private bool aguardandoBase = false;

    private int indiceBaseSoldadoAtual = 0;
    private int indiceBaseTankAtual = 0;

    private void Start()
    {
        baseArea = FindFirstObjectByType<BaseAreaIA>();
        soldadoSpawn = FindFirstObjectByType<SoldadoSpownIA>();
        tankSpawn = FindFirstObjectByType<TankSpownIA>();

        if (baseArea == null)
        {
            Debug.LogError("[RoboIA] BaseAreaIA não encontrada!");
            return;
        }

        quantidadeBaseSoldado = baseArea.ContarBasesPorIndice(INDICE_BASE_SOLDADO);
        quantidadeBaseTank = baseArea.ContarBasesPorIndice(INDICE_BASE_TANK);

        timerBaseSoldado = tempoBaseSoldado;
        timerBaseTank = tempoBaseTank;

        Debug.Log("=== ROBO IA INICIADO ===");
    }

    private void Update()
    {
        if (baseArea == null) return;
        if (Time.time < proximoUpdate) return;
        proximoUpdate = Time.time + intervalo;

        AtualizarBases();
        CriarUnidades();
    }

    private void AtualizarBases()
    {
        if (baseAtual == TipoBaseAtual.Nenhuma)
        {
            if (quantidadeBaseSoldado < maxBaseSoldado)
                baseAtual = TipoBaseAtual.Soldado;
            else if (quantidadeBaseTank < maxBaseTank)
                baseAtual = TipoBaseAtual.Tank;
        }

        if (baseAtual == TipoBaseAtual.Soldado)
        {
            if (timerBaseSoldado > 0f)
            {
                timerBaseSoldado -= intervalo;
                if (timerBaseSoldado < 0f) timerBaseSoldado = 0f;
            }
            else
            {
                aguardandoBase = true;

                if (baseArea.TentarCriarBasePorIndice(INDICE_BASE_SOLDADO))
                {
                    quantidadeBaseSoldado++;
                    timerBaseSoldado = tempoBaseSoldado;
                    baseAtual = TipoBaseAtual.Tank;
                    aguardandoBase = false;

                    Debug.Log($"[RoboIA] 🏗️ Base Soldado criada ({quantidadeBaseSoldado}/{maxBaseSoldado})");
                }
            }
        }
        else if (baseAtual == TipoBaseAtual.Tank)
        {
            if (timerBaseTank > 0f)
            {
                timerBaseTank -= intervalo;
                if (timerBaseTank < 0f) timerBaseTank = 0f;
            }
            else
            {
                aguardandoBase = true;

                if (baseArea.TentarCriarBasePorIndice(INDICE_BASE_TANK))
                {
                    quantidadeBaseTank++;
                    timerBaseTank = tempoBaseTank;
                    baseAtual = TipoBaseAtual.Soldado;
                    aguardandoBase = false;

                    Debug.Log($"[RoboIA] 🏗️ Base Tank criada ({quantidadeBaseTank}/{maxBaseTank})");
                }
            }
        }
    }

    private void CriarUnidades()
    {
        if (aguardandoBase) return;

        bool criado = false;

        Transform[] basesSoldado = baseArea.ObterBasesPorIndice(INDICE_BASE_SOLDADO);
        Transform[] basesTank = baseArea.ObterBasesPorIndice(INDICE_BASE_TANK);

        switch (etapaUnidade)
        {
            case 0: // Coletor
            case 1: // Soldado
            case 2: // Guerreiro
            {
                if (basesSoldado.Length == 0) break;

                int indiceBase = indiceBaseSoldadoAtual % basesSoldado.Length;
                Transform baseEscolhida = basesSoldado[indiceBase];

                // 🔥 ENCONTRA O PONTO DE SPAWN DENTRO DA BASE
                Transform pontoSpawnDaBase = EncontrarPontoSpawn(baseEscolhida, "Soldado");

                if (etapaUnidade == 0 && soldadoSpawn.PodeCriarColetorNaBase(pontoSpawnDaBase))
                    criado = soldadoSpawn.TentarCriarColetorNaBase(pontoSpawnDaBase);
                else if (etapaUnidade == 1 && soldadoSpawn.PodeCriarSoldadoNaBase(pontoSpawnDaBase))
                    criado = soldadoSpawn.TentarCriarSoldadoNaBase(pontoSpawnDaBase);
                else if (etapaUnidade == 2 && soldadoSpawn.PodeCriarGuerreiroNaBase(pontoSpawnDaBase))
                    criado = soldadoSpawn.TentarCriarGuerreiroNaBase(pontoSpawnDaBase);

                if (criado)
                {
                    indiceBaseSoldadoAtual++;
                    Debug.Log($"[RoboIA] 🎯 Unidade na Base Soldado #{indiceBase + 1} | Spawn: {pontoSpawnDaBase.position}");
                }

                break;
            }

            case 3: // Tank
            {
                if (basesTank.Length == 0) break;

                int indiceBase = indiceBaseTankAtual % basesTank.Length;
                Transform baseEscolhida = basesTank[indiceBase];

                // 🔥 ENCONTRA O PONTO DE SPAWN DENTRO DA BASE
                Transform pontoSpawnDaBase = EncontrarPontoSpawn(baseEscolhida, "Tank");

                if (tankSpawn.PodeCriarTankNaBase(pontoSpawnDaBase))
                    criado = tankSpawn.TentarCriarTankNaBase(pontoSpawnDaBase);

                if (criado)
                {
                    indiceBaseTankAtual++;
                    Debug.Log($"[RoboIA] 🎯 Tank na Base Tank #{indiceBase + 1} | Spawn: {pontoSpawnDaBase.position}");
                }

                break;
            }
        }

        if (criado)
        {
            etapaUnidade = (etapaUnidade + 1) % 4;
        }
    }

    // 🔥 PROCURA O PONTO DE SPAWN DENTRO DA BASE
    private Transform EncontrarPontoSpawn(Transform baseTransform, string tipo)
    {
        // Procura por nome: "Spawn", "PontoSpawn", "SpawnPoint", "SpawnSoldado", "SpawnTank"
        string[] nomesPossiveis = {
            "Spawn" + tipo,
            "PontoSpawn" + tipo,
            "SpawnPoint" + tipo,
            "Spawn_" + tipo,
            "Ponto_Spawn_" + tipo,
            "Spawn",
            "PontoSpawn",
            "SpawnPoint"
        };

        foreach (string nome in nomesPossiveis)
        {
            Transform encontrado = baseTransform.Find(nome);
            if (encontrado != null)
            {
                Debug.Log($"[RoboIA] ✅ Ponto de spawn '{nome}' encontrado em {baseTransform.name} | Pos: {encontrado.position}");
                return encontrado;
            }
        }

        // Se não encontrar por nome, procura recursivamente
        Transform spawnEncontrado = ProcurarSpawnRecursivo(baseTransform);
        if (spawnEncontrado != null)
        {
            Debug.Log($"[RoboIA] ✅ Ponto de spawn encontrado recursivamente: {spawnEncontrado.name} | Pos: {spawnEncontrado.position}");
            return spawnEncontrado;
        }

        // Fallback: usa a própria base (mas loga aviso)
        Debug.LogWarning($"[RoboIA] ⚠️ Nenhum ponto de spawn encontrado na base {baseTransform.name}! Usando posição da base.");
        return baseTransform;
    }

    private Transform ProcurarSpawnRecursivo(Transform pai)
    {
        foreach (Transform filho in pai)
        {
            string nomeLower = filho.name.ToLower();
            if (nomeLower.Contains("spawn") || nomeLower.Contains("ponto"))
            {
                return filho;
            }

            Transform encontrado = ProcurarSpawnRecursivo(filho);
            if (encontrado != null)
                return encontrado;
        }
        return null;
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.green;

        Transform[] basesSoldado = baseArea != null ? baseArea.ObterBasesPorIndice(INDICE_BASE_SOLDADO) : new Transform[0];
        Transform[] basesTank = baseArea != null ? baseArea.ObterBasesPorIndice(INDICE_BASE_TANK) : new Transform[0];

        int soldadoAtual = basesSoldado.Length > 0 ? (indiceBaseSoldadoAtual % basesSoldado.Length) + 1 : 0;
        int tankAtual = basesTank.Length > 0 ? (indiceBaseTankAtual % basesTank.Length) + 1 : 0;

        string debug = $"BASE SOLDADO: {quantidadeBaseSoldado}/{maxBaseSoldado} | Timer: {timerBaseSoldado:F0}\n";
        debug += $"BASE TANK: {quantidadeBaseTank}/{maxBaseTank} | Timer: {timerBaseTank:F0}\n";
        debug += $"Construindo: {baseAtual}\n";
        debug += $"Produção: {(aguardandoBase ? "PAUSADA" : "ATIVA")}\n";
        debug += $"Próxima Unidade: {etapaUnidade}\n";
        debug += $"---\n";
        debug += $"Bases Soldado: {basesSoldado.Length} | Rodízio: #{soldadoAtual}\n";
        debug += $"Bases Tank: {basesTank.Length} | Rodízio: #{tankAtual}";

        GUI.Label(new Rect(10, 10, 500, 250), debug, style);
    }
}