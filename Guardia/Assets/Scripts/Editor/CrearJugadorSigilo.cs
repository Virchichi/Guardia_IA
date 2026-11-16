// ═══════════════════════════════════════════════════════════════════════════
//  CrearJugadorSigilo.cs — Convierte al jugador de parkour en uno de sigilo
//
//  Menú: Tools > Jugador > Crear jugador de sigilo   (con SampleScene abierta)
//
//  1. Configura la importación de las animaciones de Assets/Animations/Player/
//     (Humanoid con el avatar del jugador, loop y raíz «Bake Into Pose»).
//  2. Crea Jugador_Sigilo.controller:
//       Locomocion = mezcla por «Agachado» de dos blend trees:
//         De pie:   Parado (Left Turn · Idle · Right Turn, por «Giro»)
//                   · Walking · Running                     (por «Velocidad»)
//         Agachado: Crouching Idle · Crouched Walking
//       Agacharse / Levantarse (Standing To Crouched / Crouched To Standing)
//       estando quieto, y Golpe (Hit Reaction) y Morir (Dying) desde cualquier estado.
//  3. En el jugador de la escena (tag Player): quita los scripts de parkour,
//     añade JugadorSigilo, AnimadorJugador y VidaJugador, asigna el controller
//     y desactiva Apply Root Motion.
//
//  Si falta una animación, usa un sustituto y lo avisa en la consola. Se puede
//  volver a ejecutar tras descargar las que faltan.
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UDIT.IA3.Jugador;

namespace UDIT.IA3.Guardia.Herramientas
{
    public static class CrearJugadorSigilo
    {
        const string CarpetaClips = "Assets/Animations/Player/";
        const string Modelo       = "Assets/Models/Player/Ch24_nonPBR.fbx";
        const string RutaAnimator = "Assets/Game/Animator/Jugador_Sigilo.controller";

        // Deben coincidir con JugadorSigilo.
        const float VelAgachado = 1.3f;
        const float VelSigilo   = 1.6f;   // JugadorSigilo.VelocidadSigilo (andar con «Walking»)
        const float VelCorrer   = 4.8f;

        static readonly Dictionary<string, bool> Clips = new Dictionary<string, bool>
        {
            { "Idle",             true  },   // ya la tenéis
            { "Walking",          true  },   // ya la tenéis
            { "Running",          true  },   // ya la tenéis
            { "Crouching Idle",   true  },   // nueva
            { "Crouched Walking", true  },   // nueva
            { "Hit Reaction",     false },   // nueva
            { "Dying",            false },   // nueva
            { "Standing To Crouched",  false },   // nueva
            { "Crouched To Standing",  false },   // nueva (si falta, usa «Crouch to Stand»)
            { "Crouch to Stand",       false },   // ya la tenéis (sustituto)
            { "Left Turn",             true  },   // nueva
            { "Right Turn",            true  },   // nueva
        };

        // Los giros conservan su rotación de raíz SIN aplicarla: las piernas dan
        // los pasos y el giro real lo hace JugadorSigilo.
        static readonly HashSet<string> SinBakeDeRotacion = new HashSet<string> { "Left Turn", "Right Turn" };

        static readonly string[] ScriptsParkour =
            { "ParkourControler", "EnvironmentScanner", "PlayerControlerNoState", "PlayerControler" };

        [MenuItem("Tools/Jugador/Crear jugador de sigilo")]
        public static void Crear()
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(Modelo).OfType<Avatar>().FirstOrDefault();
            if (avatar == null)
            {
                Debug.LogError("[Jugador Sigilo] No encuentro el Avatar de " + Modelo);
                return;
            }

            // ── 1. Clips ────────────────────────────────────────────────────
            var clip = new Dictionary<string, AnimationClip>();
            var faltan = new List<string>();
            foreach (var par in Clips)
            {
                string ruta = CarpetaClips + par.Key + ".fbx";
                if (!File.Exists(ruta)) { faltan.Add(par.Key); continue; }
                ConfigurarImportacion(ruta, par.Key, par.Value, avatar, !SinBakeDeRotacion.Contains(par.Key));
                clip[par.Key] = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>()
                                             .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            }

