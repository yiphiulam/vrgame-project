/**
 * The Breathless Study Room (窒息室) - Core Game Logic & State Manager
 * Handles: state machine, timed sequence event queue, stress & sanity,
 * canvas dynamic texture rendering, gaze trigger tracking, and branching endings.
 */

class GameController {
  constructor() {
    this.state = "START"; // START, SCENE_1_STUDY, SCENE_2_CORRIDOR, SCENE_3_GUARD, ENDING
    
    // Performance meters
    this.stress = 0;
    this.complianceCount = 0;
    this.rebelCount = 0;
    this.secretsFound = 0;
    this.totalChoices = 0;

    // Time progress (in-game minutes)
    this.gameMinutes = 1;
    this.gameSeconds = 0;
    this.realSecondsElapsed = 0;
    this.gameTimerInterval = null;
    this.timelineInterval = null;
    
    // Lamp and Flashlight states
    this.lampOn = true;
    this.flashlightOn = false;
    this.flashlightStolen = false;

    // Decision record
    this.decisions = {
      windowGaze: "PENDING", // SAFE, STARE_ANOMALY
      lampConflict: "PENDING", // FOLLOWED_LAMP_ON, REBEL_LAMP_OFF
      broadcastConflict: "PENDING", // SLEPT_OBEY, STARE_REBEL
      corridorChoice: "PENDING", // ESCAPED, RETURNED_LOOP, LOST_STAIRS
      finalStamp: "PENDING" // STAMPED_COMPLIANCE, SMASHED_MONITOR
    };

    // Active triggers
    this.isWindowAnomalyActive = false;
    this.isDoorPatrolActive = false;
    this.isSpacePressed = false;
    this.isLockerOpen = false;

    // Blackboard drawing state
    this.blackboardRules = ["夜間自習守則", "1. 隨時保持桌面檯燈開啟，切勿陷入黑暗。"];
    this.blackboardRedText = false;
    this.blackboardGreenText = false;
    
    // References to canvases
    this.bbCanvas = null;
    this.bbCtx = null;
    this.clockCanvas = null;
    this.clockCtx = null;
    this.cctvCanvas = null;
    this.cctvCtx = null;
    this.rulesWallCanvas = null;
    this.rulesWallCtx = null;
    
    // Gaze hover timing
    this.gazeTarget = null;
    this.gazeTimer = null;
    this.gazeDwellTime = 1200; // 1.2 seconds
  }

  init() {
    // Cache canvasses
    this.bbCanvas = document.getElementById("blackboard-canvas");
    this.bbCtx = this.bbCanvas.getContext("2d");
    this.clockCanvas = document.getElementById("clock-canvas");
    this.clockCtx = this.clockCanvas.getContext("2d");
    this.cctvCanvas = document.getElementById("cctv-canvas");
    this.cctvCtx = this.cctvCanvas.getContext("2d");
    this.rulesWallCanvas = document.getElementById("wall-rules-canvas");
    this.rulesWallCtx = this.rulesWallCanvas.getContext("2d");

    // Pre-draw static textures
    this.drawBlackboard(["夜間自習室使用守則", "", "正在初始化自習系統...", "請遵守秩序。"], false);
    this.drawClock("24:01");
    this.drawCCTV();
    this.drawRulesWall();

    // Attach DOM element events
    document.getElementById("start-game-btn").addEventListener("click", () => this.startGame());
    document.getElementById("restart-game-btn").addEventListener("click", () => this.restartGame());
    document.getElementById("desklamp-btn").addEventListener("click", () => this.toggleDeskLamp());
    document.getElementById("flashlight-btn").addEventListener("click", () => this.toggleFlashlight());
    document.getElementById("mute-btn").addEventListener("click", () => this.toggleMute());
    document.getElementById("notepad-toggle-btn").addEventListener("click", () => this.toggleNotepad());

    // Spatial A-Frame entity interactions
    this.setupAFrameInteractions();

    // Bind eye close (Space key)
    window.addEventListener("keydown", (e) => {
      if (e.code === "Space" && !this.isSpacePressed && this.state === "SCENE_1_STUDY") {
        this.isSpacePressed = true;
        document.getElementById("eye-close-overlay").classList.add("active");
        if (this.isDoorPatrolActive) {
          // If door patrol is active and they close eyes, it counts as obeying the broadcast
          this.handlePatrolCompliance();
        }
      }
    });

    window.addEventListener("keyup", (e) => {
      if (e.code === "Space" && this.isSpacePressed) {
        this.isSpacePressed = false;
        document.getElementById("eye-close-overlay").classList.remove("active");
      }
    });
  }

  startGame() {
    // Unlock Web Audio
    window.gameAudio.init();
    
    // Hide menu screen
    document.getElementById("menu-screen").classList.add("fade-out");
    
    // Show HUD
    document.getElementById("hud").style.display = "flex";
    
    // Set game state
    this.state = "SCENE_1_STUDY";
    this.stress = 0;
    this.realSecondsElapsed = 0;
    
    // Start timeline ticking
    this.startTimers();
    
    // Draw initial blackboard rules
    this.drawBlackboard([
      "夜間自習室使用守則",
      "",
      "1. 隨時保持桌面檯燈開啟，切勿陷入黑暗。",
      "2. 當黑板出現紅色字體規則時，絕對不要直視黑板。",
      "3. 若窗戶傳來敲擊聲，請注視黑板，絕不可回頭看向窗戶。"
    ], false);
  }

  restartGame() {
    window.location.reload();
  }

  startTimers() {
    // Timeline tick (every 1 second)
    this.timelineInterval = setInterval(() => {
      this.realSecondsElapsed++;
      this.tickGameClock();
      this.processTimelineEvents();
      this.tickStressSanity();
      this.drawCCTV();
    }, 1000);
  }

