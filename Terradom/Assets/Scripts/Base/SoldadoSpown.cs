using UnityEngine;

[DisallowMultipleComponent]
public class SoldadoSpown : MonoBehaviour
{
    [Header("Ponto onde a unidade vai nascer")]
    [SerializeField] private Transform pontoSpawn;

    [Header("Prefab Guerreiro")]
    [SerializeField] private GameObject prefabGuerreiro;

    [Header("Prefab Coletor/Recurso")]
    [SerializeField] private GameObject prefabRecurso;

    [Header("Prefab Soldado")]
    [SerializeField] private GameObject prefabSoldado;

    [Header("Custo Guerreiro")]
    [SerializeField] private int custoPedraGuerreiro = 10;
    [SerializeField] private int custoMadeiraGuerreiro = 10;
    [SerializeField] private int custoMetalGuerreiro = 10;

    [Header("Custo Recurso")]
    [SerializeField] private int custoPedraRecurso = 10;
    [SerializeField] private int custoMadeiraRecurso = 10;
    [SerializeField] private int custoMetalRecurso = 10;

    [Header("Custo Soldado")]
    [SerializeField] private int custoPedraSoldado = 10;
    [SerializeField] private int custoMadeiraSoldado = 10;
    [SerializeField] private int custoMetalSoldado = 10;

    [Header("Organização")]
    [SerializeField] private bool manterMesmoPaiDaBase = false;

    public bool PodeCriarGuerreiro()
    {
        if (GameControllerRecursos.Instance == null)
            return false;

        return GameControllerRecursos.Instance.TemRecursos(
            custoPedraGuerreiro,
            custoMadeiraGuerreiro,
            custoMetalGuerreiro
        );
    }

    public bool PodeCriarRecurso()
    {
        if (GameControllerRecursos.Instance == null)
            return false;

        return GameControllerRecursos.Instance.TemRecursos(
            custoPedraRecurso,
            custoMadeiraRecurso,
            custoMetalRecurso
        );
    }

    public bool PodeCriarSoldado()
    {
        if (GameControllerRecursos.Instance == null)
            return false;

        return GameControllerRecursos.Instance.TemRecursos(
            custoPedraSoldado,
            custoMadeiraSoldado,
            custoMetalSoldado
        );
    }

    public void CriarGuerreiro()
    {
        CriarUnidade(
            prefabGuerreiro,
            custoPedraGuerreiro,
            custoMadeiraGuerreiro,
            custoMetalGuerreiro
        );
    }

    public void CriarRecurso()
    {
        CriarUnidade(
            prefabRecurso,
            custoPedraRecurso,
            custoMadeiraRecurso,
            custoMetalRecurso
        );
    }

    public void CriarSoldado()
    {
        CriarUnidade(
            prefabSoldado,
            custoPedraSoldado,
            custoMadeiraSoldado,
            custoMetalSoldado
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