            AnimationClip idle = Obtener(clip, "Idle");
            AnimationClip walk = Obtener(clip, "Walking");
            if (idle == null || walk == null)
            {
                Debug.LogError("[Jugador Sigilo] Hacen falta al menos «Idle.fbx» y «Walking.fbx» en " + CarpetaClips);
                return;
            }
            AnimationClip run        = Obtener(clip, "Running");
            AnimationClip crouchIdle = Obtener(clip, "Crouching Idle") ?? idle;
            AnimationClip crouchWalk = Obtener(clip, "Crouched Walking") ?? walk;
            AnimationClip golpe      = Obtener(clip, "Hit Reaction");
            AnimationClip morir      = Obtener(clip, "Dying");
            AnimationClip aAgachado  = Obtener(clip, "Standing To Crouched");
            AnimationClip aDePie     = Obtener(clip, "Crouched To Standing") ?? Obtener(clip, "Crouch to Stand");
            AnimationClip giroIzq    = Obtener(clip, "Left Turn");
            AnimationClip giroDer    = Obtener(clip, "Right Turn");
            faltan.Remove("Crouch to Stand");
            if (aDePie != null) faltan.Remove("Crouched To Standing");

            // ── 2. Controller ───────────────────────────────────────────────
            AssetDatabase.DeleteAsset(RutaAnimator);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(RutaAnimator);
            ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Agachado",  AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Golpe",     AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Muerto",    AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Giro",      AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Agacharse", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Levantarse", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            AnimatorState loco = ctrl.CreateBlendTreeInController("Locomocion", out BlendTree raiz, 0);
            raiz.blendType = BlendTreeType.Simple1D;
            raiz.blendParameter = "Agachado";
            raiz.useAutomaticThresholds = false;

            BlendTree dePie = raiz.CreateBlendTreeChild(0f);
            dePie.name = "DePie";
            dePie.blendType = BlendTreeType.Simple1D;
            dePie.blendParameter = "Velocidad";
            dePie.useAutomaticThresholds = false;
            // Parado: giro en el sitio a izquierda/derecha según «Giro».
            if (giroIzq != null || giroDer != null)
            {
                BlendTree parado = dePie.CreateBlendTreeChild(0f);
                parado.name = "Parado";
                parado.blendType = BlendTreeType.Simple1D;
                parado.blendParameter = "Giro";
                parado.useAutomaticThresholds = false;
                parado.AddChild(giroIzq ?? idle, -1f);
                parado.AddChild(idle, 0f);
                parado.AddChild(giroDer ?? idle, 1f);
            }
            else
            {
                dePie.AddChild(idle, 0f);
            }

            dePie.AddChild(walk, VelSigilo);
            dePie.AddChild(run ?? walk, VelCorrer);
            if (run == null) AcelerarUltimo(dePie, VelCorrer / VelSigilo);

            BlendTree agachado = raiz.CreateBlendTreeChild(1f);
            agachado.name = "Agachado";
            agachado.blendType = BlendTreeType.Simple1D;
            agachado.blendParameter = "Velocidad";
            agachado.useAutomaticThresholds = false;
            agachado.AddChild(crouchIdle, 0f);
            agachado.AddChild(crouchWalk, VelAgachado);

            sm.defaultState = loco;

            // Agacharse / levantarse estando quieto. Si echa a andar a mitad,
            // vuelve a la locomoción sin esperar a que acabe el clip.
            CrearTransicionPostura(sm, loco, "Agacharse", aAgachado);
            CrearTransicionPostura(sm, loco, "Levantarse", aDePie);

            if (golpe != null)
            {
                AnimatorState sGolpe = sm.AddState("Golpe");
                sGolpe.motion = golpe;
                var t = sm.AddAnyStateTransition(sGolpe);
                t.hasExitTime = false; t.duration = 0.08f; t.canTransitionToSelf = true;
                t.AddCondition(AnimatorConditionMode.If, 0, "Golpe");
                var v = sGolpe.AddTransition(loco);
                v.hasExitTime = true; v.exitTime = 0.7f; v.duration = 0.25f;
            }

            if (morir != null)
            {
                AnimatorState sMorir = sm.AddState("Morir");
                sMorir.motion = morir;
                var t = sm.AddAnyStateTransition(sMorir);
                t.hasExitTime = false; t.duration = 0.15f; t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0, "Muerto");
            }

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();

