// ═══════════════════════════════════════════════════════════════════════════
//  AnimacionGuardia.cs — Ayudante para cambiar de clip sin repetir CrossFade
//
//  Los estados se ejecutan 10 veces por segundo. Si cada tick llamaran a
//  Animator.CrossFade("Walk"), el clip se reiniciaría constantemente. Estos
//  métodos solo hacen el CrossFade si el Animator no está ya en ese clip
//  (o yendo hacia él).
//
//  Nombres de estado del Guardia_Anim_Controler: Idle, Walk, StopWalking, Morir.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public static class AnimacionGuardia
    {
        /// <summary>Pone la animación de andar.</summary>
        public static void Andar(Animator animator)
        {
            if (animator == null || EstaEn(animator, "Walk")) return;
            animator.CrossFade("Walk", 0.15f);
        }

        /// <summary>Frena (StopWalking, que luego pasa a Idle). Si ya está quieto, no hace nada.</summary>
        public static void Parar(Animator animator)
        {
            if (animator == null) return;
            if (EstaEn(animator, "Idle") || EstaEn(animator, "StopWalking")) return;
            animator.CrossFade("StopWalking", 0.2f);
        }

        /// <summary>¿Está en ese estado del Animator, o en transición hacia él?</summary>
        private static bool EstaEn(Animator animator, string estado)
        {
            return animator.GetCurrentAnimatorStateInfo(0).IsName(estado) ||
                   (animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName(estado));
        }
    }
}
