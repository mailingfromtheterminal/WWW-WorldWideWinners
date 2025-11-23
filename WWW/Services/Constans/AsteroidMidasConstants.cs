namespace WWW.Services.Constans
{
    public static class AsteroidMidasConstants
    {
        // ---- Elementos Orbitales de 1981 Midas (JPL / epoch 2017) ----

        public const double SemiMajorAxis_AU = 1.7759;      // a
        public const double Eccentricity = 0.6502;          // e
        public const double Inclination_deg = 39.833;       // i
        public const double AscendingNode_deg = 356.90;     // Ω
        public const double ArgumentPerihelion_deg = 267.80; // ω
        public const double MeanAnomalyEpoch_deg = 256.48;  // M0

        // Epoch JD 2458000.5 ≈ 2017-Sep-04
        public const double Epoch_JD = 2458000.5;

        // ---- Propiedades Físicas ----
        public const double Mass_kg = 1.0e13; // 1 × 10^13 kg (aprox)
        public const double RotationPeriod_hours = 5.2;    // rotación tipo peanut

        // ---- Constantes astronómicas ----
        public const double AU_To_Meters = 1.495978707e11;
        public const double GM_Sun = 1.32712440018e20;     // µ del Sol en m^3/s^2
    }
}