            // ── 3. Jugador de la escena ─────────────────────────────────────
            ConfigurarJugador(ctrl);

            if (faltan.Count > 0)
                Debug.LogWarning("[Jugador Sigilo] Creado con sustitutos. Faltan en " + CarpetaClips + ": " +
                                 string.Join(", ", faltan.Select(f => f + ".fbx")) +
                                 ". Descargadlas de Mixamo con ese nombre y volved a ejecutar el menú.");
            else
                Debug.Log("[Jugador Sigilo] Jugador y Animator listos.");
        }

        // ───────────────────────────────────────────────────────────────────

        static void ConfigurarJugador(AnimatorController ctrl)
        {
            GameObject jugador = GameObject.FindWithTag("Player");
            if (jugador == null)
            {
                Debug.LogWarning("[Jugador Sigilo] No hay ningún objeto con tag Player en la escena abierta. " +
                                 "El Animator está creado: asignadlo a mano y añadid JugadorSigilo, AnimadorJugador y VidaJugador.");
                return;
            }

            // Quitar el sistema de parkour (en este orden: el parkour depende del controlador).
            foreach (string nombre in ScriptsParkour)
                foreach (var mb in jugador.GetComponents<MonoBehaviour>())
                    if (mb != null && mb.GetType().Name == nombre)
                        Undo.DestroyObjectImmediate(mb);

            if (jugador.GetComponent<JugadorSigilo>() == null)   Undo.AddComponent<JugadorSigilo>(jugador);
            if (jugador.GetComponent<AnimadorJugador>() == null) Undo.AddComponent<AnimadorJugador>(jugador);
            if (jugador.GetComponent<VidaJugador>() == null)     Undo.AddComponent<VidaJugador>(jugador);

            var js = jugador.GetComponent<JugadorSigilo>();
            if (js.Camara == null) js.Camara = Object.FindFirstObjectByType<CameraController>();

            Animator anim = jugador.GetComponent<Animator>();
            if (anim == null) anim = jugador.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                Undo.RecordObject(anim, "Animator jugador");
                anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
            }

            EditorUtility.SetDirty(jugador);
            EditorSceneManager.MarkSceneDirty(jugador.scene);
            Selection.activeGameObject = jugador;
        }

        static void CrearTransicionPostura(AnimatorStateMachine sm, AnimatorState loco, string trigger, AnimationClip clip)
        {
            if (clip == null) return;

            AnimatorState estado = sm.AddState(trigger);
            estado.motion = clip;

            var ida = loco.AddTransition(estado);
            ida.hasExitTime = false;
            ida.duration = 0.1f;
            ida.AddCondition(AnimatorConditionMode.If, 0, trigger);
            ida.AddCondition(AnimatorConditionMode.Less, 0.3f, "Velocidad");

            var fin = estado.AddTransition(loco);
            fin.hasExitTime = true;
            fin.exitTime = 0.85f;
            fin.duration = 0.2f;

            var corte = estado.AddTransition(loco);
            corte.hasExitTime = false;
            corte.duration = 0.2f;
            corte.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Velocidad");
        }

        static AnimationClip Obtener(Dictionary<string, AnimationClip> d, string nombre)
        {
            return d.TryGetValue(nombre, out var c) ? c : null;
        }

        static void AcelerarUltimo(BlendTree arbol, float escala)
        {
            var hijos = arbol.children;
            hijos[hijos.Length - 1].timeScale = escala;
            arbol.children = hijos;
        }

        static void ConfigurarImportacion(string ruta, string nombre, bool loop, Avatar avatar, bool bakeRotacion = true)
        {
            var imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
            if (imp == null) return;

            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            imp.sourceAvatar = avatar;
            imp.importAnimation = true;

            var clips = imp.clipAnimations;
            if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
            if (clips.Length == 0) return;

            var c = clips[0];
            c.name = nombre;
            c.loopTime = loop;
            c.loopPose = loop;
            c.lockRootRotation = bakeRotacion;
            c.lockRootHeightY = true;
            c.lockRootPositionXZ = true;
            imp.clipAnimations = new[] { c };

            imp.SaveAndReimport();
        }
    }
}
