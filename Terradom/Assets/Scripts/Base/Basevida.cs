using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class BaseVida : MonoBehaviour
{
    [System.Serializable]
    private class DanoAutomaticoPorTag
    {
        public string tagAtacante = "Bala";
        public int dano = 1;
        public bool destruirAtacanteAposDano = true;
    }

    [Header("Vida da base")]
    [SerializeField] private int vidaMaxima = 10;
    [SerializeField] private int vidaAtual = 10;
    [SerializeField] private bool reiniciarVidaAoIniciar = true;
    [SerializeField] private bool destruirAoChegarEmZero = true;

    [Header("Tags que podem causar dano")]
    [SerializeField] private bool exigirTagPermitidaParaReceberDano = true;
    [SerializeField] private string[] tagsQuePodemCausarDano =
    {
        "Vermelho",
        "Inimigo",
        "Bala"
    };

    [Header("Dano automatico por bala / projetil")]
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

    [Header("Colliders em filhos")]
    [SerializeField] private bool receberImpactoEmCollidersFilhos = true;

    [Header("Debug")]
    [SerializeField] private bool mostrarDebugDano = false;

    public int VidaMaxima => vidaMaxima;
    public int VidaAtual => vidaAtual;
    public bool EstaViva => vidaAtual > 0;

    private readonly Dictionary<int, float> ultimoDanoAutomaticoPorAtacante = new Dictionary<int, float>();
    private const float CooldownMesmoAtacante = 0.05f;

    private void Awake()
    {
        vidaMaxima = Mathf.Max(1, vidaMaxima);

        if (reiniciarVidaAoIniciar)
            vidaAtual = vidaMaxima;
        else
            vidaAtual = Mathf.Clamp(vidaAtual, 0, vidaMaxima);
    }

    private void OnEnable()
    {
        PrepararCollidersFilhos();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
            return;

        ProcessarImpactoAutomatico(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessarImpactoAutomatico(other);
    }

    /// <summary>
    /// Use este metodo quando o dano ja foi validado por outro script.
    /// Exemplo: bala, soldado, tanque ou ataque corpo a corpo chama ReceberDano(15).
    /// </summary>
    public void ReceberDano(int dano)
    {
        AplicarDano(dano, null);
    }

    /// <summary>
    /// Use este metodo quando quiser validar a Tag do atacante antes de aplicar o dano.
    /// Cada inimigo informa o proprio dano. A base nao usa mais dano fixo por colisao.
    /// </summary>
    public void ReceberDano(int dano, GameObject atacante)
    {
        if (!PodeReceberDanoDe(atacante))
            return;

        AplicarDano(dano, atacante);
    }

    /// <summary>
    /// Versao pratica para scripts que possuem Component, Collider, Rigidbody, MonoBehaviour etc.
    /// </summary>
    public void ReceberDano(int dano, Component atacante)
    {
        GameObject objetoAtacante = atacante != null ? atacante.gameObject : null;
        ReceberDano(dano, objetoAtacante);
    }

    /// <summary>
    /// Use quando o script de ataque trabalha diretamente com string de Tag.
    /// </summary>
    public void ReceberDanoPorTag(int dano, string tagAtacante)
    {
        if (!TagEstaPermitida(tagAtacante))
            return;

        AplicarDano(dano, null);
    }

    public bool PodeReceberDanoDe(GameObject atacante)
    {
        if (!exigirTagPermitidaParaReceberDano)
            return true;

        if (atacante == null)
            return false;

        return ObjetoOuPaisTemTagPermitida(atacante.transform);
    }

    public void Curar(int valor)
    {
        if (valor <= 0 || vidaAtual <= 0)
            return;

        vidaAtual = Mathf.Clamp(vidaAtual + valor, 0, vidaMaxima);
    }

    public void RestaurarVidaTotal()
    {
        vidaAtual = vidaMaxima;
    }

    public int GetVidaAtual()
    {
        return vidaAtual;
    }

    public int GetVidaMaxima()
    {
        return vidaMaxima;
    }

    private void ProcessarImpactoAutomatico(Collider colisorAtacante)
    {
        if (!receberDanoAutomaticoPorImpacto)
            return;

        if (colisorAtacante == null || vidaAtual <= 0)
            return;

        if (colisorAtacante.transform == transform || colisorAtacante.transform.IsChildOf(transform))
            return;

        GameObject atacantePrincipal = ObterObjetoPrincipalDoAtacante(colisorAtacante);

        if (atacantePrincipal == null)
            return;

        if (!PodeReceberDanoDe(atacantePrincipal))
            return;

        int idAtacante = atacantePrincipal.GetInstanceID();

        if (ultimoDanoAutomaticoPorAtacante.TryGetValue(idAtacante, out float ultimoTempo))
        {
            if (Time.time - ultimoTempo < CooldownMesmoAtacante)
                return;
        }

        if (!TentarObterDanoDoImpacto(colisorAtacante, atacantePrincipal, out int dano, out bool destruirAtacante))
            return;

        if (dano <= 0)
            return;

        ultimoDanoAutomaticoPorAtacante[idAtacante] = Time.time;
        AplicarDano(dano, atacantePrincipal);

        if (destruirAtacante)
            Destroy(atacantePrincipal);
    }

    private bool TentarObterDanoDoImpacto(Collider colisorAtacante, GameObject atacantePrincipal, out int dano, out bool destruirAtacante)
    {
        dano = 0;
        destruirAtacante = false;

        if (procurarDanoNoAtacante && TentarLerDanoDoAtacante(colisorAtacante, atacantePrincipal, out dano))
        {
            destruirAtacante = DeveDestruirAtacantePorTag(atacantePrincipal.transform);
            return true;
        }

        if (usarDanoPorTagSeNaoEncontrarDanoNoAtacante && TentarObterDanoAutomaticoPorTag(atacantePrincipal.transform, out dano, out destruirAtacante))
            return true;

        return false;
    }

    private bool TentarLerDanoDoAtacante(Collider colisorAtacante, GameObject atacantePrincipal, out int dano)
    {
        dano = 0;

        if (atacantePrincipal == null)
            return false;

        Component[] componentes = atacantePrincipal.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < componentes.Length; i++)
        {
            if (TentarLerDanoDeComponente(componentes[i], out dano))
                return true;
        }

        if (colisorAtacante != null && colisorAtacante.gameObject != atacantePrincipal)
        {
            Component[] componentesDoCollider = colisorAtacante.GetComponents<Component>();

            for (int i = 0; i < componentesDoCollider.Length; i++)
            {
                if (TentarLerDanoDeComponente(componentesDoCollider[i], out dano))
                    return true;
            }
        }

        return false;
    }

    private bool TentarLerDanoDeComponente(Component componente, out int dano)
    {
        dano = 0;

        if (componente == null)
            return false;

        if (componente is BaseVida)
            return false;

        if (componente is BaseVidaColliderFilho)
            return false;

        System.Type tipo = componente.GetType();

        string[] nomesPossiveis =
        {
            "dano",
            "Dano",
            "danoBala",
            "DanoBala",
            "danoAtaque",
            "DanoAtaque",
            "danoCausado",
            "DanoCausado",
            "danoAoAcertar",
            "DanoAoAcertar",
            "valorDano",
            "ValorDano",
            "damage",
            "Damage"
        };

        for (int i = 0; i < nomesPossiveis.Length; i++)
        {
            if (TentarLerCampoOuPropriedadeInteira(tipo, componente, nomesPossiveis[i], out dano))
                return true;
        }

        return false;
    }

    private bool TentarLerCampoOuPropriedadeInteira(System.Type tipo, object instancia, string nome, out int valor)
    {
        valor = 0;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        FieldInfo campo = tipo.GetField(nome, flags);

        if (campo != null && TentarConverterParaInteiro(campo.GetValue(instancia), out valor))
            return true;

        PropertyInfo propriedade = tipo.GetProperty(nome, flags);

        if (propriedade != null && propriedade.CanRead && TentarConverterParaInteiro(propriedade.GetValue(instancia, null), out valor))
            return true;

        return false;
    }

    private bool TentarConverterParaInteiro(object valorOriginal, out int valor)
    {
        valor = 0;

        if (valorOriginal == null)
            return false;

        if (valorOriginal is int inteiro)
        {
            valor = inteiro;
            return valor > 0;
        }

        if (valorOriginal is float numeroFloat)
        {
            valor = Mathf.RoundToInt(numeroFloat);
            return valor > 0;
        }

        if (valorOriginal is double numeroDouble)
        {
            valor = Mathf.RoundToInt((float)numeroDouble);
            return valor > 0;
        }

        return false;
    }

    private bool TentarObterDanoAutomaticoPorTag(Transform atacante, out int dano, out bool destruirAtacante)
    {
        dano = 0;
        destruirAtacante = false;

        if (atacante == null || danosAutomaticosPorTag == null)
            return false;

        Transform atual = atacante;

        while (atual != null)
        {
            for (int i = 0; i < danosAutomaticosPorTag.Length; i++)
            {
                DanoAutomaticoPorTag configuracao = danosAutomaticosPorTag[i];

                if (configuracao == null)
                    continue;

                if (string.IsNullOrWhiteSpace(configuracao.tagAtacante))
                    continue;

                if (atual.CompareTag(configuracao.tagAtacante))
                {
                    dano = Mathf.Max(0, configuracao.dano);
                    destruirAtacante = configuracao.destruirAtacanteAposDano;
                    return dano > 0;
                }
            }

            atual = atual.parent;
        }

        return false;
    }

    private bool DeveDestruirAtacantePorTag(Transform atacante)
    {
        if (atacante == null || danosAutomaticosPorTag == null)
            return false;

        Transform atual = atacante;

        while (atual != null)
        {
            for (int i = 0; i < danosAutomaticosPorTag.Length; i++)
            {
                DanoAutomaticoPorTag configuracao = danosAutomaticosPorTag[i];

                if (configuracao == null)
                    continue;

                if (string.IsNullOrWhiteSpace(configuracao.tagAtacante))
                    continue;

                if (atual.CompareTag(configuracao.tagAtacante))
                    return configuracao.destruirAtacanteAposDano;
            }

            atual = atual.parent;
        }

        return false;
    }

    private GameObject ObterObjetoPrincipalDoAtacante(Collider colisorAtacante)
    {
        if (colisorAtacante == null)
            return null;

        if (colisorAtacante.attachedRigidbody != null)
            return colisorAtacante.attachedRigidbody.gameObject;

        return colisorAtacante.gameObject;
    }

    private void PrepararCollidersFilhos()
    {
        if (!receberImpactoEmCollidersFilhos)
            return;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];

            if (col == null)
                continue;

            if (col.transform == transform)
                continue;

            BaseVidaColliderFilho ponte = col.GetComponent<BaseVidaColliderFilho>();

            if (ponte == null)
                ponte = col.gameObject.AddComponent<BaseVidaColliderFilho>();

            ponte.Configurar(this);
        }
    }

    private void ReceberImpactoDoColliderFilho(Collider colisorAtacante)
    {
        ProcessarImpactoAutomatico(colisorAtacante);
    }

    private void AplicarDano(int dano, GameObject atacante)
    {
        if (dano <= 0 || vidaAtual <= 0)
            return;

        vidaAtual -= dano;
        vidaAtual = Mathf.Max(vidaAtual, 0);

        if (mostrarDebugDano)
        {
            string nomeAtacante = atacante != null ? atacante.name : "Sem atacante informado";
            Debug.Log($"[BaseVida] {name} recebeu {dano} de dano. Atacante: {nomeAtacante}. Vida: {vidaAtual}/{vidaMaxima}");
        }

        if (vidaAtual <= 0)
            DestruirBase();
    }

    private void DestruirBase()
    {
        if (!destruirAoChegarEmZero)
            return;

        Destroy(gameObject);
    }

    private bool ObjetoOuPaisTemTagPermitida(Transform alvo)
    {
        Transform atual = alvo;

        while (atual != null)
        {
            if (TagEstaPermitida(atual.gameObject.tag))
                return true;

            atual = atual.parent;
        }

        return false;
    }

    private bool TagEstaPermitida(string tagParaTestar)
    {
        if (string.IsNullOrWhiteSpace(tagParaTestar))
            return false;

        if (tagsQuePodemCausarDano == null || tagsQuePodemCausarDano.Length == 0)
            return false;

        for (int i = 0; i < tagsQuePodemCausarDano.Length; i++)
        {
            string tagPermitida = tagsQuePodemCausarDano[i];

            if (string.IsNullOrWhiteSpace(tagPermitida))
                continue;

            if (tagParaTestar == tagPermitida)
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        vidaMaxima = Mathf.Max(1, vidaMaxima);
        vidaAtual = Mathf.Clamp(vidaAtual, 0, vidaMaxima);

        if (danosAutomaticosPorTag != null)
        {
            for (int i = 0; i < danosAutomaticosPorTag.Length; i++)
            {
                if (danosAutomaticosPorTag[i] == null)
                    continue;

                danosAutomaticosPorTag[i].dano = Mathf.Max(0, danosAutomaticosPorTag[i].dano);
            }
        }
    }

    private class BaseVidaColliderFilho : MonoBehaviour
    {
        private BaseVida baseVida;

        public void Configurar(BaseVida novaBaseVida)
        {
            baseVida = novaBaseVida;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (baseVida == null || collision == null)
                return;

            baseVida.ReceberImpactoDoColliderFilho(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (baseVida == null)
                return;

            baseVida.ReceberImpactoDoColliderFilho(other);
        }
    }
}
