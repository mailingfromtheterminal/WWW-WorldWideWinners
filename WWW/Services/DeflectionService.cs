using WWW.Services.Constans;

namespace WWW.Services
{
    public class DeflectionService
    {
        private readonly OrbitalMechanicsService _orbitalService;

        public DeflectionService(OrbitalMechanicsService orbitalService)
        {
            _orbitalService = orbitalService;
        }

        /// <summary>
        /// Devuelve los elementos originales de Midas.
        /// </summary>
        public OrbitalElements GetOriginalElements() => _orbitalService.GetMidasOriginalElements();

        /// <summary>
        /// Calcula los elementos "deflectados" aplicando un ΔV tangencial
        /// en el momento del impacto (añosDesdeEpoch).
        /// </summary>
        public OrbitalElements GetDeflectedElements(double yearsDesdeEpoch)
        {
            var original = _orbitalService.GetMidasOriginalElements();

            // 1) Convertimos años desde epoch a JD
            double targetJD = original.Epoch_JD + yearsDesdeEpoch * 365.25;

            // 2) Estado original en ese JD
            OrbitalState state = _orbitalService.PropagateToJulianDate(original, targetJD);

            Vector3D r = state.Position_m;
            Vector3D v = state.Velocity_m_s;

            double rNorm = r.Norm();
            double vNorm = v.Norm();

            if (vNorm <= 0)
            {
                throw new InvalidOperationException("Velocidad nula, no se puede aplicar ΔV tangencial.");
            }

            // 3) Dirección tangencial (unidad)
            Vector3D vHat = (1.0 / vNorm) * v;

            // 4) Aplicar ΔV total (50 sondas)
            double dv = MissionConstants.DeltaV_Total_m_s;
            Vector3D vNew = v + dv * vHat;

            // 5) Nuevo semieje mayor por vis-viva: 1/a = 2/r - v^2/μ
            double mu = AsteroidMidasConstants.GM_Sun;
            double r_m = rNorm;
            double vNewNorm = vNew.Norm();

            double invAnew = 2.0 / r_m - (vNewNorm * vNewNorm) / mu;
            double aNew_m = 1.0 / invAnew;
            double aNew_AU = aNew_m / AsteroidMidasConstants.AU_To_Meters;

            // 6) Por simplicidad, mantenemos los demás elementos.
            // (En realidad cambiarían un poco, pero para visualización es suficiente.)
            return new OrbitalElements(
                aNew_AU,
                original.Eccentricity,
                original.Inclination_deg,
                original.AscendingNode_deg,
                original.ArgumentPerihelion_deg,
                original.MeanAnomalyEpoch_deg,
                original.Epoch_JD
            );
        }

        /// <summary>
        /// Devuelve un resumen del impacto: ΔV y cambio de periodo aproximado.
        /// </summary>
        public (double dv_m_s, double deltaPeriod_days) GetImpactSummary()
        {
            var original = _orbitalService.GetMidasOriginalElements();
            var deflected = GetDeflectedElements(MissionConstants.YearsToImpact);

            double T_old_years = Math.Pow(original.A_AU, 1.5);
            double T_new_years = Math.Pow(deflected.A_AU, 1.5);
            double deltaDays = (T_new_years - T_old_years) * 365.25;

            return (MissionConstants.DeltaV_Total_m_s, deltaDays);
        }
    }
}
