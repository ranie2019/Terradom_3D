using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Versão da IA do BaseLimite.
/// Mesma lógica de registro, remoção e consulta do BaseLimite,
/// mas SEM visuais (borda, material, piscar) — a IA não precisa ver nada.
///
/// Coloque este componente nos prefabs de base da IA (ex.: prefabs Vermelho).
/// BaseAreaIA.cs chama SetTagBase("Vermelho") após instanciar cada base.
///
/// Métodos estáticos disponíveis para BaseAreaIA:
///   BaseLimiteIA.PosicaoDentroDeArea(tag, pos)         → bool
///   BaseLimiteIA.PodeConstruir(tag, pos, distMin)       → bool
///   BaseLimiteIA.TemAreaDisponivel(tag)                  → bool
///   BaseLimiteIA.ObterBaseMaisProxima(tag, pos)          → BaseLimiteIA
///   BaseLimiteIA.ObterBasesDaTag(tag)                    → List<BaseLimiteIA>
/// </summary>
[DisallowMultipleComponent]
public class BaseLimiteIA : MonoBehaviour
{
    // =========================================================
    // REGISTRO GLOBAL (estático — compartilhado por todas as instâncias)
    // =========================================================
    private static readonly Dictionary<string, List<BaseLimiteIA>> basesPorTag
        = new Dictionary<string, List<BaseLimiteIA>>();

    // Primeira base registrada por tag (menor InstanceID)
    private static readonly Dictionary<string, BaseLimiteIA> primeiraBasePorTag
        = new Dictionary<string, BaseLimiteIA>();

    // =========================================================
    // INSPECTOR
    // =========================================================
    [Header("Área da IA")]
    [Tooltip("Raio da área controlada por esta base. Idealmente igual ao raioArea do BaseLimite.")]
    [SerializeField] private float raioArea = 15f;

    [Tooltip("Tag da equipe IA (ex.: 'Vermelho'). Sobrescrito por BaseAreaIA via SetTagBase().")]
    [SerializeField] private string tagBase = "Vermelho";

    // =========================================================
    // ESTADO INTERNO
    // =========================================================
    private bool desregistradoTemporariamente = false;

    // =========================================================
    // RESET ESTÁTICO (entre sessões de Play no Editor)
    // =========================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstadoEstatico()
    {
        basesPorTag.Clear();
        primeiraBasePorTag.Clear();
    }

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        // Usa a tag do GameObject se tagBase estiver vazia
        if (string.IsNullOrEmpty(tagBase))
            tagBase = gameObject.tag;

        // Aviso: prefab com tag de objeto diferente da tagBase configurada
        string tagObjeto = gameObject.tag;
        if ((tagObjeto == "Azul" || tagObjeto == "Vermelho" || tagObjeto == "Verde")
            && tagObjeto != tagBase)
            Debug.LogWarning($"[BaseLimiteIA] '{name}' tem a tag '{tagObjeto}' mas tagBase='{tagBase}'. " +
                             "A área será registrada para a equipe da tagBase.", this);

