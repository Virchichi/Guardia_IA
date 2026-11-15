using UnityEngine;

public class Bala : MonoBehaviour
{
    public float velocidad = 40f;

    public float vidaMax = 5f;

    public LayerMask capasImpacto = ~0;

    private Rigidbody _rb;
    private Transform _tirador;
    private bool _impactada;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        Collider col = GetComponent<Collider>();
    }

    public void Lanzar(Vector3 direccion, Transform tirador)
    {
        _tirador = tirador;

        Vector3 dir = direccion.normalized;
        transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 90f, 90f);
        _rb.linearVelocity = dir * velocidad;

        Destroy(gameObject, vidaMax);
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (_impactada) return;

        // Ignoramos al tirador
        if (_tirador != null && otro.transform.IsChildOf(_tirador)) return;

        // Ignorar triggers y capas que no están en capasImpacto
        if (otro.isTrigger) return;
        if ((capasImpacto.value & (1 << otro.gameObject.layer)) == 0) return;

        Impactar(otro);
    }

    private void Impactar(Collider otro)
    {
        _impactada = true;

        if (otro.CompareTag("Player"))
        {
            Debug.Log("[Bala] Impacto en el jugador: " + otro.name, otro);
        }

        Destroy(gameObject);
    }
}
