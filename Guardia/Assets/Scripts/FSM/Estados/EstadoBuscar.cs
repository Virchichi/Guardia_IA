// ═══════════════════════════════════════════════════════════════════════════
//  EstadoBuscar.cs — Lo ha perdido de vista: busca alrededor de ultimaPos
//
//  1. Va a la última posición conocida.
//  2. Revisa PuntosBusqueda puntos en círculo alrededor (radio RadioBusqueda),
//     parándose PausaBusqueda segundos en cada uno.
//  3. Si se acaba el tiempo (TiempoBusqueda) o los puntos sin verlo, OLVIDA
//     la última posición (caducidad) y vuelve a patrullar.
//
//  Transiciones (todas leyendo la pizarra):
//    · Alerta y visible  → Perseguir
//    · tiempo o puntos agotados → Patrullar (tieneUltimaPos = false)
//
//  Los puntos se calculan con matemáticas, sin Raycast: si alguno cae dentro
//  de un muro, el NavMeshAgent va al punto alcanzable más cercano.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class EstadoBuscar : State
    {
        public EstadoBuscar(StateMachine machine) : base(machine) { }

        private float _inicio;
        private Vector3[] _puntos;
        private int _indice;            // -1 = yendo a ultimaPos; 0..n-1 = puntos del círculo
        private float _llegadaEn = -1f; // cuándo llegó al punto actual (-1 = aún andando)

        internal override void Enter()
        {
            _inicio = Pizarra.Tiempo;

            Vector3 centro = Pizarra.Get(ClavesBB.UltimaPos, Agente.transform.position);
            _puntos = GenerarPuntos(centro, Agente.RadioBusqueda, Agente.PuntosBusqueda);
            _indice = -1;

            IrA(centro);
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);
            bool visible = Pizarra.Get(ClavesBB.Visible, false);

            // ── 1. ¡Lo he vuelto a ver! ──────────────────────────────────────
            // Solo con Alerta Y visible: si exigiéramos solo Alerta, al entrar
            // desde Perseguir (aún en Alerta pero sin verlo) volveríamos a
            // Perseguir en el mismo tick y el guardia rebotaría entre estados.
            if (nivel == NivelSospecha.Alerta && visible)
            {
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
                return;
            }

            // ── 2. ¿Se acabó el tiempo de búsqueda? ─────────────────────────
            if (Pizarra.Tiempo - _inicio >= Agente.TiempoBusqueda)
            {
                Rendirse();
                return;
            }

            // ── 3. Ir de punto en punto, parándose a mirar en cada uno ──────
            if (!Pizarra.Get(ClavesBB.HaLlegado, false))
            {
                AnimacionGuardia.Andar(Agente.Animator);
                return;
            }

            // Acaba de llegar: empieza a contar la pausa.
            if (_llegadaEn < 0f)
            {
                _llegadaEn = Pizarra.Tiempo;
                Pizarra.Set(ClavesBB.TieneDestino, false);
                AnimacionGuardia.Parar(Agente.Animator);
                return;
            }

            if (Pizarra.Tiempo - _llegadaEn < Agente.PausaBusqueda) return;

            // Pausa terminada: siguiente punto.
            _indice++;
            if (_indice >= _puntos.Length)
            {
                Rendirse();
                return;
            }

            IrA(_puntos[_indice]);
        }

        internal override void Exit() { }

        // ───────────────────────────────────────────────────────────────────

        private void IrA(Vector3 punto)
        {
            Pizarra.Set(ClavesBB.Destino, punto);
            Pizarra.Set(ClavesBB.TieneDestino, true);
            Pizarra.Set(ClavesBB.HaLlegado, false);
            _llegadaEn = -1f;
            AnimacionGuardia.Andar(Agente.Animator);
        }

        /// <summary>No lo ha encontrado: caduca la última posición y vuelve a patrullar.</summary>
        private void Rendirse()
        {
            Pizarra.Set(ClavesBB.TieneUltimaPos, false);
            StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
        }

        /// <summary>Puntos repartidos en círculo, con un giro aleatorio para que no busque siempre igual.</summary>
        private static Vector3[] GenerarPuntos(Vector3 centro, float radio, int cantidad)
        {
            cantidad = Mathf.Max(1, cantidad);
            Vector3[] puntos = new Vector3[cantidad];
            float giro = Random.Range(0f, 360f);

            for (int i = 0; i < cantidad; i++)
            {
                float angulo = (giro + 360f * i / cantidad) * Mathf.Deg2Rad;
                puntos[i] = centro + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * radio;
            }
            return puntos;
        }
    }
}