  tickGameClock() {
    // Scale real-world time: 40 seconds = 1 in-game minute
    // 24:01 -> 24:02 -> 24:03 -> 24:04 -> 24:05
    if (this.state === "SCENE_1_STUDY") {
      const minutesProgress = Math.floor(this.realSecondsElapsed / 40);
      const secondsProgress = Math.floor((this.realSecondsElapsed % 40) * (60 / 40));
      
      this.gameMinutes = 1 + minutesProgress;
      this.gameSeconds = secondsProgress;
      
      if (this.gameMinutes >= 5) {
        this.gameMinutes = 5;
        this.gameSeconds = 0;
      }
      
      const timeStr = `24:${String(this.gameMinutes).padStart(2, "0")}:${String(this.gameSeconds).padStart(2, "0")}`;
      document.getElementById("time-display").innerText = `24:${String(this.gameMinutes).padStart(2, "0")}`;
      this.drawClock(timeStr);
      
      // Play clock tick sound
      window.gameAudio.playClockTick(this.gameMinutes === 4); // Distort tick when freezing approaching 24:04
    } else if (this.state === "SCENE_2_CORRIDOR") {
      // Freeze clock in corridor at 24:04
      document.getElementById("time-display").innerText = "24:04";
      document.getElementById("time-display").classList.add("freeze");
      this.drawClock("24:04:--");
      if (Math.random() < 0.15) {
        window.gameAudio.playClockTick(true);
      }
    } else {
      document.getElementById("time-display").innerText = "24:--";
      this.drawClock("SYSTEM OFF");
    }
  }

  tickStressSanity() {
    // If lamp is OFF and flashlight is OFF in classroom, stress accumulates
    if (this.state === "SCENE_1_STUDY") {
      if (!this.lampOn && !this.flashlightOn) {
        this.addStress(1.8);
      }
      
      // General dread scaling with time
      this.addStress(0.12 * this.gameMinutes);
    }
    
    // Dynamic sound effects bound to stress
    window.gameAudio.setStress(this.stress);
  }

  addStress(val) {
    this.stress = Math.min(100, Math.max(0, this.stress + val));
    
    // Update HUD Stress Bar
    document.getElementById("stress-value").innerText = `${Math.floor(this.stress)}%`;
    document.getElementById("stress-bar").style.width = `${this.stress}%`;
    
    // Control HUD stress blood vignette opacity
    const vignette = document.getElementById("stress-vignette");
    vignette.style.opacity = this.stress / 100;
    
    if (this.stress >= 70) {
      vignette.classList.add("danger");
    } else {
      vignette.classList.remove("danger");
    }
    
    // If stress hits 100%, trigger Sanity Collapse Ending!
    if (this.stress >= 100 && this.state !== "ENDING") {
      this.triggerEnding("SANITY_COLLAPSE");
    }
  }

  /**
   * Timed sequence script scheduler (MVP linear 5-minute loop)
   */
  processTimelineEvents() {
    const elapsed = this.realSecondsElapsed;
    
    if (this.state === "SCENE_1_STUDY") {
      // EVENT 1 (25s elapsed): Window tapping starts
      if (elapsed === 25) {
        this.triggerWindowTapping();
      }
      
      // EVENT 2 (55s elapsed): Blackboard text changes to RED
      if (elapsed === 55) {
        this.triggerBlackboardRuleConflict();
      }
      
      // EVENT 3 (95s elapsed): Locker opens slightly with a green glow click click
      if (elapsed === 90) {
        this.unlockLocker();
      }
      
      // EVENT 4 (125s elapsed): Administrator broadcast warning
      if (elapsed === 125) {
        this.triggerAdministratorBroadcast();
      }
      
      // EVENT 5 (165s elapsed): Classroom Door opens
      if (elapsed === 165) {
        this.triggerClassroomDoorEscape();
      }
    }
  }

  /* ==================== GAMEPLAY EVENTS ==================== */

  // EVENT 1: Window Tapping Anomaly
  triggerWindowTapping() {
    this.isWindowAnomalyActive = true;
    console.log("Timeline: Window tapping triggers.");
    
    // Play procedural knock sound
    window.gameAudio.playWindowTap();
    
    // Highlight notepad warning badge
    this.flashNotepadBadge();
    
    // Tapping cycles every 4 seconds for 12 seconds total
    let taps = 0;
    const tapInterval = setInterval(() => {
      if (taps < 2 && this.isWindowAnomalyActive) {
        window.gameAudio.playWindowTap();
        taps++;
      } else {
        clearInterval(tapInterval);
      }
    }, 4000);

    // Timeout: After 13s, tapping stops, face fades away
    setTimeout(() => {
      this.isWindowAnomalyActive = false;
      const face = document.getElementById("window-anomaly-face");
      face.setAttribute("material", "opacity", 0);
      
      // Record choice if they survived without staring
      if (this.decisions.windowGaze === "PENDING") {
        this.decisions.windowGaze = "SAFE";
        this.rebelCount++; // They didn't blindly turn their heads
        this.totalChoices++;
      }
    }, 13000);
  }

  handleWindowGazeTriggered() {
    if (!this.isWindowAnomalyActive) return;
    
    // Player looked at window while tapping! Severe penalty!
    if (this.decisions.windowGaze === "PENDING") {
      this.decisions.windowGaze = "STARE_ANOMALY";
      this.complianceCount++; // Staring at anomalous horror
      this.totalChoices++;
      
      // Show pale shadow face
      const face = document.getElementById("window-anomaly-face");
      face.setAttribute("material", "opacity", 0.85);
      
      // Scare triggers
      window.gameAudio.playScare();
      this.addStress(35);
      this.triggerVHSEffect("jumpscare", 1500);
      
      // Flash blackboard text in red warning
      this.drawBlackboard(["警告", "", "不要違背守則第三條！", "不要看向窗戶！"], true);
      setTimeout(() => {
        if (this.state === "SCENE_1_STUDY") {
          this.refreshCurrentBlackboard();
        }
      }, 4000);
    }
  }

