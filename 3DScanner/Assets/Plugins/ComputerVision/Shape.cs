using UnityEngine;
namespace UnityScanner3D.ComputerVision
{
    /// <summary>
    /// Defines the possible shape types
    /// </summary>
    public enum ShapeType
    {
        Cube,
        Cylinder
    }

    /// <summary>
    /// A struct for describing a shaped detected by an <see cref="IAlgorithm"/>
    /// </summary>
    public struct Shape
    {
        /// <summary>
        /// Specifies the type of the shape (e.g. Cube, Arch, Cylinder, etc.)
        /// </summary>
        public ShapeType Type { get; set; }

        /// <summary>
        /// Specifies the 3D translation to perform in the Unity Scene
        /// </summary>
        public Vector3 Translation { get; set; }
        /// <summary>
        /// Specifies the rotation for Unity to apply to the shape 
        /// </summary>
        public Quaternion Rotation { get; set; }
    }
}