        if (!desregistradoTemporariamente)
            RegistrarBase();
    }

    private void OnEnable()
    {
        desregistradoTemporariamente = false;
        RegistrarBase();
    }

    private void OnDisable() { /* nada — OnDestroy cuida da remoção */ }

    private void OnDestroy()
    {
        RemoverBase();
    }

    // =========================================================
    // REGISTRO
    // =========================================================

    /// <summary>
    /// Remove temporariamente esta base do registro global.
    /// Usado durante posicionamento (ghost) para que a base
    /// não seja contada como área válida antes de ser confirmada.
    /// </summary>
    public void DesregistrarTemporariamente()
    {
        desregistradoTemporariamente = true;
        RemoverBase();
    }

    private void RegistrarBase()
    {
        if (string.IsNullOrEmpty(tagBase))
        {
            Debug.LogError("[BaseLimiteIA] ❌ tagBase não pode ser vazia!", this);
            return;
        }

        if (!basesPorTag.ContainsKey(tagBase))
            basesPorTag[tagBase] = new List<BaseLimiteIA>();

        if (!basesPorTag[tagBase].Contains(this))
            basesPorTag[tagBase].Add(this);

        // Registra a primeira base (menor InstanceID) como referência
        if (!primeiraBasePorTag.ContainsKey(tagBase) ||
            primeiraBasePorTag[tagBase] == null ||
            GetInstanceID() < primeiraBasePorTag[tagBase].GetInstanceID())
        {
            primeiraBasePorTag[tagBase] = this;
        }
    }

    private void RemoverBase()
    {
        if (string.IsNullOrEmpty(tagBase)) return;

        if (basesPorTag.ContainsKey(tagBase))
        {
            basesPorTag[tagBase].Remove(this);

            if (basesPorTag[tagBase].Count == 0)
            {
                basesPorTag.Remove(tagBase);
                primeiraBasePorTag.Remove(tagBase);
            }
            else if (primeiraBasePorTag.TryGetValue(tagBase, out BaseLimiteIA primeira) && primeira == this)
            {
                // Recalcula a primeira base ao remover a que era referência
                primeiraBasePorTag.Remove(tagBase);
                BaseLimiteIA novaPrimeira = null;
                int menorId = int.MaxValue;
                foreach (BaseLimiteIA b in basesPorTag[tagBase])
                {
                    if (b == null) continue;
                    int id = b.GetInstanceID();
                    if (id < menorId) { menorId = id; novaPrimeira = b; }
                }
                if (novaPrimeira != null)
                    primeiraBasePorTag[tagBase] = novaPrimeira;
            }
        }
    }

    // =========================================================
    // VERIFICAÇÃO DE ÁREA (MÉTODOS ESTÁTICOS)
    // =========================================================

    /// <summary>
    /// Retorna true se a posição está dentro da área de alguma base IA com essa tag.
    /// Ignora o eixo Y — compara apenas X e Z (distância horizontal).
    /// </summary>
    public static bool PosicaoDentroDeArea(string tag, Vector3 posicao)
    {
        if (!basesPorTag.ContainsKey(tag)) return false;

        List<BaseLimiteIA> lista = basesPorTag[tag];

        for (int i = lista.Count - 1; i >= 0; i--)
        {
            BaseLimiteIA base_ = lista[i];

            // Remove entradas nulas (bases destruídas sem OnDestroy)
            if (base_ == null)
            {
                lista.RemoveAt(i);
                continue;
            }

            float distancia = Vector3.Distance(
                new Vector3(posicao.x,              0, posicao.z),
                new Vector3(base_.transform.position.x, 0, base_.transform.position.z));

            if (distancia <= base_.raioArea)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Verifica se uma posição está dentro da área desta base específica.
    /// </summary>
    public bool PosicaoDentroDaMinhaArea(Vector3 posicao)
    {
        float distancia = Vector3.Distance(
            new Vector3(posicao.x,           0, posicao.z),
            new Vector3(transform.position.x, 0, transform.position.z));

        return distancia <= raioArea;
    }

    /// <summary>
    /// Verifica se pode construir na posição:
    /// dentro da área aliada E longe o suficiente de bases inimigas.
    /// Mesmo padrão do BaseLimite.PodeConstruir.
    /// </summary>
    public static bool PodeConstruir(string tag, Vector3 posicao, float distanciaMinimaEntreBases = 30f)
    {
        if (!PosicaoDentroDeArea(tag, posicao)) return false;

        foreach (var kvp in basesPorTag)
        {
            if (kvp.Key == tag) continue; // pula aliadas

            foreach (BaseLimiteIA baseInimiga in kvp.Value)
            {
                if (baseInimiga == null) continue;

                float distancia = Vector3.Distance(
                    new Vector3(posicao.x,                   0, posicao.z),
                    new Vector3(baseInimiga.transform.position.x, 0, baseInimiga.transform.position.z));

                if (distancia < distanciaMinimaEntreBases)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Retorna true se esta tag já tem pelo menos uma base registrada.
    /// Usado para saber se a IA já tem área no mapa.
    /// </summary>
    public static bool TemAreaDisponivel(string tag)
    {
        return basesPorTag.ContainsKey(tag) && basesPorTag[tag].Count > 0;
    }

    /// <summary>
    /// Retorna a base IA mais próxima de uma posição para a tag informada.
    /// Útil para a IA expandir a partir do ponto mais conveniente.
    /// </summary>
    public static BaseLimiteIA ObterBaseMaisProxima(string tag, Vector3 posicao)
    {
        if (!basesPorTag.ContainsKey(tag)) return null;

        BaseLimiteIA maisProxima = null;
        float menorDistancia = float.MaxValue;

        foreach (BaseLimiteIA base_ in basesPorTag[tag])
        {
            if (base_ == null) continue;

            float distancia = Vector3.Distance(posicao, base_.transform.position);
            if (distancia < menorDistancia)
            {
                menorDistancia = distancia;
                maisProxima    = base_;
            }
        }

        return maisProxima;
    }

    /// <summary>
    /// Retorna todas as bases IA registradas para uma tag.
    /// </summary>
    public static List<BaseLimiteIA> ObterBasesDaTag(string tag)
    {
        if (basesPorTag.TryGetValue(tag, out List<BaseLimiteIA> lista))
            return lista;
        return new List<BaseLimiteIA>();
    }

    // =========================================================
    // CONFIGURAÇÃO PÚBLICA
    // =========================================================

    public float GetRaioArea() => raioArea;
    public string GetTagBase() => tagBase;

    /// <summary>
    /// Atualiza o raio da área (ex.: upgrade de base).
    /// </summary>
    public void SetRaioArea(float novoRaio)
    {
        raioArea = Mathf.Max(5f, novoRaio);
    }

    /// <summary>
    /// Define a tag da equipe e re-registra.
    /// Chamado por BaseAreaIA após instanciar a base.
    /// </summary>
    public void SetTagBase(string novaTag)
    {
        if (string.IsNullOrEmpty(novaTag)) return;

        RemoverBase();
        tagBase = novaTag;

        if (!desregistradoTemporariamente)
            RegistrarBase();
    }

    // =========================================================
    // VALIDAÇÃO
    // =========================================================

    private void OnValidate()
    {
        raioArea = Mathf.Max(5f, raioArea);
    }

    // =========================================================
    // GIZMO (Editor — não existe em build)
    // =========================================================

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.color = new Color(1f, 0.3f, 0.1f, 0.4f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, raioArea);
        UnityEditor.Handles.color = new Color(1f, 0.3f, 0.1f, 0.15f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, raioArea);
    }
#endif
}