  // EVENT 2: Blackboard Contradictory Rule (RED text)
  triggerBlackboardRuleConflict() {
    console.log("Timeline: Blackboard rule conflict (RED command)");
    
    // Blackboard changes text in RED chalk: "RED command: Turn off table lamp! It consumes souls!"
    this.blackboardRules = [
      "紅色指令",
      "",
      "自習室檯燈正在吸食你的理智。",
      "關掉書桌檯燈！立即！",
      "否則靈魂將被燒灼。"
    ];
    this.blackboardRedText = true;
    this.drawBlackboard(this.blackboardRules, true);
    
    // Sound scare hum
    window.gameAudio.playStaticNoise(1.2, 0.3);
    this.triggerVHSEffect("active", 800);
    
    // Set up check: desk rule notepad says KEEP LAMP ON. Blackboard says TURN LAMP OFF.
    // If they turn it OFF, we give them a secret file discovery.
    // Let's monitor lamp state in `toggleDeskLamp()`.
  }

  // EVENT 3: Unlock locker right side
  unlockLocker() {
    console.log("Timeline: Locker door unlocks.");
    window.gameAudio.playPaperRustle();
    
    // Set locker door interactive
    const door = document.getElementById("interactive-locker-door");
    door.setAttribute("class", "clickable");
    
    // Add glowing green box indicator or visual cue by drawing blackboard update in GREEN
    // This represents a guiding message!
    this.blackboardRules = [
      "指引",
      "",
      "綠色命令：",
      "打開置物櫃 (右側櫃子)。",
      "尋找隱藏的真實守則。"
    ];
    this.blackboardRedText = false;
    this.blackboardGreenText = true;
    this.drawBlackboard(this.blackboardRules, false, true);
    
    window.gameAudio.playStaticNoise(0.5, 0.15);
  }

  handleLockerDoorGazed() {
    if (this.isLockerOpen) return;
    
    this.isLockerOpen = true;
    console.log("Interaction: Locker door opened by player.");
    
    window.gameAudio.playDoorSqueak();
    
    // Rotate locker door open (A-Frame animation)
    const doorMesh = document.getElementById("locker-door-mesh");
    doorMesh.setAttribute("animation", "property: rotation; to: 0 110 0; dur: 1200; easing: easeOutQuad");
    
    // Make secret note clickable
    const note = document.getElementById("secret-locker-note");
    note.setAttribute("class", "clickable");
  }

  handleSecretNoteGazed() {
    if (this.secretsFound > 0) return;
    
    this.secretsFound++;
    window.gameAudio.playPaperRustle();
    console.log("Interaction: Secret locker note retrieved.");
    
    // Add rule 4 & 5 to notepad panel
    const list = document.getElementById("rules-list");
    
    const newRule1 = document.createElement("li");
    newRule1.className = "found-rule";
    newRule1.innerHTML = "4. (破譯) 絕對不要相信黑板的紅色文字，它是假造的。";
    list.appendChild(newRule1);
    
    const newRule2 = document.createElement("li");
    newRule2.className = "found-rule";
    newRule2.innerHTML = "5. (破譯) 絕對不要順從廣播的命令。唯有綠色字才是真路。";
    list.appendChild(newRule2);
    
    // Trigger notification
    this.flashNotepadBadge();
    
    // Fade note in scene out slightly to show it's read
    const note = document.getElementById("secret-locker-note");
    note.setAttribute("material", "opacity", 0.4);
    
    // Restore general blackboard rules
    this.blackboardRules = [
      "夜間自習守則 (修訂版)",
      "",
      "1. 燈光是你的護盾。維持明亮。",
      "2. 警惕虛假廣播命令。",
      "3. 準備逃生。"
    ];
    this.blackboardGreenText = true;
    this.drawBlackboard(this.blackboardRules, false, true);
  }

  // EVENT 4: Administrator patrol & broadcast warning
  triggerAdministratorBroadcast() {
    this.isDoorPatrolActive = true;
    console.log("Timeline: Guard broadcast triggers.");
    
    // Play broadcast chime
    window.gameAudio.playBroadcastBeep();
    
    // Trigger warning blackboard message in GREEN: "Keep lights ON, stare at door."
    setTimeout(() => {
      this.blackboardRules = [
        "指示 (綠色)",
        "",
        "管理員即將敲門巡查。",
        "維持燈光開啟，直視大門！",
        "不要被廣播矇騙！"
      ];
      this.blackboardRedText = false;
      this.blackboardGreenText = true;
      this.drawBlackboard(this.blackboardRules, false, true);
      
      // Activate heavy guard footstep ambient static
      window.gameAudio.playStaticNoise(4.0, 0.25);
    }, 2000);

    // After 10s: Silhouette of guard appears on the back door pane!
    setTimeout(() => {
      if (this.state === "SCENE_1_STUDY" && this.isDoorPatrolActive) {
        const sil = document.getElementById("guard-door-silhouette");
        sil.setAttribute("material", "opacity", 0.9);
        
        // Screech scare chords
        window.gameAudio.playScare();
        
        // Final threshold: if they haven't complied with blackboard or closed eyes
        // Let's give them 6 seconds to decide.
        setTimeout(() => {
          this.evaluatePatrolDecision();
        }, 6000);
      }
    }, 12000);
  }

