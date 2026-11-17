// ═══════════════════════════════════════════════════════════════════════════
//  CrearAnimatorGuardia.cs — Monta el Animator del guardia con un clic
//
//  Menú: Tools > Guardia > Crear Animator
//
//  1. Configura la importación de cada FBX de Assets/Animations/Guardia/:
//     Humanoid copiando el avatar del modelo, clip renombrado, loop donde toca
//     y la raíz «Bake Into Pose» (el NavMeshAgent mueve, no la animación).
//  2. Crea Guardia_Animator.controller y la máscara Guardia_TroncoBrazos.
//  3. Asigna el controller al prefab Guardia_2, quita Apply Root Motion y le
//     añade AnimadorGuardia y RitmoGuardia si no los tiene.
//
//  Si falta alguna animación opcional, la sustituye por otra que sí tengáis y
//  lo avisa en la consola. Se puede volver a ejecutar las veces que haga falta
//  (por ejemplo, después de descargar las que faltan).
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace UDIT.IA3.Guardia.Herramientas
{
    public static class CrearAnimatorGuardia
    {
        const string CarpetaClips  = "Assets/Animations/Guardia/";
        const string Modelo        = "Assets/Models/Guardia/Ch15_nonPBR.fbx";
        const string RutaAnimator  = "Assets/Game/Animator/Guardia_Animator.controller";
        const string RutaMascara   = "Assets/Game/Animator/Guardia_TroncoBrazos.mask";
        // Se prueba en este orden (el prefab se ha llamado de las dos formas).
        static readonly string[] RutasPrefab = { "Assets/Prefabs/Guardia.prefab", "Assets/Prefabs/Guardia_2.prefab" };

        // Deben coincidir con RitmoGuardia (VelocidadAndar / VelocidadCorrer).
        const float VelAndar  = 1.6f;
        const float VelCorrer = 4f;

        // Nombre del archivo (sin .fbx) → ¿en bucle?
        static readonly Dictionary<string, bool> Clips = new Dictionary<string, bool>
        {
            { "Rifle Idle",        true  },   // ya la tenéis
            { "Rifle Walk",        true  },   // ya la tenéis
            { "Walking To Dying",  false },   // ya la tenéis
            { "Rifle Run",         true  },   // nueva
            { "Rifle Aiming Idle", true  },   // nueva
            { "Firing Rifle",      false },   // nueva
            { "Reloading",         false },   // nueva
            { "Looking Around",    true  },   // nueva
        };

        [MenuItem("Tools/Guardia/Crear Animator")]
        public static void Crear()
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(Modelo).OfType<Avatar>().FirstOrDefault();
            if (avatar == null)
            {
                Debug.LogError("[Animator Guardia] No encuentro el Avatar de " + Modelo +
                               ". Comprobad que el modelo está importado como Humanoid.");
                return;
            }

            // ── 1. Importación de los clips ─────────────────────────────────
            var clip = new Dictionary<string, AnimationClip>();
            var faltan = new List<string>();
            foreach (var par in Clips)
            {
                string ruta = CarpetaClips + par.Key + ".fbx";
                if (!File.Exists(ruta)) { faltan.Add(par.Key); continue; }

                ConfigurarImportacion(ruta, par.Key, par.Value, avatar);
                clip[par.Key] = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>()
                                             .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            }

            AnimationClip idle  = Obtener(clip, "Rifle Idle");
            AnimationClip walk  = Obtener(clip, "Rifle Walk");
            if (idle == null || walk == null)
            {
                Debug.LogError("[Animator Guardia] Hacen falta al menos «Rifle Idle.fbx» y «Rifle Walk.fbx» en " + CarpetaClips);
                return;
            }
            AnimationClip run    = Obtener(clip, "Rifle Run");
            AnimationClip aim    = Obtener(clip, "Rifle Aiming Idle") ?? idle;
            AnimationClip fire   = Obtener(clip, "Firing Rifle");
            AnimationClip reload = Obtener(clip, "Reloading");
            AnimationClip look   = Obtener(clip, "Looking Around");
            AnimationClip die    = Obtener(clip, "Walking To Dying");

            // ── 2. Máscara de tronco y brazos ───────────────────────────────
            AssetDatabase.DeleteAsset(RutaMascara);
            var mascara = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                var parte = (AvatarMaskBodyPart)i;
                bool arriba = parte == AvatarMaskBodyPart.Body || parte == AvatarMaskBodyPart.Head ||
                              parte == AvatarMaskBodyPart.LeftArm || parte == AvatarMaskBodyPart.RightArm ||
                              parte == AvatarMaskBodyPart.LeftFingers || parte == AvatarMaskBodyPart.RightFingers ||
                              parte == AvatarMaskBodyPart.LeftHandIK || parte == AvatarMaskBodyPart.RightHandIK;
                mascara.SetHumanoidBodyPartActive(parte, arriba);
            }
            AssetDatabase.CreateAsset(mascara, RutaMascara);

            // ── 3. Controller ───────────────────────────────────────────────
            AssetDatabase.DeleteAsset(RutaAnimator);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(RutaAnimator);
            ctrl.AddParameter("Velocidad",  AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Inquieto",   AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Apuntando",  AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Disparar",   AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Recargando", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Muerto",     AnimatorControllerParameterType.Trigger);

            // ── Capa base: locomoción, mirar alrededor y morir ──────────────
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            AnimatorState loco = ctrl.CreateBlendTreeInController("Locomocion", out BlendTree arbol, 0);
            arbol.blendType = BlendTreeType.Simple1D;
            arbol.blendParameter = "Velocidad";
            arbol.useAutomaticThresholds = false;
            arbol.AddChild(idle, 0f);
            arbol.AddChild(walk, VelAndar);
            if (run != null)
            {
                arbol.AddChild(run, VelCorrer);
            }
            else
            {
                // Sin «Rifle Run»: andar acelerado para no patinar.
                arbol.AddChild(walk, VelCorrer);
                var hijos = arbol.children;
                hijos[2].timeScale = VelCorrer / VelAndar;
                arbol.children = hijos;
            }
            sm.defaultState = loco;
            loco.iKOnFeet = true;   // ajusta los pies al suelo en la mezcla andar/correr

            if (look != null)
            {
                AnimatorState mirar = sm.AddState("MirarAlrededor");
                mirar.motion = look;

                var t1 = loco.AddTransition(mirar);
                Configurar(t1, 0.3f);
                t1.AddCondition(AnimatorConditionMode.If, 0, "Inquieto");

                var t2 = mirar.AddTransition(loco);
                Configurar(t2, 0.25f);
                t2.AddCondition(AnimatorConditionMode.IfNot, 0, "Inquieto");
            }

            if (die != null)
            {
                AnimatorState morir = sm.AddState("Morir");
                morir.motion = die;
                var t = sm.AddAnyStateTransition(morir);
                Configurar(t, 0.15f);
                t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0, "Muerto");
            }

            // ── Capa «Arma»: solo tronco y brazos ───────────────────────────
            var smArma = new AnimatorStateMachine { name = "Arma", hideFlags = HideFlags.HideInHierarchy };
            AssetDatabase.AddObjectToAsset(smArma, ctrl);
            ctrl.AddLayer(new AnimatorControllerLayer
            {
                name = "Arma",
                stateMachine = smArma,
                avatarMask = mascara,
                blendingMode = AnimatorLayerBlendingMode.Override,
                defaultWeight = 0f,          // AnimadorGuardia la sube al apuntar o recargar
            });

            AnimatorState apuntar = smArma.AddState("Apuntar");
            apuntar.motion = aim;
            smArma.defaultState = apuntar;

            if (fire != null)
            {
                AnimatorState disparar = smArma.AddState("Disparar");
                disparar.motion = fire;

                var a = apuntar.AddTransition(disparar);
                Configurar(a, 0.05f);
                a.AddCondition(AnimatorConditionMode.If, 0, "Disparar");

                var otra = disparar.AddTransition(disparar);   // ráfaga: vuelve a empezar
                Configurar(otra, 0.03f);
                otra.AddCondition(AnimatorConditionMode.If, 0, "Disparar");

                var vuelta = disparar.AddTransition(apuntar);
                vuelta.hasExitTime = true;
                vuelta.exitTime = 0.85f;
                vuelta.duration = 0.1f;
            }

            if (reload != null)
            {
                AnimatorState recargar = smArma.AddState("Recargar");
                recargar.motion = reload;

                var r = smArma.AddAnyStateTransition(recargar);
                Configurar(r, 0.15f);
                r.canTransitionToSelf = false;
                r.AddCondition(AnimatorConditionMode.If, 0, "Recargando");

                var fin = recargar.AddTransition(apuntar);
                Configurar(fin, 0.2f);
                fin.AddCondition(AnimatorConditionMode.IfNot, 0, "Recargando");
            }

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();

            // ── 4. Prefab ───────────────────────────────────────────────────
            AsignarAlPrefab(ctrl);

            // ── Resumen ─────────────────────────────────────────────────────
            if (faltan.Count > 0)
                Debug.LogWarning("[Animator Guardia] Creado con sustitutos. Faltan en " + CarpetaClips + ": " +
                                 string.Join(", ", faltan.Select(f => f + ".fbx")) +
                                 ". Descargadlas de Mixamo con ese nombre y volved a ejecutar el menú.");
            else
                Debug.Log("[Animator Guardia] Animator completo creado en " + RutaAnimator);

            Selection.activeObject = ctrl;
        }

        // ───────────────────────────────────────────────────────────────────

        static AnimationClip Obtener(Dictionary<string, AnimationClip> d, string nombre)
        {
            return d.TryGetValue(nombre, out var c) ? c : null;
        }

        static void Configurar(AnimatorStateTransition t, float duracion)
        {
            t.hasExitTime = false;
            t.duration = duracion;
        }

        static void ConfigurarImportacion(string ruta, string nombre, bool loop, Avatar avatar)
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

            // Mixamo trae un solo clip («mixamo.com»): lo dejamos con nombre propio.
            var c = clips[0];
            c.name = nombre;
            c.loopTime = loop;
            c.loopPose = loop;
            c.lockRootRotation = true;      // Bake Into Pose
            c.lockRootHeightY = true;
            c.lockRootPositionXZ = true;

            // Based Upon: Original en la rotación → no se balancea siguiendo el
            // torso (con «Body Orientation» el cuerpo entero oscila al correr).
            c.keepOriginalOrientation = true;
            // Based Upon: Feet en la altura → los pies quedan a ras del suelo
            // (con «Original» la cadera baja al correr y los pies lo atraviesan).
            c.keepOriginalPositionY = false;
            c.heightFromFeet = true;
            // Based Upon: Center of Mass en XZ → el cuerpo queda centrado bajo el
            // guardia. Con «Original», clips como «Rifle Run» desplazan el rig hacia
            // un lado (el origen del clip no está bajo el cuerpo).
            c.keepOriginalPositionXZ = false;
            imp.clipAnimations = new[] { c };

            imp.SaveAndReimport();
        }

        static void AsignarAlPrefab(AnimatorController ctrl)
        {
            string ruta = RutasPrefab.FirstOrDefault(File.Exists);
            if (ruta == null)
            {
                Debug.LogWarning("[Animator Guardia] No encuentro el prefab del guardia en Assets/Prefabs/. " +
                                 "Asignad Guardia_Animator al Animator del guardia a mano.");
                return;
            }

            GameObject raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                GuardiaAgente agente = raiz.GetComponentInChildren<GuardiaAgente>();
                GameObject go = agente != null ? agente.gameObject : raiz;

                // UN SOLO Animator, en el objeto del guardia. Si hay más en los
                // hijos (el modelo de Mixamo trae el suyo), se pelean por el
                // mismo esqueleto: balanceo, pies hundidos, poses mezcladas.
                Animator anim = go.GetComponent<Animator>();
                Animator[] todos = raiz.GetComponentsInChildren<Animator>(true);
                if (anim == null && todos.Length > 0) anim = todos[0];

                foreach (Animator otro in todos)
                {
                    if (otro == anim) continue;
                    if (anim != null && anim.avatar == null) anim.avatar = otro.avatar;
                    Debug.Log("[Animator Guardia] Quitado un Animator duplicado en «" + otro.gameObject.name + "».");
                    Object.DestroyImmediate(otro, true);
                }

                if (anim != null)
                {
                    anim.runtimeAnimatorController = ctrl;
                    anim.applyRootMotion = false;
                    anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                // Pies a ras del suelo: el NavMeshAgent sube el objeto «BaseOffset»
                // metros sobre el NavMesh, así que el modelo (pivote en los pies,
                // como los de Mixamo) tiene que estar exactamente a −BaseOffset.
                var nav = go.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (anim != null && nav != null)
                {
                    Transform modelo = null;
                    foreach (Transform hijo in go.transform)
                        if (hijo.GetComponentInChildren<SkinnedMeshRenderer>(true) != null) { modelo = hijo; break; }
                    if (modelo != null)
                    {
                        Vector3 p = modelo.localPosition;
                        if (Mathf.Abs(p.y + nav.baseOffset) > 0.001f)
                        {
                            Debug.Log("[Animator Guardia] Modelo «" + modelo.name + "» de Y=" + p.y + " a Y=" + (-nav.baseOffset) +
                                      " para que los pies queden sobre el suelo.");
                            modelo.localPosition = new Vector3(p.x, -nav.baseOffset, p.z);
                        }
                    }
                }

                if (go.GetComponent<AnimadorGuardia>() == null) go.AddComponent<AnimadorGuardia>();
                if (go.GetComponent<RitmoGuardia>() == null)    go.AddComponent<RitmoGuardia>();

                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                Debug.Log("[Animator Guardia] Prefab actualizado: " + ruta);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(raiz);
            }
        }
    }
}
