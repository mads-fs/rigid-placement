using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private Rect includeNonRigidbodyRect;
        private Rect buttonGroupLabelRect;
        private Rect buttonGroupRect;
        private Rect simulateButtonRect;
        private Rect resetButtonRect;
        private Rect statusLabelRect;
        private Rect statusLabelFieldRect;
        private Rect scrollViewRect;

        // Strings
        private readonly string maxIterationsText = "Max Iterations:";
        private readonly string simulateText = "Simulate";
        private readonly string resetText = "Reset Bodies";
        private readonly string statusText = "Status:";

        // Editor Settings
        private readonly float characterWidth = 8f;
        private readonly float textFieldHeight = 16f;

        // State
        private readonly List<SimulatedBody> bodies = new();
        private readonly List<GameObject> nonRigidbodies = new();
        private readonly List<SimulatedBody> tempBodies = new();
        private string maxIterationsString;
        private bool includeNonRigidbodies = false;
        private bool isAddPressed = false;
        private bool isRemovedPressed = false;
        private bool isClearPressed = false;
        private bool isSimulatedPressed = false;
        private bool isResetPressed = false;
        private string statusString;
        private Vector2 overviewScrollPosition;

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

        [MenuItem("Window/Rigid Placement")]
        public static void ShowWindow()
        {
            GetWindow(typeof(RigidPlacementWindow), false, "Rigid Placement");
        }

        public RigidPlacementWindow()
        {
            maxIterationsString = "1000";
            statusString = ". . .";
        }

        private void OnHierarchyChange() => ValidateBodies();

        private void OnGUI()
        {
            mainPanelRect = new(new Vector2(0f, 0f), new Vector2(position.size.x, position.size.y));

            HandleMaxIterationsField();
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
                if (tempBodies.Count > 0)
                {
                    for (int i = tempBodies.Count - 1; i > -1; i--)
                    {
                        if (tempBodies[i].Rigidbody == null) continue;
                        DestroyImmediate(tempBodies[i].Rigidbody.gameObject.GetComponent<MeshCollider>());
                        DestroyImmediate(tempBodies[i].Rigidbody.gameObject.GetComponent<Rigidbody>());
                    }
                    tempBodies.Clear();
                }
                nonRigidbodies.Clear();
            }
        }

        private void HandleMaxIterationsField()
        {
            maxIterationsLabelRect = new(
                new Vector2((characterWidth * 0.5f) + mainPanelRect.xMin, mainPanelRect.yMin + 4f),
                new Vector2(maxIterationsText.Length * characterWidth, textFieldHeight));
            maxIterationsFieldRect = new(
                new Vector2(maxIterationsLabelRect.xMax, maxIterationsLabelRect.yMin),
                new Vector2(mainPanelRect.xMax - (maxIterationsLabelRect.width + characterWidth), textFieldHeight));

            GUI.Label(maxIterationsLabelRect, maxIterationsText, EditorStyles.boldLabel);
            string current = GUI.TextField(maxIterationsFieldRect, maxIterationsString, EditorStyles.numberField);
            maxIterationsString = string.Empty;
            StringBuilder sb = new();
            for (int i = 0; i < current.Length; i++)
            {
                if (char.IsNumber(current[i])) sb.Append(current[i]);
            }
            maxIterationsString = sb.ToString();
        }

        private void HandleIncludeNonRigidbodyField()
        {
            float horizontalOffset = characterWidth * 0.5f;
            float verticalOffset = textFieldHeight * 0.5f;

            includeNonRigidbodyRect = new(
                new Vector2(horizontalOffset + mainPanelRect.xMin, maxIterationsLabelRect.yMax + verticalOffset),
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
            GUILayout.BeginArea(buttonGroupRect);
            GUILayout.BeginHorizontal();
            isAddPressed = GUILayout.Button("Add");
            isRemovedPressed = GUILayout.Button("Remove");
            isClearPressed = GUILayout.Button("Clear");
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            simulateButtonRect = new(
                new Vector2(buttonGroupRect.xMin, buttonGroupRect.yMax + verticalOffset),
                new Vector2(mainPanelRect.size.x - characterWidth, textFieldHeight));
            resetButtonRect = new(
                new Vector2(simulateButtonRect.xMin, simulateButtonRect.yMax + verticalOffset),
                new Vector2(mainPanelRect.size.x - characterWidth, textFieldHeight));

            isSimulatedPressed = GUI.Button(simulateButtonRect, simulateText, EditorStyles.miniButton);
            isResetPressed = GUI.Button(resetButtonRect, resetText, EditorStyles.miniButton);
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
            scrollViewRect = new(
                new Vector2(statusLabelRect.xMin, statusLabelFieldRect.yMax + (textFieldHeight * 0.5f)),
                new Vector2(mainPanelRect.width - characterWidth, mainPanelRect.height - (textFieldHeight)));
            GUILayout.BeginArea(scrollViewRect);
            overviewScrollPosition = GUILayout.BeginScrollView(overviewScrollPosition, GUILayout.Width(scrollViewRect.width), GUILayout.Height(mainPanelRect.height));
            foreach (SimulatedBody body in bodies)
            {
                EditorGUILayout.ObjectField(obj: body.Rigidbody.gameObject, objType: typeof(GameObject), allowSceneObjects: true);
            }
            if (includeNonRigidbodies)
            {
                foreach (GameObject go in nonRigidbodies)
                {
                    EditorGUILayout.ObjectField(obj: go, objType: typeof(GameObject), allowSceneObjects: true);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
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
            statusString = $"Added {addedBodies} new bodies ({bodies.Count} total)";
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
                for (int i = bodies.Count - 1; i > -1; i--)
                {
                    if (list.Contains(bodies[i].Rigidbody.gameObject))
                    {
                        bodies.RemoveAt(i);
                        removedBodies++;
                    }
                }
                if (includeNonRigidbodies)
                {
                    if (nonRigidbodies.Count > 0)
                    {
                        for (int i = nonRigidbodies.Count - 1; i > -1; i--)
                        {
                            if (list.Contains(nonRigidbodies[i]))
                            {
                                GameObject go = nonRigidbodies[i];
                                if (go.TryGetComponent(out Rigidbody body))
                                {
                                    SimulatedBody sb = tempBodies.First(x => x.Rigidbody == body);
                                    DestroyImmediate(sb.Rigidbody.gameObject.GetComponent<MeshCollider>());
                                    DestroyImmediate(sb.Rigidbody.gameObject.GetComponent<Rigidbody>());
                                    tempBodies.RemoveAll(x => sb.Hash == x.Hash);
                                }
                                nonRigidbodies.RemoveAll(x => x == go);
                                removedBodies++;
                            }
                        }
                    }
                    if (nonRigidbodies.Count == 0) tempBodies.Clear();
                }
                statusString = $"Removed {removedBodies} bodies ({bodies.Count + nonRigidbodies.Count} total)";
            }
        }

        private void HandleClear()
        {
            bodies.Clear();
            if (includeNonRigidbodies)
            {
                if (tempBodies.Count > 0)
                {
                    for (int i = tempBodies.Count - 1; i > -1; i--)
                    {
                        DestroyImmediate(tempBodies[i].Rigidbody.gameObject.GetComponent<MeshCollider>());
                        DestroyImmediate(tempBodies[i].Rigidbody.gameObject.GetComponent<Rigidbody>());
                    }
                }
                tempBodies.Clear();
                nonRigidbodies.Clear();
            }
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
            else if (int.TryParse(maxIterationsString, out int iterations))
            {
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
                        }
                        else
                        {
                            bodies.RemoveAt(i);
                        }
                    }
                }

                if (bodies.Count > 0 || (includeNonRigidbodies && nonRigidbodies.Count > 0))
                {
                    // Find all bodies not to be simulated and note their position and rotation
                    // so they can be reset after the simulation is done.
                    List<SimulatedBody> unaffectedBodies = new();
                    GameObject[] allObjects = FindObjectsOfType<GameObject>();
                    List<Rigidbody> simulatedBodies = bodies.Select(x => x.Rigidbody).ToList();
                    simulatedBodies.AddRange(tempBodies.Select(x => x.Rigidbody));
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

                    if (includeNonRigidbodies)
                    {
                        allObjects = allObjects.Except(unaffectedBodies.Select(x => x.Rigidbody.gameObject).ToArray()).ToArray();
                        foreach (GameObject go in allObjects)
                        {
                            if (nonRigidbodies.Contains(go))
                            {
                                if (go.GetComponent<Rigidbody>() == null)
                                {
                                    Rigidbody body = go.AddComponent<Rigidbody>();
                                    MeshCollider collider = go.AddComponent<MeshCollider>();
                                    collider.convex = true;
                                    tempBodies.Add(new(body, body.position, body.rotation));
                                }
                            }
                        }
                    }

                    // Store previous mode to reset it after simulation
                    SimulationMode simMode = Physics.simulationMode;
                    // Script mode necessary for Editor script to run Physics.Simulate();
                    Physics.simulationMode = SimulationMode.Script;
                    Physics.autoSyncTransforms = false;
                    for (int i = 0; i < iterations; i++)
                    {
                        Physics.Simulate(Time.fixedDeltaTime);
                        ResetAllBodies(unaffectedBodies);
                        if (bodies.All(rb => rb.Rigidbody.IsSleeping()) && tempBodies.All(rb => rb.Rigidbody.IsSleeping()))
                        {
                            statusString = $"Done simulating in {i} iterations.";
                            break;
                        }
                    }
                    Physics.autoSyncTransforms = true;
                    Physics.simulationMode = simMode;
                    ResetAllBodies(unaffectedBodies);
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

        private List<GameObject> GetSelection()
        {
            GameObject[] selections = Selection.gameObjects;
            List<GameObject> result = new();
            foreach (GameObject selection in selections)
            {
                if (selection.TryGetComponent(out Rigidbody _))
                {
                    result.Add(selection);
                }
                else if (includeNonRigidbodies)
                {
                    result.Add(selection);
                    if (!nonRigidbodies.Contains(selection))
                    {
                        nonRigidbodies.Add(selection);
                    }
                }
            }
            return result;
        }

        private void HandleReset()
        {
            ResetAllBodies(bodies);
            ResetAllBodies(tempBodies);
        }

        // Reset the position and rotation of all passed bodies
        private void ResetAllBodies(List<SimulatedBody> list)
        {
            foreach (SimulatedBody body in list) body.Reset();
        }
    }
}