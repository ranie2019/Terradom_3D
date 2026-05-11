using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class AviaoVoo : MonoBehaviour
{
    public enum EstadoVoo { EmEspera, Rolando, Decolando, EmVoo }

    // =====================================================================
    // INSPECTOR
    // =====================================================================

    [Header("Rolagem na pista")]
    [SerializeField] private float velocidadeMaxRolagem = 80f;
    [SerializeField] private float aceleracao           = 12f;
    [SerializeField] private float velocidadeDecolagem  = 55f;

    [Header("Decolagem")]
    [SerializeField] private float anguloSubida                = 15f;
    [SerializeField] private float taxaRotacaoPitch            = 40f;
    [SerializeField] private float alturaParaConsiderarNoAr    = 4f;
    [SerializeField] private float velocidadeCruzeiro          = 120f;

    [Header("Terrain")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private float alturaMaxRaycast = 800f;

    [Header("Estado — somente leitura")]
    [SerializeField] private EstadoVoo estadoAtual = EstadoVoo.EmEspera;
    [SerializeField] private float velocidadeAtual  = 0f;
    [SerializeField] private float alturaAcimaTerrain = 0f;

    // =====================================================================
    // PRIVADOS
    // =====================================================================

    private Rigidbody rb;
    private float     pitchAtual          = 0f;
    private float     ultimaAlturaTerrain = 0f;

    // Distância entre o pivô da fuselagem e o terrain ao iniciar.
    // Preserva a altura correta das rodas (colliders filhos) durante a rolagem.
    private float offsetSolo = 0f;

    // =====================================================================
    // PROPRIEDADES PÚBLICAS
    // =====================================================================

    public EstadoVoo EstadoAtual      => estadoAtual;
    public float     Velocidade       => velocidadeAtual;
    public float     AlturaAcimaTerrain => alturaAcimaTerrain;
    public bool      EstaNoAr         => estadoAtual == EstadoVoo.EmVoo
                                      || estadoAtual == EstadoVoo.Decolando;

    public void ReiniciarDecolagem()
    {
        estadoAtual     = EstadoVoo.EmEspera;
        velocidadeAtual = 0f;
        pitchAtual      = 0f;
        AplicarPitch();
    }

    // =====================================================================
    // UNITY
    // =====================================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity      = false;
        rb.linearDamping   = 0.05f;
        rb.angularDamping  = 0.5f;
        rb.constraints     = RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationZ;

        if (terrain == null)
            terrain = Terrain.activeTerrain;

        ultimaAlturaTerrain = ObterAlturaTerrain();

        // Offset entre o pivô (fuselagem) e o solo — leva em conta os
        // colliders filhos (rodas) que ficam abaixo do pivô central.
        offsetSolo = transform.position.y - ultimaAlturaTerrain;
    }

    private void OnEnable()
    {
        // Recalcula o offset no momento em que o AviaoControler ativa este
        // componente — garante que a posição final do AviaoGaragem é respeitada.
        float h = ObterAlturaTerrain();
        if (h >= 0f)
        {
            ultimaAlturaTerrain = h;
            offsetSolo = transform.position.y - h;
        }

        if (estadoAtual == EstadoVoo.EmEspera)
            estadoAtual = EstadoVoo.Rolando;
    }

    private void Update()
    {
        AtualizarAlturaTerrain();

        switch (estadoAtual)
        {
            case EstadoVoo.Rolando:   AtualizarRolagem();   break;
            case EstadoVoo.Decolando: AtualizarDecolagem(); break;
            case EstadoVoo.EmVoo:     AtualizarVoo();       break;
        }

        alturaAcimaTerrain = transform.position.y - ultimaAlturaTerrain;
    }

    // =====================================================================
    // FASES
    // =====================================================================

    private void AtualizarRolagem()
    {
        velocidadeAtual = Mathf.MoveTowards(
            velocidadeAtual, velocidadeMaxRolagem, aceleracao * Time.deltaTime);

        Vector3 novaPos = transform.position + transform.forward * velocidadeAtual * Time.deltaTime;

        // Usa terrain + offsetSolo para manter o pivô da fuselagem
        // na altura correta, respeitando os colliders de roda (filhos)
        novaPos.y = ultimaAlturaTerrain + offsetSolo;

        transform.position = novaPos;

        if (velocidadeAtual >= velocidadeDecolagem)
            estadoAtual = EstadoVoo.Decolando;
    }

    private void AtualizarDecolagem()
    {
        velocidadeAtual = Mathf.MoveTowards(
            velocidadeAtual, velocidadeCruzeiro, aceleracao * Time.deltaTime);

        pitchAtual = Mathf.MoveTowards(pitchAtual, -anguloSubida, taxaRotacaoPitch * Time.deltaTime);

        transform.rotation = Quaternion.Euler(pitchAtual, transform.eulerAngles.y, 0f);
        transform.position += transform.forward * velocidadeAtual * Time.deltaTime;

        if (transform.position.y - ultimaAlturaTerrain >= alturaParaConsiderarNoAr)
            estadoAtual = EstadoVoo.EmVoo;
    }

    private void AtualizarVoo()
    {
        // Mantém o avião voando em frente na velocidade de cruzeiro.
        // O AviaoControler desabilita este componente quando a
        // altitude segura for atingida e passar o controle ao próximo sistema.
        velocidadeAtual = Mathf.MoveTowards(
            velocidadeAtual, velocidadeCruzeiro, aceleracao * Time.deltaTime);

        transform.position += transform.forward * velocidadeAtual * Time.deltaTime;
    }

    // =====================================================================
    // TERRAIN / ALTURA
    // =====================================================================

    private void AtualizarAlturaTerrain()
    {
        float h = ObterAlturaTerrain();
        if (h >= 0f) ultimaAlturaTerrain = h;
    }

    private float ObterAlturaTerrain()
    {
        if (terrain != null)
        {
            TerrainData td   = terrain.terrainData;
            Vector3     tPos = terrain.transform.position;
            Vector3     pos  = transform.position;

            if (pos.x >= tPos.x && pos.x <= tPos.x + td.size.x &&
                pos.z >= tPos.z && pos.z <= tPos.z + td.size.z)
                return terrain.SampleHeight(pos) + tPos.y;
        }

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, alturaMaxRaycast))
            return hit.point.y;

        return -1f;
    }

    // =====================================================================
    // UTILITÁRIOS
    // =====================================================================

    private void AplicarPitch()
    {
        float yaw = transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(pitchAtual, yaw, 0f);
    }

    // =====================================================================
    // GIZMOS
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        float h = ObterAlturaTerrain();
        if (h < 0f) return;

        Gizmos.color = EstaNoAr ? Color.green : Color.yellow;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 10f, h, transform.position.z),
            new Vector3(transform.position.x + 10f, h, transform.position.z)
        );
    }
}