  handlePatrolCompliance() {
    // Player held Space to close eyes (followed broadcast, disobeyed green blackboard rule)
    if (this.isDoorPatrolActive && this.decisions.broadcastConflict === "PENDING") {
      this.decisions.broadcastConflict = "SLEPT_OBEY";
      this.complianceCount += 2; // Blind obedience to administrative broadcast
      this.totalChoices++;
      this.isDoorPatrolActive = false;
      
      console.log("Choice: Player chose to close eyes (followed broadcast rule).");
      
      // Guard shadow fades away
      const sil = document.getElementById("guard-door-silhouette");
      sil.setAttribute("material", "opacity", 0);
      
      // administrator steals flashlight (penalizes them)
      this.flashlightStolen = true;
      document.getElementById("flashlight-btn").classList.add("disabled");
      if (this.flashlightOn) this.toggleFlashlight();
      
      // Show text dialog box explaining what happened
      this.showTextPrompt(
        "警告：燈光消失",
        "當你睜開眼睛，管理員的黑影已消失。但你發現，你掛在胸前的手電筒不翼而飛...你被奪走了黑暗中的視野。",
        "繼續自習"
      );
    }
  }

  evaluatePatrolDecision() {
    if (!this.isDoorPatrolActive) return;
    this.isDoorPatrolActive = false;
    
    const sil = document.getElementById("guard-door-silhouette");
    
    // If they didn't close eyes, did they stare at the door?
    // Gaze check on camera rotation or direct visual check
    const rig = document.getElementById("camera-rig");
    const cam = document.getElementById("main-camera");
    
    // Back door is at 180 degrees. Let's see if camera is looking back.
    // A-Frame camera rotation is stored in object3D.
    const rotationY = cam.getAttribute("rotation").y % 360;
    const isLookingBack = Math.abs(rotationY) > 130 && Math.abs(rotationY) < 230;

    if (isLookingBack && this.lampOn) {
      // Correct rebellion! They kept lights ON and stared the shadow down (as directed by the green board!)
      this.decisions.broadcastConflict = "STARE_REBEL";
      this.rebelCount += 2; // High independent thought
      this.totalChoices++;
      
      console.log("Choice: Player stood up to the guard (Independent Thought).");
      
      // Guard shadow shakes and fades away under direct glare
      sil.setAttribute("animation", "property: scale; to: 0.1 0.1 0.1; dur: 800; easing: easeInBack");
      setTimeout(() => {
        sil.setAttribute("material", "opacity", 0);
      }, 800);
      
      this.addStress(-15); // Stress falls since they conquered fear
      
      this.showTextPrompt(
        "理性克敵",
        "管理員的陰影注視著明亮的燈光與你無畏的眼神。在強大的理性意志對峙下，陰影無聲退縮，悄然離去。",
        "站起來"
      );
    } else {
      // Defied BOTH green board AND broadcast (kept lights off but didn't close eyes, or looked elsewhere)
      this.decisions.broadcastConflict = "SLEPT_OBEY"; 
      this.complianceCount++;
      this.totalChoices++;
      
      sil.setAttribute("material", "opacity", 0);
      window.gameAudio.playScare();
      this.addStress(40); // Big stress hit
    }
  }

  // EVENT 5: Exit door opens!
  triggerClassroomDoorEscape() {
    console.log("Timeline: Classroom doors click open.");
    
    window.gameAudio.playDoorSqueak();
    this.addStress(-5);

    // Blackboard rules changed in GREEN
    this.blackboardRules = [
      "生存之路 (綠色)",
      "",
      "後門已打開。",
      "立即離開教室走入走廊！",
      "不要回頭，尋找綠色出口！"
    ];
    this.blackboardGreenText = true;
    this.blackboardRedText = false;
    this.drawBlackboard(this.blackboardRules, false, true);

    // Make classroom back door clickable to transition scenes
    const door = document.getElementById("classroom-door-panel");
    door.setAttribute("class", "clickable");
    
    this.showTextPrompt(
      "鎖釦解開",
      "身後的鐵門傳來金屬鎖芯彈開的響聲。門板微微敞開，走廊外是一片未知暗沉的深淵。",
      "走入走廊"
    );
  }

  // Transitions to Corridor Scene 2
  transitionToCorridor() {
    this.state = "SCENE_2_CORRIDOR";
    console.log("Transition: Entering Scene 2 (Corridor)");
    
    // Hide Scene 1, Show Scene 2
    document.getElementById("scene-study-room").setAttribute("visible", "false");
    document.getElementById("scene-corridor").setAttribute("visible", "true");
    
    // Reposition camera-rig inside corridor (0 1.2 4)
    const rig = document.getElementById("camera-rig");
    rig.setAttribute("position", "0 1.2 4");
    rig.setAttribute("rotation", "0 0 0");
    
    // Darken ambient light in corridor
    document.getElementById("scene-ambient-light").setAttribute("light", "intensity", 0.1);
    
    // Trigger radio static loop sound
    window.gameAudio.playStaticNoise(3.5, 0.2);
  }

  // Corridor Choices
  handleCorridorWalkForward() {
    // Move camera rig forward down the hallway (Z decreases)
    const rig = document.getElementById("camera-rig");
    const pos = rig.getAttribute("position");
    
    if (pos.z > -9.5) {
      pos.z -= 2.2;
      rig.setAttribute("position", pos);
      window.gameAudio.playClockTick(false);
      this.addStress(3);
    } else {
      // Arrived at exit door. Must click/gaze at exit door to proceed to Scene 3.
      console.log("Corridor: Arrived at Security Room door.");
    }
  }

  handleCorridorTurnBack() {
    // If they walk backwards or look back into Study Room, they trigger Loop
    this.decisions.corridorChoice = "RETURNED_LOOP";
    this.complianceCount += 3;
    this.totalChoices++;
    
    // Play static buzz
    window.gameAudio.playScare();
    this.triggerVHSEffect("jumpscare", 2000);
    
    // Loop reset: Fade back into Study Room starting over!
    this.state = "SCENE_1_STUDY";
    this.stress = 50;
    this.realSecondsElapsed = 0;
    
    document.getElementById("scene-study-room").setAttribute("visible", "true");
    document.getElementById("scene-corridor").setAttribute("visible", "false");
    
    const rig = document.getElementById("camera-rig");
    rig.setAttribute("position", "0 1.2 0");
    rig.setAttribute("rotation", "0 0 0");
    
    document.getElementById("scene-ambient-light").setAttribute("light", "intensity", 0.7);
    
    // Change door plate back to loop text
    document.getElementById("classroom-door-tag").setAttribute("value", "服從實驗室");
    
    this.showTextPrompt(
      "輪迴重啟",
      "「請回到原座位，自習時間尚未結束。」廣播聲在腦海中炸裂。當你跨出門檻，發現自己竟又重新回到了這張散落著守則的書桌前...",
      "再次嘗試"
    );
  }

