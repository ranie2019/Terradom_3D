using UnityEngine;

[DisallowMultipleComponent]
public class Natureza : MonoBehaviour
{
    [Header("Terreno onde vai spawnar")]
    [SerializeField] private Terrain terrain;

    [Header("Prefabs possíveis")]
    [SerializeField] private GameObject[] prefabsNatureza;

    [Header("Tempo")]
    [SerializeField] private float tempoEntreSpawns = 5f;

    [Header("Espaçamento")]
    [SerializeField] private float distanciaMinimaEntreObjetos = 40f;
    [SerializeField] private LayerMask camadasComColisao = ~0;

    [Header("Tentativas por ciclo")]
    [SerializeField] private int tentativasPorSpawn = 20;

    private float proximoSpawn;

    private void Awake()
    {
        proximoSpawn = Time.time + tempoEntreSpawns;
    }

    private void Update()
    {
        if (Time.time < proximoSpawn)
            return;

        proximoSpawn = Time.time + tempoEntreSpawns;
        TentarSpawnarObjetoAleatorio();
    }

    private void TentarSpawnarObjetoAleatorio()
    {
        if (terrain == null)
            return;

        if (prefabsNatureza == null || prefabsNatureza.Length == 0)
            return;

        for (int i = 0; i < tentativasPorSpawn; i++)
        {
            GameObject prefab = EscolherPrefabAleatorio();

            if (prefab == null)
                continue;

            Vector3 posicao = GerarPosicaoAleatoriaNoTerrain();

            if (!TemEspacoLivre(posicao))
                continue;

            Instantiate(prefab, posicao, Quaternion.identity);
            return;
        }
    }

    private GameObject EscolherPrefabAleatorio()
    {
        if (prefabsNatureza == null || prefabsNatureza.Length == 0)
            return null;

        int index = Random.Range(0, prefabsNatureza.Length);
        return prefabsNatureza[index];
    }

    private Vector3 GerarPosicaoAleatoriaNoTerrain()
    {
        TerrainData data = terrain.terrainData;
        Vector3 origem = terrain.transform.position;
        Vector3 tamanho = data.size;

        float x = Random.Range(origem.x, origem.x + tamanho.x);
        float z = Random.Range(origem.z, origem.z + tamanho.z);
        float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + origem.y;

        return new Vector3(x, y, z);
    }

    private bool TemEspacoLivre(Vector3 posicao)
    {
        Vector3 centro = new Vector3(posicao.x, posicao.y + 2f, posicao.z);

        Collider[] colisores = Physics.OverlapSphere(
            centro,
            distanciaMinimaEntreObjetos,
            camadasComColisao,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < colisores.Length; i++)
        {
            Collider col = colisores[i];

            if (col == null)
                continue;

            if (col is TerrainCollider)
                continue;

            Vector3 posCol = col.transform.position;

            float dx = posCol.x - posicao.x;
            float dz = posCol.z - posicao.z;

            float distanciaXZ = Mathf.Sqrt(dx * dx + dz * dz);

            if (distanciaXZ < distanciaMinimaEntreObjetos)
                return false;
        }

        return true;
    }
}