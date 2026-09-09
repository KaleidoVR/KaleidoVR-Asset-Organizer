// =========================================================================
// BLOCK 1: GLOBAL ENGINE DIRECTIVES & ROOT MODULE CONFIGURATION
// KaleidoVR Asset Organizer
// https://kalivr.com
// Compatible with Unity 2022.3.22f1 through Unity 6 (6000.x)
// VRChat SDK3 Avatars optional (Auto-Link FX & Menu)
// Uses 2022.3 LTS AssetDatabase/PrefabUtility APIs only (no 2023+/Unity 6-only types)
// =========================================================================

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace KaleidoVR.EditorTools
{
    public class KaleidoAssetOrganizer : EditorWindow
    {
        public static readonly string VERSION = "1.0.0";
        public const string LOGO_FILE_NAME = "Kali_Logo.png";
        public const string FALLBACK_ICON_PATH = "Assets/KaleidoVR/Editor/Icons/Kali_Logo.png";

        // Resolved from this script's own location, so the tool folder can be renamed or moved.
        public static string ICON_PATH { get { return ResolveIconPath(); } }

        private static string cachedIconPath;

        private Texture2D headerIcon;

        public string outputDirectory = "Assets/KaleidoVR/Models/Test";
        public string sceneName = "OrganizedScene";
        public string prefabName = "NewAvatar";
        public bool createPrefab = true;
        // =========================================================================
        // BLOCK 2: AUTOMATION PIPELINES VARIABLE DECLARATIONS
        // =========================================================================

        public bool autoParsePoiyomi = true;
        public bool autoSetupVRCDescriptor = true;

        public List<UnityEngine.Object> objectsToOrganize = new List<UnityEngine.Object>();
        public List<UnityEngine.Object> ignoreList = new List<UnityEngine.Object>();
        // =========================================================================
        // BLOCK 3: SORTER PIPELINE LOOKUP ROUTING DICTIONARY
        // =========================================================================

        public Dictionary<string, string> organizeOptions = new Dictionary<string, string>()
        {
            {"GameObject", "Copy"},
            {"Texture2D", "Copy"},
            {"Cubemap", "Copy"},
            {"Material", "Copy"},
            {"VRCExpressionParameters", "Copy"},
            {"VRCExpressionsMenu", "Copy"},
            {"BlendTree", "Copy"},
            {"AnimationClip", "Copy"},
            {"AnimatorOverrideController", "Copy"},
            {"AnimatorController", "Copy"},        // FIXED: Explicitly mapped for native Unity .controller files
            {"RuntimeAnimatorController", "Copy"}, // FIXED: Explicitly mapped for runtime controller asset references
            {"AvatarMask", "Copy"},
            {"AudioClip", "Copy"},
            {"Shader", "Ignore"},
            {"MonoScript", "Ignore"},
            {"DefaultAsset", "Ignore"}
        };
        // =========================================================================
        // BLOCK 4: MENU ITEMS & TEXTURE CACHE LOADING WITH EDITORPREFS RESTORERS
        // =========================================================================

        [MenuItem("KaleidoVR/Asset Organizer")]
        public static void ShowWindow()
        {
            var window = GetWindow<KaleidoAssetOrganizer>("Asset Organizer");
            window.InitializeLocalLogo();
            window.LoadEditorPreferences(); // Load saved properties on window instantiation
            window.ResizeWindow();
        }

        private void OnEnable()
        {
            InitializeLocalLogo();
            LoadEditorPreferences(); // Fallback reload pass when the assembly compilation changes
        }

        private void InitializeLocalLogo()
        {
            cachedIconPath = null;
            headerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON_PATH);
        }

        // Locates this script in the project so Icons/ is found relative to it instead of a fixed folder name
        public static string GetToolEditorFolder()
        {
            foreach (string guid in AssetDatabase.FindAssets("KaleidoAssetOrganizer t:MonoScript"))
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
                if (scriptPath.EndsWith("/KaleidoAssetOrganizer.cs", StringComparison.OrdinalIgnoreCase))
                {
                    return scriptPath.Substring(0, scriptPath.LastIndexOf('/'));
                }
            }
            return null;
        }

        private static string ResolveIconPath()
        {
            if (!string.IsNullOrEmpty(cachedIconPath)) return cachedIconPath;

            string editorFolder = GetToolEditorFolder();
            if (!string.IsNullOrEmpty(editorFolder))
            {
                string relativeIcon = editorFolder + "/Icons/" + LOGO_FILE_NAME;
                if (AssetDatabase.LoadMainAssetAtPath(relativeIcon) != null)
                {
                    cachedIconPath = relativeIcon;
                    return cachedIconPath;
                }
            }

            // Last resort: the logo may have been placed anywhere in the project
            string logoName = Path.GetFileNameWithoutExtension(LOGO_FILE_NAME);
            foreach (string guid in AssetDatabase.FindAssets(logoName + " t:Texture2D"))
            {
                string foundPath = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
                if (foundPath.EndsWith("/" + LOGO_FILE_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    cachedIconPath = foundPath;
                    return cachedIconPath;
                }
            }

            cachedIconPath = !string.IsNullOrEmpty(editorFolder) ? editorFolder + "/Icons/" + LOGO_FILE_NAME : FALLBACK_ICON_PATH;
            return cachedIconPath;
        }

        // Internal engine caching tracker pass to recover data keys from your hard disk environment
        private void LoadEditorPreferences()
        {
            if (EditorPrefs.HasKey("KVR_OutputDir")) outputDirectory = EditorPrefs.GetString("KVR_OutputDir");
            if (EditorPrefs.HasKey("KVR_SceneName")) sceneName = EditorPrefs.GetString("KVR_SceneName");
            if (EditorPrefs.HasKey("KVR_PrefabName")) prefabName = EditorPrefs.GetString("KVR_PrefabName");
            if (EditorPrefs.HasKey("KVR_CreatePrefab")) createPrefab = EditorPrefs.GetBool("KVR_CreatePrefab");
            if (EditorPrefs.HasKey("KVR_AutoParsePoi")) autoParsePoiyomi = EditorPrefs.GetBool("KVR_AutoParsePoi");
            if (EditorPrefs.HasKey("KVR_AutoSetupVRC")) autoSetupVRCDescriptor = EditorPrefs.GetBool("KVR_AutoSetupVRC");

            // Loop through options keys matrix to load each specific Export dropdown action choice state
            List<string> keys = new List<string>(organizeOptions.Keys);
            foreach (string key in keys)
            {
                string prefKey = "KVR_Opt_" + key;
                if (EditorPrefs.HasKey(prefKey))
                {
                    organizeOptions[key] = EditorPrefs.GetString(prefKey);
                }
            }
        }

        // Saves selection states down to preferences instantly when values change
        public void SaveEditorPreferences()
        {
            EditorPrefs.SetString("KVR_OutputDir", outputDirectory);
            EditorPrefs.SetString("KVR_SceneName", sceneName);
            EditorPrefs.SetString("KVR_PrefabName", prefabName);
            EditorPrefs.SetBool("KVR_CreatePrefab", createPrefab);
            EditorPrefs.SetBool("KVR_AutoParsePoi", autoParsePoiyomi);
            EditorPrefs.SetBool("KVR_AutoSetupVRC", autoSetupVRCDescriptor);

            foreach (KeyValuePair<string, string> kvp in organizeOptions)
            {
                EditorPrefs.SetString("KVR_Opt_" + kvp.Key, kvp.Value);
            }
        }
        // =========================================================================
        // BLOCK 5: WINDOW GUI RENDER ENTRYPOINT LOOPS & SERIALIZATION HOOKS
        // =========================================================================

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck(); // Watch the UI canvas frame window layout inputs for adjustments

            KaleidoAssetOrganizerUI.DrawHeader(this, headerIcon, VERSION);
            KaleidoAssetOrganizerUI.DrawOutputDirectory(this);
            GUILayout.Space(5);
            KaleidoAssetOrganizerUI.DrawSettings(this);
            GUILayout.Space(5);
            KaleidoAssetOrganizerUI.DrawObjectsToOrganize(this);
            GUILayout.Space(5);
            KaleidoAssetOrganizerUI.DrawOrganizeOptions(this);
            GUILayout.Space(5);
            KaleidoAssetOrganizerUI.DrawIgnoreList(this);
            GUILayout.Space(5);
            KaleidoAssetOrganizerUI.DrawOrganizeButton(this);
            KaleidoAssetOrganizerUI.DrawDiscordButton();
            KaleidoAssetOrganizerUI.DrawFooter(this);

            // If you change a dropdown toggle or directory text box choice field path, save it immediately
            if (EditorGUI.EndChangeCheck())
            {
                SaveEditorPreferences();
            }

            ResizeWindow();
        }
        // =========================================================================
        // BLOCK 6: PANEL BOUNDARY DIMENSION EQUATIONS
        // =========================================================================

        public void ResizeWindow()
        {
            float logoHeight = headerIcon != null ? 200f : 15f;
            float outputDirHeight = 45f;
            float settingsHeight = 115f;
            float objectsHeight = 65f + (objectsToOrganize.Count * 22f);
            float organizeOptionsHeight = (organizeOptions.Count * 22f) + 70f;
            float ignoreListHeight = 65f + (ignoreList.Count * 22f);
            float organizeButtonHeight = 55f;
            float discordButtonHeight = 28f;
            float footerHeight = 25f;

            float totalHeight = logoHeight + outputDirHeight + settingsHeight + objectsHeight + organizeOptionsHeight + ignoreListHeight + organizeButtonHeight + discordButtonHeight + footerHeight;

            Vector2 targetSize = new Vector2(500, totalHeight);
            minSize = targetSize;
            maxSize = targetSize;
        }
    }
    // =========================================================================
    // BLOCK 7: HELPER MULTI-DROP STACKING RECEIVERS
    // =========================================================================

    public static class KaleidoAssetOrganizerHelpers
    {
        public static void HandleDragAndDrop(Rect dropArea, List<UnityEngine.Object> targetList)
        {
            Event evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                    {
                        if (obj == null) continue;
                        if (!targetList.Contains(obj))
                        {
                            targetList.Add(obj);
                        }
                    }
                    GUI.changed = true;
                }
                Event.current.Use();
            }
        }
        // =========================================================================
        // BLOCK 8: RECURSIVE EXCLUSIONS DEPENDENCY BUILDERS
        // =========================================================================

        public static HashSet<string> BuildRecursiveIgnoreMap(List<UnityEngine.Object> rawIgnoreList)
        {
            HashSet<string> subAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<UnityEngine.Object> collectedObjects = new List<UnityEngine.Object>();

            foreach (var root in rawIgnoreList)
            {
                if (root == null) continue;
                string rootPath = AssetDatabase.GetAssetPath(root);
                if (!string.IsNullOrEmpty(rootPath) && !subAssetPaths.Contains(rootPath))
                {
                    subAssetPaths.Add(rootPath);
                }

                if (root is GameObject go)
                {
                    collectedObjects.Add(go);
                    foreach (Transform child in go.transform) CollectChildObjects(child, collectedObjects, new List<UnityEngine.Object>());
                }
            }
            // =========================================================================
            // BLOCK 9: DOWNSTREAM DEPENDENCIES RESOLVER BLOCKS
            // =========================================================================

            if (collectedObjects.Count > 0)
            {
                var deps = EditorUtility.CollectDependencies(collectedObjects.ToArray());
                foreach (var dep in deps)
                {
                    if (dep == null) continue;
                    string depPath = AssetDatabase.GetAssetPath(dep);
                    if (!string.IsNullOrEmpty(depPath) && !subAssetPaths.Contains(depPath)) subAssetPaths.Add(depPath);
                }
            }
            return subAssetPaths;
        }
        // =========================================================================
        // BLOCK 10: HIERARCHY SCANNERS & SUFFIX FILTER RULES
        // =========================================================================

        public static List<UnityEngine.Object> GetAllObjectsIncludingChildren(List<UnityEngine.Object> roots, List<UnityEngine.Object> ignoreList)
        {
            List<UnityEngine.Object> allObjects = new List<UnityEngine.Object>();
            foreach (var root in roots)
            {
                if (root == null || ignoreList.Contains(root)) continue;
                if (root is GameObject go)
                {
                    allObjects.Add(go);
                    foreach (Transform child in go.transform) CollectChildObjects(child, allObjects, ignoreList);
                }
                else allObjects.Add(root);
            }
            return allObjects;
        }

        private static void CollectChildObjects(Transform parent, List<UnityEngine.Object> list, List<UnityEngine.Object> ignoreList)
        {
            if (parent == null || ignoreList.Contains(parent.gameObject)) return;
            list.Add(parent.gameObject);
            foreach (Transform child in parent) CollectChildObjects(child, list, ignoreList);
        }
        // =========================================================================
        // BLOCK 11: FILE PATH SORTER DESTINATION PARSERS
        // =========================================================================

        public static bool ShouldIgnoreAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) return true;
            string lower = path.ToLowerInvariant();
            return lower.EndsWith(".cs") || lower.EndsWith(".dll") || lower.EndsWith(".asmdef") || lower.EndsWith(".pdb")
                   || lower.EndsWith(".meta") || lower.StartsWith("packages/") || lower.StartsWith("assets/editor");
        }

        public static string GetTargetFolder(string typeName, string assetPath = "", bool parsePoiyomi = false)
        {
            if (parsePoiyomi && typeName == "Texture2D" && !string.IsNullOrEmpty(assetPath))
            {
                string lowerName = Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
                if (lowerName.Contains("normal") || lowerName.EndsWith("_n") || lowerName.Contains("nrm")) return "Textures/Normals";
                if (lowerName.Contains("emission") || lowerName.Contains("emissive") || lowerName.EndsWith("_e")) return "Textures/Emissions";
                if (lowerName.Contains("metallic") || lowerName.Contains("metal") || lowerName.EndsWith("_m")) return "Textures/Metallic";
                if (lowerName.Contains("roughness") || lowerName.Contains("rough") || lowerName.EndsWith("_r")) return "Textures/Roughness";
                if (lowerName.Contains("ao") || lowerName.Contains("occlusion")) return "Textures/AO";
            }

            // FIXED: Standardized type routing names so Animators route consistently into '3.0/Controllers' or 'Other' as intended
            return typeName switch
            {
                "GameObject" => "FBX",
                "Material" => "Materials",
                "Texture2D" => "Textures",
                "Cubemap" => "Textures",
                "AudioClip" => "Audio",
                "AnimationClip" => "3.0/Animations",
                "BlendTree" => "3.0/BlendTrees",
                "AvatarMask" => "3.0/Avatar Masks",
                "AnimatorController" => "3.0/Controllers",
                "RuntimeAnimatorController" => "3.0/Controllers",
                "AnimatorOverrideController" => "3.0/Controllers",
                "VRCExpressionParameters" => "3.0/VRCExpressionParameters",
                "VRCExpressionsMenu" => "3.0/Menus",
                _ => "Other"
            };
        }
    }
    // =========================================================================
    // BLOCK 12: ORGANIZATION ENGINE ENTRYPOINTS & LOGGING SETUP
    // =========================================================================

    public static class KaleidoAssetOrganizerLogic
    {
        public static void OrganizeAssets(KaleidoAssetOrganizer window)
        {
            string projectRoot = GetProjectRootPath();
            string logDir = Path.Combine(projectRoot, "Logs", "KaleidoVR", "Organizer");
            Directory.CreateDirectory(logDir);
            string logFile = Path.Combine(logDir, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_organizer_log.txt");

            List<string> logEntries = new List<string>() { "KaleidoVR Asset Organization Pipeline Began at " + DateTime.Now };
            Dictionary<string, string> movedAssetsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                Debug.Log("[KaleidoVR] Running integrated avatar asset organization tool...");

                EditorUtility.DisplayProgressBar("KaleidoVR Asset Organizer", "Analyzing avatar project hierarchy and dependencies...", 0.05f);

                HashSet<string> fullyIgnoredAssetPaths = KaleidoAssetOrganizerHelpers.BuildRecursiveIgnoreMap(window.ignoreList);

                string clearPrefabFolder = $"{window.outputDirectory}/Prefabs".Replace("\\", "/");
                EnsureSingleAssetDirectory(clearPrefabFolder);

                int copied = 0, moved = 0, ignored = 0;
                var flatObjects = KaleidoAssetOrganizerHelpers.GetAllObjectsIncludingChildren(window.objectsToOrganize, window.ignoreList);

                List<UnityEngine.Object> assetsToScan = new List<UnityEngine.Object>();
                foreach (var obj in flatObjects)
                {
                    if (obj == null) continue;

                    UnityEngine.Object targetAssetSource = obj;
                    if (obj is GameObject go && PrefabUtility.IsPartOfAnyPrefab(go))
                    {
                        UnityEngine.Object source = SafeGetPrefabAssetSource(go);
                        if (source != null) targetAssetSource = source;
                    }

                    string assetPath = AssetDatabase.GetAssetPath(targetAssetSource);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                        if (mainAsset != null && !assetsToScan.Contains(mainAsset)) assetsToScan.Add(mainAsset);
                    }
                    else
                    {
                        if (!assetsToScan.Contains(targetAssetSource)) assetsToScan.Add(targetAssetSource);
                    }
                }

                if (assetsToScan.Count == 0)
                {
                    logEntries.Add("No project assets were found on the objects list. Drop FBX/prefab assets from the Project window.");
                    Debug.LogWarning("[KaleidoVR] Nothing to organize. Add Project assets (not scene-only objects) to the list.");
                    return;
                }

                var dependencies = EditorUtility.CollectDependencies(assetsToScan.ToArray());

                int totalAssets = Mathf.Max(1, dependencies.Length);
                int currentAssetIndex = 0;
                HashSet<string> processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                // =========================================================================
                // BLOCK 13: FILE TRANSFER ITERATOR EXECUTION FRAMEWORK
                // =========================================================================
                // CopyAsset/MoveAsset run with a live Asset Database so folder creation and
                // unique-path checks work the same on 2022.3.22f1 and Unity 6.
                // (AssetDatabase.AssetEditingScope is 2023.1+ only — do not use it here.)

                foreach (var dep in dependencies)
                {
                    currentAssetIndex++;
                    if (dep == null) continue;
                    string path = AssetDatabase.GetAssetPath(dep);
                    if (string.IsNullOrEmpty(path)) continue;

                    float progressPercentage = (float)currentAssetIndex / totalAssets;
                    EditorUtility.DisplayProgressBar(
                        "KaleidoVR Asset Organizer",
                        $"Batch Transferring Assets Safely ({currentAssetIndex}/{totalAssets}): {dep.name}",
                        progressPercentage
                    );

                    if (processedPaths.Contains(path)) continue;
                    processedPaths.Add(path);

                    if (KaleidoAssetOrganizerHelpers.ShouldIgnoreAsset(path) || fullyIgnoredAssetPaths.Contains(path))
                    {
                        ignored++;
                        logEntries.Add("Ignored: " + path);
                        continue;
                    }

                    UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                    string typeName = mainAsset != null ? mainAsset.GetType().Name : dep.GetType().Name;
                    if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                    {
                        typeName = "GameObject";
                    }

                    string action = window.organizeOptions.ContainsKey(typeName) ? window.organizeOptions[typeName] : "Copy";
                    if (action == "Ignore")
                    {
                        ignored++;
                        logEntries.Add("Export Ignore: " + path);
                        continue;
                    }

                    string targetFolder = KaleidoAssetOrganizerHelpers.GetTargetFolder(typeName, path, window.autoParsePoiyomi);
                    string localAssetFolder = $"{window.outputDirectory}/{targetFolder}".Replace("\\", "/");
                    EnsureSingleAssetDirectory(localAssetFolder);

                    string targetPath = $"{window.outputDirectory}/{targetFolder}/{Path.GetFileName(path)}".Replace("\\", "/");
                    if (path.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        movedAssetsMap[path] = path;
                        continue;
                    }

                    if (AssetDatabase.LoadMainAssetAtPath(targetPath) != null && !path.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);
                    }

                    if (action == "Copy")
                    {
                        if (AssetDatabase.CopyAsset(path, targetPath))
                        {
                            copied++;
                            movedAssetsMap[path] = targetPath;
                            logEntries.Add("Copied: " + path + " -> " + targetPath);
                        }
                        else
                        {
                            logEntries.Add("Copy failed: " + path);
                            Debug.LogWarning("[KaleidoVR] CopyAsset failed for " + path);
                        }
                    }
                    else if (action == "Move")
                    {
                        string moveError = AssetDatabase.MoveAsset(path, targetPath);
                        if (string.IsNullOrEmpty(moveError))
                        {
                            moved++;
                            movedAssetsMap[path] = targetPath;
                            logEntries.Add("Moved: " + path + " -> " + targetPath);
                        }
                        else
                        {
                            logEntries.Add("Move failed: " + path + " (" + moveError + ")");
                            Debug.LogWarning("[KaleidoVR] MoveAsset failed for " + path + ": " + moveError);
                        }
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                RemapCopiedAssetReferences(movedAssetsMap);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                // =========================================================================
                // BLOCK 14: AUTOMATED PREFAB PACKAGING ENGINE LOOPS
                // =========================================================================

                if (window.createPrefab)
                {
                    string prefabPath = $"{window.outputDirectory}/Prefabs/{window.prefabName}.prefab".Replace("\\", "/");
                    EnsureSingleAssetDirectory($"{window.outputDirectory}/Prefabs".Replace("\\", "/"));
                    GameObject finalTargetRoot = null;
                    List<GameObject> instantiatedInstances = new List<GameObject>();

                    try
                    {
                        List<GameObject> activeTargetGameObjects = new List<GameObject>();
                        foreach (var obj in window.objectsToOrganize)
                        {
                            if (obj is GameObject go) activeTargetGameObjects.Add(go);
                        }

                        if (activeTargetGameObjects.Count == 1)
                        {
                            GameObject rootGo = ResolveOrganizedGameObject(activeTargetGameObjects[0], movedAssetsMap);
                            finalTargetRoot = InstantiateForPrefab(rootGo);
                            if (finalTargetRoot != null)
                            {
                                finalTargetRoot.name = window.prefabName;
                                instantiatedInstances.Add(finalTargetRoot);
                            }
                        }
                        else if (activeTargetGameObjects.Count > 1)
                        {
                            finalTargetRoot = new GameObject(window.prefabName);
                            foreach (var go in activeTargetGameObjects)
                            {
                                GameObject targetGo = ResolveOrganizedGameObject(go, movedAssetsMap);
                                GameObject instance = InstantiateForPrefab(targetGo);
                                if (instance != null)
                                {
                                    instance.transform.SetParent(finalTargetRoot.transform);
                                    instantiatedInstances.Add(instance);
                                }
                            }
                        }

                        if (finalTargetRoot != null)
                        {
                            if (PrefabUtility.IsPartOfPrefabInstance(finalTargetRoot))
                            {
                                PrefabUtility.UnpackPrefabInstance(finalTargetRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                            }

                            foreach (GameObject inst in instantiatedInstances)
                            {
                                if (inst != null && inst != finalTargetRoot && PrefabUtility.IsPartOfPrefabInstance(inst))
                                {
                                    PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                                }
                            }

                            Component[] allComponents = finalTargetRoot.GetComponentsInChildren<Component>(true);
                            foreach (Component comp in allComponents)
                            {
                                if (comp == null) continue;
                                RemapSerializedReferences(comp, movedAssetsMap);
                            }

                            if (window.autoSetupVRCDescriptor) ApplyVRCDescriptorSetup(finalTargetRoot, movedAssetsMap);

                            AssetDatabase.SaveAssets();
                            PrefabUtility.SaveAsPrefabAsset(finalTargetRoot, prefabPath);
                            logEntries.Add("Prefab saved: " + prefabPath);
                            Debug.Log($"[KaleidoVR] Pipeline operations finalized. Backup file records mapped independently.");
                        }
                    }
                    finally
                    {
                        if (finalTargetRoot != null)
                        {
                            UnityEngine.Object.DestroyImmediate(finalTargetRoot);
                        }
                    }
                }
                Debug.Log($"[KaleidoVR] Pipeline Complete. Copied={copied}, Moved={moved}, Ignored={ignored}");
                logEntries.Add($"Pipeline Complete. Copied={copied}, Moved={moved}, Ignored={ignored}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[KaleidoVR] Critical error during organization: " + ex);
                logEntries.Add("Critical error: " + ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                try
                {
                    File.WriteAllLines(logFile, logEntries.ToArray());
                }
                catch (Exception logEx)
                {
                    Debug.LogWarning("[KaleidoVR] Could not write organizer log: " + logEx.Message);
                }
            }
        }

        private static UnityEngine.Object SafeGetPrefabAssetSource(UnityEngine.Object instanceOrAsset)
        {
            if (instanceOrAsset == null) return null;
            try
            {
                UnityEngine.Object original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(instanceOrAsset);
                if (original != null) return original;
            }
            catch (Exception)
            {
            }

            try
            {
                UnityEngine.Object source = PrefabUtility.GetCorrespondingObjectFromSource(instanceOrAsset);
                if (source != null) return source;
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static string GetProjectRootPath()
        {
            string dataPath = Application.dataPath.Replace("\\", "/");
            if (dataPath.EndsWith("/Assets", StringComparison.OrdinalIgnoreCase))
            {
                return dataPath.Substring(0, dataPath.Length - "Assets".Length);
            }
            return Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
        }

        private static GameObject ResolveOrganizedGameObject(GameObject source, Dictionary<string, string> movedAssetsMap)
        {
            if (source == null) return null;

            string oldAssetPath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(oldAssetPath) && PrefabUtility.IsPartOfAnyPrefab(source))
            {
                UnityEngine.Object prefabSource = SafeGetPrefabAssetSource(source);
                oldAssetPath = AssetDatabase.GetAssetPath(prefabSource);
            }

            if (!string.IsNullOrEmpty(oldAssetPath) && movedAssetsMap.TryGetValue(oldAssetPath, out string newPath))
            {
                GameObject updatedAssetSource = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);
                if (updatedAssetSource != null) return updatedAssetSource;
            }

            return source;
        }

        private static GameObject TryInstantiatePrefab(UnityEngine.Object prefabAsset)
        {
            if (prefabAsset == null) return null;
            try
            {
                return PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static GameObject InstantiateForPrefab(GameObject source)
        {
            if (source == null) return null;

            if (PrefabUtility.IsPartOfPrefabAsset(source))
            {
                GameObject prefabInstance = TryInstantiatePrefab(source);
                if (prefabInstance != null) return prefabInstance;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(source))
            {
                UnityEngine.Object prefabAsset = SafeGetPrefabAssetSource(source);
                GameObject prefabInstance = TryInstantiatePrefab(prefabAsset);
                if (prefabInstance != null) return prefabInstance;
            }

            string assetPath = AssetDatabase.GetAssetPath(source);
            if (!string.IsNullOrEmpty(assetPath))
            {
                UnityEngine.Object main = AssetDatabase.LoadMainAssetAtPath(assetPath);
                GameObject prefabInstance = TryInstantiatePrefab(main);
                if (prefabInstance != null) return prefabInstance;
            }

            return UnityEngine.Object.Instantiate(source);
        }

        private static void RemapCopiedAssetReferences(Dictionary<string, string> movedAssetsMap)
        {
            HashSet<string> newPaths = new HashSet<string>(movedAssetsMap.Values, StringComparer.OrdinalIgnoreCase);
            foreach (string newPath in newPaths)
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(newPath);
                if (assets == null) continue;
                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset == null) continue;
                    RemapSerializedReferences(asset, movedAssetsMap);
                }
            }
        }

        private static void RemapSerializedReferences(UnityEngine.Object target, Dictionary<string, string> movedAssetsMap)
        {
            if (target == null || movedAssetsMap == null || movedAssetsMap.Count == 0) return;

            try
            {
                SerializedObject serializedObject = new SerializedObject(target);
                SerializedProperty property = serializedObject.GetIterator();
                bool modified = false;
                bool enterChildren = true;
                while (property.Next(enterChildren))
                {
                    enterChildren = true;
                    try
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        if (property.name == "m_Script") continue;

                        UnityEngine.Object referenced = property.objectReferenceValue;
                        if (referenced == null) continue;

                        string referencedPath = AssetDatabase.GetAssetPath(referenced);
                        if (string.IsNullOrEmpty(referencedPath)) continue;
                        if (!movedAssetsMap.TryGetValue(referencedPath, out string remappedPath)) continue;
                        if (referencedPath.Equals(remappedPath, StringComparison.OrdinalIgnoreCase)) continue;

                        UnityEngine.Object remapped = AssetDatabase.LoadAssetAtPath(remappedPath, referenced.GetType());
                        if (remapped == null) remapped = AssetDatabase.LoadMainAssetAtPath(remappedPath);
                        if (remapped == null) continue;

                        property.objectReferenceValue = remapped;
                        modified = true;
                    }
                    catch (Exception)
                    {
                        enterChildren = false;
                    }
                }

                if (modified) serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            catch (Exception)
            {
            }
        }
        // =========================================================================
        // BLOCK 15: DESCRIPTOR LAYER AUTOMATION LINKS ENGINE
        // =========================================================================

        private static Type FindTypeByFullName(string fullName)
        {
            Type direct = Type.GetType(fullName + ", VRC.SDK3A") ?? Type.GetType(fullName);
            if (direct != null) return direct;

            Assembly[] assemblies;
            try
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception)
            {
                return null;
            }

            foreach (var assembly in assemblies)
            {
                if (assembly == null) continue;
                try
                {
                    Type found = assembly.GetType(fullName, false);
                    if (found != null) return found;
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        private static bool IsFxAnimLayer(SerializedProperty typeProp)
        {
            if (typeProp == null) return false;
            try
            {
                if (typeProp.propertyType == SerializedPropertyType.Enum)
                {
                    string[] names = typeProp.enumNames;
                    int idx = typeProp.enumValueIndex;
                    if (names != null && idx >= 0 && idx < names.Length && names[idx] == "FX")
                    {
                        return true;
                    }
                }
                return typeProp.intValue == 5;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ApplyVRCDescriptorSetup(GameObject root, Dictionary<string, string> movedAssetsMap)
        {
            Type vrcDescriptorType = FindTypeByFullName("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
            if (vrcDescriptorType == null)
            {
                Debug.LogWarning("[KaleidoVR] VRCAvatarDescriptor was not found. Install the VRChat Avatars SDK to auto-link FX & Menu.");
                return;
            }

            Component descriptor = root.GetComponent(vrcDescriptorType) ?? root.AddComponent(vrcDescriptorType);
            var expressionsMenuType = FindTypeByFullName("VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu");
            var expressionParamsType = FindTypeByFullName("VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters");

            RuntimeAnimatorController targetFXController = null;
            UnityEngine.Object targetMenu = null;
            UnityEngine.Object targetParams = null;
            foreach (var kvp in movedAssetsMap)
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(kvp.Value);
                if (asset is RuntimeAnimatorController rac && rac.name.ToLowerInvariant().Contains("fx"))
                {
                    targetFXController = rac;
                }
                else if (asset != null && expressionsMenuType != null && expressionsMenuType.IsAssignableFrom(asset.GetType()))
                {
                    targetMenu = asset;
                }
                else if (asset != null && expressionParamsType != null && expressionParamsType.IsAssignableFrom(asset.GetType()))
                {
                    targetParams = asset;
                }
            }

            try
            {
                SerializedObject descriptorObject = new SerializedObject(descriptor);
                SerializedProperty menuProp = descriptorObject.FindProperty("expressionsMenu");
                SerializedProperty paramsProp = descriptorObject.FindProperty("expressionParameters");
                if (menuProp != null && targetMenu != null) menuProp.objectReferenceValue = targetMenu;
                if (paramsProp != null && targetParams != null) paramsProp.objectReferenceValue = targetParams;

                SerializedProperty layers = descriptorObject.FindProperty("baseAnimationLayers");
                if (layers != null && targetFXController != null)
                {
                    for (int i = 0; i < layers.arraySize; i++)
                    {
                        SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                        SerializedProperty typeProp = layer.FindPropertyRelative("type");
                        bool isFx = IsFxAnimLayer(typeProp);

                        if (!isFx && i != 4) continue;
                        if (!isFx && i == 4 && layers.arraySize != 5) continue;

                        SerializedProperty isDefaultProp = layer.FindPropertyRelative("isDefault");
                        SerializedProperty controllerProp = layer.FindPropertyRelative("animatorController");
                        SerializedProperty enabledProp = layer.FindPropertyRelative("isEnabled");
                        if (isDefaultProp != null) isDefaultProp.boolValue = false;
                        if (enabledProp != null) enabledProp.boolValue = true;
                        if (controllerProp != null) controllerProp.objectReferenceValue = targetFXController;
                        if (isFx) break;
                    }
                }

                descriptorObject.ApplyModifiedPropertiesWithoutUndo();
            }
            catch (Exception ex) { Debug.LogWarning("[KaleidoVR] Reflection error: " + ex.Message); }
        }

        public static void EnsureSingleAssetDirectory(string targetFolderPath)
        {
            if (string.IsNullOrEmpty(targetFolderPath)) return;

            string normalized = targetFolderPath.Replace("\\", "/").TrimEnd('/');
            string[] parts = normalized.Split('/');
            if (parts.Length == 0 || !string.Equals(parts[0], "Assets", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[KaleidoVR] Output folder must be inside Assets: " + targetFolderPath);
                return;
            }

            string absoluteDiskDirectory = Path.Combine(GetProjectRootPath(), normalized).Replace("\\", "/");
            if (!Directory.Exists(absoluteDiskDirectory))
            {
                Directory.CreateDirectory(absoluteDiskDirectory);
            }

            if (AssetDatabase.IsValidFolder(normalized)) return;

            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) continue;
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    try
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    catch (Exception)
                    {
                    }
                }
                current = next;
            }
        }
    }
}
// =========================================================================
// BLOCK 16: USER INTERFACE LIST SCRIPT SORTERS & OBJECT DROP GRIDS
// =========================================================================

namespace KaleidoVR.EditorTools
{
    public static class KaleidoAssetOrganizerUI
    {
        private static readonly string[] organizeActions = new string[] { "Copy", "Move", "Ignore" };

        public static void DrawHeader(KaleidoAssetOrganizer window, Texture2D logo, string version)
        {
            GUIStyle centeredTitleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            GUIStyle centeredVersionStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUILayout.Space(10); GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace();
            if (logo != null) { Rect logoRect = GUILayoutUtility.GetRect(320, 200, GUILayout.Width(320), GUILayout.Height(200)); GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit); }
            else { GUILayout.Label($"...Place your logo at {KaleidoAssetOrganizer.ICON_PATH}...", EditorStyles.miniLabel); }
            GUILayout.FlexibleSpace(); GUILayout.EndHorizontal(); GUILayout.Space(2);
            GUILayout.Label("KALEIDO VR ORGANIZER", centeredTitleStyle); GUILayout.Label($"v{version}", centeredVersionStyle);
        }

        public static void DrawOutputDirectory(KaleidoAssetOrganizer window)
        {
            GUILayout.Label("Output Directory", EditorStyles.boldLabel); EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true); EditorGUILayout.TextField(window.outputDirectory, GUILayout.ExpandWidth(true)); EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath)) window.outputDirectory = "Assets" + path.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawSettings(KaleidoAssetOrganizer window)
        {
            GUILayout.Label("Settings & Automation Pipelines", EditorStyles.boldLabel);
            window.sceneName = EditorGUILayout.TextField("Scene Name", window.sceneName);
            window.prefabName = EditorGUILayout.TextField("Prefab Name", window.prefabName);

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 180f;

            window.createPrefab = EditorGUILayout.Toggle("Create Prefab", window.createPrefab);

            EditorGUI.BeginDisabledGroup(!window.createPrefab);
            window.autoParsePoiyomi = EditorGUILayout.Toggle("Texture Sorter", window.autoParsePoiyomi);
            window.autoSetupVRCDescriptor = EditorGUILayout.Toggle("Auto-Link FX & Menu", window.autoSetupVRCDescriptor);
            EditorGUI.EndDisabledGroup();

            EditorGUIUtility.labelWidth = originalLabelWidth;
        }

        public static void DrawObjectsToOrganize(KaleidoAssetOrganizer window)
        {
            GUILayout.Label("Objects to Organize", EditorStyles.boldLabel);
            Rect dropArea = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true)); GUI.Box(dropArea, "Drag & Drop Objects Here", EditorStyles.helpBox);
            int countBeforeDrop = window.objectsToOrganize.Count; KaleidoAssetOrganizerHelpers.HandleDragAndDrop(dropArea, window.objectsToOrganize);

            if (window.objectsToOrganize.Count > countBeforeDrop && window.objectsToOrganize.Count > 0 && window.objectsToOrganize[countBeforeDrop] != null)
            {
                if (window.sceneName == "OrganizedScene" || string.IsNullOrEmpty(window.sceneName) || window.prefabName == "NewAvatar" || string.IsNullOrEmpty(window.prefabName))
                {
                    string primaryName = window.objectsToOrganize[countBeforeDrop].name;
                    window.sceneName = primaryName + "_Scene";
                    window.prefabName = primaryName;
                }
            }

            for (int i = 0; i < window.objectsToOrganize.Count; i++)
            {
                Rect rowRect = EditorGUILayout.BeginHorizontal(); if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(rowRect, i % 2 == 0 ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.23f, 0.23f, 0.23f, 1f));
                GUILayout.Space(5); UnityEngine.Object oldObj = window.objectsToOrganize[i]; window.objectsToOrganize[i] = EditorGUILayout.ObjectField(window.objectsToOrganize[i], typeof(UnityEngine.Object), false, GUILayout.ExpandWidth(true));

                Event evt = Event.current;
                if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && rowRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        // FIXED: Grab index 0 item from the drag array reference to fix the variable mapping mismatch
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            window.objectsToOrganize[i] = DragAndDrop.objectReferences[0];
                            GUI.changed = true;
                        }
                    }
                    evt.Use();
                }
                if (i == 0 && window.objectsToOrganize[i] != oldObj && window.objectsToOrganize[i] != null) { string primaryName = window.objectsToOrganize[i].name; window.sceneName = primaryName + "_Scene"; window.prefabName = primaryName; }
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    window.objectsToOrganize.RemoveAt(i); i--;
                    if (window.objectsToOrganize.Count == 0) { window.sceneName = "OrganizedScene"; window.prefabName = "NewAvatar"; }
                    else if (i < 0 && window.objectsToOrganize.Count > 0 && window.objectsToOrganize != null) { string primaryName = window.objectsToOrganize[0].name; window.sceneName = primaryName + "_Scene"; window.prefabName = primaryName; }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal(); GUILayout.Space(5); if (GUILayout.Button("+", GUILayout.Width(35))) window.objectsToOrganize.Add(null);
            if (GUILayout.Button("-", GUILayout.Width(35)) && window.objectsToOrganize.Count > 0) { window.objectsToOrganize.RemoveAt(window.objectsToOrganize.Count - 1); if (window.objectsToOrganize.Count == 0) { window.sceneName = "OrganizedScene"; window.prefabName = "NewAvatar"; } }
            GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal();
        }

        public static void DrawOrganizeOptions(KaleidoAssetOrganizer window)
        {
            GUILayout.Label("Export List Options", EditorStyles.boldLabel);
            foreach (var key in new List<string>(window.organizeOptions.Keys))
            {
                EditorGUILayout.BeginHorizontal(); GUIContent rowContent = new GUIContent($" {FriendlyName(key)}", GetNativeUnityIcon(key)); GUILayout.Label(rowContent, GUILayout.Height(18), GUILayout.Width(240)); GUILayout.FlexibleSpace();
                int currentIndex = Array.IndexOf(organizeActions, window.organizeOptions[key]); int selectedIndex = EditorGUILayout.Popup(currentIndex < 0 ? 0 : currentIndex, organizeActions, GUILayout.Width(90)); window.organizeOptions[key] = organizeActions[selectedIndex]; EditorGUILayout.EndHorizontal();
            }
        }
        // =========================================================================
        // BLOCK 17: NATIVE SKIN ICON TRANSLATORS & ACTION LINK PORTALS
        // =========================================================================

        private static Texture GetNativeUnityIcon(string typeName)
        {
            string iconName = typeName switch { "GameObject" => "Prefab Icon", "Texture2D" => "Texture2D Icon", "Cubemap" => "Cubemap Icon", "Material" => "Material Icon", "VRCExpressionParameters" => "ScriptableObject Icon", "VRCExpressionsMenu" => "ScriptableObject Icon", "BlendTree" => "BlendTree Icon", "AnimationClip" => "AnimationClip Icon", "AnimatorOverrideController" => "AnimatorOverrideController Icon", "AnimatorController" => "AnimatorController Icon", "AvatarMask" => "AvatarMask Icon", "AudioClip" => "AudioClip Icon", "Shader" => "Shader Icon", "MonoScript" => "cs Script Icon", _ => "DefaultAsset Icon" };
            try
            {
                GUIContent content = EditorGUIUtility.IconContent(iconName);
                return content != null ? content.image : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string FriendlyName(string typeName)
        {
            return typeName switch { "GameObject" => "GameObject (FBX)", "Texture2D" => "Texture2D Maps", "VRCExpressionParameters" => "VRCExpressionParameters", "VRCExpressionsMenu" => "VRCExpressionsMenu", "MonoScript" => "MonoScript (C#)", "DefaultAsset" => "DefaultAsset (DLLs)", _ => typeName };
        }

        public static void DrawIgnoreList(KaleidoAssetOrganizer window)
        {
            GUILayout.Label("Ignore List (Manually Exclude Objects)", EditorStyles.boldLabel); Rect ignoreDropArea = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true)); GUI.Box(ignoreDropArea, "Drag & Drop Objects to Exclude", EditorStyles.helpBox); KaleidoAssetOrganizerHelpers.HandleDragAndDrop(ignoreDropArea, window.ignoreList);
            for (int i = 0; i < window.ignoreList.Count; i++)
            {
                Rect rowRect = EditorGUILayout.BeginHorizontal(); window.ignoreList[i] = EditorGUILayout.ObjectField(window.ignoreList[i], typeof(UnityEngine.Object), false, GUILayout.ExpandWidth(true));
                Event evt = Event.current;
                if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) && rowRect.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        // FIXED: Grab index 0 item from the drag array reference to prevent structural mapping crashes
                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            window.ignoreList[i] = DragAndDrop.objectReferences[0];
                            GUI.changed = true;
                        }
                    }
                    evt.Use();
                }
                if (GUILayout.Button("X", GUILayout.Width(25))) { window.ignoreList.RemoveAt(i); i--; }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal(); GUILayout.Space(5); if (GUILayout.Button("+", GUILayout.Width(35))) window.ignoreList.Add(null); GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal();
        }

        public static void DrawOrganizeButton(KaleidoAssetOrganizer window) { if (GUILayout.Button("Organize Assets", GUILayout.Height(35))) KaleidoAssetOrganizerLogic.OrganizeAssets(window); }
        public static void DrawDiscordButton() { GUILayout.Space(2); if (GUILayout.Button("💬 Join the Discord Server", GUILayout.Height(24))) Application.OpenURL("https://discord.com/invite/cRsufJssTA"); }
        public static void DrawFooter(KaleidoAssetOrganizer window) { GUILayout.Space(5); if (GUILayout.Button("Visit kalivr.com", EditorStyles.centeredGreyMiniLabel)) Application.OpenURL("https://kalivr.com"); }
    }
}