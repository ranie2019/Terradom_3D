using UnityEngine;

[DisallowMultipleComponent]
public class SoldadoSpown : MonoBehaviour
{
    [System.Serializable]
    public class SoldadoParaSpawn
    {
        public GameObject prefabSoldado;
        public float tempoParaNascer = 5f;

        [HideInInspector] public float proximoSpawn;
    }

    [Header("Ponto onde o soldado vai nascer")]
    [SerializeField] private Transform pontoSpawn;

    [Header("Soldados que a base pode criar")]
    [SerializeField] private SoldadoParaSpawn[] soldados;

    private void Start()
    {
        for (int i = 0; i < soldados.Length; i++)
            soldados[i].proximoSpawn = Time.time + soldados[i].tempoParaNascer;
    }

    private void Update()
    {
        if (pontoSpawn == null)
            return;

        for (int i = 0; i < soldados.Length; i++)
        {
            if (soldados[i].prefabSoldado == null)
                continue;

            if (Time.time >= soldados[i].proximoSpawn)
            {
                Instantiate(
                    soldados[i].prefabSoldado,
                    pontoSpawn.position,
                    pontoSpawn.rotation
                );

                soldados[i].proximoSpawn = Time.time + soldados[i].tempoParaNascer;
            }
        }
    }
}