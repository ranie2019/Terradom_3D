using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BotoesProducaoUnidades : MonoBehaviour
{
    private static BotoesProducaoUnidades instance;

    [Header("Botoes de producao")]
    [SerializeField] private Button botaoGuerreiro;
    [SerializeField] private Button botaoRecurso;
    [SerializeField] private Button botaoSoldado;
    [SerializeField] private Button botaoTank;
    [SerializeField] private Button botaoAviao;
    [SerializeField] private Button botaoHelicoptero;

    [Header("Objetos visuais dos botoes")]
    [SerializeField] private GameObject objetoBotaoGuerreiro;
    [SerializeField] private GameObject objetoBotaoRecurso;
    [SerializeField] private GameObject objetoBotaoSoldado;
    [SerializeField] private GameObject objetoBotaoTank;
    [SerializeField] private GameObject objetoBotaoAviao;
    [SerializeField] private GameObject objetoBotaoHelicoptero;

    [Header("Indices dos prefabs na base selecionada")]
    [SerializeField] private int indiceGuerreiroNaBase = 0;
    [SerializeField] private int indiceRecursoNaBase = 1;
    [SerializeField] private int indiceSoldadoNaBase = 2;
    [SerializeField] private int indiceTankNaBase = 0;
    [SerializeField] private int indiceAviaoNaBase = 0;
    [SerializeField] private int indiceHelicopteroNaBase = 1;

    [Header("Atualizacao")]
    [SerializeField] private bool atualizarTodoFrame = true;
    [SerializeField] private float intervaloAtualizacao = 0.1f;

    private float proximaAtualizacao;

    private void Awake()
    {
        instance = this;
        EsconderTodosOsBotoes();
    }

    private void OnEnable()
    {
        AtualizarEstado();
    }

    private void Update()
    {
        if (!atualizarTodoFrame)
            return;

        if (Time.time < proximaAtualizacao)
            return;

        proximaAtualizacao = Time.time + Mathf.Max(0.02f, intervaloAtualizacao);
        AtualizarEstado();
    }

    public static void AtualizarTodos()
    {
        if (instance != null)
            instance.AtualizarEstado();
    }

    // =====================================================================
    // ONCLICK DOS BOTOES
    // =====================================================================

    public void BotaoCriarGuerreiro()
    {
        MonoBehaviour spawn = GetSpawnSelecionado();

        if (spawn == null || !EhBaseDeSoldados(spawn))
            return;

        TentarCriar(spawn, indiceGuerreiroNaBase, new string[] { "CriarGuerreiro" });
        AtualizarEstado();
    }

    public void BotaoCriarRecurso()
    {
        MonoBehaviour spawn = GetSpawnSelecionado();

        if (spawn == null || !EhBaseDeSoldados(spawn))
            return;

        TentarCriar(spawn, indiceRecursoNaBase, new string[] { "CriarRecurso", "CriarColetor" });
        AtualizarEstado();
    }

    public void BotaoCriarSoldado()
    {
        MonoBehaviour spawn = GetSpawnSelecionado();

        if (spawn == null || !EhBaseDeSoldados(spawn))
            return;

        TentarCriar(spawn, indiceSoldadoNaBase, new string[] { "CriarSoldado" });
        AtualizarEstado();
    }

    public void BotaoCriarTank()
    {
        MonoBehaviour spawn = GetSpawnSelecionado();

        if (spawn == null || !EhBaseDeVeiculos(spawn))
            return;

        TentarCriar(spawn, indiceTankNaBase, new string[] { "CriarTank", "CriarVeiculo" });
        AtualizarEstado();
    }

    public void BotaoCriarAviao()
    {
        MonoBehaviour spawn = GetSpawnSelecionado();

        if (spawn == null || !EhBaseAerea(spawn))
            return;

        TentarCriar(spawn, indiceAviaoNaBase, new string[] { "CriarAviao", "CriarAeronave" });
        AtualizarEstado();
    }

    public void BotaoCriarHelicoptero()
    {
        MonoBehaviour spawn = GetSpawnSelecionado();

        if (spawn == null || !EhBaseAerea(spawn))
            return;

        TentarCriar(spawn, indiceHelicopteroNaBase, new string[] { "CriarHelicoptero", "CriarAeronave" });
        AtualizarEstado();
    }

    // =====================================================================
    // ESTADO DOS BOTOES
    // =====================================================================

    private void AtualizarEstado()
    {
        MonoBehaviour spawn = GetSpawnSelecionado();

        if (spawn == null)
        {
            EsconderTodosOsBotoes();
            return;
        }

        bool baseSoldados = EhBaseDeSoldados(spawn);
        bool baseVeiculos = EhBaseDeVeiculos(spawn);
        bool baseAerea = EhBaseAerea(spawn);

        AtualizarBotaoProducao(
            botaoGuerreiro,
            objetoBotaoGuerreiro,
            spawn,
            baseSoldados,
            indiceGuerreiroNaBase,
            new string[] { "CriarGuerreiro" },
            new string[] { "PodeCriarGuerreiro" }
        );

        AtualizarBotaoProducao(
            botaoRecurso,
            objetoBotaoRecurso,
            spawn,
            baseSoldados,
            indiceRecursoNaBase,
            new string[] { "CriarRecurso", "CriarColetor" },
            new string[] { "PodeCriarRecurso", "PodeCriarColetor" }
        );

        AtualizarBotaoProducao(
            botaoSoldado,
            objetoBotaoSoldado,
            spawn,
            baseSoldados,
            indiceSoldadoNaBase,
            new string[] { "CriarSoldado" },
            new string[] { "PodeCriarSoldado" }
        );

        AtualizarBotaoProducao(
            botaoTank,
            objetoBotaoTank,
            spawn,
            baseVeiculos,
            indiceTankNaBase,
            new string[] { "CriarTank", "CriarVeiculo" },
            new string[] { "PodeCriarTank", "PodeCriarVeiculo" }
        );

        AtualizarBotaoProducao(
            botaoAviao,
            objetoBotaoAviao,
            spawn,
            baseAerea,
            indiceAviaoNaBase,
            new string[] { "CriarAviao", "CriarAeronave" },
            new string[] { "PodeCriarAviao", "PodeCriarAeronave" }
        );

        AtualizarBotaoProducao(
            botaoHelicoptero,
            objetoBotaoHelicoptero,
            spawn,
            baseAerea,
            indiceHelicopteroNaBase,
            new string[] { "CriarHelicoptero", "CriarAeronave" },
            new string[] { "PodeCriarHelicoptero", "PodeCriarAeronave" }
        );
    }

    private void AtualizarBotaoProducao(
        Button botao,
        GameObject objetoVisual,
        MonoBehaviour spawn,
        bool baseCorreta,
        int indicePrefab,
        string[] metodosCriar,
        string[] metodosPodeCriar
    )
    {
        bool mostrar = false;
        bool podeCriar = false;

        if (spawn != null && baseCorreta)
        {
            mostrar = ExisteOpcao(spawn, indicePrefab, metodosCriar);
            podeCriar = mostrar && PodeCriar(spawn, indicePrefab, metodosPodeCriar);
        }

        AtualizarBotao(botao, objetoVisual, mostrar, podeCriar);
    }

    private void AtualizarBotao(Button botao, GameObject objetoVisual, bool mostrar, bool podeCriar)
    {
        if (objetoVisual != null)
            objetoVisual.SetActive(mostrar);

        if (botao != null)
            botao.interactable = mostrar && podeCriar;
    }

    private void EsconderTodosOsBotoes()
    {
        AtualizarBotao(botaoGuerreiro, objetoBotaoGuerreiro, false, false);
        AtualizarBotao(botaoRecurso, objetoBotaoRecurso, false, false);
        AtualizarBotao(botaoSoldado, objetoBotaoSoldado, false, false);
        AtualizarBotao(botaoTank, objetoBotaoTank, false, false);
        AtualizarBotao(botaoAviao, objetoBotaoAviao, false, false);
        AtualizarBotao(botaoHelicoptero, objetoBotaoHelicoptero, false, false);
    }

    // =====================================================================
    // BASE SELECIONADA / SPAWN SELECIONADO
    // =====================================================================

    private MonoBehaviour GetSpawnSelecionado()
    {
        BaseSelecionavel baseSelecionada = BaseSelecionavel.BaseAtualSelecionada;

        if (baseSelecionada == null)
            return null;

        MonoBehaviour spawn = BuscarSpawnNaBase(baseSelecionada.transform, TipoBasePreferida.Nenhuma);

        if (spawn != null)
            return spawn;

        if (baseSelecionada.transform.root != null && baseSelecionada.transform.root != baseSelecionada.transform)
        {
            spawn = BuscarSpawnNaBase(baseSelecionada.transform.root, TipoBasePreferida.Nenhuma);

            if (spawn != null)
                return spawn;
        }

        return BuscarSpawnPorMetodoGet(baseSelecionada);
    }

    private MonoBehaviour BuscarSpawnPorMetodoGet(BaseSelecionavel baseSelecionada)
    {
        if (baseSelecionada == null)
            return null;

        MethodInfo metodo = baseSelecionada.GetType().GetMethod(
            "GetSoldadoSpown",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (metodo == null)
            return null;

        object resultado = metodo.Invoke(baseSelecionada, null);
        return resultado as MonoBehaviour;
    }

    private MonoBehaviour BuscarSpawnNaBase(Transform raiz, TipoBasePreferida preferencia)
    {
        if (raiz == null)
            return null;

        MonoBehaviour[] scripts = raiz.GetComponentsInChildren<MonoBehaviour>(true);

        MonoBehaviour primeiroSpawnValido = null;
        MonoBehaviour primeiroSoldado = null;
        MonoBehaviour primeiroVeiculo = null;
        MonoBehaviour primeiroAereo = null;

        for (int i = 0; i < scripts.Length; i++)
        {
            MonoBehaviour script = scripts[i];

            if (script == null)
                continue;

            if (!EhComponenteDeSpawn(script))
                continue;

            if (primeiroSpawnValido == null)
                primeiroSpawnValido = script;

            if (primeiroSoldado == null && EhBaseDeSoldados(script))
                primeiroSoldado = script;

            if (primeiroVeiculo == null && EhBaseDeVeiculos(script))
                primeiroVeiculo = script;

            if (primeiroAereo == null && EhBaseAerea(script))
                primeiroAereo = script;
        }

        if (preferencia == TipoBasePreferida.Soldados && primeiroSoldado != null)
            return primeiroSoldado;

        if (preferencia == TipoBasePreferida.Veiculos && primeiroVeiculo != null)
            return primeiroVeiculo;

        if (preferencia == TipoBasePreferida.Aerea && primeiroAereo != null)
            return primeiroAereo;

        return primeiroSpawnValido;
    }

    private enum TipoBasePreferida
    {
        Nenhuma,
        Soldados,
        Veiculos,
        Aerea
    }

    private bool EhComponenteDeSpawn(MonoBehaviour script)
    {
        if (script == null)
            return false;

        string nomeTipo = script.GetType().Name.ToLowerInvariant();

        if (nomeTipo.Contains("spown") || nomeTipo.Contains("spawn"))
            return true;

        return ExisteMetodo(script, "CriarGuerreiro") ||
               ExisteMetodo(script, "CriarRecurso") ||
               ExisteMetodo(script, "CriarSoldado") ||
               ExisteMetodo(script, "CriarTank") ||
               ExisteMetodo(script, "CriarVeiculo") ||
               ExisteMetodo(script, "CriarAviao") ||
               ExisteMetodo(script, "CriarHelicoptero") ||
               ExisteMetodoComIndice(script, "CriarPorIndice");
    }

    // =====================================================================
    // TIPO DA BASE
    // =====================================================================

    private bool EhBaseDeSoldados(MonoBehaviour spawn)
    {
        if (spawn == null)
            return false;

        return !EhBaseDeVeiculos(spawn) && !EhBaseAerea(spawn);
    }

    private bool EhBaseDeVeiculos(MonoBehaviour spawn)
    {
        if (spawn == null)
            return false;

        string nomeTipo = spawn.GetType().Name.ToLowerInvariant();
        string nomeObjeto = spawn.gameObject.name.ToLowerInvariant();

        if (nomeTipo.Contains("tank") || nomeTipo.Contains("veiculo") || nomeTipo.Contains("vehicle"))
            return true;

        if (nomeObjeto.Contains("tank") || nomeObjeto.Contains("veiculo") || nomeObjeto.Contains("vehicle"))
            return true;

        return ExisteMetodo(spawn, "CriarTank") ||
               ExisteMetodo(spawn, "PodeCriarTank") ||
               ExisteMetodo(spawn, "CriarVeiculo") ||
               ExisteMetodo(spawn, "PodeCriarVeiculo");
    }

    private bool EhBaseAerea(MonoBehaviour spawn)
    {
        if (spawn == null)
            return false;

        string nomeTipo = spawn.GetType().Name.ToLowerInvariant();
        string nomeObjeto = spawn.gameObject.name.ToLowerInvariant();

        if (nomeTipo.Contains("aviao") ||
            nomeTipo.Contains("aereo") ||
            nomeTipo.Contains("aerea") ||
            nomeTipo.Contains("air") ||
            nomeTipo.Contains("helicoptero"))
        {
            return true;
        }

        if (nomeObjeto.Contains("aviao") ||
            nomeObjeto.Contains("aereo") ||
            nomeObjeto.Contains("aerea") ||
            nomeObjeto.Contains("air") ||
            nomeObjeto.Contains("helicoptero"))
        {
            return true;
        }

        return ExisteMetodo(spawn, "CriarAviao") ||
               ExisteMetodo(spawn, "PodeCriarAviao") ||
               ExisteMetodo(spawn, "CriarHelicoptero") ||
               ExisteMetodo(spawn, "PodeCriarHelicoptero") ||
               ExisteMetodo(spawn, "CriarAeronave") ||
               ExisteMetodo(spawn, "PodeCriarAeronave");
    }

    // =====================================================================
    // CHAMADAS PARA O SPAWN DA BASE
    // =====================================================================

    private bool TentarCriar(MonoBehaviour spawn, int indicePrefab, string[] metodosFallback)
    {
        if (spawn == null)
            return false;

        if (!PodeCriar(spawn, indicePrefab, ConverterCriarParaPodeCriar(metodosFallback)))
            return false;

        string[] metodosGenericosComIndice =
        {
            "CriarPorIndice",
            "CriarUnidadePorIndice",
            "CriarPrefabPorIndice",
            "CriarObjetoPorIndice",
            "CriarUnidade",
            "CriarPrefab",
            "Criar"
        };

        for (int i = 0; i < metodosGenericosComIndice.Length; i++)
        {
            if (ChamarMetodoComIndiceSemRetorno(spawn, metodosGenericosComIndice[i], indicePrefab))
                return true;
        }

        if (metodosFallback == null)
            return false;

        for (int i = 0; i < metodosFallback.Length; i++)
        {
            if (ChamarMetodoSemRetorno(spawn, metodosFallback[i]))
                return true;
        }

        return false;
    }

    private string[] ConverterCriarParaPodeCriar(string[] metodosCriar)
    {
        if (metodosCriar == null)
            return null;

        string[] resultado = new string[metodosCriar.Length];

        for (int i = 0; i < metodosCriar.Length; i++)
        {
            string nome = metodosCriar[i];

            if (string.IsNullOrWhiteSpace(nome))
            {
                resultado[i] = nome;
                continue;
            }

            if (nome.StartsWith("Criar", StringComparison.Ordinal))
                resultado[i] = "Pode" + nome;
            else
                resultado[i] = nome;
        }

        return resultado;
    }

    private bool PodeCriar(MonoBehaviour spawn, int indicePrefab, string[] metodosFallback)
    {
        if (spawn == null)
            return false;

        string[] metodosGenericosComIndice =
        {
            "PodeCriarPorIndice",
            "PodeCriarUnidadePorIndice",
            "PodeCriarPrefabPorIndice",
            "PodeCriarObjetoPorIndice",
            "PodeCriarUnidade",
            "PodeCriarPrefab",
            "PodeCriar"
        };

        for (int i = 0; i < metodosGenericosComIndice.Length; i++)
        {
            if (ChamarMetodoBoolComIndice(spawn, metodosGenericosComIndice[i], indicePrefab, out bool resultado))
                return resultado;
        }

        if (metodosFallback != null)
        {
            for (int i = 0; i < metodosFallback.Length; i++)
            {
                if (ChamarMetodoBoolSemParametro(spawn, metodosFallback[i], out bool resultadoManual))
                    return resultadoManual;
            }
        }

        return ExistePrefabNaBase(spawn, indicePrefab);
    }

    private bool ExisteOpcao(MonoBehaviour spawn, int indicePrefab, string[] metodosFallback)
    {
        if (spawn == null)
            return false;

        if (ExisteMetodoComIndice(spawn, "CriarPorIndice") ||
            ExisteMetodoComIndice(spawn, "CriarUnidadePorIndice") ||
            ExisteMetodoComIndice(spawn, "CriarPrefabPorIndice") ||
            ExisteMetodoComIndice(spawn, "CriarObjetoPorIndice"))
        {
            return ExistePrefabNaBase(spawn, indicePrefab);
        }

        if (metodosFallback == null)
            return false;

        for (int i = 0; i < metodosFallback.Length; i++)
        {
            if (ExisteMetodo(spawn, metodosFallback[i]))
                return true;
        }

        return false;
    }

    private bool ExistePrefabNaBase(MonoBehaviour spawn, int indicePrefab)
    {
        if (spawn == null || indicePrefab < 0)
            return false;

        string[] metodosExisteComIndice =
        {
            "ExistePrefabPorIndice",
            "TemPrefabPorIndice",
            "ExisteUnidadePorIndice",
            "TemUnidadePorIndice",
            "IndiceExiste",
            "ExisteIndice"
        };

        for (int i = 0; i < metodosExisteComIndice.Length; i++)
        {
            if (ChamarMetodoBoolComIndice(spawn, metodosExisteComIndice[i], indicePrefab, out bool existe))
                return existe;
        }

        string[] metodosQuantidade =
        {
            "GetQuantidadePrefabs",
            "QuantidadePrefabs",
            "GetQuantidadeUnidades",
            "QuantidadeUnidades",
            "GetTotalPrefabs",
            "TotalPrefabs"
        };

        for (int i = 0; i < metodosQuantidade.Length; i++)
        {
            if (ChamarMetodoIntSemParametro(spawn, metodosQuantidade[i], out int quantidade))
                return indicePrefab < quantidade;
        }

        return true;
    }

    // =====================================================================
    // REFLECTION SEGURA
    // =====================================================================

    private bool ExisteMetodo(MonoBehaviour spawn, string nomeMetodo)
    {
        return BuscarMetodo(spawn, nomeMetodo, 0) != null;
    }

    private bool ExisteMetodoComIndice(MonoBehaviour spawn, string nomeMetodo)
    {
        MethodInfo metodo = BuscarMetodo(spawn, nomeMetodo, 1);

        if (metodo == null)
            return false;

        ParameterInfo[] parametros = metodo.GetParameters();
        return parametros.Length == 1 && parametros[0].ParameterType == typeof(int);
    }

    private bool ChamarMetodoSemRetorno(MonoBehaviour spawn, string nomeMetodo)
    {
        MethodInfo metodo = BuscarMetodo(spawn, nomeMetodo, 0);

        if (metodo == null)
            return false;

        metodo.Invoke(spawn, null);
        return true;
    }

    private bool ChamarMetodoComIndiceSemRetorno(MonoBehaviour spawn, string nomeMetodo, int indice)
    {
        MethodInfo metodo = BuscarMetodo(spawn, nomeMetodo, 1);

        if (metodo == null)
            return false;

        ParameterInfo[] parametros = metodo.GetParameters();

        if (parametros.Length != 1 || parametros[0].ParameterType != typeof(int))
            return false;

        metodo.Invoke(spawn, new object[] { indice });
        return true;
    }

    private bool ChamarMetodoBoolSemParametro(MonoBehaviour spawn, string nomeMetodo, out bool resultado)
    {
        resultado = false;

        MethodInfo metodo = BuscarMetodo(spawn, nomeMetodo, 0);

        if (metodo == null || metodo.ReturnType != typeof(bool))
            return false;

        resultado = (bool)metodo.Invoke(spawn, null);
        return true;
    }

    private bool ChamarMetodoBoolComIndice(MonoBehaviour spawn, string nomeMetodo, int indice, out bool resultado)
    {
        resultado = false;

        MethodInfo metodo = BuscarMetodo(spawn, nomeMetodo, 1);

        if (metodo == null || metodo.ReturnType != typeof(bool))
            return false;

        ParameterInfo[] parametros = metodo.GetParameters();

        if (parametros.Length != 1 || parametros[0].ParameterType != typeof(int))
            return false;

        resultado = (bool)metodo.Invoke(spawn, new object[] { indice });
        return true;
    }

    private bool ChamarMetodoIntSemParametro(MonoBehaviour spawn, string nomeMetodo, out int resultado)
    {
        resultado = 0;

        MethodInfo metodo = BuscarMetodo(spawn, nomeMetodo, 0);

        if (metodo == null || metodo.ReturnType != typeof(int))
            return false;

        resultado = (int)metodo.Invoke(spawn, null);
        return true;
    }

    private MethodInfo BuscarMetodo(MonoBehaviour spawn, string nomeMetodo, int quantidadeParametros)
    {
        if (spawn == null || string.IsNullOrWhiteSpace(nomeMetodo))
            return null;

        MethodInfo metodo = spawn.GetType().GetMethod(
            nomeMetodo,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (metodo == null)
            return null;

        ParameterInfo[] parametros = metodo.GetParameters();

        if (parametros.Length != quantidadeParametros)
            return null;

        return metodo;
    }

    private void OnValidate()
    {
        indiceGuerreiroNaBase = Mathf.Max(0, indiceGuerreiroNaBase);
        indiceRecursoNaBase = Mathf.Max(0, indiceRecursoNaBase);
        indiceSoldadoNaBase = Mathf.Max(0, indiceSoldadoNaBase);
        indiceTankNaBase = Mathf.Max(0, indiceTankNaBase);
        indiceAviaoNaBase = Mathf.Max(0, indiceAviaoNaBase);
        indiceHelicopteroNaBase = Mathf.Max(0, indiceHelicopteroNaBase);
        intervaloAtualizacao = Mathf.Max(0.02f, intervaloAtualizacao);
    }
}
