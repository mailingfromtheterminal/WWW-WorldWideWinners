using WWW.Services.Constans;

namespace WWW.Services
{
    public class OrbitalMechanicsService
    {
        public OrbitalElements GetMidasOriginalElements()
        {
            return new OrbitalElements(
                AsteroidMidasConstants.SemiMajorAxis_AU,
                AsteroidMidasConstants.Eccentricity,
                AsteroidMidasConstants.Inclination_deg,
                AsteroidMidasConstants.AscendingNode_deg,
                AsteroidMidasConstants.ArgumentPerihelion_deg,
                AsteroidMidasConstants.MeanAnomalyEpoch_deg,
                AsteroidMidasConstants.Epoch_JD
            );
        }

        /// <summary>
        /// Propaga Midas (o cualquier órbita kepleriana) hasta un JD dado
        /// y devuelve posición y velocidad en coordenadas heliocéntricas (m, m/s).
        /// </summary>
        public OrbitalState PropagateToJulianDate(OrbitalElements elements, double targetJD)
        {
            // 1) Conversión a radianes
            double i = DegToRad(elements.Inclination_deg);
            double om = DegToRad(elements.AscendingNode_deg);
            double w = DegToRad(elements.ArgumentPerihelion_deg);
            double M0 = DegToRad(elements.MeanAnomalyEpoch_deg);

            // 2) Semieje mayor en metros
            double a_m = elements.A_AU * AsteroidMidasConstants.AU_To_Meters;
            double mu = AsteroidMidasConstants.GM_Sun;

            // 3) Tiempo transcurrido desde epoch
            double dtSeconds = (targetJD - elements.Epoch_JD) * 86400.0;

            // 4) Anomalía media en t
            double n = Math.Sqrt(mu / (a_m * a_m * a_m)); // rad/s
            double M = M0 + n * dtSeconds;
            M = NormalizeAngle(M);

            // 5) Resolver ecuación de Kepler M = E - e sin E
            double e = elements.Eccentricity;
            double E = SolveKepler(M, e);

            // 6) De E a ν (anomalía verdadera)
            double cosE = Math.Cos(E);
            double sinE = Math.Sin(E);
            double sqrtOneMinusESq = Math.Sqrt(1 - e * e);

            double cosNu = (cosE - e) / (1 - e * cosE);
            double sinNu = (sqrtOneMinusESq * sinE) / (1 - e * cosE);
            double nu = Math.Atan2(sinNu, cosNu);

            // 7) Distancia r
            double r_m = a_m * (1 - e * cosE);

            // 8) Posición y velocidad en el plano perifocal (PQW)
            double p = a_m * (1 - e * e);
            double rdot = Math.Sqrt(mu / p) * e * sinNu;
            double rfdot = Math.Sqrt(mu / p) * (1 + e * cosNu);

            double x_p = r_m * Math.Cos(nu);
            double y_p = r_m * Math.Sin(nu);
            double z_p = 0.0;

            double vx_p = rdot * Math.Cos(nu) - rfdot * Math.Sin(nu);
            double vy_p = rdot * Math.Sin(nu) + rfdot * Math.Cos(nu);
            double vz_p = 0.0;

            var rPerifocal = new Vector3D(x_p, y_p, z_p);
            var vPerifocal = new Vector3D(vx_p, vy_p, vz_p);

            // 9) Rotaciones: PQW -> IJK (heliocéntrico)
            var rInertial = PerifocalToInertial(rPerifocal, i, om, w);
            var vInertial = PerifocalToInertial(vPerifocal, i, om, w);

            return new OrbitalState(rInertial, vInertial, targetJD);
        }

        // ==========================
        // Métodos auxiliares Kepler
        // ==========================

        private static double DegToRad(double deg) => Math.PI * deg / 180.0;

        private static double NormalizeAngle(double angleRad)
        {
            double twoPi = 2.0 * Math.PI;
            angleRad %= twoPi;
            if (angleRad < 0) angleRad += twoPi;
            return angleRad;
        }

        /// <summary>
        /// Resuelve M = E - e sin(E) por Newton-Raphson.
        /// </summary>
        private static double SolveKepler(double M, double e, int maxIter = 50, double tol = 1e-10)
        {
            double E = M; // mejor arranque: E0 = M
            for (int k = 0; k < maxIter; k++)
            {
                double f = E - e * Math.Sin(E) - M;
                double fp = 1 - e * Math.Cos(E);
                double dE = -f / fp;
                E += dE;
                if (Math.Abs(dE) < tol)
                    break;
            }
            return E;
        }

        /// <summary>
        /// Rotación desde el marco perifocal (PQW) al inercial (IJK).
        /// R = Rz(Ω) * Rx(i) * Rz(ω)
        /// </summary>
        private static Vector3D PerifocalToInertial(Vector3D v, double i, double om, double w)
        {
            double cosO = Math.Cos(om);
            double sinO = Math.Sin(om);
            double cosI = Math.Cos(i);
            double sinI = Math.Sin(i);
            double cosW = Math.Cos(w);
            double sinW = Math.Sin(w);

            // Matriz de rotación compuesta
            double R11 = cosO * cosW - sinO * sinW * cosI;
            double R12 = -cosO * sinW - sinO * cosW * cosI;
            double R13 = sinO * sinI;

            double R21 = sinO * cosW + cosO * sinW * cosI;
            double R22 = -sinO * sinW + cosO * cosW * cosI;
            double R23 = -cosO * sinI;

            double R31 = sinW * sinI;
            double R32 = cosW * sinI;
            double R33 = cosI;

            double x = R11 * v.X + R12 * v.Y + R13 * v.Z;
            double y = R21 * v.X + R22 * v.Y + R23 * v.Z;
            double z = R31 * v.X + R32 * v.Y + R33 * v.Z;

            return new Vector3D(x, y, z);
        }
    }
}
