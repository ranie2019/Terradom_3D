using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class BaseLimite : MonoBehaviour
{
    // =========================================================
    // SINGLETON - GERENCIADOR CENTRAL
    // =========================================================
    private static readonly Dictionary<string, List<BaseLimite>> basesPorTag = new Dictionary<string, List<BaseLimite>>();
    private static readonly Dictionary<string, Mesh> bordasUnificadasPorTag = new Dictionary<string, Mesh>();
    private static readonly HashSet<string> tagsComBordaDesatualizada = new HashSet<string>();
    // Primeira base registrada por tag (define a cor da borda para toda a equipe)
    private static readonly Dictionary<string, BaseLimite> primeiraBasePorTag = new Dictionary<string, BaseLimite>();

    [Header("Configuração da Área de Construção")]
    [SerializeField] private float raioArea = 15f;
    [SerializeField] private string tagBase = "Vermelho";

    [Header("Efeito Visual")]
    [SerializeField] private bool mostrarArea = true;
    [SerializeField] private Color corBorda = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    [SerializeField] private float velocidadePiscar = 2f;
    [SerializeField] private float alturaVisualizacao = 0.05f;
    [SerializeField] private int segmentosCirculo = 64;

    [Header("Referência ao Terreno")]
    [SerializeField] private Terrain terrain;

    // Material para o efeito visual
    private Material materialBorda;

    // Cache
    private float alphaAtual = 1f;
    private float timerPiscar;
    private Vector3 posicaoBase;
    private bool mostrarAreaAnterior;

    // Flag para saber se foi desregistrado temporariamente
    private bool desregistradoTemporariamente = false;

    private struct IntervaloAngular
    {
        public float inicio;
        public float fim;

        public IntervaloAngular(float inicio, float fim)
        {
            this.inicio = inicio;
            this.fim = fim;
        }
    }

    private const float CirculoCompleto = Mathf.PI * 2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstadoEstatico()
    {
        foreach (Mesh mesh in bordasUnificadasPorTag.Values)
            DestruirObjetoUnity(mesh);

        basesPorTag.Clear();
        bordasUnificadasPorTag.Clear();
        tagsComBordaDesatualizada.Clear();
        primeiraBasePorTag.Clear();
    }

    private static void DestruirObjetoUnity(Object objeto)
    {
        if (objeto == null)
            return;

        if (Application.isPlaying)
            Destroy(objeto);
        else
            DestroyImmediate(objeto);
    }

    private void Awake()
    {
        // Procura o Terrain se não foi atribuído
        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();

        // Usa a tag do GameObject se tagBase estiver vazia
        if (string.IsNullOrEmpty(tagBase))
            tagBase = gameObject.tag;

        // Aviso: prefab de uma equipe com tagBase de outra (o padrão do campo é "Vermelho").
        string tagObjeto = gameObject.tag;
        if ((tagObjeto == "Azul" || tagObjeto == "Vermelho" || tagObjeto == "Verde") && tagObjeto != tagBase)
            Debug.LogWarning($"[BaseLimite] '{name}' tem a tag '{tagObjeto}' mas tagBase='{tagBase}'. " +
                             "A área será registrada para a equipe da tagBase.", this);

        // Registra esta base no dicionário global
        if (!desregistradoTemporariamente)
            RegistrarBase();

        // Cria o material da borda para visualização
        CriarMateriais();

        timerPiscar = 0f;
        posicaoBase = transform.position;
        mostrarAreaAnterior = mostrarArea;
    }

    private void OnEnable()
    {
        InvalidarBordaUnificada(tagBase);

        // Quando reativado (após posicionamento), registra novamente
        desregistradoTemporariamente = false;
        RegistrarBase();
    }

    private void OnDisable()
    {
        InvalidarBordaUnificada(tagBase);
    }

    private void OnDestroy()
    {
        // Remove esta base do dicionário global
        RemoverBase();

        // Limpa material
        if (materialBorda != null)
            Destroy(materialBorda);
        InvalidarBordaUnificada(tagBase);
    }

    private void Update()
    {
        // Atualiza posição (a base pode ter sido movida)
        Vector3 posicaoAtual = transform.position;
        if (posicaoAtual != posicaoBase)
        {
            posicaoBase = posicaoAtual;
            InvalidarBordaUnificada(tagBase);
        }

        if (mostrarArea != mostrarAreaAnterior)
        {
            mostrarAreaAnterior = mostrarArea;
            InvalidarBordaUnificada(tagBase);
        }

        if (!mostrarArea)
            return;

        // Atualiza o efeito de pulsar
        timerPiscar += Time.deltaTime * velocidadePiscar;
        alphaAtual = (Mathf.Sin(timerPiscar) + 1f) * 0.5f; // Oscila entre 0 e 1
    }

    // =========================================================
    // REGISTRO GLOBAL
    // =========================================================

    /// <summary>
    /// Remove temporariamente esta base do dicionário global.
    /// Usado durante o posicionamento (ghost) para que a base
    /// não se conte como área válida antes de ser confirmada.
    /// </summary>
    public void DesregistrarTemporariamente()
    {
        desregistradoTemporariamente = true;
        RemoverBase();
    }

    private void RegistrarBase()
    {
        if (string.IsNullOrEmpty(tagBase))
        {
            Debug.LogError("[BaseLimite] ❌ Tag da base não pode ser vazia!");
            return;
        }

        if (!basesPorTag.ContainsKey(tagBase))
            basesPorTag[tagBase] = new List<BaseLimite>();

        if (!basesPorTag[tagBase].Contains(this))
            basesPorTag[tagBase].Add(this);

        // Registra a primeira base (menor InstanceID) como referência de cor
        if (!primeiraBasePorTag.ContainsKey(tagBase) ||
            primeiraBasePorTag[tagBase] == null ||
            GetInstanceID() < primeiraBasePorTag[tagBase].GetInstanceID())
        {
            primeiraBasePorTag[tagBase] = this;
        }

        InvalidarBordaUnificada(tagBase);
    }

    private void RemoverBase()
    {
        if (string.IsNullOrEmpty(tagBase))
            return;

        if (basesPorTag.ContainsKey(tagBase))
        {
            basesPorTag[tagBase].Remove(this);
            if (basesPorTag[tagBase].Count == 0)
            {
                basesPorTag.Remove(tagBase);
                primeiraBasePorTag.Remove(tagBase);
            }
            else if (primeiraBasePorTag.TryGetValue(tagBase, out BaseLimite primeira) && primeira == this)
            {
                // Recalcula a primeira base ao remover a que era referência
                primeiraBasePorTag.Remove(tagBase);
                BaseLimite novaPrimeira = null;
                int menorId = int.MaxValue;
                foreach (BaseLimite b in basesPorTag[tagBase])
                {
                    if (b == null) continue;
                    int id = b.GetInstanceID();
                    if (id < menorId) { menorId = id; novaPrimeira = b; }
                }
                if (novaPrimeira != null)
                    primeiraBasePorTag[tagBase] = novaPrimeira;
            }
        }

        InvalidarBordaUnificada(tagBase);
    }

    // =========================================================
    // VERIFICAÇÃO DE CONSTRUÇÃO (MÉTODOS ESTÁTICOS)
    // =========================================================

    /// <summary>
    /// Verifica se uma posição está dentro da área de construção de alguma base da tag especificada
    /// </summary>
    public static bool PosicaoDentroDeArea(string tag, Vector3 posicao)
    {
        if (!basesPorTag.ContainsKey(tag))
            return false;

        foreach (BaseLimite baseLimite in basesPorTag[tag])
        {
            if (baseLimite == null)
                continue;

            float distancia = Vector3.Distance(
                new Vector3(posicao.x, 0, posicao.z),
                new Vector3(baseLimite.transform.position.x, 0, baseLimite.transform.position.z)
            );

            if (distancia <= baseLimite.raioArea)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Verifica se uma posição está dentro da área de construção desta base específica
    /// </summary>
    public bool PosicaoDentroDaMinhaArea(Vector3 posicao)
    {
        float distancia = Vector3.Distance(
            new Vector3(posicao.x, 0, posicao.z),
            new Vector3(transform.position.x, 0, transform.position.z)
        );

        return distancia <= raioArea;
    }

    /// <summary>
    /// Verifica se pode construir na posição (dentro da área E longe o suficiente de bases inimigas)
    /// </summary>
    public static bool PodeConstruir(string tag, Vector3 posicao, float distanciaMinimaEntreBases = 30f)
    {
        // Verifica se está dentro da área de alguma base aliada
        if (!PosicaoDentroDeArea(tag, posicao))
            return false;

        // Verifica se está longe o suficiente de bases inimigas
        foreach (var kvp in basesPorTag)
        {
            if (kvp.Key == tag)
                continue; // Pula bases da mesma tag (aliadas)

            foreach (BaseLimite baseInimiga in kvp.Value)
            {
                if (baseInimiga == null)
                    continue;

                float distancia = Vector3.Distance(
                    new Vector3(posicao.x, 0, posicao.z),
                    new Vector3(baseInimiga.transform.position.x, 0, baseInimiga.transform.position.z)
                );

                if (distancia < distanciaMinimaEntreBases)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Verifica se existe pelo menos uma base da tag (para construção inicial)
    /// </summary>
    public static bool TemAreaDisponivel(string tag)
    {
        return basesPorTag.ContainsKey(tag) && basesPorTag[tag].Count > 0;
    }

    /// <summary>
    /// Retorna a base mais próxima da tag especificada a partir de uma posição
    /// </summary>
    public static BaseLimite ObterBaseMaisProxima(string tag, Vector3 posicao)
    {
        if (!basesPorTag.ContainsKey(tag))
            return null;

        BaseLimite maisProxima = null;
        float menorDistancia = float.MaxValue;

        foreach (BaseLimite baseLimite in basesPorTag[tag])
        {
            if (baseLimite == null)
                continue;

            float distancia = Vector3.Distance(posicao, baseLimite.transform.position);

            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                maisProxima = baseLimite;
            }
        }

        return maisProxima;
    }

    // =========================================================
    // VISUALIZAÇÃO (CORRIGIDA - CÍRCULO DEITADO NO CHÃO)
    // =========================================================

    private void CriarMateriais()
    {
        // Material da borda (transparente)
        Shader shaderTransparente = Shader.Find("Sprites/Default");
        if (shaderTransparente == null)
            shaderTransparente = Shader.Find("Unlit/Color");
        if (shaderTransparente == null)
            shaderTransparente = Shader.Find("Hidden/Internal-Colored");

        // Em builds (ex.: WebGL) o shader pode ter sido removido: sem ele o
        // 'new Material' lançaria exceção e quebraria o Awake da base.
        if (shaderTransparente == null)
        {
            Debug.LogError("[BaseLimite] Nenhum shader encontrado para desenhar a borda. " +
                           "Adicione 'Sprites/Default' em Project Settings > Graphics > Always Included Shaders.", this);
            mostrarArea = false;
            return;
        }

        materialBorda = new Material(shaderTransparente);
        materialBorda.color = corBorda;
    }

    private void OnRenderObject()
    {
        if (!mostrarArea || materialBorda == null)
            return;

        // Somente a base responsável desenha por toda a equipe
        if (this != ObterBaseResponsavelPelaBorda(tagBase))
            return;

        // --- BORDA UNIFICADA ---
        Mesh meshBordaUnificada = ObterMeshBordaUnificada(tagBase);
        if (meshBordaUnificada == null)
            return;

        // Usa a cor da PRIMEIRA base registrada para esta tag
        Color corBordaBase = corBorda;
        if (primeiraBasePorTag.TryGetValue(tagBase, out BaseLimite primeira) && primeira != null)
            corBordaBase = primeira.corBorda;

        Color corBordaAtual = corBordaBase;
        corBordaAtual.a = corBordaBase.a * (0.5f + alphaAtual * 0.5f);
        materialBorda.color = corBordaAtual;
        materialBorda.SetPass(0);
        Graphics.DrawMeshNow(meshBordaUnificada, Matrix4x4.identity);
    }

    private static void InvalidarBordaUnificada(string tag)
    {
        if (!string.IsNullOrEmpty(tag))
            tagsComBordaDesatualizada.Add(tag);
    }

    public static List<BaseLimite> ObterBasesDaTag(string tag)
    {
        List<BaseLimite> resultado = new List<BaseLimite>();
        if (string.IsNullOrEmpty(tag))
            return resultado;

        if (Application.isPlaying)
        {
            if (!basesPorTag.TryGetValue(tag, out List<BaseLimite> registradas))
                return resultado;

            for (int i = registradas.Count - 1; i >= 0; i--)
            {
                BaseLimite baseLimite = registradas[i];
                if (baseLimite == null)
                {
                    registradas.RemoveAt(i);
                    InvalidarBordaUnificada(tag);
                    continue;
                }

                if (baseLimite.mostrarArea && baseLimite.gameObject.activeInHierarchy)
                    resultado.Add(baseLimite);
            }
        }
        else
        {
            BaseLimite[] encontradas = FindObjectsByType<BaseLimite>(FindObjectsSortMode.None);
            for (int i = 0; i < encontradas.Length; i++)
            {
                BaseLimite baseLimite = encontradas[i];
                if (baseLimite != null && baseLimite.tagBase == tag &&
                    baseLimite.mostrarArea && baseLimite.gameObject.activeInHierarchy)
                    resultado.Add(baseLimite);
            }
        }

        return resultado;
    }

    private static BaseLimite ObterBaseResponsavelPelaBorda(string tag)
    {
        BaseLimite responsavel = null;
        int menorId = int.MaxValue;

        if (Application.isPlaying)
        {
            if (!basesPorTag.TryGetValue(tag, out List<BaseLimite> basesRegistradas))
                return null;

            for (int i = 0; i < basesRegistradas.Count; i++)
            {
                BaseLimite candidata = basesRegistradas[i];
                if (candidata == null || !candidata.isActiveAndEnabled ||
                    !candidata.mostrarArea || !candidata.gameObject.activeInHierarchy)
                    continue;

                int id = candidata.GetInstanceID();
                if (id < menorId)
                {
                    menorId = id;
                    responsavel = candidata;
                }
            }

            return responsavel;
        }

        List<BaseLimite> bases = ObterBasesDaTag(tag);
        for (int i = 0; i < bases.Count; i++)
        {
            int id = bases[i].GetInstanceID();
            if (id < menorId)
            {
                menorId = id;
                responsavel = bases[i];
            }
        }
        return responsavel;
    }

    private static Mesh ObterMeshBordaUnificada(string tag)
    {
        bool precisaReconstruir = tagsComBordaDesatualizada.Contains(tag) ||
                                  !bordasUnificadasPorTag.ContainsKey(tag);
        if (!precisaReconstruir)
            return bordasUnificadasPorTag[tag];

        if (bordasUnificadasPorTag.TryGetValue(tag, out Mesh antiga))
        {
            DestruirObjetoUnity(antiga);
            bordasUnificadasPorTag.Remove(tag);
        }

        tagsComBordaDesatualizada.Remove(tag);
        List<BaseLimite> bases = ObterBasesDaTag(tag);
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangulos = new List<int>();

        for (int i = 0; i < bases.Count; i++)
            AdicionarBordaVisivel(bases, bases[i], vertices, triangulos);

        if (vertices.Count == 0)
        {
            bordasUnificadasPorTag[tag] = null;
            return null;
        }

        Mesh mesh = new Mesh { name = "ContornoUnificadoBases_" + tag };
        if (vertices.Count > 65535)
            mesh.indexFormat = IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        bordasUnificadasPorTag[tag] = mesh;
        return mesh;
    }

    private static void AdicionarBordaVisivel(
        List<BaseLimite> bases,
        BaseLimite baseLimite,
        List<Vector3> vertices,
        List<int> triangulos)
    {
        if (baseLimite == null || baseLimite.raioArea <= 0f)
            return;

        List<IntervaloAngular> arcos = CalcularArcosVisiveis(bases, baseLimite);
        int segmentosTotais = Mathf.Max(16, baseLimite.segmentosCirculo);
        Terrain terreno = ObterTerrenoLimite(baseLimite);
        List<Vector3> bufferA = new List<Vector3>(8);
        List<Vector3> bufferB = new List<Vector3>(8);

        for (int a = 0; a < arcos.Count; a++)
        {
            IntervaloAngular arco = arcos[a];
            int passos = Mathf.Max(1, Mathf.CeilToInt(
                (arco.fim - arco.inicio) / CirculoCompleto * segmentosTotais));

            for (int passo = 0; passo < passos; passo++)
            {
                float tAtual = (float)passo / passos;
                float tProximo = (float)(passo + 1) / passos;
                float anguloAtual = Mathf.Lerp(arco.inicio, arco.fim, tAtual);
                float anguloProximo = Mathf.Lerp(arco.inicio, arco.fim, tProximo);

                ObterPontosDoAnel(baseLimite, anguloAtual, out Vector3 externoAtual, out Vector3 internoAtual);
                ObterPontosDoAnel(baseLimite, anguloProximo, out Vector3 externoProximo, out Vector3 internoProximo);

                AdicionarTrianguloRecortado(externoAtual, internoAtual, externoProximo,
                    terreno, bufferA, bufferB, vertices, triangulos);
                AdicionarTrianguloRecortado(internoAtual, internoProximo, externoProximo,
                    terreno, bufferA, bufferB, vertices, triangulos);
            }
        }
    }

    private static Terrain ObterTerrenoLimite(BaseLimite baseLimite)
    {
        if (baseLimite != null && baseLimite.terrain != null && baseLimite.terrain.terrainData != null)
            return baseLimite.terrain;

        Terrain encontrado = FindFirstObjectByType<Terrain>();
        return encontrado != null && encontrado.terrainData != null ? encontrado : null;
    }

    private static void AdicionarTrianguloRecortado(
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Terrain terreno,
        List<Vector3> bufferA,
        List<Vector3> bufferB,
        List<Vector3> vertices,
        List<int> triangulos)
    {
        if (terreno == null || terreno.terrainData == null)
        {
            AdicionarTriangulo(a, b, c, vertices, triangulos);
            return;
        }

        bufferA.Clear();
        bufferA.Add(a);
        bufferA.Add(b);
        bufferA.Add(c);

        List<Vector3> entrada = bufferA;
        List<Vector3> saida = bufferB;
        Vector3 tamanho = terreno.terrainData.size;

        RecortarPoligonoNoEixo(entrada, saida, terreno, eixoX: true, limite: 0f, manterMaior: true);
        TrocarBuffers(ref entrada, ref saida);
        RecortarPoligonoNoEixo(entrada, saida, terreno, eixoX: true, limite: tamanho.x, manterMaior: false);
        TrocarBuffers(ref entrada, ref saida);
        RecortarPoligonoNoEixo(entrada, saida, terreno, eixoX: false, limite: 0f, manterMaior: true);
        TrocarBuffers(ref entrada, ref saida);
        RecortarPoligonoNoEixo(entrada, saida, terreno, eixoX: false, limite: tamanho.z, manterMaior: false);

        if (saida.Count < 3)
            return;

        int indiceInicial = vertices.Count;
        vertices.AddRange(saida);
        for (int i = 1; i < saida.Count - 1; i++)
        {
            triangulos.Add(indiceInicial);
            triangulos.Add(indiceInicial + i);
            triangulos.Add(indiceInicial + i + 1);
        }
    }

    private static void TrocarBuffers(ref List<Vector3> a, ref List<Vector3> b)
    {
        List<Vector3> temporario = a;
        a = b;
        b = temporario;
    }

    private static void RecortarPoligonoNoEixo(
        List<Vector3> entrada,
        List<Vector3> saida,
        Terrain terreno,
        bool eixoX,
        float limite,
        bool manterMaior)
    {
        saida.Clear();
        if (entrada.Count == 0)
            return;

        Vector3 anterior = entrada[entrada.Count - 1];
        float coordenadaAnterior = ObterCoordenadaLocal(terreno, anterior, eixoX);
        bool anteriorDentro = manterMaior
            ? coordenadaAnterior >= limite - 0.0001f
            : coordenadaAnterior <= limite + 0.0001f;

        for (int i = 0; i < entrada.Count; i++)
        {
            Vector3 atual = entrada[i];
            float coordenadaAtual = ObterCoordenadaLocal(terreno, atual, eixoX);
            bool atualDentro = manterMaior
                ? coordenadaAtual >= limite - 0.0001f
                : coordenadaAtual <= limite + 0.0001f;

            if (atualDentro != anteriorDentro)
            {
                float denominador = coordenadaAtual - coordenadaAnterior;
                if (Mathf.Abs(denominador) > 0.000001f)
                {
                    float t = Mathf.Clamp01((limite - coordenadaAnterior) / denominador);
                    saida.Add(Vector3.Lerp(anterior, atual, t));
                }
            }

            if (atualDentro)
                saida.Add(atual);

            anterior = atual;
            coordenadaAnterior = coordenadaAtual;
            anteriorDentro = atualDentro;
        }
    }

    private static float ObterCoordenadaLocal(Terrain terreno, Vector3 ponto, bool eixoX)
    {
        Vector3 local = terreno.transform.InverseTransformPoint(ponto);
        return eixoX ? local.x : local.z;
    }

    private static void AdicionarTriangulo(Vector3 a, Vector3 b, Vector3 c, List<Vector3> vertices, List<int> triangulos)
    {
        int inicio = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        triangulos.Add(inicio);
        triangulos.Add(inicio + 1);
        triangulos.Add(inicio + 2);
    }

    private static void ObterPontosDoAnel(BaseLimite baseLimite, float angulo, out Vector3 pontoExterno, out Vector3 pontoInterno)
    {
        Vector3 centro = baseLimite.transform.position;
        float cos = Mathf.Cos(angulo);
        float sin = Mathf.Sin(angulo);
        pontoExterno = new Vector3(
            centro.x + cos * baseLimite.raioArea,
            centro.y,
            centro.z + sin * baseLimite.raioArea);
        pontoInterno = new Vector3(
            centro.x + cos * Mathf.Max(0f, baseLimite.raioArea - 0.5f),
            centro.y,
            centro.z + sin * Mathf.Max(0f, baseLimite.raioArea - 0.5f));

        pontoExterno.y = baseLimite.ObterAlturaVisual(pontoExterno);
        pontoInterno.y = baseLimite.ObterAlturaVisual(pontoInterno);
    }

    private float ObterAlturaVisual(Vector3 ponto)
    {
        if (terrain != null)
            return terrain.SampleHeight(ponto) + terrain.transform.position.y + alturaVisualizacao;

        return transform.position.y + alturaVisualizacao;
    }

    private static List<IntervaloAngular> CalcularArcosVisiveis(List<BaseLimite> bases, BaseLimite baseLimite)
    {
        List<IntervaloAngular> cobertos = new List<IntervaloAngular>();
        float raio = baseLimite.raioArea;
        Vector3 centro = baseLimite.transform.position;
        bool totalmenteCoberto = false;

        for (int i = 0; i < bases.Count; i++)
        {
            BaseLimite outra = bases[i];
            if (outra == null || outra == baseLimite || outra.raioArea <= 0f)
                continue;

            Vector3 centroOutra = outra.transform.position;
            float dx = centroOutra.x - centro.x;
            float dz = centroOutra.z - centro.z;
            float distancia = Mathf.Sqrt(dx * dx + dz * dz);
            float outroRaio = outra.raioArea;
            const float tolerancia = 0.001f;

            if (distancia <= tolerancia)
            {
                bool mesmoRaio = Mathf.Abs(outroRaio - raio) <= tolerancia;
                if (outroRaio > raio + tolerancia ||
                    (mesmoRaio && outra.GetInstanceID() < baseLimite.GetInstanceID()))
                {
                    totalmenteCoberto = true;
                    break;
                }
                continue;
            }

            // Uma base maior que contém esta área cobre todo o seu contorno.
            if (outroRaio >= raio && distancia + raio <= outroRaio + tolerancia)
            {
                totalmenteCoberto = true;
                break;
            }

            // Se esta base contém a outra, a menor não esconde seu contorno externo.
            if (raio >= outroRaio && distancia + outroRaio <= raio + tolerancia)
                continue;

            // Círculos separados ou apenas tangentes não escondem arcos.
            if (distancia >= raio + outroRaio - tolerancia)
                continue;

            float cosMeioAngulo = (distancia * distancia + raio * raio - outroRaio * outroRaio) /
                                  (2f * distancia * raio);
            float meioAngulo = Mathf.Acos(Mathf.Clamp(cosMeioAngulo, -1f, 1f));
            float anguloCentro = Mathf.Atan2(dz, dx);
            AdicionarIntervaloCoberto(cobertos, anguloCentro - meioAngulo, anguloCentro + meioAngulo);
        }

        if (totalmenteCoberto)
            return new List<IntervaloAngular>();

        if (cobertos.Count == 0)
            return new List<IntervaloAngular> { new IntervaloAngular(0f, CirculoCompleto) };

        cobertos.Sort((a, b) => a.inicio.CompareTo(b.inicio));
        List<IntervaloAngular> unidos = new List<IntervaloAngular>();
        IntervaloAngular atual = cobertos[0];
        for (int i = 1; i < cobertos.Count; i++)
        {
            IntervaloAngular proximo = cobertos[i];
            if (proximo.inicio <= atual.fim + 0.0001f)
            {
                atual.fim = Mathf.Max(atual.fim, proximo.fim);
            }
            else
            {
                unidos.Add(atual);
                atual = proximo;
            }
        }
        unidos.Add(atual);

        List<IntervaloAngular> visiveis = new List<IntervaloAngular>();
        float cursor = 0f;
        for (int i = 0; i < unidos.Count; i++)
        {
            if (unidos[i].inicio > cursor + 0.0001f)
                visiveis.Add(new IntervaloAngular(cursor, unidos[i].inicio));
            cursor = Mathf.Max(cursor, unidos[i].fim);
        }

        if (cursor < CirculoCompleto - 0.0001f)
            visiveis.Add(new IntervaloAngular(cursor, CirculoCompleto));

        return visiveis;
    }

    private static void AdicionarIntervaloCoberto(List<IntervaloAngular> intervalos, float inicio, float fim)
    {
        float extensao = fim - inicio;
        float inicioNormalizado = inicio % CirculoCompleto;
        if (inicioNormalizado < 0f)
            inicioNormalizado += CirculoCompleto;

        float fimNormalizado = inicioNormalizado + extensao;
        if (fimNormalizado <= CirculoCompleto)
        {
            intervalos.Add(new IntervaloAngular(inicioNormalizado, fimNormalizado));
        }
        else
        {
            intervalos.Add(new IntervaloAngular(inicioNormalizado, CirculoCompleto));
            intervalos.Add(new IntervaloAngular(0f, fimNormalizado - CirculoCompleto));
        }
    }

    private void OnDrawGizmos()
    {
        if (!mostrarArea || Application.isPlaying)
            return;

        List<BaseLimite> bases = ObterBasesDaTag(tagBase);
        BaseLimite responsavel = null;
        int menorId = int.MaxValue;
        for (int i = 0; i < bases.Count; i++)
        {
            int id = bases[i].GetInstanceID();
            if (id < menorId)
            {
                menorId = id;
                responsavel = bases[i];
            }
        }

        if (responsavel != this)
            return;

        Gizmos.color = corBorda;
        for (int i = 0; i < bases.Count; i++)
        {
            BaseLimite baseLimite = bases[i];
            List<IntervaloAngular> arcos = CalcularArcosVisiveis(bases, baseLimite);
            for (int a = 0; a < arcos.Count; a++)
                DesenharArcoGizmo(baseLimite, arcos[a]);
        }
    }

    private static void DesenharArcoGizmo(BaseLimite baseLimite, IntervaloAngular arco)
    {
        int passos = Mathf.Max(1, Mathf.CeilToInt(
            (arco.fim - arco.inicio) / CirculoCompleto * Mathf.Max(16, baseLimite.segmentosCirculo)));
        Terrain terreno = ObterTerrenoLimite(baseLimite);
        Vector3 pontoAnterior = ObterPontoNaBorda(baseLimite, arco.inicio);

        for (int i = 1; i <= passos; i++)
        {
            float angulo = Mathf.Lerp(arco.inicio, arco.fim, (float)i / passos);
            Vector3 pontoAtual = ObterPontoNaBorda(baseLimite, angulo);
            if (RecortarSegmentoAoTerreno(terreno, pontoAnterior, pontoAtual,
                    out Vector3 inicioRecortado, out Vector3 fimRecortado))
                Gizmos.DrawLine(inicioRecortado, fimRecortado);
            pontoAnterior = pontoAtual;
        }
    }

    private static bool RecortarSegmentoAoTerreno(
        Terrain terreno,
        Vector3 inicio,
        Vector3 fim,
        out Vector3 inicioRecortado,
        out Vector3 fimRecortado)
    {
        inicioRecortado = inicio;
        fimRecortado = fim;
        if (terreno == null || terreno.terrainData == null)
            return true;

        Vector3 localInicio = terreno.transform.InverseTransformPoint(inicio);
        Vector3 localFim = terreno.transform.InverseTransformPoint(fim);
        float deltaX = localFim.x - localInicio.x;
        float deltaZ = localFim.z - localInicio.z;
        float tInicio = 0f;
        float tFim = 1f;
        Vector3 tamanho = terreno.terrainData.size;

        if (!RecortarEixo(-deltaX, localInicio.x, ref tInicio, ref tFim) ||
            !RecortarEixo(deltaX, tamanho.x - localInicio.x, ref tInicio, ref tFim) ||
            !RecortarEixo(-deltaZ, localInicio.z, ref tInicio, ref tFim) ||
            !RecortarEixo(deltaZ, tamanho.z - localInicio.z, ref tInicio, ref tFim) ||
            tInicio > tFim)
            return false;

        inicioRecortado = Vector3.Lerp(inicio, fim, tInicio);
        fimRecortado = Vector3.Lerp(inicio, fim, tFim);
        return true;
    }

    private static bool RecortarEixo(float p, float q, ref float tInicio, ref float tFim)
    {
        if (Mathf.Abs(p) <= 0.000001f)
            return q >= -0.0001f;

        float r = q / p;
        if (p < 0f)
        {
            if (r > tFim)
                return false;
            if (r > tInicio)
                tInicio = r;
        }
        else
        {
            if (r < tInicio)
                return false;
            if (r < tFim)
                tFim = r;
        }

        return true;
    }

    private static Vector3 ObterPontoNaBorda(BaseLimite baseLimite, float angulo)
    {
        Vector3 centro = baseLimite.transform.position;
        Vector3 ponto = new Vector3(
            centro.x + Mathf.Cos(angulo) * baseLimite.raioArea,
            centro.y,
            centro.z + Mathf.Sin(angulo) * baseLimite.raioArea);
        ponto.y = baseLimite.ObterAlturaVisual(ponto);
        return ponto;
    }

    // =========================================================
    // CONFIGURAÇÕES PÚBLICAS
    // =========================================================

    public float GetRaioArea() => raioArea;
    public string GetTagBase() => tagBase;

    /// <summary>
    /// Atualiza o raio da área (útil para upgrades de base)
    /// </summary>
    public void SetRaioArea(float novoRaio)
    {
        raioArea = Mathf.Max(5f, novoRaio);

        // Atualiza o contorno da equipe.
        InvalidarBordaUnificada(tagBase);
    }

    /// <summary>
    /// Configura a tag da base (usado quando é adicionado via código)
    /// </summary>
    public void SetTagBase(string novaTag)
    {
        if (string.IsNullOrEmpty(novaTag))
            return;

        // Remove do registro antigo
        RemoverBase();

        // Atualiza tag
        tagBase = novaTag;

        // Registra com a nova tag (se não estiver desregistrado temporariamente)
        if (!desregistradoTemporariamente)
            RegistrarBase();
        else
            InvalidarBordaUnificada(novaTag);
    }

    private void OnValidate()
    {
        InvalidarBordaUnificada(tagBase);
        raioArea = Mathf.Max(5f, raioArea);
        velocidadePiscar = Mathf.Max(0.1f, velocidadePiscar);
        alturaVisualizacao = Mathf.Max(0.01f, alturaVisualizacao);
        segmentosCirculo = Mathf.Clamp(segmentosCirculo, 16, 128);

        // Reconstrói o material quando qualquer valor muda no Inspector
        if (!Application.isPlaying || materialBorda == null)
            return;

        Destroy(materialBorda);
        materialBorda = null;
        CriarMateriais();
    }
}