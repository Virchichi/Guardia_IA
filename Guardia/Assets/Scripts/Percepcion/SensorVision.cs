// ═══════════════════════════════════════════════════════════════════════════
//  SensorVision.cs — LA PIEZA PRINCIPAL DE LA PRÁCTICA
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  ⚠️  LO QUE OS DOY AQUÍ ESTÁ DELIBERADAMENTE MAL.
//
//  Tal y como está, este sensor detecta por DISTANCIA Y NADA MÁS. Es decir,
//  es exactamente el primero de los errores típicos de la sesión 4: el guardia
//  os ve a través de las paredes y de espaldas.
//
//  Está así a propósito. Arrancad el proyecto, jugad treinta segundos y veréis
//  el problema con vuestros propios ojos. Vuestro trabajo en el apartado A del
//  enunciado es convertir esto en un sensor que se pueda defender:
//
//     ETAPA 1  alcance   ✅ hecho (es lo único que hay)
//     ETAPA 2  ángulo    ✅ HECHO  (en el plano horizontal)
//     ETAPA 3  oclusión  ✅ HECHO  (raycast a cabeza, torso y pies)
//     MEDIDOR  sospecha  ✅ HECHO  (4 factores por producto con mínimos,
//                                   decaimiento con retardo, histéresis)
//
//  Filtrar barato antes de filtrar caro (sesión 4): el orden de las tres
//  etapas no es casual. La comprobación de distancia es un producto escalar;
//  el raycast es una consulta al motor de físicas. Poner el raycast primero
//  «funciona» y cuesta veinte veces más.
//
//  ── RESUMEN DE LO QUE HEMOS HECHO (para la memoria) ─────────────────────
//
//  · Evidencia = fDistancia · fAngulo · fVisibilidad · fMovimiento
//      Producto: las malas condiciones se acumulan (lejos Y de reojo Y medio
//      tapado casi no se nota). Pero cada factor tiene un MÍNIMO para que
//      ninguno por sí solo anule la detección: un jugador quieto delante de
//      las narices del guardia se tiene que ver.
//
//  · Para que el medidor NO FLUCTÚE:
//      1. Sube integrando evidencia·dt: nunca salta, siempre es progresivo.
//      2. Tiempo de gracia: perder de vista al jugador 1-2 frames (esquina,
//         borde del cono) no cuenta como «perderlo».
//      3. Retardo de olvido: tras perderlo, la sospecha se mantiene un rato
//         antes de empezar a bajar. Así no hay dientes de sierra si el
//         jugador entra y sale del cono.
//      4. Histéresis en los umbrales (método del profesor, abajo).
//
//  · Nervio: cada vez que lo pierde (VecesPerdido) olvida más despacio.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class SensorVision : MonoBehaviour, ISensor
    {
        [Header("Objetivo")]
        [Tooltip("El jugador. Si lo dejáis vacío se busca por tag al arrancar.")]
        public Transform Objetivo;
        public string TagObjetivo = "Player";

        [Header("Cono de visión")]
        public float Alcance = 15f;
        [Tooltip("Cono de visión DIRECTA (completo, en grados): el centro de la mirada. " +
                 "Aquí la detección es más rápida.")]
        [Range(0f, 180f)] public float AnguloVision = 60f;

        //----------------------------------------------------------------------------
        //            NUESTRO · VISIÓN PERIFÉRICA
        [Header("Visión periférica")]
        [Tooltip("Cono PERIFÉRICO completo (grados). Debe ser mayor que AnguloVision. " +
                 "Entre los dos conos está la periferia: se ve, pero se detecta más despacio.")]
        [Range(0f, 360f)] public float AnguloPeriferico = 140f;

        [Tooltip("Alcance de la visión periférica (m). Normalmente menor que el de la directa.")]
        public float AlcancePeriferico = 8f;

        [Tooltip("Ritmo de detección en la visión DIRECTA (multiplica la evidencia). 1 = normal.")]
        [Range(0f, 2f)] public float RitmoDirecta = 1f;

        [Tooltip("Ritmo de detección en la periferia, en el borde INTERIOR (junto a la directa). " +
                 "Hacia el borde exterior baja hasta MinimoAngulo. Menor que RitmoDirecta = tarda más.")]
        [Range(0f, 2f)] public float RitmoPeriferica = 0.45f;
        //----------------------------------------------------------------------------

        [Tooltip("Desde dónde se mira. Si está vacío se usa el propio transform. " +
                 "Poned aquí la cabeza: mirar desde los pies da resultados raros.")]
        public Transform Ojos;

        [Tooltip("Qué capas TAPAN la visión. Marcad aquí las paredes. " +
                 "Si dejáis esto en Nothing, no habrá oclusión y se notará.")]
        public LayerMask CapasQueOcluyen;

        //----------------------------------------------------------------------------
        //            NUESTRO
        [Header("Puntos del objetivo para la oclusión")]
        [Tooltip("Altura sobre el pivote del objetivo de cada raycast. Por defecto, " +
                 "pivote en los PIES (personaje de Mixamo / CharacterController). " +
                 "Con la cápsula de Unity (pivote en el centro): 0.8, 0 y -0.9.")]
        public float AlturaCabeza = 1.6f;
        public float AlturaTorso = 0.9f;
        public float AlturaPies = 0.1f;

        //----------------------------------------------------------------------------

        [Header("Medidor de sospecha — APARTADO A DEL ENUNCIADO")]
        [Tooltip("Cuánto sube por segundo con evidencia máxima.")]
        public float VelocidadSubida = 0.8f;

        [Tooltip("Cuánto baja por segundo sin ver nada. Debe ser MUY inferior " +
                 "a la de subida: subir en 1-2 s, olvidar en 10-15 s.")]
        public float VelocidadBajada = 0.08f;

        [Tooltip("Umbral tranquilo → sospecha (al subir).")]
        [Range(0f, 1f)] public float UmbralSospecha = 0.35f;

        [Tooltip("Umbral sospecha → alerta (al subir).")]
        [Range(0f, 1f)] public float UmbralAlerta = 0.8f;

        [Tooltip("Cuánto hay que bajar por debajo del umbral para volver atrás. " +
                 "Sin esto el guardia parpadea entre estados. Es la HISTÉRESIS.")]
        [Range(0f, 0.5f)] public float Histeresis = 0.1f;
        /// <summary>
        /// NUESTRO
        /// </summary>
        [Header("Factores de la evidencia")]
        [Tooltip("A esta distancia o menos, el factor distancia vale 1 (máximo).")]
        public float DistanciaCercana = 2f;

        [Tooltip("Velocidad del jugador (m/s) a partir de la cual el factor movimiento vale 1. " +
                 "Corriendo se nota más que andando, y andando más que quieto.")]
        public float VelocidadMuyLlamativa = 5f;

        [Tooltip("Mínimo de cada factor: evita que uno solo anule la detección por producto.")]
        [Range(0f, 1f)] public float MinimoDistancia = 0.2f;
        [Tooltip("Ritmo en el borde EXTERIOR de la visión periférica (lo peor que se ve).")]
        [Range(0f, 1f)] public float MinimoAngulo = 0.2f;
        [Range(0f, 1f)] public float MinimoMovimiento = 0.4f;

        [Header("Estabilidad (que no fluctúe)")]
        [Tooltip("Segundos sin verlo antes de darlo por PERDIDO. Evita que un parpadeo " +
                 "de 1-2 frames en una esquina o en el borde del cono cuente como perderlo.")]
        public float TiempoGracia = 0.25f;

        [Tooltip("Segundos que la sospecha se MANTIENE tras perderlo antes de empezar a bajar.")]
        public float RetardoOlvido = 1.5f;

        [Tooltip("Cuánto más despacio olvida por cada vez que lo ha perdido (VecesPerdido). " +
                 "0 = sin efecto. 0.25 con 3 pérdidas → olvida 1,75 veces más lento.")]
        [Range(0f, 1f)] public float Nervio = 0.25f;
        [Tooltip("Máximo de pérdidas que cuentan para el nervio, para que no deje de olvidar nunca.")]
        public int MaxPerdidasNervio = 3;

        //----------------------------------------------------------------------------
        //            NUESTRO · SEGUNDO SISTEMA: PROXIMIDAD (cuerpo a cuerpo)
        [Header("Proximidad (cuerpo a cuerpo)")]
        [Tooltip("Activa el segundo sistema: si el jugador se acerca demasiado, el guardia " +
                 "lo nota aunque esté de espaldas (360°), como si lo oyera o lo sintiera.")]
        public bool UsarProximidad = true;

        [Tooltip("Radio (m) alrededor del guardia en el que nota al jugador sin verlo.")]
        public float RadioProximidad = 1.8f;

        [Tooltip("Multiplica el radio si el jugador va agachado: así se puede pasar " +
                 "más pegado por la espalda. 1 = no cambia nada.")]
        [Range(0f, 1f)] public float MultiplicadorRadioAgachado = 0.6f;

        [Tooltip("Si está activo, la proximidad NO atraviesa paredes: hace falta que " +
                 "no haya un muro entre el guardia y el torso del jugador.")]
        public bool ProximidadRequiereLineaDirecta = true;

        [Tooltip("Cuánto sube la sospecha por segundo mientras está dentro del radio. " +
                 "2,5/s → de 0 a Alerta en ~0,3 s.")]
        public float VelocidadSubidaProximidad = 2.5f;

        [Tooltip("Si está activo, entrar en el radio pone la sospecha en Alerta al momento.")]
        public bool ProximidadAlertaInstantanea = false;
        //----------------------------------------------------------------------------

        // Estado interno del sensor. Lo publica en la pizarra al final de Percibir.
        private float _sospecha;
        private NivelSospecha _nivel = NivelSospecha.Tranquilo;
        private bool _visiblePrevio;

        // Para el tiempo de gracia y el retardo de olvido
        private float _ultimaVezVisto = -999f;
        private Vector3 _ultimaPosVista;

        // Para el factor movimiento: velocidad del objetivo medida desde fuera
        // (no le preguntamos nada al script del jugador: el sensor lo OBSERVA).
        private Vector3 _posObjetivoPrevia;
        private float _velocidadObjetivo;

        // Si el objetivo nos cuenta su postura (agachado, altura real), la usamos.
        private IObjetivoSigilo _objetivoSigilo;

        // Resultado de la última comprobación (los usa el medidor)
        private float _distancia;
        private float _angulo;
        private bool _enPeriferica;
        private int _rayosQueLlegan;

        /// <summary>Solo para los gizmos: si la última comprobación dio positivo.</summary>
        public bool VisibleAhora { get; private set; }
        /// <summary>Si este frame lo ha notado el sistema de proximidad (cuerpo a cuerpo).</summary>
        public bool CercaAhora { get; private set; }
        public float SospechaActual { get { return _sospecha; } }
        public NivelSospecha NivelActual { get { return _nivel; } }

        /// <summary>Para depurar y para la memoria: la evidencia del último frame y sus factores.</summary>
        public float EvidenciaActual { get; private set; }
        public float FactorDistancia { get; private set; }
        public float FactorAngulo { get; private set; }
        /// <summary>Si la última vez que lo vio fue con la visión periférica (no la directa).</summary>
        public bool EnPerifericaAhora { get { return VisibleAhora && _enPeriferica; } }
        public float FactorVisibilidad { get; private set; }
        public float FactorMovimiento { get; private set; }
        public float FactorPostura { get; private set; }

        private void Awake()
        {
            if (Ojos == null) Ojos = transform;

            if (Objetivo == null && !string.IsNullOrEmpty(TagObjetivo))
            {
                GameObject go = GameObject.FindGameObjectWithTag(TagObjetivo);
                if (go != null) Objetivo = go.transform;
            }

            if (Objetivo != null)
            {
                _posObjetivoPrevia = Objetivo.position;
                _objetivoSigilo = Objetivo.GetComponentInParent<IObjetivoSigilo>();
            }
        }

        // ───────────────────────────────────────────────────────────────────
        //  EL CICLO DE PERCEPCIÓN
        // ───────────────────────────────────────────────────────────────────

        public void Percibir(Blackboard bb)
        {
            if (Objetivo == null) return;

            float dt = Time.deltaTime;
            MedirVelocidadObjetivo(dt);

            // ── ¿Lo veo? ¿Lo noto cerca? ────────────────────────────────────
            // Dos sistemas independientes: la VISIÓN (cono + oclusión + medidor)
            // y la PROXIMIDAD (radio 360° cuerpo a cuerpo). Basta con uno de
            // los dos para que cuente como detectado este frame.
            bool vistoPorVision = ComprobarVision();
            bool notadoCerca = UsarProximidad && ComprobarProximidad();
            VisibleAhora = vistoPorVision;
            CercaAhora = notadoCerca;

            bool vistoEsteFrame = vistoPorVision || notadoCerca;

            if (vistoEsteFrame)
            {
                _ultimaVezVisto = Time.time;
                _ultimaPosVista = Objetivo.position;
            }

            // Con TIEMPO DE GRACIA: si lo he dejado de ver hace menos de
            // TiempoGracia, sigue contando como visible. Un parpadeo de un frame
            // en una esquina no es «perderlo».
            float sinVerlo = Time.time - _ultimaVezVisto;
            bool visible = sinVerlo <= TiempoGracia;

            // ── El medidor ──────────────────────────────────────────────────
            ActualizarSospecha(vistoPorVision, notadoCerca, sinVerlo, bb, dt);

            // ── Publicar en la pizarra ──────────────────────────────────────
            bb.Set(ClavesBB.Visible, visible);
            bb.Set(ClavesBB.Sospecha, _sospecha);
            bb.Set(ClavesBB.Nivel, _nivel);

            // La última posición conocida se escribe EN EL INSTANTE en que se
            // pierde el contacto. Ni antes (aún lo veo) ni después (ya no sé
            // dónde estaba).
            //
            // Mientras lo ve también se actualiza: es lo que usa Perseguir
            // para ir a por él. Al perderlo escribimos dónde lo VIMOS de
            // verdad por última vez (_ultimaPosVista), no dónde está ahora:
            // si usáramos Objetivo.position tras el tiempo de gracia, el
            // guardia sabría dónde se ha escondido sin haberlo visto.
            if (visible)
            {
                bb.Set(ClavesBB.UltimaPos, _ultimaPosVista);
                bb.Set(ClavesBB.TieneUltimaPos, true);
            }
            else if (_visiblePrevio)
            {
                bb.Set(ClavesBB.UltimaPos, _ultimaPosVista);
                bb.Set(ClavesBB.TieneUltimaPos, true);
                bb.Set(ClavesBB.VecesPerdido, bb.Get(ClavesBB.VecesPerdido, 0) + 1);
            }

            _visiblePrevio = visible;
        }

        // ───────────────────────────────────────────────────────────────────
        //  LAS TRES ETAPAS
        // ───────────────────────────────────────────────────────────────────

        private bool ComprobarVision()
        {
            _rayosQueLlegan = 0;

            // ETAPA 1 · ALCANCE ✅
            // Comparamos distancias al cuadrado para ahorrarnos la raíz.
            Vector3 haciaObjetivo = Objetivo.position - Ojos.position;

            if (haciaObjetivo.sqrMagnitude > Alcance * Alcance) return false;

            // ETAPA 2 · ÁNGULO ✅
            //
            // En el PLANO HORIZONTAL (Y a cero en los dos vectores): si no, el
            // jugador a los pies del guardia (muy por debajo de los ojos) daría
            // un ángulo grande y quedaría «fuera del cono» estando delante.
            // AnguloVision es el cono COMPLETO: comparamos con la mitad.
            Vector3 adelante = Ojos.forward;
            adelante.y = 0f;
            Vector3 haciaPlano = haciaObjetivo;
            haciaPlano.y = 0f;

            _distancia = haciaPlano.magnitude;
            _angulo = Vector3.Angle(adelante, haciaPlano);

            // Dos conos: dentro de AnguloVision es visión DIRECTA; entre ese y
            // AnguloPeriferico es visión PERIFÉRICA (con su propio alcance).
            float medioDirecto = AnguloVision * 0.5f;
            float medioPeriferico = Mathf.Max(AnguloPeriferico, AnguloVision) * 0.5f;

            if (_angulo > medioPeriferico) return false;

            _enPeriferica = _angulo > medioDirecto;
            if (_enPeriferica && _distancia > AlcancePeriferico) return false;

            // ETAPA 3 · OCLUSIÓN ✅
            //
            // Un raycast a cabeza, torso y pies. Si CUALQUIERA llega sin chocar
            // contra CapasQueOcluyen, se ve. Contamos cuántos llegan: es el
            // factor «visibilidad parcial» del medidor (asomar solo la cabeza
            // por encima de una caja se nota menos que estar a cuerpo entero).
            //
            // Physics.Raycast devuelve TRUE si ha chocado, es decir, si NO se ve.
            // Ignoramos triggers: un volumen invisible no tapa la vista.
            //
            // Si el objetivo es un IObjetivoSigilo, la cabeza y el torso se
            // calculan con su altura REAL: agachado detrás de una caja baja, el
            // rayo a la «cabeza de pie» pasaría por encima y lo vería.
            float cabeza = AlturaCabeza, torso = AlturaTorso;
            if (_objetivoSigilo != null)
            {
                cabeza = _objetivoSigilo.AlturaVisible * 0.92f;
                torso  = _objetivoSigilo.AlturaVisible * 0.55f;
            }

            Vector3 pivote = Objetivo.position;
            if (RayoLlega(pivote + Vector3.up * cabeza)) _rayosQueLlegan++;
            if (RayoLlega(pivote + Vector3.up * torso))  _rayosQueLlegan++;
            if (RayoLlega(pivote + Vector3.up * AlturaPies))   _rayosQueLlegan++;

            return _rayosQueLlegan > 0;
        }

        /// <summary>
        /// SEGUNDO SISTEMA · PROXIMIDAD. Solo distancia (360°, sin cono): si el
        /// jugador está pegado al guardia, lo nota aunque esté de espaldas.
        /// Primero la distancia (barato) y, si hace falta, un único raycast al
        /// torso para que no lo note a través de una pared.
        /// </summary>
        private bool ComprobarProximidad()
        {
            float radio = RadioProximidad;
            if (_objetivoSigilo != null && _objetivoSigilo.FactorPostura < 0.999f)
                radio *= MultiplicadorRadioAgachado;

            Vector3 delta = Objetivo.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > radio * radio) return false;

            if (!ProximidadRequiereLineaDirecta) return true;

            float torso = _objetivoSigilo != null ? _objetivoSigilo.AlturaVisible * 0.55f : AlturaTorso;
            return RayoLlega(Objetivo.position + Vector3.up * torso);
        }

        private bool RayoLlega(Vector3 punto)
        {
            Vector3 dir = punto - Ojos.position;
            float dist = dir.magnitude;
            if (dist < 0.001f) return true;

            bool choca = Physics.Raycast(Ojos.position, dir / dist, dist,
                                         CapasQueOcluyen, QueryTriggerInteraction.Ignore);
#if UNITY_EDITOR
            Debug.DrawLine(Ojos.position, punto, choca ? Color.red : Color.green);
#endif
            return !choca;
        }

        /// <summary>
        /// Velocidad del objetivo medida por diferencia de posiciones y
        /// suavizada, para que un frame raro no dispare el factor movimiento.
        /// </summary>
        private void MedirVelocidadObjetivo(float dt)
        {
            if (dt <= 0f) return;

            Vector3 delta = Objetivo.position - _posObjetivoPrevia;
            delta.y = 0f;   // saltar o subir escaleras no cuenta como «moverse más»
            float instantanea = delta.magnitude / dt;

            float suavizado = 1f - Mathf.Exp(-10f * dt);
            _velocidadObjetivo = Mathf.Lerp(_velocidadObjetivo, instantanea, suavizado);
            _posObjetivoPrevia = Objetivo.position;
        }

        // ───────────────────────────────────────────────────────────────────
        //  EL MEDIDOR DE SOSPECHA
        // ───────────────────────────────────────────────────────────────────

        private void ActualizarSospecha(bool vistoEsteFrame, bool notadoCerca, float sinVerlo, Blackboard bb, float dt)
        {
            // ── PROXIMIDAD: sube muy rápido (o salta a Alerta), con independencia
            //    del cono y de los factores de la visión.
            if (notadoCerca)
            {
                if (ProximidadAlertaInstantanea)
                    _sospecha = Mathf.Max(_sospecha, UmbralAlerta);
                else
                    _sospecha += VelocidadSubidaProximidad * dt;
            }

            if (vistoEsteFrame)
            {
                // ── 1) EVIDENCIA en [0,1] ───────────────────────────────────
                //
                // · Distancia: 1 a DistanciaCercana o menos, baja hasta el alcance.
                // · Ángulo (dos zonas): en la visión DIRECTA vale RitmoDirecta;
                //   en la PERIFÉRICA vale RitmoPeriferica junto a la directa y
                //   baja hasta MinimoAngulo en el borde exterior.
                // · Visibilidad parcial: rayos que llegan / 3.
                // · Movimiento (el nuestro): quieto se nota menos que corriendo.
                //
                // PRODUCTO con mínimos (Lerp desde el mínimo hasta 1): las malas
                // condiciones se acumulan, pero ningún factor solo la anula.
                float medioDirecto = AnguloVision * 0.5f;
                float medioPeriferico = Mathf.Max(AnguloPeriferico, AnguloVision) * 0.5f;

                FactorDistancia = Mathf.Lerp(MinimoDistancia, 1f,
                    Mathf.InverseLerp(Alcance, DistanciaCercana, _distancia));

                if (!_enPeriferica)
                    FactorAngulo = RitmoDirecta;
                else
                    FactorAngulo = Mathf.Lerp(MinimoAngulo, RitmoPeriferica,
                        Mathf.InverseLerp(medioPeriferico, medioDirecto, _angulo));

                FactorVisibilidad = _rayosQueLlegan / 3f;

                FactorMovimiento = Mathf.Lerp(MinimoMovimiento, 1f,
                    Mathf.InverseLerp(0f, VelocidadMuyLlamativa, _velocidadObjetivo));

                // · Postura: agachado se nota menos (si el objetivo lo informa).
                FactorPostura = _objetivoSigilo != null ? _objetivoSigilo.FactorPostura : 1f;

                EvidenciaActual = FactorDistancia * FactorAngulo * FactorVisibilidad *
                                  FactorMovimiento * FactorPostura;

                // ── 2) SUBIR: progresivo, nunca a saltos ────────────────────
                _sospecha += EvidenciaActual * VelocidadSubida * dt;
            }
            else if (!notadoCerca)
            {
                EvidenciaActual = 0f;

                // ── 2) BAJAR: solo tras el RETARDO de olvido, y más despacio
                //       cuanto más «nervioso» esté (veces que lo ha perdido).
                if (sinVerlo > RetardoOlvido)
                {
                    int perdidas = Mathf.Min(bb.Get(ClavesBB.VecesPerdido, 0), MaxPerdidasNervio);
                    float bajada = VelocidadBajada / (1f + Nervio * perdidas);
                    _sospecha -= bajada * dt;
                }
            }

            _sospecha = Mathf.Clamp01(_sospecha);

            // ── 3) Cruzar umbrales CON HISTÉRESIS ──────────────────────────
            _nivel = ActualizarNivel(_sospecha, _nivel);
        }

        /// <summary>
        /// Umbrales con histéresis: para SUBIR de nivel hace falta pasar el
        /// umbral; para BAJAR hace falta caer por debajo del umbral MENOS la
        /// histéresis. Sin esto, un valor oscilando alrededor de 0,35 hace que
        /// el guardia entre y salga de sospecha varias veces por segundo, y en
        /// pantalla parece que le da un ataque.
        ///
        /// Este método OS LO DOY HECHO porque es fácil equivocarse y no es lo
        /// interesante. Lo interesante es el medidor de arriba.
        /// </summary>
        private NivelSospecha ActualizarNivel(float s, NivelSospecha actual)
        {
            switch (actual)
            {
                case NivelSospecha.Tranquilo:
                    if (s >= UmbralSospecha) return NivelSospecha.Sospecha;
                    break;

                case NivelSospecha.Sospecha:
                    if (s >= UmbralAlerta) return NivelSospecha.Alerta;
                    if (s < UmbralSospecha - Histeresis) return NivelSospecha.Tranquilo;
                    break;

                case NivelSospecha.Alerta:
                    if (s < UmbralAlerta - Histeresis) return NivelSospecha.Sospecha;
                    break;
            }
            return actual;
        }
    }
}
