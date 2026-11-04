// ═══════════════════════════════════════════════════════════════════════════
//  GuardBehaviour.cs — Integración del árbol en Unity 6
//
//  Inteligencia Artificial · GDDVV 3º · UDIT · Curso 2026/2027
//
//  Este fichero es el EJEMPLO DE CLASE (sesión 9). Muestra cómo se enchufa el
//  runtime de BehaviorTree.cs a un NPC de Unity: quién percibe, quién decide y
//  quién se mueve. Es la MISMA arquitectura que el proyecto base de la P1
//  (GuardiaAgente + SensorVision + DecisorBT + LocomocionNavMesh), metida en
//  un solo fichero para poder leerla de un tirón.
//
//  ┌─────────────────────────────────────────────────────────────────────┐
//  │  LAS TRES REGLAS DE LA P1, Y DÓNDE SE VEN AQUÍ                      │
//  │                                                                     │
//  │  1. La percepción escribe, la decisión lee     → Percibir()         │
//  │  2. La decisión NO mueve: las hojas PIDEN un   → Perseguir, IrA y   │
//  │     destino en la pizarra; solo Actuar() llama    Patrullar escriben │
//  │     al NavMeshAgent                               «destino»          │
//  │  3. Cambiar de cerebro no toca nada más        → ConstruirArbol()   │
//  │                                                                     │
//  │  Los métodos marcados con  ★ EJERCICIO  son los que tenéis que      │
//  │  escribir vosotros en la P1. Y recordad el requisito que de verdad  │
//  │  puntúa: la percepción con NIVEL DE SOSPECHA, no un booleano.       │
//  └─────────────────────────────────────────────────────────────────────┘
//
//  (Hasta el 21-oct-2026 las hojas de este ejemplo llamaban directamente a
//  SetDestination. Eso rompe la regla 2 de la P1 y se corrigió.)
//
//  Requiere: NavMeshAgent en el mismo GameObject, y un NavMeshSurface horneado.
//
//  ⚠️ Es para LEERLO y probarlo aparte. NO lo copiéis dentro del proyecto de
//  la P1: GuardiaBase_P1 ya trae su propio BehaviorTree.cs y las clases
//  chocarían. En consola: `dotnet run` (el .csproj deja fuera este fichero).
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.AI;
using UDIT.IA3.BT;

/// <summary>
/// Las claves de la pizarra. Escritas como constantes por la misma razón que
/// ClavesBB en la P1: "ultimaPos" en un sitio y "UltimaPos" en otro es media
/// tarde perdida.
/// </summary>
public static class Claves
{
    // Las escribe la PERCEPCIÓN
    public const string Visible        = "visible";
    public const string PosJugador     = "posJugador";
    public const string UltimaPos      = "ultimaPos";
    public const string TieneUltimaPos = "tieneUltimaPos";

    // Las escribe la DECISIÓN, las lee el MOVIMIENTO
    public const string Destino        = "destino";
    public const string TieneDestino   = "tieneDestino";

    // Las escribe el MOVIMIENTO, las lee la DECISIÓN
    public const string HaLlegado      = "haLlegado";
    public const string SinRuta        = "sinRuta";
}

[RequireComponent(typeof(NavMeshAgent))]
public class GuardBehaviour : MonoBehaviour
{
    [Header("Percepción")]
    [SerializeField] private Transform _jugador;
    [SerializeField] private float _alcanceVision = 15f;
    [SerializeField] private float _anguloVision  = 60f;
    [SerializeField] private LayerMask _capasQueTapan;

    [Header("Patrulla")]
    [SerializeField] private Transform[] _rutaPatrulla;

    [Header("Decisión")]
    [Tooltip("Veces por segundo que el NPC piensa. NO hace falta que sea 60. " +
             "En la P1 es GuardiaAgente.FrecuenciaDecision, también a 10.")]
    [SerializeField] private float _frecuenciaPensamiento = 10f;

    [Header("Movimiento")]
    [Tooltip("A qué distancia del destino se considera que ha llegado.")]
    [SerializeField] private float _toleranciaLlegada = 0.75f;

    private NavMeshAgent  _agente;
    private BehaviorTree  _arbol;
    private Blackboard    _pizarra;
    private int           _indicePatrulla = -1;   // el primero que pida será el 0
    private float         _proximaDecision;

    // ───────────────────────────────────────────────────────────────────────
    //  ARRANQUE
    // ───────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _agente  = GetComponent<NavMeshAgent>();
        _pizarra = new Blackboard();

        // Una pizarra vacía es una fuente inagotable de sorpresas: sembradla.
        _pizarra.Set(Claves.Visible, false);
        _pizarra.Set(Claves.TieneUltimaPos, false);
        _pizarra.Set(Claves.TieneDestino, false);
        _pizarra.Set(Claves.HaLlegado, true);
        _pizarra.Set(Claves.SinRuta, false);

