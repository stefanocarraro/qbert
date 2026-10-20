// Browser side of the game: canvas, keyboard, Web Audio and localStorage.
// All game logic lives in C#; this module only moves pixels, keys and sounds across the boundary.

const highScoreKey = "qbert.highScore";
const handledKeys = new Set([
    "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight",
    "Numpad1", "Numpad3", "Numpad7", "Numpad9",
    "Enter", "NumpadEnter", "Space", "KeyP"
]);

let context2d;
let image;
let audio;
let muted = false;

export async function start() {
    const canvas = document.getElementById("screen");
    if (!canvas) {
        throw new Error("Canvas #screen not found");
    }

    context2d = canvas.getContext("2d");
    image = context2d.createImageData(canvas.width, canvas.height);

    const { getAssemblyExports } = await globalThis.getDotnetRuntime(0);
    const host = (await getAssemblyExports("Qbert.Web.dll")).Qbert.Web.GameHost;

    window.addEventListener("keydown", event => {
        unlockAudio();
        if (event.code === "KeyM") {
            muted = !muted;
            return;
        }
        if (!handledKeys.has(event.code)) {
            return;
        }
        event.preventDefault();
        if (!event.repeat) {
            host.KeyDown(event.code);
        }
    });
    window.addEventListener("keyup", event => {
        if (handledKeys.has(event.code)) {
            event.preventDefault();
            host.KeyUp(event.code);
        }
    });

    let last = performance.now();
    const loop = now => {
        host.Frame(now - last);
        last = now;
        requestAnimationFrame(loop);
    };
    requestAnimationFrame(loop);
}

export function present(pixels) {
    image.data.set(pixels.slice());
    context2d.putImageData(image, 0, 0);
}

export function loadHighScore() {
    try {
        return Number.parseInt(localStorage.getItem(highScoreKey) ?? "0", 10) || 0;
    } catch (error) {
        console.warn("High score unavailable", error);
        return 0;
    }
}

export function saveHighScore(score) {
    try {
        localStorage.setItem(highScoreKey, String(score));
    } catch (error) {
        console.warn("High score not saved", error);
    }
}

// Browsers keep audio suspended until a user gesture; the first key press resumes it.
function unlockAudio() {
    audio ??= new AudioContext();
    if (audio.state === "suspended") {
        audio.resume();
    }
}

function tone(from, to, duration, { type = "square", delay = 0, volume = 0.08 } = {}) {
    const startAt = audio.currentTime + delay;
    const oscillator = audio.createOscillator();
    const gain = audio.createGain();
    oscillator.type = type;
    oscillator.frequency.setValueAtTime(from, startAt);
    oscillator.frequency.exponentialRampToValueAtTime(to, startAt + duration);
    gain.gain.setValueAtTime(volume, startAt);
    gain.gain.exponentialRampToValueAtTime(0.001, startAt + duration);
    oscillator.connect(gain).connect(audio.destination);
    oscillator.start(startAt);
    oscillator.stop(startAt + duration);
}

function melody(notes, step, options) {
    notes.forEach((note, index) => tone(note, note, step, { ...options, delay: index * step }));
}

// Q*bert's famous gibberish: a burst of random chirps.
function curse() {
    for (let i = 0; i < 10; i++) {
        const pitch = 200 + Math.random() * 700;
        tone(pitch, pitch * (0.6 + Math.random() * 0.8), 0.07, { type: "sawtooth", delay: i * 0.07, volume: 0.06 });
    }
}

const sounds = {
    Start: () => melody([523, 659, 784, 1047], 0.1),
    Jump: () => tone(300, 600, 0.06, { volume: 0.04 }),
    Fall: () => tone(900, 60, 1.2, { type: "triangle", volume: 0.1 }),
    Curse: curse,
    Disk: () => melody([392, 494, 587, 784, 988, 1175], 0.08, { type: "triangle" }),
    Catch: () => melody([880, 1320], 0.08),
    CoilyFall: () => tone(700, 40, 1.4, { type: "sawtooth", volume: 0.07 }),
    Hatch: () => tone(120, 80, 0.25, { type: "sawtooth", volume: 0.06 }),
    RoundComplete: () => melody([523, 587, 659, 698, 784, 880, 988, 1047], 0.09, { type: "triangle" }),
    ExtraLife: () => melody([1047, 1319, 1568, 2093], 0.07),
    GameOver: () => melody([392, 330, 262, 196], 0.25, { type: "triangle" })
};

export function playSound(name) {
    if (muted || !audio || audio.state !== "running") {
        return;
    }
    const play = sounds[name];
    if (!play) {
        throw new Error(`Unknown sound ${name}`);
    }
    play();
}
