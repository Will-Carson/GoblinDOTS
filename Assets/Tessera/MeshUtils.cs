using DeBroglie.Rot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tessera
{

    public static class MeshUtils
    {
        // Creates an axis aligned cube that corresponds with a box collider
        private static Mesh CreateBoxMesh(Vector3 center, Vector3 size)
        {
            Vector3[] vertices = {
                new Vector3 (-0.5f, -0.5f, -0.5f),
                new Vector3 (+0.5f, -0.5f, -0.5f),
                new Vector3 (+0.5f, +0.5f, -0.5f),
                new Vector3 (-0.5f, +0.5f, -0.5f),
                new Vector3 (-0.5f, +0.5f, +0.5f),
                new Vector3 (+0.5f, +0.5f, +0.5f),
                new Vector3 (+0.5f, -0.5f, +0.5f),
                new Vector3 (-0.5f, -0.5f, +0.5f),
            };
            vertices = vertices.Select(v => center + Vector3.Scale(size, v)).ToArray();
            int[] triangles = {
                0, 2, 1,
	            0, 3, 2,
                2, 3, 4,
	            2, 4, 5,
                1, 2, 5,
	            1, 5, 6,
                0, 7, 4,
	            0, 4, 3,
                5, 4, 7,
	            5, 7, 6,
                0, 6, 7,
	            0, 1, 6
            };

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            return mesh;
        }

        /// <summary>
        /// Applies Transform gameObject and its children.
        /// Components affected:
        /// * MeshFilter
        /// * MeshColldier
        /// * BoxCollider
        /// </summary>
        public static void TransformRecursively(GameObject gameObject, MeshDeformation meshDeformation)
        {
            foreach (var child in gameObject.GetComponentsInChildren<MeshFilter>())
            {
                var childDeformation = (child.transform.worldToLocalMatrix * gameObject.transform.localToWorldMatrix) * meshDeformation * (gameObject.transform.worldToLocalMatrix * child.transform.localToWorldMatrix);
                if (!child.sharedMesh.isReadable) continue;
                var mesh = childDeformation.Deform(child.sharedMesh);
                mesh.hideFlags = HideFlags.HideAndDontSave;
                child.mesh = mesh;
            }
            foreach (var child in gameObject.GetComponentsInChildren<Collider>())
            {
                var childDeformation = (child.transform.worldToLocalMatrix * gameObject.transform.localToWorldMatrix) * meshDeformation * (gameObject.transform.worldToLocalMatrix * child.transform.localToWorldMatrix);
                if (child is MeshCollider meshCollider)
                {
                    meshCollider.sharedMesh = childDeformation.Deform(meshCollider.sharedMesh);
                }
                else if(child is BoxCollider boxCollider)
                {
                    // Convert box colliders to mesh colliders.
                    var childGo = child.gameObject;
                    var newMeshCollider = childGo.AddComponent<MeshCollider>();
                    newMeshCollider.enabled = child.enabled;
                    newMeshCollider.hideFlags = child.hideFlags;
                    newMeshCollider.isTrigger = child.isTrigger;
                    newMeshCollider.sharedMaterial = child.sharedMaterial;
                    newMeshCollider.name = child.name;
                    newMeshCollider.convex = false;// Cannot be sure of this
                    var mesh = CreateBoxMesh(boxCollider.center, boxCollider.size);
                    mesh.hideFlags = HideFlags.HideAndDontSave;
                    newMeshCollider.sharedMesh = childDeformation.Deform(mesh);
                    if (Application.isPlaying)
                    {
                        GameObject.Destroy(child);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(child);
                    }
                }
                else
                {
                    Debug.LogWarning($"Collider {child} is not a type Tessera supports deforming onto a mesh.");

                }
            }
        }

        /// <summary>
        /// Extracts the appropriate point transformation for a given instance from tile space to generator space
        /// </summary>
        public static MeshDeformation GetDeformation(TesseraGenerator generator, TesseraTileInstance i)
        {
            return GetDeformation(generator.surfaceMesh, generator.tileSize.y, generator.surfaceOffset, generator.surfaceSmoothNormals, i);
        }

        /// <summary>
        /// Matrix that transforms from tile local co-ordinates to a unit centered cube, mapping the cube at the given offset to the unit cube.
        /// </summary>
        public static Matrix4x4 TileToCube(TesseraTile tile, Vector3Int offset)
        {
            var translate = Matrix4x4.Translate(-tile.center - Vector3.Scale(offset, tile.tileSize));
            var scale = Matrix4x4.Scale(new Vector3(1.0f / tile.tileSize.x, 1.0f / tile.tileSize.y, 1.0f / tile.tileSize.z));
            return scale * translate;
        }

        /// <summary>
        /// Deforms from tile local space to the surface of the mesh
        /// </summary>
        public static MeshDeformation GetDeformation(Mesh surfaceMesh, float tileHeight, float surfaceOffset, bool smoothNormals, TesseraTileInstance i)
        {
            if (i.Cells.Count() == 1)
            {
                var cell = i.Cells.First();
                var offset = i.Tile.offsets.First();

                return GetDeformation(surfaceMesh, tileHeight, surfaceOffset, smoothNormals, i, cell, offset);
            }
            else
            {
                // For big tiles, we need to load the transform for every cell the tile covers
                // and apply the correct one

                var initialTransformsByOffset = new Dictionary<Vector3Int, MeshDeformation>();
                var deformationsByOffset = new Dictionary<Vector3Int, MeshDeformation>();
                foreach (var (cell, offset) in i.Cells.Zip(i.Tile.offsets, Tuple.Create))
                {
                    deformationsByOffset[offset] = initialTransformsByOffset[offset] = GetDeformation(surfaceMesh, tileHeight, surfaceOffset, smoothNormals, i, cell, offset);
                }
                MeshDeformation GetNearest(Vector3 v)
                {
                    var v2 = v - i.Tile.center;
                    var offset = new Vector3Int(
                        (int)Math.Round(v2.x / i.Tile.tileSize.x),
                        (int)Math.Round(v2.y / i.Tile.tileSize.y),
                        (int)Math.Round(v2.z / i.Tile.tileSize.z)
                        );
                    if (deformationsByOffset.TryGetValue(offset, out var nearest))
                    {
                        return nearest;
                    }
                    var nearestKv = initialTransformsByOffset
                        .OrderBy(kv => (kv.Key - offset).sqrMagnitude)
                        .First();
                    nearest = deformationsByOffset[offset] = nearestKv.Value;
                    return nearest;
                }
                Vector3 DeformPoint(Vector3 p)
                {
                    return GetNearest(p).DeformPoint(p);
                }
                Vector3 DeformNormal(Vector3 p, Vector3 v)
                {
                    return GetNearest(p).DeformNormal(p, v);
                }
                Vector4 DeformTangent(Vector3 p, Vector4 t)
                {
                    return GetNearest(p).InnerDeformTangent(p, t);
                }
                return new MeshDeformation(DeformPoint, DeformNormal, DeformTangent, !deformationsByOffset.First().Value.InvertWinding);
            }
        }

        /// <summary>
        /// Deforms from tile local space to the suface of the mesh, based on a particular offset
        /// </summary>
        private static MeshDeformation GetDeformation(Mesh surfaceMesh, float tileHeight, float surfaceOffset, bool smoothNormals, TesseraTileInstance i, Vector3Int cell, Vector3Int offset)
        {
            var meshDeformation = GetDeformation(surfaceMesh, tileHeight, surfaceOffset, smoothNormals, cell.x, cell.y, cell.z);

            var tileToCube = TileToCube(i.Tile, offset);
            var tileMatrix = Matrix4x4.Rotate(i.TileRotation) * Matrix4x4.Scale(i.TileScale);

            return meshDeformation * tileMatrix * tileToCube;
        }


        /// <summary>
        /// Transforms from a unit cube centered on the origin to the surface of the mesh
        /// </summary>
        public static MeshDeformation GetDeformation(Mesh surfaceMesh, float tileHeight, float sufaceOffset, bool smoothNormals, int face, int layer, int subMesh)
        {

            var trilinearInterpolatePoint = TrilinearInterpolate(surfaceMesh, subMesh, face, tileHeight * layer + sufaceOffset - tileHeight / 2, tileHeight * layer + sufaceOffset + tileHeight / 2);

            var trilinearInterpolateNormal = TrilinearInterpolateNormal(surfaceMesh, subMesh, face, tileHeight * layer + sufaceOffset - tileHeight / 2, tileHeight * layer + sufaceOffset + tileHeight / 2);

            var trilinearInterpolateTangent = TrilinearInterpolateTangent(surfaceMesh, subMesh, face, tileHeight * layer + sufaceOffset - tileHeight / 2, tileHeight * layer + sufaceOffset + tileHeight / 2);

            var trilinearInterpolateUv = smoothNormals ? TrilinearInterpolateUv(surfaceMesh, subMesh, face, tileHeight * layer + sufaceOffset - tileHeight / 2, tileHeight * layer + sufaceOffset + tileHeight / 2) : null;

            Vector3 DeformNormal(Vector3 p, Vector3 v)
            {
                var m = 1e-3f;

                // TODO: Do some actual differentation
                var t = trilinearInterpolatePoint(p);
                var dx = (trilinearInterpolatePoint(p + Vector3.right * m) - t) / m;
                var dy = (trilinearInterpolatePoint(p + Vector3.up * m) - t) / m;
                var dz = (trilinearInterpolatePoint(p + Vector3.forward * m) - t) / m;

                if (!smoothNormals)
                {
                    var jacobi = new Matrix4x4(dx, dy, dz, new Vector4(0, 0, 0, 1));
                    return jacobi.inverse.transpose.MultiplyVector(v).normalized;
                }
                else
                {
                    // If you want normals that are continuous on the boundary between cells,
                    // we cannot use the actual jacobi matrix (above) as it is discontinuous.

                    // The same problem comes up for uv interpolation, which is why many meshes
                    // come with a precalculated tangent field for bump mapping etc.

                    // We can re-use that pre-computation by calculating the difference between
                    // the naive uv jacobi and the one given by the tangents, and then
                    // applying that to interpolation jacobi

                    // This code is not 100% correct, but it seems to give acceptable results.
                    // TODO: Do we really need all the normalization?


                    var normal = trilinearInterpolateNormal(p).normalized;
                    var tangent = trilinearInterpolateTangent(p).normalized;
                    var bitangent = (tangent.w * Vector3.Cross(normal, tangent)).normalized;

                    // TODO: Do some actual differentation
                    var t2 = trilinearInterpolateUv(p);
                    var dx2 = (trilinearInterpolateUv(p + Vector3.right * m) - t2) / m;
                    //var dy2 = (trilinearInterpolateUv(p + Vector3.up * m) - t2) / m;// Always zero
                    var dz2 = (trilinearInterpolateUv(p + Vector3.forward * m) - t2) / m;

                    var j3 = new Matrix4x4(
                        -new Vector3(dx2.x, 0, dx2.y).normalized,
                        new Vector3(0, 1, 0),
                        new Vector3(dz2.x, 0, dz2.y).normalized,
                        new Vector4(0, 0, 0, 1)
                        );

                    var j1 = new Matrix4x4(-((Vector3)tangent) * dx.magnitude, normal * dy.magnitude, bitangent * dz.magnitude, new Vector4(0, 0, 0, 1));

                    var jacobi = j3 * j1;

                    return jacobi.inverse.transpose.MultiplyVector(v).normalized;
                }
            }

            Vector4 DeformTangent(Vector3 p, Vector4 v)
            {
                // TODO: Do some actual differentation
                var m = 1e-2f;

                var t = trilinearInterpolatePoint(p);
                var dx = (trilinearInterpolatePoint(p + Vector3.right * m) - t) / m;
                var dy = (trilinearInterpolatePoint(p + Vector3.up * m) - t) / m;
                var dz = (trilinearInterpolatePoint(p + Vector3.forward * m) - t) / m;

                // See DeformNormal
                var normal = trilinearInterpolateNormal(p).normalized * dy.magnitude;
                var tangent = trilinearInterpolateTangent(p).normalized * dx.magnitude;
                var bitangent = (tangent.w * Vector3.Cross(normal, tangent)).normalized * dz.magnitude;

                // TODO: Support smoothNormals?
                var jacobi = new Matrix4x4(-((Vector3)tangent), normal, bitangent, new Vector4(0, 0, 0, 1));

                var v2 = jacobi.MultiplyVector(v).normalized;
                return new Vector4(v2.x, v2.y, v2.z, v.w);
            }

            return new MeshDeformation(trilinearInterpolatePoint, DeformNormal, DeformTangent, false);
        }

        /// <summary>
        /// Sets up a function that does trilinear interpolation from a unit cube centered on the origin
        /// to a cube made by extruding a given face of the mesh by meshOffset1 (for y=-0.5) and meshOffset2 (for y=0.5)
        /// </summary>
        public static Func<Vector3, Vector3> TrilinearInterpolate(Mesh mesh, int submesh, int face, float meshOffset1, float meshOffset2)
        {
            if (mesh.GetTopology(submesh) != MeshTopology.Quads)
            {
                throw new Exception($"Mesh topology {mesh.GetTopology(submesh)} not supported. You need to select \"Keep Quads\"");
            }

            var indices = mesh.GetIndices(submesh);
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var i1 = indices[face * 4 + 0];
            var i2 = indices[face * 4 + 1];
            var i3 = indices[face * 4 + 2];
            var i4 = indices[face * 4 + 3];
            // Find new bounding cage

            var v1 = vertices[i1] + normals[i1] * meshOffset1;
            var v2 = vertices[i2] + normals[i2] * meshOffset1;
            var v3 = vertices[i3] + normals[i3] * meshOffset1;
            var v4 = vertices[i4] + normals[i4] * meshOffset1;
            var v5 = vertices[i1] + normals[i1] * meshOffset2;
            var v6 = vertices[i2] + normals[i2] * meshOffset2;
            var v7 = vertices[i3] + normals[i3] * meshOffset2;
            var v8 = vertices[i4] + normals[i4] * meshOffset2;

            return TrilinearInterpolate(v1, v2, v3, v4, v5, v6, v7, v8);
        }

        private static Func<Vector3, Vector3> TrilinearInterpolateNormal(Mesh mesh, int submesh, int face, float meshOffset1, float meshOffset2)
        {
            if (mesh.GetTopology(submesh) != MeshTopology.Quads)
            {
                throw new Exception($"Mesh topology {mesh.GetTopology(submesh)} not supported. You need to select \"Keep Quads\"");
            }

            var indices = mesh.GetIndices(submesh);
            var normals = mesh.normals;

            var i1 = indices[face * 4 + 0];
            var i2 = indices[face * 4 + 1];
            var i3 = indices[face * 4 + 2];
            var i4 = indices[face * 4 + 3];
            // Find new bounding cage

            var v1 = normals[i1];
            var v2 = normals[i2];
            var v3 = normals[i3];
            var v4 = normals[i4];
            var v5 = normals[i1];
            var v6 = normals[i2];
            var v7 = normals[i3];
            var v8 = normals[i4];

            return TrilinearInterpolate(v1, v2, v3, v4, v5, v6, v7, v8);
        }

        private static Func<Vector3, Vector4> TrilinearInterpolateTangent(Mesh mesh, int submesh, int face, float meshOffset1, float meshOffset2)
        {
            if (mesh.GetTopology(submesh) != MeshTopology.Quads)
            {
                throw new Exception($"Mesh topology {mesh.GetTopology(submesh)} not supported. You need to select \"Keep Quads\" in the import options.");
            }
            //if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
            if (mesh.tangents.Length == 0)
            {
                throw new Exception($"Mesh needs tangents to calculate smoothed normals. You need to select \"Calculate\" the tangent field of the import options.");
            }

            var tangents = mesh.tangents;

            var indices = mesh.GetIndices(submesh);
            var i1 = indices[face * 4 + 0];
            var i2 = indices[face * 4 + 1];
            var i3 = indices[face * 4 + 2];
            var i4 = indices[face * 4 + 3];
            // Find new bounding cage

            var v1 = tangents[i1]; 
            var v2 = tangents[i2];
            var v3 = tangents[i3];
            var v4 = tangents[i4];
            var v5 = tangents[i1];
            var v6 = tangents[i2];
            var v7 = tangents[i3];
            var v8 = tangents[i4];

            // TODO: Bilienar interpolate
            return TrilinearInterpolate(v1, v2, v3, v4, v5, v6, v7, v8);
        }


        private static Func<Vector3, Vector2> TrilinearInterpolateUv(Mesh mesh, int submesh, int face, float meshOffset1, float meshOffset2)
        {
            if (mesh.GetTopology(submesh) != MeshTopology.Quads)
            {
                throw new Exception($"Mesh topology {mesh.GetTopology(submesh)} not supported. You need to select \"Keep Quads\" in the import options.");
            }
            //if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Tangent))
            if (mesh.tangents.Length == 0)
            {
                throw new Exception($"Mesh needs tangents to calculate smoothed normals. You need to select \"Calculate\" the tangent field of the import options.");
            }

            var uvs = mesh.uv;

            var indices = mesh.GetIndices(submesh);
            var i1 = indices[face * 4 + 0];
            var i2 = indices[face * 4 + 1];
            var i3 = indices[face * 4 + 2];
            var i4 = indices[face * 4 + 3];
            // Find new bounding cage

            var v1 = uvs[i1];
            var v2 = uvs[i2];
            var v3 = uvs[i3];
            var v4 = uvs[i4];
            var v5 = uvs[i1];
            var v6 = uvs[i2];
            var v7 = uvs[i3];
            var v8 = uvs[i4];


            // TODO: Bilienar interpolate
            return TrilinearInterpolate(v1, v2, v3, v4, v5, v6, v7, v8);
        }

        public static Func<Vector3, Vector2> TrilinearInterpolate(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4, Vector2 v5, Vector2 v6, Vector2 v7, Vector2 v8)
        {
            Vector2 TrilinearInterpolatePoint(Vector3 p)
            {
                //Perform linear interpolation.
                Vector2 Interp(float t, Vector2 a, Vector2 b)
                {
                    t = t + 0.5f;
                    return (1 - t) * a + t * b;
                }
                // Linear interpolate on each axis in turn
                var u1 = Interp(p.z, v1, v2);
                var u2 = Interp(p.z, v4, v3);
                var u3 = Interp(p.z, v5, v6);
                var u4 = Interp(p.z, v8, v7);
                var w1 = Interp(-p.x, u1, u2);
                var w2 = Interp(-p.x, u3, u4);
                var z = Interp(p.y, w1, w2);
                return z;
            }

            return TrilinearInterpolatePoint;
        }

        public static Func<Vector3, Vector3> TrilinearInterpolate(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6, Vector3 v7, Vector3 v8)
        {
            Vector3 TrilinearInterpolatePoint(Vector3 p)
            {
                //Perform linear interpolation.
                Vector3 Interp(float t, Vector3 a, Vector3 b)
                {
                    t = t + 0.5f;
                    return (1 - t) * a + t * b;
                }
                // Linear interpolate on each axis in turn
                var u1 = Interp(p.z, v1, v2);
                var u2 = Interp(p.z, v4, v3);
                var u3 = Interp(p.z, v5, v6);
                var u4 = Interp(p.z, v8, v7);
                var w1 = Interp(-p.x, u1, u2);
                var w2 = Interp(-p.x, u3, u4);
                var z = Interp(p.y, w1, w2);
                return z;
            }

            return TrilinearInterpolatePoint;
        }

        public static Func<Vector3, Vector4> TrilinearInterpolate(Vector4 v1, Vector4 v2, Vector4 v3, Vector4 v4, Vector4 v5, Vector4 v6, Vector4 v7, Vector4 v8)
        {
            Vector4 TrilinearInterpolatePoint(Vector3 p)
            {
                //Perform linear interpolation.
                Vector4 Interp(float t, Vector4 a, Vector4 b)
                {
                    t = t + 0.5f;
                    return (1 - t) * a + t * b;
                }
                // Linear interpolate on each axis in turn
                var u1 = Interp(p.z, v1, v2);
                var u2 = Interp(p.z, v4, v3);
                var u3 = Interp(p.z, v5, v6);
                var u4 = Interp(p.z, v8, v7);
                var w1 = Interp(-p.x, u1, u2);
                var w2 = Interp(-p.x, u3, u4);
                var z = Interp(p.y, w1, w2);
                return z;
            }

            return TrilinearInterpolatePoint;
        }
    }
}
