﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GameObjectBrush {

    /// <summary>
    /// The main class of this extension/tool that handles the ui and the brush/paint functionality
    /// </summary>
    public class GameObjectBrushEditor : EditorWindow {

        #region Properties


        //version variable
        private static string version = "v3.0";

        //some utility vars used to determine if the editor window is open
        public static GameObjectBrushEditor Instance { get; private set; }
        public static bool IsOpen {
            get { return Instance != null; }
        }

        //custom vars that hold the brushes, the current brush, the copied brush settings/details, the scroll position of the scroll view and all previously spawned objects
        private List<BrushObject> brushes = new List<BrushObject>();
        public List<BrushObject> currentBrushes = new List<BrushObject>();
        public BrushObject selectedBrush = null;                                    //The currently selected/viewes brush (has to be public in order to be accessed by the FindProperty method)
        private List<GameObject> spawnedObjects = new List<GameObject>();
        private Vector2 scrollViewScrollPosition = new Vector2();
        private BrushObject copy = null;

        private bool isErasingEnabled = true;
        private bool isPlacingEnabled = true;

        private Color red = new Color((float)186 / 256, (float)48 / 256, (float)48 / 256);
        private Color green = new Color((float)91 / 256,(float)186 / 256, (float)48 / 256);
        private Color lightBlue = new Color((float)60 / 256, (float)160/ 256, (float)256 / 256);
        private Color darkBlue = new Color((float)14 / 256, (float) 36 / 256, (float) 56 / 256);


        #endregion


        /// <summary>
        /// Method that creates the window initially
        /// </summary>
        [MenuItem("Tools/GameObject Brush")]
        public static void ShowWindow() {
            //Show existing window instance. If one doesn't exist, make one.
            DontDestroyOnLoad(GetWindow<GameObjectBrushEditor>("GO Brush " + version));
        }

        void OnEnable() {
            SceneView.onSceneGUIDelegate += SceneGUI;
            Instance = this;
        }
        public void OnGUI() {
            SerializedObject so = new SerializedObject(this);
            EditorGUIUtility.wideMode = true;

            if (currentBrushes != null && currentBrushes.Count > 0) {
                EditorGUILayout.LabelField("Your Brushes (Current: " + GetCurrentBrushesString() + ")", EditorStyles.boldLabel);
            } else {
                EditorGUILayout.LabelField("Your Brushes", EditorStyles.boldLabel);
            }

            //scroll view
            scrollViewScrollPosition = EditorGUILayout.BeginScrollView(scrollViewScrollPosition, false, false);
            EditorGUILayout.BeginHorizontal();
            foreach (BrushObject brObj in brushes) {

                Color guiColor = GUI.backgroundColor;
                if (currentBrushes.Contains(brObj)) {
                    GUI.backgroundColor = lightBlue;
                    if (selectedBrush == brObj) {
                        GUI.backgroundColor = darkBlue;
                    }
                }

                //Create the brush entry in the scroll view and check if the user clicked on the created button (change the currently selected/edited brush accordingly and add it to the current brushes if possible)
                GUIContent btnContent = new GUIContent(AssetPreview.GetAssetPreview(brObj.brushObject), "Select the " + brObj.brushObject.name + " brush");
                if (GUILayout.Button(btnContent, GUILayout.Width(100), GUILayout.Height(100))) {



                    //Add and remove brushes from the current brushes list
                    if (Event.current.control && !currentBrushes.Contains(brObj)) {
                        currentBrushes.Add(brObj);
                    } else if (currentBrushes.Contains(brObj)) {
                        currentBrushes.Remove(brObj);
                    }

                    //select the currently edited brush and deselect all selected brushes
                    if (!Event.current.control) {
                        currentBrushes.Clear();
                        selectedBrush = brObj;
                        currentBrushes.Add(brObj);
                    }
                }
                GUI.backgroundColor = guiColor;
            }

            //add button
            if (GUILayout.Button("+", GUILayout.Width(100), GUILayout.Height(100))) {
                AddObjectPopup.Init(brushes, this);
            }


            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            //gui below the scroll view
            Color guiColorBGC = GUI.backgroundColor;
            
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = green;
            if (GUILayout.Button(new GUIContent("Add Brush", "Add a new brush to the selection."))) {
                AddObjectPopup.Init(brushes, this);
            }
            GUI.backgroundColor = red;
            if (GUILayout.Button(new GUIContent("Remove Current Brush(es)", "Removes the currently selected brush."))) {
                if (currentBrushes != null) {
                    foreach (BrushObject brush in currentBrushes) {
                        brushes.Remove(brush);
                    }
                    currentBrushes = new List<BrushObject>();
                }
            }
            if (GUILayout.Button(new GUIContent("Clear Brushes", "Removes all brushes."))) {
                brushes.Clear();
                currentBrushes = new List<BrushObject>();
            }
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = guiColorBGC;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            isPlacingEnabled = EditorGUILayout.Toggle(new GUIContent("Painting ebanled", "Should painting of gameobjects via left click be enabled?"), isPlacingEnabled);
            isErasingEnabled = EditorGUILayout.Toggle(new GUIContent("Erasing ebanled", "Should erasing of gameobjects via right click be enabled?"), isErasingEnabled);
            EditorGUILayout.EndHorizontal();





            guiColorBGC = GUI.backgroundColor;
            GUI.backgroundColor = green;
            if (GUILayout.Button(new GUIContent("Permanently Apply Spawned GameObjects (" + spawnedObjects.Count + ")", "Permanently apply the gameobjects that have been spawned with GO brush, so they can not be erased by accident anymore."))) {
                ApplyMeshedPermanently();
            }

            GUI.backgroundColor = red;
            if (GUILayout.Button(new GUIContent("Remove All Spawned GameObjects (" + spawnedObjects.Count + ")", "Removes all spawned objects from the scene that have not been applied before."))) {
                RemoveAllSpawnedObjects();
            }
            GUI.backgroundColor = guiColorBGC;

            //don't show the details of the current brush if we do not have selected a current brush
            if (currentBrushes != null && selectedBrush.brushObject != null) {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Brush Details", EditorStyles.boldLabel);

                if (GUILayout.Button(new GUIContent("Copy", "Copies the brush."), GUILayout.MaxWidth(50))) {
                    copy = selectedBrush;
                }
                if (GUILayout.Button(new GUIContent("Paste", "Pastes the details of the brush in the clipboard."), GUILayout.MaxWidth(50))) {
                    selectedBrush.PasteDetails(copy);
                }
                if (GUILayout.Button(new GUIContent("Reset", "Restores the defaults settings of the brush details."), GUILayout.MaxWidth(50))) {
                    selectedBrush.ResetDetails();
                }
                EditorGUILayout.EndHorizontal();

                selectedBrush.density = EditorGUILayout.Slider(new GUIContent("Density", "Changes the density of the brush, i.e. how many gameobjects are spawned inside the radius of the brush."), selectedBrush.density, 0f, 5f);
                selectedBrush.brushSize = EditorGUILayout.Slider(new GUIContent("Brush Size", "The radius of the brush."), selectedBrush.brushSize, 0f, 25f);
                selectedBrush.offsetFromPivot = EditorGUILayout.Vector3Field(new GUIContent("Offset from Pivot", "Changes the offset of the spawned gameobject from the calculated position. This allows you to correct the position of the spawned objects, if you find they are floating for example due to a not that correct pivot on the gameobject/prefab."), selectedBrush.offsetFromPivot);
                selectedBrush.rotOffsetFromPivot = EditorGUILayout.Vector3Field(new GUIContent("Rotational Offset", "Changes the rotational offset that is applied to the prefab/gameobject when spawning it. This allows you to current the rotation of the spawned objects."), selectedBrush.rotOffsetFromPivot);


                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Min and Max Scale", "The min and max range of the spawned gameobject. If they are not the same value a random value in between the min and max is going to be picked."));
                EditorGUILayout.MinMaxSlider(ref selectedBrush.minScale, ref selectedBrush.maxScale, 0.001f, 50);
                selectedBrush.minScale = EditorGUILayout.FloatField(selectedBrush.minScale);
                selectedBrush.maxScale = EditorGUILayout.FloatField(selectedBrush.maxScale);
                EditorGUILayout.EndHorizontal();

                selectedBrush.alignToSurface = EditorGUILayout.Toggle(new GUIContent("Align to Surface", "This option allows you to align the instantiated gameobjects to the surface you are painting on."), selectedBrush.alignToSurface);

                EditorGUILayout.BeginHorizontal();
                selectedBrush.randomizeXRotation = EditorGUILayout.Toggle(new GUIContent("Randomize X Rotation", "Should the rotation be randomized on the x axis?"), selectedBrush.randomizeXRotation);
                selectedBrush.randomizeYRotation = EditorGUILayout.Toggle(new GUIContent("Randomize Y Rotation", "Should the rotation be randomized on the y axis?"), selectedBrush.randomizeYRotation);
                selectedBrush.randomizeZRotation = EditorGUILayout.Toggle(new GUIContent("Randomize Z Rotation", "Should the rotation be randomized on the z axis?"), selectedBrush.randomizeZRotation);
                EditorGUILayout.EndHorizontal();

                selectedBrush.allowIntercollision = EditorGUILayout.Toggle(new GUIContent("Allow Intercollision", "Should the spawned objects be considered for the spawning of new objects? If so, newly spawned objects can be placed on top of previously (not yet applied) objects."), selectedBrush.allowIntercollision);


                EditorGUILayout.Space();
                EditorGUILayout.Space();


                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
                if (GUILayout.Button(new GUIContent("Copy", "Copies the brush."), GUILayout.MaxWidth(50))) {
                    copy = selectedBrush;
                }
                if (GUILayout.Button(new GUIContent("Paste", "Pastes the filters of the brush in the clipboard."), GUILayout.MaxWidth(50))) {
                    selectedBrush.PasteFilters(copy);
                }
                if (GUILayout.Button(new GUIContent("Reset", "Restores the defaults settings of the brush filters."), GUILayout.MaxWidth(50))) {
                    selectedBrush.ResetFilters();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Min and Max Slope", "The range of slope that is required for an object to be placed. If the slope is not in that range, no object is going to be placed."));
                EditorGUILayout.MinMaxSlider(ref selectedBrush.minSlope, ref selectedBrush.maxSlope, 0, 360);
                selectedBrush.minSlope = EditorGUILayout.FloatField(selectedBrush.minSlope);
                selectedBrush.maxSlope = EditorGUILayout.FloatField(selectedBrush.maxSlope);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(so.FindProperty("selectedBrush").FindPropertyRelative("layerFilter"), true);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(so.FindProperty("selectedBrush").FindPropertyRelative("isTagFilteringEnabled"), true);
                if (selectedBrush.isTagFilteringEnabled) {
                    selectedBrush.tagFilter = EditorGUILayout.TagField(new GUIContent("Tag Filter", "Limits the painting to objects that have a specific tag on them."), selectedBrush.tagFilter);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                so.ApplyModifiedProperties();
            }
        }
        public void OnDestroy() {
            SceneView.onSceneGUIDelegate -= SceneGUI;
        }
        /// <summary>
        /// Delegate that handles Scene GUI events
        /// </summary>
        /// <param name="sceneView"></param>
        void SceneGUI(SceneView sceneView) {

            //don't do anything if the gameobject brush window is not open
            if (!IsOpen) {
                return;
            }

            //Draw Brush in the scene view
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (currentBrushes != null && Physics.Raycast(ray, out hit)) {
                Color color = Color.cyan;
                color.a = 0.25f;
                Handles.color = color;
                Handles.DrawSolidArc(hit.point, hit.normal, Vector3.Cross(hit.normal, ray.direction), 360, GetMaximumBrushSizeFromCurrentBrushes());
                Handles.color = Color.white;
                Handles.DrawLine(hit.point, hit.point + hit.normal * 5);
            }

            //Check for the currently selected tool
            if (Tools.current != Tool.View) {
                //check if the bursh is used
                if (Event.current.rawType == EventType.MouseDown) {
                    //check used mouse button
                    if (Event.current.button == 0 && PlaceObjects()) {
                        Event.current.Use();
                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    }
                    //check used mouse button
                    if (Event.current.button == 1) {
                        if (RemoveObjects()) {
                            Event.current.Use();
                        }
                    }
                }
                //check if the bursh is used
                if (Event.current.rawType == EventType.MouseDrag) {
                    //check used mouse button
                    if (Event.current.button == 0 && PlaceObjects()) {
                        Event.current.Use();
                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);
                    }
                    //check used mouse button
                    if (Event.current.button == 1) {
                        if (RemoveObjects()) {
                            Event.current.Use();
                        }
                    }
                }
            }
        }

        #region GO Brush functionality methods


        /// <summary>
        /// Places the objects
        /// returns true if objects were placed, false otherwise
        /// </summary>
        private bool PlaceObjects() {
            //only paint if painting is ebanled
            if (!isPlacingEnabled) {
                return false;
            }

            bool hasPlacedObjects = false;

            foreach (BrushObject brush in currentBrushes) {

                //loop as long as we have not reached the max ammount of objects to spawn per call/brush usage (calculated by currentBrush.density * currentBrush.brushSize)
                int spawnCount = Mathf.RoundToInt(brush.density * brush.brushSize);
                if (spawnCount < 1) {
                    spawnCount = 1;
                }

                for (int i = 0; i < spawnCount; i++) {

                    //create gameobjects of the given type if possible
                    if (brush.brushObject != null && IsOpen) {

                        //raycast from the scene camera to find the position of the brush and create objects there
                        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        ray.origin += new Vector3(Random.Range(-brush.brushSize, brush.brushSize), Random.Range(-brush.brushSize, brush.brushSize), Random.Range(-brush.brushSize, brush.brushSize));
                        Vector3 startPoint = ray.origin;
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit)) {

                            //return if we are hitting an object that we have just spawned or don't if allowIntercollisionPlacement is enabled on the current brush
                            if (spawnedObjects.Contains(hit.collider.gameObject) && !brush.allowIntercollision) {
                                continue;
                            }

                            //calculate the angle and abort if it is not in the specified range/filter
                            float angle = Vector3.Angle(Vector3.up, hit.normal);
                            if (angle < brush.minSlope || angle > brush.maxSlope) {
                                continue;
                            }

                            //check if the layer of the hit object is in our layermask filter
                            if (brush.layerFilter != (brush.layerFilter | (1 << hit.transform.gameObject.layer))) {
                                continue;
                            }

                            //check if tag filtering is active, if so check the tags
                            if (brush.isTagFilteringEnabled && hit.transform.tag != brush.tagFilter) {
                                continue;
                            }

                            //randomize position
                            Vector3 position = hit.point + brush.offsetFromPivot;

                            //instantiate object
                            GameObject obj = Instantiate(brush.brushObject, position, Quaternion.identity);
                            hasPlacedObjects = true;

                            //register created objects to the undo stack
                            Undo.RegisterCreatedObjectUndo(obj, "Created " + obj.name + " with brush");

                            //check if we should align the object to the surface we are "painting" on
                            if (brush.alignToSurface) {
                                obj.transform.up = hit.normal;
                            }

                            //Randomize rotation
                            Vector3 rot = brush.rotOffsetFromPivot;
                            if (brush.randomizeXRotation)
                                rot.x = Random.Range(0, 360);
                            if (brush.randomizeYRotation)
                                rot.y = Random.Range(0, 360);
                            if (brush.randomizeZRotation)
                                rot.z = Random.Range(0, 360);

                            //apply rotation
                            obj.transform.Rotate(rot, Space.Self);

                            //randomize scale
                            float scale = Random.Range(brush.minScale, brush.maxScale);
                            obj.transform.localScale = new Vector3(scale, scale, scale);

                            //Add object to list so it can be removed later on
                            spawnedObjects.Add(obj);
                        }
                    }
                }
            }

            return hasPlacedObjects;
        }
        /// <summary>
        /// remove objects that are in the brush radius around the brush.
        /// It returns true if it removed something, false otherwise
        /// </summary>
        private bool RemoveObjects() {

            //return if erasing is disabled
            if (!isErasingEnabled) {
                return false;
            }

            bool hasRemovedSomething = false;

            foreach (BrushObject brush in currentBrushes) {
                //raycast to fin brush position
                RaycastHit hit;
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                List<GameObject> objsToRemove = new List<GameObject>();
                if (Physics.Raycast(ray, out hit)) {

                    //loop over all spawned objects to find objects thar can be removed
                    foreach (GameObject obj in spawnedObjects) {
                        if (obj != null && Vector3.Distance(obj.transform.position, hit.point) < brush.brushSize) {
                            objsToRemove.Add(obj);
                        }
                    }

                    //delete the before found objects
                    foreach (GameObject obj in objsToRemove) {
                        spawnedObjects.Remove(obj);
                        DestroyImmediate(obj);
                        hasRemovedSomething = true;
                    }
                    objsToRemove.Clear();
                }
            }

            return hasRemovedSomething;
        }

        /// <summary>
        /// Applies all currently spawned objects, so they can not be removed by the brush
        /// </summary>
        private void ApplyMeshedPermanently() {
            spawnedObjects = new List<GameObject>();
        }
        /// <summary>
        /// Removes all spawned gameobjects that can be modified by the brush
        /// </summary>
        private void RemoveAllSpawnedObjects() {
            foreach (GameObject obj in spawnedObjects) {
                DestroyImmediate(obj);
            }
            spawnedObjects.Clear();
        }


        #endregion

        #region Misc


        /// <summary>
        /// Iterates over the list of current brushes and adds the name of each brush to a string.
        /// </summary>
        /// <returns></returns>
        private string GetCurrentBrushesString() {
            string brushes = "";
            foreach (BrushObject brush in currentBrushes) {
                if (brushes != "") {
                    brushes += " ,";
                }
                brushes += brush.brushObject.name;
            }
            return brushes;
        }
        /// <summary>
        /// Get the greatest brush size value from the current brushes list
        /// </summary>
        /// <returns></returns>
        private float GetMaximumBrushSizeFromCurrentBrushes() {
            float maxBrushSize = 0f;
            foreach (BrushObject brush in currentBrushes) {
                if (brush.brushSize > maxBrushSize) {
                    maxBrushSize = brush.brushSize;
                }
            }
            return maxBrushSize;
        }
        
        
        #endregion
    }


    /// <summary>
    /// Class that is responsible for holding information about a brush, such as the prefab/gameobject, size, density, etc.
    /// </summary>
    [System.Serializable]
    public class BrushObject {
        public GameObject brushObject;

        public bool allowIntercollision = false;
        public bool alignToSurface = false;
        [Tooltip("Should the rotation be randomized on the x axis?")] public bool randomizeXRotation = false;
        [Tooltip("Should the rotation be randomized on the y axis?")] public bool randomizeYRotation = true;
        [Tooltip("Should the rotation be randomized on the z axis?")] public bool randomizeZRotation = false;
        [Range(0, 1)] public float density = 1f;
        [Range(0, 100)] public float brushSize = 5f;
        [Range(0, 10)] public float minScale = 0.5f;
        [Range(0, 10)] public float maxScale = 1.5f;
        [Tooltip("The offset applied to the pivot of the brushObject. This is usefull if you find that the placed GameObjects are floating/sticking in the ground too much.")] public Vector3 offsetFromPivot = Vector3.zero;
        [Tooltip("The offset applied to the rotation of the brushObject.")] public Vector3 rotOffsetFromPivot = Vector3.zero;


        /* filters */
        [Range(0, 360)] public float minSlope = 0f;
        [Range(0, 360)] public float maxSlope = 360f;
        public LayerMask layerFilter = ~0;

        public bool isTagFilteringEnabled = false;
        public string tagFilter = "";


        public BrushObject(GameObject obj) {
            this.brushObject = obj;
        }

        /// <summary>
        /// Pastes the details from another brush
        /// </summary>
        public void PasteDetails(BrushObject brush) {
            if (brush != null) {
                allowIntercollision = brush.allowIntercollision;
                alignToSurface = brush.alignToSurface;
                randomizeXRotation = brush.randomizeXRotation;
                randomizeYRotation = brush.randomizeYRotation;
                randomizeZRotation = brush.randomizeZRotation;
                density = brush.density;
                brushSize = brush.brushSize;
                minScale = brush.minScale;
                maxScale = brush.maxScale;
                offsetFromPivot = brush.offsetFromPivot;
                rotOffsetFromPivot = brush.rotOffsetFromPivot;
            }
        }
        /// <summary>
        /// Pastes the filters from another brush
        /// </summary>
        public void PasteFilters(BrushObject brush) {
            if (brush != null) {
                minSlope = brush.minSlope;
                maxSlope = brush.maxSlope;
                layerFilter = brush.layerFilter;
                isTagFilteringEnabled = brush.isTagFilteringEnabled;
                tagFilter = brush.tagFilter;
            }
        }

        /// <summary>
        /// Resets the filters on this bursh
        /// </summary>
        public void ResetFilters() {
            minSlope = 0f;
            maxSlope = 360f;
            layerFilter = ~0;
            isTagFilteringEnabled = false;
            tagFilter = "";
        }
        /// <summary>
        /// Resets the details of this brush
        /// </summary>
        public void ResetDetails() {
            allowIntercollision = false;
            alignToSurface = false;
            randomizeXRotation = false;
            randomizeYRotation = true;
            randomizeZRotation = false;
            density = 1f;
            brushSize = 5f;
            minScale = 0.5f;
            maxScale = 1.5f;
            offsetFromPivot = Vector3.zero;
            rotOffsetFromPivot = Vector3.zero;
        }
    }
}
