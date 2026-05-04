using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class BaseVidaIA : MonoBehaviour
{
    [System.Serializable]
    private class DanoAutomaticoPorTag
    {
        public string tagAtacante = "Azul"; // inimigo da IA
        public int dano = 1;
        public bool destruirAtacanteAposDano = true;
    }

    [Header("Vida da base IA")]
    [SerializeField] private int vidaMaxima = 10;
    [SerializeField] private int vidaAtual = 10;
    [SerializeField] private bool reiniciarVidaAoIniciar = true;
    [SerializeField] private bool destruirAoChegarEmZero = true;

    [Header("Tags que podem causar dano na IA")]
    [SerializeField] private bool exigirTagPermitidaParaReceberDano = true;
    [SerializeField] private string[] tagsQuePodemCausarDano =
    {
        "Azul",   // jogador
        "Bala"
    };

    [Header("Dano automatico")]
    [SerializeField] private bool receberDanoAutomaticoPorImpacto = true;
    [SerializeField] private bool procurarDanoNoAtacante = true;
    [SerializeField] private bool usarDanoPorTagSeNaoEncontrarDanoNoAtacante = true;

    [SerializeField] private DanoAutomaticoPorTag[] danosAutomaticosPorTag =
    {
        new DanoAutomaticoPorTag
        {
            tagAtacante = "Bala",
            dano = 1,
            destruirAtacanteAposDano = true
        }
    };

    [Header("Colliders filhos")]
    [SerializeField] private bool receberImpactoEmCollidersFilhos = true;

    public int VidaAtual => vidaAtual;

    private readonly Dictionary<int, float> ultimoDano = new Dictionary<int, float>();
    private const float cooldown = 0.05f;

    private void Awake()
    {
        vidaMaxima = Mathf.Max(1, vidaMaxima);

        if (reiniciarVidaAoIniciar)
            vidaAtual = vidaMaxima;
    }

    private void OnEnable()
    {
        PrepararCollidersFilhos();
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessarImpacto(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessarImpacto(other);
    }

    public void ReceberDano(int dano)
    {
        AplicarDano(dano, null);
    }

    public void ReceberDano(int dano, GameObject atacante)
    {
        if (!PodeReceberDano(atacante))
            return;

        AplicarDano(dano, atacante);
    }

    private bool PodeReceberDano(GameObject atacante)
    {
        if (!exigirTagPermitidaParaReceberDano)
            return true;

        if (atacante == null)
            return false;

        foreach (string tag in tagsQuePodemCausarDano)
        {
            if (atacante.CompareTag(tag))
                return true;
        }

        return false;
    }

    private void ProcessarImpacto(Collider colisor)
    {
        if (!receberDanoAutomaticoPorImpacto || colisor == null)
            return;

        GameObject atacante = colisor.attachedRigidbody != null
            ? colisor.attachedRigidbody.gameObject
            : colisor.gameObject;

        if (!PodeReceberDano(atacante))
            return;

        int id = atacante.GetInstanceID();

        if (ultimoDano.TryGetValue(id, out float tempo))
        {
            if (Time.time - tempo < cooldown)
                return;
        }

        int dano = 0;
        bool destruir = false;

        if (!TentarObterDano(atacante, out dano, out destruir))
            return;

        ultimoDano[id] = Time.time;

        AplicarDano(dano, atacante);

        if (destruir)
            Destroy(atacante);
    }

    private bool TentarObterDano(GameObject atacante, out int dano, out bool destruir)
    {
        dano = 0;
        destruir = false;

        // tenta pegar dano do script
        Component[] comps = atacante.GetComponentsInChildren<Component>();

        foreach (var c in comps)
        {
            if (TentarLerDano(c, out dano))
                return true;
        }

        // fallback por tag
        foreach (var config in danosAutomaticosPorTag)
        {
            if (atacante.CompareTag(config.tagAtacante))
            {
                dano = config.dano;
                destruir = config.destruirAtacanteAposDano;
                return true;
            }
        }

        return false;
    }

    private bool TentarLerDano(Component c, out int dano)
    {
        dano = 0;

        if (c == null) return false;

        string[] nomes =
        {
            "dano",
            "Dano",
            "damage",
            "Damage"
        };

        foreach (var nome in nomes)
        {
            var campo = c.GetType().GetField(nome,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (campo != null)
            {
                object valor = campo.GetValue(c);

                if (valor is int i)
                {
                    dano = i;
                    return true;
                }
            }
        }

        return false;
    }

    private void AplicarDano(int dano, GameObject atacante)
    {
        if (dano <= 0 || vidaAtual <= 0)
            return;

        vidaAtual -= dano;
        vidaAtual = Mathf.Max(0, vidaAtual);

        if (vidaAtual <= 0)
            DestruirBase();
    }

    private void DestruirBase()
    {
        if (destruirAoChegarEmZero)
            Destroy(gameObject);
    }

    private void PrepararCollidersFilhos()
    {
        if (!receberImpactoEmCollidersFilhos)
            return;

        Collider[] cols = GetComponentsInChildren<Collider>();

        foreach (var col in cols)
        {
            if (col.transform == transform)
                continue;

            BaseVidaColliderFilhoIA ponte =
                col.gameObject.GetComponent<BaseVidaColliderFilhoIA>();

            if (ponte == null)
                ponte = col.gameObject.AddComponent<BaseVidaColliderFilhoIA>();

            ponte.Configurar(this);
        }
    }

    private void ReceberImpactoFilho(Collider col)
    {
        ProcessarImpacto(col);
    }

    private class BaseVidaColliderFilhoIA : MonoBehaviour
    {
        private BaseVidaIA baseIA;

        public void Configurar(BaseVidaIA b)
        {
            baseIA = b;
        }

        private void OnCollisionEnter(Collision collision)
        {
            baseIA?.ReceberImpactoFilho(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            baseIA?.ReceberImpactoFilho(other);
        }
    }
}