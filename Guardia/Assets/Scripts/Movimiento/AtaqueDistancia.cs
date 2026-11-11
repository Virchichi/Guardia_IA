using UDIT.IA3.Guardia;
using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class AtaqueDistancia : MonoBehaviour
    {
        int balasCargador = 15;
        public int balas = 0;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            balas = balasCargador;
        }

        public void Recargar()
        {
            balas = balasCargador;
        }
        public void Disparar(Vector3 Objetivo)
        {
            balas--;
        }

    }
}