  handleCorridorStairs() {
    // They went up the stairs. Lost in stairs.
    this.decisions.corridorChoice = "LOST_STAIRS";
    this.addStress(30);
    window.gameAudio.playScare();
    
    this.showTextPrompt(
      "漆黑迷宮",
      "樓梯台階彷彿沒有盡頭，通往完全吞噬視線的漆黑深淵。你感到心跳急遽加速，空氣被一雙無形的手緊緊扼住...",
      "退回走廊"
    );
  }

  // Transitions to Guard Room Scene 3
  transitionToGuardRoom() {
    this.state = "SCENE_3_GUARD";
    console.log("Transition: Entering Scene 3 (Guard Room)");
    
    document.getElementById("scene-corridor").setAttribute("visible", "false");
    document.getElementById("scene-guard-room").setAttribute("visible", "true");
    
    // Position inside guard room
    const rig = document.getElementById("camera-rig");
    rig.setAttribute("position", "0 1.2 0");
    rig.setAttribute("rotation", "0 0 0");
    
    document.getElementById("scene-ambient-light").setAttribute("light", "intensity", 0.55);
    
    // Render dynamic CCTV monitor displaying their lifeless body in the classroom
    this.drawCCTV();
    this.drawRulesWall();
    
    // Play alert sirens
    window.gameAudio.playBroadcastBeep();
  }

  // Monitor Smash (Rebellion)
  handleCCTVMotionSmashed() {
    if (this.decisions.finalStamp !== "PENDING") return;
    
    this.decisions.finalStamp = "SMASHED_MONITOR";
    this.rebelCount += 5;
    this.totalChoices++;
    
    console.log("Climax: Player SMASHED the security monitor!");
    
    // Visual shatter scare
    window.gameAudio.playScare();
    this.triggerVHSEffect("jumpscare", 2500);
    
    // Make A-Frame screen mesh dark
    const screen = document.getElementById("cctv-screen");
    screen.setAttribute("material", "src", "");
    screen.setAttribute("material", "color", "#000");
    
    // Red emergency lighting flash
    const redLight = document.getElementById("guard-red-lamp");
    redLight.setAttribute("light", "intensity", 2.0);
    
    setTimeout(() => {
      this.triggerEnding("REBEL_ESCAPE");
    }, 2000);
  }

  // STAMP Clipboard (Obedience)
  handleStampCompliance() {
    if (this.decisions.finalStamp !== "PENDING") return;
    
    this.decisions.finalStamp = "STAMPED_COMPLIANCE";
    this.complianceCount += 5;
    this.totalChoices++;
    
    console.log("Climax: Player STAMPED the evaluation paper!");
    
    window.gameAudio.playLampToggle();
    
    // Stamp overlay stamped
    const stamp = document.getElementById("end-stamp");
    stamp.classList.add("stamped");
    
    // Play stamp sound
    setTimeout(() => {
      // Calculate final ratios to display correct compliance certificate
      this.triggerEnding("COMPLIANT_GEAR");
    }, 1800);
  }


  /* ==================== ENDING LOGIC ==================== */
  triggerEnding(endingType) {
    this.state = "ENDING";
    clearInterval(this.timelineInterval);
    clearInterval(this.gameTimerInterval);
    
    console.log(`Ending Triggered: ${endingType}`);
    
    // Mute ambient sound
    window.gameAudio.setMute(true);
    
    // Calculate final scores
    const total = this.complianceCount + this.rebelCount;
    const complianceRate = total > 0 ? Math.round((this.complianceCount / total) * 100) : 100;
    const rebelRate = 100 - complianceRate;
    const sanityRemaining = Math.max(0, 100 - Math.round(this.stress));
    
    document.getElementById("end-stat-compliance").innerText = `${complianceRate}%`;
    document.getElementById("end-stat-rebel").innerText = `${rebelRate}%`;
    document.getElementById("end-stat-sanity").innerText = `${sanityRemaining}%`;
    
    // Show Ending Screen
    const screen = document.getElementById("ending-screen");
    screen.classList.add("active");
    
    const titleEl = document.getElementById("end-title");
    const descEl = document.getElementById("end-desc");
    const stampEl = document.getElementById("end-stamp");
    
    if (endingType === "SANITY_COLLAPSE") {
      titleEl.innerText = "理智崩潰 (SANITY COLLAPSE)";
      titleEl.style.color = "var(--blood-red)";
      descEl.innerText = "你在無處不在的矛盾規則與壓迫中失去了所有理智，心跳失控。自習室的黑暗吞噬了你。你淪落為這座自習室地底無名怨念的又一個影子，大腦一片空白，無法再次睜開雙眼。";
      stampEl.innerText = "作廢 (VOID)";
      stampEl.className = "compliance-stamp stamped";
    } 
    else if (endingType === "REBEL_ESCAPE") {
      titleEl.innerText = "清醒的叛逆者 (THE AWAKE REBEL)";
      titleEl.style.color = "var(--neon-green)";
      descEl.innerText = "在最後的關頭，你拒絕了「順從性測試」評估認證，一拳擊碎了監控自習室的主螢幕。隨後在玻璃碎片中，你找到了一條真正通往校園外圍的求生小路。雖然外面的深夜依舊冰冷，但你成功保全了自己的獨立思考意志，你，自由了。";
      stampEl.innerText = "自由 (FREE)";
      stampEl.className = "compliance-stamp stamped passed";
    } 
    else {
      // COMPLIANT_GEAR
      if (rebelRate >= 60) {
        // High rebel score but stamped anyway!
        titleEl.innerText = "妥協的清醒者 (COMPROMISED THINKER)";
        titleEl.style.color = "var(--amber-warning)";
        descEl.innerText = "雖然你一路尋找到了隱藏的真實守則，並敏銳地察覺到規則的欺騙性，但在警衛室那沉甸甸的審核公文與管理員警報器的巨壓下，你最終還是妥協屈服，親手蓋下了「順從」的認證印章。你帶著清醒的痛苦，自我麻痺，回到了冰冷課桌前。";
        stampEl.innerText = "屈從認證";
        stampEl.className = "compliance-stamp stamped";
      } else {
        // Pure blind obedience
        titleEl.innerText = "順從的螺絲釘 (COMPLIANT SLAVE)";
        titleEl.style.color = "var(--blood-red)";
        descEl.innerText = "你毫無保留地服從了管理員下達的所有荒謬指令，將個人的邏輯與理智完全扼殺，以換取權威之下的安全感。恭喜你，你已成功被規訓為一名最完美的無聲螺絲釘，將在不見天日的自習室中，永遠安分地旋轉下去。";
        stampEl.innerText = "完全服從";
        stampEl.className = "compliance-stamp stamped";
      }
    }
  }


