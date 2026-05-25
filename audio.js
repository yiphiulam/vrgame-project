/**
 * The Breathless Study Room (窒息室) - Procedural Audio Engine
 * Uses Web Audio API to synthesize spatial, highly-customized horror ambient sounds,
 * heartbeats, clock ticks, static noises, and scares procedurally. No external assets required!
 */

class AudioEngine {
  constructor() {
    this.ctx = null;
    this.masterGain = null;
    this.ambientOscs = [];
    this.ambientGain = null;
    
    // Heartbeat variables
    this.heartbeatTimer = null;
    this.stressLevel = 0; // 0 to 100
    
    // State indicators
    this.isMuted = false;
    this.initialized = false;
  }

  /**
   * Initialize and unlock AudioContext on user interaction
   */
  init() {
    if (this.initialized) return;
    
    try {
      const AudioContextClass = window.AudioContext || window.webkitAudioContext;
      this.ctx = new AudioContextClass();
      
      // Master gain
      this.masterGain = this.ctx.createGain();
      this.masterGain.gain.setValueAtTime(0.7, this.ctx.currentTime);
      this.masterGain.connect(this.ctx.destination);
      
      this.initialized = true;
      console.log("AudioEngine: Web Audio API initialized successfully!");
      
      // Start background hum
      this.startAmbientHum();
      // Start dynamic heartbeat
      this.startHeartbeatLoop();
    } catch (e) {
      console.error("AudioEngine: Failed to initialize Web Audio API", e);
    }
  }

  /**
   * Resume context if suspended (browser security)
   */
  resume() {
    if (this.ctx && this.ctx.state === 'suspended') {
      this.ctx.resume();
    }
  }

  setMute(mute) {
    this.isMuted = mute;
    if (this.masterGain) {
      this.masterGain.gain.setValueAtTime(mute ? 0 : 0.7, this.ctx.currentTime);
    }
  }

  /**
   * Low-frequency horror background hum
   */
  startAmbientHum() {
    if (!this.ctx) return;
    
    // Filter out very high frequencies for a dark room rumble
    const filter = this.ctx.createBiquadFilter();
    filter.type = 'lowpass';
    filter.frequency.setValueAtTime(120, this.ctx.currentTime);
    filter.connect(this.masterGain);
    
    this.ambientGain = this.ctx.createGain();
    this.ambientGain.gain.setValueAtTime(0.2, this.ctx.currentTime);
    this.ambientGain.connect(filter);
    
    // 3 detuned oscillators to create a thick dark texture (55Hz, 55.4Hz, 56Hz)
    const freqs = [55.0, 55.4, 56.1];
    freqs.forEach(freq => {
      const osc = this.ctx.createOscillator();
      osc.type = 'sine';
      osc.frequency.setValueAtTime(freq, this.ctx.currentTime);
      osc.connect(this.ambientGain);
      osc.start();
      this.ambientOscs.push(osc);
    });
    
    // Modulator oscillator for sub-base wave throb
    const lfo = this.ctx.createOscillator();
    lfo.type = 'sine';
    lfo.frequency.setValueAtTime(0.25, this.ctx.currentTime); // 4 seconds cycle
    
    const lfoGain = this.ctx.createGain();
    lfoGain.gain.setValueAtTime(0.05, this.ctx.currentTime);
    
    lfo.connect(lfoGain);
    lfoGain.connect(this.ambientGain.gain);
    lfo.start();
    this.ambientOscs.push(lfo);
  }

  /**
   * Dynamic heartbeat controller
   */
  startHeartbeatLoop() {
    const triggerNextBeat = () => {
      if (!this.initialized || this.isMuted) {
        this.heartbeatTimer = setTimeout(triggerNextBeat, 1000);
        return;
      }
      
      // Calculate delay based on stress level (0% stress = 70bpm, 100% stress = 150bpm)
      const bpm = 65 + (this.stressLevel * 0.95);
      const intervalMs = (60 / bpm) * 1000;
      
      this.playHeartbeatDoublet();
      
      this.heartbeatTimer = setTimeout(triggerNextBeat, intervalMs);
    };
    
    triggerNextBeat();
  }

  setStress(stress) {
    this.stressLevel = Math.max(0, Math.min(100, stress));
  }

