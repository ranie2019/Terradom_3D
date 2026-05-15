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
    [Tooltip("Arraste aqui a primeira Base Soldado da cena. Novas bases serão geradas ao redor deste ponto.")]
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

    private void Awake()
    {
        GarantirPastaBases();
    }

    private void GarantirPastaBases()
    {
        GameObject pasta = GameObject.Find("Clone Bases IA");
        if (pasta == null) pasta = new GameObject("Clone Bases IA");
        pastaBases = pasta.transform;
    }

    // =========================================================
    // PUBLICO
    // 0 = Soldado | 1 = Tank | 2 = Aviao | 3 = Torre Terra | 4 = Torre Ar
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

    public bool TentarCriarBaseSoldado()
    {
        return CriarBase(prefabBaseSoldado, "BaseSoldado",
            () => GameControllerRecursosIA.Instance.PodeCriarBaseSoldado(),
            () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseSoldado());
    }

    public bool TentarCriarBaseTank()
    {
        return CriarBase(prefabBaseTank, "BaseTank",
            () => GameControllerRecursosIA.Instance.PodeCriarBaseVeiculo(),
            () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseVeiculo());
    }

    public bool TentarCriarBaseAviao()
    {
        return CriarBase(prefabBaseAviao, "BaseAviao",
            () => GameControllerRecursosIA.Instance.PodeCriarBaseAviao(),
            () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseAviao());
    }

    public bool TentarCriarTorreTerra()
    {
        return CriarBase(prefabTorreTerra, "TorreTerra",
            () => GameControllerRecursosIA.Instance.PodeCriarTorreTerra(),
            () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaTorreTerra());
    }

    public bool TentarCriarTorreAr()
    {
        return CriarBase(prefabTorreAr, "TorreAr",
            () => GameControllerRecursosIA.Instance.PodeCriarTorreAr(),
            () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaTorreAr());
    }

    // =========================================================
    // PRIVADO
    // =========================================================

    private bool CriarBase(GameObject prefab, string layerName,
        System.Func<bool> podeCriar,
        System.Func<bool> gastarRecursos)
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

        // Verifica recurso ANTES de gerar posição
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
            Debug.LogWarning($"[BaseAreaIA] ⚠️ Layer '{layerName}' não existe! Crie em Project Settings > Tags and Layers.");
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

    private bool TentarGerarPosicaoValida(out Vector3 posicaoFinal)
    {
        posicaoFinal = Vector3.zero;

        // Centro de referência — usa ponto configurado no Inspector
        // Se não tiver, tenta usar o próprio transform como fallback
        Vector3 centroReferencia = pontoReferencia != null
            ? pontoReferencia.position
            : transform.position;

        // Tentativa 1: ao redor do ponto de referência
        for (int i = 0; i < 20; i++)
        {
            Vector3 pos = centroReferencia + new Vector3(
                Random.Range(-raioSpawn, raioSpawn), 0,
                Random.Range(-raioSpawn, raioSpawn));

            if (PosicaoValida(pos))
            {
                posicaoFinal = AjustarAlturaTerreno(pos);
                return true;
            }
        }

        // Tentativa 2: ao redor de bases existentes
        if (pastaBases == null) GarantirPastaBases();

        for (int i = 0; i < pastaBases.childCount; i++)
        {
            Transform baseExistente = pastaBases.GetChild(i);
            if (baseExistente == null) continue;

            for (int j = 0; j < 10; j++)
            {
                Vector3 pos = baseExistente.position + new Vector3(
                    Random.Range(-raioSpawn, raioSpawn), 0,
                    Random.Range(-raioSpawn, raioSpawn));

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
        if (terrain != null)
        {
            Vector3 tPos  = terrain.transform.position;
            Vector3 tSize = terrain.terrainData.size;

            if (pos.x < tPos.x || pos.x > tPos.x + tSize.x ||
                pos.z < tPos.z || pos.z > tPos.z + tSize.z)
                return false;
        }

        Collider[] cols = Physics.OverlapSphere(pos, margemColisao, camadaBloqueio, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null) continue;
            if (cols[i].gameObject.layer == LayerMask.NameToLayer("Default")) continue;
            return false;
        }

        Collider[] nearby = Physics.OverlapSphere(pos, distanciaMinimaEntreBases);
        for (int i = 0; i < nearby.Length; i++)
        {
            if (nearby[i] == null) continue;
            int layer = nearby[i].gameObject.layer;

            if (layer == LayerMask.NameToLayer("BaseSoldado") ||
                layer == LayerMask.NameToLayer("BaseTank")    ||
                layer == LayerMask.NameToLayer("BaseAviao")   ||
                layer == LayerMask.NameToLayer("TorreTerra") ||
                layer == LayerMask.NameToLayer("TorreAr"))
                return false;
        }

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
        float y = terrain.SampleHeight(pos) + terrain.transform.position.y;
        pos.y = y;
        return pos;
    }

    // =========================================================
    // CONSULTAS
    // 0 = Soldado | 1 = Tank | 2 = Aviao | 3 = Torre Terra | 4 = Torre Ar
    // =========================================================

    public int ContarBasesPorIndice(int indice)
    {
        if (pastaBases == null) GarantirPastaBases();

        int layer = LayerMask.NameToLayer(IndiceParaLayerName(indice));
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

        int layer = LayerMask.NameToLayer(IndiceParaLayerName(indice));
        if (layer == -1) return new Transform[0];

        List<Transform> lista = new List<Transform>();
        for (int i = 0; i < pastaBases.childCount; i++)
        {
            Transform t = pastaBases.GetChild(i);
            if (t != null && t.gameObject.layer == layer)
                lista.Add(t);
        }
        return lista.ToArray();
    }

    private string IndiceParaLayerName(int indice)
    {
        switch (indice)
        {
            case 0: return "BaseSoldado";
            case 1: return "BaseTank";
            case 2: return "BaseAviao";
            case 3: return "TorreTerra";
            case 4: return "TorreAr";
            default: return "";
        }
    }

    private void OnValidate()
    {
        raioSpawn                 = Mathf.Max(5f,   raioSpawn);
        margemColisao             = Mathf.Max(0.5f, margemColisao);
        distanciaMinimaEntreBases = Mathf.Max(10f,  distanciaMinimaEntreBases);
    }
}