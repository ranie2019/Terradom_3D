using UnityEngine;

[DisallowMultipleComponent]
public class Vida : MonoBehaviour
{
    [Header("Vida")]
    [Tooltip("Vida inicial do boneco")]
    public int vidaMax = 3;

    [Tooltip("Vida atual (pública)")]
    public int vidaAtual = 3;

    [Header("Dano por colisão")]
    [Tooltip("Tags das ESPADAS que podem causar dano nesse boneco. Ex: no Azul coloque 'espada vermelho'. No Vermelho coloque 'espada azul'.")]
    public string[] tagsEspadaQueDano = new string[] { "espada vermelho" };

    [Tooltip("Cooldown para não perder múltiplas vidas num encostão só")]
    public float cooldownDano = 0.25f;

    [Header("Ignorar dano quando o HIT for nesses objetos (filhos)")]
    [Tooltip("Arraste aqui os Transforms da espada e do escudo do PRÓPRIO boneco (ou o pai deles).")]
    public Transform[] ignorarSeAtingirEsses;

    private float proximoDanoPermitido;

    void Awake()
    {
        if (vidaMax <= 0) vidaMax = 1;
        if (vidaAtual <= 0) vidaAtual = vidaMax;
        if (vidaAtual > vidaMax) vidaAtual = vidaMax;
    }

    // Trigger (espada trigger)
    void OnTriggerEnter(Collider other) => ProcessarHit(other, other != null ? other.transform : null);
    void OnTriggerStay(Collider other) => ProcessarHit(other, other != null ? other.transform : null);

    // Colisão (se usar collider sem trigger)
    void OnCollisionEnter(Collision c) => ProcessarHit(c != null ? c.collider : null, c != null ? c.transform : null);
    void OnCollisionStay(Collision c) => ProcessarHit(c != null ? c.collider : null, c != null ? c.transform : null);

    private void ProcessarHit(Collider colliderDoAtacante, Transform transformDoAtacante)
    {
        if (Time.time < proximoDanoPermitido) return;
        if (colliderDoAtacante == null || transformDoAtacante == null) return;

        GameObject atacante = colliderDoAtacante.gameObject;

        // 1) Só toma dano se o atacante tiver tag configurada (espada inimiga)
        if (!TemTagDeEspadaValida(atacante.tag)) return;

        // 2) Se o HIT aconteceu em espada/escudo do PRÓPRIO boneco, ignora
        // Aqui a checagem é REAL: usamos o collider que recebeu o callback (this.transform)
        if (AtingiuAreaIgnorada(this.transform)) return;

        // 3) Aplica dano
        AplicarDano(1);
    }

    private bool TemTagDeEspadaValida(string tagAtacante)
    {
        if (tagsEspadaQueDano == null) return false;

        for (int i = 0; i < tagsEspadaQueDano.Length; i++)
        {
            var t = tagsEspadaQueDano[i];
            if (!string.IsNullOrWhiteSpace(t) && tagAtacante == t)
                return true;
        }
        return false;
    }

    private bool AtingiuAreaIgnorada(Transform parteAtingida)
    {
        if (ignorarSeAtingirEsses == null || ignorarSeAtingirEsses.Length == 0) return false;
        if (!parteAtingida) return false;

        for (int i = 0; i < ignorarSeAtingirEsses.Length; i++)
        {
            var ig = ignorarSeAtingirEsses[i];
            if (!ig) continue;

            if (parteAtingida == ig || parteAtingida.IsChildOf(ig))
                return true;
        }
        return false;
    }

    // ✅ Método usado pelo PlayControleAI
    public void AplicarDano(int dano)
    {
        if (dano <= 0) return;
        if (vidaAtual <= 0) return;

        proximoDanoPermitido = Time.time + cooldownDano;

        vidaAtual -= dano;
        if (vidaAtual < 0) vidaAtual = 0;

        SendMessage("TakeDamage", dano, SendMessageOptions.DontRequireReceiver);

        if (vidaAtual <= 0)
        {
            Destroy(gameObject);
        }
    }
}
