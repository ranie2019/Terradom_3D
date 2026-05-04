using UnityEngine;

[DisallowMultipleComponent]
public class BaseAreaIA : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject prefabBaseSoldado;
    [SerializeField] private GameObject prefabBaseTank;
    [SerializeField] private GameObject prefabBaseAviao;

    [Header("Area de Spawn")]
    [SerializeField] private float raioSpawn = 30f;
    [SerializeField] private LayerMask camadaBloqueio = ~0;

    [Header("Colisao")]
    [SerializeField] private float margemColisao = 0.95f;

    [Header("Distancia entre bases")]
    [SerializeField] private float distanciaMinimaEntreBases = 25f;

    [Header("Evitar inimigo")]
    [SerializeField] private Transform baseInimiga;
    [SerializeField] private float distanciaMinimaDoInimigo = 25f;

    // =========================================================
    // API
    // =========================================================

    public bool TentarCriarBasePorIndice(int indice)
    {
        if (indice == 0) return TentarCriarBaseSoldado();
        if (indice == 1) return TentarCriarBaseTank();
        if (indice == 2) return TentarCriarBaseAviao();
        return false;
    }

    public bool TentarCriarBaseSoldado()
    {
        if (GameControllerRecursosIA.Instance == null) return false;
        if (!GameControllerRecursosIA.Instance.PodeCriarBaseSoldado()) return false;

        Vector3 pos = GerarPosicaoValida();
        if (pos == Vector3.zero) return false;

        if (!GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseSoldado())
            return false;

        InstanciarBase(prefabBaseSoldado, pos, "BaseSoldado");
        return true;
    }

    public bool TentarCriarBaseTank()
    {
        if (GameControllerRecursosIA.Instance == null) return false;
        if (!GameControllerRecursosIA.Instance.PodeCriarBaseVeiculo()) return false;

        Vector3 pos = GerarPosicaoValida();
        if (pos == Vector3.zero) return false;

        if (!GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseVeiculo())
            return false;

        InstanciarBase(prefabBaseTank, pos, "BaseTank");
        return true;
    }

    public bool TentarCriarBaseAviao()
    {
        if (GameControllerRecursosIA.Instance == null) return false;
        if (!GameControllerRecursosIA.Instance.PodeCriarBaseAviao()) return false;

        Vector3 pos = GerarPosicaoValida();
        if (pos == Vector3.zero) return false;

        if (!GameControllerRecursosIA.Instance.TentarGastarRecursosDaBaseAviao())
            return false;

        InstanciarBase(prefabBaseAviao, pos, "BaseAviao");
        return true;
    }

    // =========================================================
    // SPAWN
    // =========================================================

    private void InstanciarBase(GameObject prefab, Vector3 pos, string layerName)
    {
        if (prefab == null) return;

        GameObject pasta = GameObject.Find("Clone IA");
        if (pasta == null)
            pasta = new GameObject("Clone IA");

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, pasta.transform);
        obj.name = prefab.name;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer != -1)
            obj.layer = layer;

        // aplica layer nos filhos também
        AplicarLayerRecursivo(obj, layer);
    }

    private void AplicarLayerRecursivo(GameObject obj, int layer)
    {
        obj.layer = layer;

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            AplicarLayerRecursivo(obj.transform.GetChild(i).gameObject, layer);
        }
    }

    // =========================================================
    // POSIÇÃO
    // =========================================================

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
                return pos;
        }

        return Vector3.zero;
    }

    private bool PosicaoValida(Vector3 pos)
    {
        // 1. colisão REAL (ignora triggers e chão problemático)
        Collider[] cols = Physics.OverlapSphere(
            pos,
            margemColisao,
            camadaBloqueio,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null)
                continue;

            // ignora terreno explicitamente
            if (cols[i].gameObject.layer == LayerMask.NameToLayer("Default"))
                continue;

            return false;
        }

        // 2. detectar apenas bases por layer (CORRETO)
        Collider[] nearby = Physics.OverlapSphere(pos, distanciaMinimaEntreBases);

        for (int i = 0; i < nearby.Length; i++)
        {
            if (nearby[i] == null)
                continue;

            int layer = nearby[i].gameObject.layer;

            if (layer == LayerMask.NameToLayer("BaseSoldado") ||
                layer == LayerMask.NameToLayer("BaseTank") ||
                layer == LayerMask.NameToLayer("BaseAviao"))
            {
                return false;
            }
        }

        // 3. inimigo
        if (baseInimiga != null &&
            Vector3.Distance(pos, baseInimiga.position) < distanciaMinimaDoInimigo)
            return false;

        return true;
    }

    // =========================================================
    // CONSULTA
    // =========================================================

    public bool ExisteBasePorIndice(int indice)
    {
        string layerName = "";

        if (indice == 0) layerName = "BaseSoldado";
        if (indice == 1) layerName = "BaseTank";
        if (indice == 2) layerName = "BaseAviao";

        Collider[] cols = Physics.OverlapSphere(transform.position, 9999f);

        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null) continue;

            if (cols[i].gameObject.layer == LayerMask.NameToLayer(layerName))
                return true;
        }

        return false;
    }

    // =========================================================
    // VALIDATE
    // =========================================================

    private void OnValidate()
    {
        raioSpawn = Mathf.Max(1f, raioSpawn);
        margemColisao = Mathf.Max(0.1f, margemColisao);
        distanciaMinimaEntreBases = Mathf.Max(1f, distanciaMinimaEntreBases);
        distanciaMinimaDoInimigo = Mathf.Max(1f, distanciaMinimaDoInimigo);
    }
}