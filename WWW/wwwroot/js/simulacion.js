// wwwroot/js/simulacion.js

// =====================
// 1. CONSTANTES GLOBALES
// =====================

// Escala temporal global
const YEAR_START = 2025;
const YEAR_IMPACT_EARTH = 2129;
const YEAR_END = 2140;
const TOTAL_YEARS = YEAR_END - YEAR_START;

const DAYS_PER_YEAR = 365.25;

// JD de referencia (≈ 2025-09-24)
const JD_START = 2460943.5;
const JD_END = JD_START + TOTAL_YEARS * DAYS_PER_YEAR;

// Misión de sondas
const PROBE_LAUNCH_YEAR = YEAR_IMPACT_EARTH - 11; // 2118
const PROBE_IMPACT_YEAR = YEAR_IMPACT_EARTH;      // 2129

// Conversión
const AU_TO_KM = 149597870.7;

// Datos orbitales aproximados de 1981 Midas
const ASTEROID_DATA = {
    name: "1981 Midas",
    a: 1.7759,        // AU
    e: 0.6502,
    i: 39.833,        // deg
    om: 356.90,       // deg
    w: 267.80,        // deg
    ma_epoch: 256.48, // deg
    epochJD: 2458000.5
};

// Periodo aproximado original en años (T^2 ~ a^3)
const T_ORIGINAL_YEARS = Math.pow(ASTEROID_DATA.a, 1.5);

// =====================
// 2. UTILIDADES
// =====================

function jdToDateString(jd) {
    const date = new Date((jd - 2440587.5) * 86400000);
    return date.toISOString().split("T")[0];
}

// =====================
// 3. SIMULACIÓN SPACEKIT
// =====================

