// ═══════════════════════════════════════════════════════════════════════════
//  IObjetivoSigilo.cs — Lo que un objetivo le cuenta al sensor de visión
//
//  El sensor no conoce la clase del jugador: solo esta interfaz. Si el
//  objetivo la implementa, el sensor apunta los raycast a su altura REAL
//  (agachado detrás de una caja baja no se le ve) y aplica un factor de
//  postura a la evidencia (agachado se nota menos).
// ═══════════════════════════════════════════════════════════════════════════

namespace UDIT.IA3.Guardia
{
    public interface IObjetivoSigilo
    {
        /// <summary>Altura actual del cuerpo sobre el pivote, en metros.</summary>
        float AlturaVisible { get; }

        /// <summary>Multiplicador de la evidencia por postura: 1 de pie, menos agachado.</summary>
        float FactorPostura { get; }
    }
}
