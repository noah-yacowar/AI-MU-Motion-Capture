#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include "BNO08x/BNO08x.h"
#include "DataTypes/Quaternion.h"
#include "Networking/ConnectionManager.h"
#include "configuration/TrackerConfiguration.h"
#include "configuration/NetworkConfiguration.h"

unsigned long lastSendTime = 0;

WiFiUDP udp;
BNO08x imu(D1, D2, 0x4A, D5);

void setup() {
    Serial.begin(115200);
    delay(1000);
    ConnectionManager::begin(SSID, PASS, SERVER_IP, SERVER_PORT); 

    if (!imu.begin()) 
    {
        Serial.println("IMU not detected.");
        while (true) delay(1000);
    }

    Serial.println("IMU Initialized.");
}

void loop() 
{
    unsigned long now = millis();

    if (now - lastSendTime >= IMU_PERIOD_MS) 
    {
        lastSendTime = now;

        if (imu.update()) 
        {
            Quaternion corrected = imu.GetCorrectedQuaternion();

            // Format: SUIT_ID:JOINT:quat_values
            String packet = JOINT;
            packet += ":";
            packet += imu.getQuatString(corrected);
            Serial.println(packet);

            ConnectionManager::sendMessage(packet);
        }
    }
}