        _arbol = new BehaviorTree(ConstruirArbol(), _pizarra);
    }

    // ───────────────────────────────────────────────────────────────────────
    //  EL CICLO: percibir → pensar → actuar  (como GuardiaAgente en la P1)
    // ───────────────────────────────────────────────────────────────────────

    private void Update()
    {
        _pizarra.Tiempo = Time.time;

        // 1. PERCIBIR — cada frame: es barato y queremos que sea fino.
        Percibir();

        // 2. PENSAR — solo _frecuenciaPensamiento veces por segundo. Con 40
        //    NPC en escena, la diferencia entre 10 Hz y 60 Hz es la diferencia
        //    entre 400 y 2.400 decisiones por segundo.
        if (Time.time >= _proximaDecision)
        {
            _proximaDecision = Time.time + 1f / Mathf.Max(1f, _frecuenciaPensamiento);
            _arbol.Tick();
        }

        // 3. ACTUAR — cada frame: el único sitio que toca el NavMeshAgent.
        Actuar();
    }

    // ───────────────────────────────────────────────────────────────────────
    //  PERCEPCIÓN                                            ★ EJERCICIO (P1)
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rellena la pizarra con lo que el guardia sabe del mundo.
    ///
    /// Lo que hay aquí es la versión MÍNIMA (un booleano). Para la Práctica 1
    /// tenéis que sustituirla por un medidor de sospecha continuo:
    ///
    ///   · sube más deprisa cuanto más cerca y más centrado esté el jugador
    ///   · decae solo con el tiempo cuando deja de verlo
    ///   · por debajo de un umbral el guardia patrulla; por encima, investiga;
    ///     y saturado, persigue
    ///
    /// Ese medidor es lo que separa a un guardia creíble de uno que se
    /// enciende y se apaga como un interruptor.
    /// </summary>
    private void Percibir()
    {
        bool visible = TieneLineaDeVision();

        if (visible) _pizarra.Set(Claves.PosJugador, _jugador.position);

        // Si acaba de perderlo, guardamos dónde estaba: es la pista que usará
        // la rama de búsqueda.
        if (_pizarra.Get(Claves.Visible, false) && !visible && _jugador != null)
        {
            _pizarra.Set(Claves.UltimaPos, _jugador.position);
            _pizarra.Set(Claves.TieneUltimaPos, true);
        }

        _pizarra.Set(Claves.Visible, visible);
    }

    private bool TieneLineaDeVision()
    {
        if (_jugador == null) return false;

        Vector3 haciaJugador = _jugador.position - transform.position;

        if (haciaJugador.magnitude > _alcanceVision) return false;
        if (Vector3.Angle(transform.forward, haciaJugador) > _anguloVision * 0.5f)
            return false;

        // ¿Hay algo en medio? (en la P1: contra varios puntos, no solo uno)
        return !Physics.Raycast(transform.position + Vector3.up,
                                haciaJugador.normalized,
                                haciaJugador.magnitude,
                                _capasQueTapan);
    }

    // ───────────────────────────────────────────────────────────────────────
    //  MOVIMIENTO — el único que llama a SetDestination
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lee «destino» de la pizarra y mueve. Escribe de vuelta si ha llegado y
    /// si no hay ruta. Es LocomocionNavMesh.Actuar() de la P1, casi literal.
    /// </summary>
    private void Actuar()
    {
        if (_agente == null || !_agente.isOnNavMesh) return;

        if (!_pizarra.Get(Claves.TieneDestino, false))
        {
            _agente.isStopped = true;
            _pizarra.Set(Claves.HaLlegado, true);
            return;
        }

        Vector3 destino = _pizarra.Get(Claves.Destino, transform.position);

        // Solo se recalcula la ruta si el destino ha cambiado de verdad.
        if ((_agente.destination - destino).sqrMagnitude > 0.01f)
            _agente.SetDestination(destino);

        _agente.isStopped = false;

        bool calculada = !_agente.pathPending;

        // Una ruta parcial es la puerta que alguien cerró: no se llega.
        _pizarra.Set(Claves.SinRuta,
                     calculada && _agente.pathStatus != NavMeshPathStatus.PathComplete);

        _pizarra.Set(Claves.HaLlegado,
                     calculada && _agente.remainingDistance <=
                         Mathf.Max(_toleranciaLlegada, _agente.stoppingDistance));
    }

    // ───────────────────────────────────────────────────────────────────────
    //  EL ÁRBOL                                              ★ EJERCICIO (P1)
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// El árbol del guardia: el de «El guardia definitivo» (B3.2b).
    ///
    /// Fijaos en dos cosas:
    ///   · la raíz es un ActiveSelector, para que ver al jugador INTERRUMPA
    ///     lo que estuviera haciendo;
    ///   · la rama de Cazar es un Monitor, no una Sequence, para que la
    ///     condición se vuelva a comprobar en cada tick.
    ///
    /// En la P1 las guardas no leen «visible» sino el NIVEL de sospecha
    /// (ClavesBB.Nivel), y hay una rama más: mirad el comentario de DecisorBT.
    /// </summary>
    private Node ConstruirArbol()
    {
        return new ActiveSelector("Guardia",

            // ── Prioridad 1: si lo ve, va a por él ──────────────────────────
            new Monitor("Cazar",
                new Condition("¿Lo ve?", bb => bb.Get(Claves.Visible, false)),
                new Perseguir()),

            // ── Prioridad 2: si lo perdió, busca ────────────────────────────
            new Sequence("Buscar",
                new Condition("¿Hay última posición?",
                              bb => bb.Get(Claves.TieneUltimaPos, false)),
                new IrA("Ir al último punto", Claves.UltimaPos),
                new ActionNode("Mirar alrededor", MirarAlrededor)),

            // ── Prioridad 3: por defecto, patrulla ──────────────────────────
            new ActionNode("Patrullar", Patrullar));
    }

    // ───────────────────────────────────────────────────────────────────────
    //  NODOS HOJA
    //
    //  Una acción devuelve Running mientras trabaja, Success cuando termina y
    //  Failure si no puede hacerlo. Y PIDE, no mueve: escribe Destino y
    //  TieneDestino, y mira HaLlegado.
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>★ EJERCICIO: girar sobre sí mismo un par de segundos.</summary>
    /// <remarks>
    /// Este esbozo termina AL INSTANTE, y por eso en la traza de consola sale
    /// un tick con «(nada en curso)». El vuestro tiene que durar: Running
    /// mientras gira, Success al acabar.
    /// </remarks>
    private Status MirarAlrededor(Blackboard bb)
    {
        bb.Set(Claves.TieneUltimaPos, false);   // se agotó la pista
        return Status.Success;
    }

    /// <summary>
    /// Patrullar no «termina» nunca: es la acción de fondo. Cuando la
    /// locomoción dice que ha llegado, pide el siguiente punto.
    /// </summary>
    private Status Patrullar(Blackboard bb)
    {
        if (_rutaPatrulla == null || _rutaPatrulla.Length == 0)
            return Status.Failure;

        if (bb.Get(Claves.HaLlegado, false) || !bb.Get(Claves.TieneDestino, false))
        {
            _indicePatrulla = (_indicePatrulla + 1) % _rutaPatrulla.Length;
            bb.Set(Claves.Destino, _rutaPatrulla[_indicePatrulla].position);
            bb.Set(Claves.TieneDestino, true);
            bb.Set(Claves.HaLlegado, false);
        }

        return Status.Running;
    }

    // ───────────────────────────────────────────────────────────────────────
    //  DEPURACIÓN
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Un árbol sin traza es imposible de depurar: no ves por qué rama ha
    /// bajado. Activadlo desde el inspector cuando algo no se comporte.
    /// </summary>
    [ContextMenu("Trazar un tick en la consola")]
    private void TrazarUnTick()
    {
        Trazador.Activo = true;
        _arbol.Tick();
        Debug.Log($"[{name}] {Trazador.Ultima()}", this);
        Trazador.Activo = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _alcanceVision);

        Vector3 izq = Quaternion.Euler(0, -_anguloVision * 0.5f, 0) * transform.forward;
        Vector3 der = Quaternion.Euler(0,  _anguloVision * 0.5f, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, izq * _alcanceVision);
        Gizmos.DrawRay(transform.position, der * _alcanceVision);
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Dos hojas con clase propia, porque necesitan OnInitialize y OnTerminate.
//  No son MonoBehaviour: son nodos del árbol, y solo hablan con la pizarra.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Pide ir a donde está el jugador mientras el Monitor lo permita.</summary>
public sealed class Perseguir : Node
{
    public Perseguir() : base("Perseguir") { }

