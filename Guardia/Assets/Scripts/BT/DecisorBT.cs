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
        //   ActiveSelector "Guardia"            (reevalúa desde la izquierda en cada tick)
        //   │
        //   ├── Monitor "Combate"               guarda: ¿Alerta? Y ¿lo veo?
        //   │     └── Sequence "Combatir"
        //   │           ├── MarcarBusquedaPendiente
        //   │           └── ActiveSelector "AtacarOPerseguir"
        //   │                 ├── Monitor "Atacar"     guarda: ¿a tiro?  → Disparar
        //   │                 └── Seguir "Perseguir"
        //   │
        //   ├── Monitor "Buscar"                guarda: ¿búsqueda pendiente? O ¿Alerta?
        //   │     └── Sequence "Busqueda"
        //   │           ├── MarcarBusquedaPendiente
        //   │           ├── Succeeder → LimiteDeTiempo (TiempoBusqueda)
        //   │           │     └── Sequence "RecorrerZona"
        //   │           │           ├── IrA(ultimaPos) → Detenerse → Esperar(PausaBusqueda)
        //   │           │           ├── PrepararPuntosDeBusqueda
        //   │           │           └── Repeat x PuntosBusqueda
        //   │           │                 └── ElegirPunto → IrA → Detenerse → Esperar
        //   │           └── TerminarBusqueda
        //   │
        //   ├── Monitor "Investigar"            guarda: ¿Sospecha?
        //   │     └── OlvidarSiFalsaAlarma
        //   │           └── Sequence "Investigacion"
        //   │                 ├── Detenerse → Esperar(PausaInvestigar)
        //   │                 └── AcercarseConCautela
        //   │
        //   └── RepetirSiempre "Patrullar"
        //         └── Sequence "Ronda"
        //               ├── ¿Tengo ruta?
        //               ├── IrA(waypoint) → AvanzarRuta
        //               └── Detenerse → Esperar(TiempoDescanso)
        //
        // EQUIVALENCIA CON LA FSM (apartado D):
        //
        //   Rama del árbol        Estado(s) de la FSM
        //   ──────────────────    ─────────────────────────────
        //   Combate → Atacar      EstadoAtacar
        //   Combate → Perseguir   EstadoPerseguir (mientras lo ve)
        //   Buscar                EstadoPerseguir (sin verlo) + EstadoBuscar
        //   Investigar            EstadoInvestigar
        //   Patrullar             EstadoPatrullar + EstadoIdle
        //
        //   Todos los tiempos, distancias y radios se leen de GuardiaAgente, los
        //   mismos campos que usa la FSM. Cambiarlos en el inspector cambia a los
        //   dos cerebros a la vez.
        //
        // DECISIONES DE DISEÑO (para la memoria):
        //
        //  · Ramas ordenadas por PRIORIDAD con ActiveSelector: ver al jugador en
        //    alerta interrumpe cualquier otra cosa (patrulla, investigación o
        //    búsqueda), igual que en la FSM.
        //
        //  · Monitor y no Sequence: las guardas se vuelven a mirar en cada tick.
        //    Con una Sequence, el guardia seguiría disparando a quien ya no ve.
        //
        //  · Combate exige Alerta Y verlo. Si lo pierde de vista (aunque siga en
        //    Alerta), entra Buscar, cuyo primer paso es ir a la última posición
        //    conocida: lo mismo que hace EstadoPerseguir sin verlo, seguido de
        //    EstadoBuscar al llegar.
        //
        //  · Buscar va DELANTE de Investigar. En la FSM, una vez buscando, el
        //    guardia sigue buscando aunque la sospecha baje a Sospecha o a
        //    Tranquilo. En el árbol eso lo consigue la clave BusquedaPendiente:
        //    mientras valga true, la rama Buscar tiene prioridad sobre
        //    Investigar y Patrullar. Es la diferencia más interesante entre
        //    las dos arquitecturas: la FSM recuerda «dónde estaba» porque tiene
        //    un estado actual; el árbol no lo tiene, y ese recuerdo tiene que
        //    vivir en la pizarra.
        //
        //  · Recargar no es una rama: la recarga la gestiona AtaqueDistancia (capa
        //    de acción), igual que con la FSM. El árbol solo pide disparar.
        //
        //  · Ningún nodo guarda «en qué estado está el guardia». Ningún nodo mira
        //    el mundo (Raycast, sensor) ni mueve (SetDestination, NavMeshAgent):
        //    leen la pizarra y escriben intenciones (Destino, QuiereDisparar).

        private Node ConstruirArbol()
        {
            RutaPatrulla ruta = _agente.GetComponent<RutaPatrulla>();
            if (ruta == null || !ruta.TieneRuta)
                Debug.LogWarning("[" + _agente.name + "] no tiene RutaPatrulla con puntos. " +
                                 "Mirad los apartados 3.4 y 3.5 del README_Montaje.", _agente);

            return new ActiveSelector("Guardia",
                RamaCombate(),
                RamaBuscar(),
                RamaInvestigar(),
                RamaPatrullar(ruta)
            );
        }

        // ── Rama 1 · COMBATE (atacar o perseguir) ─────────────────────────
        // Lo ve y está en alerta. Si está a tiro, se para y dispara; si no, va
        // a por él. El ActiveSelector interior permite pasar de perseguir a
        // disparar (y al revés) en cuanto cambia la distancia.
        private Node RamaCombate()
        {
            return new Monitor("Combate",
                new Sequence("¿Alerta y lo veo?",
                    new CondicionNivel("¿Alerta?", NivelSospecha.Alerta),
                    new CondicionClave("¿Visible?", ClavesBB.Visible, true)),
                new Sequence("Combatir",
                    new MarcarBusquedaPendiente("RecordarQueHayQueBuscar"),
                    new ActiveSelector("AtacarOPerseguir",
                        new Monitor("Atacar",
                            new CondicionEnRangoDeDisparo("¿ATiro?", _agente),
                            new Disparar("Disparar")),
                        new Seguir("Perseguir"))));
        }

        // ── Rama 2 · BUSCAR ───────────────────────────────────────────────
        // Lo ha perdido de vista (o lo perdió hace poco y aún no ha terminado
        // de buscar). Va a la última posición, revisa puntos en círculo y, si
        // se acaban los puntos o el tiempo, se rinde: olvida la posición y la
        // guarda deja de cumplirse, así que vuelve a patrullar.
        private Node RamaBuscar()
        {
            return new Monitor("Buscar",
                new Selector("¿Hay que buscar?",
                    new CondicionClave("¿BusquedaPendiente?", ClavesBB.BusquedaPendiente, true),
                    new CondicionNivel("¿Alerta?", NivelSospecha.Alerta)),
                new Sequence("Busqueda",
                    new MarcarBusquedaPendiente("RecordarQueHayQueBuscar"),
                    new Succeeder(
                        new LimiteDeTiempo("MaximoTiempoBusqueda", () => _agente.TiempoBusqueda,
                            new Sequence("RecorrerZona",
                                new IrA("IrAUltimaPos", UltimaPosicion),
                                new Detenerse("Pararse"),
                                new Esperar("MirarAlrededor", () => _agente.PausaBusqueda),
                                new PrepararPuntosDeBusqueda("PrepararPuntos"),
                                new Repeat(_agente.PuntosBusqueda,
                                    new Sequence("RevisarPunto",
                                        new ElegirPuntoDeBusqueda("ElegirPunto", _agente),
                                        new IrA("IrAPunto", bb => bb.Get(ClavesBB.PuntoBusqueda, Vector3.zero)),
                                        new Detenerse("Pararse"),
                                        new Esperar("MirarAlrededor", () => _agente.PausaBusqueda)))))),
                    new TerminarBusqueda("Rendirse")));
        }

        // ── Rama 3 · INVESTIGAR ───────────────────────────────────────────
        // «¿Qué ha sido eso?»: se para, espera un momento y se acerca un poco
        // hacia lo que ha visto, sin comprometerse. Si la sospecha se calma,
        // OlvidarSiFalsaAlarma borra la última posición (como la FSM).
        private Node RamaInvestigar()
        {
            return new Monitor("Investigar",
                new CondicionNivel("¿Sospecha?", NivelSospecha.Sospecha),
                new OlvidarSiFalsaAlarma("OlvidarSiFalsaAlarma",
                    new Sequence("Investigacion",
                        new Detenerse("PararseEnSeco"),
                        new Esperar("Pausa", () => _agente.PausaInvestigar),
                        new AcercarseConCautela("AcercarseConCautela", _agente))));
        }

        // ── Rama 4 · PATRULLAR ────────────────────────────────────────────
        // Sin condición: es lo que hace si no pasa nada más. Una ronda = ir al
        // waypoint, pasar al siguiente y descansar (EstadoPatrullar + EstadoIdle).
        // RepetirSiempre encadena rondas sin fin.
        private Node RamaPatrullar(RutaPatrulla ruta)
        {
            return new RepetirSiempre("Patrullar",
                new Sequence("Ronda",
                    new Condition("¿TengoRuta?", bb => ruta != null && ruta.TieneRuta),
                    new IrA("IrAWaypoint", bb => ruta.PuntoActual),
                    new AvanzarRuta("SiguienteWaypoint", ruta),
                    new Detenerse("Pararse"),
                    new Esperar("Descansar", () => _agente.TiempoDescanso)));
        }

        /// <summary>
        /// Lee la última posición conocida del jugador. Tiene la misma forma que
        /// Func&lt;Blackboard, Vector3&gt;, así que se puede pasar directamente
        /// donde se espera una lambda (como en IrA).
        /// </summary>
        private static Vector3 UltimaPosicion(Blackboard bb)
        {
            return bb.Get(ClavesBB.UltimaPos, Vector3.zero);
        }
    }
}
