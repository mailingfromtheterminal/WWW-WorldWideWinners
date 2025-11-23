using Microsoft.AspNetCore.Mvc;
using WWW.Services;
using WWW.Services.Constans;

namespace WWW.Controllers
{
    [ApiController]
    [Route("api")]
    public class SimulationController : ControllerBase
    {
        private readonly OrbitalMechanicsService _orbitalService;
        private readonly DeflectionService _deflectionService;

        public SimulationController(
            OrbitalMechanicsService orbitalService,
            DeflectionService deflectionService)
        {
            _orbitalService = orbitalService;
            _deflectionService = deflectionService;
        }

        /// <summary>
        /// Posición/velocidad de Midas sin desvío.
        /// yearsSinceEpoch: años desde la epoch de JPL (2017 aprox).
        /// </summary>
        [HttpGet("orbit/midas")]
        public IActionResult GetMidasOrbitPoint([FromQuery] double yearsSinceEpoch)
        {
            var elements = _orbitalService.GetMidasOriginalElements();

            double jd = elements.Epoch_JD + yearsSinceEpoch * 365.25;
            var state = _orbitalService.PropagateToJulianDate(elements, jd);

            // Devolvemos en AU y km/s para el frontend
            const double AU = AsteroidMidasConstants.AU_To_Meters;
            const double KM_PER_M = 0.001;

            return Ok(new
            {
                julianDate = state.JulianDate,
                position = new
                {
                    au = new
                    {
                        x = state.Position_m.X / AU,
                        y = state.Position_m.Y / AU,
                        z = state.Position_m.Z / AU
                    }
                },
                velocity = new
                {
                    km_s = new
                    {
                        x = state.Velocity_m_s.X * KM_PER_M,
                        y = state.Velocity_m_s.Y * KM_PER_M,
                        z = state.Velocity_m_s.Z * KM_PER_M
                    }
                }
            });
        }

        /// <summary>
        /// Posición/velocidad de Midas con órbita deflectada
        /// (aplicando ΔV en YearsToImpact).
        /// </summary>
        [HttpGet("orbit/midas-deflected")]
        public IActionResult GetMidasDeflectedOrbitPoint([FromQuery] double yearsSinceEpoch)
        {
            // 1) Elementos deflectados
            var deflectedElems = _deflectionService.GetDeflectedElements(MissionConstants.YearsToImpact);

            // 2) Propagación
            double jd = deflectedElems.Epoch_JD + yearsSinceEpoch * 365.25;
            var state = _orbitalService.PropagateToJulianDate(deflectedElems, jd);

            const double AU = AsteroidMidasConstants.AU_To_Meters;
            const double KM_PER_M = 0.001;

            return Ok(new
            {
                julianDate = state.JulianDate,
                position = new
                {
                    au = new
                    {
                        x = state.Position_m.X / AU,
                        y = state.Position_m.Y / AU,
                        z = state.Position_m.Z / AU
                    }
                },
                velocity = new
                {
                    km_s = new
                    {
                        x = state.Velocity_m_s.X * KM_PER_M,
                        y = state.Velocity_m_s.Y * KM_PER_M,
                        z = state.Velocity_m_s.Z * KM_PER_M
                    }
                }
            });
        }

        /// <summary>
        /// Resumen del impacto: ΔV total y cambio de periodo.
        /// </summary>
        [HttpGet("impact/summary")]
        public IActionResult GetImpactSummary()
        {
            var (dv, dTdays) = _deflectionService.GetImpactSummary();

            return Ok(new
            {
                deltaV_m_s = dv,
                deltaPeriod_days = dTdays
            });
        }
    }
}