    protected override Status Update(Blackboard bb)
    {
        bb.Set(Claves.Destino, bb.Get(Claves.PosJugador, Vector3.zero));
        bb.Set(Claves.TieneDestino, true);
        return Status.Running;                 // perseguir no «termina» nunca
    }

    protected override void OnTerminate(Blackboard bb, Status resultado)
    {
        // Lo ha abortado el Monitor (dejó de verlo). Si no se retira la
        // petición, la locomoción sigue corriendo hacia el sitio viejo.
        bb.Set(Claves.TieneDestino, false);
    }
}

/// <summary>Pide ir al punto guardado en una clave y espera a llegar.</summary>
public sealed class IrA : Node
{
    private readonly string _claveObjetivo;

    public IrA(string nombre, string claveObjetivo) : base(nombre)
    {
        _claveObjetivo = claveObjetivo;
    }

    protected override void OnInitialize(Blackboard bb)
    {
        // Configurar al ENTRAR (cap. 6, §6.3.3). Y HaLlegado a false: si no,
        // vale la llegada del destino anterior y la acción acaba en el acto.
        bb.Set(Claves.Destino, bb.Get(_claveObjetivo, Vector3.zero));
        bb.Set(Claves.TieneDestino, true);
        bb.Set(Claves.HaLlegado, false);
        bb.Set(Claves.SinRuta, false);
    }

    protected override Status Update(Blackboard bb)
    {
        if (bb.Get(Claves.SinRuta, false)) return Status.Failure;   // puerta cerrada
        return bb.Get(Claves.HaLlegado, false) ? Status.Success : Status.Running;
    }

    protected override void OnTerminate(Blackboard bb, Status resultado)
    {
        // Si la cortaron desde fuera, retirar lo pedido.
        if (resultado == Status.Aborted) bb.Set(Claves.TieneDestino, false);
    }
}
