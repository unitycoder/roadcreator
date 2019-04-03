﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[HelpURL("https://github.com/MCrafterzz/roadcreator/wiki/Prefab-Lines")]
public class PrefabLineCreator : MonoBehaviour
{

    public GameObject prefab;
    public GameObject startPrefab;
    public GameObject endPrefab;

    public enum YModification { none, matchTerrain, matchCurve };
    public YModification yModification;
    public float terrainCheckHeight = 1;

    public bool bendObjects = true;
    public float bendMultiplier = 1;
    public bool fillGap = true;
    public float spacing = -1;
    public bool rotateAlongCurve = true;

    public enum RotationDirection { forward, backward, left, right };
    public RotationDirection rotationDirection = RotationDirection.left;
    public float yRotationRandomization;

    public float scale = 1;
    public float pointCalculationDivisions = 100;

    public GameObject objectToMove;
    private bool mouseDown;

    public GlobalSettings globalSettings;

    public void UndoUpdate()
    {
        PlacePrefabs();
    }

    public void CreatePoints(Vector3 hitPosition)
    {
        if (transform.GetChild(0).childCount > 0 && transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).name == "Point")
        {
            if (globalSettings.roadCurved == true)
            {
                Undo.RegisterCreatedObjectUndo(CreatePoint("Control Point", hitPosition), "Create Point");
            }
            else
            {
                Undo.RegisterCreatedObjectUndo(CreatePoint("Control Point", Misc.GetCenter(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).position, hitPosition)), "Create Point");
                Undo.RegisterCreatedObjectUndo(CreatePoint("Point", hitPosition), "Create Point");
                PlacePrefabs();
            }
        }
        else
        {
            Undo.RegisterCreatedObjectUndo(CreatePoint("Point", hitPosition), "Create Point");
            PlacePrefabs();
        }
    }

    public GameObject CreatePoint(string name, Vector3 raycastHit)
    {
        GameObject point = new GameObject(name);
        point.AddComponent<BoxCollider>();
        point.GetComponent<BoxCollider>().size = new Vector3(globalSettings.pointSize, globalSettings.pointSize, globalSettings.pointSize);
        point.GetComponent<BoxCollider>().hideFlags = HideFlags.NotEditable;
        point.transform.SetParent(transform.GetChild(0));
        point.transform.position = raycastHit;
        point.layer = globalSettings.ignoreMouseRayLayer;
        return point;
    }

    public void MovePoints(Vector3 hitPosition, Event guiEvent, RaycastHit raycastHit)
    {
        if (hitPosition == Misc.MaxVector3)
        {
            if (objectToMove != null)
            {
                mouseDown = false;
                objectToMove.GetComponent<BoxCollider>().enabled = true;
                objectToMove = null;
                PlacePrefabs();
            }
        }
        else
        {
            if (mouseDown == true && objectToMove != null)
            {
                if (guiEvent.keyCode == KeyCode.Plus || guiEvent.keyCode == KeyCode.KeypadPlus)
                {
                    Undo.RecordObject(objectToMove.transform, "Moved Point");
                    objectToMove.transform.position += new Vector3(0, 0.2f, 0);

                    if (guiEvent.control == true)
                    {
                        objectToMove.transform.position = new Vector3(objectToMove.transform.position.x, Mathf.Ceil(objectToMove.transform.position.y), objectToMove.transform.position.z);
                    }
                }
                else if (guiEvent.keyCode == KeyCode.Minus || guiEvent.keyCode == KeyCode.KeypadMinus)
                {
                    Vector3 position = objectToMove.transform.position - new Vector3(0, 0.2f, 0);

                    if (guiEvent.control == true)
                    {
                        position = new Vector3(position.x, Mathf.Floor(position.y), position.z);
                    }

                    if (position.y < raycastHit.point.y)
                    {
                        position.y = raycastHit.point.y;
                    }

                    Undo.RecordObject(objectToMove.transform, "Moved Point");
                    objectToMove.transform.position = position;
                }
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && objectToMove == null)
            {
                mouseDown = true;

                if (raycastHit.transform.name.Contains("Point") && raycastHit.collider.transform.parent.parent.GetComponent<PrefabLineCreator>() != null && raycastHit.collider.transform.parent.parent.gameObject == gameObject)
                {
                    objectToMove = raycastHit.collider.gameObject;
                    objectToMove.GetComponent<BoxCollider>().enabled = false;
                }
            }
            else if (guiEvent.type == EventType.MouseDrag && objectToMove != null)
            {
                Undo.RecordObject(objectToMove.transform, "Moved Point");
                objectToMove.transform.position = hitPosition;
            }
            else if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0 && objectToMove != null)
            {
                mouseDown = false;
                objectToMove.GetComponent<BoxCollider>().enabled = true;
                objectToMove = null;
                PlacePrefabs();
            }
        }
    }

    public void RemovePoints(bool removeTwo = false)
    {
        if (transform.GetChild(0).childCount > 0)
        {
            Undo.DestroyObjectImmediate(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).gameObject);

            if (removeTwo == true && transform.GetChild(0).childCount > 0)
            {
                Undo.DestroyObjectImmediate(transform.GetChild(0).GetChild(transform.GetChild(0).childCount - 1).gameObject);
            }

            PlacePrefabs();
        }
    }

    public void PlacePrefabs()
    {
        if (prefab == null)
        {
            prefab = Resources.Load("Prefabs/Low Poly/Concrete Barrier") as GameObject;
        }

        for (int i = transform.GetChild(1).childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(1).GetChild(i).gameObject);
        }

        if (fillGap == true && rotationDirection != PrefabLineCreator.RotationDirection.left && rotationDirection != PrefabLineCreator.RotationDirection.right)
        {
            rotationDirection = PrefabLineCreator.RotationDirection.left;
        }

        if (transform.GetChild(0).childCount > 2)
        {
            PointPackage currentPoints = CalculatePoints();
            for (int j = 0; j < currentPoints.prefabPoints.Length; j++)
            {
                GameObject placedPrefab;

                if (j == 0 && startPrefab != null)
                {
                    placedPrefab = Instantiate(startPrefab);
                }
                else if (j == currentPoints.prefabPoints.Length - 1 && endPrefab != null)
                {
                    placedPrefab = Instantiate(endPrefab);
                }
                else
                {
                    placedPrefab = Instantiate(prefab);
                }

                placedPrefab.transform.SetParent(transform.GetChild(1));
                placedPrefab.transform.position = Misc.GetCenter(currentPoints.startPoints[j], currentPoints.endPoints[j]);
                placedPrefab.name = "Prefab";
                placedPrefab.layer = globalSettings.roadLayer;
                placedPrefab.transform.localScale = new Vector3(scale, scale, scale);
                Vector3 left = Misc.CalculateLeft(currentPoints.startPoints[j], currentPoints.endPoints[j]);
                Vector3 forward = new Vector3(left.z, 0, -left.x);

                if (rotateAlongCurve == true)
                {
                    if (rotationDirection == PrefabLineCreator.RotationDirection.forward)
                    {
                        placedPrefab.transform.rotation = Quaternion.Euler(Quaternion.LookRotation(forward).eulerAngles.x, Quaternion.LookRotation(forward).eulerAngles.y + Random.Range(-yRotationRandomization / 2, yRotationRandomization / 2), Quaternion.LookRotation(forward).eulerAngles.z);
                    }
                    else if (rotationDirection == PrefabLineCreator.RotationDirection.backward)
                    {
                        placedPrefab.transform.rotation = Quaternion.Euler(Quaternion.LookRotation(-forward).eulerAngles.x, Quaternion.LookRotation(-forward).eulerAngles.y + Random.Range(-yRotationRandomization / 2, yRotationRandomization / 2), Quaternion.LookRotation(-forward).eulerAngles.z);
                    }
                    else if (rotationDirection == PrefabLineCreator.RotationDirection.left)
                    {
                        placedPrefab.transform.rotation = Quaternion.Euler(Quaternion.LookRotation(left).eulerAngles.x, Quaternion.LookRotation(left).eulerAngles.y + Random.Range(-yRotationRandomization / 2, yRotationRandomization / 2), Quaternion.LookRotation(left).eulerAngles.z);
                    }
                    else if (rotationDirection == PrefabLineCreator.RotationDirection.right)
                    {
                        placedPrefab.transform.rotation = Quaternion.Euler(Quaternion.LookRotation(-left).eulerAngles.x, Quaternion.LookRotation(-left).eulerAngles.y + Random.Range(-yRotationRandomization / 2, yRotationRandomization / 2), Quaternion.LookRotation(-left).eulerAngles.z);
                    }
                }

                if (bendObjects == true)
                {
                    Mesh mesh = GameObject.Instantiate(placedPrefab.GetComponent<MeshFilter>().sharedMesh);
                    Vector3[] vertices = mesh.vertices;
                    float distanceToChange = Mathf.Abs((Quaternion.Euler(0, -placedPrefab.transform.rotation.eulerAngles.y, 0) * placedPrefab.transform.position).z - (Quaternion.Euler(0, -placedPrefab.transform.rotation.eulerAngles.y, 0) * currentPoints.prefabPoints[j]).z);

                    Vector3 controlPoint;
                    if (currentPoints.rotateTowardsLeft[j] == false)
                    {
                        distanceToChange = -distanceToChange;
                    }

                    controlPoint = mesh.bounds.center + new Vector3(0, 0, distanceToChange * 4);

                    for (var i = 0; i < vertices.Length; i++)
                    {
                        float distance = Mathf.Abs(vertices[i].x - mesh.bounds.min.x);
                        float distanceCovered = (distance / mesh.bounds.size.x);
                        Vector3 lerpedPoint = Misc.Lerp3(new Vector3(-mesh.bounds.extents.x, 0, 0), controlPoint, new Vector3(mesh.bounds.extents.x, 0, 0), distanceCovered);
                        vertices[i].z = vertices[i].z - (lerpedPoint).z;
                    }

                    mesh.vertices = vertices;
                    mesh.RecalculateBounds();
                    placedPrefab.GetComponent<MeshFilter>().sharedMesh = mesh;

                    // Change collider to match
                    System.Type type = placedPrefab.GetComponent<Collider>().GetType();
                    if (type != null)
                    {
                        DestroyImmediate(placedPrefab.GetComponent<Collider>());
                        placedPrefab.AddComponent(type);
                    }
                }

                if (yModification != PrefabLineCreator.YModification.none)
                {
                    Vector3[] vertices = placedPrefab.GetComponent<MeshFilter>().sharedMesh.vertices;
                    float startHeight = currentPoints.startPoints[j].y;
                    float endHeight = currentPoints.endPoints[j].y;

                    for (var i = 0; i < vertices.Length; i++)
                    {
                        if (yModification == PrefabLineCreator.YModification.matchTerrain)
                        {
                            RaycastHit raycastHit;
                            if (Physics.Raycast(placedPrefab.transform.position + (placedPrefab.transform.rotation * vertices[i] * scale) + new Vector3(0, terrainCheckHeight, 0), Vector3.down, out raycastHit, 100f, ~(1 << globalSettings.ignoreMouseRayLayer | 1 << globalSettings.roadLayer)))
                            {
                                vertices[i].y += (raycastHit.point.y - placedPrefab.transform.position.y) / scale;
                            }
                        }
                        else if (yModification == PrefabLineCreator.YModification.matchCurve)
                        {
                            float time = Misc.Remap(vertices[i].x, placedPrefab.GetComponent<MeshFilter>().sharedMesh.bounds.min.x, placedPrefab.GetComponent<MeshFilter>().sharedMesh.bounds.max.x, 0, 1);

                            if (rotationDirection == PrefabLineCreator.RotationDirection.right)
                            {
                                time = 1 - time;
                            }

                            vertices[i].y += (Mathf.Lerp(startHeight, endHeight, time) - placedPrefab.transform.position.y) / scale;
                        }
                    }

                    Mesh mesh = Instantiate(placedPrefab.GetComponent<MeshFilter>().sharedMesh);
                    mesh.vertices = vertices;
                    placedPrefab.GetComponent<MeshFilter>().sharedMesh = mesh;
                    placedPrefab.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();

                    // Change collider to match
                    System.Type type = placedPrefab.GetComponent<Collider>().GetType();
                    if (type != null)
                    {
                        DestroyImmediate(placedPrefab.GetComponent<Collider>());
                        placedPrefab.AddComponent(type);
                    }
                }

                if (fillGap == true && j > 0)
                {
                    // Add last vertices
                    List<Vector3> lastVertexPositions = new List<Vector3>();
                    Vector3[] lastVertices = transform.GetChild(1).GetChild(j - 1).GetComponent<MeshFilter>().sharedMesh.vertices;
                    for (int i = 0; i < lastVertices.Length; i++)
                    {
                        if (Mathf.Abs(lastVertices[i].x - GetMaxX()) < 0.001f)
                        {
                            lastVertexPositions.Add((transform.GetChild(1).GetChild(j - 1).transform.rotation * (scale * lastVertices[i])) + transform.GetChild(1).GetChild(j - 1).transform.position);
                        }
                    }

                    // Move current vertices to last ones
                    Mesh mesh = Instantiate(placedPrefab.GetComponent<MeshFilter>().sharedMesh);
                    Vector3[] vertices = mesh.vertices;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        if (Mathf.Abs(vertices[i].x - GetMinX()) < 0.001f)
                        {
                            Vector3 nearestVertex = Vector3.zero;
                            float currentDistance = float.MaxValue;

                            for (int k = 0; k < lastVertexPositions.Count; k++)
                            {
                                float localZ = (Quaternion.Euler(0, -(transform.GetChild(1).GetChild(j - 1).transform.rotation.eulerAngles.y), 0) * (lastVertexPositions[k] - transform.GetChild(1).GetChild(j - 1).transform.position)).z;
                                float zDifference = Mathf.Abs(localZ - (vertices[i].z * scale));
                                if (zDifference < 0.001f)
                                {
                                    if (yModification == PrefabLineCreator.YModification.none)
                                    {
                                        if (Mathf.Abs(lastVertexPositions[k].y - ((vertices[i].y * scale) + placedPrefab.transform.position.y)) < currentDistance)
                                        {
                                            nearestVertex = lastVertexPositions[k];
                                            currentDistance = Mathf.Abs(lastVertexPositions[k].y - ((vertices[i].y * scale) + placedPrefab.transform.position.y));
                                        }
                                    }
                                    else
                                    {
                                        float calculatedDistance = Vector3.Distance(lastVertexPositions[k], (placedPrefab.transform.rotation * (scale * vertices[i])) + placedPrefab.transform.position);

                                        if (calculatedDistance < currentDistance)
                                        {
                                            currentDistance = calculatedDistance;
                                            nearestVertex = lastVertexPositions[k];
                                        }
                                    }
                                }
                            }

                            if (nearestVertex != Vector3.zero)
                            {
                                float scaleModifier = 1 / scale;
                                vertices[i] = Quaternion.Euler(0, -placedPrefab.transform.rotation.eulerAngles.y, 0) * (nearestVertex - placedPrefab.transform.position) * scaleModifier;
                            }
                        }
                    }

                    mesh.vertices = vertices;
                    placedPrefab.GetComponent<MeshFilter>().sharedMesh = mesh;
                    placedPrefab.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();

                    // Change collider to match
                    System.Type type = placedPrefab.GetComponent<Collider>().GetType();
                    if (type != null)
                    {
                        DestroyImmediate(placedPrefab.GetComponent<Collider>());
                        placedPrefab.AddComponent(type);
                    }
                }
            }
        }
    }

    private float GetMaxX()
    {
        if (rotationDirection == PrefabLineCreator.RotationDirection.left)
        {
            return prefab.GetComponent<MeshFilter>().sharedMesh.bounds.max.x;
        }
        else
        {
            return prefab.GetComponent<MeshFilter>().sharedMesh.bounds.min.x;
        }
    }

    private float GetMinX()
    {
        if (rotationDirection == PrefabLineCreator.RotationDirection.left)
        {
            return prefab.GetComponent<MeshFilter>().sharedMesh.bounds.min.x;
        }
        else
        {
            return prefab.GetComponent<MeshFilter>().sharedMesh.bounds.max.x;
        }
    }

    public PointPackage CalculatePoints()
    {
        List<Vector3> prefabPoints = new List<Vector3>();
        List<Vector3> startPoints = new List<Vector3>();
        List<Vector3> endPoints = new List<Vector3>();
        List<bool> rotateTowardsLeft = new List<bool>();

        Vector3 firstPoint = transform.GetChild(0).GetChild(0).position;
        Vector3 controlPoint = transform.GetChild(0).GetChild(1).position;
        Vector3 endPoint = transform.GetChild(0).GetChild(2).position;
        float distance = Misc.CalculateDistance(firstPoint, controlPoint, endPoint);
        startPoints.Add(firstPoint);
        Vector3 lastPoint = firstPoint;
        bool endPointAdded = true;

        Vector3 currentPoint = Vector3.zero;

        for (int i = 0; i < transform.GetChild(0).childCount - 2; i += 2)
        {
            distance = Misc.CalculateDistance(firstPoint, controlPoint, endPoint);
            float divisions = distance / spacing;
            divisions = Mathf.Max(2, divisions);
            float distancePerDivision = 1 / divisions;

            firstPoint = transform.GetChild(0).GetChild(i).position;
            controlPoint = transform.GetChild(0).GetChild(i + 1).position;
            endPoint = transform.GetChild(0).GetChild(i + 2).position;

            for (float t = 0; t < 1; t += distancePerDivision / pointCalculationDivisions)
            {
                if (t > 1)
                {
                    t = 1;
                }

                currentPoint = Misc.Lerp3(firstPoint, controlPoint, endPoint, t);
                currentPoint.y = Mathf.Lerp(Mathf.Lerp(firstPoint.y, Misc.GetCenter(firstPoint.y, endPoint.y), t), endPoint.y, t);

                float currentDistance = Vector3.Distance(new Vector3(lastPoint.x, 0, lastPoint.z), new Vector3(currentPoint.x, 0, currentPoint.z));

                if (currentDistance > spacing / 2 && endPointAdded == false)
                {
                    endPoints.Add(currentPoint);
                    startPoints.Add(currentPoint);
                    endPointAdded = true;
                    rotateTowardsLeft.Add(IsSegmentLeft(startPoints[startPoints.Count - 2], prefabPoints[prefabPoints.Count - 1], endPoints[endPoints.Count - 1]));
                }

                if (endPointAdded == true && ((currentDistance > spacing) || (startPoints.Count == 1 && currentDistance > spacing / 2)))
                {
                    prefabPoints.Add(currentPoint);
                    lastPoint = currentPoint;
                    endPointAdded = false;
                }
            }

            if (endPoints.Count < prefabPoints.Count && (i + 2) >= transform.GetChild(0).childCount - 2)
            {
                endPoints.Add(currentPoint);
                rotateTowardsLeft.Add(IsSegmentLeft(startPoints[startPoints.Count - 1], prefabPoints[prefabPoints.Count - 1], endPoints[endPoints.Count - 1]));
            }
        }

        return new PointPackage(prefabPoints.ToArray(), startPoints.ToArray(), endPoints.ToArray(), rotateTowardsLeft.ToArray());
    }

    private bool IsSegmentLeft(Vector3 startPosition, Vector3 prefabPosition, Vector3 endPosition)
    {
        //(b.X - a.X)*(c.Y - a.Y) - (b.Y - a.Y)*(c.X - a.X);
        float direction = ((float)endPosition.x - startPosition.x) * ((float)prefabPosition.z - startPosition.z) - ((float)endPosition.z - startPosition.z) * ((float)prefabPosition.x - startPosition.x);

        if (direction < 0.0f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

}
