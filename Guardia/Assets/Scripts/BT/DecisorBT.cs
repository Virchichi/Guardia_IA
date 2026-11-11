// ═══════════════════════════════════════════════════════════════════════════
//  DecisorBT.cs — Cerebro versión 2: árbol de comportamiento
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  APARTADO C DEL ENUNCIADO.
//
//  El runtime (BehaviorTree.cs) OS LO DOY. No lo modifiquéis: en la sesión 9
//  lo explicamos línea a línea y en la memoria os preguntaré por él.
//
//  Lo que escribís vosotros es lo de este fichero: LA FORMA DEL ÁRBOL y LOS
//  NODOS HOJA. Eso es lo que se evalúa.
//
//  ⚠️ La condición del apartado D: este decisor tiene que producir el MISMO
//  comportamiento observable que DecisorFSM, leyendo la MISMA pizarra, sin que
//  haga falta tocar ni SensorVision ni LocomocionNavMesh. Si para que funcione
//  el árbol necesitáis cambiar algo de la percepción, algo está mal colocado.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class DecisorBT : IDecisor
    {
        // ── Parámetros de comportamiento ───────────────────────────────────
        // Ajustadlos jugando. Si cambiáis alguno, cambiad también el valor
        // equivalente en la FSM: las dos versiones deben comportarse igual.

        /// <summary>Investigar: a cuántos metros del jugador se para el guardia.</summary>
        private const float DistanciaInvestigar = 4f;

        /// <summary>Buscar: radio alrededor de la última posición en el que revisa puntos.</summary>
        private const float RadioBusqueda = 4f;

        /// <summary>Buscar: cuántos puntos revisa antes de rendirse.</summary>
        private const int PuntosDeBusqueda = 3;

        /// <summary>Buscar: segundos que se queda mirando en cada punto.</summary>
        private const float EsperaEnCadaPunto = 1.5f;

        /// <summary>Buscar: segundos que espera al final, antes de volver a patrullar.</summary>
        private const float EsperaAntesDeRendirse = 2f;

        private readonly GuardiaAgente _agente;
        private BehaviorTree _arbol;

        public DecisorBT(GuardiaAgente agente) { _agente = agente; }

        public void Iniciar(Blackboard bb)
        {
            _arbol = new BehaviorTree(ConstruirArbol(), bb);
        }

        public void Decidir(Blackboard bb)
        {
            if (_arbol != null) _arbol.Tick();
        }

        public string Traza { get { return "BT: " + Trazador.Ultima(); } }

        // ───────────────────────────────────────────────────────────────────
        //  EL ÁRBOL
        // ───────────────────────────────────────────────────────────────────
        //
        //   ActiveSelector "Guardia"   (reevalúa desde la izquierda en cada tick)
        //   │
        //   ├── Monitor "Perseguir"
        //   │     guarda: ¿lo veo?  Y  ¿nivel ≥ Alerta?
        //   │     cuerpo: Seguir(ultimaPos)
        //   │
        //   ├── Monitor "Investigar"
        //   │     guarda: ¿lo veo?  Y  ¿nivel ≥ Sospecha?
        //   │     cuerpo: Seguir(ultimaPos, parándose a DistanciaInvestigar)
        //   │
        //   ├── Monitor "Buscar"
        //   │     guarda: ¿NO lo veo?  Y  ¿tengo última posición?  Y  ¿nivel ≥ Sospecha?
        //   │     cuerpo: Sequence
        //   │               ├── IrA(ultimaPos)
        //   │               ├── Repeat x PuntosDeBusqueda
        //   │               │     └── Sequence: ElegirPunto → IrA(punto) → Esperar
        //   │               ├── Esperar
        //   │               └── OlvidarUltimaPos
        //   │
        //   └── Patrullar   (sin condición: es lo que hace si no pasa nada más)
        //
        // DECISIONES DE DISEÑO (para la memoria):
        //
        //  · Las ramas se ordenan por PRIORIDAD: perseguir > investigar > buscar
        //    > patrullar. El ActiveSelector garantiza que una rama más prioritaria
        //    interrumpe a la que esté corriendo (ver al jugador corta la patrulla).
        //
        //  · Se usa Monitor y no Sequence porque la condición debe seguir
        //    cumpliéndose DURANTE la acción. Con una Sequence, la condición solo
        //    se mira al empezar, y el guardia perseguiría a un jugador que ya no ve.
        //
        //  · Perseguir e Investigar exigen VERLO ahora. En cuanto lo pierde de
        //    vista, esas guardas fallan y entra Buscar, aunque el nivel siga en
        //    Alerta. Así se cumple la tabla del §3: «lo pierde de vista → busca
        //    alrededor de la última posición conocida».
        //
        //  · Buscar exige además nivel ≥ Sospecha. Si la sospecha decae hasta
        //    Tranquilo a mitad de búsqueda, el guardia se rinde y vuelve a
        //    patrullar. Eso es el «tras un tiempo» del §3, y lo marca el
        //    decaimiento del sensor, no un temporizador del árbol.
        //
        //  · Si lo ve estando en Tranquilo, no reacciona: sigue patrullando
        //    mientras la sospecha sube. Ese tramo intermedio es justo lo que hace
        //    que el guardia sea un medidor y no un interruptor.
        //
        //  · Ningún nodo guarda «en qué estado estoy». Qué hace el guardia sale
        //    únicamente de la forma del árbol y de lo que dice la pizarra.
        //
        //  · Ningún nodo mira el mundo ni mueve al guardia: leen la pizarra y
        //    escriben Destino/TieneDestino. LocomocionNavMesh hace el resto.

        private Node ConstruirArbol()
        {
            RutaPatrulla ruta = _agente.GetComponent<RutaPatrulla>();
            if (ruta == null || !ruta.TieneRuta)
                Debug.LogWarning("[" + _agente.name + "] no tiene RutaPatrulla con puntos. " +
                                 "Mirad los apartados 3.4 y 3.5 del README_Montaje.", _agente);

            return new ActiveSelector("Guardia",
                RamaPerseguir(),
                RamaInvestigar(),
                RamaBuscar(),
                new Patrullar("Patrullar", ruta)
            );
        }

        // ── Rama 1 · PERSEGUIR ────────────────────────────────────────────
        // Mientras lo vea y esté en alerta, va a por él. El sensor actualiza
        // ultimaPos en cada frame en que lo ve, así que seguir ultimaPos es
        // seguir al jugador.
        private Node RamaPerseguir()
        {
            return new Monitor("Perseguir",
                new Sequence("¿Lo veo y estoy en alerta?",
                    new CondicionClave("¿Visible?", ClavesBB.Visible, true),
                    new CondicionNivelAlMenos("¿Alerta?", NivelSospecha.Alerta)),
                new Seguir("IrAlJugador", UltimaPosicion));
        }

        // ── Rama 2 · INVESTIGAR ───────────────────────────────────────────
        // «Se orienta o se acerca, sin comprometerse aún»: se acerca, pero se
        // detiene a DistanciaInvestigar metros. Al moverse hacia él, el
        // NavMeshAgent lo orienta en esa dirección.
        private Node RamaInvestigar()
        {
            return new Monitor("Investigar",
                new Sequence("¿Lo veo y sospecho?",
                    new CondicionClave("¿Visible?", ClavesBB.Visible, true),
                    new CondicionNivelAlMenos("¿Sospecha?", NivelSospecha.Sospecha)),
                new Seguir("AcercarseConCautela", UltimaPosicion,
                           _agente.transform, DistanciaInvestigar));
        }

        // ── Rama 3 · BUSCAR ───────────────────────────────────────────────
        // Lo ha perdido de vista: va a la última posición conocida, revisa
        // varios puntos alrededor, espera un poco y, si no ha vuelto a verlo,
        // olvida la posición. Al olvidarla, la guarda falla y vuelve a patrullar.
        private Node RamaBuscar()
        {
            return new Monitor("Buscar",
                new Sequence("¿Lo he perdido?",
                    new CondicionClave("¿NoVisible?", ClavesBB.Visible, false),
                    new CondicionClave("¿TengoUltimaPos?", ClavesBB.TieneUltimaPos, true),
                    new CondicionNivelAlMenos("¿Sospecha?", NivelSospecha.Sospecha)),
                new Sequence("Busqueda",
                    new IrA("IrAUltimaPos", UltimaPosicion),
                    new Repeat(PuntosDeBusqueda,
                        new Sequence("RevisarPunto",
                            new ElegirPuntoDeBusqueda("ElegirPunto", RadioBusqueda),
                            new IrA("IrAPunto", bb => bb.Get(ClavesBB.PuntoBusqueda, Vector3.zero)),
                            new Esperar("MirarAlrededor", EsperaEnCadaPunto))),
                    new Esperar("UltimoVistazo", EsperaAntesDeRendirse),
                    new OlvidarUltimaPos("OlvidarUltimaPos")));
        }

        /// <summary>
        /// Lee la última posición conocida del jugador. Es un método con la misma
        /// forma que Func&lt;Blackboard, Vector3&gt;, así que se puede pasar
        /// directamente donde se espera una lambda (IrA, Seguir).
        /// </summary>
        private static Vector3 UltimaPosicion(Blackboard bb)
        {
            return bb.Get(ClavesBB.UltimaPos, Vector3.zero);
        }
    }
}
