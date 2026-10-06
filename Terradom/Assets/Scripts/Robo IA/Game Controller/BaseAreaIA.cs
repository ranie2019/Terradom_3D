using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class BaseAreaIA : MonoBehaviour
{
    [System.Serializable]
    private class BaseInimiga
    {
        public Transform transform;
        [Min(5f)] public float distanciaMinima = 25f;
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject prefabBaseSoldado;
    [SerializeField] private GameObject prefabBaseTank;
    [SerializeField] private GameObject prefabBaseAviao;
    [SerializeField] private GameObject prefabTorreTerra;
    [SerializeField] private GameObject prefabTorreAr;

    [Header("Área de Spawn")]
    [SerializeField] private float raioSpawn = 30f;
    [SerializeField] private LayerMask camadaBloqueio = ~0;

    [Header("Ponto de Referência")]
    [Tooltip("Arraste aqui a primeira Base Soldado da cena.")]
    [SerializeField] private Transform pontoReferencia;

    [Header("Limite do Terreno")]
    [SerializeField] private Terrain terrain;

    [Header("Colisão")]
    [SerializeField] private float margemColisao = 1.2f;

    [Header("Distância entre bases")]
    [SerializeField] private float distanciaMinimaEntreBases = 30f;

    [Header("Evitar inimigo")]
    [SerializeField] private BaseInimiga[] basesInimigas;

    private Transform pastaBases;

    // Cache de layers — calculado uma vez no Awake para não chamar
    // LayerMask.NameToLayer (string lookup) em cada tick de jogo.
    private int _layerBaseSoldado;
    private int _layerBaseTank;
    private int _layerBaseAviao;
    private int _layerTorreTerra;
    private int _layerTorreAr;
    private int _layerDefault;

    private readonly List<Transform> _bufferBases = new List<Transform>();

    private void Awake()
    {
        _layerBaseSoldado = LayerMask.NameToLayer("BaseSoldado");
        _layerBaseTank    = LayerMask.NameToLayer("BaseTank");
        _layerBaseAviao   = LayerMask.NameToLayer("BaseAviao");
        _layerTorreTerra  = LayerMask.NameToLayer("TorreTerra");
        _layerTorreAr     = LayerMask.NameToLayer("TorreAr");
        _layerDefault     = LayerMask.NameToLayer("Default");

        GarantirPastaBases();
    }

    private void GarantirPastaBases()
    {
        GameObject pasta = GameObject.Find("Clone Bases IA");
        if (pasta == null) pasta = new GameObject("Clone Bases IA");
        pastaBases = pasta.transform;
    }

    // =========================================================
    // PÚBLICO — 0=Soldado 1=Tank 2=Aviao 3=TorreTerra 4=TorreAr
    // =========================================================

    public bool TentarCriarBasePorIndice(int indice)
    {
        switch (indice)
        {
            case 0: return TentarCriarBaseSoldado();
            case 1: return TentarCriarBaseTank();
            case 2: return TentarCriarBaseAviao();
            case 3: return TentarCriarTorreTerra();
            case 4: return TentarCriarTorreAr();
        }
        return false;
    }

    public bool TentarCriarBaseSoldado() => CriarBase(prefabBaseSoldado, "BaseSoldado",
        () => GameControllerRecursosIA.Instance.PodeCriarBaseSoldado(),
        () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseSoldado());

    public bool TentarCriarBaseTank() => CriarBase(prefabBaseTank, "BaseTank",
        () => GameControllerRecursosIA.Instance.PodeCriarBaseVeiculo(),
        () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseVeiculo());

    public bool TentarCriarBaseAviao() => CriarBase(prefabBaseAviao, "BaseAviao",
        () => GameControllerRecursosIA.Instance.PodeCriarBaseAviao(),
        () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseAviao());

    public bool TentarCriarTorreTerra() => CriarBase(prefabTorreTerra, "TorreTerra",
        () => GameControllerRecursosIA.Instance.PodeCriarTorreTerra(),
        () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaTorreTerra());

    public bool TentarCriarTorreAr() => CriarBase(prefabTorreAr, "TorreAr",
        () => GameControllerRecursosIA.Instance.PodeCriarTorreAr(),
        () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaTorreAr());

    // =========================================================
    // CRIAÇÃO
    // =========================================================

    private bool CriarBase(GameObject prefab, string layerName,
        System.Func<bool> podeCriar, System.Func<bool> gastarRecursos)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[BaseAreaIA] ❌ Prefab '{layerName}' não atribuído no Inspector!");
            return false;
        }

        if (GameControllerRecursosIA.Instance == null)
        {
            Debug.LogWarning("[BaseAreaIA] ❌ GameControllerRecursosIA.Instance é null!");
            return false;
        }

        if (!podeCriar()) return false;

        if (!TentarGerarPosicaoValida(out Vector3 pos))
        {
            Debug.LogWarning($"[BaseAreaIA] ❌ Sem espaço para criar '{layerName}'");
            return false;
        }

        if (!gastarRecursos()) return false;

        InstanciarBase(prefab, pos, layerName);
        return true;
    }

    private void InstanciarBase(GameObject prefab, Vector3 pos, string layerName)
    {
        if (pastaBases == null) GarantirPastaBases();

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, pastaBases);
        obj.name = prefab.name;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer == -1)
            Debug.LogWarning($"[BaseAreaIA] ⚠️ Layer '{layerName}' não existe!");
        else
            AplicarLayerRecursivo(obj, layer);
    }

    private void AplicarLayerRecursivo(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        for (int i = 0; i < obj.transform.childCount; i++)
            AplicarLayerRecursivo(obj.transform.GetChild(i).gameObject, layer);
    }

    // =========================================================
    // BUSCA DE POSIÇÃO VÁLIDA
    //
    // BUG CORRIGIDO: A Tentativa 2 gerava posições em
    // Random.Range(-raioSpawn, raioSpawn) ao redor da base existente.
    // Como a base está no centro, e a restrição é distanciaMinimaEntreBases,
    // quando raioSpawn <= distanciaMinimaEntreBases era matematicamente
    // IMPOSSÍVEL encontrar uma posição válida — ela sempre estava perto
    // demais da base que serviu de referência.
    //
    // Solução: Tentativa 2 usa coordenadas polares para gerar posições
    // num ANEL entre distanciaMinimaEntreBases e distanciaMinimaEntreBases+raioSpawn,
    // garantindo que a posição começa JÁ além da distância mínima.
    // =========================================================

    private bool TentarGerarPosicaoValida(out Vector3 posicaoFinal)
    {
        posicaoFinal = Vector3.zero;
        if (pastaBases == null) GarantirPastaBases();

        Vector3 centroReferencia = pontoReferencia != null
            ? pontoReferencia.position
            : transform.position;

        // Tentativa 1: ao redor do ponto de referência com raio maior
        // Usa raioSpawn + distanciaMinimaEntreBases para cobrir área além da base inicial
        float raioExtendido = raioSpawn + distanciaMinimaEntreBases;
        for (int i = 0; i < 30; i++)
        {
            float angulo = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distancia = Random.Range(distanciaMinimaEntreBases + 2f, raioExtendido);
            Vector3 pos = centroReferencia + new Vector3(
                Mathf.Cos(angulo) * distancia, 0,
                Mathf.Sin(angulo) * distancia);

            if (PosicaoValida(pos))
            {
                posicaoFinal = AjustarAlturaTerreno(pos);
                return true;
            }
        }

        // Tentativa 2: anel ao redor de cada base existente
        // CORRIGIDO: gera posições ALÉM da distância mínima (não dentro dela)
        // usando coordenadas polares com raio entre distanciaMinima e distanciaMinima+raioSpawn.
        for (int i = 0; i < pastaBases.childCount; i++)
        {
            Transform baseExistente = pastaBases.GetChild(i);
            if (baseExistente == null) continue;

            for (int j = 0; j < 20; j++)
            {
                // Anel: começa ALÉM da distância mínima, não do centro
                float angulo    = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distancia = Random.Range(distanciaMinimaEntreBases + 2f,
                                              distanciaMinimaEntreBases + raioSpawn);

                Vector3 pos = baseExistente.position + new Vector3(
                    Mathf.Cos(angulo) * distancia, 0,
                    Mathf.Sin(angulo) * distancia);

                if (PosicaoValida(pos))
                {
                    posicaoFinal = AjustarAlturaTerreno(pos);
                    return true;
                }
            }
        }

        Debug.LogWarning("[BaseAreaIA] ❌ Nenhuma posição válida encontrada");
        return false;
    }

    private bool PosicaoValida(Vector3 pos)
    {
        // 1. Limites do terreno
        if (terrain != null)
        {
            Vector3 tPos  = terrain.transform.position;
            Vector3 tSize = terrain.terrainData.size;
            if (pos.x < tPos.x || pos.x > tPos.x + tSize.x ||
                pos.z < tPos.z || pos.z > tPos.z + tSize.z)
                return false;
        }

        // 2. Colisão com obstáculos — usa cache de layer Default
        Collider[] cols = Physics.OverlapSphere(pos, margemColisao, camadaBloqueio,
                                                QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null) continue;
            if (cols[i].gameObject.layer == _layerDefault) continue;
            return false;
        }

        // 3. Distância mínima entre bases — usa cache de layers
        Collider[] nearby = Physics.OverlapSphere(pos, distanciaMinimaEntreBases);
        for (int i = 0; i < nearby.Length; i++)
        {
            if (nearby[i] == null) continue;
            int layer = nearby[i].gameObject.layer;
            if (layer == _layerBaseSoldado || layer == _layerBaseTank  ||
                layer == _layerBaseAviao   || layer == _layerTorreTerra ||
                layer == _layerTorreAr)
                return false;
        }

        // 4. Distância mínima de bases inimigas configuradas manualmente
        if (basesInimigas != null)
        {
            foreach (BaseInimiga inimiga in basesInimigas)
            {
                if (inimiga == null || inimiga.transform == null) continue;
                if (Vector3.Distance(pos, inimiga.transform.position) < inimiga.distanciaMinima)
                    return false;
            }
        }

        return true;
    }

    private Vector3 AjustarAlturaTerreno(Vector3 pos)
    {
        if (terrain == null) return pos;
        pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;
        return pos;
    }

    // =========================================================
    // CONSULTAS — 0=Soldado 1=Tank 2=Aviao 3=TorreTerra 4=TorreAr
    // =========================================================

    public int ContarBasesPorIndice(int indice)
    {
        if (pastaBases == null) GarantirPastaBases();
        int layer = CamadaPorIndice(indice);
        if (layer == -1) return 0;

        int count = 0;
        for (int i = 0; i < pastaBases.childCount; i++)
        {
            Transform t = pastaBases.GetChild(i);
            if (t != null && t.gameObject.layer == layer) count++;
        }
        return count;
    }

    public bool ExisteBasePorIndice(int indice) => ContarBasesPorIndice(indice) > 0;

    public Transform[] ObterBasesPorIndice(int indice)
    {
        if (pastaBases == null) GarantirPastaBases();
        int layer = CamadaPorIndice(indice);
        if (layer == -1) return System.Array.Empty<Transform>();

        _bufferBases.Clear();
        for (int i = 0; i < pastaBases.childCount; i++)
        {
            Transform t = pastaBases.GetChild(i);
            if (t != null && t.gameObject.layer == layer)
                _bufferBases.Add(t);
        }
        return _bufferBases.ToArray();
    }

    private int CamadaPorIndice(int indice)
    {
        switch (indice)
        {
            case 0: return _layerBaseSoldado;
            case 1: return _layerBaseTank;
            case 2: return _layerBaseAviao;
            case 3: return _layerTorreTerra;
            case 4: return _layerTorreAr;
            default: return -1;
        }
    }

    private void OnValidate()
    {
        raioSpawn                 = Mathf.Max(5f,   raioSpawn);
        margemColisao             = Mathf.Max(0.5f, margemColisao);
        distanciaMinimaEntreBases = Mathf.Max(10f,  distanciaMinimaEntreBases);
    }
}