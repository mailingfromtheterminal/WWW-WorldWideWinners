namespace WWW.Services.Constans
{
    public static class MissionConstants
    {
        // --- Misión de 50 sondas cinéticas ---

        public const int NumberOfProbes = 50;
        public const double ProbeMass_kg = 10_000;       // 10 toneladas c/u
        public const double ImpactSpeed_m_s = 10_000;    // 10 km/s
        public const double Beta = 2.5;                  // factor de eficiencia

        // ΔV total ~1.25 mm/s (calculado)
        public const double DeltaV_Total_m_s = 0.00125;

        // Tiempo del impacto respecto al "t0" (años)
        public const double YearsToImpact = 11.0;

        // Ventana de simulación en modo colisión: 0–15 años
        public const double DeflectionWindow_Years = 15.0;
    }
}
