using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TheBreathlessStudyRoom.Core;
using TheBreathlessStudyRoom.Interactables;

namespace TheBreathlessStudyRoom.Editor
{
    /// <summary>
    /// Editor helper script that automates the rapid assembly of the high-fidelity MVP scene inside Unity.
    /// Generates the complete Game Manager, Environment Scenes (Classroom, Corridor, Guard Room) 
    /// aligned 1:1 with WebVR index.html, spatial sound nodes, CCTV camera feeds, and player XR Origin tracking rigs.
    /// </summary>
    public static class SceneSetupHelper
    {
        [MenuItem("The Breathless Study Room/Setup Complete MVP Scene", true)]
        public static bool SetupCompleteMVPSceneValidate()
        {
            return !EditorApplication.isPlaying;
        }

        [MenuItem("The Breathless Study Room/Setup Complete MVP Scene", false, 10)]
        public static void SetupCompleteMVPScene()
        {
            Debug.Log("[SceneSetupHelper] Starting scene assembly sequence...");

            // Play Mode protection safety bounds check
            if (EditorApplication.isPlaying)
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("窒息室 (The Breathless Study Room)",
                        "無法在播放模式 (Play Mode) 下建立場景！\n\n請先在編輯器頂部點擊 🛑 停止播放，回到編輯模式後再試一次。", 
                        "確定");
                }
                return;
            }

