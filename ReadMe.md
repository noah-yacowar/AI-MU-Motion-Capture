# Motion Capture System

A modular motion capture platform that uses **custom ESP8266 + BNO080 IMU trackers** to stream real-time orientation data into a **Unity application**.  
Designed for multi-participant sessions, the system supports calibration, live skeleton visualization, and data export for post-analysis.

---

## ✨ Features
- **Custom IMU Trackers**  
  - ESP8266 (Wemos D1 Mini) with BNO080 sensor  
  - Wi-Fi UDP streaming at 100 Hz  
  - Calibration for mounting error and yaw alignment  

- **Unity Application**  
  - Manages multiple tracked participants  
  - Real-time skeleton visualization with custom `SkeletonManager`  
  - Session management: start, stop, calibrate  
  - Export to **BVH** and **FBX** for use in Blender, Maya, etc.  

- **Calibration Pipeline**  
  - Baseline hip yaw alignment  
  - Pitch/roll mounting error correction  
  - Per-sensor calibration offsets stored and reapplied  

- **Data Pipeline**  
  - Live UDP reception from trackers  
  - Per-participant IMU mapping to skeleton joints  
  - BVH/FBX export for post-processing and animation pipelines  

---

## 🛠 Hardware
- **ESP8266 (Wemos D1 Mini)**  
- **BNO080 / BNO085 IMU sensor**  
- 3D-printed cases with adjustable mounting  

---

## 📂 Project Structure
