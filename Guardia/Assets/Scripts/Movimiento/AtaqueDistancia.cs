using UDIT.IA3.Guardia;
using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class AtaqueDistancia : MonoBehaviour
    {
        int balasCargador = 15;
        public int balas = 0;
       public Bala prefabBala; //Nombre del script de la bala 
        public Transform puntoDisparo; //Desde donde se va a disparar la bala  

        public float cadencia = 0.5f; //Segundos minimos entre disparos

        public float alturaApuntado = 1f; //altura del pecho 
        private float _proximoDisparo;
        // Start is called once before the first execution of Update after the MonoBehaviour is created

        public float timepoRecarga = 3f;
        public bool recargando = false;
        private float _terminarRecarga;
        void Start()
        {
            balas = balasCargador;
        }

        public void Recargar()
        {
            if (!recargando)_terminarRecarga = Time.time + timepoRecarga;
   
            if (Time.time < _terminarRecarga)
            { recargando = true; }
            else
            {
                balas = balasCargador;
                recargando = false;
            }

        }
        public bool Disparar(Vector3 posicionObjetivo) //Devuelve true si sale la bala, sino devuelve false
        {
            if ( balas <= 0 || Time.time < _proximoDisparo || recargando) return false;

            if (prefabBala == null) return false;


            Vector3 origen = puntoDisparo != null
                  ? puntoDisparo.position
                  : transform.position + Vector3.up * 1.5f;

            Vector3 objetivo = posicionObjetivo + Vector3.up * alturaApuntado;
            Vector3 direccion = CalcularDireccion(objetivo - origen);

            Bala bala = Instantiate(prefabBala, origen, Quaternion.LookRotation(direccion));
            bala.Lanzar(direccion, transform);


            balas--;
            _proximoDisparo = Time.time + cadencia;
            return true;
        }
        private Vector3 CalcularDireccion(Vector3 haciaObjetivo)
        {
            Quaternion apunte = Quaternion.LookRotation(haciaObjetivo.normalized);
            return apunte  * Vector3.forward;

        }

    }
}
