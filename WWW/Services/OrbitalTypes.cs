namespace WWW.Services
{
    public readonly struct Vector3D
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double Norm() => Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vector3D Normalize()
        {
            var n = Norm();
            return n > 0 ? new Vector3D(X / n, Y / n, Z / n) : new Vector3D(0, 0, 0);
        }

        public static Vector3D operator +(Vector3D a, Vector3D b)
            => new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3D operator -(Vector3D a, Vector3D b)
            => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector3D operator *(double s, Vector3D v)
            => new Vector3D(s * v.X, s * v.Y, s * v.Z);

        public static Vector3D operator *(Vector3D v, double s)
            => new Vector3D(s * v.X, s * v.Y, s * v.Z);
    }

    public sealed class OrbitalState
    {
        public Vector3D Position_m { get; }
        public Vector3D Velocity_m_s { get; }
        public double JulianDate { get; }

        public OrbitalState(Vector3D position_m, Vector3D velocity_m_s, double jd)
        {
            Position_m = position_m;
            Velocity_m_s = velocity_m_s;
            JulianDate = jd;
        }
    }

    public sealed class OrbitalElements
    {
        public double A_AU { get; }
        public double Eccentricity { get; }
        public double Inclination_deg { get; }
        public double AscendingNode_deg { get; }
        public double ArgumentPerihelion_deg { get; }
        public double MeanAnomalyEpoch_deg { get; }
        public double Epoch_JD { get; }

        public OrbitalElements(
            double a_AU,
            double e,
            double inc_deg,
            double om_deg,
            double w_deg,
            double M0_deg,
            double epoch_JD)
        {
            A_AU = a_AU;
            Eccentricity = e;
            Inclination_deg = inc_deg;
            AscendingNode_deg = om_deg;
            ArgumentPerihelion_deg = w_deg;
            MeanAnomalyEpoch_deg = M0_deg;
            Epoch_JD = epoch_JD;
        }
    }
}
