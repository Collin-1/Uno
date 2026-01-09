// WebRTC Voice Chat Manager
class VoiceChat {
  constructor(signalRConnection, roomId) {
    this.connection = signalRConnection;
    this.roomId = roomId;
    this.peerConnections = new Map(); // connectionId -> RTCPeerConnection
    this.localStream = null;
    this.isMuted = false;

    // STUN servers for NAT traversal
    this.iceServers = {
      iceServers: [
        { urls: "stun:stun.l.google.com:19302" },
        { urls: "stun:stun1.l.google.com:19302" },
      ],
    };

    this.setupSignalRHandlers();
  }

  async initialize() {
    try {
      console.log("Requesting microphone access...");
      // Request microphone access
      this.localStream = await navigator.mediaDevices.getUserMedia({
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true,
        },
        video: false,
      });

      console.log("Microphone access granted", this.localStream);
      this.updateMicButton(true);
      return true;
    } catch (error) {
      console.error("Failed to get microphone access:", error);
      if (
        error.name === "NotAllowedError" ||
        error.name === "PermissionDeniedError"
      ) {
        alert(
          "Microphone access denied. Please allow microphone access to use voice chat."
        );
      } else if (error.name === "NotFoundError") {
        alert(
          "No microphone found. Please connect a microphone to use voice chat."
        );
      } else {
        alert("Failed to access microphone: " + error.message);
      }
      return false;
    }
  }

  setupSignalRHandlers() {
    // Handle incoming WebRTC offers
    this.connection.on(
      "ReceiveWebRTCOffer",
      async (senderConnectionId, offer) => {
        console.log("Received WebRTC offer from:", senderConnectionId);
        await this.handleOffer(senderConnectionId, offer);
      }
    );

    // Handle incoming WebRTC answers
    this.connection.on(
      "ReceiveWebRTCAnswer",
      async (senderConnectionId, answer) => {
        console.log("Received WebRTC answer from:", senderConnectionId);
        await this.handleAnswer(senderConnectionId, answer);
      }
    );

    // Handle ICE candidates
    this.connection.on(
      "ReceiveICECandidate",
      async (senderConnectionId, candidate) => {
        console.log("Received ICE candidate from:", senderConnectionId);
        await this.handleICECandidate(senderConnectionId, candidate);
      }
    );
  }

  async createPeerConnection(targetConnectionId, isInitiator = false) {
    if (this.peerConnections.has(targetConnectionId)) {
      return this.peerConnections.get(targetConnectionId);
    }

    const pc = new RTCPeerConnection(this.iceServers);
    this.peerConnections.set(targetConnectionId, pc);

    // Add local stream tracks
    if (this.localStream) {
      this.localStream.getTracks().forEach((track) => {
        pc.addTrack(track, this.localStream);
      });
    }

    // Handle incoming remote stream
    pc.ontrack = (event) => {
      console.log("Received remote track from:", targetConnectionId);
      this.playRemoteStream(targetConnectionId, event.streams[0]);
    };

    // Handle ICE candidates
    pc.onicecandidate = (event) => {
      if (event.candidate) {
        this.connection
          .invoke(
            "SendICECandidate",
            this.roomId,
            targetConnectionId,
            JSON.stringify(event.candidate)
          )
          .catch((err) => console.error("Error sending ICE candidate:", err));
      }
    };

    // Handle connection state changes
    pc.onconnectionstatechange = () => {
      console.log(
        `Connection state with ${targetConnectionId}:`,
        pc.connectionState
      );
      if (
        pc.connectionState === "disconnected" ||
        pc.connectionState === "failed"
      ) {
        this.closePeerConnection(targetConnectionId);
      }
    };

    return pc;
  }

  async connectToPeer(targetConnectionId) {
    console.log("Initiating connection to:", targetConnectionId);
    const pc = await this.createPeerConnection(targetConnectionId, true);

    try {
      const offer = await pc.createOffer();
      await pc.setLocalDescription(offer);

      await this.connection.invoke(
        "SendWebRTCOffer",
        this.roomId,
        targetConnectionId,
        JSON.stringify(offer)
      );
    } catch (error) {
      console.error("Error creating offer:", error);
    }
  }

  async handleOffer(senderConnectionId, offerString) {
    const pc = await this.createPeerConnection(senderConnectionId, false);

    try {
      const offer = JSON.parse(offerString);
      await pc.setRemoteDescription(new RTCSessionDescription(offer));

      const answer = await pc.createAnswer();
      await pc.setLocalDescription(answer);

      await this.connection.invoke(
        "SendWebRTCAnswer",
        this.roomId,
        senderConnectionId,
        JSON.stringify(answer)
      );
    } catch (error) {
      console.error("Error handling offer:", error);
    }
  }

  async handleAnswer(senderConnectionId, answerString) {
    const pc = this.peerConnections.get(senderConnectionId);
    if (!pc) {
      console.error("No peer connection found for:", senderConnectionId);
      return;
    }

    try {
      const answer = JSON.parse(answerString);
      await pc.setRemoteDescription(new RTCSessionDescription(answer));
    } catch (error) {
      console.error("Error handling answer:", error);
    }
  }

  async handleICECandidate(senderConnectionId, candidateString) {
    const pc = this.peerConnections.get(senderConnectionId);
    if (!pc) {
      console.error("No peer connection found for:", senderConnectionId);
      return;
    }

    try {
      const candidate = JSON.parse(candidateString);
      await pc.addIceCandidate(new RTCIceCandidate(candidate));
    } catch (error) {
      console.error("Error adding ICE candidate:", error);
    }
  }

  playRemoteStream(connectionId, stream) {
    // Check if audio element already exists
    let audioElement = document.getElementById(`audio-${connectionId}`);

    if (!audioElement) {
      audioElement = document.createElement("audio");
      audioElement.id = `audio-${connectionId}`;
      audioElement.autoplay = true;
      audioElement.style.display = "none";
      document.body.appendChild(audioElement);
    }

    audioElement.srcObject = stream;
  }

  toggleMute() {
    if (!this.localStream) return;

    this.isMuted = !this.isMuted;
    this.localStream.getAudioTracks().forEach((track) => {
      track.enabled = !this.isMuted;
    });

    this.updateMicButton(!this.isMuted);
    console.log("Microphone", this.isMuted ? "muted" : "unmuted");
  }

  updateMicButton(enabled) {
    const micBtn = document.getElementById("mic-toggle");
    if (micBtn) {
      micBtn.textContent = enabled ? "🎤 Mute" : "🔇 Unmute";
      micBtn.className = enabled ? "mic-button mic-on" : "mic-button mic-off";
    }
  }

  closePeerConnection(connectionId) {
    const pc = this.peerConnections.get(connectionId);
    if (pc) {
      pc.close();
      this.peerConnections.delete(connectionId);
    }

    // Remove audio element
    const audioElement = document.getElementById(`audio-${connectionId}`);
    if (audioElement) {
      audioElement.srcObject = null;
      audioElement.remove();
    }
  }

  async connectToAllPeers(playerConnectionIds) {
    // Connect to all other players
    for (const connectionId of playerConnectionIds) {
      if (connectionId !== this.connection.connectionId) {
        await this.connectToPeer(connectionId);
      }
    }
  }

  disconnectAll() {
    // Close all peer connections
    this.peerConnections.forEach((pc, connectionId) => {
      this.closePeerConnection(connectionId);
    });

    // Stop local stream
    if (this.localStream) {
      this.localStream.getTracks().forEach((track) => track.stop());
      this.localStream = null;
    }
  }
}