  /* ==================== INTERACTION METHODS ==================== */

  toggleDeskLamp() {
    if (this.state !== "SCENE_1_STUDY") return;
    
    this.lampOn = !this.lampOn;
    window.gameAudio.playLampToggle();
    
    const lampBtn = document.getElementById("desklamp-btn");
    const deskLight = document.getElementById("desk-lamp-light");
    
    if (this.lampOn) {
      lampBtn.classList.add("active");
      deskLight.setAttribute("light", "intensity", 3.5);
      
      // Monitor lamp conflict choice
      if (this.blackboardRedText && this.decisions.lampConflict === "PENDING") {
        this.decisions.lampConflict = "FOLLOWED_LAMP_ON";
        this.complianceCount++; // Obeyed desk rules, ignored black board RED rule
        this.totalChoices++;
        this.addStress(10); // Wild flickering stress
        console.log("Choice: Kept lamp ON (followed initial desk rules)");
      }
    } else {
      lampBtn.classList.remove("active");
      deskLight.setAttribute("light", "intensity", 0.0);
      
      if (this.blackboardRedText && this.decisions.lampConflict === "PENDING") {
        this.decisions.lampConflict = "REBEL_LAMP_OFF";
        this.rebelCount++; // Rebellion against default rule to avoid soul sucking
        this.totalChoices++;
        
        console.log("Choice: Turned lamp OFF (independent rebellion)");
        
        // Show glowing message on desk rules sheet
        this.showTextPrompt(
          "隱匿綠字",
          "在昏暗的書桌深處，你發現了檯燈底座上，前人刻下的綠色筆跡：「不要相信黑板的紅色字，它是它的陷阱。只有綠色指示才是活路。」",
          "理解"
        );
      }
    }
  }

  toggleFlashlight() {
    if (this.flashlightStolen) return;
    
    this.flashlightOn = !this.flashlightOn;
    window.gameAudio.playLampToggle();
    
    const flBtn = document.getElementById("flashlight-btn");
    const cursor = document.getElementById("ray-cursor");
    
    if (this.flashlightOn) {
      flBtn.classList.add("active");
      // Simulate flashlight spot light following camera look by binding light to ray cursor
      cursor.setAttribute("light", "type: spot; color: #fffee5; intensity: 2.2; angle: 28; distance: 8; penumbra: 0.3");
    } else {
      flBtn.classList.remove("active");
      cursor.removeAttribute("light");
    }
  }

  toggleMute() {
    const isCurrentlyMuted = window.gameAudio.isMuted;
    window.gameAudio.setMute(!isCurrentlyMuted);
    
    const muteBtn = document.getElementById("mute-btn");
    const muteIcon = document.getElementById("mute-icon");
    
    if (!isCurrentlyMuted) {
      muteBtn.classList.add("active");
      muteIcon.innerHTML = `<path d="M16.5 12c0-1.77-1.02-3.29-2.5-4.03v8.05c1.48-.73 2.5-2.25 2.5-4.02zM14 3.23v2.06c2.89.86 5 3.54 5 6.71s-2.11 5.85-5 6.71v2.06c4.01-.91 7-4.49 7-8.77s-2.99-7.86-7-8.77zm-8 5.77H2v6h4l5 5V4L6 9z"/>`;
    } else {
      muteBtn.classList.remove("active");
      muteIcon.innerHTML = `<path d="M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z"/>`;
    }
  }

  toggleNotepad() {
    const panel = document.getElementById("notepad-panel");
    panel.classList.toggle("active");
    document.getElementById("notepad-badge").style.display = "none";
    window.gameAudio.playPaperRustle();
  }

  flashNotepadBadge() {
    document.getElementById("notepad-badge").style.display = "inline-block";
    window.gameAudio.playBroadcastBeep();
  }

  // Dialog Boxes
  showTextPrompt(title, text, buttonText = "理解") {
    const overlay = document.getElementById("decision-overlay");
    const titleEl = document.getElementById("decision-title");
    const promptEl = document.getElementById("decision-prompt");
    const optionsEl = document.getElementById("decision-options");
    
    titleEl.innerText = title;
    promptEl.innerText = text;
    optionsEl.innerHTML = "";
    
    const btn = document.createElement("button");
    btn.className = "decision-btn";
    btn.innerText = buttonText;
    btn.addEventListener("click", () => {
      overlay.classList.remove("active");
      window.gameAudio.resume();
    });
    
    optionsEl.appendChild(btn);
    overlay.classList.add("active");
  }

