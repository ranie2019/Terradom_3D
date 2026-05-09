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
    [Tooltip("Tags das ESPADAS que podem causar dano nesse boneco.")]
    public string[] tagsEspadaQueDano = new string[] { "espada vermelho" };

    [Tooltip("Cooldown para não perder múltiplas vidas num encostão só")]
    public float cooldownDano = 0.25f;

    [Header("Ignorar dano quando o HIT for nesses objetos")]
    public Transform[] ignorarSeAtingirEsses;

    private float proximoDanoPermitido;

    private void Awake()
    {
        if (vidaMax <= 0)
            vidaMax = 1;

        if (vidaAtual <= 0)
            vidaAtual = vidaMax;

        if (vidaAtual > vidaMax)
            vidaAtual = vidaMax;

        // CONFIGURA A BARRA
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
        if (Time.time < proximoDanoPermitido)
            return;

        if (colliderDoAtacante == null || transformDoAtacante == null)
            return;

        GameObject atacante = colliderDoAtacante.gameObject;

        // Só toma dano de espada válida
        if (!TemTagDeEspadaValida(atacante.tag))
            return;

        // Ignora espada/escudo próprio
        if (AtingiuAreaIgnorada(this.transform))
            return;

        AplicarDano(1);
    }

    private bool TemTagDeEspadaValida(string tagAtacante)
    {
        if (tagsEspadaQueDano == null)
            return false;

        for (int i = 0; i < tagsEspadaQueDano.Length; i++)
        {
            string t = tagsEspadaQueDano[i];

            if (!string.IsNullOrWhiteSpace(t) && tagAtacante == t)
                return true;
        }

        return false;
    }

    private bool AtingiuAreaIgnorada(Transform parteAtingida)
    {
        if (ignorarSeAtingirEsses == null || ignorarSeAtingirEsses.Length == 0)
            return false;

        if (!parteAtingida)
            return false;

        for (int i = 0; i < ignorarSeAtingirEsses.Length; i++)
        {
            Transform ig = ignorarSeAtingirEsses[i];

            if (!ig)
                continue;

            if (parteAtingida == ig || parteAtingida.IsChildOf(ig))
                return true;
        }

        return false;
    }

    // =========================
    // DANO
    // =========================

    public void AplicarDano(int dano)
    {
        if (dano <= 0)
            return;

        if (vidaAtual <= 0)
            return;

        proximoDanoPermitido = Time.time + cooldownDano;

        vidaAtual -= dano;

        if (vidaAtual < 0)
            vidaAtual = 0;

        // ATUALIZA BARRA
        if (barraVidaUI != null)
        {
            barraVidaUI.AtualizarVida(vidaAtual);
        }

        SendMessage("TakeDamage", dano, SendMessageOptions.DontRequireReceiver);

        if (vidaAtual <= 0)
        {
            Destroy(gameObject);
        }
    }
}