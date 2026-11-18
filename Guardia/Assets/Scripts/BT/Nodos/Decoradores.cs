// ═══════════════════════════════════════════════════════════════════════════
//  Decoradores.cs — Decoradores que no trae el runtime y nos hacían falta
//
//  El enunciado (§5.C) dice: «si necesitáis un decorador que no está, añadidlo:
//  eso suma, siempre que lo expliquéis». Van en un fichero NUESTRO: el runtime
//  (BehaviorTree.cs) no se toca.
//
//    RepetirSiempre       → vuelve a empezar al hijo cada vez que termina bien
//    LimiteDeTiempo       → corta al hijo si tarda más de N segundos
//    OlvidarSiFalsaAlarma → al cortarlo por calma, olvida la última posición
// ═══════════════════════════════════════════════════════════════════════════

using System;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    /// <summary>
    /// Repite al hijo indefinidamente. Si el hijo termina con Success, lo
    /// reinicia y devuelve Running; si falla, falla.
    ///
    /// Lo usa la patrulla: una ronda (ir al waypoint, descansar) termina, pero
    /// patrullar no termina nunca. Así la rama devuelve Running, como pide el
    /// profesor en el ejemplo de DecisorBT, en vez de Success.
    ///
    /// Tras el éxito NO vuelve a ejecutar al hijo en el mismo tick: espera al
    /// siguiente. Así, si el hijo terminase siempre al instante (por ejemplo,
    /// una ruta mal montada), no se quedaría en un bucle infinito.
    /// </summary>
    public class RepetirSiempre : Decorator
    {
        public RepetirSiempre(string nombre, Node hijo) : base(nombre, hijo) { }

        protected override Status Update(Blackboard bb)
        {
            Status s = Hijo.Tick(bb);

            if (s == Status.Success)
            {
                Hijo.Reset();   // para que el próximo Tick lo vuelva a inicializar
                return Status.Running;
            }

            return s;
        }
    }

    /// <summary>
    /// Deja que el hijo trabaje como mucho <c>segundos</c>. Si se acaba el
    /// tiempo, lo aborta y devuelve Failure. Si el hijo termina antes,
    /// devuelve lo que devuelva el hijo.
    ///
    /// Lo usa la búsqueda: EstadoBuscar se rinde a los TiempoBusqueda segundos
    /// aunque no haya revisado todos los puntos. Los segundos se leen al
    /// empezar (lambda), igual que en Esperar.
    /// </summary>
    public class LimiteDeTiempo : Decorator
    {
        private readonly Func<float> _segundos;
        private float _fin;

        public LimiteDeTiempo(string nombre, Func<float> segundos, Node hijo)
            : base(nombre, hijo)
        {
            _segundos = segundos;
        }

        protected override void OnInitialize(Blackboard bb)
        {
            _fin = bb.Tiempo + _segundos();
        }

        protected override Status Update(Blackboard bb)
        {
            if (bb.Tiempo >= _fin)
            {
                Hijo.Abort(bb);
                return Status.Failure;
            }

            return Hijo.Tick(bb);
        }
    }

    /// <summary>
    /// Envuelve la investigación. Si la cortan cuando el nivel ya ha bajado a
    /// Tranquilo, olvida la última posición: «falsa alarma, lo que vi ya no
    /// cuenta». Es exactamente lo que hace EstadoInvestigar al pasar a
    /// Patrullar.
    ///
    /// ¿Por qué un decorador y no OnTerminate en la hoja? Porque la
    /// investigación es una Sequence (pausa, acercarse) y Abort solo llega a
    /// la hoja que esté corriendo en ese momento. Si se calmara durante la
    /// pausa, la hoja AcercarseConCautela ni se enteraría. El decorador
    /// envuelve la Sequence entera y se entera siempre.
    /// </summary>
    public class OlvidarSiFalsaAlarma : Decorator
    {
        public OlvidarSiFalsaAlarma(string nombre, Node hijo) : base(nombre, hijo) { }

        protected override Status Update(Blackboard bb)
        {
            return Hijo.Tick(bb);
        }

        public override void Abort(Blackboard bb)
        {
            bool estabaCorriendo = Estado == Status.Running;

            base.Abort(bb);   // aborta al hijo y marca este nodo como Aborted

            if (estabaCorriendo &&
                bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo) == NivelSospecha.Tranquilo)
            {
                bb.Set(ClavesBB.TieneUltimaPos, false);
            }
        }
    }
}
