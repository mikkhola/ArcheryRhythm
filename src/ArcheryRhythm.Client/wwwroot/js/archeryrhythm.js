/* global Blazor */
window.archeryRhythm = window.archeryRhythm || {};

archeryRhythm.storage = {
    get: (key) => {
        try { return localStorage.getItem(key); } catch { return null; }
    },
    set: (key, value) => {
        try { localStorage.setItem(key, value); } catch { }
    },
    remove: (key) => {
        try { localStorage.removeItem(key); } catch { }
    }
};

archeryRhythm.culture = {
    get: () => archeryRhythm.storage.get("archeryrhythm.culture") || "fi-FI",
    set: (value) => archeryRhythm.storage.set("archeryrhythm.culture", value)
};

archeryRhythm.files = {
    downloadJson: (filename, jsonText) => {
        const blob = new Blob([jsonText], { type: "application/json;charset=utf-8" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 0);
    }
};

archeryRhythm.speech = (() => {
    const isSupported = () => typeof window.speechSynthesis !== "undefined" && typeof window.SpeechSynthesisUtterance !== "undefined";

    const getVoicesNow = () => {
        if (!isSupported()) return [];
        const voices = window.speechSynthesis.getVoices() || [];
        return voices.map(v => ({
            voiceUri: v.voiceURI,
            name: v.name,
            lang: v.lang,
            default: v.default === true
        }));
    };

    const getVoices = async () => {
        if (!isSupported()) return [];
        const existing = getVoicesNow();
        if (existing.length > 0) return existing;

        return await new Promise(resolve => {
            let done = false;
            const finish = () => {
                if (done) return;
                done = true;
                try { window.speechSynthesis.removeEventListener("voiceschanged", finish); } catch { }
                resolve(getVoicesNow());
            };

            try { window.speechSynthesis.addEventListener("voiceschanged", finish); } catch { }
            setTimeout(finish, 1000);
        });
    };

    const cancel = () => {
        if (!isSupported()) return;
        try { window.speechSynthesis.cancel(); } catch { }
    };

    const pickVoice = (voices, voiceUri, lang) => {
        if (!voices || voices.length === 0) return null;
        if (voiceUri) {
            const match = voices.find(v => v.voiceUri === voiceUri);
            if (match) return match;
        }

        const l = (lang || "").toString();
        const prefix = l.includes("-") ? l.split("-")[0] : l;
        const byLang = voices.find(v => (v.lang || "").toLowerCase() === l.toLowerCase());
        if (byLang) return byLang;

        const byPrefix = voices.find(v => (v.lang || "").toLowerCase().startsWith(prefix.toLowerCase()));
        if (byPrefix) return byPrefix;

        const def = voices.find(v => v.default === true);
        return def || voices[0];
    };

    const speak = async (text, voiceUri, volume, lang) => {
        if (!isSupported()) throw new Error("Speech synthesis not supported");
        const trimmed = (text || "").toString().trim();
        if (!trimmed) return;

        const utterance = new SpeechSynthesisUtterance(trimmed);
        utterance.volume = Math.max(0, Math.min(1, Number(volume ?? 1)));
        if (lang) utterance.lang = lang;

        const voices = await getVoices();
        const chosen = pickVoice(voices, voiceUri, lang);
        if (chosen) {
            const nativeVoices = window.speechSynthesis.getVoices() || [];
            const native = nativeVoices.find(v => v.voiceURI === chosen.voiceUri);
            if (native) utterance.voice = native;
        }

        utterance.onerror = () => { };
        window.speechSynthesis.speak(utterance);
    };

    return { isSupported, getVoices, cancel, speak };
})();

archeryRhythm.audio = (() => {
    const isSupported = () => typeof window.Audio !== "undefined";

    const cache = new Map();
    const cueUrl = (cueKey) => `audio/fi/${encodeURIComponent(cueKey)}.mp3`;

    const getAudio = (cueKey) => {
        if (cache.has(cueKey)) return cache.get(cueKey);
        const audio = new Audio(cueUrl(cueKey));
        audio.preload = "auto";
        cache.set(cueKey, audio);
        return audio;
    };

    // Returns true if play started, false if blocked/failed.
    const playFiCue = async (cueKey, volume) => {
        if (!isSupported()) return false;
        const key = (cueKey || "").toString().trim();
        if (!key) return false;

        const audio = getAudio(key);
        audio.volume = Math.max(0, Math.min(1, Number(volume ?? 1)));
        try { audio.currentTime = 0; } catch { }

        try {
            const p = audio.play();
            if (p && typeof p.then === "function") await p;
            return true;
        } catch {
            return false;
        }
    };

    return { isSupported, playFiCue };
})();
