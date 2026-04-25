using UnityEngine;

[DisallowMultipleComponent]
public class Visao : MonoBehaviour
{
    [Header("Raio de visão (PÚBLICO)")]
    [Min(0.1f)] public float raioVisao = 15f;

    [Header("Campo de visão")]
    [Range(1f, 360f)] public float anguloCampoDeVisao = 120f;

    [Header("Alvos")]
    [SerializeField] private string[] tagsAlvo = new string[] { "Vermelho" };
    [SerializeField] private float scansPorSegundo = 8f;
    [SerializeField] private LayerMask camadasAlvo = ~0;
    [SerializeField] private bool travarY = true;

    private Transform _alvoDetectado360;
    private Transform _alvoEngajavel;
    private float _proximoScan = 0f;

    private void Update()
    {
        if (Time.time >= _proximoScan)
        {
            _proximoScan = Time.time + (1f / Mathf.Max(0.1f, scansPorSegundo));
            ScanAlvos();
        }

        ValidarAlvosAtuais();
    }

    private void ScanAlvos()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            raioVisao,
            camadasAlvo,
            QueryTriggerInteraction.Ignore
        );

        Transform melhor360 = null;
        float melhorDist360 = float.MaxValue;

        Transform melhorCampo = null;
        float melhorDistCampo = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (!c) continue;

            Transform unidade = ResolverTransformDaUnidade(c.transform);
            if (!unidade) continue;
            if (unidade == transform) continue;
            if (!unidade.gameObject.activeInHierarchy) continue;
            if (!EhAlvoValido(unidade.tag)) continue;

            float d = DistXZ(transform.position, unidade.position);
            if (d > raioVisao) continue;

            if (d < melhorDist360)
            {
                melhorDist360 = d;
                melhor360 = unidade;
            }

            if (EstaNoCampoDeVisao(unidade.position) && d < melhorDistCampo)
            {
                melhorDistCampo = d;
                melhorCampo = unidade;
            }
        }

        _alvoDetectado360 = melhor360;
        _alvoEngajavel = melhorCampo;
    }

    private void ValidarAlvosAtuais()
    {
        if (_alvoDetectado360 != null)
        {
            if (!_alvoDetectado360.gameObject.activeInHierarchy ||
                DistXZ(transform.position, _alvoDetectado360.position) > raioVisao)
            {
                _alvoDetectado360 = null;
            }
        }

        if (_alvoEngajavel != null)
        {
            if (!_alvoEngajavel.gameObject.activeInHierarchy ||
                DistXZ(transform.position, _alvoEngajavel.position) > raioVisao)
            {
                _alvoEngajavel = null;
            }
            else if (!EstaNoCampoDeVisao(_alvoEngajavel.position))
            {
                _alvoEngajavel = null;
            }
        }
    }

    private Transform ResolverTransformDaUnidade(Transform origem)
    {
        if (!origem) return null;

        Transform atual = origem;

        while (atual != null)
        {
            if (EhAlvoValido(atual.tag))
                return atual;

            atual = atual.parent;
        }

        return null;
    }

    private bool EhAlvoValido(string tagRecebida)
    {
        if (tagsAlvo == null || tagsAlvo.Length == 0)
            return false;

        for (int i = 0; i < tagsAlvo.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(tagsAlvo[i]) && tagsAlvo[i] == tagRecebida)
                return true;
        }

        return false;
    }

    private bool EstaNoCampoDeVisao(Vector3 alvoPos)
    {
        if (anguloCampoDeVisao >= 360f)
            return true;

        Vector3 dir = alvoPos - transform.position;
        if (travarY) dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return true;

        Vector3 frente = transform.forward;
        frente.y = 0f;

        if (frente.sqrMagnitude < 0.0001f)
            frente = dir.normalized;

        float cos = Vector3.Dot(frente.normalized, dir.normalized);
        float cosLimite = Mathf.Cos(Mathf.Deg2Rad * (anguloCampoDeVisao * 0.5f));

        return cos >= cosLimite;
    }

    public Transform GetAlvoAtual()
    {
        if (_alvoEngajavel != null)
            return _alvoEngajavel;

        return _alvoDetectado360;
    }

    public bool AlvoEstaNoCampoDeVisao()
    {
        return _alvoEngajavel != null;
    }

    public Transform GetAlvoDetectado360()
    {
        return _alvoDetectado360;
    }

    public Transform GetAlvoEngajavel()
    {
        return _alvoEngajavel;
    }

    public bool TemAlvoNoCampo()
    {
        return _alvoEngajavel != null;
    }

    public bool AlvoEstaNoRaio(Transform alvo)
    {
        if (!alvo || !alvo.gameObject.activeInHierarchy)
            return false;

        return DistXZ(transform.position, alvo.position) <= raioVisao;
    }

    private static float DistXZ(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, raioVisao);

        Vector3 frente = transform.forward;
        frente.y = 0f;

        if (frente.sqrMagnitude < 0.0001f)
            frente = Vector3.forward;

        Quaternion rotEsq = Quaternion.Euler(0f, -anguloCampoDeVisao * 0.5f, 0f);
        Quaternion rotDir = Quaternion.Euler(0f, anguloCampoDeVisao * 0.5f, 0f);

        Vector3 linhaEsq = rotEsq * frente.normalized * raioVisao;
        Vector3 linhaDir = rotDir * frente.normalized * raioVisao;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + linhaEsq);
        Gizmos.DrawLine(transform.position, transform.position + linhaDir);

        if (_alvoDetectado360 != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _alvoDetectado360.position);
        }

        if (_alvoEngajavel != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _alvoEngajavel.position);
        }
    }
#endif
}