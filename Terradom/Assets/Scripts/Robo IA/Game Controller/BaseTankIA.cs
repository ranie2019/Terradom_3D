using UnityEngine;

[DisallowMultipleComponent]
public class BaseTankIA : MonoBehaviour
{
    private BaseAreaIA baseArea;
    private TankSpownIA tankSpawn;

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

        if (tankSpawn == null)
            tankSpawn = FindFirstObjectByType<TankSpownIA>();
    }

    private void GarantirReferencias()
    {
        if (baseArea == null || tankSpawn == null)
            AtualizarReferencias();
    }

    // =========================================================
    // BASE TANK (APENAS EXECUTA)
    // =========================================================
    public bool CriarBaseTank()
    {
        GarantirReferencias();

        if (baseArea == null)
        {
            Debug.LogError("[BaseTankIA] ❌ BaseAreaIA não encontrada!");
            return false;
        }

        bool criada = baseArea.TentarCriarBasePorIndice(1);

        if (criada)
            Debug.Log("[BaseTankIA] 🏗️ Base Tank criada com sucesso");

        return criada;
    }

    public bool BaseTankExiste()
    {
        GarantirReferencias();
        return baseArea != null && baseArea.ExisteBasePorIndice(1);
    }

    // =========================================================
    // TANK
    // =========================================================
    public bool PodeCriarTank()
    {
        GarantirReferencias();
        return tankSpawn != null && tankSpawn.PodeCriarTank();
    }

    public bool CriarTank()
    {
        GarantirReferencias();

        if (tankSpawn == null)
        {
            Debug.LogError("[BaseTankIA] ❌ TankSpownIA não encontrado!");
            return false;
        }

        return tankSpawn.TentarCriarTank();
    }
}