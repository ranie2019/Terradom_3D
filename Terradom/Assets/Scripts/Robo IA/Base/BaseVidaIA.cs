using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class BaseVidaIA : MonoBehaviour
{
    [System.Serializable]
    private class DanoAutomaticoPorTag
    {
        public string tagAtacante = "Azul";
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
        "Azul",
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

    [Header("Barra de Vida")]
    [SerializeField] private BarraVidaUI barraVidaUI;

    [Header("Colliders filhos")]
    [SerializeField] private bool receberImpactoEmCollidersFilhos = true;

    [Header("Áudio")]
    [Tooltip("Som tocado quando a base IA é destruída (vida chega a zero).")]
    [SerializeField] private AudioClip clipDestruicao;

    [Range(0f, 1f)]
    [SerializeField] private float volumeDestruicao = 1f;

    public int VidaAtual => vidaAtual;

    private readonly Dictionary<int, float> ultimoDano = new Dictionary<int, float>();
    private const float cooldown = 0.05f;

    private void Awake()
    {
        vidaMaxima = Mathf.Max(1, vidaMaxima);

        if (reiniciarVidaAoIniciar)
            vidaAtual = vidaMaxima;

        if (barraVidaUI != null)
        {
            barraVidaUI.Configurar(vidaMaxima);
            barraVidaUI.AtualizarVida(vidaAtual);
        }
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

    // =========================================================
    // API PÚBLICA DE DANO
    // =========================================================

    public void ReceberDano(int dano)
    {
        AplicarDano(dano, null);
    }

    public void ReceberDano(int dano, GameObject atacante)
    {
        if (!PodeReceberDano(atacante)) return;
        AplicarDano(dano, atacante);
    }

    private bool PodeReceberDano(GameObject atacante)
    {
        if (atacante != null
            && TentarObterEquipeDeProjetil(atacante, out string equipeAtacante)
            && !string.IsNullOrEmpty(equipeAtacante))
        {
            string equipeDaBase = ObterTagEquipe(transform);
            if (!string.IsNullOrEmpty(equipeDaBase) && equipeDaBase == equipeAtacante)
                return false;
        }

        if (!exigirTagPermitidaParaReceberDano) return true;
        if (atacante == null) return false;

        foreach (string tag in tagsQuePodemCausarDano)
        {
            if (atacante.CompareTag(tag)) return true;
        }

        return false;
    }

    // =========================================================
    // IMPACTO AUTOMÁTICO
    // =========================================================

    private void ProcessarImpacto(Collider colisor)
    {
        if (!receberDanoAutomaticoPorImpacto || colisor == null) return;

        GameObject atacante = colisor.attachedRigidbody != null
            ? colisor.attachedRigidbody.gameObject
            : colisor.gameObject;

        if (!PodeReceberDano(atacante)) return;

        int id = atacante.GetInstanceID();

        if (ultimoDano.TryGetValue(id, out float tempo))
        {
            if (Time.time - tempo < cooldown) return;
        }

        if (!TentarObterDano(atacante, out int dano, out bool destruir)) return;

        ultimoDano[id] = Time.time;
        AplicarDano(dano, atacante);

        if (destruir) Destroy(atacante);
    }

    private bool TentarObterDano(GameObject atacante, out int dano, out bool destruir)
    {
        dano    = 0;
        destruir = false;

        if (procurarDanoNoAtacante)
        {
            Component[] comps = atacante.GetComponentsInChildren<Component>();
            foreach (var c in comps)
            {
                if (c != null && TentarLerDano(c, out dano))
                {
                    destruir = true;
                    return true;
                }
            }
        }

        if (usarDanoPorTagSeNaoEncontrarDanoNoAtacante || !procurarDanoNoAtacante)
        {
            foreach (var config in danosAutomaticosPorTag)
            {
                if (atacante.CompareTag(config.tagAtacante))
                {
                    dano     = config.dano;
                    destruir = config.destruirAtacanteAposDano;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TentarLerDano(Component c, out int dano)
    {
        dano = 0;
        if (c == null) return false;

        string[] nomes = { "dano", "Dano", "damage", "Damage" };

        foreach (var nome in nomes)
        {
            FieldInfo campo = c.GetType().GetField(nome,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (campo != null)
            {
                object valor = campo.GetValue(c);
                if (valor is int i) { dano = i; return true; }
            }
        }

        return false;
    }

    // =========================================================
    // APLICAR DANO E DESTRUIÇÃO
    // =========================================================

    private void AplicarDano(int dano, GameObject atacante)
    {
        if (dano <= 0 || vidaAtual <= 0) return;

        vidaAtual -= dano;
        vidaAtual  = Mathf.Max(0, vidaAtual);

        if (barraVidaUI != null)
            barraVidaUI.AtualizarVida(vidaAtual);

        if (vidaAtual <= 0)
            DestruirBase();
    }

    private void DestruirBase()
    {
        if (!destruirAoChegarEmZero) return;

        TocarSomDestruicao();
        Destroy(gameObject);
    }

    private void TocarSomDestruicao()
    {
        if (clipDestruicao == null) return;

        GameObject tempAudio = new GameObject("AudioDestruicaoBaseIA");
        tempAudio.transform.position = transform.position;

        AudioSource fonte = tempAudio.AddComponent<AudioSource>();
        fonte.clip         = clipDestruicao;
        fonte.volume       = volumeDestruicao;
        fonte.spatialBlend = 0f;
        fonte.playOnAwake  = false;
        fonte.Play();

        Destroy(tempAudio, clipDestruicao.length + 0.1f);
    }

    // =========================================================
    // HELPERS DE EQUIPE
    // =========================================================

    private static bool TentarObterEquipeDeProjetil(GameObject objeto, out string equipe)
    {
        equipe = string.Empty;
        if (objeto == null) return false;

        ProjetilDistancia bala = objeto.GetComponentInParent<ProjetilDistancia>();
        if (bala == null) bala  = objeto.GetComponentInChildren<ProjetilDistancia>(true);
        if (bala != null && !string.IsNullOrEmpty(bala.TagEquipeDona))
        {
            equipe = bala.TagEquipeDona;
            return true;
        }

        Missel missel = objeto.GetComponentInParent<Missel>();
        if (missel == null) missel = objeto.GetComponentInChildren<Missel>(true);
        if (missel != null && !string.IsNullOrEmpty(missel.TagEquipeDona))
        {
            equipe = missel.TagEquipeDona;
            return true;
        }

        return false;
    }

    private static string ObterTagEquipe(Transform origem)
    {
        Transform atual = origem;
        while (atual != null)
        {
            if (atual.CompareTag("Azul") || atual.CompareTag("Vermelho") || atual.CompareTag("Verde"))
                return atual.tag;
            atual = atual.parent;
        }
        return string.Empty;
    }

    // =========================================================
    // COLLIDERS FILHOS
    // =========================================================

    private void PrepararCollidersFilhos()
    {
        if (!receberImpactoEmCollidersFilhos) return;

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var col in cols)
        {
            if (col.transform == transform) continue;

            BaseVidaColliderFilhoIA ponte = col.gameObject.GetComponent<BaseVidaColliderFilhoIA>();
            if (ponte == null) ponte = col.gameObject.AddComponent<BaseVidaColliderFilhoIA>();
            ponte.Configurar(this);
        }
    }

    private void ReceberImpactoFilho(Collider col)
    {
        ProcessarImpacto(col);
    }

    // =========================================================
    // CLASSE INTERNA — BRIDGE DE COLLIDER
    // =========================================================

    private class BaseVidaColliderFilhoIA : MonoBehaviour
    {
        private BaseVidaIA baseIA;

        public void Configurar(BaseVidaIA b) { baseIA = b; }

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