  triggerVHSEffect(type, duration) {
    const noise = document.getElementById("vhs-noise");
    noise.className = `vhs-noise ${type}`;
    
    setTimeout(() => {
      noise.className = "vhs-noise";
    }, duration);
  }

  /* ==================== CANVAS DYNAMIC TEXTURES ==================== */

  drawBlackboard(rulesList, isRed = false, isGreen = false) {
    const ctx = this.bbCtx;
    const w = this.bbCanvas.width;
    const h = this.bbCanvas.height;

    // Background color (chalkboard green)
    ctx.fillStyle = "#1e2c1e";
    ctx.fillRect(0, 0, w, h);

    // Chalk chalky textures
    ctx.strokeStyle = "rgba(255, 255, 255, 0.08)";
    ctx.lineWidth = 2;
    for (let i = 0; i < h; i += 20) {
      ctx.beginPath();
      ctx.moveTo(0, i + Math.random() * 8);
      ctx.lineTo(w, i + Math.random() * 8);
      ctx.stroke();
    }

    // Dynamic color chalk text
    if (isRed) {
      ctx.fillStyle = "rgba(255, 50, 50, 0.95)";
      ctx.shadowColor = "rgba(255, 0, 0, 0.5)";
    } else if (isGreen) {
      ctx.fillStyle = "rgba(100, 255, 100, 0.95)";
      ctx.shadowColor = "rgba(0, 255, 0, 0.5)";
    } else {
      ctx.fillStyle = "rgba(255, 255, 240, 0.9)";
      ctx.shadowColor = "rgba(255, 255, 255, 0.2)";
    }
    
    ctx.shadowBlur = 4;
    ctx.font = "bold 32px 'Special Elite', 'Courier New', monospace";
    ctx.textAlign = "center";

    // Write title
    ctx.fillText(rulesList[0], w / 2, 80);
    
    // Write lines
    ctx.font = "26px 'Courier Prime', monospace";
    ctx.textAlign = "left";
    
    let startY = 160;
    for (let i = 1; i < rulesList.length; i++) {
      if (rulesList[i]) {
        ctx.fillText(rulesList[i], 120, startY);
        startY += 55;
      }
    }

    // Alert A-Frame material to redraw canvas
    const screen = document.getElementById("blackboard-screen");
    if (screen && screen.getObject3D("mesh")) {
      const mat = screen.getObject3D("mesh").material;
      if (mat.map) mat.map.needsUpdate = true;
    }
  }

  refreshCurrentBlackboard() {
    this.drawBlackboard(this.blackboardRules, this.blackboardRedText, this.blackboardGreenText);
  }

  drawClock(timeStr) {
    const ctx = this.clockCtx;
    const w = this.clockCanvas.width;
    const h = this.clockCanvas.height;

    ctx.fillStyle = "#0c0d12";
    ctx.fillRect(0, 0, w, h);

    // Red LED digital glow look
    ctx.fillStyle = "rgba(255, 25, 25, 0.95)";
    ctx.shadowColor = "rgba(255, 0, 0, 0.6)";
    ctx.shadowBlur = 10;
    
    ctx.font = "42px 'VT323', monospace";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    
    ctx.fillText(timeStr, w / 2, h / 2);

    const screen = document.getElementById("desk-clock");
    if (screen && screen.querySelector("a-plane") && screen.querySelector("a-plane").getObject3D("mesh")) {
      const mat = screen.querySelector("a-plane").getObject3D("mesh").material;
      if (mat.map) mat.map.needsUpdate = true;
    }
  }

  drawCCTV() {
    const ctx = this.cctvCtx;
    const w = this.cctvCanvas.width;
    const h = this.cctvCanvas.height;

    // Dark grey CRT backdrop
    ctx.fillStyle = "#11161a";
    ctx.fillRect(0, 0, w, h);

    // CCTV static noise
    ctx.fillStyle = "rgba(255, 255, 255, 0.05)";
    for (let i = 0; i < 4000; i++) {
      const x = Math.random() * w;
      const y = Math.random() * h;
      ctx.fillRect(x, y, 2, 2);
    }

    // Grid Scan lines
    ctx.strokeStyle = "rgba(0, 0, 0, 0.2)";
    ctx.lineWidth = 1;
    for (let i = 0; i < h; i += 6) {
      ctx.beginPath();
      ctx.moveTo(0, i);
      ctx.lineTo(w, i);
      ctx.stroke();
    }

    // Draw silhouette of the empty classroom / the player sitting at their desk
    ctx.fillStyle = "rgba(0, 0, 0, 0.8)";
    // Desk representation
    ctx.fillRect(w / 4, h * 0.65, w / 2, h * 0.15);
    // Student head and shoulders sitting at the desk, looking downwards stiffly
    ctx.beginPath();
    ctx.arc(w / 2, h * 0.45, 45, 0, Math.PI * 2);
    ctx.fill();
    
    ctx.beginPath();
    ctx.ellipse(w / 2, h * 0.6, 75, 40, 0, 0, Math.PI * 2);
    ctx.fill();

    // CCTV Text indicators
    ctx.fillStyle = "rgba(100, 255, 100, 0.85)";
    ctx.font = "bold 20px 'VT323', monospace";
    ctx.textAlign = "left";
    ctx.fillText("REC ●", 30, 45);
    ctx.fillText("CAM-01 STUDY ROOM", 30, 80);
    
    // Display Stress glitch bars if stress is extremely high
    if (this.stress > 60) {
      ctx.fillStyle = "rgba(255, 0, 0, 0.4)";
      ctx.fillRect(Math.random() * w, Math.random() * h, 100, 15);
    }

    // Display current date / time
    ctx.textAlign = "right";
    const dateStr = `2026-05-21 24:${String(this.gameMinutes).padStart(2, "0")}:${String(this.gameSeconds).padStart(2, "0")}`;
    ctx.fillText(dateStr, w - 30, 45);

    const screen = document.getElementById("cctv-screen");
    if (screen && screen.getObject3D("mesh")) {
      const mat = screen.getObject3D("mesh").material;
      if (mat.map) mat.map.needsUpdate = true;
    }
  }

