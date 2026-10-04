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

    [Header("Barra de Vida")]
    [SerializeField] private BarraVidaUI barraVidaUI;

    // =========================================================
    // ÁUDIO DE DESTRUIÇÃO
    // Som tocado quando a vida chega a zero e a base é destruída.
    // Usa AudioSource 2D (spatialBlend = 0) em objeto temporário
    // para não ser cortado junto com o Destroy(gameObject).
    // =========================================================
    [Header("Áudio")]
    [Tooltip("Som tocado quando a base é destruída (vida chega a zero).")]
    [SerializeField] private AudioClip clipDestruicao;

    [Range(0f, 1f)]
    [SerializeField] private float volumeDestruicao = 1f;

    public int VidaMaxima => vidaMaxima;
    public int VidaAtual  => vidaAtual;
    public bool EstaViva  => vidaAtual > 0;

    private readonly Dictionary<int, float> ultimoDanoAutomaticoPorAtacante = new Dictionary<int, float>();
    private const float CooldownMesmoAtacante = 0.05f;

    private void Awake()
    {
        vidaMaxima = Mathf.Max(1, vidaMaxima);

        if (reiniciarVidaAoIniciar)
            vidaAtual = vidaMaxima;
        else
            vidaAtual = Mathf.Clamp(vidaAtual, 0, vidaMaxima);

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
        if (collision == null) return;
        ProcessarImpactoAutomatico(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessarImpactoAutomatico(other);
    }

    // =========================================================
    // API PÚBLICA DE DANO
    // =========================================================

    /// <summary>Use quando o dano já foi validado por outro script.</summary>
    public void ReceberDano(int dano)
    {
        AplicarDano(dano, null);
    }

    /// <summary>Use quando quiser validar a tag do atacante antes de aplicar o dano.</summary>
    public void ReceberDano(int dano, GameObject atacante)
    {
        if (!PodeReceberDanoDe(atacante)) return;
        AplicarDano(dano, atacante);
    }

    /// <summary>Versão prática para scripts que possuem Component, Collider, Rigidbody, etc.</summary>
    public void ReceberDano(int dano, Component atacante)
    {
        GameObject objetoAtacante = atacante != null ? atacante.gameObject : null;
        ReceberDano(dano, objetoAtacante);
    }

    /// <summary>Use quando o script de ataque trabalha diretamente com string de Tag.</summary>
    public void ReceberDanoPorTag(int dano, string tagAtacante)
    {
        if (!TagEstaPermitida(tagAtacante)) return;
        AplicarDano(dano, null);
    }

    public bool PodeReceberDanoDe(GameObject atacante)
    {
        if (atacante != null)
        {
            string equipeAtacante;
            if (!TentarObterEquipeDeProjetil(atacante, out equipeAtacante)
                || string.IsNullOrEmpty(equipeAtacante))
                equipeAtacante = ObterTagEquipe(atacante.transform);

            string equipeDaBase = ObterTagEquipe(transform);
            if (!string.IsNullOrEmpty(equipeDaBase) && equipeDaBase == equipeAtacante)
                return false;
        }

        if (!exigirTagPermitidaParaReceberDano) return true;
        if (atacante == null) return false;

        return ObjetoOuPaisTemTagPermitida(atacante.transform);
    }

    // =========================================================
    // CURA
    // =========================================================

    public void Curar(int valor)
    {
        if (valor <= 0 || vidaAtual <= 0) return;

        vidaAtual = Mathf.Clamp(vidaAtual + valor, 0, vidaMaxima);

        if (barraVidaUI != null)
            barraVidaUI.AtualizarVida(vidaAtual);
    }

    public void RestaurarVidaTotal()
    {
        vidaAtual = vidaMaxima;

        if (barraVidaUI != null)
            barraVidaUI.AtualizarVida(vidaAtual);
    }

    public int GetVidaAtual()  => vidaAtual;
    public int GetVidaMaxima() => vidaMaxima;

    // =========================================================
    // IMPACTO AUTOMÁTICO
    // =========================================================

    private void ProcessarImpactoAutomatico(Collider colisorAtacante)
    {
        if (!receberDanoAutomaticoPorImpacto) return;
        if (colisorAtacante == null || vidaAtual <= 0) return;

        if (colisorAtacante.transform == transform ||
            colisorAtacante.transform.IsChildOf(transform)) return;

        GameObject atacantePrincipal = ObterObjetoPrincipalDoAtacante(colisorAtacante);
        if (atacantePrincipal == null) return;

        if (!PodeReceberDanoDe(atacantePrincipal)) return;

        int idAtacante = atacantePrincipal.GetInstanceID();

        if (ultimoDanoAutomaticoPorAtacante.TryGetValue(idAtacante, out float ultimoTempo))
        {
            if (Time.time - ultimoTempo < CooldownMesmoAtacante) return;
        }

        if (!TentarObterDanoDoImpacto(colisorAtacante, atacantePrincipal, out int dano, out bool destruirAtacante))
            return;

        if (dano <= 0) return;

        ultimoDanoAutomaticoPorAtacante[idAtacante] = Time.time;
        if (ultimoDanoAutomaticoPorAtacante.Count > 128)
            LimparCooldownsAntigos();

        AplicarDano(dano, atacantePrincipal);

        if (destruirAtacante)
            Destroy(atacantePrincipal);
    }

    private void LimparCooldownsAntigos()
    {
        List<int> remover = new List<int>();
        foreach (KeyValuePair<int, float> par in ultimoDanoAutomaticoPorAtacante)
        {
            if (Time.time - par.Value > 1f)
                remover.Add(par.Key);
        }
        for (int i = 0; i < remover.Count; i++)
            ultimoDanoAutomaticoPorAtacante.Remove(remover[i]);
    }

    // =========================================================
    // APLICAR DANO
    // =========================================================

    private void AplicarDano(int dano, GameObject atacante)
    {
        if (dano <= 0 || vidaAtual <= 0) return;

        vidaAtual -= dano;
        vidaAtual  = Mathf.Max(vidaAtual, 0);

        if (barraVidaUI != null)
            barraVidaUI.AtualizarVida(vidaAtual);

        // Debug de dano removido conforme solicitado.

        if (vidaAtual <= 0)
            DestruirBase();
    }

    // =========================================================
    // DESTRUIÇÃO
    // =========================================================

    private void DestruirBase()
    {
        if (!destruirAoChegarEmZero) return;

        // Toca o som ANTES de destruir o GameObject.
        // Usa objeto temporário com AudioSource 2D para o som não ser
        // cortado junto com o Destroy — mesma abordagem da Torre.cs.
        TocarSomDestruicao();

        Destroy(gameObject);
    }

    private void TocarSomDestruicao()
    {
        if (clipDestruicao == null) return;

        GameObject tempAudio = new GameObject("AudioDestruicaoBase");
        tempAudio.transform.position = transform.position;

        AudioSource fonte = tempAudio.AddComponent<AudioSource>();
        fonte.clip         = clipDestruicao;
        fonte.volume       = volumeDestruicao;
        fonte.spatialBlend = 0f;   // 2D — toca em volume cheio independente da câmera
        fonte.playOnAwake  = false;
        fonte.Play();

        Destroy(tempAudio, clipDestruicao.length + 0.1f);
    }

    // =========================================================
    // HELPERS DE EQUIPE / TAG
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

    private bool ObjetoOuPaisTemTagPermitida(Transform alvo)
    {
        Transform atual = alvo;
        while (atual != null)
        {
            if (TagEstaPermitida(atual.gameObject.tag)) return true;
            atual = atual.parent;
        }
        return false;
    }

    private bool TagEstaPermitida(string tagParaTestar)
    {
        if (string.IsNullOrWhiteSpace(tagParaTestar)) return false;
        if (tagsQuePodemCausarDano == null || tagsQuePodemCausarDano.Length == 0) return false;

        for (int i = 0; i < tagsQuePodemCausarDano.Length; i++)
        {
            string tagPermitida = tagsQuePodemCausarDano[i];
            if (string.IsNullOrWhiteSpace(tagPermitida)) continue;
            if (tagParaTestar == tagPermitida) return true;
        }
        return false;
    }

    // =========================================================
    // LEITURA DE DANO DO ATACANTE
    // =========================================================

    private bool TentarObterDanoDoImpacto(Collider colisorAtacante, GameObject atacantePrincipal,
                                           out int dano, out bool destruirAtacante)
    {
        dano = 0;
        destruirAtacante = false;

        if (procurarDanoNoAtacante &&
            TentarLerDanoDoAtacante(colisorAtacante, atacantePrincipal, out dano))
        {
            destruirAtacante = DeveDestruirAtacantePorTag(atacantePrincipal.transform);
            return true;
        }

        if (usarDanoPorTagSeNaoEncontrarDanoNoAtacante &&
            TentarObterDanoAutomaticoPorTag(atacantePrincipal.transform, out dano, out destruirAtacante))
            return true;

        return false;
    }

    private bool TentarLerDanoDoAtacante(Collider colisorAtacante, GameObject atacantePrincipal, out int dano)
    {
        dano = 0;
        if (atacantePrincipal == null) return false;

        Component[] componentes = atacantePrincipal.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < componentes.Length; i++)
        {
            if (TentarLerDanoDeComponente(componentes[i], out dano)) return true;
        }

        if (colisorAtacante != null && colisorAtacante.gameObject != atacantePrincipal)
        {
            Component[] componentesDoCollider = colisorAtacante.GetComponents<Component>();
            for (int i = 0; i < componentesDoCollider.Length; i++)
            {
                if (TentarLerDanoDeComponente(componentesDoCollider[i], out dano)) return true;
            }
        }

        return false;
    }

    private static readonly string[] NomesCampoDano =
    {
        "dano", "Dano", "danoBala", "DanoBala", "danoAtaque", "DanoAtaque",
        "danoCausado", "DanoCausado", "danoAoAcertar", "DanoAoAcertar",
        "valorDano", "ValorDano", "damage", "Damage"
    };

    private static readonly Dictionary<System.Type, MemberInfo[]> membrosDanoPorTipo =
        new Dictionary<System.Type, MemberInfo[]>();

    private static MemberInfo[] ObterMembrosDeDano(System.Type tipo)
    {
        if (membrosDanoPorTipo.TryGetValue(tipo, out MemberInfo[] cache)) return cache;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        List<MemberInfo> encontrados = new List<MemberInfo>();

        for (int i = 0; i < NomesCampoDano.Length; i++)
        {
            FieldInfo campo = tipo.GetField(NomesCampoDano[i], flags);
            if (campo != null) encontrados.Add(campo);

            PropertyInfo propriedade = tipo.GetProperty(NomesCampoDano[i], flags);
            if (propriedade != null && propriedade.CanRead) encontrados.Add(propriedade);
        }

        MemberInfo[] resultado = encontrados.ToArray();
        membrosDanoPorTipo[tipo] = resultado;
        return resultado;
    }

    private bool TentarLerDanoDeComponente(Component componente, out int dano)
    {
        dano = 0;
        if (componente == null) return false;
        if (componente is BaseVida) return false;
        if (componente is BaseVidaColliderFilho) return false;

        MemberInfo[] membros = ObterMembrosDeDano(componente.GetType());
        for (int i = 0; i < membros.Length; i++)
        {
            object valor = null;

            FieldInfo campo = membros[i] as FieldInfo;
            if (campo != null)
                valor = campo.GetValue(componente);
            else
            {
                PropertyInfo propriedade = membros[i] as PropertyInfo;
                if (propriedade != null) valor = propriedade.GetValue(componente, null);
            }

            if (TentarConverterParaInteiro(valor, out dano)) return true;
        }
        return false;
    }

    private bool TentarLerCampoOuPropriedadeInteira(System.Type tipo, object instancia, string nome, out int valor)
    {
        valor = 0;
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        FieldInfo campo = tipo.GetField(nome, flags);
        if (campo != null && TentarConverterParaInteiro(campo.GetValue(instancia), out valor)) return true;

        PropertyInfo propriedade = tipo.GetProperty(nome, flags);
        if (propriedade != null && propriedade.CanRead &&
            TentarConverterParaInteiro(propriedade.GetValue(instancia, null), out valor)) return true;

        return false;
    }

    private bool TentarConverterParaInteiro(object valorOriginal, out int valor)
    {
        valor = 0;
        if (valorOriginal == null) return false;

        if (valorOriginal is int inteiro)       { valor = inteiro;                         return valor > 0; }
        if (valorOriginal is float numeroFloat) { valor = Mathf.RoundToInt(numeroFloat);   return valor > 0; }
        if (valorOriginal is double numDouble)  { valor = Mathf.RoundToInt((float)numDouble); return valor > 0; }

        return false;
    }

    private bool TentarObterDanoAutomaticoPorTag(Transform atacante, out int dano, out bool destruirAtacante)
    {
        dano = 0;
        destruirAtacante = false;
        if (atacante == null || danosAutomaticosPorTag == null) return false;

        Transform atual = atacante;
        while (atual != null)
        {
            for (int i = 0; i < danosAutomaticosPorTag.Length; i++)
            {
                DanoAutomaticoPorTag configuracao = danosAutomaticosPorTag[i];
                if (configuracao == null) continue;
                if (string.IsNullOrWhiteSpace(configuracao.tagAtacante)) continue;

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
        if (atacante == null || danosAutomaticosPorTag == null) return false;

        Transform atual = atacante;
        while (atual != null)
        {
            for (int i = 0; i < danosAutomaticosPorTag.Length; i++)
            {
                DanoAutomaticoPorTag configuracao = danosAutomaticosPorTag[i];
                if (configuracao == null) continue;
                if (string.IsNullOrWhiteSpace(configuracao.tagAtacante)) continue;
                if (atual.CompareTag(configuracao.tagAtacante)) return configuracao.destruirAtacanteAposDano;
            }
            atual = atual.parent;
        }
        return false;
    }

    private GameObject ObterObjetoPrincipalDoAtacante(Collider colisorAtacante)
    {
        if (colisorAtacante == null) return null;
        if (colisorAtacante.attachedRigidbody != null) return colisorAtacante.attachedRigidbody.gameObject;
        return colisorAtacante.gameObject;
    }

    // =========================================================
    // COLLIDERS FILHOS
    // =========================================================

    private void PrepararCollidersFilhos()
    {
        if (!receberImpactoEmCollidersFilhos) return;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null) continue;
            if (col.transform == transform) continue;
            if (EhColliderDeProjetil(col)) continue;

            BaseVidaColliderFilho ponte = col.GetComponent<BaseVidaColliderFilho>();
            if (ponte == null) ponte = col.gameObject.AddComponent<BaseVidaColliderFilho>();
            ponte.Configurar(this);
        }
    }

    private static bool EhColliderDeProjetil(Collider col)
    {
        return col.GetComponentInParent<Missel>(true) != null
            || col.GetComponentInParent<ProjetilDistancia>(true) != null;
    }

    private void ReceberImpactoDoColliderFilho(Collider colisorAtacante)
    {
        ProcessarImpactoAutomatico(colisorAtacante);
    }

    // =========================================================
    // VALIDAÇÃO
    // =========================================================

    private void OnValidate()
    {
        vidaMaxima = Mathf.Max(1, vidaMaxima);
        vidaAtual  = Mathf.Clamp(vidaAtual, 0, vidaMaxima);

        if (danosAutomaticosPorTag != null)
        {
            for (int i = 0; i < danosAutomaticosPorTag.Length; i++)
            {
                if (danosAutomaticosPorTag[i] == null) continue;
                danosAutomaticosPorTag[i].dano = Mathf.Max(0, danosAutomaticosPorTag[i].dano);
            }
        }
    }

    // =========================================================
    // CLASSE INTERNA — BRIDGE DE COLLIDER
    // =========================================================

    private class BaseVidaColliderFilho : MonoBehaviour
    {
        private BaseVida baseVida;

        public void Configurar(BaseVida novaBaseVida)
        {
            baseVida = novaBaseVida;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (baseVida == null || collision == null) return;
            baseVida.ReceberImpactoDoColliderFilho(collision.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (baseVida == null) return;
            baseVida.ReceberImpactoDoColliderFilho(other);
        }
    }
}