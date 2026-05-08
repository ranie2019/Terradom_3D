using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class BaseLimite : MonoBehaviour
{
    // =========================================================
    // SINGLETON - GERENCIADOR CENTRAL
    // =========================================================
    private static Dictionary<string, List<BaseLimite>> basesPorTag = new Dictionary<string, List<BaseLimite>>();
    
    [Header("Configuração da Área de Construção")]
    [SerializeField] private float raioArea = 15f;
    [SerializeField] private string tagBase = "Vermelho";
    
    [Header("Efeito Visual")]
    [SerializeField] private bool mostrarArea = true;
    [SerializeField] private Color corArea = new Color(0.2f, 0.8f, 0.2f, 0.15f);
    [SerializeField] private Color corBorda = new Color(0.2f, 0.8f, 0.2f, 0.8f);
    [SerializeField] private float velocidadePiscar = 2f;
    [SerializeField] private float alturaVisualizacao = 0.05f;
    [SerializeField] private int segmentosCirculo = 64;
    
    [Header("Referência ao Terreno")]
    [SerializeField] private Terrain terrain;
    
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool mostrarConexoes = true;
    [SerializeField] private Color corConexao = Color.yellow;
    
    // Materiais para o efeito visual
    private Material materialArea;
    private Material materialBorda;
    private Mesh meshCirculo;
    private Mesh meshBorda;
    
    // Cache
    private float alphaAtual = 1f;
    private float timerPiscar;
    private Vector3 posicaoBase;
    
    private void Awake()
    {
        // Procura o Terrain se não foi atribuído
        if (terrain == null)
            terrain = FindFirstObjectByType<Terrain>();
        
        // Usa a tag do GameObject se tagBase estiver vazia
        if (string.IsNullOrEmpty(tagBase))
            tagBase = gameObject.tag;
        
        // Registra esta base no dicionário global
        RegistrarBase();
        
        // Cria os materiais e meshes para visualização
        CriarMateriais();
        CriarMeshes();
        
        timerPiscar = 0f;
        posicaoBase = transform.position;
        
        if (debugLogs)
            Debug.Log($"[BaseLimite] Base registrada com tag '{tagBase}'. Total bases desta tag: {ObterTotalBasesDaTag()}");
    }
    
    private void OnDestroy()
    {
        // Remove esta base do dicionário global
        RemoverBase();
        
        // Limpa materiais
        if (materialArea != null)
            Destroy(materialArea);
        if (materialBorda != null)
            Destroy(materialBorda);
        if (meshCirculo != null)
            Destroy(meshCirculo);
        if (meshBorda != null)
            Destroy(meshBorda);
    }
    
    private void Update()
    {
        if (!mostrarArea)
            return;
        
        // Atualiza posição (a base pode ter sido movida)
        posicaoBase = transform.position;
        
        // Atualiza o efeito de pulsar
        timerPiscar += Time.deltaTime * velocidadePiscar;
        alphaAtual = (Mathf.Sin(timerPiscar) + 1f) * 0.5f; // Oscila entre 0 e 1
    }
    
    // =========================================================
    // REGISTRO GLOBAL
    // =========================================================
    
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
    }
    
    private void RemoverBase()
    {
        if (string.IsNullOrEmpty(tagBase))
            return;
        
        if (basesPorTag.ContainsKey(tagBase))
        {
            basesPorTag[tagBase].Remove(this);
            if (basesPorTag[tagBase].Count == 0)
                basesPorTag.Remove(tagBase);
        }
    }
    
    private int ObterTotalBasesDaTag()
    {
        if (basesPorTag.ContainsKey(tagBase))
            return basesPorTag[tagBase].Count;
        return 0;
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
        // Material da área (transparente)
        Shader shaderTransparente = Shader.Find("Sprites/Default");
        if (shaderTransparente == null)
            shaderTransparente = Shader.Find("Unlit/Color");
        
        materialArea = new Material(shaderTransparente);
        materialArea.color = corArea;
        
        // Material da borda
        materialBorda = new Material(shaderTransparente);
        materialBorda.color = corBorda;
    }
    
    private void CriarMeshes()
    {
        // Cria malha circular para a área
        meshCirculo = CriarMalhaCircular(raioArea, segmentosCirculo, preenchido: true);
        
        // Cria malha circular para a borda (anel)
        meshBorda = CriarMalhaCircular(raioArea, segmentosCirculo, preenchido: false);
    }
    
    private Mesh CriarMalhaCircular(float raio, int segmentos, bool preenchido)
    {
        Mesh mesh = new Mesh();
        mesh.name = preenchido ? "CirculoArea" : "BordaArea";
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangulos = new List<int>();
        
        if (preenchido)
        {
            // Centro do círculo
            vertices.Add(Vector3.zero);
            
            // Vértices da borda (no plano XZ - horizontal)
            for (int i = 0; i <= segmentos; i++)
            {
                float angulo = (float)i / segmentos * Mathf.PI * 2f;
                float x = Mathf.Cos(angulo) * raio;
                float z = Mathf.Sin(angulo) * raio;
                vertices.Add(new Vector3(x, 0, z)); // Y = 0, plano horizontal
            }
            
            // Triângulos (todos conectados ao centro)
            for (int i = 1; i <= segmentos; i++)
            {
                triangulos.Add(0);
                triangulos.Add(i);
                triangulos.Add(i + 1 > segmentos ? 1 : i + 1);
            }
        }
        else
        {
            // Borda - cria um anel fino
            float larguraBorda = 0.5f;
            float raioInterno = raio - larguraBorda;
            
            for (int i = 0; i <= segmentos; i++)
            {
                float angulo = (float)i / segmentos * Mathf.PI * 2f;
                float x = Mathf.Cos(angulo);
                float z = Mathf.Sin(angulo);
                
                // Vértice externo
                vertices.Add(new Vector3(x * raio, 0, z * raio));
                // Vértice interno
                vertices.Add(new Vector3(x * raioInterno, 0, z * raioInterno));
            }
            
            // Triângulos do anel
            for (int i = 0; i < segmentos; i++)
            {
                int extAtual = i * 2;
                int intAtual = i * 2 + 1;
                int extProx = (i + 1) * 2;
                int intProx = (i + 1) * 2 + 1;
                
                // Primeiro triângulo
                triangulos.Add(extAtual);
                triangulos.Add(intAtual);
                triangulos.Add(extProx);
                
                // Segundo triângulo
                triangulos.Add(intAtual);
                triangulos.Add(intProx);
                triangulos.Add(extProx);
            }
        }
        
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangulos, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    private void OnRenderObject()
    {
        if (!mostrarArea)
            return;
        
        if (materialArea == null || materialBorda == null)
            return;
        
        if (meshCirculo == null || meshBorda == null)
            return;
        
        // Ajusta a posição Y baseada no terreno
        float alturaY = posicaoBase.y;
        if (terrain != null)
        {
            alturaY = terrain.SampleHeight(posicaoBase) + terrain.transform.position.y + alturaVisualizacao;
        }
        else
        {
            alturaY += alturaVisualizacao;
        }
        
        Vector3 posicaoRender = new Vector3(posicaoBase.x, alturaY, posicaoBase.z);
        
        // CORREÇÃO: Sem rotação - a malha já está no plano XZ (horizontal)
        Matrix4x4 matriz = Matrix4x4.TRS(posicaoRender, Quaternion.identity, Vector3.one);
        
        // Renderiza a área com alpha pulsante
        Color corAreaAtual = materialArea.color;
        corAreaAtual.a = corArea.a * alphaAtual;
        materialArea.color = corAreaAtual;
        materialArea.SetPass(0);
        Graphics.DrawMeshNow(meshCirculo, matriz);
        
        // Renderiza a borda com alpha pulsante (um pouco mais opaco)
        Color corBordaAtual = materialBorda.color;
        corBordaAtual.a = corBorda.a * (0.5f + alphaAtual * 0.5f);
        materialBorda.color = corBordaAtual;
        materialBorda.SetPass(0);
        Graphics.DrawMeshNow(meshBorda, matriz);
    }
    
    private void OnDrawGizmos()
    {
        // Desenha a área no Editor (não pulsante)
        if (mostrarArea && !Application.isPlaying)
        {
            Gizmos.color = corArea;
            DrawCircleGizmo(transform.position, raioArea, segmentosCirculo / 2);
            
            Gizmos.color = corBorda;
            DrawCircleGizmo(transform.position, raioArea, segmentosCirculo);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!mostrarConexoes || !Application.isPlaying)
            return;
        
        // Desenha conexões com outras bases da mesma tag
        if (basesPorTag.ContainsKey(tagBase))
        {
            Gizmos.color = corConexao;
            
            foreach (BaseLimite outraBase in basesPorTag[tagBase])
            {
                if (outraBase == this || outraBase == null)
                    continue;
                
                Gizmos.DrawLine(transform.position, outraBase.transform.position);
            }
        }
        
        // Destaca a área desta base
        Gizmos.color = new Color(corArea.r, corArea.g, corArea.b, 0.3f);
        DrawCircleGizmo(transform.position, raioArea, segmentosCirculo);
    }
    
    private void DrawCircleGizmo(Vector3 centro, float raio, int segmentos)
    {
        float anguloPasso = 360f / segmentos;
        Vector3 pontoAnterior = centro + new Vector3(raio, 0, 0);
        
        for (int i = 1; i <= segmentos; i++)
        {
            float angulo = anguloPasso * i * Mathf.Deg2Rad;
            Vector3 pontoAtual = centro + new Vector3(
                Mathf.Cos(angulo) * raio,
                0,
                Mathf.Sin(angulo) * raio
            );
            
            Gizmos.DrawLine(pontoAnterior, pontoAtual);
            pontoAnterior = pontoAtual;
        }
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
        
        // Recria os meshes com o novo raio
        if (meshCirculo != null) Destroy(meshCirculo);
        if (meshBorda != null) Destroy(meshBorda);
        
        CriarMeshes();
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
        
        // Registra com a nova tag
        RegistrarBase();
    }
    
    private void OnValidate()
    {
        raioArea = Mathf.Max(5f, raioArea);
        velocidadePiscar = Mathf.Max(0.1f, velocidadePiscar);
        alturaVisualizacao = Mathf.Max(0.01f, alturaVisualizacao);
        segmentosCirculo = Mathf.Clamp(segmentosCirculo, 16, 128);
    }
}