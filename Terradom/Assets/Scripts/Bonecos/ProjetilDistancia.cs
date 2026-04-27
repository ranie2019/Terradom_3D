using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class ProjetilDistancia : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private int dano = 1;
    [SerializeField] private float velocidade = 20f;
    [SerializeField] private float tempoDeVida = 3f;

    [Header("Alvo")]
    [SerializeField] private string[] tagsQueRecebemDano = { "Vermelho" };

    private Transform alvo;
    private Vector3 direcaoInicial;
    private bool jaColidiu;
    private bool direcaoDefinida;

    public void Configurar(Transform novoAlvo, int novoDano, float novaVelocidade)
    {
        alvo = novoAlvo;
        dano = novoDano;
        velocidade = novaVelocidade;

        DefinirDirecaoInicial();
    }

    private void Start()
    {
        if (!direcaoDefinida)
            direcaoInicial = transform.forward;

        Destroy(gameObject, tempoDeVida);
    }

    private void Update()
    {
        if (jaColidiu)
            return;

        transform.position += direcaoInicial.normalized * velocidade * Time.deltaTime;
    }

    private void DefinirDirecaoInicial()
    {
        if (alvo != null)
        {
            Vector3 destino = alvo.position;
            destino.y = transform.position.y;

            direcaoInicial = destino - transform.position;

            if (direcaoInicial.sqrMagnitude < 0.001f)
                direcaoInicial = transform.forward;
        }
        else
        {
            direcaoInicial = transform.forward;
        }

        direcaoInicial.Normalize();
        transform.rotation = Quaternion.LookRotation(direcaoInicial, Vector3.up);
        direcaoDefinida = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        ColidiuCom(collision.collider.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        ColidiuCom(other.gameObject);
    }

    public void ColidiuCom(GameObject objetoAtingido)
    {
        if (jaColidiu)
            return;

        jaColidiu = true;

        TentarAplicarDano(objetoAtingido);
        Destroy(gameObject);
    }

    private void TentarAplicarDano(GameObject objetoAtingido)
    {
        if (objetoAtingido == null)
            return;

        Transform raizComTag = EncontrarPaiComTagPermitida(objetoAtingido.transform);

        if (raizComTag == null)
            return;

        Vida vida = raizComTag.GetComponent<Vida>();

        if (vida == null)
            vida = raizComTag.GetComponentInChildren<Vida>(true);

        if (vida == null)
            vida = raizComTag.GetComponentInParent<Vida>(true);

        if (vida != null)
        {
            vida.AplicarDano(dano);
            return;
        }

        if (TentarAplicarDanoPorMetodo(raizComTag, dano))
            return;

        TentarAplicarDanoPorMetodo(objetoAtingido.transform, dano);
    }

    private Transform EncontrarPaiComTagPermitida(Transform origem)
    {
        if (origem == null)
            return null;

        Transform atual = origem;

        while (atual != null)
        {
            if (TemTagPermitida(atual.gameObject))
                return atual;

            atual = atual.parent;
        }

        return null;
    }

    private bool TemTagPermitida(GameObject obj)
    {
        if (obj == null)
            return false;

        if (tagsQueRecebemDano == null || tagsQueRecebemDano.Length == 0)
            return true;

        for (int i = 0; i < tagsQueRecebemDano.Length; i++)
        {
            string tagPermitida = tagsQueRecebemDano[i];

            if (string.IsNullOrWhiteSpace(tagPermitida))
                continue;

            if (obj.CompareTag(tagPermitida))
                return true;
        }

        return false;
    }

    private bool TentarAplicarDanoPorMetodo(Transform alvoTransform, int valorDano)
    {
        if (alvoTransform == null)
            return false;

        if (TentarInvocarMetodoDeDanoNosComponentes(alvoTransform.GetComponents<MonoBehaviour>(), valorDano))
            return true;

        if (TentarInvocarMetodoDeDanoNosComponentes(alvoTransform.GetComponentsInChildren<MonoBehaviour>(true), valorDano))
            return true;

        if (TentarInvocarMetodoDeDanoNosComponentes(alvoTransform.GetComponentsInParent<MonoBehaviour>(true), valorDano))
            return true;

        return false;
    }

    private bool TentarInvocarMetodoDeDanoNosComponentes(MonoBehaviour[] componentes, int valorDano)
    {
        if (componentes == null || componentes.Length == 0)
            return false;

        for (int i = 0; i < componentes.Length; i++)
        {
            MonoBehaviour comp = componentes[i];

            if (comp == null)
                continue;

            if (TentarInvocarMetodo(comp, "AplicarDano", valorDano))
                return true;

            if (TentarInvocarMetodo(comp, "ReceberDano", valorDano))
                return true;

            if (TentarInvocarMetodo(comp, "TomarDano", valorDano))
                return true;
        }

        return false;
    }

    private bool TentarInvocarMetodo(MonoBehaviour componente, string nomeMetodo, int valorDano)
    {
        MethodInfo metodo = componente.GetType().GetMethod(
            nomeMetodo,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(int) },
            null
        );

        if (metodo == null)
            return false;

        metodo.Invoke(componente, new object[] { valorDano });
        return true;
    }
}