            // 1. Create a clean new Scene
            if (!Application.isBatchMode)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return; // User cancelled
                }
            }

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Remove the default Main Camera to avoid duplicate audio listener and camera transform conflicts
            var defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null)
            {
                UnityEngine.Object.DestroyImmediate(defaultCamera);
            }

            // Create global folder for scenes if it doesn't exist
            string scenesDir = "Assets/_Project/Scenes";
            if (!Directory.Exists(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
            }

            // 2. Initialize the _GameManager GameObject
            GameObject gameManagerObj = new GameObject("_GameManager");
            var timelineManager = gameManagerObj.AddComponent<TimelineManager>();
            Undo.RegisterCreatedObjectUndo(gameManagerObj, "Create _GameManager");

            // 3. Initialize the _AudioManager and spatial sound nodes
            GameObject audioManagerObj = new GameObject("_AudioManager");
            audioManagerObj.transform.SetParent(gameManagerObj.transform);
            var audioManager = audioManagerObj.AddComponent<AudioManager>();

            // Create Spatial Audio Source Nodes
            var deskHeartbeat = CreateAudioSourceNode("DeskHeartbeatSource", audioManagerObj.transform, 0f, 1.0f);
            var ambientSource = CreateAudioSourceNode("AmbientSource", audioManagerObj.transform, 0f, 0.35f, true);
            var classroomDoor = CreateAudioSourceNode("ClassroomDoorSource", audioManagerObj.transform, 1.0f, 0.8f);
            var leftWindow = CreateAudioSourceNode("LeftWindowSource", audioManagerObj.transform, 1.0f, 0.8f);
            var paSpeaker = CreateAudioSourceNode("PASpeakerSource", audioManagerObj.transform, 1.0f, 1.0f);

            // Connect references using SerializedObject to safely assign serialized private fields
            SerializedObject audioSO = new SerializedObject(audioManager);
            audioSO.FindProperty("_headAudioSource").objectReferenceValue = deskHeartbeat;
            audioSO.FindProperty("_ambientSource").objectReferenceValue = ambientSource;
            audioSO.FindProperty("_doorSource").objectReferenceValue = classroomDoor;
            audioSO.FindProperty("_windowSource").objectReferenceValue = leftWindow;
            audioSO.FindProperty("_broadcastSource").objectReferenceValue = paSpeaker;
            audioSO.FindProperty("_timelineManager").objectReferenceValue = timelineManager;
            audioSO.ApplyModifiedProperties();

            // 4. Initialize the _VisualDirector and URP Lighting Anomaly controller
            GameObject visualDirectorObj = new GameObject("_VisualDirector");
            var anomalyController = visualDirectorObj.AddComponent<AnomalyController>();
            Undo.RegisterCreatedObjectUndo(visualDirectorObj, "Create _VisualDirector");

            // 5. Initialize the _CCTVController
            GameObject cctvControllerObj = new GameObject("_CCTVController");
            var cctvMonitor = cctvControllerObj.AddComponent<SecurityCameraMonitor>();
            Undo.RegisterCreatedObjectUndo(cctvControllerObj, "Create _CCTVController");

            // Initialize anomaly SO for visual controller
            SerializedObject anomalySO = new SerializedObject(anomalyController);

            // ==================== SCENE 1: STUDY ROOM (自習室) ====================
            GameObject classroomParent = new GameObject("Environment_Classroom");
            classroomParent.transform.position = Vector3.zero;

            // 1. Classroom Box (Floor, Ceiling, Walls)
            try
            {
                // Floor (8x8 flat cube based on A-Frame width="8" height="8" and color #1a1b18)
                CreatePrimitiveCube("Floor", classroomParent.transform, new Vector3(0f, -0.025f, 0f), new Vector3(8f, 0.05f, 8f), GetColor("#1a1b18"));
                
                // Ceiling (8x8 flat cube based on position="0 3 0" and color #0c0d0b)
                CreatePrimitiveCube("Ceiling", classroomParent.transform, new Vector3(0f, 3.025f, 0f), new Vector3(8f, 0.05f, 8f), GetColor("#0c0d0b"));

                // Front Blackboard Wall (Z = +4 in Unity since A-Frame Z = -4, color #121411)
                CreatePrimitiveCube("FrontWall", classroomParent.transform, new Vector3(0f, 1.5f, 4f), new Vector3(8f, 3f, 0.1f), GetColor("#121411"));
                
                // Back Door Wall (Z = -4 in Unity since A-Frame Z = 4, color #121411)
                CreatePrimitiveCube("BackWall", classroomParent.transform, new Vector3(0f, 1.5f, -4f), new Vector3(8f, 3f, 0.1f), GetColor("#121411"));
                
                // Left Window Wall (X = -4 in Unity, color #0e100d)
                CreatePrimitiveCube("LeftWall", classroomParent.transform, new Vector3(-4f, 1.5f, 0f), new Vector3(0.1f, 3f, 8f), GetColor("#0e100d"));
                
                // Right Locker Wall (X = +4 in Unity, color #121411)
                CreatePrimitiveCube("RightWall", classroomParent.transform, new Vector3(4f, 1.5f, 0f), new Vector3(0.1f, 3f, 8f), GetColor("#121411"));

                // Ceiling Grid Lines
                CreatePrimitiveCube("CeilingGrid_X", classroomParent.transform, new Vector3(0f, 2.95f, 0f), new Vector3(8f, 0.1f, 0.05f), GetColor("#050505"));
                CreatePrimitiveCube("CeilingGrid_Z", classroomParent.transform, new Vector3(0f, 2.95f, 0f), new Vector3(0.1f, 0.05f, 8f), GetColor("#050505"));
                Debug.Log("[SceneSetupHelper] Classroom shell created successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error creating classroom shell: {ex}");
            }

            // 2. Blackboard Board Assembly (Front Wall Z=3.95f)
            MeshRenderer blackboardScreenRenderer = null;
            try
            {
                GameObject blackboardGroup = new GameObject("Blackboard_Board");
                blackboardGroup.transform.SetParent(classroomParent.transform);
                blackboardGroup.transform.localPosition = new Vector3(0f, 1.6f, 3.95f);

                // Blackboard slate frame (width 3.8, height 1.6, depth 0.05, color #1a2219)
                var blackboardSlate = CreatePrimitiveCube("BlackboardSlate", blackboardGroup.transform, Vector3.zero, new Vector3(3.8f, 1.6f, 0.05f), GetColor("#1a2219"));

                // Wood Frame Borders (wood brown #3d2314)
                CreatePrimitiveCube("FrameTop", blackboardGroup.transform, new Vector3(0f, 0.82f, 0f), new Vector3(3.9f, 0.06f, 0.08f), GetColor("#3d2314"));
                CreatePrimitiveCube("FrameBottom", blackboardGroup.transform, new Vector3(0f, -0.82f, 0f), new Vector3(3.9f, 0.06f, 0.08f), GetColor("#3d2314"));
                CreatePrimitiveCube("FrameRight", blackboardGroup.transform, new Vector3(1.92f, 0f, 0f), new Vector3(0.06f, 1.7f, 0.08f), GetColor("#3d2314"));
                CreatePrimitiveCube("FrameLeft", blackboardGroup.transform, new Vector3(-1.92f, 0f, 0f), new Vector3(0.06f, 1.7f, 0.08f), GetColor("#3d2314"));

                // Blackboard screen plane for dynamic textures
                var blackboardScreen = CreatePrimitiveCube("BlackboardScreen", blackboardGroup.transform, new Vector3(0f, 0f, -0.03f), new Vector3(3.6f, 1.4f, 0.01f), GetColor("#1a2219"));
                blackboardScreenRenderer = blackboardScreen.GetComponent<MeshRenderer>();

                // Setup mock chalkboard rules materials safely
                Material defaultChalk = CreateSafetyMaterial(Shader.Find("Universal Render Pipeline/Unlit"), Color.white, "Chalkboard_DefaultRules.mat");
                Material redChalk = CreateSafetyMaterial(Shader.Find("Universal Render Pipeline/Unlit"), Color.red, "Chalkboard_ConflictRules.mat");
                Material greenChalk = CreateSafetyMaterial(Shader.Find("Universal Render Pipeline/Unlit"), Color.green, "Chalkboard_DecryptRules.mat");
                Material greenExitChalk = CreateSafetyMaterial(Shader.Find("Universal Render Pipeline/Unlit"), new Color(0.2f, 1.0f, 0.4f), "Chalkboard_ExitRules.mat");

                // Assign Blackboard materials in Anomaly Controller
                anomalySO.FindProperty("_blackboardMeshRenderer").objectReferenceValue = blackboardScreenRenderer;
                anomalySO.FindProperty("_defaultRulesMaterial").objectReferenceValue = defaultChalk;
                anomalySO.FindProperty("_redConflictRulesMaterial").objectReferenceValue = redChalk;
                anomalySO.FindProperty("_greenDecryptRulesMaterial").objectReferenceValue = greenChalk;
                anomalySO.FindProperty("_greenExitRulesMaterial").objectReferenceValue = greenExitChalk;
                anomalySO.FindProperty("_timelineManager").objectReferenceValue = timelineManager;

                // Blackboard Interactable Gaze Selection
                var bbGaze = blackboardScreen.AddComponent<GazeDwellSelector>();
                bbGaze.interactionLayers = GetGazeInteractionLayer();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    bbGaze.OnDwellSelected, 
                    new UnityAction(timelineManager.LogCompliance)
                );
                Debug.Log("[SceneSetupHelper] Blackboard created successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error creating Blackboard: {ex}");
            }

            // 3. Classroom Window with Storm & Peeping Shadow (X = -3.95f)
            MeshRenderer windowGlassRenderer = null;
            GameObject windowAnomalyPlane = null;
            try
            {
                GameObject windowGroup = new GameObject("Classroom_Window");
                windowGroup.transform.SetParent(classroomParent.transform);
                windowGroup.transform.localPosition = new Vector3(-3.95f, 1.6f, 0f);
                windowGroup.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

                // Window Outer Frame (color #0a0a0c)
                CreatePrimitiveCube("OuterFrame", windowGroup.transform, Vector3.zero, new Vector3(2.4f, 1.6f, 0.05f), GetColor("#0a0a0c"));
                
                // Window Glass Pane (transparent blue, color #0b1a24)
                Material windowGlassMat = CreateLitMaterial(new Color(0.043f, 0.102f, 0.141f, 0.7f), 0.1f, 0.9f, true);
                windowGlassMat = SaveSafetyAsset(windowGlassMat, "WindowGlassMaterial.mat");
                
                var windowGlassPane = CreatePrimitiveCube("GlassPane", windowGroup.transform, new Vector3(0f, 0f, 0.03f), new Vector3(2.2f, 1.4f, 0.02f), Color.white);
                windowGlassRenderer = windowGlassPane.GetComponent<MeshRenderer>();
                windowGlassRenderer.sharedMaterial = windowGlassMat;

                // Window Pane Separators (color #08080a)
                CreatePrimitiveCube("SeparatorHorizontal", windowGroup.transform, new Vector3(0f, 0f, 0.04f), new Vector3(2.25f, 0.03f, 0.02f), GetColor("#08080a"));
                CreatePrimitiveCube("SeparatorVertical", windowGroup.transform, new Vector3(0f, 0f, 0.04f), new Vector3(0.03f, 1.45f, 0.02f), GetColor("#08080a"));

                // Anomaly Window Face Plane
                windowAnomalyPlane = CreatePrimitiveCube("WindowAnomalyFacePlane", windowGroup.transform, new Vector3(0f, 0f, -0.01f), new Vector3(0.8f, 0.8f, 0.01f), Color.black);
                windowAnomalyPlane.SetActive(false);

                Material windowFaceMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                windowFaceMat.color = new Color(1f, 1f, 1f, 0f); // Initially transparent
                windowFaceMat = SaveSafetyAsset(windowFaceMat, "WindowAnomalyFace.mat");
                windowAnomalyPlane.GetComponent<MeshRenderer>().material = windowFaceMat;

                // Connect Window materials inside anomaly SO
                anomalySO.FindProperty("_windowGlassRenderer").objectReferenceValue = windowGlassRenderer;
                anomalySO.FindProperty("_windowAnomalyPlane").objectReferenceValue = windowAnomalyPlane;
                anomalySO.FindProperty("_windowAnomalyFaceMaterial").objectReferenceValue = windowFaceMat;
                Debug.Log("[SceneSetupHelper] Window created successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error creating window components: {ex}");
            }

            // 4. RIGHT: Locker Set Assembly with Hidden Decrypt Paper (X = +3.9f, Z = -0.8f)
            GameObject secretLockerNote = null;
            GameObject doorPivotObj = null;
            try
            {
                Debug.Log("[SceneSetupHelper] Generating Locker Set Assembly...");
                GameObject lockerGroup = new GameObject("Locker_Set");
                lockerGroup.transform.SetParent(classroomParent.transform);
                // Placed flush against the right wall at X=3.75 (width is 0.5, extends from 3.5 to 4.0)
                lockerGroup.transform.localPosition = new Vector3(3.75f, 1.4f, -0.8f);
                lockerGroup.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

                // Main Locker Frame (width 1.6, height 2.0, depth 0.5, color #3b3a36, metalness 0.3)
                var lockerFrame = CreatePrimitiveCube("LockerFrame", lockerGroup.transform, Vector3.zero, new Vector3(1.6f, 2.0f, 0.5f), GetColor("#3b3a36"));
                var lockerFrameRenderer = lockerFrame.GetComponent<MeshRenderer>();
                if (lockerFrameRenderer.sharedMaterial != null)
                {
                    lockerFrameRenderer.sharedMaterial.SetFloat("_Metallic", 0.3f);
                    lockerFrameRenderer.sharedMaterial.SetFloat("_Smoothness", 0.1f);
                }

                // Left static door (color #2b2a26)
                CreatePrimitiveCube("LockerDoorLeft", lockerGroup.transform, new Vector3(-0.4f, 0f, 0.26f), new Vector3(0.75f, 1.9f, 0.03f), GetColor("#2b2a26"));

                // Right interactive door node (Gaze opens, pivots around its right edge)
                doorPivotObj = new GameObject("InteractiveLockerDoorPivot");
                doorPivotObj.transform.SetParent(lockerGroup.transform);
                doorPivotObj.transform.localPosition = new Vector3(0.4f + 0.36f, 0f, 0.26f); // Edge pivot
                
                var lockerDoorRight = CreatePrimitiveCube("LockerDoorRightMesh", doorPivotObj.transform, new Vector3(-0.36f, 0f, 0f), new Vector3(0.72f, 1.9f, 0.03f), GetColor("#2f2e2a"));
                // Grill slits
                CreatePrimitiveCube("GrillSlit1", lockerDoorRight.transform, new Vector3(0f, 0.7f, 0.02f), new Vector3(0.4f, 0.02f, 0.01f), GetColor("#151515"));
                CreatePrimitiveCube("GrillSlit2", lockerDoorRight.transform, new Vector3(0f, 0.6f, 0.02f), new Vector3(0.4f, 0.02f, 0.01f), GetColor("#151515"));
                // Handle
                CreatePrimitiveCube("LockerHandle", lockerDoorRight.transform, new Vector3(-0.3f, 0f, 0.03f), new Vector3(0.03f, 0.2f, 0.04f), GetColor("#111"));

                // Secret locker rules note paper (located inside, visible when right door pivots open)
                secretLockerNote = CreatePrimitiveCube("SecretLockerNote", lockerGroup.transform, new Vector3(0.4f, -0.1f, 0.1f), new Vector3(0.4f, 0.55f, 0.01f), GetColor("#f7f3e6"));
                var grabSecretNote = secretLockerNote.AddComponent<XRGrabInteractable>();
                var secretNoteRb = secretLockerNote.GetComponent<Rigidbody>();
                if (secretNoteRb != null)
                {
                    secretNoteRb.mass = 0.5f;
                    secretNoteRb.drag = 0.5f;
                    secretNoteRb.angularDrag = 0.5f;
                    secretNoteRb.useGravity = true;
                    secretNoteRb.isKinematic = false;
                }

                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                    grabSecretNote.selectEntered, 
                    new UnityAction(timelineManager.LogSecretFound)
                );

                // Door Interaction: open right door on gaze
                var doorGaze = lockerDoorRight.AddComponent<GazeDwellSelector>();
                doorGaze.interactionLayers = GetGazeInteractionLayer();
                
                GameObject doorController = new GameObject("LockerDoorController");
                doorController.transform.SetParent(gameManagerObj.transform);
                var doorRotator = doorController.AddComponent<LockerDoorOpener>();
                doorRotator.targetPivot = doorPivotObj.transform;
                doorRotator.secretNote = secretLockerNote;
                
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    doorGaze.OnDwellSelected, 
                    new UnityAction(doorRotator.OpenLockerDoor)
                );
                Debug.Log("[SceneSetupHelper] Locker Set Assembly and components generated successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error creating Locker Set Assembly: {ex}");
            }

            // 5. BACK: Locked Heavy Iron Door & Window (Z = -3.9f)
            GameObject classroomDoorPanel = null;
            Rigidbody signRb = null;
            AudioSource signAudio = null;
            DoorSignScare doorSignScare = null;
            try
            {
                GameObject doorGroup = new GameObject("Classroom_Door");
                doorGroup.transform.SetParent(classroomParent.transform);
                doorGroup.transform.localPosition = new Vector3(0f, 1.3f, -3.9f);
                doorGroup.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                // Door Frame (color #1b1510)
                CreatePrimitiveCube("DoorFrame", doorGroup.transform, Vector3.zero, new Vector3(1.4f, 2.5f, 0.1f), GetColor("#1b1510"));
                
                // Inner Door Panel (color #2b2118, width 1.2, height 2.4, depth 0.06)
                classroomDoorPanel = CreatePrimitiveCube("ClassroomDoorPanel", doorGroup.transform, new Vector3(0f, 0f, 0.02f), new Vector3(1.2f, 2.4f, 0.06f), GetColor("#2b2118"));
                
                // Metallic knob (color #888)
                var lockKnob = CreatePrimitiveSphere("LockKnob", classroomDoorPanel.transform, new Vector3(0.48f, 0f, 0.04f), new Vector3(0.1f, 0.1f, 0.1f), GetColor("#888"));
                var knobRenderer = lockKnob.GetComponent<MeshRenderer>();
                if (knobRenderer.sharedMaterial != null)
                {
                    knobRenderer.sharedMaterial.SetFloat("_Metallic", 0.9f);
                    knobRenderer.sharedMaterial.SetFloat("_Smoothness", 0.9f);
                }

                // Obs Window
                CreatePrimitiveCube("WindowInnerFrame", classroomDoorPanel.transform, new Vector3(0f, 0.5f, 0.01f), new Vector3(0.35f, 0.5f, 0.05f), GetColor("#0e0a05"));
                CreatePrimitiveCube("WindowGlass", classroomDoorPanel.transform, new Vector3(0f, 0.5f, 0.04f), new Vector3(0.3f, 0.45f, 0.01f), GetColor("#1a1a24"));
                
                // Admin shadow
                var guardSilhouette = CreatePrimitiveCube("GuardSilhouette", classroomDoorPanel.transform, new Vector3(0f, 0.5f, 0.02f), new Vector3(0.32f, 0.45f, 0.01f), Color.black);
                guardSilhouette.SetActive(false);

                // Dropping sign plate for DoorSignScare
                var doorSignPlate = CreatePrimitiveCube("DoorSignPlate", classroomDoorPanel.transform, new Vector3(0f, 0.9f, 0.06f), new Vector3(0.4f, 0.15f, 0.02f), GetColor("#111"));
                signRb = doorSignPlate.AddComponent<Rigidbody>();
                signRb.isKinematic = true;
                signRb.useGravity = false;
                
                signAudio = doorSignPlate.AddComponent<AudioSource>();
                signAudio.spatialBlend = 1f;

                // Environmental Jump Scare Component on Door
                doorSignScare = classroomDoorPanel.AddComponent<DoorSignScare>();

                // Add GazeDwellSelector and StageTransitionTrigger for stage transition
                var doorGaze = classroomDoorPanel.AddComponent<GazeDwellSelector>();
                doorGaze.interactionLayers = GetGazeInteractionLayer();
                
                var transitionTrigger = classroomDoorPanel.AddComponent<StageTransitionTrigger>();
                transitionTrigger.targetState = GameState.Scene2Corridor;
                transitionTrigger.checkClassroomLock = true;
                
                // Hook visual dwell and controller click select
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                    doorGaze.OnDwellSelected,
                    new UnityAction(transitionTrigger.Transition)
                );
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                    doorGaze.selectEntered,
                    new UnityAction(transitionTrigger.Transition)
                );

                Debug.Log("[SceneSetupHelper] Classroom Door created successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error creating classroom door components: {ex}");
            }

            // 6. CENTER: Player Desk Station (Z = 0.8f)
            GameObject lampGroup = null;
            GameObject deskLampBase = null;
            Light deskLampLight = null;
            GameObject rulesPaper = null;
            LampToggler toggleHelper = null;
            try
            {
                GameObject playerDeskGroup = new GameObject("PlayerDesk_Station");
                playerDeskGroup.transform.SetParent(classroomParent.transform);
                playerDeskGroup.transform.localPosition = new Vector3(0f, 0f, 0.8f);

                // Wooden desk (color #4a3423, width 1.4, height 0.06, depth 0.8)
                CreatePrimitiveCube("DeskTop", playerDeskGroup.transform, new Vector3(0f, 0.75f, 0f), new Vector3(1.4f, 0.06f, 0.8f), GetColor("#4a3423"));
                
                // Legs
                CreatePrimitiveCube("LegFL", playerDeskGroup.transform, new Vector3(-0.65f, 0.37f, 0.35f), new Vector3(0.06f, 0.74f, 0.06f), GetColor("#151515"));
                CreatePrimitiveCube("LegFR", playerDeskGroup.transform, new Vector3(0.65f, 0.37f, 0.35f), new Vector3(0.06f, 0.74f, 0.06f), GetColor("#151515"));
                CreatePrimitiveCube("LegBL", playerDeskGroup.transform, new Vector3(-0.65f, 0.37f, -0.35f), new Vector3(0.06f, 0.74f, 0.06f), GetColor("#151515"));
                CreatePrimitiveCube("LegBR", playerDeskGroup.transform, new Vector3(0.65f, 0.37f, -0.35f), new Vector3(0.06f, 0.74f, 0.06f), GetColor("#151515"));

                // Desk Lamp Group (A-Frame position="-0.4 0.78 -0.2" -> Unity Z=0.2)
                lampGroup = new GameObject("Desk_Lamp");
                lampGroup.transform.SetParent(playerDeskGroup.transform);
                lampGroup.transform.localPosition = new Vector3(-0.4f, 0.78f, 0.2f);

                // Lamp Base (breaths breathing lamp)
                deskLampBase = CreatePrimitiveCylinder("LampBase", lampGroup.transform, Vector3.zero, new Vector3(0.16f, 0.01f, 0.16f), GetColor("#2b2b2b"));
                
                // Stand
                var lampStand = CreatePrimitiveCylinder("LampStand", lampGroup.transform, new Vector3(0f, 0.12f, 0f), new Vector3(0.024f, 0.125f, 0.024f), GetColor("#2b2b2b"));
                lampStand.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);

                // Shade cap
                var lampShade = CreatePrimitiveCone("LampShade", lampGroup.transform, new Vector3(0.04f, 0.26f, 0f), new Vector3(0.18f, 0.1f, 0.18f), GetColor("#8b0000"));
                lampShade.transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);

                // Bulb
                var lampBulb = CreatePrimitiveSphere("LampBulb", lampShade.transform, new Vector3(0f, -0.04f, 0f), new Vector3(0.06f, 0.06f, 0.06f), Color.white);
                lampBulb.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

                // Spotlight
                GameObject spotlightObj = new GameObject("DeskLampSpotlight");
                spotlightObj.transform.SetParent(lampGroup.transform);
                spotlightObj.transform.localPosition = new Vector3(0.04f, 0.22f, 0f);
                spotlightObj.transform.localRotation = Quaternion.Euler(80f, 0f, 0f); // face downward onto rules paper
                
                deskLampLight = spotlightObj.AddComponent<Light>();
                deskLampLight.type = LightType.Spot;
                deskLampLight.color = GetColor("#ffebc2");
                deskLampLight.intensity = 3.5f;
                deskLampLight.spotAngle = 45f;
                deskLampLight.range = 3.5f;

                anomalySO.FindProperty("_deskLamp").objectReferenceValue = deskLampLight;

                // Desk Lamp Interaction: Toggle desk lamp and log choice (Rebellion)
                var lampGaze = deskLampBase.AddComponent<GazeDwellSelector>();
                lampGaze.interactionLayers = GetGazeInteractionLayer();
                
                GameObject lampController = new GameObject("LampController");
                lampController.transform.SetParent(gameManagerObj.transform);
                toggleHelper = lampController.AddComponent<LampToggler>();
                toggleHelper.TargetLight = deskLampLight;
                toggleHelper.TimelineManager = timelineManager;

                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    lampGaze.OnDwellSelected, 
                    new UnityAction(toggleHelper.ToggleDeskLamp)
                );

                // Alarm Clock
                GameObject clockGroup = new GameObject("Desk_Clock");
                clockGroup.transform.SetParent(playerDeskGroup.transform);
                clockGroup.transform.localPosition = new Vector3(0.4f, 0.8f, 0.2f);
                clockGroup.transform.localRotation = Quaternion.Euler(10f, -25f, 0f);
                CreatePrimitiveCube("ClockCasing", clockGroup.transform, Vector3.zero, new Vector3(0.3f, 0.12f, 0.1f), GetColor("#15151a"));
                CreatePrimitiveCube("ClockScreen", clockGroup.transform, new Vector3(0f, 0f, 0.052f), new Vector3(0.27f, 0.095f, 0.01f), Color.black);

                // Desk Rules paper
                rulesPaper = CreatePrimitiveCube("RulesClipboardNote", playerDeskGroup.transform, new Vector3(0f, 0.785f, -0.1f), new Vector3(0.42f, 0.01f, 0.52f), GetColor("#eae3d2"));
                rulesPaper.tag = "RulePaper";
                rulesPaper.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);

                var grabNote = rulesPaper.AddComponent<XRGrabInteractable>();
                var rulesPaperRb = rulesPaper.GetComponent<Rigidbody>();
                if (rulesPaperRb != null)
                {
                    rulesPaperRb.mass = 0.5f;
                    rulesPaperRb.drag = 0.5f;
                    rulesPaperRb.angularDrag = 0.5f;
                    rulesPaperRb.useGravity = true;
                    rulesPaperRb.isKinematic = false;
                }

                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                    grabNote.selectEntered, 
                    new UnityAction(timelineManager.LogSecretFound)
                );
                Debug.Log("[SceneSetupHelper] Player desk, lamp, clock, and rules paper created successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error creating Player Desk components: {ex}");
            }

            // 7. Ambient lights, backgrounds, and students card
            try
            {
                GameObject scatteredDesks = new GameObject("Scattered_Desks");
                scatteredDesks.transform.SetParent(classroomParent.transform);
                
                // Left Bg desk
                GameObject bgDeskLeft = new GameObject("BGDeskLeft");
                bgDeskLeft.transform.SetParent(scatteredDesks.transform);
                bgDeskLeft.transform.localPosition = new Vector3(-2f, 0f, 2.5f);
                bgDeskLeft.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);
                CreatePrimitiveCube("DeskTop", bgDeskLeft.transform, new Vector3(0f, 0.75f, 0f), new Vector3(1.2f, 0.06f, 0.7f), GetColor("#3a2a1b"));
                CreatePrimitiveCube("Leg1", bgDeskLeft.transform, new Vector3(-0.5f, 0.35f, 0f), new Vector3(0.05f, 0.7f, 0.05f), GetColor("#222"));
                CreatePrimitiveCube("Leg2", bgDeskLeft.transform, new Vector3(0.5f, 0.35f, 0f), new Vector3(0.05f, 0.7f, 0.05f), GetColor("#222"));

                // Right Bg desk
                GameObject bgDeskRight = new GameObject("BGDeskRight");
                bgDeskRight.transform.SetParent(scatteredDesks.transform);
                bgDeskRight.transform.localPosition = new Vector3(2f, 0f, 2.5f);
                bgDeskRight.transform.localRotation = Quaternion.Euler(0f, -10f, 0f);
                CreatePrimitiveCube("DeskTop", bgDeskRight.transform, new Vector3(0f, 0.75f, 0f), new Vector3(1.2f, 0.06f, 0.7f), GetColor("#3a2a1b"));
                CreatePrimitiveCube("Leg1", bgDeskRight.transform, new Vector3(-0.5f, 0.35f, 0f), new Vector3(0.05f, 0.7f, 0.05f), GetColor("#222"));
                CreatePrimitiveCube("Leg2", bgDeskRight.transform, new Vector3(0.5f, 0.35f, 0f), new Vector3(0.05f, 0.7f, 0.05f), GetColor("#222"));

                // Student card
                var studentCard = CreatePrimitiveCube("FloorStudentCard", classroomParent.transform, new Vector3(-0.8f, 0.01f, 0.4f), new Vector3(0.22f, 0.005f, 0.14f), GetColor("#eaeaea"));
                studentCard.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                var grabCard = studentCard.AddComponent<XRGrabInteractable>();
                var cardRb = studentCard.GetComponent<Rigidbody>();
                if (cardRb != null)
                {
                    cardRb.mass = 0.5f;
                    cardRb.drag = 0.5f;
                    cardRb.angularDrag = 0.5f;
                    cardRb.useGravity = true;
                    cardRb.isKinematic = false;
                }

                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                    grabCard.selectEntered, 
                    new UnityAction(timelineManager.LogSecretFound)
                );

                // Overhead Point lights
                GameObject lightsGroup = new GameObject("Overhead_Lights");
                lightsGroup.transform.SetParent(classroomParent.transform);

                var overheadLight1 = new GameObject("NeonLight_Left");
                overheadLight1.transform.SetParent(lightsGroup.transform);
                overheadLight1.transform.localPosition = new Vector3(-3f, 2.8f, 0f);
                var lightComp1 = overheadLight1.AddComponent<Light>();
                lightComp1.type = LightType.Point;
                lightComp1.range = 8f;
                lightComp1.intensity = 0.6f;
                lightComp1.color = GetColor("#cceeff");

                var overheadLight2 = new GameObject("NeonLight_Right");
                overheadLight2.transform.SetParent(lightsGroup.transform);
                overheadLight2.transform.localPosition = new Vector3(3f, 2.8f, 0f);
                var lightComp2 = overheadLight2.AddComponent<Light>();
                lightComp2.type = LightType.Point;
                lightComp2.range = 8f;
                lightComp2.intensity = 0.6f;
                lightComp2.color = GetColor("#cceeff");

                anomalySO.FindProperty("_overheadFluorescents").ClearArray();
                anomalySO.FindProperty("_overheadFluorescents").InsertArrayElementAtIndex(0);
                anomalySO.FindProperty("_overheadFluorescents").GetArrayElementAtIndex(0).objectReferenceValue = lightComp1;
                anomalySO.FindProperty("_overheadFluorescents").InsertArrayElementAtIndex(1);
                anomalySO.FindProperty("_overheadFluorescents").GetArrayElementAtIndex(1).objectReferenceValue = lightComp2;

                // Ambient light
                var globalAmbientObj = new GameObject("GlobalAmbientLight");
                globalAmbientObj.transform.SetParent(classroomParent.transform);
                var ambientLight = globalAmbientObj.AddComponent<Light>();
                ambientLight.type = LightType.Directional;
                ambientLight.intensity = 0.3f;
                ambientLight.color = GetColor("#111115");
                Debug.Log("[SceneSetupHelper] Classroom environment lighting and點綴 props loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error loading classroom lighting: {ex}");
            }


            // ==================== SCENE 2: THE CORRIDOR (走廊) ====================
            GameObject corridorParent = new GameObject("Environment_Corridor");
            corridorParent.transform.position = Vector3.zero;
            corridorParent.SetActive(false); // Hidden at start

            Light corrLightComp1 = null;
            Light corrLightComp2 = null;
            try
            {
                // Floor & Ceiling & Walls
                CreatePrimitiveCube("CorridorFloor", corridorParent.transform, new Vector3(0f, -0.025f, 0f), new Vector3(3f, 0.05f, 24f), GetColor("#0f100e"));
                CreatePrimitiveCube("CorridorCeiling", corridorParent.transform, new Vector3(0f, 2.825f, 0f), new Vector3(3f, 0.05f, 24f), GetColor("#050605"));
                CreatePrimitiveCube("CorridorWallLeft", corridorParent.transform, new Vector3(-1.5f, 1.4f, 0f), new Vector3(0.05f, 2.8f, 24f), GetColor("#121411"));
                CreatePrimitiveCube("CorridorWallRight", corridorParent.transform, new Vector3(1.5f, 1.4f, 0f), new Vector3(0.05f, 2.8f, 24f), GetColor("#121411"));

                // Door Group
                GameObject corrDoorGroup = new GameObject("EntranceClassroomDoor");
                corrDoorGroup.transform.SetParent(corridorParent.transform);
                corrDoorGroup.transform.localPosition = new Vector3(0f, 1.3f, -5f);
                CreatePrimitiveCube("DoorMesh", corrDoorGroup.transform, Vector3.zero, new Vector3(1.2f, 2.4f, 0.1f), GetColor("#2b2118"));
                CreatePrimitiveCube("DoorTagPlate", corrDoorGroup.transform, new Vector3(0f, 0.7f, 0.06f), new Vector3(0.4f, 0.15f, 0.01f), GetColor("#111"));

                // Bathroom Door Left
                GameObject bathroomDoorGroup = new GameObject("CreepyBathroomDoor");
                bathroomDoorGroup.transform.SetParent(corridorParent.transform);
                bathroomDoorGroup.transform.localPosition = new Vector3(-1.48f, 1.3f, 2f);
                bathroomDoorGroup.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                CreatePrimitiveCube("DoorMesh", bathroomDoorGroup.transform, Vector3.zero, new Vector3(1.0f, 2.2f, 0.04f), GetColor("#222522"));

                // Stairs Door Right
                GameObject stairsDoorGroup = new GameObject("StairsUpDoor");
                stairsDoorGroup.transform.SetParent(corridorParent.transform);
                stairsDoorGroup.transform.localPosition = new Vector3(1.48f, 1.0f, 4f);
                stairsDoorGroup.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                CreatePrimitiveCube("DoorMesh", stairsDoorGroup.transform, Vector3.zero, new Vector3(1.2f, 2.2f, 0.04f), GetColor("#0d0c0c"));

                // EXIT Safety Node
                GameObject exitGroup = new GameObject("ExitSafetyNode");
                exitGroup.transform.SetParent(corridorParent.transform);
                exitGroup.transform.localPosition = new Vector3(0f, 1.3f, 11f);

                var exitSignBox = CreatePrimitiveCube("ExitSignBox", exitGroup.transform, new Vector3(0f, 1.2f, 0f), new Vector3(0.5f, 0.25f, 0.15f), GetColor("#0d0d0d"));
                var exitSignScreen = CreatePrimitiveCube("ExitSignScreen", exitSignBox.transform, new Vector3(0f, 0f, 0.08f), new Vector3(0.45f, 0.2f, 0.01f), GetColor("#8b0000"));
                exitSignScreen.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                exitSignScreen.GetComponent<MeshRenderer>().sharedMaterial.color = GetColor("#8b0000");

                var exitGlassDoor = CreatePrimitiveCube("ExitGlassDoorMesh", exitGroup.transform, Vector3.zero, new Vector3(1.2f, 2.3f, 0.08f), Color.white);
                exitGlassDoor.GetComponent<MeshRenderer>().sharedMaterial = CreateLitMaterial(new Color(0.12f, 0.13f, 0.12f, 0.85f), 0.1f, 0.1f, true);
                CreatePrimitiveSphere("ExitKnob", exitGlassDoor.transform, new Vector3(0.4f, 0f, 0.05f), new Vector3(0.08f, 0.08f, 0.08f), Color.white);

                // Add GazeDwellSelector and StageTransitionTrigger for corridor transition to Guard Room
                var exitGaze = exitGlassDoor.AddComponent<GazeDwellSelector>();
                exitGaze.interactionLayers = GetGazeInteractionLayer();

                var exitTransition = exitGlassDoor.AddComponent<StageTransitionTrigger>();
                exitTransition.targetState = GameState.Scene3GuardRoom;
                exitTransition.checkClassroomLock = false;

                // Hook visual dwell and controller select
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                    exitGaze.OnDwellSelected,
                    new UnityAction(exitTransition.Transition)
                );
                UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                    exitGaze.selectEntered,
                    new UnityAction(exitTransition.Transition)
                );

                // Corridor Lights
                var corrLightObj1 = new GameObject("CorridorLight_1");
                corrLightObj1.transform.SetParent(corridorParent.transform);
                corrLightObj1.transform.localPosition = new Vector3(0f, 2.7f, 2f);
                corrLightComp1 = corrLightObj1.AddComponent<Light>();
                corrLightComp1.type = LightType.Point;
                corrLightComp1.range = 6f;
                corrLightComp1.intensity = 0.5f;
                corrLightComp1.color = GetColor("#aaffcc");

                var corrLightObj2 = new GameObject("CorridorLight_2");
                corrLightObj2.transform.SetParent(corridorParent.transform);
                corrLightObj2.transform.localPosition = new Vector3(0f, 2.7f, 8f);
                corrLightComp2 = corrLightObj2.AddComponent<Light>();
                corrLightComp2.type = LightType.Point;
                corrLightComp2.range = 6f;
                corrLightComp2.intensity = 0.5f;
                corrLightComp2.color = GetColor("#ffcccc");
                Debug.Log("[SceneSetupHelper] Corridor environment created successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error building Corridor: {ex}");
            }

            // Bind corridor lights to DoorSignScare
            if (doorSignScare != null && corrLightComp1 != null && corrLightComp2 != null)
            {
                doorSignScare.corridorLights = new Light[] { corrLightComp1, corrLightComp2 };
            }


            // ==================== SCENE 3: GUARD ROOM (警衛室) ====================
            GameObject guardRoomParent = new GameObject("Environment_GuardRoom");
            guardRoomParent.transform.position = Vector3.zero;
            guardRoomParent.SetActive(false); // Hidden at start

            GameObject stampButton = null;
            GazeDwellSelector stampGaze = null;
            GameObject cctvScreen = null;
            MeshRenderer cctvScreenRenderer = null;
            GazeDwellSelector monitorGaze = null;
            Light guardRedLight = null;

            try
            {
                // Floor & Ceiling & Walls
                CreatePrimitiveCube("GuardRoomFloor", guardRoomParent.transform, new Vector3(0f, -0.025f, 0f), new Vector3(4f, 0.05f, 4f), GetColor("#2b2b2b"));
                CreatePrimitiveCube("GuardRoomCeiling", guardRoomParent.transform, new Vector3(0f, 2.525f, 0f), new Vector3(4f, 0.05f, 4f), GetColor("#121212"));
                CreatePrimitiveCube("GuardRoomWallFront", guardRoomParent.transform, new Vector3(0f, 1.25f, 2f), new Vector3(4f, 2.5f, 0.1f), GetColor("#1a1a1c"));
                CreatePrimitiveCube("GuardRoomWallBack", guardRoomParent.transform, new Vector3(0f, 1.25f, -2f), new Vector3(4f, 2.5f, 0.1f), GetColor("#1a1a1c"));
                CreatePrimitiveCube("GuardRoomWallLeft", guardRoomParent.transform, new Vector3(-2f, 1.25f, 0f), new Vector3(0.1f, 2.5f, 4f), GetColor("#1a1a1c"));
                CreatePrimitiveCube("GuardRoomWallRight", guardRoomParent.transform, new Vector3(2f, 1.25f, 0f), new Vector3(0.1f, 2.5f, 4f), GetColor("#1a1a1c"));

                // Guard Desk
                GameObject guardDesk = new GameObject("Guard_Desk");
                guardDesk.transform.SetParent(guardRoomParent.transform);
                guardDesk.transform.localPosition = new Vector3(0f, 0f, 1.2f);

                CreatePrimitiveCube("MetalTop", guardDesk.transform, new Vector3(0f, 0.65f, 0f), new Vector3(1.8f, 0.08f, 0.8f), GetColor("#555c63"));
                CreatePrimitiveCube("LegFL", guardDesk.transform, new Vector3(-0.85f, 0.3f, 0.35f), new Vector3(0.05f, 0.6f, 0.05f), GetColor("#222"));
                CreatePrimitiveCube("LegFR", guardDesk.transform, new Vector3(0.85f, 0.3f, 0.35f), new Vector3(0.05f, 0.6f, 0.05f), GetColor("#222"));
                CreatePrimitiveCube("LegBL", guardDesk.transform, new Vector3(-0.85f, 0.3f, -0.35f), new Vector3(0.05f, 0.6f, 0.05f), GetColor("#222"));
                CreatePrimitiveCube("LegBR", guardDesk.transform, new Vector3(0.85f, 0.3f, -0.35f), new Vector3(0.05f, 0.6f, 0.05f), GetColor("#222"));

                // Clipboard stamp paper
                var guardClipboard = CreatePrimitiveCube("GuardClipboardSheet", guardDesk.transform, new Vector3(0f, 0.695f, 0f), new Vector3(0.5f, 0.01f, 0.65f), GetColor("#eae3d2"));
                stampButton = CreatePrimitiveCube("StampButton", guardClipboard.transform, new Vector3(0f, 0.01f, -0.2f), new Vector3(0.32f, 0.05f, 0.1f), GetColor("#8b0000"));
                stampGaze = stampButton.AddComponent<GazeDwellSelector>();
                stampGaze.interactionLayers = GetGazeInteractionLayer();

                // CCTV Stack casing & Screen Mesh
                GameObject cctvStack = new GameObject("CCTV_MonitorStack");
                cctvStack.transform.SetParent(guardRoomParent.transform);
                cctvStack.transform.localPosition = new Vector3(0f, 1.4f, 1.95f);
                CreatePrimitiveCube("Casing", cctvStack.transform, Vector3.zero, new Vector3(1.4f, 1.0f, 0.4f), GetColor("#1d2124"));
                
                cctvScreen = CreatePrimitiveCube("CCTVScreenMesh", cctvStack.transform, new Vector3(0f, 0f, -0.21f), new Vector3(1.3f, 0.9f, 0.01f), Color.black);
                cctvScreenRenderer = cctvScreen.GetComponent<MeshRenderer>();

                // Virtual CCTV camera setup inside Classroom
                var cctvCameraObj = new GameObject("CCTVCameraNode");
                cctvCameraObj.transform.SetParent(classroomParent.transform);
                cctvCameraObj.transform.localPosition = new Vector3(0f, 2.8f, 3.5f);
                cctvCameraObj.transform.localRotation = Quaternion.Euler(30f, 180f, 0f);
                var cctvCam = cctvCameraObj.AddComponent<Camera>();
                cctvCam.fieldOfView = 60f;

                // CCTV Monitor configs
                SerializedObject cctvSO = new SerializedObject(cctvMonitor);
                cctvSO.FindProperty("_cctvCamera").objectReferenceValue = cctvCam;
                cctvSO.FindProperty("_monitorRenderers").ClearArray();
                cctvSO.FindProperty("_monitorRenderers").InsertArrayElementAtIndex(0);
                cctvSO.FindProperty("_monitorRenderers").GetArrayElementAtIndex(0).objectReferenceValue = cctvScreenRenderer;
                if (rulesPaper != null)
                {
                    cctvSO.FindProperty("_assignedRulePapers").ClearArray();
                    cctvSO.FindProperty("_assignedRulePapers").InsertArrayElementAtIndex(0);
                    cctvSO.FindProperty("_assignedRulePapers").GetArrayElementAtIndex(0).objectReferenceValue = rulesPaper.GetComponent<MeshRenderer>();
                }
                cctvSO.FindProperty("_timelineManager").objectReferenceValue = timelineManager;
                cctvSO.ApplyModifiedProperties();

                // Gaze monitor interaction
                monitorGaze = cctvScreen.AddComponent<GazeDwellSelector>();
                monitorGaze.interactionLayers = GetGazeInteractionLayer();

                // Sticky notes wall
                CreatePrimitiveCube("StickyRulesWall", guardRoomParent.transform, new Vector3(-1.95f, 1.3f, 0f), new Vector3(0.01f, 1.6f, 2.2f), Color.gray);

                // Guard Lights
                var guardLightObj = new GameObject("GuardRoomLamp");
                guardLightObj.transform.SetParent(guardRoomParent.transform);
                guardLightObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);
                var guardLight = guardLightObj.AddComponent<Light>();
                guardLight.type = LightType.Point;
                guardLight.range = 5f;
                guardLight.intensity = 0.45f;
                guardLight.color = GetColor("#aaffff");

                var guardRedAlertObj = new GameObject("GuardRedAlertLamp");
                guardRedAlertObj.transform.SetParent(guardRoomParent.transform);
                guardRedAlertObj.transform.localPosition = new Vector3(0f, 2.3f, -1.5f);
                guardRedLight = guardRedAlertObj.AddComponent<Light>();
                guardRedLight.type = LightType.Point;
                guardRedLight.range = 3f;
                guardRedLight.intensity = 0.0f;
                guardRedLight.color = Color.red;
                Debug.Log("[SceneSetupHelper] Guard Room created successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error building Guard Room: {ex}");
            }


            // ==================== PLAYER XR ORIGIN TRACKING RIG ====================
            GameObject xrOriginObj = null;
            GameObject blackoutQuad = null;
            try
            {
                // Create Interaction Manager first
                GameObject interactionManagerObj = new GameObject("XR Interaction Manager");
                var interactionManager = interactionManagerObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
                Undo.RegisterCreatedObjectUndo(interactionManagerObj, "Create XR Interaction Manager");

                // Copy and load input actions asset
                string inputActionsPath = CopyInputActionsAsset();
                InputActionAsset inputActions = null;
                if (!string.IsNullOrEmpty(inputActionsPath))
                {
                    inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputActionsPath);
                }

                if (inputActions != null)
                {
                    GameObject inputActionManagerObj = new GameObject("Input Action Manager");
                    var inputActionManager = inputActionManagerObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Inputs.InputActionManager>();
                    inputActionManager.actionAssets = new List<InputActionAsset> { inputActions };
                    Undo.RegisterCreatedObjectUndo(inputActionManagerObj, "Create Input Action Manager");
                }

                InputActionReference leftMoveRef = null;
                InputActionReference rightMoveRef = null;
                InputActionReference leftTurnRef = null;
                InputActionReference rightTurnRef = null;
                InputActionReference leftPosRef = null;
                InputActionReference leftRotRef = null;
                InputActionReference leftSelectRef = null;
                InputActionReference leftActivateRef = null;
                InputActionReference leftIsTrackedRef = null;
                InputActionReference leftTrackingStateRef = null;

                InputActionReference rightPosRef = null;
                InputActionReference rightRotRef = null;
                InputActionReference rightSelectRef = null;
                InputActionReference rightActivateRef = null;
                InputActionReference rightIsTrackedRef = null;
                InputActionReference rightTrackingStateRef = null;

                InputActionReference headPosRef = null;
                InputActionReference headRotRef = null;

                if (inputActions != null && !string.IsNullOrEmpty(inputActionsPath))
                {
                    var allRefs = AssetDatabase.LoadAllAssetsAtPath(inputActionsPath);
                    foreach (var asset in allRefs)
                    {
                        if (asset is InputActionReference actionRef)
                        {
                            string actionPath = actionRef.action.actionMap.name + "/" + actionRef.action.name;
                            switch (actionPath)
                            {
                                case "XRI Left Locomotion/Move": leftMoveRef = actionRef; break;
                                case "XRI Left Locomotion/Turn": leftTurnRef = actionRef; break;
                                case "XRI Right Locomotion/Move": rightMoveRef = actionRef; break;
                                case "XRI Right Locomotion/Turn": rightTurnRef = actionRef; break;

                                case "XRI Left/Position": leftPosRef = actionRef; break;
                                case "XRI Left/Rotation": leftRotRef = actionRef; break;
                                case "XRI Left/Is Tracked": leftIsTrackedRef = actionRef; break;
                                case "XRI Left/Tracking State": leftTrackingStateRef = actionRef; break;

                                case "XRI Left Interaction/Select": leftSelectRef = actionRef; break;
                                case "XRI Left Interaction/Activate": leftActivateRef = actionRef; break;

                                case "XRI Right/Position": rightPosRef = actionRef; break;
                                case "XRI Right/Rotation": rightRotRef = actionRef; break;
                                case "XRI Right/Is Tracked": rightIsTrackedRef = actionRef; break;
                                case "XRI Right/Tracking State": rightTrackingStateRef = actionRef; break;

                                case "XRI Right Interaction/Select": rightSelectRef = actionRef; break;
                                case "XRI Right Interaction/Activate": rightActivateRef = actionRef; break;

                                case "XRI Head/Position": headPosRef = actionRef; break;
                                case "XRI Head/Rotation": headRotRef = actionRef; break;
                            }
                        }
                    }
                }

                xrOriginObj = new GameObject("XR Origin");
                xrOriginObj.transform.position = Vector3.zero;
                var xrOrigin = xrOriginObj.AddComponent<Unity.XR.CoreUtils.XROrigin>();
                xrOrigin.RequestedTrackingOriginMode = Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.Floor;
                xrOrigin.CameraYOffset = 1.36f; // Default eye level height for editor play mode testing
                Undo.RegisterCreatedObjectUndo(xrOriginObj, "Create XR Origin");

                // Character Controller on XR Origin for physics collision & height driver
                var charController = xrOriginObj.AddComponent<CharacterController>();
                charController.center = new Vector3(0f, 0.88f, 0f);
                charController.radius = 0.3f;
                charController.height = 1.75f;
                charController.skinWidth = 0.05f;

