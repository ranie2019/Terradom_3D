using UnityEngine;

[DisallowMultipleComponent]
public class TankSpown : MonoBehaviour
{
    [Header("Ponto onde o tank vai nascer")]
    [SerializeField] private Transform pontoSpawn;

    [Header("Prefab Tank")]
    [SerializeField] private GameObject prefabTank;

    [Header("Custo Tank")]
    [SerializeField] private int custoPedraTank = 10;
    [SerializeField] private int custoMadeiraTank = 10;
    [SerializeField] private int custoMetalTank = 10;

    [Header("Organização")]
    [SerializeField] private bool manterMesmoPaiDaBase = false;

    public bool PodeCriarTank()
    {
        if (GameControllerRecursos.Instance == null)
            return false;

        return GameControllerRecursos.Instance.TemRecursos(
            custoPedraTank,
            custoMadeiraTank,
            custoMetalTank
        );
    }

    public void CriarTank()
    {
        CriarUnidade(
            prefabTank,
            custoPedraTank,
            custoMadeiraTank,
            custoMetalTank
        );
    }

    private void CriarUnidade(GameObject prefab, int custoPedra, int custoMadeira, int custoMetal)
    {
        if (prefab == null || pontoSpawn == null)
            return;

        if (GameControllerRecursos.Instance == null)
            return;

        bool gastou = GameControllerRecursos.Instance.TentarGastarRecursos(
            custoPedra,
            custoMadeira,
            custoMetal
        );

        if (!gastou)
            return;

        GameObject novo = Instantiate(
            prefab,
            pontoSpawn.position,
            pontoSpawn.rotation
        );

        novo.SetActive(true);
        AtivarObjetoCompleto(novo);

        if (manterMesmoPaiDaBase && transform.parent != null)
            novo.transform.SetParent(transform.parent);
    }

    private void AtivarObjetoCompleto(GameObject obj)
    {
        if (obj == null)
            return;

        obj.SetActive(true);

        Transform[] filhos = obj.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < filhos.Length; i++)
        {
            if (filhos[i] != null)
                filhos[i].gameObject.SetActive(true);
        }

        MonoBehaviour[] scripts = obj.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] != null)
                scripts[i].enabled = true;
        }

        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = true;
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = true;
        }
    }
}