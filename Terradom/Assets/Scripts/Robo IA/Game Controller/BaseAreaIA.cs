using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class BaseAreaIA : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject prefabBaseSoldado;
    [SerializeField] private GameObject prefabBaseTank;
    [SerializeField] private GameObject prefabBaseAviao;

    [Header("Área de Spawn")]
    [SerializeField] private float raioSpawn = 30f;
    [SerializeField] private LayerMask camadaBloqueio = ~0;

    [Header("Limite do Terreno")]
    [SerializeField] private Terrain terrain;

    [Header("Colisão")]
    [SerializeField] private float margemColisao = 1.2f;

    [Header("Distância entre bases")]
    [SerializeField] private float distanciaMinimaEntreBases = 30f;

    [Header("Evitar inimigo")]
    [SerializeField] private Transform baseInimiga;
    [SerializeField] private float distanciaMinimaDoInimigo = 25f;

    private Transform pastaBases;

    private void Awake()
    {
        GameObject pasta = GameObject.Find("Clone IA");
        if (pasta == null)
            pasta = new GameObject("Clone IA");

        pastaBases = pasta.transform;
    }

    public bool TentarCriarBasePorIndice(int indice)
    {
        switch (indice)
        {
            case 0: return TentarCriarBaseSoldado();
            case 1: return TentarCriarBaseTank();
            case 2: return TentarCriarBaseAviao();
        }
        return false;
    }

    public bool TentarCriarBaseSoldado()
    {
        return CriarBase(
            prefabBaseSoldado,
            "BaseSoldado",
            () => GameControllerRecursosIA.Instance.PodeCriarBaseSoldado(),
            () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseSoldado()
        );
    }

    public bool TentarCriarBaseTank()
    {
        return CriarBase(
            prefabBaseTank,
            "BaseTank",
            () => GameControllerRecursosIA.Instance.PodeCriarBaseVeiculo(),
            () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseVeiculo()
        );
    }

    public bool TentarCriarBaseAviao()
    {
        return CriarBase(
            prefabBaseAviao,
            "BaseAviao",
            () => GameControllerRecursosIA.Instance.PodeCriarBaseAviao(),
            () => GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseAviao()
        );
    }

    private bool CriarBase(GameObject prefab, string layerName,
        System.Func<bool> podeCriar,
        System.Func<bool> gastarRecursos)
    {
        if (prefab == null) return false;
        if (GameControllerRecursosIA.Instance == null) return false;

        Vector3 pos = GerarPosicaoValida();
        if (pos == Vector3.zero)
        {
            Debug.LogWarning("[BaseAreaIA] ❌ Sem espaço para criar base");
            return false;
        }

        if (!podeCriar()) return false;
        if (!gastarRecursos()) return false;

        InstanciarBase(prefab, pos, layerName);

        return true;
    }

    private void InstanciarBase(GameObject prefab, Vector3 pos, string layerName)
    {
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, pastaBases);
        obj.name = prefab.name;

        int layer = LayerMask.NameToLayer(layerName);
        AplicarLayerRecursivo(obj, layer);
    }

    private void AplicarLayerRecursivo(GameObject obj, int layer)
    {
        if (layer != -1)
            obj.layer = layer;

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            AplicarLayerRecursivo(obj.transform.GetChild(i).gameObject, layer);
        }
    }

    private Vector3 GerarPosicaoValida()
    {
        for (int i = 0; i < 20; i++)
        {
            Vector3 pos = transform.position +
                new Vector3(
                    Random.Range(-raioSpawn, raioSpawn),
                    0,
                    Random.Range(-raioSpawn, raioSpawn)
                );

            if (PosicaoValida(pos))
                return AjustarAlturaTerreno(pos);
        }

        for (int i = 0; i < pastaBases.childCount; i++)
        {
            Transform baseExistente = pastaBases.GetChild(i);

            for (int j = 0; j < 10; j++)
            {
                Vector3 pos = baseExistente.position +
                    new Vector3(
                        Random.Range(-raioSpawn, raioSpawn),
                        0,
                        Random.Range(-raioSpawn, raioSpawn)
                    );

                if (PosicaoValida(pos))
                    return AjustarAlturaTerreno(pos);
            }
        }

        Debug.LogWarning("[BaseAreaIA] ❌ Nenhuma posição válida encontrada");
        return Vector3.zero;
    }

    private bool PosicaoValida(Vector3 pos)
    {
        // 🔥 BLOQUEIO FORA DO TERRAIN
        if (terrain != null)
        {
            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = terrain.terrainData.size;

            if (pos.x < tPos.x || pos.x > tPos.x + tSize.x ||
                pos.z < tPos.z || pos.z > tPos.z + tSize.z)
                return false;
        }

        // colisão física
        Collider[] cols = Physics.OverlapSphere(
            pos,
            margemColisao,
            camadaBloqueio,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null) continue;

            if (cols[i].gameObject.layer == LayerMask.NameToLayer("Default"))
                continue;

            return false;
        }

        // distância entre bases
        Collider[] nearby = Physics.OverlapSphere(pos, distanciaMinimaEntreBases);

        for (int i = 0; i < nearby.Length; i++)
        {
            if (nearby[i] == null) continue;

            int layer = nearby[i].gameObject.layer;

            if (layer == LayerMask.NameToLayer("BaseSoldado") ||
                layer == LayerMask.NameToLayer("BaseTank") ||
                layer == LayerMask.NameToLayer("BaseAviao"))
                return false;
        }

        // evitar inimigo
        if (baseInimiga != null &&
            Vector3.Distance(pos, baseInimiga.position) < distanciaMinimaDoInimigo)
            return false;

        return true;
    }

    // 🔥 AJUSTA ALTURA NO TERRAIN
    private Vector3 AjustarAlturaTerreno(Vector3 pos)
    {
        if (terrain == null) return pos;

        float y = terrain.SampleHeight(pos) + terrain.transform.position.y;
        pos.y = y;
        return pos;
    }

    public int ContarBasesPorIndice(int indice)
    {
        string layerName = "";

        switch (indice)
        {
            case 0: layerName = "BaseSoldado"; break;
            case 1: layerName = "BaseTank"; break;
            case 2: layerName = "BaseAviao"; break;
        }

        int layer = LayerMask.NameToLayer(layerName);
        int count = 0;

        for (int i = 0; i < pastaBases.childCount; i++)
        {
            GameObject obj = pastaBases.GetChild(i).gameObject;

            if (obj.layer == layer)
                count++;
        }

        return count;
    }

    public bool ExisteBasePorIndice(int indice)
    {
        return ContarBasesPorIndice(indice) > 0;
    }

    public Transform[] ObterBasesPorIndice(int indice)
    {
        string layerName = "";

        switch (indice)
        {
            case 0: layerName = "BaseSoldado"; break;
            case 1: layerName = "BaseTank"; break;
            case 2: layerName = "BaseAviao"; break;
        }

        int layer = LayerMask.NameToLayer(layerName);

        List<Transform> lista = new List<Transform>();

        for (int i = 0; i < pastaBases.childCount; i++)
        {
            Transform t = pastaBases.GetChild(i);

            if (t.gameObject.layer == layer)
                lista.Add(t);
        }

        return lista.ToArray();
    }

    private void OnValidate()
    {
        raioSpawn = Mathf.Max(5f, raioSpawn);
        margemColisao = Mathf.Max(0.5f, margemColisao);
        distanciaMinimaEntreBases = Mathf.Max(10f, distanciaMinimaEntreBases);
        distanciaMinimaDoInimigo = Mathf.Max(5f, distanciaMinimaDoInimigo);
    }
}