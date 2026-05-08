using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class RoboCriar : MonoBehaviour
{
    private BaseAreaIA baseArea;
    private SoldadoSpownIA soldadoSpawn;
    private TankSpownIA tankSpawn;

    public bool TankSpawnDisponivel() => tankSpawn != null;

    private void Awake()
    {
        AtualizarReferencias();
    }

    // =========================================================
    // REFERÊNCIAS
    // =========================================================

    public void AtualizarReferencias()
    {
        if (baseArea == null)
            baseArea = FindFirstObjectByType<BaseAreaIA>();
        if (soldadoSpawn == null)
            soldadoSpawn = FindFirstObjectByType<SoldadoSpownIA>();
        if (tankSpawn == null)
            tankSpawn = FindFirstObjectByType<TankSpownIA>();

        if (baseArea == null)
            Debug.LogError("[RoboCriar] ❌ BaseAreaIA não encontrada!");
        if (soldadoSpawn == null)
            Debug.LogError("[RoboCriar] ❌ SoldadoSpownIA não encontrada!");
        if (tankSpawn == null)
            Debug.LogWarning("[RoboCriar] ⚠️ TankSpownIA não encontrada. Produção de tank desativada.");
    }

    private void GarantirReferencias()
    {
        if (baseArea == null || soldadoSpawn == null || tankSpawn == null)
            AtualizarReferencias();
    }

    // =========================================================
    // BASES — CRIAR
    // =========================================================

    public bool CriarBaseSoldado()
    {
        GarantirReferencias();
        if (baseArea == null) return false;
        bool criada = baseArea.TentarCriarBasePorIndice(0);
        if (criada) Debug.Log("[RoboCriar] 🏗️ Base Soldado criada");
        return criada;
    }

    public bool CriarBaseTank()
    {
        GarantirReferencias();
        if (baseArea == null) return false;
        bool criada = baseArea.TentarCriarBasePorIndice(1);
        if (criada) Debug.Log("[RoboCriar] 🏗️ Base Tank criada");
        return criada;
    }

    public bool CriarTorreTerra()
    {
        GarantirReferencias();
        if (baseArea == null) return false;
        bool criada = baseArea.TentarCriarBasePorIndice(3);
        if (criada) Debug.Log("[RoboCriar] 🏗️ Torre Terra criada");
        return criada;
    }

    // =========================================================
    // BASES — CONSULTAS
    // =========================================================

    public bool BaseSoldadoExiste()
    {
        GarantirReferencias();
        return baseArea != null && baseArea.ExisteBasePorIndice(0);
    }

    public bool BaseTankExiste()
    {
        GarantirReferencias();
        return baseArea != null && baseArea.ExisteBasePorIndice(1);
    }

    public bool TorreTerraExiste()
    {
        GarantirReferencias();
        return baseArea != null && baseArea.ExisteBasePorIndice(3);
    }

    public int ContarBaseSoldado()
    {
        GarantirReferencias();
        return baseArea != null ? baseArea.ContarBasesPorIndice(0) : 0;
    }

    public int ContarBaseTank()
    {
        GarantirReferencias();
        return baseArea != null ? baseArea.ContarBasesPorIndice(1) : 0;
    }

    public int ContarTorreTerra()
    {
        GarantirReferencias();
        return baseArea != null ? baseArea.ContarBasesPorIndice(3) : 0;
    }

    public Transform[] ObterBasesSoldado()
    {
        GarantirReferencias();
        return baseArea != null ? baseArea.ObterBasesPorIndice(0) : new Transform[0];
    }

    public Transform[] ObterBasesTank()
    {
        GarantirReferencias();
        return baseArea != null ? baseArea.ObterBasesPorIndice(1) : new Transform[0];
    }

    // =========================================================
    // SOLDADO — UNIDADES
    // =========================================================

    public bool PodeCriarColetor() =>
        soldadoSpawn != null && soldadoSpawn.PodeCriarColetor();

    public bool CriarColetor()
    {
        GarantirReferencias();
        if (soldadoSpawn == null) return false;
        return soldadoSpawn.TentarCriarColetor();
    }

    public bool PodeCriarSoldado() =>
        soldadoSpawn != null && soldadoSpawn.PodeCriarSoldado();

    public bool CriarSoldado()
    {
        GarantirReferencias();
        if (soldadoSpawn == null) return false;
        return soldadoSpawn.TentarCriarSoldado();
    }

    public bool PodeCriarGuerreiro() =>
        soldadoSpawn != null && soldadoSpawn.PodeCriarGuerreiro();

    public bool CriarGuerreiro()
    {
        GarantirReferencias();
        if (soldadoSpawn == null) return false;
        return soldadoSpawn.TentarCriarGuerreiro();
    }

    // =========================================================
    // SOLDADO — UNIDADES NA BASE (usado pelo RoboIA)
    // =========================================================

    public bool PodeCriarColetorNaBase(Transform base_)
    {
        GarantirReferencias();
        return soldadoSpawn != null && base_ != null &&
               soldadoSpawn.PodeCriarColetorNaBase(base_);
    }

    public bool CriarColetorNaBase(Transform base_)
    {
        GarantirReferencias();
        if (soldadoSpawn == null || base_ == null) return false;
        return soldadoSpawn.TentarCriarColetorNaBase(base_);
    }

    public bool PodeCriarSoldadoNaBase(Transform base_)
    {
        GarantirReferencias();
        return soldadoSpawn != null && base_ != null &&
               soldadoSpawn.PodeCriarSoldadoNaBase(base_);
    }

    public bool CriarSoldadoNaBase(Transform base_)
    {
        GarantirReferencias();
        if (soldadoSpawn == null || base_ == null) return false;
        return soldadoSpawn.TentarCriarSoldadoNaBase(base_);
    }

    public bool PodeCriarGuerreiroNaBase(Transform base_)
    {
        GarantirReferencias();
        return soldadoSpawn != null && base_ != null &&
               soldadoSpawn.PodeCriarGuerreiroNaBase(base_);
    }

    public bool CriarGuerreiroNaBase(Transform base_)
    {
        GarantirReferencias();
        if (soldadoSpawn == null || base_ == null) return false;
        return soldadoSpawn.TentarCriarGuerreiroNaBase(base_);
    }

    // =========================================================
    // TANK — UNIDADES
    // =========================================================

    public bool PodeCriarTank() =>
        tankSpawn != null && tankSpawn.PodeCriarTank();

    public bool CriarTank()
    {
        GarantirReferencias();
        if (tankSpawn == null) return false;
        return tankSpawn.TentarCriarTank();
    }

    public bool PodeCriarTankNaBase(Transform base_)
    {
        GarantirReferencias();
        return tankSpawn != null && base_ != null &&
               tankSpawn.PodeCriarTankNaBase(base_);
    }

    public bool CriarTankNaBase(Transform base_)
    {
        GarantirReferencias();
        if (tankSpawn == null || base_ == null) return false;
        return tankSpawn.TentarCriarTankNaBase(base_);
    }

    // =========================================================
    // PONTO DE SPAWN — movido do RoboIA para cá
    // =========================================================

    public Transform EncontrarPontoSpawn(Transform baseTransform, string tipo)
    {
        if (baseTransform == null) return null;

        string[] nomesPossiveis = {
            "Spawn" + tipo, "PontoSpawn" + tipo, "SpawnPoint" + tipo,
            "Spawn_" + tipo, "Ponto_Spawn_" + tipo,
            "Spawn", "PontoSpawn", "SpawnPoint"
        };

        foreach (string nome in nomesPossiveis)
        {
            Transform encontrado = baseTransform.Find(nome);
            if (encontrado != null) return encontrado;
        }

        Transform spawnRecursivo = ProcurarSpawnRecursivo(baseTransform);
        if (spawnRecursivo != null) return spawnRecursivo;

        Debug.LogWarning($"[RoboCriar] ⚠️ Nenhum ponto de spawn em {baseTransform.name}. Usando posição da base.");
        return baseTransform;
    }

    private Transform ProcurarSpawnRecursivo(Transform pai)
    {
        if (pai == null) return null;

        foreach (Transform filho in pai)
        {
            if (filho == null) continue;
            string nomeLower = filho.name.ToLower();
            if (nomeLower.Contains("spawn") || nomeLower.Contains("ponto"))
                return filho;

            Transform encontrado = ProcurarSpawnRecursivo(filho);
            if (encontrado != null) return encontrado;
        }
        return null;
    }
}