document.addEventListener("DOMContentLoaded", () => {
    // DOM
    const container = document.getElementById("simulation-container");
    const elDistEarth = document.getElementById("dist-earth");
    const elCurrentDate = document.getElementById("current-date-text");
    const elSliderDate = document.getElementById("slider-date-display");
    const slider = document.getElementById("time-slider");
    const selectMitigation = document.getElementById("mitigation-select");
    const checkDeflection = document.getElementById("toggle-deflection");
    const groupDeflection = document.getElementById("deflection-toggle-group");
    const btnPause = document.getElementById("btn-pause-play");
    const statusText = document.getElementById("mission-status");
    const elImpactStats = document.getElementById("impact-stats");
    const elImpactDv = document.getElementById("impact-dv");
    const elImpactDt = document.getElementById("impact-dt");
    const lblStart = document.getElementById("timeline-start-label");
    const lblMid = document.getElementById("timeline-mid-label");
    const lblEnd = document.getElementById("timeline-end-label");

    // Estado
    let viz = null;
    let earth = null;
    let asteroidOriginal = null;
    let asteroidDeflected = null;
    let isPlaying = true;
    let isDeflectionActive = false;
    let currentMode = "normal"; // "normal" | "deflection"

    // 50 sondas
    const probes = [];

    // Crear simulación
    viz = new Spacekit.Simulation(container, {
        basePath: "https://typpo.github.io/spacekit/src/",
        jd: JD_START,
        jdPerSecond: 10, // días por segundo
        camera: {
            initialPosition: [0, -3, 1.5],
            enableDrift: false
        },
        debug: {
            showAxes: false,
            showGrid: false,
            showStats: false
        }
    });

    // Fondo y objetos base
    viz.createSkybox(Spacekit.SkyboxPresets.NASA_TYCHO);
    viz.createObject("Sun", Spacekit.SpaceObjectPresets.SUN);
    earth = viz.createObject("Earth", Spacekit.SpaceObjectPresets.EARTH);

    // Midas original (rojo)
    asteroidOriginal = viz.createObject("midas_original", {
        labelText: "1981 Midas",
        ephem: new Spacekit.Ephem(
            {
                a: ASTEROID_DATA.a,
                e: ASTEROID_DATA.e,
                i: ASTEROID_DATA.i,
                om: ASTEROID_DATA.om,
                w: ASTEROID_DATA.w,
                ma: ASTEROID_DATA.ma_epoch,
                epoch: ASTEROID_DATA.epochJD
            },
            "deg"
        ),
        theme: {
            orbitColor: 0xff3333,
            objectColor: 0xffaaaa,
            color: 0xffaaaa
        }
    });

    // Etiquetas iniciales del timeline
    lblStart.textContent = YEAR_START.toString();
    lblMid.textContent = YEAR_IMPACT_EARTH.toString();
    lblEnd.textContent = YEAR_END.toString();

    // Inicializar sondas
    initProbes();

    // onTick – se ejecuta cada frame
    viz.onTick = function () {
        const currentJD = viz.getJd();
        const currentYearContinuous = YEAR_START + (currentJD - JD_START) / DAYS_PER_YEAR;

        // 1) Obtener posiciones de Tierra y Midas en este JD
        const posEarth = earth.getPosition();           // [x,y,z] AU
        const posAstOriginal = asteroidOriginal.getPosition(); // [x,y,z] AU

        const hasValidEarth =
            Array.isArray(posEarth) &&
            posEarth.length === 3 &&
            !posEarth.some((v) => isNaN(v));

        const hasValidAst =
            Array.isArray(posAstOriginal) &&
            posAstOriginal.length === 3 &&
            !posAstOriginal.some((v) => isNaN(v));

        if (hasValidEarth && hasValidAst) {
            // Distancia Tierra–Midas
            const dx = posAstOriginal[0] - posEarth[0];
            const dy = posAstOriginal[1] - posEarth[1];
            const dz = posAstOriginal[2] - posEarth[2];
            const distAU = Math.sqrt(dx * dx + dy * dy + dz * dz);
            const distKM = distAU * AU_TO_KM;

            elDistEarth.innerText =
                distKM.toLocaleString("es-MX", { maximumFractionDigits: 0 }) + " km";

            if (distKM < 8_000_000 && !isDeflectionActive) {
                elDistEarth.classList.add("warning-text");
                statusText.innerText = "ALERTA COLISIÓN";
                statusText.className = "status-indicator";
            } else {
                elDistEarth.classList.remove("warning-text");

                const yearInt = Math.round(currentYearContinuous);
                if (!isDeflectionActive && yearInt === YEAR_IMPACT_EARTH) {
                    statusText.innerText = "IMPACTO CON LA TIERRA";
                    statusText.className = "status-indicator";
                } else if (isDeflectionActive) {
                    statusText.innerText = "TRAYECTORIA DESVIADA";
                    statusText.className = "status-indicator status-safe";
                } else {
                    statusText.innerText = "MONITOREO";
                    statusText.className = "status-indicator";
                }
            }

            // Actualizar sondas (solo en modo deflection)
            updateProbes(currentMode, currentYearContinuous, posEarth, posAstOriginal);
        } else {
            elDistEarth.innerText = "Calculando...";
        }

        // Fecha textual
        const dateStr = jdToDateString(currentJD);
        elCurrentDate.innerText = dateStr;
        elSliderDate.innerText = dateStr;

        // 2) Sincronizar slider solo cuando está en play
        if (isPlaying) {
            const totalRangeJD = JD_END - JD_START;
            const progress = (currentJD - JD_START) / totalRangeJD;
            slider.value = Math.max(0, Math.min(1000, progress * 1000));

            // Loop
            if (currentJD >= JD_END) {
                viz.setJd(JD_START);
            }
        }
    };

    // =====================
    // 4. CONTROLES UI
    // =====================

    // Slider manual: mapear 0..1000 -> [JD_START, JD_END]
    slider.addEventListener("input", (e) => {
        const val = parseFloat(e.target.value);
        const totalRangeJD = JD_END - JD_START;
        const newJD = JD_START + (val / 1000) * totalRangeJD;
        viz.setJd(newJD);
        elSliderDate.innerText = jdToDateString(newJD);
    });

    // Pausar / Reanudar
    btnPause.addEventListener("click", () => {
        if (isPlaying) {
            viz.stop();
            btnPause.innerText = "REANUDAR";
        } else {
            viz.start();
            btnPause.innerText = "PAUSAR";
        }
        isPlaying = !isPlaying;
    });

    // Selector de estrategia
    selectMitigation.addEventListener("change", (e) => {
        const value = e.target.value;

        if (value === "none") {
            currentMode = "normal";
            groupDeflection.classList.add("disabled");
            checkDeflection.disabled = true;
            checkDeflection.checked = false;
            desactivarDesvio();

            lblStart.textContent = YEAR_START.toString();
            lblMid.textContent = YEAR_IMPACT_EARTH.toString();
            lblEnd.textContent = YEAR_END.toString();
        } else if (value === "multi-kinetic") {
            currentMode = "deflection";
            groupDeflection.classList.remove("disabled");
            checkDeflection.disabled = false;
            checkDeflection.checked = false;

            lblStart.textContent = `${YEAR_START} (inicio sim)`;
            lblMid.textContent = `${PROBE_LAUNCH_YEAR} (lanzamiento sondas)`;
            lblEnd.textContent = `${PROBE_IMPACT_YEAR} (impacto Midas)`;

            viz.setJd(JD_START);
            slider.value = 0;
            elSliderDate.innerText = jdToDateString(JD_START);

            desactivarDesvio();
        }
    });

    // Checkbox de simular desvío
    checkDeflection.addEventListener("change", (e) => {
        if (e.target.checked) {
            activarDesvio();
        } else {
            desactivarDesvio();
        }
    });

    // =====================
    // 5. LÓGICA DE DESVÍO (usa BACKEND)
    // =====================

    async function activarDesvio() {
        isDeflectionActive = true;
        elImpactStats.style.display = "block";

        try {
            const resp = await fetch("/api/impact/summary");
            if (!resp.ok) {
                throw new Error("Error HTTP " + resp.status);
            }

            const data = await resp.json();
            const dv = data.deltaV_m_s;            // m/s
            const deltaTdays = data.deltaPeriod_days; // días

            elImpactDv.textContent = dv.toFixed(5) + " m/s";
            elImpactDt.textContent = deltaTdays.toFixed(2) + " días";

            const deltaYears = deltaTdays / DAYS_PER_YEAR;
            const T_new_years = T_ORIGINAL_YEARS + deltaYears; // aproximado
            const a_new = Math.pow(T_new_years, 2.0 / 3.0);    // AU

            if (asteroidDeflected) {
                viz.removeObject(asteroidDeflected);
            }

            asteroidDeflected = viz.createObject("midas_deflected", {
                labelText: "1981 Midas (desviado)",
                ephem: new Spacekit.Ephem(
                    {
                        a: a_new,
                        e: ASTEROID_DATA.e,
                        i: ASTEROID_DATA.i,
                        om: ASTEROID_DATA.om,
                        w: ASTEROID_DATA.w,
                        ma: ASTEROID_DATA.ma_epoch,
                        epoch: ASTEROID_DATA.epochJD
                    },
                    "deg"
                ),
                theme: {
                    orbitColor: 0x2ecc71,
                    objectColor: 0x2ecc71,
                    color: 0x2ecc71
                }
            });

            statusText.innerText = "TRAYECTORIA DESVIADA";
            statusText.className = "status-indicator status-safe";

        } catch (err) {
            console.error("Error al obtener datos de impacto:", err);
            isDeflectionActive = false;
            elImpactStats.style.display = "none";
            statusText.innerText = "MONITOREO";
            statusText.className = "status-indicator";
            if (asteroidDeflected) {
                viz.removeObject(asteroidDeflected);
                asteroidDeflected = null;
            }
        }
    }

    function desactivarDesvio() {
        isDeflectionActive = false;
        elImpactStats.style.display = "none";
        statusText.innerText = "MONITOREO";
        statusText.className = "status-indicator";

        if (asteroidDeflected) {
            viz.removeObject(asteroidDeflected);
            asteroidDeflected = null;
        }
    }

    // =====================
    // 6. SONDAS – INICIALIZACIÓN Y ANIMACIÓN
    // =====================

    function initProbes() {
        const nProbes = 50;
        const texture = "https://typpo.github.io/spacekit/assets/sprites/asteroid.png";

        for (let i = 0; i < nProbes; i++) {
            // Dispersión tipo "shotgun" alrededor del vector Tierra→Midas
            const spread = {
                x: (Math.random() - 0.5) * 0.08, // AU
                y: (Math.random() - 0.5) * 0.08,
                z: (Math.random() - 0.5) * 0.08
            };

            const probeObj = viz.createObject(`probe_${i}`, {
                labelText: "",
                textureUrl: texture,
                radius: 0.01,           // pequeño para que parezcan sondas
                position: [0, 0, 0],
                theme: {
                    objectColor: 0xffffff,
                    color: 0xffffff
                }
            });

            probes.push({ obj: probeObj, spread });
        }
    }

    function updateProbes(mode, currentYear, posEarth, posMidas) {
        if (probes.length === 0) return;

        if (mode !== "deflection") {
            probes.forEach((p) => {
                p.obj.setPosition(9999, 9999, 9999); // fuera de la vista
            });
            return;
        }

        const launch = PROBE_LAUNCH_YEAR;
        const impact = PROBE_IMPACT_YEAR;

        // Antes del lanzamiento: sondas agrupadas cerca de la Tierra
        if (currentYear < launch) {
            probes.forEach((p) => {
                const x = posEarth[0] + p.spread.x * 0.2;
                const y = posEarth[1] + p.spread.y * 0.2;
                const z = posEarth[2] + p.spread.z * 0.2;
                p.obj.setPosition(x, y, z);
            });
            return;
        }

        // Después del impacto: sondas en Midas
        if (currentYear >= impact) {
            probes.forEach((p) => {
                p.obj.setPosition(posMidas[0], posMidas[1], posMidas[2]);
            });
            return;
        }

        // Durante el viaje: interpolación Tierra → Midas
        const t = (currentYear - launch) / (impact - launch); // 0..1
        probes.forEach((p) => {
            const baseX = (1 - t) * posEarth[0] + t * posMidas[0];
            const baseY = (1 - t) * posEarth[1] + t * posMidas[1];
            const baseZ = (1 - t) * posEarth[2] + t * posMidas[2];

            const spreadFactor = 1 - t;
            const x = baseX + p.spread.x * spreadFactor;
            const y = baseY + p.spread.y * spreadFactor;
            const z = baseZ + p.spread.z * spreadFactor;

            p.obj.setPosition(x, y, z);
        });
    }
});
