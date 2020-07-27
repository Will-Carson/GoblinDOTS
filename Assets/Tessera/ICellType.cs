using DeBroglie.Rot;
using DeBroglie.Topo;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tessera
{
    // Contains details about the shape of a cell
    // This is responsible for several things:
    // * Drawing / interacting with a cell in the UI
    // * A topology like interface for dealing with offsets and big tiles. 
    //     The topology uses offsets and facedirs instead of co-ordinates and directions to distinguish it. We call this "tile local space" as opposed to "generator space"
    // * Interpretation of Direction and Rotation
    public interface ICellType
    {
        // Rotaiton handling

        IEnumerable<FaceDir> GetFaceDirs();

        IEnumerable<Direction> GetDirections();

        IEnumerable<(Direction, Direction)> GetDirectionPairs();

        Direction Invert(Direction d);

        RotationGroup GetRotationGroup();

        Direction Rotate(FaceDir faceDir, Rotation rotation);

        (Direction, FaceDetails) RotateBy(FaceDir faceDir, FaceDetails faceDetails, Rotation rot);

        (Direction, FaceDetails) ApplyRotator(FaceDir faceDir, FaceDetails faceDetails, object rotator);

        // Offset topology

        bool TryMove(Vector3Int offset, FaceDir dir, out Vector3Int dest);

        IEnumerable<FaceDir> FindPath(Vector3Int startOffset, Vector3Int endOffset);

        Vector3 GetCellCenter(Vector3Int offset, Vector3 center, Vector3 tileSize);
    }
}