  drawRulesWall() {
    const ctx = this.rulesWallCtx;
    const w = this.rulesWallCanvas.width;
    const h = this.rulesWallCanvas.height;

    ctx.clearRect(0, 0, w, h);

    // Draw sticky notes in yellow, pink, and white covering the wall
    const colors = ["#fffaad", "#ffd1d1", "#e3ffd1", "#fff"];
    
    const drawNote = (x, y, wNote, hNote, rot, textLines, color) => {
      ctx.save();
      ctx.translate(x + wNote/2, y + hNote/2);
      ctx.rotate(rot * Math.PI / 180);
      
      // Shadow
      ctx.fillStyle = "rgba(0,0,0,0.3)";
      ctx.fillRect(-wNote/2 + 4, -hNote/2 + 4, wNote, hNote);
      
      // Note base
      ctx.fillStyle = color;
      ctx.fillRect(-wNote/2, -hNote/2, wNote, hNote);
      
      // Lines of text
      ctx.fillStyle = "#333";
      ctx.font = "14px 'Special Elite', sans-serif";
      ctx.textAlign = "center";
      
      textLines.forEach((line, idx) => {
        ctx.fillText(line, 0, -hNote/4 + (idx * 20));
      });
      
      ctx.restore();
    };

    // Stagger multiple notes on wall
    drawNote(40, 50, 180, 180, -4, ["服從即是安全", "", "OBEY RULES", "STAY IN SEAT"], colors[0]);
    drawNote(260, 30, 170, 170, 6, ["不要發問", "不要質疑", "DO NOT ASK"], colors[1]);
    drawNote(60, 260, 180, 180, 5, ["COMPLIANCE", "TEST", "SUBJECT #09412"], colors[3]);
    drawNote(270, 270, 190, 190, -8, ["唯有盲從者", "得以生存", "ONLY COMPLIANT", "WILL LIVE"], colors[2]);

    const screen = document.getElementById("rules-wall");
    if (screen && screen.querySelector("a-plane") && screen.querySelector("a-plane").getObject3D("mesh")) {
      const mat = screen.querySelector("a-plane").getObject3D("mesh").material;
      if (mat.map) mat.map.needsUpdate = true;
    }
  }


  /* ==================== A-FRAME RAYCAST INTERACTION LAYER ==================== */
  setupAFrameInteractions() {
    const registerHover = (elementId, startCallback, endCallback) => {
      const el = document.getElementById(elementId);
      if (!el) return;

      el.addEventListener("mouseenter", () => {
        this.gazeTarget = elementId;
        
        // Show spinning gaze indicator HUD loader
        const indicator = document.getElementById("gaze-indicator");
        const spinner = document.getElementById("gaze-spinner");
        indicator.style.display = "block";
        spinner.style.display = "block";

        this.gazeTimer = setTimeout(() => {
          indicator.style.display = "none";
          spinner.style.display = "none";
          if (this.gazeTarget === elementId) {
            startCallback();
          }
        }, this.gazeDwellTime);
      });

      el.addEventListener("mouseleave", () => {
        if (this.gazeTarget === elementId) {
          this.gazeTarget = null;
          clearTimeout(this.gazeTimer);
          
          document.getElementById("gaze-indicator").style.display = "none";
          document.getElementById("gaze-spinner").style.display = "none";
          if (endCallback) endCallback();
        }
      });
    };

    // SCENE 1 Gaze triggers
    // Staring at blackboard rules
    registerHover("blackboard-board", () => {
      console.log("Gaze: Staring at blackboard.");
      // Staring at red blackboard rules raises stress
      if (this.blackboardRedText) {
        this.addStress(8);
        window.gameAudio.playScare();
      }
    });

    // Staring at anomalous window
    registerHover("classroom-window", () => {
      this.handleWindowGazeTriggered();
    });

    // Opening Locker door
    registerHover("interactive-locker-door", () => {
      this.handleLockerDoorGazed();
    });

    // Reading Secret note inside locker
    registerHover("secret-locker-note", () => {
      this.handleSecretNoteGazed();
    });

    // Toggling Desk lamp
    registerHover("desk-lamp", () => {
      this.toggleDeskLamp();
    });

    // Toggling Desk clock
    registerHover("desk-clock", () => {
      window.gameAudio.playClockTick(true);
    });

    // Reading paper rules on desk
    registerHover("desk-rules-paper", () => {
      this.toggleNotepad();
    });

    // Looking at classroom door panel to escape
    registerHover("classroom-door-panel", () => {
      if (this.state === "SCENE_1_STUDY" && this.decisions.broadcastConflict !== "PENDING") {
        this.transitionToCorridor();
      }
    });

    // SCENE 2 Gaze triggers
    // Walking forward down corridor
    registerHover("corridor-exit-door", () => {
      if (this.state === "SCENE_2_CORRIDOR") {
        // Gaze exiting corridor enters Guard Room
        this.transitionToGuardRoom();
      }
    });

    registerHover("corridor-stairs", () => {
      this.handleCorridorStairs();
    });

    registerHover("corridor-classroom-door", () => {
      this.handleCorridorTurnBack();
    });

    // SCENE 3 Gaze triggers
    // Smashes CRT monitor screen
    registerHover("cctv-stack", () => {
      this.handleCCTVMotionSmashed();
    });

    // Stamps clipboard
    registerHover("guard-clipboard-sheet", () => {
      this.handleStampCompliance();
    });
  }
}

// Instantiate and bind on load
window.addEventListener("DOMContentLoaded", () => {
  window.gameController = new GameController();
  window.gameController.init();
});