#pragma warning disable 0618
                var charDriver = xrOriginObj.AddComponent<CharacterControllerDriver>();
                charDriver.minHeight = 1.0f;
                charDriver.maxHeight = 2.2f;
#pragma warning restore 0618

                GameObject cameraOffsetObj = new GameObject("Camera Offset");
                cameraOffsetObj.transform.SetParent(xrOriginObj.transform);
                cameraOffsetObj.transform.localPosition = new Vector3(0f, 1.36f, 0f); // sitting height offset fallback

                GameObject mainCameraObj = new GameObject("Main Camera");
                mainCameraObj.transform.SetParent(cameraOffsetObj.transform);
                mainCameraObj.transform.localPosition = Vector3.zero;
                var mainCam = mainCameraObj.AddComponent<Camera>();
                mainCam.nearClipPlane = 0.05f;
                mainCam.farClipPlane = 100f;
                mainCameraObj.AddComponent<AudioListener>();

                // Setup scare references
                if (doorSignScare != null)
                {
                    doorSignScare.playerCamera = mainCameraObj.transform;
                    doorSignScare.playerFlashlight = deskLampLight;
                }

                // Add standard camera tracking with HMD bindings
#if UNITY_2019_1_OR_NEWER
                var tpd = mainCameraObj.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
                tpd.positionInput = headPosRef != null ? new InputActionProperty(headPosRef) : new InputActionProperty(new InputAction("Position", InputActionType.Value, binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
                tpd.rotationInput = headRotRef != null ? new InputActionProperty(headRotRef) : new InputActionProperty(new InputAction("Rotation", InputActionType.Value, binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
                tpd.trackingType = UnityEngine.InputSystem.XR.TrackedPoseDriver.TrackingType.RotationAndPosition;
#endif

                // Create Eye Close Blackout Quad under Main Camera
                blackoutQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                blackoutQuad.name = "EyeCloseBlackout";
                blackoutQuad.transform.SetParent(mainCameraObj.transform);
                blackoutQuad.transform.localPosition = new Vector3(0f, 0f, 0.06f); // just past near clip plane
                blackoutQuad.transform.localRotation = Quaternion.identity;
                blackoutQuad.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
                UnityEngine.Object.DestroyImmediate(blackoutQuad.GetComponent<Collider>());
                
                var quadRenderer = blackoutQuad.GetComponent<MeshRenderer>();
                var unlitMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                unlitMat.color = Color.black;
                quadRenderer.sharedMaterial = unlitMat;
                blackoutQuad.SetActive(false);

                // XRI Gaze Interactor node
                GameObject gazeInteractorObj = new GameObject("GazeInteractor");
                gazeInteractorObj.transform.SetParent(cameraOffsetObj.transform);
                gazeInteractorObj.transform.localPosition = Vector3.zero;

                Type gazeType = typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRGazeInteractor);
                if (gazeType != null)
                {
                    var gazeInteractor = gazeInteractorObj.AddComponent(gazeType);
                    SerializedObject gazeSO = new SerializedObject(gazeInteractor);
                    gazeSO.FindProperty("m_InteractionLayers").FindPropertyRelative("m_Bits").longValue = GetGazeInteractionLayerValue();
                    gazeSO.ApplyModifiedProperties();
                }
                else
                {
                    gazeInteractorObj.AddComponent<XRRayCastGazeFallback>();
                }

                // Left Hand Controller
                GameObject leftHandObj = new GameObject("LeftHand Controller");
                leftHandObj.transform.SetParent(cameraOffsetObj.transform);
                leftHandObj.transform.localPosition = Vector3.zero;
                leftHandObj.transform.localRotation = Quaternion.identity;

#pragma warning disable 0618
                var leftController = leftHandObj.AddComponent<ActionBasedController>();
#pragma warning restore 0618

                leftController.positionAction = leftPosRef != null ? new InputActionProperty(leftPosRef) : new InputActionProperty(new InputAction("Position", InputActionType.Value, binding: "<XRController>{LeftHand}/devicePosition", expectedControlType: "Vector3"));
                leftController.rotationAction = leftRotRef != null ? new InputActionProperty(leftRotRef) : new InputActionProperty(new InputAction("Rotation", InputActionType.Value, binding: "<XRController>{LeftHand}/deviceRotation", expectedControlType: "Quaternion"));
                leftController.selectAction = leftSelectRef != null ? new InputActionProperty(leftSelectRef) : new InputActionProperty(new InputAction("Select", InputActionType.Button, binding: "<XRController>{LeftHand}/gripPressed"));
                leftController.activateAction = leftActivateRef != null ? new InputActionProperty(leftActivateRef) : new InputActionProperty(new InputAction("Activate", InputActionType.Button, binding: "<XRController>{LeftHand}/triggerPressed"));
                leftController.isTrackedAction = leftIsTrackedRef != null ? new InputActionProperty(leftIsTrackedRef) : new InputActionProperty(new InputAction("IsTracked", InputActionType.Button, binding: "<XRController>{LeftHand}/isTracked"));
                leftController.trackingStateAction = leftTrackingStateRef != null ? new InputActionProperty(leftTrackingStateRef) : new InputActionProperty(new InputAction("TrackingState", InputActionType.Value, binding: "<XRController>{LeftHand}/trackingState", expectedControlType: "Integer"));

                leftHandObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
                var leftSphereCollider = leftHandObj.AddComponent<SphereCollider>();
                leftSphereCollider.isTrigger = true;
                leftSphereCollider.radius = 0.15f;

                // Right Hand Controller
                GameObject rightHandObj = new GameObject("RightHand Controller");
                rightHandObj.transform.SetParent(cameraOffsetObj.transform);
                rightHandObj.transform.localPosition = Vector3.zero;
                rightHandObj.transform.localRotation = Quaternion.identity;

#pragma warning disable 0618
                var rightController = rightHandObj.AddComponent<ActionBasedController>();
#pragma warning restore 0618

                rightController.positionAction = rightPosRef != null ? new InputActionProperty(rightPosRef) : new InputActionProperty(new InputAction("Position", InputActionType.Value, binding: "<XRController>{RightHand}/devicePosition", expectedControlType: "Vector3"));
                rightController.rotationAction = rightRotRef != null ? new InputActionProperty(rightRotRef) : new InputActionProperty(new InputAction("Rotation", InputActionType.Value, binding: "<XRController>{RightHand}/deviceRotation", expectedControlType: "Quaternion"));
                rightController.selectAction = rightSelectRef != null ? new InputActionProperty(rightSelectRef) : new InputActionProperty(new InputAction("Select", InputActionType.Button, binding: "<XRController>{RightHand}/gripPressed"));
                rightController.activateAction = rightActivateRef != null ? new InputActionProperty(rightActivateRef) : new InputActionProperty(new InputAction("Activate", InputActionType.Button, binding: "<XRController>{RightHand}/triggerPressed"));
                rightController.isTrackedAction = rightIsTrackedRef != null ? new InputActionProperty(rightIsTrackedRef) : new InputActionProperty(new InputAction("IsTracked", InputActionType.Button, binding: "<XRController>{RightHand}/isTracked"));
                rightController.trackingStateAction = rightTrackingStateRef != null ? new InputActionProperty(rightTrackingStateRef) : new InputActionProperty(new InputAction("TrackingState", InputActionType.Value, binding: "<XRController>{RightHand}/trackingState", expectedControlType: "Integer"));

                rightHandObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
                var rightSphereCollider = rightHandObj.AddComponent<SphereCollider>();
                rightSphereCollider.isTrigger = true;
                rightSphereCollider.radius = 0.15f;

                // Locomotion System
                GameObject locomotionSystemObj = new GameObject("Locomotion System");
                locomotionSystemObj.transform.SetParent(xrOriginObj.transform);
                locomotionSystemObj.transform.localPosition = Vector3.zero;

#pragma warning disable 0618
                var locomotionSystem = locomotionSystemObj.AddComponent<LocomotionSystem>();
                locomotionSystem.xrOrigin = xrOrigin;

                // Continuous Move Provider (Action-based)
                var moveProvider = locomotionSystemObj.AddComponent<ActionBasedContinuousMoveProvider>();
                moveProvider.system = locomotionSystem;
                moveProvider.moveSpeed = 1.5f;
                moveProvider.leftHandMoveAction = leftMoveRef != null ? new InputActionProperty(leftMoveRef) : new InputActionProperty(new InputAction("LeftHandMove", InputActionType.Value, binding: "<XRController>{LeftHand}/thumbstick", expectedControlType: "Vector2"));
                moveProvider.rightHandMoveAction = rightMoveRef != null ? new InputActionProperty(rightMoveRef) : new InputActionProperty(new InputAction("RightHandMove", InputActionType.Value, binding: "<XRController>{RightHand}/thumbstick", expectedControlType: "Vector2"));

                // Continuous Turn Provider (Action-based)
                var turnProvider = locomotionSystemObj.AddComponent<ActionBasedContinuousTurnProvider>();
                turnProvider.system = locomotionSystem;
                turnProvider.turnSpeed = 60f;
                turnProvider.leftHandTurnAction = leftTurnRef != null ? new InputActionProperty(leftTurnRef) : new InputActionProperty(new InputAction("LeftHandTurn", InputActionType.Value, binding: "<XRController>{LeftHand}/thumbstick", expectedControlType: "Vector2"));
                turnProvider.rightHandTurnAction = rightTurnRef != null ? new InputActionProperty(rightTurnRef) : new InputActionProperty(new InputAction("RightHandTurn", InputActionType.Value, binding: "<XRController>{RightHand}/thumbstick", expectedControlType: "Vector2"));

                charDriver.locomotionProvider = moveProvider;
#pragma warning restore 0618

                xrOrigin.Camera = mainCam;
                xrOrigin.CameraFloorOffsetObject = cameraOffsetObj;
                Debug.Log("[SceneSetupHelper] Player XR Origin Camera Rig with locomotion and hand controllers created successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error building XR Origin: {ex}");
            }

            // ==================== UX DYNAMIC STATE HANDLERS ====================
            try
            {
                var tutorialManager = gameManagerObj.AddComponent<TutorialManager>();
                tutorialManager.timelineManager = timelineManager;

                var hintSystem = gameManagerObj.AddComponent<HintSystem>();
                tutorialManager.hintSystem = hintSystem;

                // Wire targets for Hint system
                if (deskLampBase != null) hintSystem.flashlightTarget = deskLampBase.transform;
                if (blackboardScreenRenderer != null) hintSystem.blackboardTarget = blackboardScreenRenderer.transform;
                if (classroomDoorPanel != null) hintSystem.doorTarget = classroomDoorPanel.transform;

                // Flashlight opening breathing fader & grabbing
                if (lampGroup != null)
                {
                    var grabInteractable = lampGroup.AddComponent<XRGrabInteractable>();
                    var lampRb = lampGroup.GetComponent<Rigidbody>();
                    if (lampRb != null)
                    {
                        lampRb.mass = 1.0f;
                        lampRb.drag = 1.0f;
                        lampRb.angularDrag = 1.0f;
                        lampRb.useGravity = true;
                        lampRb.isKinematic = false;
                    }

                    var flashIntro = lampGroup.AddComponent<FlashlightIntro>();
                    flashIntro.flashlightRenderer = deskLampBase.GetComponent<MeshRenderer>();
                    flashIntro.electricSoundSource = lampGroup.AddComponent<AudioSource>();

                    var lampHighlight = lampGroup.AddComponent<InteractableHighlight>();
                    lampHighlight.targetRenderer = deskLampBase.GetComponent<MeshRenderer>();

                    // Toggle lamp when player presses trigger button (Activate action) while holding it
                    if (toggleHelper != null)
                    {
                        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                            grabInteractable.activated, 
                            new UnityAction(toggleHelper.ToggleDeskLamp)
                        );
                    }
                }

                // Blackboard Highlight
                if (blackboardScreenRenderer != null)
                {
                    var bbHighlight = blackboardScreenRenderer.gameObject.AddComponent<InteractableHighlight>();
                    bbHighlight.targetRenderer = blackboardScreenRenderer;
                }

                // Rules Paper Highlight
                if (rulesPaper != null)
                {
                    var paperHighlight = rulesPaper.AddComponent<InteractableHighlight>();
                    paperHighlight.targetRenderer = rulesPaper.GetComponent<MeshRenderer>();
                    paperHighlight.isImportantRedWarning = true;
                }

                // Wire up the EnvironmentStateController for multi-room swapping
                var envController = gameManagerObj.AddComponent<EnvironmentStateController>();
                envController.classroomParent = classroomParent;
                envController.corridorParent = corridorParent;
                envController.guardRoomParent = guardRoomParent;
                if (xrOriginObj != null) envController.playerRig = xrOriginObj.transform;

                // Eye Close Manager setup
                var eyeCloseManager = gameManagerObj.AddComponent<EyeCloseManager>();
                eyeCloseManager.blackoutQuad = blackoutQuad;
                eyeCloseManager.timelineManager = timelineManager;

                Debug.Log("[SceneSetupHelper] UX dynamic state managers, Highlights, and Teleport controller linked successfully.");

                // ==================== ENDING BILLBOARD & MANAGER SETUP ====================
                GameObject endingPanelObj = new GameObject("Ending_Billboard");
                endingPanelObj.transform.position = new Vector3(0f, 1.3f, 1.5f);
                endingPanelObj.transform.rotation = Quaternion.identity;

                // Dark backing plate to make text readable in VR
                var backgroundPlate = CreatePrimitiveCube("BackgroundPlate", endingPanelObj.transform, Vector3.zero, new Vector3(2.2f, 1.4f, 0.02f), GetColor("#0a0a0a"));
                var borderPlate = CreatePrimitiveCube("BorderPlate", endingPanelObj.transform, new Vector3(0f, 0f, 0.005f), new Vector3(2.24f, 1.44f, 0.015f), GetColor("#222222"));

                // Title Text Mesh
                GameObject titleObj = new GameObject("TitleText");
                titleObj.transform.SetParent(endingPanelObj.transform);
                titleObj.transform.localPosition = new Vector3(0f, 0.5f, -0.015f);
                titleObj.transform.localRotation = Quaternion.identity;
                var titleMesh = titleObj.AddComponent<TextMesh>();
                titleMesh.text = "ENDING TITLE";
                titleMesh.fontSize = 48;
                titleMesh.characterSize = 0.015f;
                titleMesh.anchor = TextAnchor.MiddleCenter;
                titleMesh.alignment = TextAlignment.Center;
                titleMesh.color = Color.white;

                // Description Text Mesh
                GameObject descObj = new GameObject("DescText");
                descObj.transform.SetParent(endingPanelObj.transform);
                descObj.transform.localPosition = new Vector3(0f, 0.05f, -0.015f);
                descObj.transform.localRotation = Quaternion.identity;
                var descMesh = descObj.AddComponent<TextMesh>();
                descMesh.text = "Ending Description goes here...";
                descMesh.fontSize = 28;
                descMesh.characterSize = 0.013f;
                descMesh.anchor = TextAnchor.MiddleCenter;
                descMesh.alignment = TextAlignment.Center;
                descMesh.color = GetColor("#cccccc");

                // Stats Text Mesh
                GameObject statsObj = new GameObject("StatsText");
                statsObj.transform.SetParent(endingPanelObj.transform);
                statsObj.transform.localPosition = new Vector3(-0.85f, -0.45f, -0.015f);
                statsObj.transform.localRotation = Quaternion.identity;
                var statsMesh = statsObj.AddComponent<TextMesh>();
                statsMesh.text = "Stats details...";
                statsMesh.fontSize = 24;
                statsMesh.characterSize = 0.012f;
                statsMesh.anchor = TextAnchor.MiddleLeft;
                statsMesh.alignment = TextAlignment.Left;
                statsMesh.color = GetColor("#99ff99");

                // Stamp Text Mesh
                GameObject stampObj = new GameObject("StampText");
                stampObj.transform.SetParent(endingPanelObj.transform);
                stampObj.transform.localPosition = new Vector3(0.5f, -0.45f, -0.015f);
                stampObj.transform.localRotation = Quaternion.identity;
                var stampMesh = stampObj.AddComponent<TextMesh>();
                stampMesh.text = "STAMP";
                stampMesh.fontSize = 36;
                stampMesh.characterSize = 0.015f;
                stampMesh.anchor = TextAnchor.MiddleCenter;
                stampMesh.alignment = TextAlignment.Center;
                stampMesh.color = Color.red;

                // Add EndingManager component to _GameManager
                var endingManager = gameManagerObj.AddComponent<EndingManager>();
                endingManager.endingBillboardPanel = endingPanelObj;
                endingManager.titleText = titleMesh;
                endingManager.descriptionText = descMesh;
                endingManager.statsText = statsMesh;
                endingManager.stampText = stampMesh;
                endingManager.cctvScreenRenderer = cctvScreenRenderer;
                endingManager.redEmergencyLight = guardRedLight;

                // Wire TimelineManager via SerializedObject to the EndingManager
                SerializedObject endingSO = new SerializedObject(endingManager);
                endingSO.FindProperty("_timelineManager").objectReferenceValue = timelineManager;
                endingSO.ApplyModifiedProperties();

                // Parent the billboard under the _GameManager
                endingPanelObj.transform.SetParent(gameManagerObj.transform);

                // Wire up climax choices
                if (stampGaze != null)
                {
                    UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                        stampGaze.OnDwellSelected,
                        new UnityAction(endingManager.TriggerStampedComplianceEnding)
                    );
                    UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                        stampGaze.selectEntered,
                        new UnityAction(endingManager.TriggerStampedComplianceEnding)
                    );
                }

                if (monitorGaze != null)
                {
                    UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                        monitorGaze.OnDwellSelected,
                        new UnityAction(endingManager.TriggerSmashedMonitorEnding)
                    );
                    UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                        monitorGaze.selectEntered,
                        new UnityAction(endingManager.TriggerSmashedMonitorEnding)
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error creating UX dynamic handlers: {ex}");
            }

            // Save the newly synthesized Scene file
            try
            {
                string sceneFilePath = Path.Combine(scenesDir, "TheBreathlessStudyRoom_MVP.unity");
                EditorSceneManager.SaveScene(newScene, sceneFilePath);
                Debug.Log($"[SceneSetupHelper] Scene file successfully saved to disk: {sceneFilePath}");

                // Auto-open the newly created scene in the editor for the user!
                if (!Application.isBatchMode)
                {
                    EditorSceneManager.OpenScene(sceneFilePath, OpenSceneMode.Single);
                    Debug.Log("[SceneSetupHelper] Auto-opened generated scene in the Editor.");
                }

                // Print output reports
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("窒息室 (The Breathless Study Room)",
                        "MVP VR Scene assembled successfully! 🎉\n\n" +
                        "Complete setup generated at: " + sceneFilePath + "\n\n" +
                        "All components loaded with separate error-tolerant try-catch protectors.\n" +
                        "Locker Set Assembly is built flush against the right wall at Z=-0.8.\n" +
                        "Scene is automatically opened in your Editor!", 
                        "Acknowledge");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneSetupHelper] Error saving scene file: {ex}");
            }

            anomalySO.ApplyModifiedProperties();
            Debug.Log("[SceneSetupHelper] Scene assembly sequence completed successfully!");
        }

        // --- Core helper construction nodes ---

        private static AudioSource CreateAudioSourceNode(string name, Transform parent, float spatialBlend, float volume, bool loop = false)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
            var source = obj.AddComponent<AudioSource>();
            source.spatialBlend = spatialBlend;
            source.volume = volume;
            source.loop = loop;
            source.playOnAwake = loop;
            return source;
        }

        private static GameObject CreatePrimitiveCube(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return obj;
        }

        private static GameObject CreatePrimitiveSphere(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return obj;
        }

        private static GameObject CreatePrimitiveCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return obj;
        }

        private static GameObject CreatePrimitiveCone(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;

            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return obj;
        }

        private static Color GetColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }
            return Color.gray;
        }

        private static Material CreateLitMaterial(Color color, float smoothness = 0.5f, float metallic = 0.0f, bool transparent = false)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            if (transparent)
            {
                mat.SetFloat("_Blend", 1); 
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            return mat;
        }

        private static Material CreateSafetyMaterial(Shader shader, Color color, string assetName)
        {
            string path = "Assets/_Project/Scenes/" + assetName;
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.color = color;
                mat.name = Path.GetFileNameWithoutExtension(assetName);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static Material SaveSafetyAsset(Material mat, string assetName)
        {
            string path = "Assets/_Project/Scenes/" + assetName;
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static string CopyInputActionsAsset()
        {
            string destDir = "Assets/_Project/Input";
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            string destPath = Path.Combine(destDir, "XRI Default Input Actions.inputactions");
            string destMetaPath = destPath + ".meta";
            
            if (File.Exists(destPath))
            {
                return destPath;
            }

            // Find XRI package folder in PackageCache
            string packageCacheDir = "Library/PackageCache";
            if (Directory.Exists(packageCacheDir))
            {
                foreach (var dir in Directory.GetDirectories(packageCacheDir, "com.unity.xr.interaction.toolkit@*"))
                {
                    string srcPath = Path.Combine(dir, "Samples~/Starter Assets/XRI Default Input Actions.inputactions");
                    string srcMetaPath = srcPath + ".meta";
                    if (File.Exists(srcPath))
                    {
                        try
                        {
                            File.Copy(srcPath, destPath);
                            if (File.Exists(srcMetaPath))
                            {
                                File.Copy(srcMetaPath, destMetaPath);
                            }
                            AssetDatabase.ImportAsset(destPath);
                            Debug.Log($"[SceneSetupHelper] Successfully imported input actions asset to: {destPath}");
                            return destPath;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[SceneSetupHelper] Failed to copy input actions asset: {ex.Message}");
                        }
                    }
                }
            }
            return null;
        }

        private static UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask GetGazeInteractionLayer()
        {
            var mask = new UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask();
            mask.value = 1 << 0; 
            return mask;
        }

        private static long GetGazeInteractionLayerValue()
        {
            return 1; 
        }
    }
}
