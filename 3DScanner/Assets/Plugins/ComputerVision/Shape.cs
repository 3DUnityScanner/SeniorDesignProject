namespace UnityScanner3D.ComputerVision
{
    /// <summary>
    /// Defines the possible shape types
    /// </summary>
    enum ShapeType
    {
        Cube,
        Cylinder
    }

    /// <summary>
    /// A struct for describing a shaped detected by an <see cref="IAlgorithm"/>
    /// </summary>
    struct Shape
    {
        /// <summary>
        /// Specifies the type of the shape (e.g. Cube, Arch, Cylinder, etc.)
        /// </summary>
        public ShapeType Type { get; set; }

        /// <summary>
        /// X axis position (Translational)
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y axis position (Translational)
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Z axis position (Translational)
        /// </summary>
        public double Z { get; set; }

        /// <summary>
        /// U position (Rotational)
        /// </summary>
        public double U { get; set; }

        /// <summary>
        /// V position (Rotational)
        /// </summary>
        public double V { get; set; }

        /// <summary>
        /// W position (Rotational)
        /// </summary>
        public double W { get; set; }
    }
}
