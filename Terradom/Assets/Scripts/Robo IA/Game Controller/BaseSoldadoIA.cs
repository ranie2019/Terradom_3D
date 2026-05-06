using UnityEngine;

[DisallowMultipleComponent]
public class BaseSoldadoIA : MonoBehaviour
{
    private BaseAreaIA baseArea;
    private SoldadoSpownIA soldadoSpawn;

    private void Awake()
    {
        AtualizarReferencias();
    }

    // =========================================================
    // REFERÊNCIAS (SEGURAS)
    // =========================================================
    public void AtualizarReferencias()
    {
        if (baseArea == null)
            baseArea = FindFirstObjectByType<BaseAreaIA>();

        if (soldadoSpawn == null)
            soldadoSpawn = FindFirstObjectByType<SoldadoSpownIA>();
    }

    private void GarantirReferencias()
    {
        if (baseArea == null || soldadoSpawn == null)
            AtualizarReferencias();
    }

    // =========================================================
    // BASE SOLDADO (ÍNDICE 0)
    // =========================================================
    public bool CriarBaseSoldado()
    {
        GarantirReferencias();

        if (baseArea == null)
        {
            Debug.LogError("[BaseSoldadoIA] ❌ BaseAreaIA não encontrada!");
            return false;
        }

        bool criada = baseArea.TentarCriarBasePorIndice(0);

        if (criada)
            Debug.Log("[BaseSoldadoIA] 🏗️ Base Soldado criada com sucesso");

        return criada;
    }

    public bool BaseSoldadoExiste()
    {
        GarantirReferencias();
        return baseArea != null && baseArea.ExisteBasePorIndice(0);
    }

    // =========================================================
    // COLETOR
    // =========================================================
    public bool PodeCriarColetor()
    {
        GarantirReferencias();
        return soldadoSpawn != null && soldadoSpawn.PodeCriarColetor();
    }

    public bool CriarColetor()
    {
        GarantirReferencias();

        if (soldadoSpawn == null)
        {
            Debug.LogError("[BaseSoldadoIA] ❌ SoldadoSpownIA não encontrado!");
            return false;
        }

        return soldadoSpawn.TentarCriarColetor();
    }

    // =========================================================
    // SOLDADO
    // =========================================================
    public bool PodeCriarSoldado()
    {
        GarantirReferencias();
        return soldadoSpawn != null && soldadoSpawn.PodeCriarSoldado();
    }

    public bool CriarSoldado()
    {
        GarantirReferencias();

        if (soldadoSpawn == null)
        {
            Debug.LogError("[BaseSoldadoIA] ❌ SoldadoSpownIA não encontrado!");
            return false;
        }

        return soldadoSpawn.TentarCriarSoldado();
    }

    // =========================================================
    // GUERREIRO
    // =========================================================
    public bool PodeCriarGuerreiro()
    {
        GarantirReferencias();
        return soldadoSpawn != null && soldadoSpawn.PodeCriarGuerreiro();
    }

    public bool CriarGuerreiro()
    {
        GarantirReferencias();

        if (soldadoSpawn == null)
        {
            Debug.LogError("[BaseSoldadoIA] ❌ SoldadoSpownIA não encontrado!");
            return false;
        }

        return soldadoSpawn.TentarCriarGuerreiro();
    }
}