  /**
   * Plays a double heartbeat "lub-dub"
   */
  playHeartbeatDoublet() {
    if (!this.ctx || this.ctx.state === 'suspended') return;
    
    const now = this.ctx.currentTime;
    
    // Heartbeats are made of low sine frequency waves with exponential decay
    const playThump = (time, intensity) => {
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      
      osc.type = 'sine';
      // Low rumble frequency
      osc.frequency.setValueAtTime(55, time);
      osc.frequency.exponentialRampToValueAtTime(10, time + 0.18);
      
      // Amplitude envelope
      gain.gain.setValueAtTime(0, time);
      gain.gain.linearRampToValueAtTime(intensity, time + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.001, time + 0.18);
      
      osc.connect(gain);
      gain.connect(this.masterGain);
      
      osc.start(time);
      osc.stop(time + 0.2);
    };
    
    // Adjust volume with stress
    const volume = 0.3 + (this.stressLevel / 100) * 0.5;
    
    // Play double beat: "lub" then "dub" (150ms apart)
    playThump(now, volume);
    playThump(now + 0.18, volume * 0.7);
  }

  /**
   * Procedural clock tick (fast high pitch transient)
   */
  playClockTick(distorted = false) {
    if (!this.ctx || this.ctx.state === 'suspended') return;
    
    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    
    osc.type = 'sine';
    
    if (distorted) {
      // Deep creepy mechanical metallic tick
      osc.frequency.setValueAtTime(150, now);
      osc.frequency.linearRampToValueAtTime(30, now + 0.08);
      
      gain.gain.setValueAtTime(0.3, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.08);
      osc.connect(gain);
      gain.connect(this.masterGain);
      osc.start(now);
      osc.stop(now + 0.1);
    } else {
      // Normal subtle high tick
      osc.frequency.setValueAtTime(1800, now);
      
      gain.gain.setValueAtTime(0.12, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.015);
      osc.connect(gain);
      gain.connect(this.masterGain);
      osc.start(now);
      osc.stop(now + 0.02);
    }
  }

  /**
   * Window tapping: wood knock
   */
  playWindowTap() {
    if (!this.ctx || this.ctx.state === 'suspended') return;
    
    const now = this.ctx.currentTime;
    
    const singleKnock = (time) => {
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      
      // Pitch envelope simulating hitting a wooden window pane
      osc.type = 'triangle';
      osc.frequency.setValueAtTime(320, time);
      osc.frequency.exponentialRampToValueAtTime(80, time + 0.06);
      
      gain.gain.setValueAtTime(0, time);
      gain.gain.linearRampToValueAtTime(0.6, time + 0.005);
      gain.gain.exponentialRampToValueAtTime(0.001, time + 0.06);
      
      // High-pass filter to sound sharp and dry
      const hp = this.ctx.createBiquadFilter();
      hp.type = 'highpass';
      hp.frequency.setValueAtTime(100, time);
      
      osc.connect(hp);
      hp.connect(gain);
      gain.connect(this.masterGain);
      
      osc.start(time);
      osc.stop(time + 0.08);
    };
    
    // Play 3 successive knocks: rap-rap-rap
    singleKnock(now);
    singleKnock(now + 0.14);
    singleKnock(now + 0.28);
  }

  /**
   * Eerie broadcast notification ring tone (retro chime)
   */
  playBroadcastBeep() {
    if (!this.ctx || this.ctx.state === 'suspended') return;
    
    const now = this.ctx.currentTime;
    
    const playNote = (time, freq, duration) => {
      const osc = this.ctx.createOscillator();
      const gain = this.ctx.createGain();
      
      osc.type = 'sine';
      osc.frequency.setValueAtTime(freq, time);
      
      gain.gain.setValueAtTime(0, time);
      gain.gain.linearRampToValueAtTime(0.3, time + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.001, time + duration);
      
      osc.connect(gain);
      gain.connect(this.masterGain);
      
      osc.start(time);
      osc.stop(time + duration + 0.1);
    };
    
    // Retro dual tone chime: E5 -> C5 -> A4
    playNote(now, 659.25, 0.4); // E5
    playNote(now + 0.35, 523.25, 0.4); // C5
    playNote(now + 0.7, 440.00, 0.6); // A4
  }

  /**
   * TV radio static noise
   */
  playStaticNoise(duration = 1.0, volume = 0.25) {
    if (!this.ctx || this.ctx.state === 'suspended') return;
    
    const now = this.ctx.currentTime;
    const bufferSize = this.ctx.sampleRate * duration;
    const buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
    const data = buffer.getChannelData(0);
    
    // Fill buffer with white noise
    for (let i = 0; i < bufferSize; i++) {
      data[i] = Math.random() * 2 - 1;
    }
    
    const noiseNode = this.ctx.createBufferSource();
    noiseNode.buffer = buffer;
    
    // Filter to make it crackly and muffled
    const lp = this.ctx.createBiquadFilter();
    lp.type = 'bandpass';
    lp.frequency.setValueAtTime(1000, now);
    lp.Q.setValueAtTime(1.5, now);
    
    const gain = this.ctx.createGain();
    gain.gain.setValueAtTime(volume, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + duration - 0.05);
    
    noiseNode.connect(lp);
    lp.connect(gain);
    gain.connect(this.masterGain);
    
    noiseNode.start(now);
    noiseNode.stop(now + duration);
  }

