using UnityEngine;

[DisallowMultipleComponent]
public class Teste : MonoBehaviour
{
    [Header("Referência")]
    [SerializeField] private Transform objetoFilho;

    [Header("Configuração")]
    public float velocidadeRotacao = 50f;

    [Header("Eixo de Rotação (marque apenas um)")]
    public bool rotacionarX;
    public bool rotacionarY;
    public bool rotacionarZ;

    void Update()
    {
        if (objetoFilho == null) return;

        Vector3 eixo = Vector3.zero;

        if (rotacionarX) eixo = Vector3.right;
        if (rotacionarY) eixo = Vector3.up;
        if (rotacionarZ) eixo = Vector3.forward;

        objetoFilho.Rotate(eixo * velocidadeRotacao * Time.deltaTime);
    }
}