using System.Collections.Generic;
using UnityEngine;

namespace Tessera
{
    // Extension to ICellType containing various methods used for drawing / interacting with the cells in the UI
    public interface ICellDrawingType
    {
        Vector3[] GetSubFaceVertices(Vector3Int offset, FaceDir dir, SubFace subface, Vector3 center, Vector3 tileSize);

        Vector3[] GetFaceVertices(Vector3Int offset, FaceDir dir, Vector3 center, Vector3 tileSize);

        void GetFaceCenterAndNormal(Vector3Int offset, FaceDir dir, Vector3 center, Vector3 tileSize, out Vector3 faceCenter, out Vector3 faceNormal);

        RaycastCellHit Raycast(Ray ray, Vector3Int offset, Vector3 center, Vector3 tileSize, float? minDistance, float? maxDistance);

        SubFace RoundSubFace(Vector3Int offset, FaceDir dir, Vector2 subface, PaintMode paintMode);

        bool IsAffected(Vector3Int parentOffset, FaceDir parentFaceDir, SubFace parentSubface, Vector3Int childOffset, FaceDir childFaceDir, SubFace childSubface, PaintMode paintMode);

        IEnumerable<SubFace> GetSubFaces(Vector3Int offset, FaceDir dir);

        void SetSubFaceValue(FaceDetails faceDetails, SubFace subface, int color);

        int GetSubFaceValue(FaceDetails faceDetails, SubFace subface);

        Vector3Int Move(Vector3Int offset, FaceDir dir);
    }

    public enum SubFace
    {
        // Square subfaces
        BottomLeft,
        Bottom,
        BottomRight,
        Left,
        Center,
        Right,
        TopLeft,
        Top,
        TopRight,

        // Clockwise from the the subface at the center of the edge facing right.
        // XAndY means adjacent to the vertex joining those two sides
        HexRight,
        HexRightAndTopRight,
        HexTopRight,
        HexTopRightAndTopLeft,
        HexTopLeft,
        HexTopLeftAndLeft,
        HexLeft,
        HexLeftAndBottomLeft,
        HexBottomLeft,
        HexBottomLeftAndBottomRight,
        HexBottomRight,
        HexBottomRightAndRight,
        HexCenter
    }

    public class RaycastCellHit
    {
        public FaceDir dir;
        public Vector3 point;
        // Varies from -1 to 1
        public Vector2 subface;
    }
}