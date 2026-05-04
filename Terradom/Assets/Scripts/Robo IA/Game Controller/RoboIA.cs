using UnityEngine;

[DisallowMultipleComponent]
public class RoboIA : MonoBehaviour
{
    private BaseSoldadoIA baseSoldadoIA;
    private BaseTankIA baseTankIA;

    private float proximoSpawn = 0f;
    private float intervaloSpawn = 2f;

    // 0 = Coletor, 1 = Soldado, 2 = Guerreiro, 3 = Tank
    private int etapaCriacao = 0;

    private void Start()
    {
        // Inicializa BaseSoldadoIA
        baseSoldadoIA = GetComponent<BaseSoldadoIA>();
        if (baseSoldadoIA == null)
        {
            baseSoldadoIA = gameObject.AddComponent<BaseSoldadoIA>();
        }
        baseSoldadoIA.AtualizarReferencias();

        // Inicializa BaseTankIA
        baseTankIA = GetComponent<BaseTankIA>();
        if (baseTankIA == null)
        {
            baseTankIA = gameObject.AddComponent<BaseTankIA>();
        }
        baseTankIA.AtualizarReferencias();

        Debug.Log("=== INICIANDO ROBO IA ===");

        // Força a criação das bases imediatamente
        bool soldadoCriado = baseSoldadoIA.CriarBaseSoldado();
        Debug.Log("[RoboIA] Base Soldado criada: " + soldadoCriado);
    
        bool tankCriado = baseTankIA.CriarBaseTank();
        Debug.Log("[RoboIA] Base Tank criada: " + tankCriado);
    }

    private void Update()
    {
        if (baseSoldadoIA == null || baseTankIA == null) return;

        if (Time.time < proximoSpawn) return;

        proximoSpawn = Time.time + intervaloSpawn;

        baseSoldadoIA.AtualizarReferencias();
        baseTankIA.AtualizarReferencias();

        // Verifica se as bases existem
        if (!baseSoldadoIA.BaseSoldadoExiste())
        {
            Debug.Log("[RoboIA] Base de soldado não existe ainda");
            return;
        }

        if (!baseTankIA.BaseTankExiste())
        {
            Debug.Log("[RoboIA] Base de tank não existe ainda");
            return;
        }

        bool criado = false;

        switch (etapaCriacao)
        {
            case 0:
                if (baseSoldadoIA.PodeCriarColetor())
                {
                    criado = baseSoldadoIA.CriarColetor();
                    if (criado) Debug.Log("[RoboIA] Coletor criado");
                }
                break;

            case 1:
                if (baseSoldadoIA.PodeCriarSoldado())
                {
                    criado = baseSoldadoIA.CriarSoldado();
                    if (criado) Debug.Log("[RoboIA] Soldado criado");
                }
                break;

            case 2:
                if (baseSoldadoIA.PodeCriarGuerreiro())
                {
                    criado = baseSoldadoIA.CriarGuerreiro();
                    if (criado) Debug.Log("[RoboIA] Guerreiro criado");
                }
                break;

            case 3:
                if (baseTankIA.PodeCriarTank())
                {
                    criado = baseTankIA.CriarTank();
                    if (criado) Debug.Log("[RoboIA] Tank criado");
                }
                break;
        }

        if (criado)
        {
            etapaCriacao++;

            if (etapaCriacao > 3)
                etapaCriacao = 0;
        }
        else
        {
            Debug.Log("[RoboIA] Falhou etapa: " + etapaCriacao);
        }
    }
}