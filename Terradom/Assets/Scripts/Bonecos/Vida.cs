using UnityEngine;

[DisallowMultipleComponent]
public class Vida : MonoBehaviour
{
    [Header("Vida")]
    [Tooltip("Vida inicial do boneco")]
    public int vidaMax = 3;
    [Tooltip("Vida atual (pública)")]
    public int vidaAtual = 3;

    [Header("Barra de Vida")]
    [SerializeField] private BarraVidaUI barraVidaUI;

    [Header("Dano por colisão")]
    [Tooltip("Tags que causam dano.")]
    public string[] tagsQueCausanDano = new string[] { "" };

    [Tooltip("Cooldown para não perder múltiplas vidas num encosto.")]
    public float cooldownDano = 0.25f;

    private float proximoDanoPermitido;

    private void Awake()
    {
        if (vidaMax <= 0) vidaMax = 1;
        if (vidaAtual <= 0) vidaAtual = vidaMax;
        if (vidaAtual > vidaMax) vidaAtual = vidaMax;

        if (barraVidaUI != null)
        {
            barraVidaUI.Configurar(vidaMax);
            barraVidaUI.AtualizarVida(vidaAtual);
        }
    }

    // =========================
    // TRIGGER
    // =========================
    private void OnTriggerEnter(Collider other)
    {
        ProcessarHit(other, other != null ? other.transform : null);
    }

    private void OnTriggerStay(Collider other)
    {
        ProcessarHit(other, other != null ? other.transform : null);
    }

    // =========================
    // COLLISION
    // =========================
    private void OnCollisionEnter(Collision c)
    {
        ProcessarHit(c != null ? c.collider : null, c != null ? c.transform : null);
    }

    private void OnCollisionStay(Collision c)
    {
        ProcessarHit(c != null ? c.collider : null, c != null ? c.transform : null);
    }

    // =========================
    // PROCESSAR HIT
    // =========================
    private void ProcessarHit(Collider colliderDoAtacante, Transform transformDoAtacante)
    {
        if (Time.time < proximoDanoPermitido) return;
        if (colliderDoAtacante == null || transformDoAtacante == null) return;

        GameObject atacante = colliderDoAtacante.gameObject;
        if (!TemTagDeEspadaValida(atacante.tag)) return;

        AplicarDano(1);
    }

    private bool TemTagDeEspadaValida(string tagAtacante)
    {
        if (tagsQueCausanDano == null) return false;
        for (int i = 0; i < tagsQueCausanDano.Length; i++)
        {
            string t = tagsQueCausanDano[i];
            if (!string.IsNullOrWhiteSpace(t) && tagAtacante == t)
                return true;
        }
        return false;
    }

    // =========================
    // DANO
    // =========================
    public void AplicarDano(int dano)
    {
        if (dano <= 0) return;
        if (vidaAtual <= 0) return;

        proximoDanoPermitido = Time.time + cooldownDano;
        vidaAtual -= dano;
        if (vidaAtual < 0) vidaAtual = 0;

        if (barraVidaUI != null)
            barraVidaUI.AtualizarVida(vidaAtual);

        // FIX: removido SendMessage("TakeDamage", dano) que estava aqui.
        // Nenhum script no projeto tem o método TakeDamage — era código morto
        // gerando overhead de SendMessage a cada ponto de dano recebido.

        if (vidaAtual <= 0)
            Destroy(gameObject);
    }
}