using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace RigidPlacement
{
    public class RigidPlacementWindow : EditorWindow
    {
        // Rects
        private Rect mainPanelRect;
        private Rect maxIterationsLabelRect;
        private Rect maxIterationsFieldRect;
        private Rect forceLabelRect;
        private Rect forceFieldRect;
        private Rect randomizeAngleRect;
        private Rect angleLabelRect;
        private Rect angleFieldRect;
        private Rect includeNonRigidbodyRect;
        private Rect buttonGroupLabelRect;
        private Rect buttonGroupRect;
        private Rect showSimulationRect;
        private Rect simulateButtonRect;
        private Rect resetButtonRect;
        private Rect statusLabelRect;
        private Rect statusLabelFieldRect;
        private Rect scrollViewRect;

        // Strings
        private readonly string maxIterationsText = "Max Iterations:";
        private readonly string forceText = "Min|Max Force:";
        private readonly string randomAngleText = "Randomize Angle";
        private readonly string angleText = "Force Angle (Degrees):";
        private readonly string showSimText = "Show Simulation";
        private readonly string simulateText = "Simulate";
        private readonly string resetText = "Reset Bodies";
        private readonly string statusText = "Status:";

        // Editor Settings
        private readonly float characterWidth = 8f;
        private readonly float textFieldHeight = 16f;

        // State
        private readonly List<SimulatedBody> bodies = new();
        private readonly List<VirtualParent> nonRigidbodies = new();
        private readonly List<VirtualSimulatedBody> virtualBodies = new();
        private string maxIterationsString;
        private int maxIterations;
        private string forceMinString;
        private string forceMaxString;
        private string angleString;
        private bool randomizeAngle = false;
        private bool includeNonRigidbodies = false;
        private bool isAddPressed = false;
        private bool isRemovedPressed = false;
        private bool isClearPressed = false;
        private bool isShowSimulationChecked = false;
        private bool isSimulatedPressed = false;
        private bool isResetPressed = false;
        private string statusString;
        private Vector2 overviewScrollPosition;
        private readonly List<Rigidbody> simulatedBodies = new();
        private readonly List<SimulatedBody> unaffectedBodies = new();
        private SimulationMode simMode = SimulationMode.FixedUpdate;
        private bool simulate = false;
        private int simStep = 0;

        private readonly struct SimulatedBody
        {
            public readonly Rigidbody Rigidbody;
            public readonly Vector3 OriginalPosition;
            public readonly Quaternion OriginalRotation;
            public readonly int Hash;

            public SimulatedBody(Rigidbody rBody, Vector3 pos, Quaternion rot)
            {
                this.Rigidbody = rBody;
                this.OriginalPosition = pos;
                this.OriginalRotation = rot;
                this.Hash = HashCode.Combine(Rigidbody, OriginalPosition, OriginalRotation);
            }

            public readonly void Reset()
            {
                if (Rigidbody != null)
                {
                    Rigidbody.velocity = Vector3.zero;
                    Rigidbody.angularVelocity = Vector3.zero;

                    Rigidbody.gameObject.transform.position = OriginalPosition;
                    Rigidbody.gameObject.transform.rotation = OriginalRotation;
                }
            }
        }

        private readonly struct VirtualSimulatedBody
        {
            public readonly GameObject Parent;
            public readonly Vector3 OriginalPosition;
            public readonly Quaternion OriginalRotation;
            public readonly int ParentHash;
            public readonly int Hash;

            public VirtualSimulatedBody(GameObject parent, Vector3 pos, Quaternion rot, int parentHash)
            {
                this.Parent = parent;
                this.OriginalPosition = pos;
                this.OriginalRotation = rot;
                this.ParentHash = parentHash;
                this.Hash = HashCode.Combine(Parent, OriginalPosition, OriginalRotation, ParentHash);
            }

            public readonly void Reset()
            {
                if (Parent != null)
                {
                    Parent.transform.position = OriginalPosition;
                    Parent.transform.rotation = OriginalRotation;
                }
            }
        }

        private readonly struct VirtualParent
        {
            public readonly GameObject Target;
            public readonly int Hash;

            public VirtualParent(GameObject parent)
            {
                this.Target = parent;
                this.Hash = HashCode.Combine(parent, parent.GetInstanceID());
            }
        }

        [MenuItem("Window/Rigid Placement")]
        public static void ShowWindow()
        {
            GetWindow(typeof(RigidPlacementWindow), false, "Rigid Placement");
        }

        public RigidPlacementWindow()
        {
            // setting up strings
            // the editor breaks if any used string starts out as null
            maxIterationsString = "1000";
            forceMinString = "0";
            forceMaxString = "0";
            angleString = "0";
            statusString = ". . .";
        }

        private void OnHierarchyChange() => ValidateBodies();

        private void OnGUI()
        {
            mainPanelRect = new(new Vector2(0f, 0f), new Vector2(position.size.x, position.size.y));

            HandleMaxIterationsField();
            HandleMinMaxForce();
            HandleForceAngle();
            HandleIncludeNonRigidbodyField();
            HandleButtons();
            HandleStatusText();
            HandleOverview();

            if (this.isAddPressed) HandleAdd();
            if (this.isRemovedPressed) HandleRemove();
            if (this.isClearPressed) HandleClear();

            if (this.isSimulatedPressed) HandleSimulate();
            else if (this.isResetPressed)
            {
                HandleReset();
            }
        }

        /// <summary>Validates all currently tracked bodies and removes invalid ones.</summary>
        private void ValidateBodies()
        {
            int difference = 0;
            if (bodies.Count > 0)
            {
                for (int i = bodies.Count - 1; i > -1; i--)
                {
                    if (bodies[i].Rigidbody == null)
                    {
                        bodies.RemoveAt(i);
                        difference++;
                    }
                }
                if (difference > 0)
                {
                    statusString = difference switch
                    {
                        1 => $"{difference} entry invalidated.",
                        _ => $"{difference} entries invalidated.",
                    };
                }
            }

            if (!includeNonRigidbodies)
            {
                nonRigidbodies.Clear();
                virtualBodies.Clear();
            }
            else
            {
                if (nonRigidbodies.Count > 0)
                {
                    for (int i = nonRigidbodies.Count - 1; i > -1; i--)
                    {
                        if (nonRigidbodies[i].Target == null)
                        {
                            virtualBodies.RemoveAll(x => x.ParentHash == nonRigidbodies[i].Hash);
                            nonRigidbodies.RemoveAt(i);
                        }
                    }
                }
            }
        }

        private void HandleMaxIterationsField()
        {
            maxIterationsLabelRect = new(
                new Vector2((characterWidth * 0.5f) + mainPanelRect.xMin, mainPanelRect.yMin + 4f),
                new Vector2(maxIterationsText.Length * characterWidth, textFieldHeight));
            maxIterationsFieldRect = new(
                new Vector2(maxIterationsLabelRect.xMax - 12f, maxIterationsLabelRect.yMin),
                new Vector2(mainPanelRect.xMax - (maxIterationsLabelRect.width + characterWidth), textFieldHeight));

            GUI.Label(maxIterationsLabelRect, maxIterationsText, EditorStyles.boldLabel);
            string current = GUI.TextField(maxIterationsFieldRect, maxIterationsString);
            maxIterationsString = ValidateNumberField(current);
        }

        private void HandleMinMaxForce()
        {
            float horizontalOffset = characterWidth * 0.5f;
            float verticalOffset = textFieldHeight * 0.5f;

            forceLabelRect = new(
                new Vector2(horizontalOffset + mainPanelRect.xMin, maxIterationsLabelRect.yMax + verticalOffset),
                new Vector2(mainPanelRect.xMax - horizontalOffset, textFieldHeight));

            forceFieldRect = new(
                new Vector2(forceText.Length * characterWidth, forceLabelRect.yMin),
                new Vector2(forceText.Length * characterWidth, textFieldHeight + 8));

            GUI.Label(forceLabelRect, forceText, EditorStyles.boldLabel);
            GUILayout.BeginArea(forceFieldRect);
            GUILayout.BeginHorizontal();
            string tempForceMinString = GUILayout.TextField(forceMinString);
            string tempForceMaxString = GUILayout.TextField(forceMaxString);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            forceMinString = ValidateNumberField(tempForceMinString);
            forceMaxString = ValidateNumberField(tempForceMaxString);
        }

        private void HandleForceAngle()
        {
            float horizontalOffset = characterWidth * 0.5f;
            float verticalOffset = textFieldHeight * 0.5f;

            randomizeAngleRect = new(
                new Vector2(horizontalOffset + mainPanelRect.xMin, forceLabelRect.yMax + verticalOffset),
                new Vector2(randomAngleText.Length * characterWidth, textFieldHeight));

            angleLabelRect = new(
                new Vector2(horizontalOffset + mainPanelRect.xMin, randomizeAngleRect.yMax + verticalOffset),
                new Vector2(angleText.Length * characterWidth, textFieldHeight));

            angleFieldRect = new(
                new Vector2(angleLabelRect.xMax, angleLabelRect.yMin),
                new Vector2(characterWidth * 6, textFieldHeight));

            randomizeAngle = GUI.Toggle(randomizeAngleRect, randomizeAngle, randomAngleText, EditorStyles.toggle);
            GUI.Label(angleLabelRect, angleText, EditorStyles.boldLabel);
            if (randomizeAngle)
            {
                GUI.Label(angleFieldRect, "Randomized", EditorStyles.label);
            }
            else
            {
                string temp = GUI.TextField(angleFieldRect, angleString);
                angleString = ValidateNumberField(temp);
            }
        }

        private void HandleIncludeNonRigidbodyField()
        {
            float horizontalOffset = characterWidth * 0.5f;
            float verticalOffset = textFieldHeight * 0.5f;

            includeNonRigidbodyRect = new(
                new Vector2(horizontalOffset + mainPanelRect.xMin, angleLabelRect.yMax + verticalOffset),
                new Vector2(mainPanelRect.xMax - horizontalOffset, textFieldHeight));

            bool tempState = GUI.Toggle(includeNonRigidbodyRect, includeNonRigidbodies, "Include Non-Rigidbodies", EditorStyles.toggle);
            if (tempState != includeNonRigidbodies)
            {
                includeNonRigidbodies = tempState;
                ValidateBodies();
            }
        }

        private void HandleButtons()
        {
            float horizontalOffset = characterWidth * 0.5f;
            float verticalOffset = textFieldHeight * 0.5f;

            buttonGroupLabelRect = new(
                new Vector2(horizontalOffset + mainPanelRect.xMin, includeNonRigidbodyRect.yMax + verticalOffset),
                new Vector2(mainPanelRect.xMax - horizontalOffset, textFieldHeight));
            GUI.Label(buttonGroupLabelRect, "Add/Remove/Clear SimulatedBodies", EditorStyles.boldLabel);

            buttonGroupRect = new(
                new Vector2(horizontalOffset + mainPanelRect.xMin, buttonGroupLabelRect.yMax + verticalOffset),
                new Vector2(mainPanelRect.xMax - characterWidth, textFieldHeight));

            GUI.enabled = !simulate;
            GUILayout.BeginArea(buttonGroupRect);
            GUILayout.BeginHorizontal();
            isAddPressed = GUILayout.Button("Add");
            isRemovedPressed = GUILayout.Button("Remove");
            isClearPressed = GUILayout.Button("Clear");
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.enabled = simulate;

            showSimulationRect = new(
                new Vector2(buttonGroupRect.xMin, buttonGroupRect.yMax + verticalOffset),
                new Vector2(showSimText.Length * characterWidth, textFieldHeight));
            simulateButtonRect = new(
                new Vector2(showSimulationRect.xMax, showSimulationRect.yMin),
                new Vector2(mainPanelRect.size.x - (showSimulationRect.xMax + characterWidth), textFieldHeight));
            resetButtonRect = new(
                new Vector2(buttonGroupRect.xMin, simulateButtonRect.yMax + verticalOffset),
                new Vector2(mainPanelRect.size.x - characterWidth, textFieldHeight));

            GUI.enabled = !simulate;
            isShowSimulationChecked = GUI.Toggle(showSimulationRect, isShowSimulationChecked, showSimText);
            isSimulatedPressed = GUI.Button(simulateButtonRect, simulateText, EditorStyles.miniButton);
            isResetPressed = GUI.Button(resetButtonRect, resetText, EditorStyles.miniButton);
            GUI.enabled = simulate;
        }

        private void HandleStatusText()
        {
            statusLabelRect = new(
                new Vector2(resetButtonRect.xMin, resetButtonRect.yMax + (textFieldHeight * 0.5f)),
                new Vector2(statusText.Length * characterWidth, textFieldHeight));
            statusLabelFieldRect = new(
                new Vector2(statusLabelRect.xMax, statusLabelRect.yMin),
                new Vector2(statusString.Length * characterWidth, textFieldHeight));

            GUI.Label(statusLabelRect, statusText, EditorStyles.boldLabel);
            GUI.Label(statusLabelFieldRect, statusString, EditorStyles.label);
        }

        private void HandleOverview()
        {
            float aggregatedItemHeight = ((bodies.Count + nonRigidbodies.Count) - 2) * textFieldHeight;
            float probe = mainPanelRect.height - statusLabelFieldRect.yMax;
            float padding = textFieldHeight * 0.75f;
            float adjustableHeight = aggregatedItemHeight < probe ?
                probe - padding :
                (probe - aggregatedItemHeight) + aggregatedItemHeight - padding;

            scrollViewRect = new(
                new Vector2(statusLabelRect.xMin, statusLabelFieldRect.yMax + (textFieldHeight * 0.5f)),
                new Vector2(mainPanelRect.width - characterWidth, adjustableHeight));
            GUI.enabled = !simulate;
            GUILayout.BeginArea(scrollViewRect);
            overviewScrollPosition = GUILayout.BeginScrollView(overviewScrollPosition, GUILayout.Width(scrollViewRect.width));
            foreach (SimulatedBody body in bodies)
            {
                EditorGUILayout.ObjectField(obj: body.Rigidbody.gameObject, objType: typeof(GameObject), allowSceneObjects: true);
            }
            if (includeNonRigidbodies)
            {
                foreach (GameObject go in nonRigidbodies.Select(x => x.Target))
                {
                    EditorGUILayout.ObjectField(obj: go, objType: typeof(GameObject), allowSceneObjects: true);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.enabled = simulate;
        }

        private void HandleAdd()
        {
            List<GameObject> list = GetSelection();
            IEnumerable<Rigidbody> simulatedBodies = bodies.Select(x => x.Rigidbody);
            int addedBodies = 0;
            foreach (GameObject go in list)
            {
                if (go.TryGetComponent(out Rigidbody body))
                {
                    if (simulatedBodies.Contains(body)) continue;
                    bodies.Add(new(body, body.position, body.rotation));
                    addedBodies++;
                }
                else if (includeNonRigidbodies) addedBodies++;
            }
            statusString = $"Added {addedBodies} new bodies ({bodies.Count + nonRigidbodies.Count} total)";
        }

        private void HandleRemove()
        {
            int nonRigidbodyProbe = nonRigidbodies.Count;
            List<GameObject> list = GetSelection();
            if (bodies.Count == 0 && nonRigidbodyProbe == 0)
            {
                nonRigidbodies.Clear();
                statusString = "No bodies to remove.";
            }
            else if (list.Count > 0)
            {
                int removedBodies = 0;
                if (bodies.Count > 0)
                {
                    for (int i = bodies.Count - 1; i > -1; i--)
                    {
                        if (list.Contains(bodies[i].Rigidbody.gameObject))
                        {
                            bodies.RemoveAt(i);
                            removedBodies++;
                        }
                    }
                }
                if (includeNonRigidbodies)
                {
                    if (nonRigidbodies.Count > 0)
                    {
                        for (int i = nonRigidbodies.Count - 1; i > -1; i--)
                        {
                            if (list.Contains(nonRigidbodies[i].Target))
                            {
                                virtualBodies.RemoveAll(x => x.Parent == nonRigidbodies[i].Target);
                                nonRigidbodies.RemoveAt(i);
                                removedBodies++;
                            }
                        }
                    }
                }
                statusString = $"Removed {removedBodies} bodies ({bodies.Count + nonRigidbodies.Count} total)";
            }
        }

        private void HandleClear()
        {
            bodies.Clear();
            virtualBodies.Clear();
            nonRigidbodies.Clear();
            statusString = "Cleared out all bodies.";
        }

        private void HandleSimulate()
        {
            if (bodies.Count == 0 && !includeNonRigidbodies)
            {
                statusString = "No bodies to simulate.";
            }
            else if (maxIterationsString.Length == 0)
            {
                statusString = "Max Iterations must be higher than 0";
            }
            else if (int.TryParse(maxIterationsString, out maxIterations))
            {
                simulatedBodies.Clear();
                if (bodies.Count > 0)
                {
                    // Update position and rotation to make sure that the new
                    // simulation starts where the bodies are now compare to
                    // where they were when they were first recorded
                    for (int i = bodies.Count - 1; i > -1; i--)
                    {
                        if (bodies[i].Rigidbody != null)
                        {
                            bodies[i] = new(bodies[i].Rigidbody, bodies[i].Rigidbody.position, bodies[i].Rigidbody.rotation);
                            simulatedBodies.Add(bodies[i].Rigidbody);
                        }
                        else
                        {
                            bodies.RemoveAt(i);
                        }
                    }
                }

                List<Rigidbody> virtualBodies = new();
                if (nonRigidbodies.Count > 0)
                {
                    // Update position and rotation to make sure that the new
                    // simulation starts where the bodies are now compare to
                    // where they were when they were first recorded
                    for (int i = nonRigidbodies.Count - 1; i > -1; i--)
                    {
                        MeshCollider collider = nonRigidbodies[i].Target.AddComponent<MeshCollider>();
                        collider.convex = true;
                        Rigidbody rb = nonRigidbodies[i].Target.AddComponent<Rigidbody>();

                        virtualBodies.Add(rb);
                        GameObject parent = nonRigidbodies[i].Target;
                        this.virtualBodies.Add(new(parent, parent.transform.position, parent.transform.rotation, nonRigidbodies[i].Hash));
                    }
                }

                if (simulatedBodies.Count > 0 || virtualBodies.Count > 0)
                {
                    StartSimulation();
                }
                else
                {
                    statusString = "No bodies to simulate.";
                }
            }
            else
            {
                statusString = "Max Iterations Format Error.";
            }
        }

        private void StartSimulation()
        {
            // Find all bodies not to be simulated and note their position and rotation
            // so they can be reset after the simulation is done.
            statusString = "Simulating...";
            unaffectedBodies.Clear();
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            simulatedBodies.AddRange(virtualBodies.Select(x => x.Parent.GetComponent<Rigidbody>()));
            foreach (GameObject go in allObjects)
            {
                if (go.TryGetComponent(out Rigidbody body))
                {
                    if (!simulatedBodies.Contains(body))
                    {
                        unaffectedBodies.Add(new(body, body.position, body.rotation));
                    }
                }
            }

            // Randomly Generate Forces
            float minForce = string.IsNullOrEmpty(forceMinString) ? 0f : float.Parse(forceMinString);
            float maxForce = string.IsNullOrEmpty(forceMaxString) ? 0f : float.Parse(forceMaxString);
            float forceRad;
            if (randomizeAngle)
            {
                forceRad = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            }
            else
            {
                forceRad = string.IsNullOrEmpty(angleString) ? 0f : Mathf.Clamp(float.Parse(angleString), 0f, 360f) * Mathf.Deg2Rad;
            }

            // Apply forces
            foreach (Rigidbody body in simulatedBodies)
            {
                float randomForce = UnityEngine.Random.Range(minForce, maxForce);
                Vector3 forceDir = new(Mathf.Sin(forceRad), 0, Mathf.Cos(forceRad));
                body.AddForce(forceDir * randomForce, ForceMode.Impulse);
            }
            // Store previous mode to reset it after simulation
            simMode = Physics.simulationMode;
            // Script mode necessary for Editor script to run Physics.Simulate();
            Physics.simulationMode = SimulationMode.Script;
            Physics.autoSyncTransforms = false;
            if (isShowSimulationChecked)
            {
                simulate = true;
                simStep = 0;
            }
            else
            {
                for (int i = 0; i < maxIterations; i++)
                {
                    Physics.Simulate(Time.fixedDeltaTime);
                    ResetAllBodies(unaffectedBodies);
                    if (simulatedBodies.All(rb => rb.IsSleeping()))
                    {
                        statusString = $"Done simulating in {i + 1} iterations.";
                        break;
                    }
                }
                PostSimulate();
            }
        }

        private void Update()
        {
            if (simulate)
            {
                SceneView.RepaintAll();
                if (simStep >= maxIterations)
                {
                    statusString = $"Done simulating in {maxIterations} iterations.";
                    PostSimulate();
                }
                else
                {
                    Physics.Simulate(Time.fixedDeltaTime);
                    ResetAllBodies(unaffectedBodies);
                    if (simulatedBodies.All(rb => rb.IsSleeping()))
                    {
                        statusString = $"Done simulating in {simStep + 1} iterations.";
                        PostSimulate();
                    }
                }
                simStep++;
            }
        }

        private void PostSimulate()
        {
            Physics.autoSyncTransforms = true;
            Physics.simulationMode = simMode;
            ResetAllBodies(unaffectedBodies);

            simulatedBodies.Clear();
            for (int i = 0; i < nonRigidbodies.Count; i++)
            {
                DestroyImmediate(nonRigidbodies[i].Target.GetComponent<MeshCollider>());
                DestroyImmediate(nonRigidbodies[i].Target.GetComponent<Rigidbody>());
            }
            simulate = false;
            Repaint();
        }

        private List<GameObject> GetSelection()
        {
            GameObject[] selections = Selection.gameObjects;
            List<GameObject> result = new();
            IEnumerable<GameObject> targets = nonRigidbodies.Select(x => x.Target);
            foreach (GameObject selection in selections)
            {
                if (selection.TryGetComponent(out Rigidbody _))
                {
                    result.Add(selection);
                }
                else if (includeNonRigidbodies)
                {
                    result.Add(selection);
                    if (!targets.Contains(selection))
                    {
                        nonRigidbodies.Add(new(selection));
                    }
                }
            }
            return result;
        }

        private void HandleReset()
        {
            ResetAllBodies(bodies);
            ResetAllBodies(virtualBodies);
            virtualBodies.Clear();
        }

        private string ValidateNumberField(string text)
        {
            StringBuilder sb = new(string.Empty);
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsNumber(text[i])) sb.Append(text[i]);
            }
            return sb.ToString();
        }

        // Reset the position and rotation of all passed bodies
        private void ResetAllBodies(List<SimulatedBody> list)
        {
            foreach (SimulatedBody body in list) body.Reset();
        }

        // Reset the position and rotation of all passed bodies
        private void ResetAllBodies(List<VirtualSimulatedBody> list)
        {
            foreach (VirtualSimulatedBody body in list) body.Reset();
        }
    }
}