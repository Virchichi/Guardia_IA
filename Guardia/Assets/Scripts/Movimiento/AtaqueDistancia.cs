// ═══════════════════════════════════════════════════════════════════════════
//  AtaqueDistancia.cs — CAPA DE ACCIÓN: ejecuta los disparos que pide la decisión
//
//  Lee de la pizarra quiereDisparar y objetivoDisparo (los escribe
//  EstadoAtacar) y hace el trabajo físico: girar el cuerpo hacia el objetivo,
//  respetar la cadencia, recargar y crear la bala.
//
//  Va en el MISMO GameObject que GuardiaAgente.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace UDIT.IA3.Guardia
{
    [RequireComponent(typeof(GuardiaAgente))]
    public class AtaqueDistancia : MonoBehaviour
    {
        [Header("Cargador")]
        public int balasCargador = 15;
        public int balas = 0;
        public float timepoRecarga = 3f;
        public bool recargando = false;

        [Header("Disparo")]
        public Bala prefabBala;           // Prefab con el script Bala
        public Transform puntoDisparo;    // Desde dónde sale la bala (punta del arma)
        public float cadencia = 0.5f;     // Segundos mínimos entre disparos
        public float alturaApuntado = 1f; // Altura del pecho sobre el pivote del objetivo

        [Header("Apuntado")]
        [Tooltip("Grados por segundo que gira el guardia para encarar al objetivo.")]
        public float velocidadGiro = 360f;
        [Tooltip("Solo dispara si está encarado a menos de estos grados del objetivo.")]
        public float anguloMaximoDisparo = 15f;

        private GuardiaAgente _agente;
        private float _proximoDisparo;
        private float _terminarRecarga;

        void Awake()
        {
            _agente = GetComponent<GuardiaAgente>();
        }

        void Start()
        {
            balas = balasCargador;
        }

        void Update()
        {
            // Una recarga empezada termina aunque deje de atacar.
            if (balas <= 0 || recargando) Recargar();

            if (_agente == null || _agente.Pizarra == null) return;
            if (!_agente.Pizarra.Get(ClavesBB.QuiereDisparar, false)) return;

            Vector3 objetivo = _agente.Pizarra.Get(ClavesBB.ObjetivoDisparo, transform.position);

            // Girar hacia el objetivo (solo en horizontal). Parado, el
            // NavMeshAgent no rota, así que no nos pisamos con la locomoción.
            Vector3 hacia = objetivo - transform.position;
            hacia.y = 0f;
            if (hacia.sqrMagnitude < 0.001f) return;

            Quaternion deseada = Quaternion.LookRotation(hacia);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, deseada,
                                                          velocidadGiro * Time.deltaTime);

            // No dispara de espaldas: primero se encara.
            if (Quaternion.Angle(transform.rotation, deseada) <= anguloMaximoDisparo)
                Disparar(objetivo);
        }

        public void Recargar()
        {
            if (!recargando)
            {
                recargando = true;
                _terminarRecarga = Time.time + timepoRecarga;
                return;
            }

            if (Time.time >= _terminarRecarga)
            {
                balas = balasCargador;
                recargando = false;
            }
        }

        /// <summary>Devuelve true si sale la bala; false si no puede (cadencia, recarga, sin prefab).</summary>
        public bool Disparar(Vector3 posicionObjetivo)
        {
            if (balas <= 0 || Time.time < _proximoDisparo || recargando) return false;
            if (prefabBala == null) return false;

            Vector3 origen = puntoDisparo != null
                  ? puntoDisparo.position
                  : transform.position + Vector3.up * 1.5f;

            Vector3 objetivo = posicionObjetivo + Vector3.up * alturaApuntado;
            Vector3 direccion = (objetivo - origen).normalized;

            Bala bala = Instantiate(prefabBala, origen, Quaternion.LookRotation(direccion));
            bala.Lanzar(direccion, transform);

            balas--;
            _proximoDisparo = Time.time + cadencia;
            return true;
        }
    }
}
