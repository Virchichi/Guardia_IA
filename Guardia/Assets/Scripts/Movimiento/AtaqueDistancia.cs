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

        [Header("Precisión: el factor de fallo")]
        [Tooltip("Error angular (grados) mínimo, aunque el jugador esté pegado al guardia.")]
        public float dispersionBase = 1f;

        [Tooltip("Grados de error extra por cada metro de distancia. " +
                 "Cuanto más lejos, peor puntería.")]
        public float dispersionPorMetro = 0.8f;

        [Tooltip("Error máximo (grados). A partir de aquí la puntería no empeora.")]
        public float dispersionMax = 12f;

        [Tooltip("True: 1/2 de probailidad para acertar, False: 1/3 de probalilidad de acertar")]
        public bool buenaPunteria; 


        /// <summary>Se lanza cada vez que sale una bala (lo usa AnimadorGuardia).</summary>
        public event System.Action AlDisparar;

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

            Quaternion deseada = Quaternion.LookRotation(hacia) * Quaternion.Euler(0, 30, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation , deseada,
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
            Vector3 direccion = CalcularDireccionConFallo(objetivo - origen).normalized;

            Bala bala = Instantiate(prefabBala, origen, Quaternion.LookRotation(direccion));
            bala.Lanzar(direccion, transform);

            AlDisparar?.Invoke();
            balas--;
            _proximoDisparo = Time.time + cadencia;
            return true;
        }
        /// <summary>
        /// Apunta de forma perfecta y luego desvía la dirección al azar dentro
        /// de un cono. El cono crece con la distancia: de cerca acierta casi
        /// siempre, de lejos falla más. Se limita a dispersionMax.
        /// </summary>
        private Vector3 CalcularDireccionConFallo(Vector3 haciaObjetivo)
        {
            float distancia = haciaObjetivo.magnitude;
            float error = Mathf.Min(dispersionBase + dispersionPorMetro * distancia, dispersionMax);

            // Punto aleatorio dentro de un círculo: x e y son grados de desvío.
            Vector2 desvio;
            if(buenaPunteria)
            {
                float r = Random.value * error;        // radio uniforme entre 0 y error
                float a = Random.value * 2f * Mathf.PI; // ángulo uniforme
                 desvio = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; //Guardia con mejor punteria. 1/2 de que se vaya fuera
            }
            else
            {
                 desvio = Random.insideUnitCircle * error; //Guardia con peor punteria. 2/3 de que se vaya fuera
            }

            Quaternion apunte = Quaternion.LookRotation(haciaObjetivo.normalized);
            Quaternion ruido = Quaternion.Euler(desvio.y, desvio.x, 0f);

            return (apunte * ruido) * Vector3.forward;
        }

    }
}