  /**
   * Terrifying chord shriek for jumpscares or severe stress
   */
  playScare() {
    if (!this.ctx || this.ctx.state === 'suspended') return;
    
    const now = this.ctx.currentTime;
    
    // 5 highly detuned saw oscillators for a screeching feedback sound
    const frequencies = [300, 311, 415, 520, 680, 850];
    const delayTimes = [0, 0.02, 0.04, 0.06, 0.08, 0.1];
    
    const scareGain = this.ctx.createGain();
    scareGain.gain.setValueAtTime(0, now);
    scareGain.gain.linearRampToValueAtTime(0.8, now + 0.05); // Abrupt rise
    scareGain.gain.exponentialRampToValueAtTime(0.001, now + 1.8); // Long release
    
    // Reverb filter (simple bandpass combination)
    const bp = this.ctx.createBiquadFilter();
    bp.type = 'peaking';
    bp.frequency.setValueAtTime(800, now);
    bp.Q.setValueAtTime(2.0, now);
    
    scareGain.connect(bp);
    bp.connect(this.masterGain);
    
    frequencies.forEach((freq, i) => {
      const osc = this.ctx.createOscillator();
      osc.type = 'sawtooth';
      
      // Pitch drop to increase dread
      osc.frequency.setValueAtTime(freq, now + delayTimes[i]);
      osc.frequency.linearRampToValueAtTime(freq / 2, now + 1.2);
      
      osc.connect(scareGain);
      osc.start(now + delayTimes[i]);
      osc.stop(now + 2.0);
    });
    
    // Play static alongside the shriek
    this.playStaticNoise(1.5, 0.45);
  }

  /**
   * Sound effect for switching the desk lamp
   */
  playLampToggle() {
    if (!this.ctx || this.ctx.state === 'suspended') return;
    
    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    
    osc.type = 'sine';
    // Metal toggle click
    osc.frequency.setValueAtTime(800, now);
    osc.frequency.exponentialRampToValueAtTime(100, now + 0.02);
    
    gain.gain.setValueAtTime(0.4, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 0.025);
    
    osc.connect(gain);
    gain.connect(this.masterGain);
    
    osc.start(now);
    osc.stop(now + 0.03);
  }

  /**
   * Squeaking door opening/closing
   */
  playDoorSqueak() {
    if (!this.ctx || this.ctx.state === 'suspended') return;
    
    const now = this.ctx.currentTime;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    
    // High-pitched squeal that modulates
    osc.type = 'triangle';
    osc.frequency.setValueAtTime(880, now);
    osc.frequency.linearRampToValueAtTime(720, now + 0.8);
    osc.frequency.exponentialRampToValueAtTime(200, now + 1.2);
    
    // Wobble effect
    const modulator = this.ctx.createOscillator();
    modulator.frequency.setValueAtTime(6, now);
    const modGain = this.ctx.createGain();
    modGain.gain.setValueAtTime(40, now);
    
    modulator.connect(modGain);
    modGain.connect(osc.frequency);
    
    gain.gain.setValueAtTime(0.0, now);
    gain.gain.linearRampToValueAtTime(0.18, now + 0.15);
    gain.gain.exponentialRampToValueAtTime(0.001, now + 1.2);
    
    osc.connect(gain);
    gain.connect(this.masterGain);
    
    modulator.start(now);
    osc.start(now);
    
    modulator.stop(now + 1.3);
    osc.stop(now + 1.3);
  }

  /**
   * Sound of writing on locker / notebook paper rip
   */
  playPaperRustle() {
    if (!this.ctx || this.ctx.state === 'suspended') return;
    
    const now = this.ctx.currentTime;
    const duration = 0.45;
    const bufferSize = this.ctx.sampleRate * duration;
    const buffer = this.ctx.createBuffer(1, bufferSize, this.ctx.sampleRate);
    const data = buffer.getChannelData(0);
    
    // Generate soft crunch/rustle using filtered random numbers
    for (let i = 0; i < bufferSize; i++) {
      data[i] = (Math.random() * 2 - 1) * (1 - (i / bufferSize));
    }
    
    const noise = this.ctx.createBufferSource();
    noise.buffer = buffer;
    
    const hp = this.ctx.createBiquadFilter();
    hp.type = 'highpass';
    hp.frequency.setValueAtTime(2500, now);
    
    const gain = this.ctx.createGain();
    gain.gain.setValueAtTime(0.2, now);
    gain.gain.exponentialRampToValueAtTime(0.001, now + duration);
    
    noise.connect(hp);
    hp.connect(gain);
    gain.connect(this.masterGain);
    
    noise.start(now);
    noise.stop(now + duration);
  }
}

// Export single global instance
window.gameAudio = new AudioEngine();
